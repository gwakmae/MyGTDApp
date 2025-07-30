// Program.cs (최종 수정본)
using Microsoft.EntityFrameworkCore;
using MyGtdApp.Components;
using MyGtdApp.Services;
using Microsoft.Extensions.Logging; // LogLevel을 사용하기 위해 추가

var builder = WebApplication.CreateBuilder(args);

// 👇 이 코드를 추가하세요
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

/* ────────────────────────────────
 * 1) 연결 문자열 결정
 * ──────────────────────────────── */
string? connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? ConvertUrlToNpgsql(Environment.GetEnvironmentVariable("DATABASE_URL"))
    ?? throw new InvalidOperationException("PostgreSQL 연결 문자열을 찾을 수 없습니다.");

/* ────────────────────────────────
 * 2) 서비스 등록 (수정)
 * ──────────────────────────────── */
// [변경 전]
// builder.Services.AddDbContext<GtdDbContext>(opt => opt.UseNpgsql(connectionString));

// [변경 후] 👇 IDbContextFactory를 등록합니다.
builder.Services.AddDbContextFactory<GtdDbContext>(opt => opt.UseNpgsql(connectionString));

builder.Services.AddScoped<ITaskService, DatabaseTaskService>();

builder.Services.AddRazorComponents()
       .AddInteractiveServerComponents();

var app = builder.Build();

// 👇 이 코드를 추가하세요
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<GtdDbContext>();
    try
    {
        context.Database.EnsureCreated(); // 또는 context.Database.Migrate();
    }
    catch (Exception ex)
    {
        // 로그 출력 (배포 환경에서 확인 가능)
        Console.WriteLine($"Database initialization failed: {ex.Message}");
    }
}

/* ────────────────────────────────
 * 4) 미들웨어 파이프라인
 * ──────────────────────────────── */
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

app.Run();

/* ────────────────────────────────
 * 5) 헬퍼 함수
 * ──────────────────────────────── */
static string? ConvertUrlToNpgsql(string? url)
{
    if (string.IsNullOrWhiteSpace(url) ||
        url.StartsWith("Host=", StringComparison.OrdinalIgnoreCase))
        return url;

    var uri = new Uri(url);
    var userPass = uri.UserInfo.Split(':', 2);
    var user = userPass[0];
    var pass = userPass.Length > 1 ? userPass[1] : "";

    // 👇 [수정된 부분] uri.Port가 -1 (명시되지 않음)이면 기본 포트 5432를 사용합니다.
    var port = uri.Port != -1 ? uri.Port : 5432;

    return $"Host={uri.Host};Port={port};Database={uri.AbsolutePath.TrimStart('/')};" +
           $"Username={user};Password={pass};Ssl Mode=Require;Trust Server Certificate=true";
}
