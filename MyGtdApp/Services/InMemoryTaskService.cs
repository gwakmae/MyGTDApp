using MyGtdApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyGtdApp.Services
{
    public class InMemoryTaskService : ITaskService
    {
        private readonly List<TaskItem> _tasks;
        private int _nextId = 1000;

        public event Action? OnChange;
        private void NotifyStateChanged() => OnChange?.Invoke();

        public InMemoryTaskService(List<TaskItem> initialTasks)
        {
            _tasks = initialTasks;
            if (_tasks.Any())
            {
                _nextId = _tasks.Max(t => t.Id) + 1;
            }
        }

        public Task<List<TaskItem>> GetAllTasksAsync()
        {
            var taskMap = _tasks.ToDictionary(t => t.Id);
            var topLevelTasks = new List<TaskItem>();
            foreach (var task in _tasks) { task.Children.Clear(); }
            foreach (var task in _tasks)
            {
                if (task.ParentId.HasValue && taskMap.TryGetValue(task.ParentId.Value, out var parent))
                {
                    parent.Children.Add(task);
                }
                else
                {
                    topLevelTasks.Add(task);
                }
            }
            void SortChildrenRecursive(TaskItem parentTask)
            {
                if (parentTask.Children.Any())
                {
                    var sortedChildren = parentTask.Children.OrderBy(c => c.SortOrder).ToList();
                    parentTask.Children.Clear();
                    parentTask.Children.AddRange(sortedChildren);
                    foreach (var child in parentTask.Children) { SortChildrenRecursive(child); }
                }
            }
            var sortedTopLevel = topLevelTasks.OrderBy(t => t.SortOrder).ToList();
            foreach (var task in sortedTopLevel) { SortChildrenRecursive(task); }
            return Task.FromResult(sortedTopLevel);
        }

        public Task<TaskItem> AddTaskAsync(string title, Models.TaskStatus status, int? parentId)
        {
            var maxSortOrder = _tasks.Where(t => t.ParentId == parentId && t.Status == status).Select(t => (int?)t.SortOrder).Max() ?? -1;
            var newTask = new TaskItem { Id = _nextId++, Title = title, Status = status, ParentId = parentId, SortOrder = maxSortOrder + 1 };
            _tasks.Add(newTask);
            NotifyStateChanged();
            return Task.FromResult(newTask);
        }

        public Task UpdateTaskAsync(TaskItem taskToUpdate)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == taskToUpdate.Id);
            if (task != null)
            {
                task.Title = taskToUpdate.Title;
                task.Priority = taskToUpdate.Priority;
                task.StartDate = taskToUpdate.StartDate;
                task.DueDate = taskToUpdate.DueDate;
                task.Contexts = taskToUpdate.Contexts;
                if (task.Status == Models.TaskStatus.Inbox && task.StartDate.HasValue)
                {
                    task.Status = Models.TaskStatus.NextActions;
                }
                NotifyStateChanged();
            }
            return Task.CompletedTask;
        }

        public Task ToggleCompleteStatusAsync(int taskId)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == taskId);
            if (task != null)
            {
                task.IsCompleted = !task.IsCompleted;
                task.Status = task.IsCompleted ? Models.TaskStatus.Completed : Models.TaskStatus.NextActions;
                NotifyStateChanged();
            }
            return Task.CompletedTask;
        }

        public Task DeleteTaskAsync(int taskId)
        {
            var taskToDelete = _tasks.FirstOrDefault(t => t.Id == taskId);
            if (taskToDelete != null)
            {
                var childrenToDelete = _tasks.Where(t => t.ParentId == taskId).ToList();
                foreach (var child in childrenToDelete)
                {
                    // 자식 항목을 재귀적으로 삭제
                    _ = DeleteTaskAsync(child.Id);
                }
                _tasks.Remove(taskToDelete);
                NotifyStateChanged();
            }
            return Task.CompletedTask;
        }

        public Task MoveTaskAsync(int taskId, Models.TaskStatus newStatus, int? newParentId, int newSortOrder)
        {
            var taskToMove = _tasks.FirstOrDefault(t => t.Id == taskId);
            if (taskToMove != null)
            {
                var oldParentId = taskToMove.ParentId;
                var oldStatus = taskToMove.Status;
                if (oldParentId != taskToMove.ParentId || oldStatus != taskToMove.Status)
                {
                    var sourceSiblings = _tasks.Where(t => t.ParentId == oldParentId && t.Status == oldStatus && t.Id != taskId).OrderBy(t => t.SortOrder).ToList();
                    for (int i = 0; i < sourceSiblings.Count; i++) { sourceSiblings[i].SortOrder = i; }
                }
                taskToMove.ParentId = newParentId;
                taskToMove.Status = newStatus;
                var destinationSiblings = _tasks.Where(t => t.ParentId == newParentId && t.Status == newStatus && t.Id != taskId).OrderBy(t => t.SortOrder).ToList();
                newSortOrder = Math.Clamp(newSortOrder, 0, destinationSiblings.Count);
                destinationSiblings.Insert(newSortOrder, taskToMove);
                for (int i = 0; i < destinationSiblings.Count; i++) { destinationSiblings[i].SortOrder = i; }
                NotifyStateChanged();
            }
            return Task.CompletedTask;
        }

        public Task<List<TaskItem>> GetTodayTasksAsync()
        {
            var today = DateTime.Today;
            var result = _tasks.Where(t =>
                !t.IsCompleted &&
                t.StartDate.HasValue &&
                t.StartDate.Value.Date <= today
            ).OrderBy(t => t.DueDate ?? DateTime.MaxValue)
             .ThenByDescending(t => t.Priority)
             .ToList();
            return Task.FromResult(result);
        }

        public Task<List<string>> GetAllContextsAsync()
        {
            var result = _tasks
                .SelectMany(t => t.Contexts)
                .Distinct()
                .OrderBy(c => c)
                .ToList();
            return Task.FromResult(result);
        }

        public Task<List<TaskItem>> GetTasksByContextAsync(string context)
        {
            var result = _tasks
                .Where(t => !t.IsCompleted && t.Contexts.Contains(context, StringComparer.OrdinalIgnoreCase)) // Modified line
                .OrderBy(t => t.Status)
                .ThenBy(t => t.SortOrder)
                .ToList();
            return Task.FromResult(result);
        }
    }
}