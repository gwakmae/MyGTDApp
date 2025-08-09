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

    // 🚀 [로직 전면 재작성] '루트 노드'를 식별하여 계층 구조를 보존하는 최종 해결책
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

            var selectedTasks = allTasksInDb.Where(t => taskIds.Contains(t.Id)).ToList();
            if (!selectedTasks.Any())
            {
                await transaction.RollbackAsync();
                return;
            }

            var selectedIdsSet = new HashSet<int>(taskIds);
            var rootTasksToMove = selectedTasks
                .Where(t => t.ParentId == null || !selectedIdsSet.Contains(t.ParentId.Value))
                .OrderBy(t => t.SortOrder)
                .ToList();

            if (!rootTasksToMove.Any())
            {
                await transaction.RollbackAsync();
                return;
            }

            int? oldParentId = rootTasksToMove.First().ParentId;
            TaskStatus oldStatus = rootTasksToMove.First().Status;
            bool isMoveInSameList = oldParentId == newParentId && oldStatus == newStatus;

            var finalSortOrders = new Dictionary<int, int>();

            if (isMoveInSameList)
            {
                var siblings = allTasksInDb
                    .Where(t => t.ParentId == oldParentId && t.Status == oldStatus)
                    .OrderBy(t => t.SortOrder).ToList();
                var remaining = siblings.Where(t => !taskIds.Contains(t.Id)).ToList();

                newSortOrder = Math.Clamp(newSortOrder, 0, remaining.Count);
                remaining.InsertRange(newSortOrder, rootTasksToMove);

                finalSortOrders = remaining.Select((t, i) => new { t.Id, Index = i }).ToDictionary(x => x.Id, x => x.Index);
            }
            else
            {
                var oldSiblings = allTasksInDb
                    .Where(t => t.ParentId == oldParentId && t.Status == oldStatus && !allAffectedIds.Contains(t.Id))
                    .OrderBy(t => t.SortOrder).ToList();

                var destSiblings = allTasksInDb
                    .Where(t => t.ParentId == newParentId && t.Status == newStatus && !allAffectedIds.Contains(t.Id))
                    .OrderBy(t => t.SortOrder).ToList();

                newSortOrder = Math.Clamp(newSortOrder, 0, destSiblings.Count);
                destSiblings.InsertRange(newSortOrder, rootTasksToMove);

                for (int i = 0; i < oldSiblings.Count; i++) finalSortOrders[oldSiblings[i].Id] = i;
                for (int i = 0; i < destSiblings.Count; i++) finalSortOrders[destSiblings[i].Id] = i;
            }

            var allIdsToUpdate = allAffectedIds.Union(finalSortOrders.Keys);
            var tasksToUpdateInDb = await context.Tasks.Where(t => allIdsToUpdate.Contains(t.Id)).ToListAsync();
            var tasksMap = tasksToUpdateInDb.ToDictionary(t => t.Id);

            foreach (var affectedId in allAffectedIds)
            {
                if (tasksMap.TryGetValue(affectedId, out var task))
                {
                    task.Status = newStatus;
                    if (rootTasksToMove.Any(r => r.Id == task.Id))
                    {
                        task.ParentId = newParentId;
                    }
                }
            }

            foreach (var kvp in finalSortOrders)
            {
                if (tasksMap.TryGetValue(kvp.Key, out var task))
                {
                    task.SortOrder = kvp.Value;
                }
            }

            await context.SaveChangesAsync();

            var trackedRootTasks = tasksToUpdateInDb.Where(t => rootTasksToMove.Any(r => r.Id == t.Id)).ToList();
            foreach (var rootTask in trackedRootTasks)
            {
                await UpdatePathDepthRecursiveAsync(context, rootTask);
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