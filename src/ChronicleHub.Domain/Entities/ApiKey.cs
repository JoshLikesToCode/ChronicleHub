namespace ChronicleHub.Domain.Entities;

public sealed class ApiKey
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Name { get; private set; }
    public string KeyHash { get; private set; }
    public string KeyPrefix { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ExpiresAtUtc { get; private set; }
    public DateTime? LastUsedAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }

    // Navigation properties
    public Tenant Tenant { get; private set; } = null!;

    private ApiKey()
    {
        // Private parameterless constructor for EF Core
        Name = string.Empty;
        KeyHash = string.Empty;
        KeyPrefix = string.Empty;
    }

    public ApiKey(Guid id, Guid tenantId, string name, string keyHash, string keyPrefix, DateTime? expiresAtUtc = null)
    {
        Id = id;
        TenantId = tenantId;
        Name = name;
        KeyHash = keyHash;
        KeyPrefix = keyPrefix;
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
        ExpiresAtUtc = expiresAtUtc;
    }

    public void RecordUsage()
    {
        LastUsedAtUtc = DateTime.UtcNow;
    }

    public void Revoke()
    {
        if (IsActive)
        {
            IsActive = false;
            RevokedAtUtc = DateTime.UtcNow;
        }
    }

    public bool IsExpired()
    {
        return ExpiresAtUtc.HasValue && ExpiresAtUtc.Value < DateTime.UtcNow;
    }
}
