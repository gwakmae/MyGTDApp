// 파일명: Components/Pages/Home.ModalInterop.cs  (NEW)
using Microsoft.JSInterop;
using MyGtdApp.Models;
using System;
using System.Linq;

namespace MyGtdApp.Components.Pages;

public partial class Home
{
    [JSInvokable]
    public void ShowEditModal(int taskId)
    {
        Console.WriteLine($"[MODAL] open request: {taskId}");

        taskToEdit =
            FindTaskById(allTopLevelTasks, taskId) ??
            FindTaskById(contextTasks, taskId) ??
            FindTaskById(focusTasks, taskId);

        if (taskToEdit != null)
        {
            Console.WriteLine($"[MODAL] found: {taskToEdit.Title}");
            StateHasChanged();
        }
        else
        {
            Console.WriteLine($"[MODAL] not found: {taskId}");
        }
    }
}
