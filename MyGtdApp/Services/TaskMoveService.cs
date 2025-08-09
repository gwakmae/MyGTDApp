using Microsoft.EntityFrameworkCore;
using MyGtdApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskStatus = MyGtdApp.Models.TaskStatus;

namespace MyGtdApp.Services;

public class TaskMoveService : ITaskMoveService
{
    private readonly IDbContextFactory<GtdDbContext> _dbContextFactory;

    public TaskMoveService(IDbContextFactory<GtdDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task MoveTaskAsync(int taskId, TaskStatus newStatus, int? newParentId, int newSortOrder)
    {
        await MoveTasksAsync(new List<int> { taskId }, newStatus, newParentId, newSortOrder);
    }

    // 🚀 [로직 전면 재작성] '같은 목록 내 이동' 시나리오를 명확히 분리하여 처리
    public async Task MoveTasksAsync(List<int> taskIds, TaskStatus newStatus, int? newParentId, int newSortOrder)
    {
        if (taskIds == null || !taskIds.Any()) return;

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var allTasksInDb = await context.Tasks.AsNoTracking().ToListAsync();
            var allAffectedIds = GetAllAffectedIds(taskIds, allTasksInDb);

            if (newParentId.HasValue && allAffectedIds.Contains(newParentId.Value))
            {
                await transaction.RollbackAsync();
                return;
            }

            var firstTask = allTasksInDb.FirstOrDefault(t => t.Id == taskIds[0]);
            if (firstTask == null)
            {
                await transaction.RollbackAsync();
                return;
            }
            int? oldParentId = firstTask.ParentId;
            TaskStatus oldStatus = firstTask.Status;

            bool isMoveInSameList = oldParentId == newParentId && oldStatus == newStatus;

            // --- 1. 메모리에서 최종 순서 목록 생성 ---
            var finalOrderedList = new List<TaskItem>();
            var tasksToUpdateInDb = new List<TaskItem>();

            if (isMoveInSameList)
            {
                // [시나리오 1: 같은 목록 내에서 이동]
                var siblings = allTasksInDb
                    .Where(t => t.ParentId == oldParentId && t.Status == oldStatus)
                    .OrderBy(t => t.SortOrder)
                    .ToList();

                var tasksToMove = siblings.Where(t => taskIds.Contains(t.Id)).ToList();
                var remainingTasks = siblings.Except(tasksToMove).ToList();

                newSortOrder = Math.Clamp(newSortOrder, 0, remainingTasks.Count);
                remainingTasks.InsertRange(newSortOrder, tasksToMove);

                finalOrderedList = remainingTasks;
            }
            else
            {
                // [시나리오 2: 다른 목록으로 이동]
                var oldSiblings = allTasksInDb
                    .Where(t => t.ParentId == oldParentId && t.Status == oldStatus && !allAffectedIds.Contains(t.Id))
                    .OrderBy(t => t.SortOrder)
                    .ToList();

                var destinationSiblings = allTasksInDb
                    .Where(t => t.ParentId == newParentId && t.Status == newStatus && !allAffectedIds.Contains(t.Id))
                    .OrderBy(t => t.SortOrder)
                    .ToList();

                var topLevelTasksToMove = allTasksInDb.Where(t => taskIds.Contains(t.Id)).OrderBy(t => t.SortOrder).ToList();

                newSortOrder = Math.Clamp(newSortOrder, 0, destinationSiblings.Count);
                destinationSiblings.InsertRange(newSortOrder, topLevelTasksToMove);

                // 각 목록은 독립적으로 0부터 재정렬
                for (int i = 0; i < oldSiblings.Count; i++) oldSiblings[i].SortOrder = i;
                for (int i = 0; i < destinationSiblings.Count; i++) destinationSiblings[i].SortOrder = i;

                finalOrderedList.AddRange(oldSiblings);
                finalOrderedList.AddRange(destinationSiblings);
            }

            // --- 2. DB 엔티티를 로드하여 최종 상태 적용 ---
            var allIdsToUpdate = finalOrderedList.Select(t => t.Id).Union(allAffectedIds);
            tasksToUpdateInDb = await context.Tasks.Where(t => allIdsToUpdate.Contains(t.Id)).ToListAsync();
            var finalOrderMap = finalOrderedList.Select((task, index) => new { task.Id, NewSortOrder = index })
                                                .ToDictionary(x => x.Id, x => x.NewSortOrder);

            foreach (var task in tasksToUpdateInDb)
            {
                // 이동 그룹에 속한 항목들의 상태/부모 변경
                if (allAffectedIds.Contains(task.Id))
                {
                    task.Status = newStatus;
                    if (taskIds.Contains(task.Id)) // 최상위 이동 항목만 ParentId 변경
                    {
                        task.ParentId = newParentId;
                    }
                }

                // 최종 순서 적용
                if (finalOrderMap.TryGetValue(task.Id, out var newOrder))
                {
                    task.SortOrder = newOrder;
                }
            }

            await context.SaveChangesAsync();

            // --- 3. Path/Depth 업데이트 ---
            var movedTopLevelTasksInDb = tasksToUpdateInDb.Where(t => taskIds.Contains(t.Id)).ToList();
            foreach (var task in movedTopLevelTasksInDb)
            {
                await UpdatePathDepthRecursiveAsync(context, task);
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MOVE ERROR] {ex.Message}\n{ex.StackTrace}");
            await transaction.RollbackAsync();
            throw;
        }
    }

    private HashSet<int> GetAllAffectedIds(List<int> initialIds, List<TaskItem> allTasks)
    {
        var allAffected = new HashSet<int>(initialIds);
        var queue = new Queue<int>(initialIds);
        var taskLookup = allTasks.ToLookup(t => t.ParentId);

        while (queue.Count > 0)
        {
            var parentId = queue.Dequeue();
            if (taskLookup.Contains(parentId))
            {
                foreach (var child in taskLookup[parentId])
                {
                    if (allAffected.Add(child.Id))
                    {
                        queue.Enqueue(child.Id);
                    }
                }
            }
        }
        return allAffected;
    }

    private static async Task UpdatePathDepthRecursiveAsync(GtdDbContext ctx, TaskItem node)
    {
        string Pad(int n) => n.ToString("D6");

        if (node.ParentId == null)
        {
            node.Path = Pad(node.Id);
            node.Depth = 0;
        }
        else
        {
            var parent = await ctx.Tasks.AsNoTracking().SingleOrDefaultAsync(t => t.Id == node.ParentId.Value);
            if (parent == null) throw new InvalidOperationException($"Parent task with ID {node.ParentId} not found during Path update.");
            node.Path = $"{parent.Path}/{Pad(node.Id)}";
            node.Depth = parent.Depth + 1;
        }

        var children = await ctx.Tasks.Where(t => t.ParentId == node.Id).ToListAsync();
        foreach (var child in children)
        {
            await UpdatePathDepthRecursiveAsync(ctx, child);
        }
    }
}