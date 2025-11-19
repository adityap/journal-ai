using Microsoft.EntityFrameworkCore;
using JournalAI.Models;

namespace JournalAI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Entry> Entries { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<Media> Media { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public DbSet<ExportJob> ExportJobs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();

        modelBuilder.Entity<Entry>()
            .HasIndex(e => new { e.UserId, e.CreatedAt });

        // configure arrays for tags (Npgsql)
        modelBuilder.Entity<Entry>().Property(e => e.Tags).HasColumnType("text[]");
    }
}
