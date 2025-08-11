using MyGtdApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MyGtdApp.Services
{
    /// <summary>
    /// 메모리 내 작업 목록에 대한 변경(쓰기) 관련 로직을 캡슐화합니다.
    /// </summary>
    internal class InMemoryTaskManipulationHelper
    {
        private readonly List<TaskItem> _tasks;

        public InMemoryTaskManipulationHelper(List<TaskItem> tasks)
        {
            _tasks = tasks;
        }

        public void AddTask(TaskItem newTask)
        {
            var maxSortOrder = _tasks.Where(t => t.ParentId == newTask.ParentId && t.Status == newTask.Status)
                                     .Select(t => (int?)t.SortOrder).Max() ?? -1;
            newTask.SortOrder = maxSortOrder + 1;
            _tasks.Add(newTask);
        }

        public void UpdateTask(TaskItem taskToUpdate)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == taskToUpdate.Id);
            if (task == null) return;

            task.Title = taskToUpdate.Title;
            task.Description = taskToUpdate.Description;
            task.Priority = taskToUpdate.Priority;
            task.StartDate = taskToUpdate.StartDate;
            task.DueDate = taskToUpdate.DueDate;
            task.Contexts = taskToUpdate.Contexts;
            task.IsHidden = taskToUpdate.IsHidden;
        }

        public void ToggleCompleteStatus(int taskId)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == taskId);
            if (task == null) return;

            task.IsCompleted = !task.IsCompleted;
            if (task.IsCompleted)
            {
                task.OriginalStatus = task.Status;
                task.Status = Models.TaskStatus.Completed;
            }
            else
            {
                task.Status = task.OriginalStatus ?? Models.TaskStatus.NextActions;
                task.OriginalStatus = null;
            }
        }

        public void DeleteTaskRecursive(int taskId)
        {
            var idsToDelete = GetDescendantIds(taskId);
            idsToDelete.Add(taskId);
            _tasks.RemoveAll(t => idsToDelete.Contains(t.Id));
        }

        public void DeleteTasksRecursive(List<int> taskIds)
        {
            var allIdsToDelete = new HashSet<int>();
            foreach (var id in taskIds)
            {
                allIdsToDelete.UnionWith(GetDescendantIds(id));
                allIdsToDelete.Add(id);
            }
            _tasks.RemoveAll(t => allIdsToDelete.Contains(t.Id));
        }

        public void MoveTasks(List<int> taskIds, Models.TaskStatus newStatus, int? newParentId, int newSortOrder)
        {
            if (taskIds == null || !taskIds.Any()) return;
            var tasksToMove = _tasks.Where(t => taskIds.Contains(t.Id)).ToList();
            if (!tasksToMove.Any()) return;

            var tasksByOldParent = tasksToMove.GroupBy(t => new { t.ParentId, t.Status });
            foreach (var group in tasksByOldParent)
            {
                var remaining = _tasks.Where(t => t.ParentId == group.Key.ParentId && t.Status == group.Key.Status && !taskIds.Contains(t.Id))
                                      .OrderBy(t => t.SortOrder).ToList();
                for (int i = 0; i < remaining.Count; i++) remaining[i].SortOrder = i;
            }

            var newSiblings = _tasks.Where(t => t.ParentId == newParentId && t.Status == newStatus && !taskIds.Contains(t.Id))
                                    .OrderBy(t => t.SortOrder).ToList();

            foreach (var task in tasksToMove)
            {
                task.ParentId = newParentId;
                task.Status = newStatus;
            }

            newSortOrder = Math.Clamp(newSortOrder, 0, newSiblings.Count);
            newSiblings.InsertRange(newSortOrder, tasksToMove.OrderBy(t => t.SortOrder));
            for (int i = 0; i < newSiblings.Count; i++) newSiblings[i].SortOrder = i;
        }

        public void BulkUpdateTasks(BulkUpdateModel model)
        {
            var tasksToUpdate = _tasks.Where(t => model.TaskIds.Contains(t.Id)).ToList();
            foreach (var task in tasksToUpdate)
            {
                if (model.DueDate.HasValue) task.DueDate = model.DueDate;
                if (model.Priority.HasValue) task.Priority = model.Priority.Value;

                if (!string.IsNullOrWhiteSpace(model.ContextToAdd))
                {
                    var context = model.ContextToAdd.StartsWith("@") ? model.ContextToAdd : $"@{model.ContextToAdd}";
                    if (!task.Contexts.Contains(context)) task.Contexts.Add(context);
                }
                if (!string.IsNullOrWhiteSpace(model.ContextToRemove))
                {
                    var context = model.ContextToRemove.StartsWith("@") ? model.ContextToRemove : $"@{model.ContextToRemove}";
                    task.Contexts.Remove(context);
                }
            }
        }

        public void UpdateTaskExpandState(int taskId, bool isExpanded)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == taskId);
            if (task != null) task.IsExpanded = isExpanded;
        }

        public void DeleteAllCompletedTasks()
            => _tasks.RemoveAll(t => t.Status == Models.TaskStatus.Completed);

        public void DeleteContext(string context)
            => _tasks.ForEach(t => t.Contexts.Remove(context));

        private HashSet<int> GetDescendantIds(int parentId)
        {
            var descendants = new HashSet<int>();
            var queue = new Queue<int>(new[] { parentId });
            while (queue.Count > 0)
            {
                var currentId = queue.Dequeue();
                var children = _tasks.Where(t => t.ParentId == currentId).Select(t => t.Id);
                foreach (var childId in children)
                {
                    if (descendants.Add(childId))
                    {
                        queue.Enqueue(childId);
                    }
                }
            }
            return descendants;
        }
    }
}