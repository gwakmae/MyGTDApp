// 파일명: Services/DatabaseTaskService.cs
using Microsoft.EntityFrameworkCore;
using MyGtdApp.Models;
using MyGtdApp.Repositories;
using MyGtdApp.Services.Undo; // <-- using 문 추가
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskStatus = MyGtdApp.Models.TaskStatus;

namespace MyGtdApp.Services
{
    public class DatabaseTaskService : ITaskService
    {
        private readonly ITaskRepository _repository;
        private readonly ITaskMoveService _moveService;
        private readonly ITaskDataService _dataService;
        // --- 아래 2개 필드 추가 ---
        private readonly IUndoService _undo;
        private readonly IDbContextFactory<GtdDbContext> _contextFactory;

        public event System.Action? OnChange;

        // --- 생성자 수정 ---
        public DatabaseTaskService(
            ITaskRepository repository,
            ITaskMoveService moveService,
            ITaskDataService dataService,
            IUndoService undoService,
            IDbContextFactory<GtdDbContext> contextFactory)
        {
            _repository = repository;
            _moveService = moveService;
            _dataService = dataService;
            _undo = undoService; // 추가
            _contextFactory = contextFactory; // 추가
        }

        private void NotifyStateChanged() => OnChange?.Invoke();

        // ✨ 수정: 인터페이스와 일치하도록 bool showHidden 파라미터를 추가
        public async Task<List<TaskItem>> GetActiveTasksAsync(bool showHidden)
        {
            var allTasks = await _repository.GetAllAsync(); // 계층 구조로 가져옴
            var today = DateTime.Today;

            var activeFiltered = new List<TaskItem>();

            void FilterRecursive(IEnumerable<TaskItem> tasks, bool isParentHidden)
            {
                foreach (var task in tasks)
                {
                    bool isEffectivelyHidden = isParentHidden || task.IsHidden;

                    if (showHidden || !isEffectivelyHidden)
                    {
                        bool meetsActiveCriteria =
                            task.Status != TaskStatus.Inbox &&
                            !task.Children.Any() &&
                            !task.IsCompleted &&
                            (!task.StartDate.HasValue || task.StartDate.Value.Date <= today);

                        if (meetsActiveCriteria)
                        {
                            activeFiltered.Add(task);
                        }
                    }

                    if (task.Children.Any())
                    {
                        FilterRecursive(task.Children, isEffectivelyHidden);
                    }
                }
            }

            FilterRecursive(allTasks, false);

            return activeFiltered
                .OrderBy(t => t.Status)
                .ThenBy(t => t.SortOrder)
                .ToList();
        }

        // --- 이하 다른 모든 메서드는 그대로 유지됩니다 ---
        public async Task<List<TaskItem>> GetAllTasksAsync() => await _repository.GetAllAsync();

        public async Task<TaskItem> AddTaskAsync(string title, TaskStatus status, int? parentId)
        {
            var newTask = new TaskItem { Title = title, Status = status, ParentId = parentId };
            var result = await _repository.AddAsync(newTask);
            NotifyStateChanged();
            return result;
        }

        // --- DeleteTaskAsync, DeleteTasksAsync 수정 ---
        public async Task DeleteTaskAsync(int taskId)
        {
            await PrepareDeleteUndo(new List<int> { taskId });
            await _repository.DeleteAsync(taskId);
            NotifyStateChanged();
        }

        public async Task UpdateTaskAsync(TaskItem taskToUpdate)
        {
            var existingTask = await _repository.GetByIdAsync(taskToUpdate.Id);
            if (existingTask == null) return;

            bool contextsChanged = !existingTask.Contexts.SequenceEqual(taskToUpdate.Contexts);
            bool hiddenStateChanged = existingTask.IsHidden != taskToUpdate.IsHidden;

            await _repository.UpdateAsync(taskToUpdate);

            if (hiddenStateChanged)
            {
                await CascadeHiddenStateAsync(taskToUpdate.Id, taskToUpdate.IsHidden);
            }

            if (contextsChanged) Console.WriteLine("컨텍스트 변경 감지 - OnChange 이벤트 발생");
            NotifyStateChanged();
        }

