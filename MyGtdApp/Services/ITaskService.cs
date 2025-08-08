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

        // 🆕 추가: 다중 작업 이동을 위한 인터페이스
        Task MoveTasksAsync(List<int> taskIds, TaskStatus newStatus, int? newParentId, int newSortOrder);

        Task<TaskItem> AddTaskAsync(string title, TaskStatus status, int? parentId);
        Task DeleteTaskAsync(int taskId);
        Task UpdateTaskAsync(TaskItem taskToUpdate);
        Task ToggleCompleteStatusAsync(int taskId);
        Task<List<TaskItem>> GetTodayTasksAsync();
        Task<List<string>> GetAllContextsAsync();
        Task<List<TaskItem>> GetTasksByContextAsync(string context);

        // 🆕 추가
        Task<List<TaskItem>> GetFocusTasksAsync();

        // 🆕 추가
        Task BulkUpdateTasksAsync(BulkUpdateModel updateModel);

        Task DeleteTasksAsync(List<int> taskIds);
        
        Task<string> ExportTasksToJsonAsync();
        Task ImportTasksFromJsonAsync(string jsonData);
        Task UpdateTaskExpandStateAsync(int taskId, bool isExpanded);
        Task DeleteAllCompletedTasksAsync();
        Task DeleteContextAsync(string context);
    }
}