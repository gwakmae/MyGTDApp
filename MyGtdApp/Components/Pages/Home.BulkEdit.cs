// 파일명: Components/Pages/Home.BulkEdit.cs  (FIXED)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyGtdApp.Models;

namespace MyGtdApp.Components.Pages;

public partial class Home
{
    private void OpenBulkEditPanel() => isBulkEditPanelVisible = true;

    private async Task HandleBulkUpdate(BulkUpdateModel model)
    {
        if (model.TaskIds.Any())
            await TaskService.BulkUpdateTasksAsync(model);

        CloseBulkEditPanel();
    }

    private void CloseBulkEditPanel() => isBulkEditPanelVisible = false;

    private async Task HandleDeleteSelected()
    {
        if (!selectedTaskIds.Any()) return;

        var message = $"{selectedTaskIds.Count}개의 항목과 모든 하위 항목을 삭제하시겠습니까?";
        var confirmed = await JSRuntime.InvokeAsync<bool>("confirm", new object?[] { message });
        if (confirmed)
        {
            await TaskService.DeleteTasksAsync(new List<int>(selectedTaskIds));
            DeselectAll();
        }
    }
}
