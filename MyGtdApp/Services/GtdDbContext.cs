using Microsoft.EntityFrameworkCore;
using MyGtdApp.Models;

namespace MyGtdApp.Services;

public class GtdDbContext : DbContext
{
    public GtdDbContext(DbContextOptions<GtdDbContext> options) : base(options)
    {
    }

    public DbSet<TaskItem> Tasks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 추가 : TaskItem 구성 적용
        modelBuilder.ApplyConfiguration(new TaskItemConfiguration());
    }
}
