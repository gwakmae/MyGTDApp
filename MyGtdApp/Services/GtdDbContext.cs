using Microsoft.EntityFrameworkCore;
using MyGtdApp.Models;

namespace MyGtdApp.Services;

public class GtdDbContext : DbContext
{
    public GtdDbContext(DbContextOptions<GtdDbContext> options) : base(options)
    {
    }

    public DbSet<TaskItem> Tasks { get; set; }
}