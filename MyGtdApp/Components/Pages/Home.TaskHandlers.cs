using MyGtdApp.Models;

namespace MyGtdApp.Components.Pages;

public partial class Home
{
    private async Task HandleTaskAdded() => await RefreshTasks();
    private async Task HandleUpdateTask() => await RefreshTasks();

    private async Task HandleToggleComplete(int id)
    {
        await TaskService.ToggleCompleteStatusAsync(id);
    }

    private async Task HandleDeleteTask(int id)
    {
        await TaskService.DeleteTaskAsync(id);
    }

    private void ShowEditModal(int id)
    {
        taskToEdit = FindTaskById(allTopLevelTasks, id) ??
                     FindTaskById(contextTasks, id);
    }

    private async Task HandleSaveTask(TaskItem updated)
    {
        await TaskService.UpdateTaskAsync(updated);
        CloseEditModal();
    }

    private void CloseEditModal() => taskToEdit = null;

    // 🆕 추가: 완료된 항목들 모두 삭제
    private async Task HandleClearCompleted()
    {
        await TaskService.DeleteAllCompletedTasksAsync();
    }
}
