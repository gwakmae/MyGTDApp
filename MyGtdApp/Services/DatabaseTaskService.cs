using Microsoft.EntityFrameworkCore;
using MyGtdApp.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// [수정됨] 이름 충돌을 피하기 위해 using 구문을 명시적으로 사용합니다.
using TaskStatus = MyGtdApp.Models.TaskStatus;


namespace MyGtdApp.Services
{
    public class DatabaseTaskService : ITaskService
    {
        private readonly GtdDbContext _context;

        public event System.Action? OnChange;

        public DatabaseTaskService(GtdDbContext context)
        {
            _context = context;
        }

        private void NotifyStateChanged() => OnChange?.Invoke();

        // [수정됨] 모호성을 없애기 위해 전체 경로를 사용합니다.
        public async Task<TaskItem> AddTaskAsync(string title, Models.TaskStatus status, int? parentId)
        {
            var maxSortOrder = await _context.Tasks
                .Where(t => t.ParentId == parentId && t.Status == status)
                .Select(t => (int?)t.SortOrder)
                .MaxAsync() ?? -1;

            var newTask = new TaskItem
            {
                Title = title,
                Status = status,
                ParentId = parentId,
                SortOrder = maxSortOrder + 1
            };

            _context.Tasks.Add(newTask);
            await _context.SaveChangesAsync();
            NotifyStateChanged();
            return newTask;
        }

        public async Task DeleteTaskAsync(int taskId)
        {
            var taskToDelete = await _context.Tasks.FindAsync(taskId);
            if (taskToDelete != null)
            {
                await DeleteChildrenRecursive(taskId);
                _context.Tasks.Remove(taskToDelete);
                await _context.SaveChangesAsync();
                NotifyStateChanged();
            }
        }
        private async Task DeleteChildrenRecursive(int parentId)
        {
            var children = await _context.Tasks.Where(t => t.ParentId == parentId).ToListAsync();
            foreach (var child in children)
            {
                await DeleteChildrenRecursive(child.Id);
                _context.Tasks.Remove(child);
            }
        }


        public async Task<List<TaskItem>> GetAllTasksAsync()
        {
            var allTasks = await _context.Tasks.Include(t => t.Children).ToListAsync();
            var topLevelTasks = allTasks.Where(t => t.ParentId == null)
                                        .OrderBy(t => t.SortOrder)
                                        .ToList();
            // 자식들을 정렬하는 로직은 클라이언트 측에서 처리하거나, 필요하다면 재귀적으로 로드해야 합니다.
            // 여기서는 단순화하여 1단계 자식만 로드합니다.
            foreach (var task in topLevelTasks)
            {
                task.Children = allTasks.Where(t => t.ParentId == task.Id).OrderBy(t => t.SortOrder).ToList();
            }
            return topLevelTasks;
        }


        public async Task<List<string>> GetAllContextsAsync()
        {
            var allContexts = await _context.Tasks
                                     .SelectMany(t => t.Contexts)
                                     .Distinct()
                                     .OrderBy(c => c)
                                     .ToListAsync();
            return allContexts;
        }

        public async Task<List<TaskItem>> GetTasksByContextAsync(string context)
        {
            return await _context.Tasks
                .Where(t => !t.IsCompleted && t.Contexts.Contains(context))
                .OrderBy(t => t.Status)
                .ThenBy(t => t.SortOrder)
                .ToListAsync();
        }

        public async Task<List<TaskItem>> GetTodayTasksAsync()
        {
            var today = System.DateTime.Today;
            return await _context.Tasks.Where(t =>
                !t.IsCompleted &&
                t.StartDate.HasValue &&
                t.StartDate.Value.Date <= today
            ).OrderBy(t => t.DueDate ?? System.DateTime.MaxValue)
             .ThenByDescending(t => t.Priority)
             .ToListAsync();
        }

        // [수정됨] 모호성을 없애기 위해 전체 경로를 사용합니다.
        public async Task MoveTaskAsync(int taskId, Models.TaskStatus newStatus, int? newParentId, int newSortOrder)
        {
            var taskToMove = await _context.Tasks.FindAsync(taskId);
            if (taskToMove != null)
            {
                var oldStatus = taskToMove.Status;

                // 재정렬 로직
                var oldSiblings = await _context.Tasks.Where(t => t.ParentId == taskToMove.ParentId && t.Status == oldStatus && t.Id != taskId).OrderBy(t => t.SortOrder).ToListAsync();
                for (int i = 0; i < oldSiblings.Count; i++) { oldSiblings[i].SortOrder = i; }

                taskToMove.ParentId = newParentId;
                taskToMove.Status = newStatus;

                var newSiblings = await _context.Tasks.Where(t => t.ParentId == newParentId && t.Status == newStatus && t.Id != taskId).OrderBy(t => t.SortOrder).ToListAsync();
                newSortOrder = System.Math.Clamp(newSortOrder, 0, newSiblings.Count);
                newSiblings.Insert(newSortOrder, taskToMove);
                for (int i = 0; i < newSiblings.Count; i++) { newSiblings[i].SortOrder = i; }

                await _context.SaveChangesAsync();
                NotifyStateChanged();
            }
        }

        public async Task ToggleCompleteStatusAsync(int taskId)
        {
            var task = await _context.Tasks.FindAsync(taskId);
            if (task != null)
            {
                task.IsCompleted = !task.IsCompleted;
                // [수정됨] 모호성을 없애기 위해 전체 경로를 사용합니다.
                task.Status = task.IsCompleted ? Models.TaskStatus.Completed : Models.TaskStatus.NextActions;
                await _context.SaveChangesAsync();
                NotifyStateChanged();
            }
        }

        public async Task UpdateTaskAsync(TaskItem taskToUpdate)
        {
            _context.Tasks.Update(taskToUpdate);
            await _context.SaveChangesAsync();
            NotifyStateChanged();
        }
    }
}