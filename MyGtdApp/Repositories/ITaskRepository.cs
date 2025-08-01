using MyGtdApp.Models;
using TaskStatus = MyGtdApp.Models.TaskStatus; // 모호성 해결

namespace MyGtdApp.Repositories;

public interface ITaskRepository
{
    Task<List<TaskItem>> GetAllAsync();
    Task<TaskItem?> GetByIdAsync(int id);
    Task<TaskItem> AddAsync(TaskItem task);
    Task UpdateAsync(TaskItem task);
    Task DeleteAsync(int id);
    Task<List<TaskItem>> GetByStatusAsync(TaskStatus status);
    Task<List<TaskItem>> GetByContextAsync(string context);
    Task<List<TaskItem>> GetTodayTasksAsync();
    Task<List<string>> GetAllContextsAsync();
    Task UpdateExpandStateAsync(int taskId, bool isExpanded);
    Task<List<TaskItem>> GetAllRawAsync();
}
