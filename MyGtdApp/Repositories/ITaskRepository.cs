using MyGtdApp.Models;
using TaskStatus = MyGtdApp.Models.TaskStatus;

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
    Task DeleteByStatusRecursiveAsync(TaskStatus status);

    // 🆕 추가: Focus View 용 데이터 조회
    Task<List<TaskItem>> GetFocusTasksAsync();

    // 🆕 추가: 일괄 업데이트
    Task BulkUpdateTasksAsync(BulkUpdateModel model);
    Task DeleteTasksAsync(List<int> taskIds);
}