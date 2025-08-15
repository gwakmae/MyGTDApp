// 파일명: Components/Pages/Home.Metrics.cs  (NEW)
using System.Linq;
using MyGtdApp.Models;
using System.Collections.Generic;

namespace MyGtdApp.Components.Pages;

public partial class Home
{
    /// <summary>
    /// 완료되지 않은 '실행 가능한 리프 태스크' 개수
    /// </summary>
    private int GetActiveLeafTasksCount()
    {
        if (showHidden) // showHidden은 UIState partial
        {
            var all = new List<TaskItem>();
            CollectAllTasks(allTopLevelTasks, all);
            return all.Count(t => !t.IsCompleted && !t.Children.Any());
        }

        int CountVisibleLeafTasks(IEnumerable<TaskItem> tasks, bool parentHidden)
        {
            int count = 0;
            foreach (var t in tasks)
            {
                bool effectiveHidden = parentHidden || t.IsHidden;
                if (!effectiveHidden && !t.IsCompleted && !t.Children.Any())
                {
                    count++;
                }
                if (t.Children.Any())
                    count += CountVisibleLeafTasks(t.Children, effectiveHidden);
            }
            return count;
        }

        return CountVisibleLeafTasks(allTopLevelTasks, false);
    }

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
