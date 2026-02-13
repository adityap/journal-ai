using Microsoft.EntityFrameworkCore;
using JournalAI.Backend.Models;

namespace JournalAI.Backend.Data;

/// <summary>
/// Application database context for Journal AI
/// Configures all entity models, relationships, and indexes
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Entity sets
    public DbSet<User> Users { get; set; }
    public DbSet<Entry> Entries { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Media> Media { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<ExportJob> ExportJobs { get; set; }
    public DbSet<UnlockSession> UnlockSessions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.Timezone).IsRequired().HasMaxLength(64).HasDefaultValue("UTC");
            entity.Property(e => e.NoTrainingUse).HasDefaultValue(true);
            entity.Property(e => e.Settings).HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasMany(e => e.Entries).WithOne(e => e.User).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Categories).WithOne(e => e.User).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.AuditLogs).WithOne(e => e.User).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.SetNull);
        });

        // Entry configuration
        modelBuilder.Entity<Entry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.Title).HasMaxLength(255);
            entity.Property(e => e.BodyText).HasColumnType("text");
            entity.Property(e => e.Type).IsRequired().HasMaxLength(16).HasDefaultValue("text");
            entity.Property(e => e.Confidentiality).IsRequired().HasMaxLength(16).HasDefaultValue("public");
            entity.Property(e => e.ConfidentialityMethod).HasMaxLength(32).HasDefaultValue("none");
            entity.Property(e => e.Tags).HasColumnType("text[]");
            entity.Property(e => e.SentimentScore);
            entity.Property(e => e.SentimentLabel).HasMaxLength(32);
            entity.Property(e => e.SentimentModel).HasMaxLength(64);
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.ReadOnlyAfter).IsRequired();
            entity.Property(e => e.Immutable).HasDefaultValue(false);
            entity.Property(e => e.Metadata).HasColumnType("jsonb");
            entity.Property(e => e.Source).HasMaxLength(16).HasDefaultValue("ui");
            entity.Property(e => e.OriginalHash).HasMaxLength(64);

            entity.HasIndex(e => new { e.UserId, e.CreatedAt }).IsDescending(false, true);
            entity.HasIndex(e => new { e.UserId, e.CategoryId });
            entity.HasIndex(e => e.Immutable);
            entity.HasIndex(e => new { e.UserId, e.Confidentiality });

            entity.HasOne(e => e.User).WithMany(u => u.Entries).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Category).WithMany(c => c.Entries).HasForeignKey(e => e.CategoryId).OnDelete(DeleteBehavior.SetNull);
            entity.HasMany(e => e.Media).WithOne(m => m.Entry).HasForeignKey(m => m.EntryId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.AuditLogs).WithOne(a => a.Entry).HasForeignKey(a => a.EntryId).OnDelete(DeleteBehavior.Cascade);
        });

        // Category configuration
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(128);
            entity.Property(e => e.Color).HasMaxLength(7).HasDefaultValue("#000000");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => new { e.UserId, e.Name }).IsUnique();
            entity.HasOne(e => e.User).WithMany(u => u.Categories).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        // Media configuration
        modelBuilder.Entity<Media>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.Url).IsRequired();
            entity.Property(e => e.MimeType).IsRequired().HasMaxLength(64);
            entity.Property(e => e.Size).IsRequired();
            entity.Property(e => e.StorageKey).IsRequired().HasMaxLength(512);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => new { e.UserId, e.CreatedAt }).IsDescending(false, true);
            entity.HasIndex(e => e.EntryId);

            entity.HasOne(e => e.User).WithMany(u => u.Media).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Entry).WithMany(en => en.Media).HasForeignKey(e => e.EntryId).OnDelete(DeleteBehavior.SetNull);
        });

        // AuditLog configuration
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EntryId).IsRequired();
            entity.Property(e => e.Action).IsRequired().HasMaxLength(16);
            entity.Property(e => e.ActorIp).HasMaxLength(45);
            entity.Property(e => e.Timestamp).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Diff).HasColumnType("jsonb");

            entity.HasIndex(e => new { e.EntryId, e.Timestamp }).IsDescending(false, true);
            entity.HasIndex(e => new { e.UserId, e.Timestamp }).IsDescending(false, true);

            entity.HasOne(e => e.Entry).WithMany(en => en.AuditLogs).HasForeignKey(e => e.EntryId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User).WithMany(u => u.AuditLogs).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.SetNull);
        });

        // ExportJob configuration
        modelBuilder.Entity<ExportJob>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.Scope).HasColumnType("jsonb");
            entity.Property(e => e.Format).IsRequired().HasMaxLength(16).HasDefaultValue("json");
            entity.Property(e => e.Status).IsRequired().HasMaxLength(16).HasDefaultValue("queued");
            entity.Property(e => e.DownloadUrl).HasMaxLength(1024);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.CompletedAt);
            entity.Property(e => e.ErrorMessage).HasColumnType("text");

            entity.HasIndex(e => new { e.UserId, e.CreatedAt }).IsDescending(false, true);
            entity.HasIndex(e => e.Status);

            entity.HasOne(e => e.User).WithMany(u => u.ExportJobs).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
