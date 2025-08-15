// 파일명: Components/Pages/Home.Metrics.cs
using System.Linq;
using MyGtdApp.Models;
using System.Collections.Generic;
using System;
using TaskStatus = MyGtdApp.Models.TaskStatus;

namespace MyGtdApp.Components.Pages;

public partial class Home
{
    private IEnumerable<TaskItem> GetTasksToShow()
    {
        if (IsFocusView) return focusTasks;
        if (IsContextView) return contextTasks;
        if (IsActiveTasksView) return activeTasks;

        return Enumerable.Empty<TaskItem>();
    }

    private int GetActiveLeafTasksCount()
    {
        // 'activeTasks' 리스트는 이미 서비스에서
        // 'showHidden' 상태까지 모두 고려하여 완벽하게 필터링된 결과물입니다.
        // 따라서 UI는 그저 그 결과물의 개수만 세면 됩니다.
        return activeTasks.Count;
    }

    // 이 메서드는 이제 Board View에서만 사용되므로 그대로 둡니다.
    private void CollectAllTasks(IEnumerable<TaskItem> src, List<TaskItem> result)
    {
        foreach (var t in src)
        {
            result.Add(t);
            if (t.Children.Any())
                CollectAllTasks(t.Children, result);
        }
    }
}