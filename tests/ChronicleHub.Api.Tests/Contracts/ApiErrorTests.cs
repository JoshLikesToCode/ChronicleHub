using ChronicleHub.Api.Contracts.Common;
using FluentAssertions;

namespace ChronicleHub.Api.Tests.Contracts;

public class ApiErrorTests
{
    [Fact]
    public void Constructor_WithCodeAndMessage_ShouldCreateApiError()
    {
        // Arrange
        var code = "ERROR_CODE";
        var message = "Error message";

        // Act
        var sut = new ApiError(code, message);

        // Assert
        sut.Code.Should().Be(code);
        sut.Message.Should().Be(message);
        sut.Details.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithCodeMessageAndDetails_ShouldCreateApiErrorWithDetails()
    {
        // Arrange
        var code = "VALIDATION_ERROR";
        var message = "Validation failed";
        var details = new Dictionary<string, string[]>
        {
            { "Email", new[] { "Email is required", "Email must be valid" } },
            { "Password", new[] { "Password is too short" } }
        };

        // Act
        var sut = new ApiError(code, message, details);

        // Assert
        sut.Code.Should().Be(code);
        sut.Message.Should().Be(message);
        sut.Details.Should().NotBeNull();
        sut.Details.Should().BeEquivalentTo(details);
    }

    [Fact]
    public void Constructor_WithEmptyDetails_ShouldAcceptEmptyDictionary()
    {
        // Arrange
        var details = new Dictionary<string, string[]>();

        // Act
        var sut = new ApiError("CODE", "Message", details);

        // Assert
        sut.Details.Should().NotBeNull();
        sut.Details.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithNullDetails_ShouldAcceptNullDetails()
    {
        // Arrange & Act
        var sut = new ApiError("CODE", "Message", null);

        // Assert
        sut.Details.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyOrWhiteSpaceCode_ShouldStillCreateError(string code)
    {
        // Arrange & Act
        var sut = new ApiError(code, "Message");

        // Assert
        sut.Code.Should().Be(code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyOrWhiteSpaceMessage_ShouldStillCreateError(string message)
    {
        // Arrange & Act
        var sut = new ApiError("CODE", message);

        // Assert
        sut.Message.Should().Be(message);
    }

    [Fact]
    public void Constructor_WithMultipleErrorsPerField_ShouldStoreAllErrors()
    {
        // Arrange
        var details = new Dictionary<string, string[]>
        {
            { "Field1", new[] { "Error 1", "Error 2", "Error 3" } },
            { "Field2", new[] { "Error A" } },
            { "Field3", new[] { "Error X", "Error Y" } }
        };

        // Act
        var sut = new ApiError("MULTI_ERROR", "Multiple validation errors", details);

        // Assert
        sut.Details.Should().HaveCount(3);
        sut.Details!["Field1"].Should().HaveCount(3);
        sut.Details["Field2"].Should().HaveCount(1);
        sut.Details["Field3"].Should().HaveCount(2);
    }

    [Fact]
    public void Record_Equality_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var sut1 = new ApiError("CODE", "Message");
        var sut2 = new ApiError("CODE", "Message");

        // Act & Assert
        sut1.Should().Be(sut2);
        (sut1 == sut2).Should().BeTrue();
    }

    [Fact]
    public void Record_Equality_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var sut1 = new ApiError("CODE_1", "Message 1");
        var sut2 = new ApiError("CODE_2", "Message 2");

        // Act & Assert
        sut1.Should().NotBe(sut2);
        (sut1 == sut2).Should().BeFalse();
    }

    [Fact]
    public void Record_WithDeconstruction_ShouldDeconstructCorrectly()
    {
        // Arrange
        var details = new Dictionary<string, string[]> { { "key", new[] { "value" } } };
        var sut = new ApiError("CODE", "Message", details);

        // Act
        var (code, message, deconstructedDetails) = sut;

        // Assert
        code.Should().Be("CODE");
        message.Should().Be("Message");
        deconstructedDetails.Should().BeEquivalentTo(details);
    }
}
