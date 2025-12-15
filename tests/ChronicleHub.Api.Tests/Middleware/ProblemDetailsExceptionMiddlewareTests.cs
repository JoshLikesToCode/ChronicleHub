using System.Text.Json;
using ChronicleHub.Api.Middleware;
using ChronicleHub.Application.ProblemDetails;
using ChronicleHub.Domain.Exceptions;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace ChronicleHub.Api.Tests.Middleware;

public class ProblemDetailsExceptionMiddlewareTests
{
    private readonly Mock<ILogger<ProblemDetailsExceptionMiddleware>> _mockLogger;
    private readonly Mock<IHostEnvironment> _mockEnvironment;
    private readonly DefaultHttpContext _httpContext;

    public ProblemDetailsExceptionMiddlewareTests()
    {
        _mockLogger = new Mock<ILogger<ProblemDetailsExceptionMiddleware>>();
        _mockEnvironment = new Mock<IHostEnvironment>();
        _httpContext = new DefaultHttpContext();
        _httpContext.Response.Body = new MemoryStream();
    }

    [Fact]
    public async Task InvokeAsync_WhenNoException_ShouldCallNext()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = (context) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new ProblemDetailsExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        nextCalled.Should().BeTrue();
        _httpContext.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_WhenNotFoundException_ShouldReturn404WithProblemDetails()
    {
        // Arrange
        var exception = new NotFoundException("Event", Guid.NewGuid());
        RequestDelegate next = (context) => throw exception;

        var middleware = new ProblemDetailsExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(404);
        _httpContext.Response.ContentType.Should().Be("application/problem+json");

        var problemDetails = await DeserializeResponse();
        problemDetails.Status.Should().Be(404);
        problemDetails.Title.Should().Be("Not Found");
        problemDetails.Detail.Should().Contain(exception.Message);
    }

    [Fact]
    public async Task InvokeAsync_WhenValidationException_ShouldReturn400WithProblemDetails()
    {
        // Arrange
        var errors = new Dictionary<string, string[]>
        {
            ["Email"] = new[] { "Email is required" }
        };
        var exception = new ChronicleHub.Domain.Exceptions.ValidationException("Validation failed", errors);
        RequestDelegate next = (context) => throw exception;

        var middleware = new ProblemDetailsExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(400);
        _httpContext.Response.ContentType.Should().Be("application/problem+json");

        var problemDetails = await DeserializeResponse();
        problemDetails.Status.Should().Be(400);
        problemDetails.Title.Should().Be("Validation Failed");
        problemDetails.Extensions.Should().ContainKey("errors");
    }

    [Fact]
    public async Task InvokeAsync_WhenFluentValidationException_ShouldReturn400WithProblemDetails()
    {
        // Arrange
        var validationFailures = new List<ValidationFailure>
        {
            new ValidationFailure("Email", "Email is required"),
            new ValidationFailure("Email", "Email format is invalid"),
            new ValidationFailure("Password", "Password is too short")
        };
        var exception = new FluentValidation.ValidationException(validationFailures);
        RequestDelegate next = (context) => throw exception;

        var middleware = new ProblemDetailsExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(400);
        _httpContext.Response.ContentType.Should().Be("application/problem+json");

        var problemDetails = await DeserializeResponse();
        problemDetails.Status.Should().Be(400);
        problemDetails.Title.Should().Be("Validation Failed");
        problemDetails.Extensions.Should().ContainKey("errors");

        var errors = JsonSerializer.Deserialize<Dictionary<string, string[]>>(
            problemDetails.Extensions!["errors"].ToString()!);
        errors.Should().ContainKey("Email");
        errors!["Email"].Should().HaveCount(2);
        errors.Should().ContainKey("Password");
    }

    [Fact]
    public async Task InvokeAsync_WhenConflictException_ShouldReturn409WithProblemDetails()
    {
        // Arrange
        var exception = new ConflictException("User", "john@example.com", "Email already registered");
        RequestDelegate next = (context) => throw exception;

        var middleware = new ProblemDetailsExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(409);
        _httpContext.Response.ContentType.Should().Be("application/problem+json");

        var problemDetails = await DeserializeResponse();
        problemDetails.Status.Should().Be(409);
        problemDetails.Title.Should().Be("Conflict");
    }

