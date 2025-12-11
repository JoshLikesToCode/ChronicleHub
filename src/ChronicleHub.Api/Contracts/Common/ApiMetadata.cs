namespace ChronicleHub.Api.Contracts.Common;

/// <summary>
/// Metadata about the API request/response.
/// </summary>
public sealed record ApiMetadata(
    double RequestDurationMs,
    DateTime TimestampUtc
);
