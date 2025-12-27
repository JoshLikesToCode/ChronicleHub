namespace ChronicleHub.Domain.Entities;

public sealed class RefreshToken
{
    public Guid Id { get; private set; }
    public string UserId { get; private set; }
    public string TokenHash { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }
    public string? RevokedByIp { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }
    public string CreatedByIp { get; private set; }

    private RefreshToken()
    {
        // Private parameterless constructor for EF Core
        UserId = string.Empty;
        TokenHash = string.Empty;
        CreatedByIp = string.Empty;
    }

    public RefreshToken(Guid id, string userId, string tokenHash, DateTime expiresAtUtc, string createdByIp)
    {
        Id = id;
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = DateTime.UtcNow;
        CreatedByIp = createdByIp;
    }

    public void Revoke(string revokedByIp, string? replacedByTokenHash = null)
    {
        if (!RevokedAtUtc.HasValue)
        {
            RevokedAtUtc = DateTime.UtcNow;
            RevokedByIp = revokedByIp;
            ReplacedByTokenHash = replacedByTokenHash;
        }
    }

    public bool IsExpired()
    {
        return DateTime.UtcNow >= ExpiresAtUtc;
    }

    public bool IsActive()
    {
        return !RevokedAtUtc.HasValue && !IsExpired();
    }
}
