using Microsoft.EntityFrameworkCore;
using MyGtdApp.Models;
using TaskStatus = MyGtdApp.Models.TaskStatus; // 모호성 해결

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
        using var context = _dbContextFactory.CreateDbContext();

        var taskToMove = await context.Tasks.FindAsync(taskId);
        if (taskToMove == null) return;

        // 순환 방지 로직
        if (newParentId != null)
        {
            int cursorId = newParentId.Value;
            while (true)
            {
                if (cursorId == taskId)
                {
                    return; // 자기 자손에게 넣으려는 시도 차단
                }

                var parentInfo = await context.Tasks
                                              .AsNoTracking()
                                              .Where(t => t.Id == cursorId)
                                              .Select(t => new { t.ParentId })
                                              .FirstOrDefaultAsync();

                if (parentInfo?.ParentId == null) break;
                cursorId = parentInfo.ParentId.Value;
            }
        }

        var oldStatus = taskToMove.Status;

        // 원래 형제들의 SortOrder 재정렬
        var oldSiblings = await context.Tasks
            .Where(t => t.ParentId == taskToMove.ParentId
                        && t.Status == oldStatus
                        && t.Id != taskId)
            .OrderBy(t => t.SortOrder)
            .ToListAsync();
        for (int i = 0; i < oldSiblings.Count; i++)
            oldSiblings[i].SortOrder = i;

        // 이동
        taskToMove.ParentId = newParentId;
        taskToMove.Status = newStatus;

        // 새 위치 형제들 + 자기 자신 정렬
        var newSiblings = await context.Tasks
            .Where(t => t.ParentId == newParentId
                        && t.Status == newStatus
                        && t.Id != taskId)
            .OrderBy(t => t.SortOrder)
            .ToListAsync();

        newSortOrder = Math.Clamp(newSortOrder, 0, newSiblings.Count);
        newSiblings.Insert(newSortOrder, taskToMove);

        for (int i = 0; i < newSiblings.Count; i++)
            newSiblings[i].SortOrder = i;

        await context.SaveChangesAsync();

        // ---- Path·Depth 갱신 ----
        await UpdatePathDepthAsync(context, taskToMove);
    }

    private static async Task UpdatePathDepthAsync(GtdDbContext ctx, TaskItem node)
    {
        /* ★ 추가 */
        string Pad(int n) => n.ToString("D6");
        /* ------- */

        // ── 자기 자신 ──
        if (node.ParentId == null)
        {
            node.Path = Pad(node.Id);          // ← 수정
            node.Depth = 0;
        }
        else
        {
            var parent = await ctx.Tasks.FindAsync(node.ParentId);
            node.Path = $"{parent!.Path}/{Pad(node.Id)}";   // ← 수정
            node.Depth = parent.Depth + 1;
        }

        // ── 자손 재귀 ──
        var descendants = await ctx.Tasks
                                   .Where(t => t.ParentId == node.Id)
                                   .ToListAsync();
        foreach (var d in descendants)
        {
            await UpdatePathDepthAsync(ctx, d);
        }
    }
}
