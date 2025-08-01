using MyGtdApp.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskStatus = MyGtdApp.Models.TaskStatus; // 모호성 해결

namespace MyGtdApp.Services
{
    public interface ITaskService
    {
        event Action OnChange;

        Task<List<TaskItem>> GetAllTasksAsync();
        Task MoveTaskAsync(int taskId, TaskStatus newStatus, int? newParentId, int newSortOrder);
        Task<TaskItem> AddTaskAsync(string title, TaskStatus status, int? parentId);
        Task DeleteTaskAsync(int taskId);
        Task UpdateTaskAsync(TaskItem taskToUpdate);
        Task ToggleCompleteStatusAsync(int taskId);
        Task<List<TaskItem>> GetTodayTasksAsync();
        Task<List<string>> GetAllContextsAsync();
        Task<List<TaskItem>> GetTasksByContextAsync(string context);
        Task<string> ExportTasksToJsonAsync();
        Task ImportTasksFromJsonAsync(string jsonData);
        Task UpdateTaskExpandStateAsync(int taskId, bool isExpanded);

        // 🆕 추가: 완료된 항목 모두 삭제
        Task DeleteAllCompletedTasksAsync();
    }
}
