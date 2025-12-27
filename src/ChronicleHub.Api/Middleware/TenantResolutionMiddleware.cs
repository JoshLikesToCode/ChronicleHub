using ChronicleHub.Infrastructure.Persistence;

namespace ChronicleHub.Api.Middleware;

public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;

    public TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ChronicleHubDbContext dbContext)
    {
        // Skip for anonymous endpoints
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            await _next(context);
            return;
        }

        // Extract tenant ID from claims (JWT or API key)
        var tenantIdClaim = context.User.FindFirst("tid")?.Value;
        if (!string.IsNullOrWhiteSpace(tenantIdClaim) && Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            dbContext.SetCurrentTenant(tenantId);
            context.Items["TenantId"] = tenantId;

            _logger.LogDebug("Resolved tenant {TenantId} for request", tenantId);
        }

        try
        {
            await _next(context);
        }
        finally
        {
            dbContext.ClearCurrentTenant();
        }
    }
}
