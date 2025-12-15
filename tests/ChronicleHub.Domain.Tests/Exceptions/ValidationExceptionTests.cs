using ChronicleHub.Domain.Exceptions;
using FluentAssertions;

namespace ChronicleHub.Domain.Tests.Exceptions;

public class ValidationExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_ShouldSetMessageAndEmptyErrors()
    {
        // Arrange
        var message = "Validation failed";

        // Act
        var exception = new ValidationException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithMessageAndErrors_ShouldSetProperties()
    {
        // Arrange
        var message = "Validation failed";
        var errors = new Dictionary<string, string[]>
        {
            ["Email"] = new[] { "Email is required", "Email format is invalid" },
            ["Password"] = new[] { "Password is too short" }
        };

        // Act
        var exception = new ValidationException(message, errors);

        // Assert
        exception.Message.Should().Be(message);
        exception.Errors.Should().BeEquivalentTo(errors);
    }

    [Fact]
    public void Constructor_WithPropertyNameAndError_ShouldSetPropertiesAndMessage()
    {
        // Arrange
        var propertyName = "Username";
        var errorMessage = "Username already exists";

        // Act
        var exception = new ValidationException(propertyName, errorMessage);

        // Assert
        exception.Message.Should().Be($"Validation failed for {propertyName}: {errorMessage}");
        exception.Errors.Should().ContainKey(propertyName);
        exception.Errors[propertyName].Should().BeEquivalentTo(new[] { errorMessage });
    }

    [Fact]
    public void ShouldInheritFromDomainException()
    {
        // Arrange & Act
        var exception = new ValidationException("Test");

        // Assert
        exception.Should().BeAssignableTo<DomainException>();
    }
}
