using Microsoft.AspNetCore.Http;

namespace ChronicleHub.Application.ProblemDetails;

/// <summary>
/// Factory for creating RFC 9457 compliant Problem Details responses.
/// </summary>
public static class ProblemDetailsFactory
{
    private const string DefaultTypePrefix = "https://httpstatuses.io/";

    /// <summary>
    /// Creates a Problem Details response with the specified parameters.
    /// </summary>
    public static ProblemDetailsResponse Create(
        int statusCode,
        string? title = null,
        string? detail = null,
        string? type = null,
        string? instance = null,
        Dictionary<string, object>? extensions = null)
    {
        return new ProblemDetailsResponse
        {
            Status = statusCode,
            Title = title ?? GetDefaultTitle(statusCode),
            Type = type ?? $"{DefaultTypePrefix}{statusCode}",
            Detail = detail,
            Instance = instance,
            Extensions = extensions
        };
    }

    /// <summary>
    /// Creates a Problem Details response for a 400 Bad Request.
    /// </summary>
    public static ProblemDetailsResponse BadRequest(
        string? detail = null,
        string? instance = null,
        Dictionary<string, object>? extensions = null)
    {
        return Create(
            StatusCodes.Status400BadRequest,
            "Bad Request",
            detail,
            instance: instance,
            extensions: extensions);
    }

    /// <summary>
    /// Creates a Problem Details response for a 401 Unauthorized.
    /// </summary>
    public static ProblemDetailsResponse Unauthorized(
        string? detail = null,
        string? instance = null,
        Dictionary<string, object>? extensions = null)
    {
        return Create(
            StatusCodes.Status401Unauthorized,
            "Unauthorized",
            detail,
            instance: instance,
            extensions: extensions);
    }

    /// <summary>
    /// Creates a Problem Details response for a 403 Forbidden.
    /// </summary>
    public static ProblemDetailsResponse Forbidden(
        string? detail = null,
        string? instance = null,
        Dictionary<string, object>? extensions = null)
    {
        return Create(
            StatusCodes.Status403Forbidden,
            "Forbidden",
            detail,
            instance: instance,
            extensions: extensions);
    }

    /// <summary>
    /// Creates a Problem Details response for a 404 Not Found.
    /// </summary>
    public static ProblemDetailsResponse NotFound(
        string? detail = null,
        string? instance = null,
        Dictionary<string, object>? extensions = null)
    {
        return Create(
            StatusCodes.Status404NotFound,
            "Not Found",
            detail,
            instance: instance,
            extensions: extensions);
    }

    /// <summary>
    /// Creates a Problem Details response for a 409 Conflict.
    /// </summary>
    public static ProblemDetailsResponse Conflict(
        string? detail = null,
        string? instance = null,
        Dictionary<string, object>? extensions = null)
    {
        return Create(
            StatusCodes.Status409Conflict,
            "Conflict",
            detail,
            instance: instance,
            extensions: extensions);
    }

    /// <summary>
    /// Creates a Problem Details response for a 422 Unprocessable Entity.
    /// </summary>
    public static ProblemDetailsResponse UnprocessableEntity(
        string? detail = null,
        string? instance = null,
        Dictionary<string, object>? extensions = null)
    {
        return Create(
            StatusCodes.Status422UnprocessableEntity,
            "Unprocessable Entity",
            detail,
            instance: instance,
            extensions: extensions);
    }

    /// <summary>
    /// Creates a Problem Details response for a 500 Internal Server Error.
    /// </summary>
    public static ProblemDetailsResponse InternalServerError(
        string? detail = null,
        string? instance = null,
        Dictionary<string, object>? extensions = null)
    {
        return Create(
            StatusCodes.Status500InternalServerError,
            "Internal Server Error",
            detail,
            instance: instance,
            extensions: extensions);
    }

    /// <summary>
    /// Creates a Problem Details response for validation errors.
    /// </summary>
    public static ProblemDetailsResponse ValidationError(
        Dictionary<string, string[]> errors,
        string? detail = null,
        string? instance = null)
    {
        var extensions = new Dictionary<string, object>
        {
            ["errors"] = errors
        };

        return Create(
            StatusCodes.Status400BadRequest,
            "Validation Failed",
            detail ?? "One or more validation errors occurred.",
            type: $"{DefaultTypePrefix}400",
            instance: instance,
            extensions: extensions);
    }

    private static string GetDefaultTitle(int statusCode) => statusCode switch
    {
        400 => "Bad Request",
        401 => "Unauthorized",
        403 => "Forbidden",
        404 => "Not Found",
        405 => "Method Not Allowed",
        406 => "Not Acceptable",
        409 => "Conflict",
        415 => "Unsupported Media Type",
        422 => "Unprocessable Entity",
        429 => "Too Many Requests",
        500 => "Internal Server Error",
        501 => "Not Implemented",
        502 => "Bad Gateway",
        503 => "Service Unavailable",
        504 => "Gateway Timeout",
        _ => "Error"
    };
}
