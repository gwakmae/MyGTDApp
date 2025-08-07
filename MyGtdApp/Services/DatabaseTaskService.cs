using MyGtdApp.Models;
using MyGtdApp.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskStatus = MyGtdApp.Models.TaskStatus;

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

    public async Task<List<TaskItem>> GetAllTasksAsync() => await _repository.GetAllAsync();

    public async Task<TaskItem> AddTaskAsync(string title, TaskStatus status, int? parentId)
    {
        var newTask = new TaskItem { Title = title, Status = status, ParentId = parentId };
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
        var existingTask = await _repository.GetByIdAsync(taskToUpdate.Id);
        bool contextsChanged = existingTask != null && !existingTask.Contexts.SequenceEqual(taskToUpdate.Contexts);
        
        await _repository.UpdateAsync(taskToUpdate);

        if (contextsChanged) Console.WriteLine("컨텍스트 변경 감지 - OnChange 이벤트 발생");
        NotifyStateChanged();
    }

    public async Task MoveTaskAsync(int taskId, TaskStatus newStatus, int? newParentId, int newSortOrder)
    {
        await _moveService.MoveTaskAsync(taskId, newStatus, newParentId, newSortOrder);
        NotifyStateChanged();
    }
    
    // 🆕 추가: 다중 작업 이동 서비스 호출
    public async Task MoveTasksAsync(List<int> taskIds, TaskStatus newStatus, int? newParentId, int newSortOrder)
    {
        await _moveService.MoveTasksAsync(taskIds, newStatus, newParentId, newSortOrder);
        NotifyStateChanged();
    }

    public async Task ToggleCompleteStatusAsync(int taskId)
    {
        var task = await _repository.GetByIdAsync(taskId);
        if (task is null) return;

        var completed = !task.IsCompleted;
        task.IsCompleted = completed;
        task.Status = completed ? TaskStatus.Completed : (task.OriginalStatus ?? TaskStatus.NextActions);
        if (completed) task.OriginalStatus = task.Status == TaskStatus.Completed ? task.OriginalStatus : task.Status;
        else task.OriginalStatus = null;
        
        await _repository.UpdateAsync(task);
        await SetChildrenCompletedRecursive(taskId, completed);
        NotifyStateChanged();
    }
    
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

    public async Task<List<TaskItem>> GetTodayTasksAsync() => await _repository.GetTodayTasksAsync();
    public async Task<List<string>> GetAllContextsAsync() => await _repository.GetAllContextsAsync();
    public async Task<List<TaskItem>> GetTasksByContextAsync(string context) => await _repository.GetByContextAsync(context);
    public async Task<string> ExportTasksToJsonAsync() => await _dataService.ExportTasksToJsonAsync();

    public async Task ImportTasksFromJsonAsync(string jsonData)
    {
        await _dataService.ImportTasksFromJsonAsync(jsonData);
        NotifyStateChanged();
    }

    public async Task UpdateTaskExpandStateAsync(int taskId, bool isExpanded)
    {
        await _repository.UpdateExpandStateAsync(taskId, isExpanded);
    }

    public async Task DeleteAllCompletedTasksAsync()
    {
        await _repository.DeleteByStatusRecursiveAsync(TaskStatus.Completed);
        NotifyStateChanged();
    }

    public async Task DeleteContextAsync(string context)
    {
        var allTasks = await _repository.GetAllRawAsync();
        var tasksWithContext = allTasks.Where(t => t.Contexts.Contains(context)).ToList();
        foreach (var task in tasksWithContext)
        {
            task.Contexts.Remove(context);
            await _repository.UpdateAsync(task);
        }
        NotifyStateChanged();
    }
}