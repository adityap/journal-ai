using Microsoft.EntityFrameworkCore;
using JournalAI.Backend.Models;

namespace JournalAI.Backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Entry> Entries => Set<Entry>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Media> Media => Set<Media>();
    public DbSet<ExportJob> ExportJobs => Set<ExportJob>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>().HasKey(u => u.Id);
        modelBuilder.Entity<Entry>().HasKey(e => e.Id);
        // Minimal configurations for scaffold
    }
}
