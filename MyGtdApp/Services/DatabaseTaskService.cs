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
        // [변경] DbContext 대신 DbContextFactory를 주입받습니다.
        private readonly IDbContextFactory<GtdDbContext> _dbContextFactory;

        public event System.Action? OnChange;

        // [변경] 생성자에서 DbContextFactory를 주입받습니다.
        public DatabaseTaskService(IDbContextFactory<GtdDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        private void NotifyStateChanged() => OnChange?.Invoke();

        // [변경] 모든 메서드에서 using 구문으로 context를 생성하도록 수정

        public async Task<TaskItem> AddTaskAsync(string title, Models.TaskStatus status, int? parentId)
        {
            using var context = _dbContextFactory.CreateDbContext();

            var maxSortOrder = await context.Tasks
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

            context.Tasks.Add(newTask);
            await context.SaveChangesAsync();
            NotifyStateChanged();
            return newTask;
        }

        public async Task DeleteTaskAsync(int taskId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            var taskToDelete = await context.Tasks.FindAsync(taskId);
            if (taskToDelete != null)
            {
                await DeleteChildrenRecursive(context, taskId);
                context.Tasks.Remove(taskToDelete);
                await context.SaveChangesAsync();
                NotifyStateChanged();
            }
        }
        private async Task DeleteChildrenRecursive(GtdDbContext context, int parentId)
        {
            var children = await context.Tasks.Where(t => t.ParentId == parentId).ToListAsync();
            foreach (var child in children)
            {
                await DeleteChildrenRecursive(context, child.Id);
                context.Tasks.Remove(child);
            }
        }

        public async Task<List<TaskItem>> GetAllTasksAsync()
        {
            using var context = _dbContextFactory.CreateDbContext();
            var allTasks = await context.Tasks.Include(t => t.Children).ToListAsync();
            var topLevelTasks = allTasks.Where(t => t.ParentId == null)
                                         .OrderBy(t => t.SortOrder)
                                         .ToList();
            foreach (var task in topLevelTasks)
            {
                task.Children = allTasks.Where(t => t.ParentId == task.Id).OrderBy(t => t.SortOrder).ToList();
            }
            return topLevelTasks;
        }

        public async Task<List<string>> GetAllContextsAsync()
        {
            using var context = _dbContextFactory.CreateDbContext();

            // 1. 먼저 DB에서 모든 Task를 메모리로 가져옵니다. (단순한 요청)
            var allTasks = await context.Tasks.ToListAsync();

            // 2. 메모리로 가져온 데이터를 C# 코드로 가공합니다. (복잡한 작업)
            var allContexts = allTasks
                                     .SelectMany(t => t.Contexts)
                                     .Distinct()
                                     .OrderBy(c => c)
                                     .ToList(); // 이미 메모리에 있으므로 ToList() 사용

            return allContexts;
        }

        public async Task<List<TaskItem>> GetTasksByContextAsync(string context)
        {
            using var contextDb = _dbContextFactory.CreateDbContext();
            return await contextDb.Tasks
                .Where(t => !t.IsCompleted && t.Contexts.Contains(context))
                .OrderBy(t => t.Status)
                .ThenBy(t => t.SortOrder)
                .ToListAsync();
        }

        public async Task<List<TaskItem>> GetTodayTasksAsync()
        {
            using var context = _dbContextFactory.CreateDbContext();
            var today = System.DateTime.Today;
            return await context.Tasks.Where(t =>
                !t.IsCompleted &&
                t.StartDate.HasValue &&
                t.StartDate.Value.Date <= today
            ).OrderBy(t => t.DueDate ?? System.DateTime.MaxValue)
             .ThenByDescending(t => t.Priority)
             .ToListAsync();
        }

        public async Task MoveTaskAsync(int taskId, Models.TaskStatus newStatus, int? newParentId, int newSortOrder)
        {
            using var context = _dbContextFactory.CreateDbContext();
            var taskToMove = await context.Tasks.FindAsync(taskId);
            if (taskToMove != null)
            {
                var oldStatus = taskToMove.Status;

                var oldSiblings = await context.Tasks.Where(t => t.ParentId == taskToMove.ParentId && t.Status == oldStatus && t.Id != taskId).OrderBy(t => t.SortOrder).ToListAsync();
                for (int i = 0; i < oldSiblings.Count; i++) { oldSiblings[i].SortOrder = i; }

                taskToMove.ParentId = newParentId;
                taskToMove.Status = newStatus;

                var newSiblings = await context.Tasks.Where(t => t.ParentId == newParentId && t.Status == newStatus && t.Id != taskId).OrderBy(t => t.SortOrder).ToListAsync();
                newSortOrder = System.Math.Clamp(newSortOrder, 0, newSiblings.Count);
                newSiblings.Insert(newSortOrder, taskToMove);
                for (int i = 0; i < newSiblings.Count; i++) { newSiblings[i].SortOrder = i; }

                await context.SaveChangesAsync();
                NotifyStateChanged();
            }
        }

        public async Task ToggleCompleteStatusAsync(int taskId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            var task = await context.Tasks.FindAsync(taskId);
            if (task != null)
            {
                task.IsCompleted = !task.IsCompleted;
                task.Status = task.IsCompleted ? Models.TaskStatus.Completed : Models.TaskStatus.NextActions;
                await context.SaveChangesAsync();
                NotifyStateChanged();
            }
        }

        public async Task UpdateTaskAsync(TaskItem taskToUpdate)
        {
            using var context = _dbContextFactory.CreateDbContext();
            context.Tasks.Update(taskToUpdate);
            await context.SaveChangesAsync();
            NotifyStateChanged();
        }
    }
}