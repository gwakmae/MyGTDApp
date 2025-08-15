using Microsoft.EntityFrameworkCore;
using MyGtdApp.Models;
using MyGtdApp.Services;
using System.Collections.Generic;
using System.Linq;
using TaskStatus = MyGtdApp.Models.TaskStatus;

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
        await context.SaveChangesAsync();

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

        await context.SaveChangesAsync();
        return task;
    }

    public async Task UpdateAsync(TaskItem task)
    {
        using var context = _dbContextFactory.CreateDbContext();
        context.Tasks.Update(task);
        await context.SaveChangesAsync();
    }

    public async Task UpdateRangeAsync(IEnumerable<TaskItem> tasks)
    {
        using var context = _dbContextFactory.CreateDbContext();
        context.Tasks.UpdateRange(tasks);
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
        var allTasks = await contextDb.Tasks
            .Where(t => !t.IsCompleted)
            .ToListAsync();

        return allTasks
            .Where(t => t.Contexts.Contains(context))
            .OrderBy(t => t.Status)
            .ThenBy(t => t.SortOrder)
            .ToList();
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

    public async Task<List<TaskItem>> GetFocusTasksAsync()
    {
        using var context = _dbContextFactory.CreateDbContext();
        var today = DateTime.Today;
        return await context.Tasks
            .Where(t => !t.IsCompleted &&
                        (t.Priority == Priority.High || (t.DueDate.HasValue && t.DueDate.Value.Date <= today.AddDays(3))))
            .OrderBy(t => t.DueDate ?? DateTime.MaxValue)
            .ThenByDescending(t => t.Priority)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task BulkUpdateTasksAsync(BulkUpdateModel model)
    {
        if (model.TaskIds == null || !model.TaskIds.Any())
            return;

        using var context = _dbContextFactory.CreateDbContext();
        var tasksToUpdate = await context.Tasks
            .Where(t => model.TaskIds.Contains(t.Id))
            .ToListAsync();

        foreach (var task in tasksToUpdate)
        {
            if (model.DueDate.HasValue) task.DueDate = model.DueDate;
            if (model.Priority.HasValue) task.Priority = model.Priority.Value;

            if (!string.IsNullOrWhiteSpace(model.ContextToAdd))
            {
                var c = model.ContextToAdd.StartsWith("@") ? model.ContextToAdd : $"@{model.ContextToAdd}";
                if (!task.Contexts.Contains(c)) task.Contexts.Add(c);
            }
            if (!string.IsNullOrWhiteSpace(model.ContextToRemove))
            {
                var c = model.ContextToRemove.StartsWith("@") ? model.ContextToRemove : $"@{model.ContextToRemove}";
                task.Contexts.Remove(c);
            }
        }

        await context.SaveChangesAsync();
    }

    public async Task DeleteTasksAsync(List<int> taskIds)
    {
        if (taskIds == null || !taskIds.Any()) return;

        using var context = _dbContextFactory.CreateDbContext();
        var allTasks = await context.Tasks.AsNoTracking().ToListAsync();
        var allIdsToDelete = new HashSet<int>();

        foreach (var id in taskIds)
        {
            var task = allTasks.FirstOrDefault(t => t.Id == id);
            if (task != null)
            {
                var descendants = allTasks.Where(t => t.Path.StartsWith(task.Path + "/")).Select(t => t.Id);
                allIdsToDelete.Add(id);
                foreach (var d in descendants) allIdsToDelete.Add(d);
            }
        }

        var toDelete = await context.Tasks.Where(t => allIdsToDelete.Contains(t.Id)).ToListAsync();
        if (toDelete.Any())
        {
            context.Tasks.RemoveRange(toDelete);
            await context.SaveChangesAsync();
        }
    }

    public async Task<List<string>> GetAllContextsAsync()
    {
        using var context = _dbContextFactory.CreateDbContext();
        var allTasks = await context.Tasks.ToListAsync();
        return allTasks.SelectMany(t => t.Contexts)
                       .Distinct()
                       .OrderBy(c => c)
                       .ToList();
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
