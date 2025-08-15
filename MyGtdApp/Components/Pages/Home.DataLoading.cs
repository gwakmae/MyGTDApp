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
        // ✨ 수정: ActiveTasksAsync 호출을 다른 뷰와 함께 로드하도록 변경
        focusTasks = await TaskService.GetFocusTasksAsync();
        activeTasks = await TaskService.GetActiveTasksAsync(); // 데이터 미리 로드

        if (IsFocusView)
        {
            pageTitle = "Focus";
        }
        else if (IsContextView)
        {
            pageTitle = $"Context: @{Context}";
            contextTasks = await TaskService.GetTasksByContextAsync($"@{Context}");
        }
        else if (IsActiveTasksView) // ✨ 추가: Active Tasks 뷰 처리
        {
            pageTitle = "Active Tasks";
            // activeTasks는 위에서 이미 로드됨
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

        // ✨ 수정: IsActiveTasksView 조건 추가
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