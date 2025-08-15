using MyGtdApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MyGtdApp.Services
{
    internal class InMemoryTaskQueryHelper
    {
        private readonly List<TaskItem> _tasks;

        public InMemoryTaskQueryHelper(List<TaskItem> tasks)
        {
            _tasks = tasks;
        }

        public List<TaskItem> GetActiveTasks()
        {
            var today = DateTime.Today;
            // ✨ 수정: .Value 앞에 null-forgiving 연산자(!) 추가
            var parentIds = new HashSet<int>(_tasks.Where(t => t.ParentId.HasValue).Select(t => t.ParentId!.Value));

            return _tasks.Where(t =>
                t.Status != Models.TaskStatus.Inbox &&
                !parentIds.Contains(t.Id) &&
                !t.IsCompleted &&
                (!t.StartDate.HasValue || t.StartDate.Value.Date <= today))
                .OrderBy(t => t.Status)
                .ThenBy(t => t.SortOrder)
                .ToList();
        }

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

        public List<TaskItem> GetTodayTasks()
        {
            var today = DateTime.Today;
            return _tasks.Where(t =>
                !t.IsCompleted && t.StartDate.HasValue && t.StartDate.Value.Date <= today)
                .OrderBy(t => t.DueDate ?? DateTime.MaxValue)
                .ThenByDescending(t => t.Priority)
                .ToList();
        }

        public List<string> GetAllContexts()
        {
            return _tasks.SelectMany(t => t.Contexts).Distinct().OrderBy(c => c).ToList();
        }

        public List<TaskItem> GetTasksByContext(string context)
        {
            return _tasks.Where(t => !t.IsCompleted && t.Contexts.Contains(context, StringComparer.OrdinalIgnoreCase))
                         .OrderBy(t => t.Status)
                         .ThenBy(t => t.SortOrder)
                         .ToList();
        }

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