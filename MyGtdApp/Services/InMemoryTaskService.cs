using MyGtdApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;

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
                if (!task.IsCompleted)
                {
                    task.OriginalStatus = task.Status;
                    task.IsCompleted = true;
                    task.Status = Models.TaskStatus.Completed;
                }
                else
                {
                    task.IsCompleted = false;
                    task.Status = task.OriginalStatus ?? Models.TaskStatus.NextActions;
                    task.OriginalStatus = null;
                }
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
                    _ = DeleteTaskAsync(child.Id);
                }
                _tasks.Remove(taskToDelete);
                NotifyStateChanged();
            }
            return Task.CompletedTask;
        }

        public Task MoveTaskAsync(int taskId, Models.TaskStatus newStatus, int? newParentId, int newSortOrder)
        {
            // 🔄 수정: 단일 이동을 다중 이동 로직으로 통합하여 처리
            return MoveTasksAsync(new List<int> { taskId }, newStatus, newParentId, newSortOrder);
        }

        // 🆕 추가: 다중 이동을 위한 `MoveTasksAsync` 구현
        public Task MoveTasksAsync(List<int> taskIds, Models.TaskStatus newStatus, int? newParentId, int newSortOrder)
        {
            if (taskIds == null || !taskIds.Any()) return Task.CompletedTask;

            var tasksToMove = _tasks.Where(t => taskIds.Contains(t.Id)).ToList();
            if (!tasksToMove.Any()) return Task.CompletedTask;
            
            // 기존 위치 정리
            var tasksByOldParent = tasksToMove.GroupBy(t => new { t.ParentId, t.Status });
            foreach (var group in tasksByOldParent)
            {
                var remainingSiblings = _tasks
                    .Where(t => t.ParentId == group.Key.ParentId && t.Status == group.Key.Status && !taskIds.Contains(t.Id))
                    .OrderBy(t => t.SortOrder).ToList();
                for (int i = 0; i < remainingSiblings.Count; i++) remainingSiblings[i].SortOrder = i;
            }

            // 새 위치에 삽입 및 정렬
            var newSiblings = _tasks
                .Where(t => t.ParentId == newParentId && t.Status == newStatus && !taskIds.Contains(t.Id))
                .OrderBy(t => t.SortOrder).ToList();
            
            newSortOrder = Math.Clamp(newSortOrder, 0, newSiblings.Count);

            // 이동할 작업들을 새 위치에 삽입
            var currentOrder = newSortOrder;
            foreach (var task in tasksToMove.OrderBy(t=>t.SortOrder))
            {
                task.ParentId = newParentId;
                task.Status = newStatus;
                // 실제 삽입은 나중에 한번에 처리
            }

            // 전체 목록을 기준으로 정렬
            var allDestinationSiblings = new List<TaskItem>();
            allDestinationSiblings.AddRange(newSiblings);
            allDestinationSiblings.InsertRange(newSortOrder, tasksToMove);

            // 최종 순서 부여
            for (int i = 0; i < allDestinationSiblings.Count; i++)
            {
                allDestinationSiblings[i].SortOrder = i;
            }

            NotifyStateChanged();
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
                .Where(t => !t.IsCompleted && t.Contexts.Contains(context, StringComparer.OrdinalIgnoreCase))
                .OrderBy(t => t.Status)
                .ThenBy(t => t.SortOrder)
                .ToList();
            return Task.FromResult(result);
        }

        public Task<string> ExportTasksToJsonAsync()
        {
            var exportData = new { tasks = _tasks };
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter() }
            };
            var json = JsonSerializer.Serialize(exportData, options);
            return Task.FromResult(json);
        }

        public Task ImportTasksFromJsonAsync(string jsonData)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };
            var importData = JsonSerializer.Deserialize<JsonTaskHelper>(jsonData, options);

            if (importData?.Tasks != null && importData.Tasks.Any())
            {
                _tasks.Clear();
                _tasks.AddRange(importData.Tasks);
                if (_tasks.Any()) _nextId = _tasks.Max(t => t.Id) + 1;
                NotifyStateChanged();
            }

            return Task.CompletedTask;
        }

        public Task UpdateTaskExpandStateAsync(int taskId, bool isExpanded)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == taskId);
            if (task != null) task.IsExpanded = isExpanded;
            return Task.CompletedTask;
        }
        
        public Task DeleteAllCompletedTasksAsync()
        {
            var completedTasks = _tasks.Where(t => t.Status == Models.TaskStatus.Completed).ToList();
            foreach (var task in completedTasks) _tasks.Remove(task);
            NotifyStateChanged();
            return Task.CompletedTask;
        }

        public Task DeleteContextAsync(string context)
        {
            foreach (var task in _tasks)
            {
                if (task.Contexts.Contains(context)) task.Contexts.Remove(context);
            }
            NotifyStateChanged();
            return Task.CompletedTask;
        }
    }
}