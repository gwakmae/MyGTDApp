using MyGtdApp.Models;

namespace MyGtdApp.Components.Pages;

public partial class Home
{
    private async Task HandleTaskAdded() => await RefreshDataBasedOnRoute();
    private async Task HandleUpdateTask() => await RefreshDataBasedOnRoute();

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
        // 🔽 [FIX] Focus 뷰의 Task도 찾을 수 있도록 로직 보강
        taskToEdit = FindTaskById(allTopLevelTasks, id) ??
                     FindTaskById(contextTasks, id) ??
                     FindTaskById(focusTasks, id); // focusTasks에서도 찾기
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
