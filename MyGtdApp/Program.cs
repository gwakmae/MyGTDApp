// Program.cs (최종 수정본)
using Microsoft.EntityFrameworkCore;
using MyGtdApp.Components;
using MyGtdApp.Services;

var builder = WebApplication.CreateBuilder(args);

/* ────────────────────────────────
 * 1) 연결 문자열 결정
 * ──────────────────────────────── */
string? connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? ConvertUrlToNpgsql(Environment.GetEnvironmentVariable("DATABASE_URL"))
    ?? throw new InvalidOperationException("PostgreSQL 연결 문자열을 찾을 수 없습니다.");

/* ────────────────────────────────
 * 2) 서비스 등록
 * ──────────────────────────────── */
builder.Services.AddDbContext<GtdDbContext>(opt => opt.UseNpgsql(connectionString));
builder.Services.AddScoped<ITaskService, DatabaseTaskService>();

builder.Services.AddRazorComponents()
       .AddInteractiveServerComponents();

var app = builder.Build();

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

    return $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};" +
           $"Username={user};Password={pass};Ssl Mode=Require;Trust Server Certificate=true";
}