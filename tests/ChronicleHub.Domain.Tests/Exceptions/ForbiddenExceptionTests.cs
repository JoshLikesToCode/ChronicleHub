using ChronicleHub.Domain.Exceptions;
using FluentAssertions;

namespace ChronicleHub.Domain.Tests.Exceptions;

public class ForbiddenExceptionTests
{
    [Fact]
    public void Constructor_WithoutMessage_ShouldUseDefaultMessage()
    {
        // Act
        var exception = new ForbiddenException();

        // Assert
        exception.Message.Should().Be("You do not have permission to access this resource.");
    }

    [Fact]
    public void Constructor_WithMessage_ShouldSetMessage()
    {
        // Arrange
        var message = "Insufficient privileges to delete this resource";

        // Act
        var exception = new ForbiddenException(message);

        // Assert
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void ShouldInheritFromDomainException()
    {
        // Arrange & Act
        var exception = new ForbiddenException();

        // Assert
        exception.Should().BeAssignableTo<DomainException>();
    }
}
