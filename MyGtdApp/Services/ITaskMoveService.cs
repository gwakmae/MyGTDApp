using MyGtdApp.Models;
using TaskStatus = MyGtdApp.Models.TaskStatus; // 모호성 해결

namespace MyGtdApp.Services;

public interface ITaskMoveService
{
    Task MoveTaskAsync(int taskId, TaskStatus newStatus, int? newParentId, int newSortOrder);
}
