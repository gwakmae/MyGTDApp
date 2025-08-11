using MyGtdApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MyGtdApp.Services
{
    /// <summary>
    /// 메모리 내 작업 목록에 대한 조회(읽기) 관련 로직을 캡슐화합니다.
    /// 이 클래스는 상태를 변경하지 않습니다.
    /// </summary>
    internal class InMemoryTaskQueryHelper
    {
        private readonly List<TaskItem> _tasks;

        public InMemoryTaskQueryHelper(List<TaskItem> tasks)
        {
            _tasks = tasks;
        }

        /// <summary>
        /// 모든 작업을 계층적 트리 구조로 반환합니다.
        /// </summary>
        public List<TaskItem> GetAllTasksAsTree()
        {
            var taskMap = _tasks.ToDictionary(t => t.Id);
            var topLevelTasks = new List<TaskItem>();
            _tasks.ForEach(t => t.Children.Clear());

            foreach (var task in _tasks)
            {
                if (task.ParentId.HasValue && taskMap.TryGetValue(task.ParentId.Value, out var parent))
                {
                    parent.Children.Add(task);
                }
                else
                {
                    topLevelTasks.Add(task);
                }
            }

            void SortChildrenRecursive(TaskItem parentTask)
            {
                parentTask.Children = parentTask.Children.OrderBy(c => c.SortOrder).ToList();
                parentTask.Children.ForEach(SortChildrenRecursive);
            }

            var sortedTopLevel = topLevelTasks.OrderBy(t => t.SortOrder).ToList();
            sortedTopLevel.ForEach(SortChildrenRecursive);

            return sortedTopLevel;
        }

        /// <summary>
        /// 오늘 할 일 목록을 조회합니다.
        /// </summary>
        public List<TaskItem> GetTodayTasks()
        {
            var today = DateTime.Today;
            return _tasks.Where(t =>
                !t.IsCompleted && t.StartDate.HasValue && t.StartDate.Value.Date <= today)
                .OrderBy(t => t.DueDate ?? DateTime.MaxValue)
                .ThenByDescending(t => t.Priority)
                .ToList();
        }

        /// <summary>
        /// 모든 컨텍스트의 고유 목록을 반환합니다.
        /// </summary>
        public List<string> GetAllContexts()
        {
            return _tasks.SelectMany(t => t.Contexts).Distinct().OrderBy(c => c).ToList();
        }

        /// <summary>
        /// 특정 컨텍스트에 속한 작업 목록을 반환합니다.
        /// </summary>
        public List<TaskItem> GetTasksByContext(string context)
        {
            return _tasks.Where(t => !t.IsCompleted && t.Contexts.Contains(context, StringComparer.OrdinalIgnoreCase))
                         .OrderBy(t => t.Status)
                         .ThenBy(t => t.SortOrder)
                         .ToList();
        }

        /// <summary>
        /// 집중(Focus) 보기의 작업 목록을 반환합니다.
        /// </summary>
        public List<TaskItem> GetFocusTasks()
        {
            var today = DateTime.Today;
            return _tasks.Where(t => !t.IsCompleted &&
                                     (t.Priority == Priority.High || (t.DueDate.HasValue && t.DueDate.Value.Date <= today.AddDays(3))))
                         .OrderBy(t => t.DueDate ?? DateTime.MaxValue)
                         .ThenByDescending(t => t.Priority)
                         .ToList();
        }
    }
}