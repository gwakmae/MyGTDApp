using Microsoft.EntityFrameworkCore;
using MyGtdApp.Components;
using MyGtdApp.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. 데이터베이스 연결 설정 추가
// Render.com에서 제공하는 데이터베이스 URL 환경 변수를 가져옵니다.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<GtdDbContext>(options =>
    options.UseNpgsql(connectionString));

// 2. 새로운 DatabaseTaskService를 ITaskService의 구현체로 등록합니다.
builder.Services.AddScoped<ITaskService, DatabaseTaskService>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// 3. 앱 시작 시 데이터베이스 자동 업데이트 (마이그레이션)
// 앱이 시작될 때마다 데이터베이스 스키마를 최신 상태로 유지합니다.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GtdDbContext>();
    db.Database.Migrate();
}


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