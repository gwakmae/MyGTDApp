using MyGtdApp.Models;
using MyGtdApp.Components.Shared;
using Microsoft.JSInterop;
using TaskStatus = MyGtdApp.Models.TaskStatus; // 모호성 해결

namespace MyGtdApp.Components.Pages;

public partial class Home
{
    private List<TaskItem> GetTasksForStatus(TaskStatus status) =>
        allTopLevelTasks.Where(t => t.Status == status)
                        .OrderBy(t => t.SortOrder)
                        .ToList();

    private void HandleDragStart(int id) => draggedTaskId = id;

    private async Task HandleDragEnd()
    {
        if (draggedTaskId == 0) return;
        draggedTaskId = 0;
        await InvokeAsync(StateHasChanged);
    }

    private string GetColumnDropClass(TaskStatus status) =>
        dragOverStatus == status ? "drag-over" : "";

    private async Task HandleDropOnColumn(TaskStatus targetStatus)
    {
        if (draggedTaskId == 0) return;

        var siblings = GetTasksForStatus(targetStatus);
        await TaskService.MoveTaskAsync(draggedTaskId, targetStatus, null, siblings.Count);
        draggedTaskId = 0;
    }

    private async Task HandleDropOnProject(int targetTaskId, ProjectTaskNode.DropIndicator position)
    {
        if (draggedTaskId == 0 || draggedTaskId == targetTaskId) return;

        var targetTask = FindTaskById(allTopLevelTasks, targetTaskId) ??
                         FindTaskById(contextTasks, targetTaskId);
        if (targetTask == null) return;

        var (parentId, sortOrder) = position switch
        {
            ProjectTaskNode.DropIndicator.Inside => (targetTask.Id, targetTask.Children.Count),
            ProjectTaskNode.DropIndicator.Above => (targetTask.ParentId, targetTask.SortOrder),
            ProjectTaskNode.DropIndicator.Below => (targetTask.ParentId, targetTask.SortOrder + 1),
            _ => (null, 0)
        };

        await TaskService.MoveTaskAsync(draggedTaskId, targetTask.Status, parentId, sortOrder);
        draggedTaskId = 0;
    }

    private TaskItem? FindTaskById(IEnumerable<TaskItem> list, int id)
    {
        foreach (var t in list)
        {
            if (t.Id == id) return t;
            var found = FindTaskById(t.Children, id);
            if (found != null) return found;
        }
        return null;
    }

    [JSInvokable]
    public async Task HandleDropOnProject(int targetTaskId, string position)
    {
        var pos = position switch
        {
            "Above" => ProjectTaskNode.DropIndicator.Above,
            "Below" => ProjectTaskNode.DropIndicator.Below,
            _ => ProjectTaskNode.DropIndicator.Inside
        };
        await HandleDropOnProject(targetTaskId, pos);
    }
}
