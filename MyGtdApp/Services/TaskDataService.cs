using Microsoft.EntityFrameworkCore;
using MyGtdApp.Models;
using MyGtdApp.Repositories;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic; // 추가: 딕셔너리 사용
using System.Linq; // 추가: LINQ 쿼

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
        // 1. JSON 파싱 (기존 코드)
        var dto = JsonSerializer.Deserialize<JsonTaskHelper>(jsonData);
        if (dto?.Tasks is not { Count: > 0 })
        {
            throw new InvalidOperationException("파일에 작업 데이터가 없거나 잘못되었습니다.");
        }

        // ★ 새로 추가: 유효성 검사 함수 호출
        ValidateImportedTasks(dto.Tasks);

        // 2. 데이터베이스 처리 (기존 코드, 검사 통과 후 실행)
        await using var ctx = _contextFactory.CreateDbContext();
        await using var tx = await ctx.Database.BeginTransactionAsync();
        try
        {
            ctx.Tasks.RemoveRange(ctx.Tasks);           // 전체 삭제
            await ctx.SaveChangesAsync();

            ctx.Tasks.AddRange(dto.Tasks);              // 새 데이터 삽입
            await ctx.SaveChangesAsync();

            await tx.CommitAsync();

            // 관계(Path/Depth) 다시 계산
            await Infrastructure.Seeders.FillPathDepth.RunAsync(ctx);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // ★ 새로 추가: 검사 함수 (여기서 데이터 확인)
    private void ValidateImportedTasks(List<TaskItem> tasks)
    {
        // 1. 데이터 크기 제한 (예: 1000개 초과 시 에러)
        if (tasks.Count > 1000)
        {
            throw new InvalidOperationException("파일에 너무 많은 작업이 있습니다. (최대 1000개 허용)");
        }

        // 2. ID 중복 확인 (딕셔너리로 빠르게 체크)
        var idSet = new HashSet<int>();
        foreach (var task in tasks)
        {
            if (!idSet.Add(task.Id))
            {
                throw new InvalidOperationException($"ID {task.Id}가 중복되었습니다. 파일을 확인하세요.");
            }
        }

        // 3. 필수 필드 확인 (Title 비어 있거나 Status 유효하지 않음)
        foreach (var task in tasks)
        {
            if (string.IsNullOrWhiteSpace(task.Title))
            {
                throw new InvalidOperationException($"작업 ID {task.Id}의 제목이 비어 있습니다.");
            }
            if (!Enum.IsDefined(typeof(MyGtdApp.Models.TaskStatus), task.Status))
            {
                throw new InvalidOperationException($"작업 ID {task.Id}의 상태({task.Status})가 유효하지 않습니다.");
            }
            // Contexts가 null이면 빈 리스트로 초기화 (안전 조치)
            task.Contexts ??= new List<string>();
        }

        // 4. 부모 ID 확인 (존재 여부 + 순환 방지)
        var taskDict = tasks.ToDictionary(t => t.Id); // ID로 빠르게 찾기
        foreach (var task in tasks)
        {
            if (task.ParentId.HasValue)
            {
                var parentId = task.ParentId.Value;
                if (!taskDict.ContainsKey(parentId))
                {
                    throw new InvalidOperationException($"작업 ID {task.Id}의 부모 ID {parentId}가 파일에 없습니다.");
                }
                if (parentId == task.Id)
                {
                    throw new InvalidOperationException($"작업 ID {task.Id}가 자기 자신을 부모로 가리키고 있습니다. (순환 오류)");
                }
                // 간단한 순환 체크 (더 깊게 하려면 그래프 탐색 추가 가능)
                var current = parentId;
                var visited = new HashSet<int>();
                while (taskDict.TryGetValue(current, out var parent))
                {
                    if (visited.Contains(current))
                    {
                        throw new InvalidOperationException($"작업 ID {task.Id}에서 순환 관계가 발견되었습니다.");
                    }
                    visited.Add(current);
                    if (!parent.ParentId.HasValue) break;
                    current = parent.ParentId.Value;
                }
            }
        }

        // 모든 검사 통과!
    }
}