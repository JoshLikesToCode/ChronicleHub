using ChronicleHub.Application.ProblemDetails;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace ChronicleHub.Application.Tests.ProblemDetails;

public class ProblemDetailsFactoryTests
{
    [Fact]
    public void Create_WithAllParameters_ShouldReturnCorrectProblemDetails()
    {
        // Arrange
        var statusCode = 404;
        var title = "Resource Not Found";
        var detail = "The requested resource was not found";
        var type = "https://example.com/errors/not-found";
        var instance = "/api/events/123";
        var extensions = new Dictionary<string, object> { ["traceId"] = "abc123" };

        // Act
        var result = ProblemDetailsFactory.Create(statusCode, title, detail, type, instance, extensions);

        // Assert
        result.Status.Should().Be(statusCode);
        result.Title.Should().Be(title);
        result.Detail.Should().Be(detail);
        result.Type.Should().Be(type);
        result.Instance.Should().Be(instance);
        result.Extensions.Should().BeEquivalentTo(extensions);
    }

    [Fact]
    public void Create_WithMinimalParameters_ShouldUseDefaults()
    {
        // Arrange
        var statusCode = 500;

        // Act
        var result = ProblemDetailsFactory.Create(statusCode);

        // Assert
        result.Status.Should().Be(statusCode);
        result.Title.Should().Be("Internal Server Error");
        result.Type.Should().Be("https://httpstatuses.io/500");
        result.Detail.Should().BeNull();
        result.Instance.Should().BeNull();
        result.Extensions.Should().BeNull();
    }

    [Fact]
    public void BadRequest_ShouldReturnBadRequestProblemDetails()
    {
        // Arrange
        var detail = "Invalid request data";
        var instance = "/api/events";

        // Act
        var result = ProblemDetailsFactory.BadRequest(detail, instance);

        // Assert
        result.Status.Should().Be(StatusCodes.Status400BadRequest);
        result.Title.Should().Be("Bad Request");
        result.Detail.Should().Be(detail);
        result.Instance.Should().Be(instance);
    }

    [Fact]
    public void Unauthorized_ShouldReturnUnauthorizedProblemDetails()
    {
        // Arrange
        var detail = "Missing API key";
        var instance = "/api/events";

        // Act
        var result = ProblemDetailsFactory.Unauthorized(detail, instance);

        // Assert
        result.Status.Should().Be(StatusCodes.Status401Unauthorized);
        result.Title.Should().Be("Unauthorized");
        result.Detail.Should().Be(detail);
        result.Instance.Should().Be(instance);
    }

    [Fact]
    public void Forbidden_ShouldReturnForbiddenProblemDetails()
    {
        // Arrange
        var detail = "Insufficient permissions";
        var instance = "/api/events";

        // Act
        var result = ProblemDetailsFactory.Forbidden(detail, instance);

        // Assert
        result.Status.Should().Be(StatusCodes.Status403Forbidden);
        result.Title.Should().Be("Forbidden");
        result.Detail.Should().Be(detail);
        result.Instance.Should().Be(instance);
    }

    [Fact]
    public void NotFound_ShouldReturnNotFoundProblemDetails()
    {
        // Arrange
        var detail = "Event not found";
        var instance = "/api/events/123";

        // Act
        var result = ProblemDetailsFactory.NotFound(detail, instance);

        // Assert
        result.Status.Should().Be(StatusCodes.Status404NotFound);
        result.Title.Should().Be("Not Found");
        result.Detail.Should().Be(detail);
        result.Instance.Should().Be(instance);
    }

    [Fact]
    public void Conflict_ShouldReturnConflictProblemDetails()
    {
        // Arrange
        var detail = "Resource already exists";
        var instance = "/api/events";

        // Act
        var result = ProblemDetailsFactory.Conflict(detail, instance);

        // Assert
        result.Status.Should().Be(StatusCodes.Status409Conflict);
        result.Title.Should().Be("Conflict");
        result.Detail.Should().Be(detail);
        result.Instance.Should().Be(instance);
    }

    [Fact]
    public void UnprocessableEntity_ShouldReturnUnprocessableEntityProblemDetails()
    {
        // Arrange
        var detail = "Cannot process entity";
        var instance = "/api/events";

        // Act
        var result = ProblemDetailsFactory.UnprocessableEntity(detail, instance);

        // Assert
        result.Status.Should().Be(StatusCodes.Status422UnprocessableEntity);
        result.Title.Should().Be("Unprocessable Entity");
        result.Detail.Should().Be(detail);
        result.Instance.Should().Be(instance);
    }

    [Fact]
    public void InternalServerError_ShouldReturnInternalServerErrorProblemDetails()
    {
        // Arrange
        var detail = "An unexpected error occurred";
        var instance = "/api/events";

        // Act
        var result = ProblemDetailsFactory.InternalServerError(detail, instance);

        // Assert
        result.Status.Should().Be(StatusCodes.Status500InternalServerError);
        result.Title.Should().Be("Internal Server Error");
        result.Detail.Should().Be(detail);
        result.Instance.Should().Be(instance);
    }

    [Fact]
    public void ValidationError_ShouldReturnValidationProblemDetails()
    {
        // Arrange
        var errors = new Dictionary<string, string[]>
        {
            ["Email"] = new[] { "Email is required", "Email format is invalid" },
            ["Password"] = new[] { "Password is too short" }
        };
        var instance = "/api/users";

        // Act
        var result = ProblemDetailsFactory.ValidationError(errors, instance: instance);

        // Assert
        result.Status.Should().Be(StatusCodes.Status400BadRequest);
        result.Title.Should().Be("Validation Failed");
        result.Detail.Should().Be("One or more validation errors occurred.");
        result.Instance.Should().Be(instance);
        result.Extensions.Should().ContainKey("errors");
        result.Extensions!["errors"].Should().BeEquivalentTo(errors);
    }

    [Fact]
    public void ValidationError_WithCustomDetail_ShouldUseCustomDetail()
    {
        // Arrange
        var errors = new Dictionary<string, string[]>
        {
            ["Name"] = new[] { "Name is required" }
        };
        var detail = "Custom validation message";

        // Act
        var result = ProblemDetailsFactory.ValidationError(errors, detail);

        // Assert
        result.Detail.Should().Be(detail);
    }

    [Theory]
    [InlineData(400, "Bad Request")]
    [InlineData(401, "Unauthorized")]
    [InlineData(403, "Forbidden")]
    [InlineData(404, "Not Found")]
    [InlineData(409, "Conflict")]
    [InlineData(422, "Unprocessable Entity")]
    [InlineData(500, "Internal Server Error")]
    [InlineData(503, "Service Unavailable")]
    public void Create_ShouldGenerateCorrectDefaultTitle(int statusCode, string expectedTitle)
    {
        // Act
        var result = ProblemDetailsFactory.Create(statusCode);

        // Assert
        result.Title.Should().Be(expectedTitle);
    }
}
