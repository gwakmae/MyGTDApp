using MyGtdApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyGtdApp.Services
{
    public class InMemoryTaskService : ITaskService
    {
        public event Action? OnChange;

        private readonly List<TaskItem> _tasks;
        private int _nextId = 1;

        private readonly InMemoryTaskQueryHelper _queryHelper;
        private readonly InMemoryTaskManipulationHelper _manipulationHelper;
        private readonly InMemoryTaskDataHelper _dataHelper;

        public InMemoryTaskService(List<TaskItem> initialTasks)
        {
            _tasks = initialTasks ?? new List<TaskItem>();
            if (_tasks.Any())
            {
                _nextId = _tasks.Max(t => t.Id) + 1;
            }

            _queryHelper = new InMemoryTaskQueryHelper(_tasks);
            _manipulationHelper = new InMemoryTaskManipulationHelper(_tasks);
            _dataHelper = new InMemoryTaskDataHelper(_tasks, () => _tasks.Any() ? _tasks.Max(t => t.Id) + 1 : 1, newId => _nextId = newId);
        }

        private void NotifyStateChanged() => OnChange?.Invoke();

        // --- Query Methods (Read Operations) ---
        public Task<List<TaskItem>> GetAllTasksAsync()
            => Task.FromResult(_queryHelper.GetAllTasksAsTree());

        public Task<List<TaskItem>> GetTodayTasksAsync()
            => Task.FromResult(_queryHelper.GetTodayTasks());

        public Task<List<string>> GetAllContextsAsync()
            => Task.FromResult(_queryHelper.GetAllContexts());

        public Task<List<TaskItem>> GetTasksByContextAsync(string context)
            => Task.FromResult(_queryHelper.GetTasksByContext(context));

        public Task<List<TaskItem>> GetFocusTasksAsync()
            => Task.FromResult(_queryHelper.GetFocusTasks());

        // ✨ 추가: 인터페이스 구현 오류 해결
        public Task<List<TaskItem>> GetActiveTasksAsync()
            => Task.FromResult(_queryHelper.GetActiveTasks());

        // --- Manipulation Methods (Write Operations) ---
        public Task<TaskItem> AddTaskAsync(string title, Models.TaskStatus status, int? parentId)
        {
            var newTask = new TaskItem { Id = _nextId++, Title = title, Status = status, ParentId = parentId };
            _manipulationHelper.AddTask(newTask);
            NotifyStateChanged();
            return Task.FromResult(newTask);
        }

        public Task UpdateTaskAsync(TaskItem taskToUpdate)
        {
            _manipulationHelper.UpdateTask(taskToUpdate);
            NotifyStateChanged();
            return Task.CompletedTask;
        }

        public Task ToggleCompleteStatusAsync(int taskId)
        {
            _manipulationHelper.ToggleCompleteStatus(taskId);
            NotifyStateChanged();
            return Task.CompletedTask;
        }

        public Task DeleteTaskAsync(int taskId)
        {
            _manipulationHelper.DeleteTaskRecursive(taskId);
            NotifyStateChanged();
            return Task.CompletedTask;
        }

        public Task DeleteTasksAsync(List<int> taskIds)
        {
            _manipulationHelper.DeleteTasksRecursive(taskIds);
            NotifyStateChanged();
            return Task.CompletedTask;
        }

        public Task MoveTaskAsync(int taskId, Models.TaskStatus newStatus, int? newParentId, int newSortOrder)
        {
            _manipulationHelper.MoveTasks(new List<int> { taskId }, newStatus, newParentId, newSortOrder);
            NotifyStateChanged();
            return Task.CompletedTask;
        }

        public Task MoveTasksAsync(List<int> taskIds, Models.TaskStatus newStatus, int? newParentId, int newSortOrder)
        {
            _manipulationHelper.MoveTasks(taskIds, newStatus, newParentId, newSortOrder);
            NotifyStateChanged();
            return Task.CompletedTask;
        }

        public Task BulkUpdateTasksAsync(BulkUpdateModel model)
        {
            _manipulationHelper.BulkUpdateTasks(model);
            NotifyStateChanged();
            return Task.CompletedTask;
        }

        public Task UpdateTaskExpandStateAsync(int taskId, bool isExpanded)
        {
            _manipulationHelper.UpdateTaskExpandState(taskId, isExpanded);
            return Task.CompletedTask;
        }

        public Task DeleteAllCompletedTasksAsync()
        {
            _manipulationHelper.DeleteAllCompletedTasks();
            NotifyStateChanged();
            return Task.CompletedTask;
        }

        public Task DeleteContextAsync(string context)
        {
            _manipulationHelper.DeleteContext(context);
            NotifyStateChanged();
            return Task.CompletedTask;
        }

        // --- Data Import/Export Methods ---
        public Task<string> ExportTasksToJsonAsync()
            => Task.FromResult(_dataHelper.ExportTasksToJson());

        public Task ImportTasksFromJsonAsync(string jsonData)
        {
            _dataHelper.ImportTasksFromJson(jsonData);
            NotifyStateChanged();
            return Task.CompletedTask;
        }
    }
}