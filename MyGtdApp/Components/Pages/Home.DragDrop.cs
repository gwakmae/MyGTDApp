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
        // 🔄 수정: 드래그 시작 시, 선택된 항목 중 하나가 아니면 선택 목록을 초기화하고 현재 항목만 선택
        if (!selectedTaskIds.Contains(id))
        {
            selectedTaskIds.Clear();
            selectedTaskIds.Add(id);
            lastClickedTaskId = id;
            StateHasChanged(); // UI에 선택 상태 반영
        }
        draggedTaskId = id; // 단일/다중 구분 없이 드래그 주체는 필요
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
        foreach(var child in parent.Children)
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