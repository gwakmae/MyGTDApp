// 파일명: Components/Pages/Home.Selection.cs  (NEW)
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MyGtdApp.Components.Pages;

public partial class Home
{
    private void HandleTaskClick(int taskId, MouseEventArgs e)
    {
        // [제거] e.Detail == 2 분기는 TaskCard의 @ondblclick과 중복되므로 제거합니다.
        // if (e.Detail == 2)
        // {
        //     ShowEditModal(taskId);
        //     return;
        // }

        if (isMultiSelectMode || e.CtrlKey || e.ShiftKey)
        {
            if (e.ShiftKey && lastClickedTaskId.HasValue)
            {
                var lastIndex = renderedTasks.FindIndex(t => t.Id == lastClickedTaskId.Value);
                var currentIndex = renderedTasks.FindIndex(t => t.Id == taskId);
                if (lastIndex != -1 && currentIndex != -1)
                {
                    var start = Math.Min(lastIndex, currentIndex);
                    var end = Math.Max(lastIndex, currentIndex);
                    var rangeIds = renderedTasks.Skip(start).Take(end - start + 1).Select(t => t.Id);

                    if (!e.CtrlKey) selectedTaskIds.Clear();
                    foreach (var id in rangeIds)
                        if (!selectedTaskIds.Contains(id))
                            selectedTaskIds.Add(id);
                }
            }
            else
            {
                if (selectedTaskIds.Contains(taskId))
                    selectedTaskIds.Remove(taskId);
                else
                    selectedTaskIds.Add(taskId);
            }
        }
        else
        {
            // ▼ [핵심 수정] 단일 클릭 시, 기존 선택을 지우고 현재 클릭한 항목을 선택합니다.
            selectedTaskIds.Clear();
            selectedTaskIds.Add(taskId);
        }

        lastClickedTaskId = taskId;
        isBulkEditPanelVisible = false;
        StateHasChanged();
    }

    private async Task<bool> IsMobileDevice()
    {
        try
        {
            return await JSRuntime.InvokeAsync<bool>("isMobileDevice");
        }
        catch
        {
            return false;
        }
    }

    [JSInvokable]
    public void HandleEscapeKey()
    {
        if (selectedTaskIds.Any() || isBulkEditPanelVisible)
        {
            DeselectAll();
            Console.WriteLine("[SELECTION] ESC -> cleared");
        }
    }

    [JSInvokable]
    public void HandleBackgroundClick()
    {
        if (selectedTaskIds.Any() || isBulkEditPanelVisible)
        {
            DeselectAll();
            Console.WriteLine("[SELECTION] background -> cleared");
        }
    }

    [JSInvokable]
    public async Task EnterSelectionMode(int taskId)
    {
        isMultiSelectMode = true;
        if (!selectedTaskIds.Contains(taskId))
        {
            selectedTaskIds.Add(taskId);
        }
        lastClickedTaskId = taskId;
        Console.WriteLine($"[SELECTION] multi-touch mode: Task {taskId}");
        await InvokeAsync(StateHasChanged);
    }

    [JSInvokable]
    public void SelectAllTasks()
    {
        selectedTaskIds.Clear();
        selectedTaskIds.AddRange(renderedTasks.Select(t => t.Id));
        Console.WriteLine($"[SELECTION] SelectAll -> {selectedTaskIds.Count}");
        StateHasChanged();
    }

    private void DeselectAll()
    {
        selectedTaskIds.Clear();
        lastClickedTaskId = null;
        isBulkEditPanelVisible = false;
        isMultiSelectMode = false;
        StateHasChanged();
    }

    private void ClearAllSelections()
    {
        selectedTaskIds.Clear();
        isBulkEditPanelVisible = false;
        lastClickedTaskId = null;
        StateHasChanged();
    }
}
