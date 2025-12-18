namespace ChronicleHub.Api.Middleware;

public class ApiKeyAuthenticationMiddleware
{
    private const string ApiKeyHeaderName = "X-Api-Key";
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApiKeyAuthenticationMiddleware> _logger;

    public ApiKeyAuthenticationMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<ApiKeyAuthenticationMiddleware> logger)
    {
        _next = next;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip authentication for health check and Swagger endpoints
        if (context.Request.Path.StartsWithSegments("/health") ||
            context.Request.Path.StartsWithSegments("/swagger"))
        {
            await _next(context);
            return;
        }

        // Only require API key for write operations (POST, PUT, PATCH, DELETE)
        var httpMethod = context.Request.Method.ToUpperInvariant();
        var isWriteOperation = httpMethod is "POST" or "PUT" or "PATCH" or "DELETE";

        if (!isWriteOperation)
        {
            // Read operations don't require API key
            await _next(context);
            return;
        }

        // Get the expected API key from configuration (environment variable: ApiKey__Key)
        var expectedApiKey = _configuration["ApiKey:Key"];

        if (string.IsNullOrEmpty(expectedApiKey))
        {
            _logger.LogWarning("API Key is not configured. Check appsettings.json or set ApiKey__Key environment variable");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new { error = "API Key configuration is missing" });
            return;
        }

        // Check if the X-Api-Key header is present
        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var providedApiKey))
        {
            _logger.LogWarning("Write operation without API key from {RemoteIp}", context.Connection.RemoteIpAddress);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "API Key is missing. Include X-Api-Key header for write operations." });
            return;
        }

        // Validate the API key
        if (!expectedApiKey.Equals(providedApiKey))
        {
            _logger.LogWarning("Invalid API key attempt from {RemoteIp}", context.Connection.RemoteIpAddress);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid API Key" });
            return;
        }

        // API key is valid, continue to the next middleware
        await _next(context);
    }
}