        private async Task CascadeHiddenStateAsync(int parentId, bool isHidden)
        {
            var allTasks = await _repository.GetAllRawAsync();
            var parentTask = allTasks.FirstOrDefault(t => t.Id == parentId);
            if (parentTask == null || string.IsNullOrEmpty(parentTask.Path)) return;

            var descendantsToUpdate = allTasks
                .Where(t => t.Path.StartsWith(parentTask.Path + "/") && t.IsHidden != isHidden)
                .ToList();

            if (!descendantsToUpdate.Any()) return;

            foreach (var d in descendantsToUpdate)
                d.IsHidden = isHidden;

            await _repository.UpdateRangeAsync(descendantsToUpdate);
        }

        public async Task MoveTaskAsync(int taskId, TaskStatus newStatus, int? newParentId, int newSortOrder)
        {
            await _moveService.MoveTaskAsync(taskId, newStatus, newParentId, newSortOrder);
            NotifyStateChanged();
        }

        public async Task MoveTasksAsync(List<int> taskIds, TaskStatus newStatus, int? newParentId, int newSortOrder)
        {
            await _moveService.MoveTasksAsync(taskIds, newStatus, newParentId, newSortOrder);
            NotifyStateChanged();
        }

        public async Task ToggleCompleteStatusAsync(int taskId)
        {
            var allTasks = await _repository.GetAllRawAsync();
            var lookup = allTasks.ToLookup(t => t.ParentId);
            var task = allTasks.FirstOrDefault(t => t.Id == taskId);
            if (task is null) return;

            bool completed = !task.IsCompleted;

            var toUpdate = new List<TaskItem>();
            var processedIds = new HashSet<int>();

            void UpdateRecursively(TaskItem currentTask, bool isCompleted)
            {
                if (!processedIds.Add(currentTask.Id)) return;

                currentTask.IsCompleted = isCompleted;
                if (isCompleted)
                {
                    if (currentTask.Status != TaskStatus.Completed)
                    {
                        currentTask.OriginalStatus = currentTask.Status;
                    }
                    currentTask.Status = TaskStatus.Completed;
                }
                else
                {
                    currentTask.Status = currentTask.OriginalStatus ?? TaskStatus.NextActions;
                    currentTask.OriginalStatus = null;
                }
                toUpdate.Add(currentTask);

                foreach (var child in lookup[currentTask.Id])
                {
                    UpdateRecursively(child, isCompleted);
                }
            }

            UpdateRecursively(task, completed);

            // 🎯 [수정된 로직] 마지막 자식이 완료되면 부모도 완료 처리
            if (completed && task.ParentId.HasValue)
            {
                var parent = allTasks.FirstOrDefault(t => t.Id == task.ParentId.Value);
                while (parent != null && !parent.IsCompleted)
                {
                    var siblings = lookup[parent.Id].ToList();
                    if (siblings.All(s => s.IsCompleted))
                    {
                        UpdateRecursively(parent, true);

                        // 다음 부모로 이동하여 계속 확인
                        parent = parent.ParentId.HasValue
                            ? allTasks.FirstOrDefault(t => t.Id == parent.ParentId.Value)
                            : null;
                    }
                    else
                    {
                        // 모든 자식이 완료되지 않았으므로 연쇄 중단
                        break;
                    }
                }
            }

            await _repository.UpdateRangeAsync(toUpdate);
            NotifyStateChanged();
        }

        public async Task<List<TaskItem>> GetTodayTasksAsync() => await _repository.GetTodayTasksAsync();
        public async Task<List<string>> GetAllContextsAsync() => await _repository.GetAllContextsAsync();
        public async Task<List<TaskItem>> GetTasksByContextAsync(string context) => await _repository.GetByContextAsync(context);
        public async Task<List<TaskItem>> GetFocusTasksAsync() => await _repository.GetFocusTasksAsync();

        public async Task BulkUpdateTasksAsync(BulkUpdateModel updateModel)
        {
            await _repository.BulkUpdateTasksAsync(updateModel);
            NotifyStateChanged();
        }

        public async Task DeleteTasksAsync(List<int> taskIds)
        {
            await PrepareDeleteUndo(taskIds);
            await _repository.DeleteTasksAsync(taskIds);
            NotifyStateChanged();
        }

        public async Task<string> ExportTasksToJsonAsync() => await _dataService.ExportTasksToJsonAsync();

