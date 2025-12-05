namespace ChronicleHub.Domain;

public sealed class ActivityEvent
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }

    public string Type { get; private set; } 
    public string Source { get; private set; }
    public DateTime TimestampUtc { get; private set; }
    public string PayloadJson { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private ActivityEvent() { }

    public ActivityEvent(
        Guid id,
        Guid tenantId,
        Guid userId,
        string type,
        string source,
        DateTime timestampUtc,
        string payloadJson
    )
    {
        Id = id;
        TenantId = tenantId;
        UserId = userId;
        Type = type;
        Source = source;
        TimestampUtc = timestampUtc;
        PayloadJson = payloadJson;
        CreatedAtUtc = DateTime.UtcNow;
    }
}
