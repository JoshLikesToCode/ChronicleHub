using System.Text.Json;

namespace ChronicleHub.Api.Contracts.Events;

public sealed record CreateEventRequest(
    string Type,
    string Source,
    DateTime TimestampUtc,
    JsonElement Payload
);