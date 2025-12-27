namespace ChronicleHub.Domain.Entities;

public sealed class Tenant
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Slug { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? DeactivatedAtUtc { get; private set; }

    private Tenant()
    {
        // Private parameterless constructor for EF Core
        Name = string.Empty;
        Slug = string.Empty;
    }

    public Tenant(Guid id, string name, string slug)
    {
        Id = id;
        Name = name;
        Slug = slug;
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (IsActive)
        {
            IsActive = false;
            DeactivatedAtUtc = DateTime.UtcNow;
        }
    }

    public void Reactivate()
    {
        IsActive = true;
        DeactivatedAtUtc = null;
    }
}
