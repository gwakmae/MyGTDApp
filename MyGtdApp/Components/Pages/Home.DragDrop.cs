using MyGtdApp.Models;
using MyGtdApp.Components.Shared;
using Microsoft.JSInterop;
using System.Collections.Generic;
using System.Linq;
using TaskStatus = MyGtdApp.Models.TaskStatus;

namespace MyGtdApp.Components.Pages;

public partial class Home
{
    private List<TaskItem> GetTasksForStatus(TaskStatus status) =>
        allTopLevelTasks.Where(t => t.Status == status)
                        .OrderBy(t => t.SortOrder)
                        .ToList();

    // 🚀 [핵심 수정] 단일/다중 드래그를 모두 올바르게 처리하도록 로직 재구성
    private void HandleDragStart(int id)
    {
        draggedTaskId = id;

        // 드래그 시작한 항목이 현재 선택 목록에 없으면,
        // 이는 새로운 단일 항목 드래그로 간주한다.
        if (!selectedTaskIds.Contains(id))
        {
            // 기존 선택을 모두 지우고, 선택 모드를 해제한다.
            selectedTaskIds.Clear();
            isMultiSelectMode = false;
            StateHasChanged();
        }
        // 드래그 시작한 항목이 선택 목록에 있으면,
        // 다중 항목 이동으로 간주하고 선택 상태를 유지한다.
    }

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

        // 🚀 [핵심 수정] 이동할 ID 목록을 명확하게 결정
        var idsToMove = selectedTaskIds.Contains(draggedTaskId)
            ? new List<int>(selectedTaskIds)
            : new List<int> { draggedTaskId };

        await TaskService.MoveTasksAsync(idsToMove, targetStatus, null, siblings.Count);

        draggedTaskId = 0;
        await RefreshDataBasedOnRoute();
    }

    private async Task HandleDropOnProject(int targetTaskId, ProjectTaskNode.DropIndicator position)
    {
        if (draggedTaskId == 0) return;

        // 🚀 [핵심 수정] 이동할 ID 목록을 명확하게 결정
        var idsToMove = selectedTaskIds.Contains(draggedTaskId)
            ? new List<int>(selectedTaskIds)
            : new List<int> { draggedTaskId };

        // 자기 자신 또는 자신의 자손에게 드롭하는 것을 방지 (다중 선택 포함)
        if (idsToMove.Contains(targetTaskId)) return;

        var targetTask = FindTaskById(allTopLevelTasks, targetTaskId) ??
                         FindTaskById(contextTasks, targetTaskId);
        if (targetTask == null) return;

        var allDescendantsOfSelected = new List<int>();
        foreach (var id in idsToMove)
        {
            var task = FindTaskById(allTopLevelTasks, id) ?? FindTaskById(contextTasks, id);
            if (task != null)
            {
                allDescendantsOfSelected.AddRange(GetAllDescendantIds(task));
            }
        }
        if (allDescendantsOfSelected.Contains(targetTaskId)) return;

        var (parentId, sortOrder) = position switch
        {
            ProjectTaskNode.DropIndicator.Inside => (targetTask.Id, targetTask.Children.Count),
            ProjectTaskNode.DropIndicator.Above => (targetTask.ParentId, targetTask.SortOrder),
            ProjectTaskNode.DropIndicator.Below => (targetTask.ParentId, targetTask.SortOrder + 1),
            _ => (null, 0)
        };

        await TaskService.MoveTasksAsync(idsToMove, targetTask.Status, parentId, sortOrder);

        draggedTaskId = 0;
        await RefreshDataBasedOnRoute();
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

    private List<int> GetAllDescendantIds(TaskItem parent)
    {
        var ids = new List<int>();
        foreach (var child in parent.Children)
        {
            ids.Add(child.Id);
            ids.AddRange(GetAllDescendantIds(child));
        }
        return ids;
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