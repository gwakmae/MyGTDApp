using MyGtdApp.Components;
using MyGtdApp.Services;
using MyGtdApp.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// 1. 앱 시작 시 JSON 데이터를 동기적으로 미리 로드합니다.
var jsonString = File.ReadAllText("wwwroot/sample-data/tasks.json");
var options = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    Converters = { new JsonStringEnumConverter() }
};

var dataWrapper = JsonSerializer.Deserialize<TaskDataWrapper>(jsonString, options);
var initialTasks = dataWrapper?.Tasks ?? new List<TaskItem>();

// 2. 미리 로드한 데이터를 싱글턴(Singleton) 리스트로 등록합니다.
builder.Services.AddSingleton(initialTasks);

// 3. 싱글턴 리스트를 주입받는 InMemoryTaskService를 Scoped로 등록합니다.
builder.Services.AddScoped<ITaskService, InMemoryTaskService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();

// [수정] JSON 파일의 구조를 담는 도우미 클래스를 파일의 맨 아래로 이동하여
// CS8803 오류를 해결합니다.
file class TaskDataWrapper
{
    public List<TaskItem> Tasks { get; set; } = new();
}