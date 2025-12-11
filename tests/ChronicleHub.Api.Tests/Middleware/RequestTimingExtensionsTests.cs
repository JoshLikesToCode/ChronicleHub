using System.Diagnostics;
using ChronicleHub.Api.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace ChronicleHub.Api.Tests.Middleware;

public class RequestTimingExtensionsTests
{
    [Fact]
    public void GetRequestDurationMs_WithStopwatchInContext_ShouldReturnElapsedTime()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var stopwatch = Stopwatch.StartNew();
        Thread.Sleep(10); // Sleep for a bit to ensure some time has elapsed
        context.Items[RequestTimingMiddleware.RequestStartTimeKey] = stopwatch;

        // Act
        var result = context.GetRequestDurationMs();

        // Assert
        result.Should().BeGreaterThan(0);
        result.Should().BeGreaterThanOrEqualTo(10);
    }

    [Fact]
    public void GetRequestDurationMs_WithoutStopwatchInContext_ShouldReturnZero()
    {
        // Arrange
        var context = new DefaultHttpContext();

        // Act
        var result = context.GetRequestDurationMs();

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void GetRequestDurationMs_WithNullValueInContext_ShouldReturnZero()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Items[RequestTimingMiddleware.RequestStartTimeKey] = null;

        // Act
        var result = context.GetRequestDurationMs();

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void GetRequestDurationMs_WithWrongTypeInContext_ShouldReturnZero()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Items[RequestTimingMiddleware.RequestStartTimeKey] = "not a stopwatch";

        // Act
        var result = context.GetRequestDurationMs();

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void GetRequestDurationMs_WithStoppedStopwatch_ShouldReturnElapsedTime()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var stopwatch = Stopwatch.StartNew();
        Thread.Sleep(10);
        stopwatch.Stop();
        context.Items[RequestTimingMiddleware.RequestStartTimeKey] = stopwatch;

        // Act
        var result = context.GetRequestDurationMs();

        // Assert
        result.Should().BeGreaterThan(0);
        result.Should().BeApproximately(stopwatch.Elapsed.TotalMilliseconds, 1.0);
    }

    [Fact]
    public void GetRequestDurationMs_CalledMultipleTimes_ShouldReturnIncreasingValues()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var stopwatch = Stopwatch.StartNew();
        context.Items[RequestTimingMiddleware.RequestStartTimeKey] = stopwatch;

        // Act
        var result1 = context.GetRequestDurationMs();
        Thread.Sleep(10);
        var result2 = context.GetRequestDurationMs();

        // Assert
        result2.Should().BeGreaterThan(result1);
    }

    [Fact]
    public void GetRequestDurationMs_WithNewStopwatch_ShouldReturnNearZero()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var stopwatch = Stopwatch.StartNew();
        context.Items[RequestTimingMiddleware.RequestStartTimeKey] = stopwatch;

        // Act
        var result = context.GetRequestDurationMs();

        // Assert
        result.Should().BeGreaterThanOrEqualTo(0);
        result.Should().BeLessThan(10); // Should be very small for a just-started stopwatch
    }

    [Fact]
    public void GetRequestDurationMs_WithEmptyItems_ShouldReturnZero()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Items.Clear();

        // Act
        var result = context.GetRequestDurationMs();

        // Assert
        result.Should().Be(0);
    }
}
