namespace ChronicleHub.Domain.Entities;

public sealed class UserTenant
{
    public Guid Id { get; private set; }
    public string UserId { get; private set; }
    public Guid TenantId { get; private set; }
    public string Role { get; private set; }
    public DateTime JoinedAtUtc { get; private set; }

    // Navigation properties
    public Tenant Tenant { get; private set; } = null!;

    private UserTenant()
    {
        // Private parameterless constructor for EF Core
        UserId = string.Empty;
        Role = string.Empty;
    }

    public UserTenant(Guid id, string userId, Guid tenantId, string role)
    {
        Id = id;
        UserId = userId;
        TenantId = tenantId;
        Role = role;
        JoinedAtUtc = DateTime.UtcNow;
    }

    public void UpdateRole(string newRole)
    {
        Role = newRole;
    }
}
