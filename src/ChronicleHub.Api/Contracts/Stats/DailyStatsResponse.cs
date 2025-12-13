namespace ChronicleHub.Api.Contracts.Stats;

public sealed record DailyStatsResponse(
    Guid TenantId,
    Guid UserId,
    DateOnly Date,
    int TotalEvents,
    IReadOnlyList<CategoryStatsItem> CategoryBreakdown
);

public sealed record CategoryStatsItem(
    string Category,
    int EventCount
);
