// 파일명: Components/Pages/Home.DataLoading.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyGtdApp.Models;
using TaskStatus = MyGtdApp.Models.TaskStatus;

namespace MyGtdApp.Components.Pages;

public partial class Home
{
    private async Task RefreshDataBasedOnRoute()
    {
        // ✨ 수정: activeTasks와 focusTasks를 항상 최신 상태로 먼저 불러옵니다.
        // 이렇게 하면 어떤 뷰에 있든지 헤더의 숫자 집계가 정확해집니다.
        focusTasks = await TaskService.GetFocusTasksAsync();
        activeTasks = await TaskService.GetActiveTasksAsync(showHidden);

        if (IsFocusView)
        {
            pageTitle = "Focus";
        }
        else if (IsContextView)
        {
            pageTitle = $"Context: @{Context}";
            contextTasks = await TaskService.GetTasksByContextAsync($"@{Context}");
        }
        else if (IsActiveTasksView)
        {
            pageTitle = "Active Tasks";
            // activeTasks는 위에서 이미 로드되었으므로 별도 작업이 필요 없습니다.
        }
        else
        {
            pageTitle = "GTD Board";
            allTopLevelTasks = await TaskService.GetAllTasksAsync();
            todayTasks = await TaskService.GetTodayTasksAsync();
        }

        BuildRenderedTaskList();
        StateHasChanged();
    }

    private void BuildRenderedTaskList()
    {
        renderedTasks.Clear();
        IEnumerable<TaskItem> roots;

        if (IsFocusView) roots = focusTasks;
        else if (IsContextView) roots = contextTasks;
        else if (IsActiveTasksView) roots = activeTasks;
        else
        {
            var ordered = new List<TaskItem>();
            ordered.AddRange(todayTasks.OrderBy(t => t.SortOrder));
            foreach (var status in (TaskStatus[])Enum.GetValues(typeof(TaskStatus)))
            {
                ordered.AddRange(GetTasksForStatus(status));
            }
            roots = ordered;
        }

        var seen = new HashSet<int>();

        void Flatten(IEnumerable<TaskItem> src)
        {
            foreach (var t in src)
            {
                if (!seen.Add(t.Id))
                {
                    continue;
                }

                renderedTasks.Add(t);

                if (t.IsExpanded && t.Children.Any())
                {
                    Flatten(t.Children.OrderBy(c => c.SortOrder));
                }
            }
        }

        Flatten(roots);
    }
}