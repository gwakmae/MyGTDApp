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

    private void HandleDragStart(int id)
    {
        // 항상 물리적으로 드래그되는 아이템의 ID를 설정합니다.
        draggedTaskId = id;

        // 만약 공식적인 다중 선택 모드가 아니라면 (즉, 한 손가락 롱프레스 드래그)
        if (!isMultiSelectMode)
        {
            // 이전에 선택된 항목이 남아있을 수 있으므로 모두 초기화합니다.
            if (selectedTaskIds.Any())
            {
                selectedTaskIds.Clear();
                lastClickedTaskId = null;
                isBulkEditPanelVisible = false;
                StateHasChanged();
            }
            // 핵심: 한 손가락 드래그 시에는 selectedTaskIds에 아무것도 추가하지 않습니다.
        }
        else // 다중 선택 모드일 때
        {
            // 만약 선택된 그룹의 일부가 아닌 다른 항목을 드래그 시작했다면,
            // 기존 선택을 모두 해제하고 새로 드래그한 항목만 선택된 것으로 간주합니다.
            if (!selectedTaskIds.Contains(id))
            {
                selectedTaskIds.Clear();
                selectedTaskIds.Add(id);
                lastClickedTaskId = id;
                StateHasChanged();
            }
            // 선택된 그룹 내의 항목을 드래그했다면, 선택 상태를 그대로 유지합니다.
        }
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

        // 🔄 수정: 다중/단일 이동 분기 처리
        if (selectedTaskIds.Any())
        {
            await TaskService.MoveTasksAsync(selectedTaskIds, targetStatus, null, siblings.Count);
        }
        else
        {
            await TaskService.MoveTaskAsync(draggedTaskId, targetStatus, null, siblings.Count);
        }

        draggedTaskId = 0;
    }

    private async Task HandleDropOnProject(int targetTaskId, ProjectTaskNode.DropIndicator position)
    {
        if (draggedTaskId == 0) return;

        // 🔄 수정: 다중 선택 시 자기 자신이나 자손에게 드롭하는 것 방지
        if (selectedTaskIds.Contains(targetTaskId)) return;

        var targetTask = FindTaskById(allTopLevelTasks, targetTaskId) ??
                         FindTaskById(contextTasks, targetTaskId);
        if (targetTask == null) return;

        // 🆕 추가: 다중 선택 시 순환 참조 방지 강화
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

        // 🔄 수정: 다중/단일 이동 분기 처리
        if (selectedTaskIds.Any())
        {
            await TaskService.MoveTasksAsync(selectedTaskIds, targetTask.Status, parentId, sortOrder);
        }
        else
        {
            if (draggedTaskId == targetTaskId) return; // 단일 이동 시 자기 자신에게 드롭 방지
            await TaskService.MoveTaskAsync(draggedTaskId, targetTask.Status, parentId, sortOrder);
        }

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

    // 🆕 추가: 특정 작업의 모든 자손 ID를 가져오는 헬퍼 메서드
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
