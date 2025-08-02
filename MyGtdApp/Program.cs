// Program.cs
// ─────────────────────────────────────────────────────────────
using Microsoft.EntityFrameworkCore;
using MyGtdApp.Components;
using MyGtdApp.Models;
using MyGtdApp.Repositories;
using MyGtdApp.Services;
using System.Text.Json;
using System.Text.Json.Serialization;

// ------------------------------------------------------------
// 0. 기본 빌더
// ------------------------------------------------------------
var builder = WebApplication.CreateBuilder(args);

// 메모리 캐시
builder.Services.AddMemoryCache();

// ------------------------------------------------------------
// 1. 데이터베이스 (SQLite)
// ------------------------------------------------------------
var dbDir = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dbDir);
var dbPath = Path.Combine(dbDir, "mygtd.db");
var connectionString = $"Data Source={dbPath}";

builder.Services.AddDbContextFactory<GtdDbContext>(opt =>
    opt.UseSqlite(connectionString));

// ------------------------------------------------------------
// 2. 레포지터리 · 서비스 등록
// ------------------------------------------------------------
builder.Services.AddScoped<ITaskRepository, TaskRepository>();

builder.Services.AddScoped<ITaskMoveService, TaskMoveService>();
builder.Services.AddScoped<ITaskDataService, TaskDataService>();
builder.Services.AddScoped<ITaskService, DatabaseTaskService>();
builder.Services.AddScoped<ISidebarJsService, SidebarJsService>();
builder.Services.AddScoped<IGtdBoardJsService, GtdBoardJsService>();


// ------------------------------------------------------------
// 3. Blazor 컴포넌트
// ------------------------------------------------------------
builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

// ------------------------------------------------------------
// 4. 애플리케이션 빌드
// ------------------------------------------------------------
var app = builder.Build();

// ------------------------------------------------------------
// 5. DB 초기화 & 샘플 데이터 삽입
// ------------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContextFactory = services.GetRequiredService<IDbContextFactory<GtdDbContext>>();

    // ➊ 첫 번째 DbContext : DB 생성 및 샘플 데이터 삽입
    using (var ctx = dbContextFactory.CreateDbContext())
    {
        ctx.Database.EnsureCreated();

        if (!ctx.Tasks.Any())
        {
            try
            {
                var jsonText = File.ReadAllText("wwwroot/sample-data/tasks.json");

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter() }
                };

                var initial = JsonSerializer.Deserialize<JsonTaskHelper>(jsonText, jsonOptions);

                if (initial?.Tasks is { Count: > 0 })
                {
                    foreach (var t in initial.Tasks)
                    {
                        t.Children = new List<TaskItem>();
                        t.Contexts ??= new List<string>();
                        if (!t.IsExpanded) t.IsExpanded = true;
                    }

                    ctx.Tasks.AddRange(initial.Tasks);
                    ctx.SaveChanges();

                    Console.WriteLine($"샘플 데이터 {initial.Tasks.Count}건 삽입 완료");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"초기 데이터 삽입 오류: {ex.Message}");
            }
        }
    }

    // ➋ ★ 두 번째 DbContext : Path · Depth 계산
    using (var ctx2 = dbContextFactory.CreateDbContext())
    {
        await MyGtdApp.Infrastructure.Seeders.FillPathDepth.RunAsync(ctx2);
    }
}

// ------------------------------------------------------------
// 6. HTTP 파이프라인
// ------------------------------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseHttpsRedirection(); // 프로덕션에서만 실행
}
else
{
    Console.WriteLine("[DEV] HTTPS 리디렉션 비활성화");
}


/* ✚ 추가 : CSP */
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "connect-src 'self' ws: wss: https: http://localhost:*; " +  // 🔧 http://localhost:* 추가
        "script-src 'self' 'unsafe-inline' 'unsafe-eval' 'wasm-unsafe-eval'; " +
        "style-src  'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
        "font-src   'self' https://cdn.jsdelivr.net data:; " +
        "img-src    'self' data: blob:; " +
        "frame-ancestors 'self';";
    await next();
});


app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

// 개발용 디버그 엔드포인트
if (app.Environment.IsDevelopment())
{
    app.MapGet("/api/debug/tasks", async (ITaskService taskSvc) =>
    {
        var tasks = await taskSvc.GetAllTasksAsync();
        return Results.Json(tasks);
    });
}

app.Run();