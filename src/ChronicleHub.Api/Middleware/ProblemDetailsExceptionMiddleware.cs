using System.Net;
using System.Text.Json;
using ChronicleHub.Application.ProblemDetails;
using ChronicleHub.Domain.Exceptions;
using FluentValidation;

namespace ChronicleHub.Api.Middleware;

/// <summary>
/// Middleware that catches exceptions and converts them to RFC 9457 Problem Details responses.
/// </summary>
public class ProblemDetailsExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ProblemDetailsExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ProblemDetailsExceptionMiddleware(
        RequestDelegate next,
        ILogger<ProblemDetailsExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var problemDetails = exception switch
        {
            NotFoundException notFoundEx => CreateNotFoundProblem(notFoundEx, context),
            Domain.Exceptions.ValidationException validationEx => CreateValidationProblem(validationEx, context),
            FluentValidation.ValidationException fluentValidationEx => CreateFluentValidationProblem(fluentValidationEx, context),
            ConflictException conflictEx => CreateConflictProblem(conflictEx, context),
            UnauthorizedException unauthorizedEx => CreateUnauthorizedProblem(unauthorizedEx, context),
            ForbiddenException forbiddenEx => CreateForbiddenProblem(forbiddenEx, context),
            _ => CreateInternalServerErrorProblem(exception, context)
        };

        // Log the exception with appropriate level
        LogException(exception, problemDetails.Status);

        // Set response headers
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = problemDetails.Status;

        // Serialize and write response
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null, // Use exact property names from the model
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, options));
    }

    private ProblemDetailsResponse CreateNotFoundProblem(NotFoundException ex, HttpContext context)
    {
        var extensions = new Dictionary<string, object>();

        if (ex.ResourceName != null)
        {
            extensions["resourceName"] = ex.ResourceName;
        }

        if (ex.Key != null)
        {
            extensions["resourceKey"] = ex.Key;
        }

        return ProblemDetailsFactory.NotFound(
            detail: ex.Message,
            instance: context.Request.Path,
            extensions: extensions.Count > 0 ? extensions : null);
    }

    private ProblemDetailsResponse CreateValidationProblem(
        Domain.Exceptions.ValidationException ex,
        HttpContext context)
    {
        return ProblemDetailsFactory.ValidationError(
            errors: ex.Errors,
            detail: ex.Message,
            instance: context.Request.Path);
    }

    private ProblemDetailsResponse CreateFluentValidationProblem(
        FluentValidation.ValidationException ex,
        HttpContext context)
    {
        var errors = ex.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        return ProblemDetailsFactory.ValidationError(
            errors: errors,
            detail: "One or more validation errors occurred.",
            instance: context.Request.Path);
    }

    private ProblemDetailsResponse CreateConflictProblem(ConflictException ex, HttpContext context)
    {
        var extensions = new Dictionary<string, object>();

        if (ex.ResourceName != null)
        {
            extensions["resourceName"] = ex.ResourceName;
        }

        if (ex.Key != null)
        {
            extensions["resourceKey"] = ex.Key;
        }

        if (ex.Reason != null)
        {
            extensions["reason"] = ex.Reason;
        }

        return ProblemDetailsFactory.Conflict(
            detail: ex.Message,
            instance: context.Request.Path,
            extensions: extensions.Count > 0 ? extensions : null);
    }

    private ProblemDetailsResponse CreateUnauthorizedProblem(UnauthorizedException ex, HttpContext context)
    {
        return ProblemDetailsFactory.Unauthorized(
            detail: ex.Message,
            instance: context.Request.Path);
    }

    private ProblemDetailsResponse CreateForbiddenProblem(ForbiddenException ex, HttpContext context)
    {
        return ProblemDetailsFactory.Forbidden(
            detail: ex.Message,
            instance: context.Request.Path);
    }

    private ProblemDetailsResponse CreateInternalServerErrorProblem(Exception ex, HttpContext context)
    {
        // In production, don't expose internal error details
        var detail = _environment.IsDevelopment()
            ? ex.Message
            : "An unexpected error occurred. Please try again later.";

        var extensions = _environment.IsDevelopment()
            ? new Dictionary<string, object>
            {
                ["exceptionType"] = ex.GetType().Name,
                ["stackTrace"] = ex.StackTrace ?? string.Empty
            }
            : null;

        return ProblemDetailsFactory.InternalServerError(
            detail: detail,
            instance: context.Request.Path,
            extensions: extensions);
    }

    private void LogException(Exception exception, int statusCode)
    {
        var logLevel = statusCode switch
        {
            >= 500 => LogLevel.Error,
            >= 400 => LogLevel.Warning,
            _ => LogLevel.Information
        };

        _logger.Log(logLevel, exception, "Request resulted in {StatusCode}: {Message}",
            statusCode, exception.Message);
    }
}