        public async Task ImportTasksFromJsonAsync(string jsonData)
        {
            await _dataService.ImportTasksFromJsonAsync(jsonData);
            NotifyStateChanged();
        }

        public async Task UpdateTaskExpandStateAsync(int taskId, bool isExpanded)
        {
            await _repository.UpdateExpandStateAsync(taskId, isExpanded);
        }

        public async Task DeleteAllCompletedTasksAsync()
        {
            await _repository.DeleteByStatusRecursiveAsync(TaskStatus.Completed);
            NotifyStateChanged();
        }

        public async Task DeleteContextAsync(string context)
        {
            var allTasks = await _repository.GetAllRawAsync();
            var tasksWithContext = allTasks.Where(t => t.Contexts.Contains(context)).ToList();
            if (!tasksWithContext.Any()) return;

            foreach (var t in tasksWithContext)
            {
                t.Contexts.Remove(context);
            }

            await _repository.UpdateRangeAsync(tasksWithContext);
            NotifyStateChanged();
        }

        // --- 아래 3개의 새 메서드를 클래스 내부에 추가 ---

        // 헬퍼: 삭제 직전의 Task 정보(스냅샷)를 Undo 서비스에 저장
        private async Task PrepareDeleteUndo(List<int> rootIds)
        {
            var allRaw = await _repository.GetAllRawAsync();
            var snapshot = CaptureSubtrees(allRaw, rootIds);

            if (!snapshot.Any()) return;

            _undo.Push(new UndoAction
            {
                Type = UndoActionType.Delete,
                Description = $"{snapshot.Count}개 작업 삭제됨",
                UndoAsync = async () =>
                {
                    await RestoreDeletedAsync(snapshot);
                    NotifyStateChanged();
                }
            });
        }

        // 헬퍼: 삭제된 Task 들을 DB에 다시 삽입하는 복원 로직
        private async Task RestoreDeletedAsync(List<TaskItem> items)
        {
            await using var ctx = _contextFactory.CreateDbContext();
            // ID가 충돌할 수 있으므로, 복원하려는 항목과 ID가 같은 항목이 이미 DB에 있는지 확인하고 제거
            var existingIds = items.Select(i => i.Id).ToList();
            var conflicts = await ctx.Tasks.Where(t => existingIds.Contains(t.Id)).ToListAsync();
            if (conflicts.Any())
            {
                ctx.Tasks.RemoveRange(conflicts);
                await ctx.SaveChangesAsync();
            }

            ctx.Tasks.AddRange(items);
            await ctx.SaveChangesAsync();
            await Infrastructure.Seeders.FillPathDepth.RunAsync(ctx); // Path/Depth 재계산
        }

        // 헬퍼: 삭제할 Task와 그 모든 자식 Task들을 복사하여 스냅샷 생성
        private static List<TaskItem> CaptureSubtrees(List<TaskItem> allRaw, IEnumerable<int> rootIds)
        {
            var map = allRaw.ToDictionary(t => t.Id);
            var byParent = allRaw.ToLookup(t => t.ParentId);
            var seen = new HashSet<int>();
            var result = new List<TaskItem>();

            foreach (var rootId in rootIds.Distinct())
            {
                if (map.ContainsKey(rootId)) Collect(rootId);
            }
            return result;

            void Collect(int id)
            {
                if (!seen.Add(id)) return;
                var src = map[id];
                var copy = new TaskItem // 참조가 아닌 값으로 깊은 복사
                {
                    Id = src.Id,
                    Title = src.Title,
                    Description = src.Description,
                    Priority = src.Priority,
                    Status = src.Status,
                    ParentId = src.ParentId,
                    SortOrder = src.SortOrder,
                    IsCompleted = src.IsCompleted,
                    OriginalStatus = src.OriginalStatus,
                    StartDate = src.StartDate,
                    DueDate = src.DueDate,
                    IsExpanded = src.IsExpanded,
                    IsHidden = src.IsHidden,
                    Path = src.Path,
                    Depth = src.Depth,
                    Contexts = new List<string>(src.Contexts)
                };
                result.Add(copy);

                foreach (var child in byParent[id])
                    Collect(child.Id);
            }
        }
    }
}
