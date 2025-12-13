using System.Text.Json;

namespace ChronicleHub.Api.Contracts.Events;

public sealed record EventResponse(
    Guid Id,
    Guid TenantId,
    Guid UserId,
    string Type,
    string Source,
    DateTime TimestampUtc,
    JsonElement Payload,
    DateTime CreatedAtUtc,
    DateTime ReceivedAtUtc,
    double? ProcessingDurationMs = null
);