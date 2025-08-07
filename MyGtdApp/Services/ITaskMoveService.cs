using MyGtdApp.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskStatus = MyGtdApp.Models.TaskStatus;

namespace MyGtdApp.Services;

public interface ITaskMoveService
{
    Task MoveTaskAsync(int taskId, TaskStatus newStatus, int? newParentId, int newSortOrder);
    
    // 🆕 추가: 다중 작업 이동을 위한 인터페이스
    Task MoveTasksAsync(List<int> taskIds, TaskStatus newStatus, int? newParentId, int newSortOrder);
}