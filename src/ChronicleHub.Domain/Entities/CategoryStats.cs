namespace ChronicleHub.Domain.Entities;

public sealed class CategoryStats
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public string Category { get; set; } = default!;

    public int EventCount { get; set; }
}
