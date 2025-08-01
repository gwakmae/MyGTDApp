using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using TaskStatus = MyGtdApp.Models.TaskStatus; // 모호성 해결

namespace MyGtdApp.Components.Pages;

public partial class Home
{
    private async Task ShowQuickAdd(TaskStatus status)
    {
        addingTaskStatus = status;
        await Task.Delay(50);
        await quickAddInputRef.FocusAsync();
    }

    private void CancelQuickAdd()
    {
        addingTaskStatus = null;
        newTaskTitle = "";
    }

    private async Task HandleQuickAddKeyUp(KeyboardEventArgs e, TaskStatus status)
    {
        if (e.Key == "Enter" && !string.IsNullOrWhiteSpace(newTaskTitle))
        {
            await TaskService.AddTaskAsync(newTaskTitle, status, null);
            newTaskTitle = "";
            await quickAddInputRef.FocusAsync();
        }
        else if (e.Key == "Escape")
        {
            CancelQuickAdd();
        }
    }

    private async Task HandleQuickAddBlur(TaskStatus status)
    {
        if (!string.IsNullOrWhiteSpace(newTaskTitle))
        {
            await TaskService.AddTaskAsync(newTaskTitle, status, null);
        }

        addingTaskStatus = null;
        newTaskTitle = "";
    }
}
