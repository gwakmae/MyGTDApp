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

    // 🚀 [핵심 수정] 아래 HandleDragStart 메서드를 새로운 코드로 교체합니다.
    private void HandleDragStart(int id)
    {
        // 항상 물리적으로 드래그되는 아이템의 ID를 설정합니다.
        draggedTaskId = id;

        // 만약 현재 드래그를 시작한 항목이 기존 선택 목록에 포함되어 있지 않다면,
        // 이는 새로운 단일 드래그로 간주합니다.
        if (!selectedTaskIds.Contains(id))
        {
            // 기존 선택을 모두 지우고, 현재 항목만 새로 선택합니다.
            selectedTaskIds.Clear();
            selectedTaskIds.Add(id);
            lastClickedTaskId = id;
            isMultiSelectMode = false; // 혹시 모르니 플래그도 초기화
            StateHasChanged();
        }
        // 만약 드래그 시작 항목이 기존 선택 목록에 이미 포함되어 있다면,
        // 아무것도 하지 않습니다. 드롭 시점에 selectedTaskIds 전체가 이동될 것입니다.
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

        if (selectedTaskIds.Any())
        {
            await TaskService.MoveTasksAsync(selectedTaskIds, targetStatus, null, siblings.Count);
        }
        else
        {
            await TaskService.MoveTaskAsync(draggedTaskId, targetStatus, null, siblings.Count);
        }

        draggedTaskId = 0;

        // ✅ [추가] UI 동기화를 위해 데이터 다시 로드
        await RefreshDataBasedOnRoute();
    }

    private async Task HandleDropOnProject(int targetTaskId, ProjectTaskNode.DropIndicator position)
    {
        if (draggedTaskId == 0) return;

        if (selectedTaskIds.Contains(targetTaskId)) return;

        var targetTask = FindTaskById(allTopLevelTasks, targetTaskId) ??
                         FindTaskById(contextTasks, targetTaskId);
        if (targetTask == null) return;

        if (selectedTaskIds.Any())
        {
            var allDescendantsOfSelected = new List<int>();
            foreach (var id in selectedTaskIds)
            {
                var task = FindTaskById(allTopLevelTasks, id) ?? FindTaskById(contextTasks, id);
                if (task != null)
                {
                    allDescendantsOfSelected.AddRange(GetAllDescendantIds(task));
                }
            }
            if (allDescendantsOfSelected.Contains(targetTaskId)) return;
        }

        var (parentId, sortOrder) = position switch
        {
            ProjectTaskNode.DropIndicator.Inside => (targetTask.Id, targetTask.Children.Count),
            ProjectTaskNode.DropIndicator.Above => (targetTask.ParentId, targetTask.SortOrder),
            ProjectTaskNode.DropIndicator.Below => (targetTask.ParentId, targetTask.SortOrder + 1),
            _ => (null, 0)
        };

        if (selectedTaskIds.Any())
        {
            await TaskService.MoveTasksAsync(selectedTaskIds, targetTask.Status, parentId, sortOrder);
        }
        else
        {
            if (draggedTaskId == targetTaskId) return;
            await TaskService.MoveTaskAsync(draggedTaskId, targetTask.Status, parentId, sortOrder);
        }

        draggedTaskId = 0;

        // ✅ [추가] UI 동기화를 위해 데이터 다시 로드
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