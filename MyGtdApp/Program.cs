using Microsoft.EntityFrameworkCore;
using MyGtdApp.Components;
using MyGtdApp.Services;
using MyGtdApp.Models;
using System.Text.Json;
using System.Text.Json.Serialization; // ✨ JsonStringEnumConverter를 위해 추가

var builder = WebApplication.CreateBuilder(args);

// --- 1. 서비스 등록 (SQLite 데이터베이스 사용) ---
var connectionString = "Data Source=mygtd.db";
builder.Services.AddDbContextFactory<GtdDbContext>(opt => opt.UseSqlite(connectionString));
builder.Services.AddScoped<ITaskService, DatabaseTaskService>();

builder.Services.AddRazorComponents()
       .AddInteractiveServerComponents();

// --- 2. 애플리케이션 빌드 ---
var app = builder.Build();

// --- 3. 데이터베이스 초기화 및 초기 데이터 삽입 ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<GtdDbContext>();

    context.Database.EnsureCreated();

    if (!context.Tasks.Any())
    {
        var jsonText = File.ReadAllText("wwwroot/sample-data/tasks.json");
        
        // 👇 아래 옵션에 JsonStringEnumConverter를 추가했습니다.
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };
        var initialData = JsonSerializer.Deserialize<JsonTaskHelper>(jsonText, jsonOptions);

        if (initialData?.Tasks != null && initialData.Tasks.Any())
        {
            context.Tasks.AddRange(initialData.Tasks);
            context.SaveChanges();
        }
    }
}

// --- 4. 미들웨어 파이프라인 ---
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

// --- 5. 애플리케이션 실행 ---
app.Run();

// --- 6. JSON 파싱을 위한 헬퍼 클래스 ---
public class JsonTaskHelper
{
    public List<TaskItem> Tasks { get; set; } = new();
}