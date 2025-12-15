using ChronicleHub.Domain.Exceptions;
using FluentAssertions;

namespace ChronicleHub.Domain.Tests.Exceptions;

public class ConflictExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_ShouldSetMessage()
    {
        // Arrange
        var message = "Resource already exists";

        // Act
        var exception = new ConflictException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.ResourceName.Should().BeNull();
        exception.Key.Should().BeNull();
        exception.Reason.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithResourceNameKeyAndReason_ShouldSetProperties()
    {
        // Arrange
        var resourceName = "User";
        var key = "john.doe@example.com";
        var reason = "Email already registered";

        // Act
        var exception = new ConflictException(resourceName, key, reason);

        // Assert
        exception.ResourceName.Should().Be(resourceName);
        exception.Key.Should().Be(key);
        exception.Reason.Should().Be(reason);
        exception.Message.Should().Be($"Conflict with {resourceName} '{key}': {reason}");
    }

    [Fact]
    public void ShouldInheritFromDomainException()
    {
        // Arrange & Act
        var exception = new ConflictException("Test");

        // Assert
        exception.Should().BeAssignableTo<DomainException>();
    }
}
