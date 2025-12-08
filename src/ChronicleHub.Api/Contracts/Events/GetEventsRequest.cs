namespace ChronicleHub.Api.Contracts.Events;

public sealed class GetEventsRequest
{
    public string? Type { get; init; }
    public string? Source { get; init; }
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;

    public string? SortBy { get; init; } = "CreatedAtUtc";
    public string? SortDirection { get; init; } = "desc"; // asc | desc
}