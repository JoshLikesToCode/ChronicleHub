namespace ChronicleHub.Api.Contracts.Events;

public sealed record PagedEventsResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount
);