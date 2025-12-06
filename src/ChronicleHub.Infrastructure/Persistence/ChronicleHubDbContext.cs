namespace ChronicleHub.Infrastructure.Persistence;

using ChronicleHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public sealed class ChronicleHubDbContext : DbContext
{
    public ChronicleHubDbContext(DbContextOptions<ChronicleHubDbContext> options)
        : base(options)
    {
    }

    public DbSet<ActivityEvent> Events => Set<ActivityEvent>();
    public DbSet<DailyStats> DailyStats => Set<DailyStats>();
    public DbSet<CategoryStats> CategoryStats => Set<CategoryStats>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ActivityEvent
        modelBuilder.Entity<ActivityEvent>(cfg =>
        {
            cfg.ToTable("ActivityEvents");

            cfg.HasKey(e => e.Id);

            cfg.Property(e => e.Type)
                .IsRequired()
                .HasMaxLength(200);

            cfg.Property(e => e.Source)
                .IsRequired()
                .HasMaxLength(200);

            cfg.Property(e => e.PayloadJson)
                .IsRequired();

            cfg.HasIndex(e => new { e.TenantId, e.UserId, e.TimestampUtc });
        });

        // DailyStats
        modelBuilder.Entity<DailyStats>(cfg =>
        {
            cfg.ToTable("DailyStats");

            cfg.HasKey(x => x.Id);

            cfg.HasIndex(x => new { x.TenantId, x.UserId, x.Date })
                .IsUnique();
        });

        // CategoryStats
        modelBuilder.Entity<CategoryStats>(cfg =>
        {
            cfg.ToTable("CategoryStats");

            cfg.HasKey(x => x.Id);

            cfg.Property(x => x.Category)
                .IsRequired()
                .HasMaxLength(200);

            cfg.HasIndex(x => new { x.TenantId, x.UserId, x.Category })
                .IsUnique();
        });
    }
}
