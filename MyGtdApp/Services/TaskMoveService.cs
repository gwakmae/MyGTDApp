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

    // 🚀 [리팩토링된 메인 메서드] - 기존 100+줄을 여러 작은 메서드로 분해
    public async Task MoveTasksAsync(List<int> taskIds, TaskStatus newStatus, int? newParentId, int newSortOrder)
    {
        if (taskIds == null || !taskIds.Any()) return;

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var allTasks = await context.Tasks.AsNoTracking().ToListAsync();

            // 1. 유효성 검증
            if (!IsValidMove(taskIds, newParentId, allTasks))
            {
                await transaction.RollbackAsync();
                return;
            }

            // 2. 루트 작업 식별
            var rootTasks = GetRootTasksToMove(taskIds, allTasks);
            if (!rootTasks.Any())
            {
                await transaction.RollbackAsync();
                return;
            }

            // 3. 영향받는 모든 작업 ID 계산
            var affectedIds = GetAllAffectedIds(taskIds, allTasks);

            // 4. 정렬 순서 계산
            var sortOrders = CalculateSortOrders(rootTasks, affectedIds, newParentId, newStatus, newSortOrder, allTasks);

            // 5. 데이터베이스 업데이트
            await ApplyChangesToDatabase(context, affectedIds, sortOrders, newStatus, newParentId, rootTasks);

            // 6. Path/Depth 업데이트
            var trackedRootTasks = await context.Tasks
                .Where(t => rootTasks.Select(r => r.Id).Contains(t.Id))
                .ToListAsync();

            await UpdatePathDepthForMovedTasks(context, trackedRootTasks);

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

    // ===== 🔽 Private Helper 메서드들 (기존 로직을 분리) =====

    private bool IsValidMove(List<int> taskIds, int? newParentId, List<TaskItem> allTasks)
    {
        if (taskIds == null || !taskIds.Any()) return false;

        // 순환 참조 검증
        if (newParentId.HasValue)
        {
            var affectedIds = GetAllAffectedIds(taskIds, allTasks);
            if (affectedIds.Contains(newParentId.Value))
            {
                Console.WriteLine($"[VALIDATION] 순환 참조 감지: 부모 ID {newParentId}가 이동 대상에 포함됨");
                return false;
            }
        }

        return true;
    }

    private List<TaskItem> GetRootTasksToMove(List<int> taskIds, List<TaskItem> allTasks)
    {
        var selectedTasks = allTasks.Where(t => taskIds.Contains(t.Id)).ToList();
        if (!selectedTasks.Any()) return new List<TaskItem>();

        var selectedIdsSet = new HashSet<int>(taskIds);

        // 부모가 선택되지 않은 작업들만 루트로 간주
        var rootTasks = selectedTasks
            .Where(t => t.ParentId == null || !selectedIdsSet.Contains(t.ParentId.Value))
            .OrderBy(t => t.SortOrder)
            .ToList();

        return rootTasks;
    }

    private HashSet<int> GetAllAffectedIds(List<int> taskIds, List<TaskItem> allTasks)
    {
        var allAffected = new HashSet<int>(taskIds);
        var queue = new Queue<int>(taskIds);
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

    private Dictionary<int, int> CalculateSortOrders(
        List<TaskItem> rootTasksToMove,
        HashSet<int> allAffectedIds,
        int? newParentId,
        TaskStatus newStatus,
        int newSortOrder,
        List<TaskItem> allTasks)
    {
        var finalSortOrders = new Dictionary<int, int>();

        // 첫 번째 루트 작업의 원래 정보를 기준으로 판단
        var firstRoot = rootTasksToMove.First();
        int? oldParentId = firstRoot.ParentId;
        TaskStatus oldStatus = firstRoot.Status;
        bool isSameList = oldParentId == newParentId && oldStatus == newStatus;

        if (isSameList)
        {
            // 같은 리스트 내에서 재정렬
            var siblings = allTasks
                .Where(t => t.ParentId == oldParentId && t.Status == oldStatus)
                .OrderBy(t => t.SortOrder)
                .ToList();

            var rootIds = rootTasksToMove.Select(t => t.Id).ToHashSet();
            var remaining = siblings.Where(t => !rootIds.Contains(t.Id)).ToList();

            newSortOrder = Math.Clamp(newSortOrder, 0, remaining.Count);
            remaining.InsertRange(newSortOrder, rootTasksToMove);

            finalSortOrders = remaining.Select((task, index) => new { task.Id, Index = index })
                                     .ToDictionary(x => x.Id, x => x.Index);
        }
        else
        {
            // 다른 리스트로 이동
            // 기존 위치의 형제들 재정렬
            var oldSiblings = allTasks
                .Where(t => t.ParentId == oldParentId && t.Status == oldStatus && !allAffectedIds.Contains(t.Id))
                .OrderBy(t => t.SortOrder)
                .ToList();

            for (int i = 0; i < oldSiblings.Count; i++)
            {
                finalSortOrders[oldSiblings[i].Id] = i;
            }

            // 새 위치의 형제들과 함께 배치
            var destSiblings = allTasks
                .Where(t => t.ParentId == newParentId && t.Status == newStatus && !allAffectedIds.Contains(t.Id))
                .OrderBy(t => t.SortOrder)
                .ToList();

            newSortOrder = Math.Clamp(newSortOrder, 0, destSiblings.Count);
            destSiblings.InsertRange(newSortOrder, rootTasksToMove);

            for (int i = 0; i < destSiblings.Count; i++)
            {
                finalSortOrders[destSiblings[i].Id] = i;
            }
        }

        return finalSortOrders;
    }

    private async Task ApplyChangesToDatabase(
        GtdDbContext context,
        HashSet<int> allAffectedIds,
        Dictionary<int, int> sortOrders,
        TaskStatus newStatus,
        int? newParentId,
        List<TaskItem> rootTasksToMove)
    {
        var allIdsToUpdate = allAffectedIds.Union(sortOrders.Keys);
        var tasksToUpdate = await context.Tasks.Where(t => allIdsToUpdate.Contains(t.Id)).ToListAsync();
        var tasksMap = tasksToUpdate.ToDictionary(t => t.Id);

        // 상태 및 부모 업데이트
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

        // 정렬 순서 업데이트
        foreach (var kvp in sortOrders)
        {
            if (tasksMap.TryGetValue(kvp.Key, out var task))
            {
                task.SortOrder = kvp.Value;
            }
        }

        await context.SaveChangesAsync();
    }

    private async Task UpdatePathDepthForMovedTasks(GtdDbContext context, List<TaskItem> rootTasks)
    {
        foreach (var rootTask in rootTasks)
        {
            await UpdatePathDepthRecursiveAsync(context, rootTask);
        }
    }

    private async Task UpdatePathDepthRecursiveAsync(GtdDbContext context, TaskItem node)
    {
        string Pad(int n) => n.ToString("D6");

        if (node.ParentId == null)
        {
            node.Path = Pad(node.Id);
            node.Depth = 0;
        }
        else
        {
            var parent = await context.Tasks.AsNoTracking()
                .SingleOrDefaultAsync(t => t.Id == node.ParentId.Value);

            if (parent == null)
            {
                throw new InvalidOperationException($"Parent task with ID {node.ParentId} not found during Path update.");
            }

            node.Path = $"{parent.Path}/{Pad(node.Id)}";
            node.Depth = parent.Depth + 1;
        }

        var children = await context.Tasks.Where(t => t.ParentId == node.Id).ToListAsync();
        foreach (var child in children)
        {
            await UpdatePathDepthRecursiveAsync(context, child);
        }
    }
}
