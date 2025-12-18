using ChronicleHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChronicleHub.Api.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly ChronicleHubDbContext _dbContext;
    private readonly ILogger<HealthController> _logger;

    public HealthController(ChronicleHubDbContext dbContext, ILogger<HealthController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Liveness probe - indicates if the application is running
    /// </summary>
    [HttpGet("live")]
    public IActionResult GetLiveness()
    {
        return Ok(new
        {
            status = "Healthy",
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Readiness probe - indicates if the application is ready to accept traffic
    /// Checks database connectivity
    /// </summary>
    [HttpGet("ready")]
    public async Task<IActionResult> GetReadiness(CancellationToken cancellationToken)
    {
        try
        {
            // Perform a simple query to verify DB connectivity
            await _dbContext.Database.CanConnectAsync(cancellationToken);

            return Ok(new
            {
                status = "Healthy",
                database = "Connected",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Readiness check failed: database connection error");

            return StatusCode(503, new
            {
                status = "Unhealthy",
                database = "Disconnected",
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }
}
