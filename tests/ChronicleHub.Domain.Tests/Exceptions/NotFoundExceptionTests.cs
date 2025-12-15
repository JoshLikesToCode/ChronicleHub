using ChronicleHub.Domain.Exceptions;
using FluentAssertions;

namespace ChronicleHub.Domain.Tests.Exceptions;

public class NotFoundExceptionTests
{
    [Fact]
    public void Constructor_WithResourceNameAndKey_ShouldSetPropertiesAndMessage()
    {
        // Arrange
        var resourceName = "User";
        var key = Guid.NewGuid();

        // Act
        var exception = new NotFoundException(resourceName, key);

        // Assert
        exception.ResourceName.Should().Be(resourceName);
        exception.Key.Should().Be(key);
        exception.Message.Should().Be($"{resourceName} with key '{key}' was not found.");
    }

    [Fact]
    public void Constructor_WithMessage_ShouldSetMessage()
    {
        // Arrange
        var message = "Custom not found message";

        // Act
        var exception = new NotFoundException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.ResourceName.Should().BeNull();
        exception.Key.Should().BeNull();
    }

    [Fact]
    public void ShouldInheritFromDomainException()
    {
        // Arrange & Act
        var exception = new NotFoundException("Test", 123);

        // Assert
        exception.Should().BeAssignableTo<DomainException>();
    }
}
