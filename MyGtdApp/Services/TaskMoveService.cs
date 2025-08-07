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

    // 기존 단일 이동 메서드
    public async Task MoveTaskAsync(int taskId, TaskStatus newStatus, int? newParentId, int newSortOrder)
    {
        // 🔄 수정: 단일 이동을 다중 이동의 특수한 경우로 처리하여 코드 재사용
        await MoveTasksAsync(new List<int> { taskId }, newStatus, newParentId, newSortOrder);
    }

    // 🆕 추가: 새로운 다중 이동 핵심 메서드
    public async Task MoveTasksAsync(List<int> taskIds, TaskStatus newStatus, int? newParentId, int newSortOrder)
    {
        if (taskIds == null || !taskIds.Any()) return;

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            // --- 1. 전체 이동 대상 확정 (선택된 항목 + 모든 자손) ---
            var allTasks = await context.Tasks.AsNoTracking().ToListAsync();
            var allAffectedIds = GetAllAffectedIds(taskIds, allTasks);

            // --- 2. 유효성 검사 (순환 참조 방지) ---
            if (newParentId.HasValue && allAffectedIds.Contains(newParentId.Value))
            {
                // 자신의 자손 밑으로 이동하려는 시도 차단
                await transaction.RollbackAsync();
                return;
            }
            
            // --- 3. DB에서 실제 Task 엔티티 가져오기 ---
            var tasksToMove = await context.Tasks.Where(t => taskIds.Contains(t.Id)).ToListAsync();
            
            // --- 4. 기존 위치 정리 (형제들 순서 재정렬) ---
            var tasksByOldParent = tasksToMove.GroupBy(t => new { t.ParentId, t.Status });
            foreach (var group in tasksByOldParent)
            {
                var siblings = await context.Tasks
                    .Where(t => t.ParentId == group.Key.ParentId && t.Status == group.Key.Status && !taskIds.Contains(t.Id))
                    .OrderBy(t => t.SortOrder)
                    .ToListAsync();
                for (int i = 0; i < siblings.Count; i++) siblings[i].SortOrder = i;
            }

            // --- 5. 새 위치의 형제들 가져오기 ---
            var newSiblings = await context.Tasks
                .Where(t => t.ParentId == newParentId && t.Status == newStatus && !taskIds.Contains(t.Id))
                .OrderBy(t => t.SortOrder)
                .ToListAsync();

            // --- 6. 이동 작업 수행 (평탄화 및 정렬) ---
            newSortOrder = Math.Clamp(newSortOrder, 0, newSiblings.Count);
            
            // 먼저 기존 형제들의 순서를 뒤로 밀어 공간 확보
            foreach (var sibling in newSiblings.Skip(newSortOrder))
            {
                sibling.SortOrder += tasksToMove.Count;
            }

            // 선택된 항목들을 새 위치에 순서대로 삽입
            var currentSortOrder = newSortOrder;
            foreach (var task in tasksToMove.OrderBy(t => t.SortOrder))
            {
                task.ParentId = newParentId;
                task.Status = newStatus;
                task.SortOrder = currentSortOrder++;
            }

            await context.SaveChangesAsync();

            // --- 7. Path 및 Depth 재귀적 업데이트 ---
            foreach (var task in tasksToMove)
            {
                await UpdatePathDepthRecursiveAsync(context, task);
            }
            
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
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
            // 🔄 수정: FindAsync 대신 비동기 메서드 SingleOrDefaultAsync 사용
            var parent = await ctx.Tasks.SingleOrDefaultAsync(t => t.Id == node.ParentId.Value);
            if(parent == null) throw new InvalidOperationException($"Parent task with ID {node.ParentId} not found.");
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