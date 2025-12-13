using ChronicleHub.Domain.Entities;

namespace ChronicleHub.Infrastructure.Services;

public interface IStatisticsService
{
    /// <summary>
    /// Updates statistics for the given event.
    /// Creates or updates DailyStats and CategoryStats records.
    /// </summary>
    Task UpdateStatisticsAsync(ActivityEvent activityEvent, CancellationToken ct = default);

    /// <summary>
    /// Gets daily statistics for a specific tenant, user, and date.
    /// </summary>
    Task<(DailyStats? DailyStats, IReadOnlyList<CategoryStats> CategoryStats)> GetDailyStatisticsAsync(
        Guid tenantId,
        Guid userId,
        DateOnly date,
        CancellationToken ct = default);
}
