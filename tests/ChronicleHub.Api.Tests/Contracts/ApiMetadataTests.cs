using ChronicleHub.Api.Contracts.Common;
using FluentAssertions;

namespace ChronicleHub.Api.Tests.Contracts;

public class ApiMetadataTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateApiMetadata()
    {
        // Arrange
        var requestDurationMs = 123.45;
        var timestampUtc = new DateTime(2025, 12, 11, 10, 30, 0, DateTimeKind.Utc);

        // Act
        var sut = new ApiMetadata(requestDurationMs, timestampUtc);

        // Assert
        sut.RequestDurationMs.Should().Be(requestDurationMs);
        sut.TimestampUtc.Should().Be(timestampUtc);
    }

    [Fact]
    public void Constructor_WithZeroDuration_ShouldAcceptZero()
    {
        // Arrange
        var requestDurationMs = 0.0;
        var timestampUtc = DateTime.UtcNow;

        // Act
        var sut = new ApiMetadata(requestDurationMs, timestampUtc);

        // Assert
        sut.RequestDurationMs.Should().Be(0.0);
    }

    [Fact]
    public void Constructor_WithNegativeDuration_ShouldAcceptNegativeValue()
    {
        // Arrange
        var requestDurationMs = -5.0;
        var timestampUtc = DateTime.UtcNow;

        // Act
        var sut = new ApiMetadata(requestDurationMs, timestampUtc);

        // Assert
        sut.RequestDurationMs.Should().Be(-5.0);
    }

    [Fact]
    public void Constructor_WithLargeDuration_ShouldHandleLargeValues()
    {
        // Arrange
        var requestDurationMs = 999999.999;
        var timestampUtc = DateTime.UtcNow;

        // Act
        var sut = new ApiMetadata(requestDurationMs, timestampUtc);

        // Assert
        sut.RequestDurationMs.Should().Be(999999.999);
    }

    [Fact]
    public void Constructor_WithSmallDuration_ShouldHandleSmallValues()
    {
        // Arrange
        var requestDurationMs = 0.001;
        var timestampUtc = DateTime.UtcNow;

        // Act
        var sut = new ApiMetadata(requestDurationMs, timestampUtc);

        // Assert
        sut.RequestDurationMs.Should().Be(0.001);
    }

    [Fact]
    public void Record_Equality_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var timestamp = new DateTime(2025, 12, 11, 10, 30, 0, DateTimeKind.Utc);
        var sut1 = new ApiMetadata(100.0, timestamp);
        var sut2 = new ApiMetadata(100.0, timestamp);

        // Act & Assert
        sut1.Should().Be(sut2);
        (sut1 == sut2).Should().BeTrue();
    }

    [Fact]
    public void Record_Equality_WithDifferentDurations_ShouldNotBeEqual()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var sut1 = new ApiMetadata(100.0, timestamp);
        var sut2 = new ApiMetadata(200.0, timestamp);

        // Act & Assert
        sut1.Should().NotBe(sut2);
        (sut1 == sut2).Should().BeFalse();
    }

    [Fact]
    public void Record_Equality_WithDifferentTimestamps_ShouldNotBeEqual()
    {
        // Arrange
        var duration = 100.0;
        var sut1 = new ApiMetadata(duration, DateTime.UtcNow);
        var sut2 = new ApiMetadata(duration, DateTime.UtcNow.AddSeconds(1));

        // Act & Assert
        sut1.Should().NotBe(sut2);
    }

    [Fact]
    public void Record_WithDeconstruction_ShouldDeconstructCorrectly()
    {
        // Arrange
        var expectedDuration = 123.45;
        var expectedTimestamp = new DateTime(2025, 12, 11, 10, 30, 0, DateTimeKind.Utc);
        var sut = new ApiMetadata(expectedDuration, expectedTimestamp);

        // Act
        var (requestDurationMs, timestampUtc) = sut;

        // Assert
        requestDurationMs.Should().Be(expectedDuration);
        timestampUtc.Should().Be(expectedTimestamp);
    }

    [Fact]
    public void Constructor_WithCurrentTimestamp_ShouldStoreCurrentTime()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var sut = new ApiMetadata(50.0, DateTime.UtcNow);

        var afterCreation = DateTime.UtcNow;

        // Assert
        sut.TimestampUtc.Should().BeOnOrAfter(beforeCreation);
        sut.TimestampUtc.Should().BeOnOrBefore(afterCreation);
    }

    [Fact]
    public void Constructor_WithPreciseDuration_ShouldMaintainPrecision()
    {
        // Arrange
        var requestDurationMs = 123.456789;
        var timestampUtc = DateTime.UtcNow;

        // Act
        var sut = new ApiMetadata(requestDurationMs, timestampUtc);

        // Assert
        sut.RequestDurationMs.Should().Be(123.456789);
    }
}
