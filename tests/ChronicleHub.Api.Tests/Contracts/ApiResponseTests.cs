using ChronicleHub.Api.Contracts.Common;
using FluentAssertions;

namespace ChronicleHub.Api.Tests.Contracts;

public class ApiResponseTests
{
    [Fact]
    public void SuccessResult_WithData_ShouldCreateSuccessResponse()
    {
        // Arrange
        var data = "test data";

        // Act
        var sut = ApiResponse<string>.SuccessResult(data);

        // Assert
        sut.Success.Should().BeTrue();
        sut.Data.Should().Be(data);
        sut.Error.Should().BeNull();
        sut.Metadata.Should().BeNull();
    }

    [Fact]
    public void SuccessResult_WithDataAndMetadata_ShouldCreateSuccessResponseWithMetadata()
    {
        // Arrange
        var data = 42;
        var metadata = new ApiMetadata(
            RequestDurationMs: 123.45,
            TimestampUtc: DateTime.UtcNow
        );

        // Act
        var sut = ApiResponse<int>.SuccessResult(data, metadata);

        // Assert
        sut.Success.Should().BeTrue();
        sut.Data.Should().Be(data);
        sut.Error.Should().BeNull();
        sut.Metadata.Should().Be(metadata);
    }

    [Fact]
    public void SuccessResult_WithNullData_ShouldAcceptNullData()
    {
        // Arrange
        string? data = null;

        // Act
        var sut = ApiResponse<string?>.SuccessResult(data);

        // Assert
        sut.Success.Should().BeTrue();
        sut.Data.Should().BeNull();
        sut.Error.Should().BeNull();
    }

    [Fact]
    public void ErrorResult_WithError_ShouldCreateErrorResponse()
    {
        // Arrange
        var error = new ApiError(
            Code: "TEST_ERROR",
            Message: "This is a test error"
        );

        // Act
        var sut = ApiResponse<string>.ErrorResult(error);

        // Assert
        sut.Success.Should().BeFalse();
        sut.Data.Should().BeNull();
        sut.Error.Should().Be(error);
        sut.Metadata.Should().BeNull();
    }

    [Fact]
    public void ErrorResult_WithErrorAndMetadata_ShouldCreateErrorResponseWithMetadata()
    {
        // Arrange
        var error = new ApiError(
            Code: "NOT_FOUND",
            Message: "Resource not found"
        );
        var metadata = new ApiMetadata(
            RequestDurationMs: 5.67,
            TimestampUtc: DateTime.UtcNow
        );

        // Act
        var sut = ApiResponse<object>.ErrorResult(error, metadata);

        // Assert
        sut.Success.Should().BeFalse();
        sut.Data.Should().BeNull();
        sut.Error.Should().Be(error);
        sut.Metadata.Should().Be(metadata);
    }

    [Fact]
    public void ErrorResult_WithErrorDetails_ShouldStoreDetailsCorrectly()
    {
        // Arrange
        var errorDetails = new Dictionary<string, string[]>
        {
            { "field1", new[] { "Error 1", "Error 2" } },
            { "field2", new[] { "Error 3" } }
        };
        var error = new ApiError(
            Code: "VALIDATION_ERROR",
            Message: "Validation failed",
            Details: errorDetails
        );

        // Act
        var sut = ApiResponse<object>.ErrorResult(error);

        // Assert
        sut.Error.Should().NotBeNull();
        sut.Error!.Details.Should().BeEquivalentTo(errorDetails);
    }

    [Fact]
    public void ApiResponse_WithComplexDataType_ShouldHandleComplexTypes()
    {
        // Arrange
        var complexData = new
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            Items = new[] { 1, 2, 3 }
        };

        // Act
        var sut = ApiResponse<object>.SuccessResult(complexData);

        // Assert
        sut.Success.Should().BeTrue();
        sut.Data.Should().BeEquivalentTo(complexData);
    }

    [Fact]
    public void SuccessResult_CreatedMultipleTimes_ShouldCreateIndependentInstances()
    {
        // Arrange
        var data1 = "first";
        var data2 = "second";

        // Act
        var sut1 = ApiResponse<string>.SuccessResult(data1);
        var sut2 = ApiResponse<string>.SuccessResult(data2);

        // Assert
        sut1.Data.Should().Be(data1);
        sut2.Data.Should().Be(data2);
        sut1.Should().NotBeSameAs(sut2);
    }

    [Fact]
    public void ErrorResult_CreatedMultipleTimes_ShouldCreateIndependentInstances()
    {
        // Arrange
        var error1 = new ApiError("ERROR_1", "First error");
        var error2 = new ApiError("ERROR_2", "Second error");

        // Act
        var sut1 = ApiResponse<object>.ErrorResult(error1);
        var sut2 = ApiResponse<object>.ErrorResult(error2);

        // Assert
        sut1.Error.Should().Be(error1);
        sut2.Error.Should().Be(error2);
        sut1.Should().NotBeSameAs(sut2);
    }
}
