using ChronicleHub.Domain.Entities;
using ChronicleHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChronicleHub.Infrastructure.Services;

public class StatisticsService : IStatisticsService
{
    private readonly ChronicleHubDbContext _db;
    private readonly ILogger<StatisticsService> _logger;

    public StatisticsService(ChronicleHubDbContext db, ILogger<StatisticsService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task UpdateStatisticsAsync(ActivityEvent activityEvent, CancellationToken ct = default)
    {
        var date = DateOnly.FromDateTime(activityEvent.TimestampUtc.Date);

        // Update or create DailyStats
        var dailyStats = await _db.DailyStats
            .FirstOrDefaultAsync(
                ds => ds.TenantId == activityEvent.TenantId
                      && ds.UserId == activityEvent.UserId
                      && ds.Date == date,
                ct);

        if (dailyStats == null)
        {
            dailyStats = new DailyStats
            {
                Id = Guid.NewGuid(),
                TenantId = activityEvent.TenantId,
                UserId = activityEvent.UserId,
                Date = date,
                TotalEvents = 0
            };
            _db.DailyStats.Add(dailyStats);
        }

        // Increment total events
        dailyStats.TotalEvents++;

        // Update or create CategoryStats
        var categoryStats = await _db.CategoryStats
            .FirstOrDefaultAsync(
                cs => cs.TenantId == activityEvent.TenantId
                      && cs.UserId == activityEvent.UserId
                      && cs.Category == activityEvent.Type,
                ct);

        if (categoryStats == null)
        {
            categoryStats = new CategoryStats
            {
                Id = Guid.NewGuid(),
                TenantId = activityEvent.TenantId,
                UserId = activityEvent.UserId,
                Category = activityEvent.Type,
                EventCount = 0
            };
            _db.CategoryStats.Add(categoryStats);
        }

        // Increment category count
        categoryStats.EventCount++;

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Updated statistics for tenant {TenantId}, user {UserId}, date {Date}, category {Category}",
            activityEvent.TenantId,
            activityEvent.UserId,
            date,
            activityEvent.Type);
    }

    public async Task<(DailyStats? DailyStats, IReadOnlyList<CategoryStats> CategoryStats)> GetDailyStatisticsAsync(
        Guid tenantId,
        Guid userId,
        DateOnly date,
        CancellationToken ct = default)
    {
        var dailyStats = await _db.DailyStats
            .AsNoTracking()
            .FirstOrDefaultAsync(
                ds => ds.TenantId == tenantId
                      && ds.UserId == userId
                      && ds.Date == date,
                ct);

        // Get all category stats for this user (not filtered by date since CategoryStats is cumulative)
        var categoryStats = await _db.CategoryStats
            .AsNoTracking()
            .Where(cs => cs.TenantId == tenantId && cs.UserId == userId)
            .OrderByDescending(cs => cs.EventCount)
            .ToListAsync(ct);

        return (dailyStats, categoryStats);
    }
}
