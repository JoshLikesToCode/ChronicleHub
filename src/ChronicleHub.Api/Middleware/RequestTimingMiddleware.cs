using System.Diagnostics;

namespace ChronicleHub.Api.Middleware;

/// <summary>
/// Middleware that tracks request duration and makes it available via HttpContext.
/// </summary>
public class RequestTimingMiddleware
{
    public const string RequestStartTimeKey = "RequestStartTime";

    private readonly RequestDelegate _next;

    public RequestTimingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        context.Items[RequestStartTimeKey] = stopwatch;

        await _next(context);
    }
}

/// <summary>
/// Extension methods for request timing.
/// </summary>
public static class RequestTimingExtensions
{
    /// <summary>
    /// Gets the request duration in milliseconds from HttpContext.
    /// </summary>
    public static double GetRequestDurationMs(this HttpContext context)
    {
        if (context.Items.TryGetValue(RequestTimingMiddleware.RequestStartTimeKey, out var stopwatchObj)
            && stopwatchObj is Stopwatch stopwatch)
        {
            return stopwatch.Elapsed.TotalMilliseconds;
        }

        return 0;
    }
}
