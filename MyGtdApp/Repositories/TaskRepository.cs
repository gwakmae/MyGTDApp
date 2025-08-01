using Microsoft.EntityFrameworkCore;
using MyGtdApp.Models;
using MyGtdApp.Services;
using TaskStatus = MyGtdApp.Models.TaskStatus; // 모호성 해결

namespace MyGtdApp.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly IDbContextFactory<GtdDbContext> _dbContextFactory;

    public TaskRepository(IDbContextFactory<GtdDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<List<TaskItem>> GetAllAsync()
    {
        using var context = _dbContextFactory.CreateDbContext();

        var allTasks = await context.Tasks
                                     .AsNoTracking()
                                     .OrderBy(t => t.SortOrder)
                                     .ToListAsync();

        var lookup = allTasks.ToDictionary(t => t.Id);

        foreach (var t in allTasks) t.Children = new();

        foreach (var t in allTasks)
        {
            if (t.ParentId.HasValue && lookup.TryGetValue(t.ParentId.Value, out var parent))
            {
                parent.Children.Add(t);
            }
        }

        void SortRecursive(TaskItem node)
        {
            node.Children = node.Children.OrderBy(c => c.SortOrder).ToList();
            foreach (var c in node.Children) SortRecursive(c);
        }

        var topLevel = allTasks.Where(t => t.ParentId == null)
                               .OrderBy(t => t.SortOrder)
                               .ToList();

        foreach (var root in topLevel) SortRecursive(root);

        return topLevel;
    }

    public async Task<List<TaskItem>> GetAllRawAsync()
    {
        using var context = _dbContextFactory.CreateDbContext();
        return await context.Tasks
            .AsNoTracking()
            .OrderBy(t => t.Id)
            .ToListAsync();
    }

    public async Task<TaskItem?> GetByIdAsync(int id)
    {
        using var context = _dbContextFactory.CreateDbContext();
        return await context.Tasks.FindAsync(id);
    }

    public async Task<TaskItem> AddAsync(TaskItem task)
    {
        using var context = _dbContextFactory.CreateDbContext();

        var maxSortOrder = await context.Tasks
            .Where(t => t.ParentId == task.ParentId && t.Status == task.Status)
            .Select(t => (int?)t.SortOrder)
            .MaxAsync() ?? -1;

        task.SortOrder = maxSortOrder + 1;

        context.Tasks.Add(task);
        await context.SaveChangesAsync();    // Id 확정

        /* ---------- 여기부터 변경 ---------- */
        string Pad(int n) => n.ToString("D6");

        if (task.ParentId == null)
        {
            task.Path = Pad(task.Id);
            task.Depth = 0;
        }
        else
        {
            var parent = await context.Tasks.FindAsync(task.ParentId);
            task.Path = $"{parent!.Path}/{Pad(task.Id)}";
            task.Depth = parent.Depth + 1;
        }
        /* ----------------------------------- */

        await context.SaveChangesAsync();
        return task;
    }

    public async Task UpdateAsync(TaskItem task)
    {
        using var context = _dbContextFactory.CreateDbContext();
        context.Tasks.Update(task);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var taskToDelete = await context.Tasks.FindAsync(id);
        if (taskToDelete != null)
        {
            await DeleteChildrenRecursive(context, id);
            context.Tasks.Remove(taskToDelete);
            await context.SaveChangesAsync();
        }
    }

    private async Task DeleteChildrenRecursive(GtdDbContext context, int parentId)
    {
        var children = await context.Tasks.Where(t => t.ParentId == parentId).ToListAsync();
        foreach (var child in children)
        {
            await DeleteChildrenRecursive(context, child.Id);
            context.Tasks.Remove(child);
        }
    }

    public async Task<List<TaskItem>> GetByStatusAsync(TaskStatus status)
    {
        using var context = _dbContextFactory.CreateDbContext();
        return await context.Tasks
            .Where(t => t.Status == status)
            .OrderBy(t => t.SortOrder)
            .ToListAsync();
    }

    public async Task DeleteByStatusRecursiveAsync(TaskStatus status)
    {
        using var ctx = _dbContextFactory.CreateDbContext();
        var roots = await ctx.Tasks
                              .Where(t => t.Status == status)
                              .ToListAsync();

        foreach (var r in roots)
        {
            await DeleteChildrenRecursive(ctx, r.Id);
            ctx.Tasks.Remove(r);
        }
        await ctx.SaveChangesAsync();
    }

    public async Task<List<TaskItem>> GetByContextAsync(string context)
    {
        using var contextDb = _dbContextFactory.CreateDbContext();
        return await contextDb.Tasks
            .Where(t => !t.IsCompleted && t.Contexts.Contains(context))
            .OrderBy(t => t.Status)
            .ThenBy(t => t.SortOrder)
            .ToListAsync();
    }

    public async Task<List<TaskItem>> GetTodayTasksAsync()
    {
        using var context = _dbContextFactory.CreateDbContext();
        var today = DateTime.Today;
        return await context.Tasks.Where(t =>
            !t.IsCompleted &&
            t.StartDate.HasValue &&
            t.StartDate.Value.Date <= today
        ).OrderBy(t => t.DueDate ?? DateTime.MaxValue)
         .ThenByDescending(t => t.Priority)
         .ToListAsync();
    }

    public async Task<List<string>> GetAllContextsAsync()
    {
        using var context = _dbContextFactory.CreateDbContext();
        var allTasks = await context.Tasks.ToListAsync();

        var allContexts = allTasks
                                   .SelectMany(t => t.Contexts)
                                   .Distinct()
                                   .OrderBy(c => c)
                                   .ToList();

        return allContexts;
    }

    public async Task UpdateExpandStateAsync(int taskId, bool isExpanded)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var task = await context.Tasks.FindAsync(taskId);
        if (task != null)
        {
            task.IsExpanded = isExpanded;
            await context.SaveChangesAsync();
        }
    }
}