// 파일명: Components/Pages/Home.Metrics.cs (수정됨)
using System.Linq;
using MyGtdApp.Models;
using System.Collections.Generic;
using System; // DateTime 사용을 위해 추가
using TaskStatus = MyGtdApp.Models.TaskStatus;

namespace MyGtdApp.Components.Pages;

public partial class Home
{
    /// <summary>
    /// 새로운 규칙이 적용된 'Active Task' 개수를 계산합니다.
    /// </summary>
    private int GetActiveLeafTasksCount()
    {
        var today = DateTime.Today;

        // 작업이 'Active'로 간주될 조건을 검사하는 헬퍼 함수
        bool IsTaskConsideredActive(TaskItem t)
        {
            // 1. Inbox 상태는 제외
            if (t.Status == TaskStatus.Inbox) return false;

            // 2. 프로젝트(자식이 있는 작업)는 제외
            if (t.Children.Any()) return false;

            // 3. 이미 완료된 작업은 제외
            if (t.IsCompleted) return false;

            // 4. 시작 날짜가 미래로 지정된 작업은 제외
            if (t.StartDate.HasValue && t.StartDate.Value.Date > today) return false;

            // 모든 조건을 통과하면 'Active'로 간주
            return true;
        }

        // '숨김 보이기'가 켜져 있으면, 숨김 상태를 고려할 필요 없이 전체 목록을 필터링합니다.
        if (showHidden)
        {
            var allTasks = new List<TaskItem>();
            CollectAllTasks(allTopLevelTasks, allTasks);
            return allTasks.Count(IsTaskConsideredActive);
        }

        // '숨김 보이기'가 꺼져 있으면, 계층 구조를 탐색하며 보이는 작업만 계산합니다.
        int CountVisibleActiveTasks(IEnumerable<TaskItem> tasks, bool isParentHidden)
        {
            int count = 0;
            foreach (var task in tasks)
            {
                // 부모가 숨겨졌거나, 자기 자신이 숨겨진 경우 실질적으로 숨겨진 상태입니다.
                bool isEffectivelyHidden = isParentHidden || task.IsHidden;

                // 보이는 작업에 대해서만 'Active' 조건을 검사합니다.
                if (!isEffectivelyHidden && IsTaskConsideredActive(task))
                {
                    count++;
                }

                // 자식 노드가 있다면, 현재 작업의 '실질적 숨김' 상태를 전달하며 재귀 호출합니다.
                if (task.Children.Any())
                {
                    count += CountVisibleActiveTasks(task.Children, isEffectivelyHidden);
                }
            }
            return count;
        }

        // 최상위 작업부터 재귀 카운트를 시작합니다.
        return CountVisibleActiveTasks(allTopLevelTasks, false);
    }

    // 모든 하위 작업을 평탄화하는 헬퍼 메서드 (기존 코드 유지)
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