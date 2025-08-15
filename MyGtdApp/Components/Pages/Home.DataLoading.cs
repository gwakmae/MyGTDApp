// 파일명: Components/Pages/Home.DataLoading.cs (UPDATED)
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
        focusTasks = await TaskService.GetFocusTasksAsync();

        if (IsFocusView)
        {
            pageTitle = "Focus";
        }
        else if (IsContextView)
        {
            pageTitle = $"Context: @{Context}";
            contextTasks = await TaskService.GetTasksByContextAsync($"@{Context}");
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

    /// <summary>
    /// UI상의 Shift 선택/범위 계산 시 "중복(Task Id 반복)"으로 인한
    /// 인덱스 혼란을 제거하기 위해 Today / Status 컬럼에 중복으로
    /// 표시되는 항목을 renderedTasks 평면 목록에서는 1회만 포함한다.
    /// (시각적 컬럼 중복은 Razor에서 그대로 유지)
    /// </summary>
    private void BuildRenderedTaskList()
    {
        renderedTasks.Clear();
        IEnumerable<TaskItem> roots;

        if (IsFocusView) roots = focusTasks;
        else if (IsContextView) roots = contextTasks;
        else
        {
            // Board View: Today + 각 Status 컬럼 순서대로
            // (표시 순서를 반영하기 위해 리스트 구성)
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
                    // 중복 감지 → selection 인덱스 혼동 방지 위해 skip
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
