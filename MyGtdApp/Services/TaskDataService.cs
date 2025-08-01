using Microsoft.EntityFrameworkCore;
using MyGtdApp.Models;
using MyGtdApp.Repositories;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyGtdApp.Services;

public class TaskDataService : ITaskDataService
{
    private readonly ITaskRepository _repository;
    private readonly IDbContextFactory<GtdDbContext> _contextFactory;

    public TaskDataService(ITaskRepository repository, IDbContextFactory<GtdDbContext> contextFactory)
    {
        _repository = repository;
        _contextFactory = contextFactory;
    }

    public async Task<string> ExportTasksToJsonAsync()
    {
        var allTasks = await _repository.GetAllRawAsync();
        var exportData = new { tasks = allTasks };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };

        return JsonSerializer.Serialize(exportData, options);
    }

    public async Task ImportTasksFromJsonAsync(string jsonData)
    {
        var dto = JsonSerializer.Deserialize<JsonTaskHelper>(jsonData);
        if (dto?.Tasks is not { Count: > 0 }) return;

        await using var ctx = _contextFactory.CreateDbContext();
        await using var tx = await ctx.Database.BeginTransactionAsync();
        try
        {
            ctx.Tasks.RemoveRange(ctx.Tasks);           // 전체 삭제
            await ctx.SaveChangesAsync();

            ctx.Tasks.AddRange(dto.Tasks);              // 새 데이터 삽입
            await ctx.SaveChangesAsync();

            await tx.CommitAsync();

            // ▶ 관계(Path/Depth) 다시 계산
            await Infrastructure.Seeders.FillPathDepth.RunAsync(ctx);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}