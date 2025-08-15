// 파일명: Services/ITaskService.cs
using MyGtdApp.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskStatus = MyGtdApp.Models.TaskStatus;

namespace MyGtdApp.Services
{
    public interface ITaskService
    {
        event Action? OnChange;

        Task<List<TaskItem>> GetAllTasksAsync();
        Task MoveTaskAsync(int taskId, TaskStatus newStatus, int? newParentId, int newSortOrder);
        Task MoveTasksAsync(List<int> taskIds, TaskStatus newStatus, int? newParentId, int newSortOrder);
        Task<TaskItem> AddTaskAsync(string title, TaskStatus status, int? parentId);
        Task DeleteTaskAsync(int taskId);
        Task UpdateTaskAsync(TaskItem taskToUpdate);
        Task ToggleCompleteStatusAsync(int taskId);
        Task<List<TaskItem>> GetTodayTasksAsync();
        Task<List<string>> GetAllContextsAsync();
        Task<List<TaskItem>> GetTasksByContextAsync(string context);
        Task<List<TaskItem>> GetFocusTasksAsync();

        Task<List<TaskItem>> GetActiveTasksAsync(bool showHidden);

        Task BulkUpdateTasksAsync(BulkUpdateModel updateModel);
        Task DeleteTasksAsync(List<int> taskIds);
        Task<string> ExportTasksToJsonAsync();
        Task ImportTasksFromJsonAsync(string jsonData);
        Task UpdateTaskExpandStateAsync(int taskId, bool isExpanded);
        Task DeleteAllCompletedTasksAsync();
        Task DeleteContextAsync(string context);
    }
}