using System.Text.Json;
using ChronicleHub.Api.Contracts.Events;
using ChronicleHub.Api.Validators;
using FluentAssertions;
using Xunit;

namespace ChronicleHub.Api.Tests.Validators;

public class CreateEventRequestValidatorTests
{
    private readonly CreateEventRequestValidator _validator = new();

    [Fact]
    public async Task Validate_ValidRequest_ShouldPass()
    {
        // Arrange
        var request = new CreateEventRequest(
            Type: "UserLogin",
            Source: "WebApp",
            TimestampUtc: DateTime.UtcNow,
            Payload: JsonDocument.Parse("{\"userId\":\"123\"}").RootElement
        );

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_EmptyType_ShouldFail()
    {
        // Arrange
        var request = new CreateEventRequest(
            Type: "",
            Source: "WebApp",
            TimestampUtc: DateTime.UtcNow,
            Payload: JsonDocument.Parse("{\"userId\":\"123\"}").RootElement
        );

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Type");
        result.Errors.First(e => e.PropertyName == "Type").ErrorMessage
            .Should().Be("Type is required and cannot be empty.");
    }

    [Fact]
    public async Task Validate_EmptySource_ShouldFail()
    {
        // Arrange
        var request = new CreateEventRequest(
            Type: "UserLogin",
            Source: "",
            TimestampUtc: DateTime.UtcNow,
            Payload: JsonDocument.Parse("{\"userId\":\"123\"}").RootElement
        );

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Source");
        result.Errors.First(e => e.PropertyName == "Source").ErrorMessage
            .Should().Be("Source is required and cannot be empty.");
    }

    [Fact]
    public async Task Validate_FutureDateMoreThanOneDay_ShouldFail()
    {
        // Arrange
        var request = new CreateEventRequest(
            Type: "UserLogin",
            Source: "WebApp",
            TimestampUtc: DateTime.UtcNow.AddDays(2),
            Payload: JsonDocument.Parse("{\"userId\":\"123\"}").RootElement
        );

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "TimestampUtc");
        result.Errors.First(e => e.PropertyName == "TimestampUtc").ErrorMessage
            .Should().Be("TimestampUtc cannot be more than 1 day in the future.");
    }

    [Fact]
    public async Task Validate_NullPayload_ShouldFail()
    {
        // Arrange
        var request = new CreateEventRequest(
            Type: "UserLogin",
            Source: "WebApp",
            TimestampUtc: DateTime.UtcNow,
            Payload: JsonDocument.Parse("null").RootElement
        );

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Payload");
        result.Errors.First(e => e.PropertyName == "Payload").ErrorMessage
            .Should().Be("Payload cannot be null or undefined.");
    }

    [Fact]
    public async Task Validate_PastTimestamp_ShouldPass()
    {
        // Arrange
        var request = new CreateEventRequest(
            Type: "UserLogin",
            Source: "WebApp",
            TimestampUtc: DateTime.UtcNow.AddDays(-30),
            Payload: JsonDocument.Parse("{\"userId\":\"123\"}").RootElement
        );

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_FutureDateWithinOneDay_ShouldPass()
    {
        // Arrange
        var request = new CreateEventRequest(
            Type: "UserLogin",
            Source: "WebApp",
            TimestampUtc: DateTime.UtcNow.AddHours(12),
            Payload: JsonDocument.Parse("{\"userId\":\"123\"}").RootElement
        );

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
