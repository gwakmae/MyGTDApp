using MyGtdApp.Models;
using MyGtdApp.Repositories;
using TaskStatus = MyGtdApp.Models.TaskStatus; // 모호성 해결

namespace MyGtdApp.Services;

public class DatabaseTaskService : ITaskService
{
    private readonly ITaskRepository _repository;
    private readonly ITaskMoveService _moveService;
    private readonly ITaskDataService _dataService;

    public event System.Action? OnChange;

    public DatabaseTaskService(
        ITaskRepository repository,
        ITaskMoveService moveService,
        ITaskDataService dataService)
    {
        _repository = repository;
        _moveService = moveService;
        _dataService = dataService;
    }

    private void NotifyStateChanged() => OnChange?.Invoke();

    public async Task<List<TaskItem>> GetAllTasksAsync()
        => await _repository.GetAllAsync();

    public async Task<TaskItem> AddTaskAsync(string title, TaskStatus status, int? parentId)
    {
        var newTask = new TaskItem
        {
            Title = title,
            Status = status,
            ParentId = parentId
        };

        var result = await _repository.AddAsync(newTask);
        NotifyStateChanged();
        return result;
    }

    public async Task DeleteTaskAsync(int taskId)
    {
        await _repository.DeleteAsync(taskId);
        NotifyStateChanged();
    }

    public async Task UpdateTaskAsync(TaskItem taskToUpdate)
    {
        await _repository.UpdateAsync(taskToUpdate);
        NotifyStateChanged();
    }

    public async Task MoveTaskAsync(int taskId, TaskStatus newStatus, int? newParentId, int newSortOrder)
    {
        await _moveService.MoveTaskAsync(taskId, newStatus, newParentId, newSortOrder);
        NotifyStateChanged();
    }

    // [수정됨] ToggleCompleteStatusAsync 메서드
    public async Task ToggleCompleteStatusAsync(int taskId)
    {
        var task = await _repository.GetByIdAsync(taskId);
        if (task is null) return;

        var completed = !task.IsCompleted;

        if (completed)
        {
            task.OriginalStatus = task.Status;
            task.Status = TaskStatus.Completed;
        }
        else
        {
            task.Status = task.OriginalStatus ?? TaskStatus.NextActions;
            task.OriginalStatus = null;
        }
        task.IsCompleted = completed;
        await _repository.UpdateAsync(task);

        // ★ 자식들도 동일 상태로 재귀 적용
        await SetChildrenCompletedRecursive(taskId, completed);

        NotifyStateChanged();
    }

    /* --- 아래 메서드 신규 추가 --- */
    private async Task SetChildrenCompletedRecursive(int parentId, bool completed)
    {
        var stack = new Stack<int>();
        stack.Push(parentId);

        while (stack.Count > 0)
        {
            var id = stack.Pop();
            var children = (await _repository.GetAllRawAsync())
                           .Where(c => c.ParentId == id);

            foreach (var c in children)
            {
                c.IsCompleted = completed;
                c.Status = completed ? TaskStatus.Completed
                                     : (c.OriginalStatus ?? TaskStatus.NextActions);
                if (!completed) c.OriginalStatus = null;

                await _repository.UpdateAsync(c);
                stack.Push(c.Id);
            }
        }
    }

    public async Task<List<TaskItem>> GetTodayTasksAsync()
        => await _repository.GetTodayTasksAsync();

    public async Task<List<string>> GetAllContextsAsync()
        => await _repository.GetAllContextsAsync();

    public async Task<List<TaskItem>> GetTasksByContextAsync(string context)
        => await _repository.GetByContextAsync(context);

    public async Task<string> ExportTasksToJsonAsync()
        => await _dataService.ExportTasksToJsonAsync();

    public async Task ImportTasksFromJsonAsync(string jsonData)
    {
        await _dataService.ImportTasksFromJsonAsync(jsonData);
        NotifyStateChanged();
    }

    public async Task UpdateTaskExpandStateAsync(int taskId, bool isExpanded)
    {
        await _repository.UpdateExpandStateAsync(taskId, isExpanded);
        // UI 성능을 위해 OnChange 이벤트는 발생시키지 않음
    }

    // 🆕 추가: 완료된 항목 모두 삭제
    public async Task DeleteAllCompletedTasksAsync()
    {
        await _repository.DeleteByStatusRecursiveAsync(TaskStatus.Completed);
        NotifyStateChanged();
    }
}
