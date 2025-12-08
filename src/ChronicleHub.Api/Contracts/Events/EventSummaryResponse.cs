namespace ChronicleHub.Api.Contracts.Events;

// purposefully omitting the payload here, as it's expensive and rarely needed for lists
public sealed record EventSummaryResponse(
    Guid Id,
    Guid TenantId,
    Guid UserId,
    string Type,
    string Source,
    DateTime TimestampUtc,
    DateTime CreatedAtUtc
);