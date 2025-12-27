namespace ChronicleHub.Infrastructure.Persistence;

using ChronicleHub.Domain.Entities;
using ChronicleHub.Domain.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public sealed class ChronicleHubDbContext : IdentityDbContext<ApplicationUser>
{
    public ChronicleHubDbContext(DbContextOptions<ChronicleHubDbContext> options)
        : base(options)
    {
    }

    public DbSet<ActivityEvent> Events => Set<ActivityEvent>();
    public DbSet<DailyStats> DailyStats => Set<DailyStats>();
    public DbSet<CategoryStats> CategoryStats => Set<CategoryStats>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<UserTenant> UserTenants => Set<UserTenant>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // Tenant context management for global query filters
    private Guid? _currentTenantId;

    public void SetCurrentTenant(Guid tenantId)
    {
        _currentTenantId = tenantId;
    }

    public void ClearCurrentTenant()
    {
        _currentTenantId = null;
    }

    private Guid GetCurrentTenantId()
    {
        return _currentTenantId ?? Guid.Empty;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // MUST be first for Identity

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

        // Tenant
        modelBuilder.Entity<Tenant>(cfg =>
        {
            cfg.ToTable("Tenants");

            cfg.HasKey(t => t.Id);

            cfg.Property(t => t.Name)
                .IsRequired()
                .HasMaxLength(200);

            cfg.Property(t => t.Slug)
                .IsRequired()
                .HasMaxLength(100);

            cfg.HasIndex(t => t.Slug).IsUnique();
            cfg.HasIndex(t => t.IsActive);
        });

        // UserTenant
        modelBuilder.Entity<UserTenant>(cfg =>
        {
            cfg.ToTable("UserTenants");

            cfg.HasKey(ut => ut.Id);

            cfg.Property(ut => ut.UserId)
                .IsRequired()
                .HasMaxLength(450); // Standard Identity user ID length

            cfg.Property(ut => ut.Role)
                .IsRequired()
                .HasMaxLength(50);

            cfg.HasIndex(ut => new { ut.UserId, ut.TenantId }).IsUnique();
            cfg.HasIndex(ut => ut.TenantId);

            cfg.HasOne(ut => ut.Tenant)
                .WithMany()
                .HasForeignKey(ut => ut.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ApiKey
        modelBuilder.Entity<ApiKey>(cfg =>
        {
            cfg.ToTable("ApiKeys");

            cfg.HasKey(ak => ak.Id);

            cfg.Property(ak => ak.Name)
                .IsRequired()
                .HasMaxLength(100);

            cfg.Property(ak => ak.KeyHash)
                .IsRequired()
                .HasMaxLength(64);

            cfg.Property(ak => ak.KeyPrefix)
                .IsRequired()
                .HasMaxLength(20);

            cfg.HasIndex(ak => ak.KeyHash);
            cfg.HasIndex(ak => new { ak.TenantId, ak.IsActive });

            cfg.HasOne(ak => ak.Tenant)
                .WithMany()
                .HasForeignKey(ak => ak.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // RefreshToken
        modelBuilder.Entity<RefreshToken>(cfg =>
        {
            cfg.ToTable("RefreshTokens");

            cfg.HasKey(rt => rt.Id);

            cfg.Property(rt => rt.UserId)
                .IsRequired()
                .HasMaxLength(450); // Standard Identity user ID length

            cfg.Property(rt => rt.TokenHash)
                .IsRequired()
                .HasMaxLength(64);

            cfg.Property(rt => rt.CreatedByIp)
                .IsRequired()
                .HasMaxLength(45); // IPv6 max length

            cfg.Property(rt => rt.RevokedByIp)
                .HasMaxLength(45);

            cfg.Property(rt => rt.ReplacedByTokenHash)
                .HasMaxLength(64);

            cfg.HasIndex(rt => rt.TokenHash);
            cfg.HasIndex(rt => new { rt.UserId, rt.ExpiresAtUtc });
        });

        // Global query filters for tenant isolation
        modelBuilder.Entity<ActivityEvent>()
            .HasQueryFilter(e => e.TenantId == GetCurrentTenantId());

        modelBuilder.Entity<DailyStats>()
            .HasQueryFilter(s => s.TenantId == GetCurrentTenantId());

        modelBuilder.Entity<CategoryStats>()
            .HasQueryFilter(s => s.TenantId == GetCurrentTenantId());
    }
}
