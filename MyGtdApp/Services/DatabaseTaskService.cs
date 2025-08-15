// 파일명: Services/DatabaseTaskService.cs
using MyGtdApp.Models;
using MyGtdApp.Repositories;
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

        public event System.Action? OnChange;

        public DatabaseTaskService(
            ITaskRepository repository,
            ITaskMoveService moveService,
            ITaskDataService dataService)
        {
            _repository = repository;
            _moveService = moveService;
            _dataService = dataService;
        }

        private void NotifyStateChanged() => OnChange?.Invoke();

        public async Task<List<TaskItem>> GetActiveTasksAsync() // ✨ 추가
        {
            var allTasks = await _repository.GetAllRawAsync();
            var today = DateTime.Today;

            // 자식 관계를 파악하기 위해 모든 작업을 조회하고 메모리에서 필터링
            var taskLookup = allTasks.ToLookup(t => t.ParentId);

            return allTasks.Where(t =>
            {
                // 1. Inbox 상태는 제외
                if (t.Status == TaskStatus.Inbox) return false;

                // 2. 프로젝트(자식이 있는 작업)는 제외
                if (taskLookup.Contains(t.Id)) return false;

                // 3. 이미 완료된 작업은 제외
                if (t.IsCompleted) return false;

                // 4. 시작 날짜가 미래로 지정된 작업은 제외
                if (t.StartDate.HasValue && t.StartDate.Value.Date > today) return false;

                return true;
            })
            .OrderBy(t => t.Status)
            .ThenBy(t => t.SortOrder)
            .ToList();
        }

        // --- 기존 메서드들은 그대로 유지 ---
        public async Task<List<TaskItem>> GetAllTasksAsync() => await _repository.GetAllAsync();

        public async Task<TaskItem> AddTaskAsync(string title, TaskStatus status, int? parentId)
        {
            var newTask = new TaskItem { Title = title, Status = status, ParentId = parentId };
            var result = await _repository.AddAsync(newTask);
            NotifyStateChanged();
            return result;
        }

        public async Task DeleteTaskAsync(int taskId)
        {
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

            if (completed)
            {
                task.OriginalStatus = task.Status;
                task.Status = TaskStatus.Completed;
            }
            else
            {
                task.Status = task.OriginalStatus ?? TaskStatus.NextActions;
                task.OriginalStatus = null;
            }
            task.IsCompleted = completed;

            var toUpdate = new List<TaskItem> { task };

            void Visit(int parent)
            {
                foreach (var child in lookup[parent])
                {
                    child.IsCompleted = completed;
                    if (completed)
                    {
                        child.OriginalStatus = child.Status;
                        child.Status = TaskStatus.Completed;
                    }
                    else
                    {
                        child.Status = child.OriginalStatus ?? TaskStatus.NextActions;
                        child.OriginalStatus = null;
                    }
                    toUpdate.Add(child);
                    Visit(child.Id);
                }
            }
            Visit(task.Id);

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
    }
}