using ChronicleHub.Domain.Exceptions;
using FluentAssertions;

namespace ChronicleHub.Domain.Tests.Exceptions;

public class UnauthorizedExceptionTests
{
    [Fact]
    public void Constructor_WithoutMessage_ShouldUseDefaultMessage()
    {
        // Act
        var exception = new UnauthorizedException();

        // Assert
        exception.Message.Should().Be("Authentication is required to access this resource.");
    }

    [Fact]
    public void Constructor_WithMessage_ShouldSetMessage()
    {
        // Arrange
        var message = "Invalid API key";

        // Act
        var exception = new UnauthorizedException(message);

        // Assert
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void ShouldInheritFromDomainException()
    {
        // Arrange & Act
        var exception = new UnauthorizedException();

        // Assert
        exception.Should().BeAssignableTo<DomainException>();
    }
}
