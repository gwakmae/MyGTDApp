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

    Task<List<TaskItem>> GetFocusTasksAsync();
    Task BulkUpdateTasksAsync(BulkUpdateModel model);
    Task DeleteTasksAsync(List<int> taskIds);

    // 🔧 추가: 대량 업데이트 (N+1 제거용)
    Task UpdateRangeAsync(IEnumerable<TaskItem> tasks);
}
