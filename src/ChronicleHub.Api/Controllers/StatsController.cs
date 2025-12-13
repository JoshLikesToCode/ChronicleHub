using ChronicleHub.Api.Contracts.Common;
using ChronicleHub.Api.Contracts.Stats;
using ChronicleHub.Api.Middleware;
using ChronicleHub.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChronicleHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatsController : ControllerBase
{
    private readonly IStatisticsService _statisticsService;
    private readonly ILogger<StatsController> _logger;

    public StatsController(IStatisticsService statisticsService, ILogger<StatsController> logger)
    {
        _statisticsService = statisticsService;
        _logger = logger;
    }

    // GET /api/stats/daily/{date}
    [HttpGet("daily/{date}")]
    [ProducesResponseType(typeof(ApiResponse<DailyStatsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DailyStatsResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDailyStats(
        [FromRoute] DateOnly date,
        CancellationToken ct)
    {
        // TODO: replace with real tenant/user from auth later
        var tenantId = Guid.Empty;
        var userId = Guid.Empty;

        var (dailyStats, categoryStats) = await _statisticsService.GetDailyStatisticsAsync(
            tenantId,
            userId,
            date,
            ct);

        var metadata = new ApiMetadata(
            RequestDurationMs: HttpContext.GetRequestDurationMs(),
            TimestampUtc: DateTime.UtcNow
        );

        if (dailyStats == null)
        {
            var error = new ApiError(
                Code: "STATS_NOT_FOUND",
                Message: $"No statistics found for date '{date}'.",
                Details: null
            );

            var errorResponse = ApiResponse<DailyStatsResponse>.ErrorResult(error, metadata);
            return NotFound(errorResponse);
        }

        var categoryBreakdown = categoryStats
            .Select(cs => new CategoryStatsItem(cs.Category, cs.EventCount))
            .ToList();

        var response = new DailyStatsResponse(
            dailyStats.TenantId,
            dailyStats.UserId,
            dailyStats.Date,
            dailyStats.TotalEvents,
            categoryBreakdown
        );

        var successResponse = ApiResponse<DailyStatsResponse>.SuccessResult(response, metadata);
        return Ok(successResponse);
    }
}
