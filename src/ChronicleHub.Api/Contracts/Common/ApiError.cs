namespace ChronicleHub.Api.Contracts.Common;

/// <summary>
/// Structured error information for API responses.
/// </summary>
public sealed record ApiError(
    string Code,
    string Message,
    IDictionary<string, string[]>? Details = null
);
