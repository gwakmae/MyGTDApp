using MyGtdApp.Models;
using System; // Action을 사용하기 위해 추가
using System.Collections.Generic; // List<T>를 사용하기 위해 추가
using System.Threading.Tasks; // Task를 사용하기 위해 추가

namespace MyGtdApp.Services
{
    public interface ITaskService
    {
        // 데이터 변경을 알리는 이벤트
        event Action OnChange;

        Task<List<TaskItem>> GetAllTasksAsync();
        Task MoveTaskAsync(int taskId, Models.TaskStatus newStatus, int? newParentId, int newSortOrder);
        Task<TaskItem> AddTaskAsync(string title, Models.TaskStatus status, int? parentId);
        Task DeleteTaskAsync(int taskId);
        Task UpdateTaskAsync(TaskItem taskToUpdate);
        Task ToggleCompleteStatusAsync(int taskId);
        Task<List<TaskItem>> GetTodayTasksAsync();
        Task<List<string>> GetAllContextsAsync();
        Task<List<TaskItem>> GetTasksByContextAsync(string context);

        // 🆕 데이터 백업/복원 메서드 추가
        Task<string> ExportTasksToJsonAsync();
        Task ImportTasksFromJsonAsync(string jsonData);

        // 🆕 트리 노드 확장/축소 상태 업데이트 추가
        Task UpdateTaskExpandStateAsync(int taskId, bool isExpanded);
    }
}