    [Fact]
    public async Task InvokeAsync_WhenUnauthorizedException_ShouldReturn401WithProblemDetails()
    {
        // Arrange
        var exception = new UnauthorizedException("Invalid API key");
        RequestDelegate next = (context) => throw exception;

        var middleware = new ProblemDetailsExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(401);
        _httpContext.Response.ContentType.Should().Be("application/problem+json");

        var problemDetails = await DeserializeResponse();
        problemDetails.Status.Should().Be(401);
        problemDetails.Title.Should().Be("Unauthorized");
        problemDetails.Detail.Should().Be("Invalid API key");
    }

    [Fact]
    public async Task InvokeAsync_WhenForbiddenException_ShouldReturn403WithProblemDetails()
    {
        // Arrange
        var exception = new ForbiddenException("Insufficient privileges");
        RequestDelegate next = (context) => throw exception;

        var middleware = new ProblemDetailsExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(403);
        _httpContext.Response.ContentType.Should().Be("application/problem+json");

        var problemDetails = await DeserializeResponse();
        problemDetails.Status.Should().Be(403);
        problemDetails.Title.Should().Be("Forbidden");
        problemDetails.Detail.Should().Be("Insufficient privileges");
    }

    [Fact]
    public async Task InvokeAsync_WhenUnhandledException_InDevelopment_ShouldReturn500WithDetails()
    {
        // Arrange
        var exception = new InvalidOperationException("Something went wrong");
        RequestDelegate next = (context) => throw exception;

        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Development");

        var middleware = new ProblemDetailsExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(500);
        _httpContext.Response.ContentType.Should().Be("application/problem+json");

        var problemDetails = await DeserializeResponse();
        problemDetails.Status.Should().Be(500);
        problemDetails.Title.Should().Be("Internal Server Error");
        problemDetails.Detail.Should().Be(exception.Message);
        problemDetails.Extensions.Should().ContainKey("exceptionType");
        problemDetails.Extensions.Should().ContainKey("stackTrace");
    }

    [Fact]
    public async Task InvokeAsync_WhenUnhandledException_InProduction_ShouldReturn500WithoutSensitiveDetails()
    {
        // Arrange
        var exception = new InvalidOperationException("Sensitive internal error");
        RequestDelegate next = (context) => throw exception;

        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");

        var middleware = new ProblemDetailsExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(500);
        _httpContext.Response.ContentType.Should().Be("application/problem+json");

        var problemDetails = await DeserializeResponse();
        problemDetails.Status.Should().Be(500);
        problemDetails.Title.Should().Be("Internal Server Error");
        problemDetails.Detail.Should().NotContain("Sensitive");
        problemDetails.Detail.Should().Be("An unexpected error occurred. Please try again later.");
        problemDetails.Extensions.Should().BeNull();
    }

    [Fact]
    public async Task InvokeAsync_ShouldSetInstanceToRequestPath()
    {
        // Arrange
        var exception = new NotFoundException("Event", Guid.NewGuid());
        RequestDelegate next = (context) => throw exception;

        _httpContext.Request.Path = "/api/events/123";

        var middleware = new ProblemDetailsExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        var problemDetails = await DeserializeResponse();
        problemDetails.Instance.Should().Be("/api/events/123");
    }

    [Theory]
    [InlineData(typeof(NotFoundException), LogLevel.Warning)]
    [InlineData(typeof(ChronicleHub.Domain.Exceptions.ValidationException), LogLevel.Warning)]
    [InlineData(typeof(UnauthorizedException), LogLevel.Warning)]
    [InlineData(typeof(ForbiddenException), LogLevel.Warning)]
    public async Task InvokeAsync_ShouldLogWithCorrectLevel(Type exceptionType, LogLevel expectedLogLevel)
    {
        // Arrange
        var exception = (Exception)Activator.CreateInstance(exceptionType, "Test message")!;
        RequestDelegate next = (context) => throw exception;

        var middleware = new ProblemDetailsExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                expectedLogLevel,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private async Task<ProblemDetailsResponse> DeserializeResponse()
    {
        _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(_httpContext.Response.Body);
        var responseBody = await reader.ReadToEndAsync();

        return JsonSerializer.Deserialize<ProblemDetailsResponse>(responseBody,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }
}
