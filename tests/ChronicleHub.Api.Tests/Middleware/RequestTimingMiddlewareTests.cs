using System.Diagnostics;
using ChronicleHub.Api.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;

namespace ChronicleHub.Api.Tests.Middleware;

public class RequestTimingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldStoreStopwatchInHttpContext()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var sut = new RequestTimingMiddleware(next);
        var context = new DefaultHttpContext();

        // Act
        await sut.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
        context.Items.Should().ContainKey(RequestTimingMiddleware.RequestStartTimeKey);
        context.Items[RequestTimingMiddleware.RequestStartTimeKey].Should().BeOfType<Stopwatch>();
    }

    [Fact]
    public async Task InvokeAsync_ShouldStartStopwatchBeforeCallingNext()
    {
        // Arrange
        Stopwatch? capturedStopwatch = null;
        RequestDelegate next = ctx =>
        {
            if (ctx.Items.TryGetValue(RequestTimingMiddleware.RequestStartTimeKey, out var sw))
            {
                capturedStopwatch = sw as Stopwatch;
            }
            return Task.CompletedTask;
        };

        var sut = new RequestTimingMiddleware(next);
        var context = new DefaultHttpContext();

        // Act
        await sut.InvokeAsync(context);

        // Assert
        capturedStopwatch.Should().NotBeNull();
        capturedStopwatch!.IsRunning.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WhenNextThrows_ShouldPropagateException()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Test exception");
        RequestDelegate next = _ => throw expectedException;

        var sut = new RequestTimingMiddleware(next);
        var context = new DefaultHttpContext();

        // Act
        var action = async () => await sut.InvokeAsync(context);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Test exception");
    }

    [Fact]
    public async Task InvokeAsync_WithDelay_ShouldTrackElapsedTime()
    {
        // Arrange
        var delayMs = 50;
        RequestDelegate next = async _ => await Task.Delay(delayMs);

        var sut = new RequestTimingMiddleware(next);
        var context = new DefaultHttpContext();

        // Act
        await sut.InvokeAsync(context);

        // Assert
        context.Items.Should().ContainKey(RequestTimingMiddleware.RequestStartTimeKey);
        var stopwatch = context.Items[RequestTimingMiddleware.RequestStartTimeKey] as Stopwatch;
        stopwatch.Should().NotBeNull();
        stopwatch!.ElapsedMilliseconds.Should().BeGreaterThanOrEqualTo(delayMs - 10); // Allow 10ms tolerance
    }

    [Fact]
    public async Task InvokeAsync_CalledMultipleTimes_ShouldCreateNewStopwatchEachTime()
    {
        // Arrange
        RequestDelegate next = _ => Task.CompletedTask;
        var sut = new RequestTimingMiddleware(next);

        var context1 = new DefaultHttpContext();
        var context2 = new DefaultHttpContext();

        // Act
        await sut.InvokeAsync(context1);
        await sut.InvokeAsync(context2);

        // Assert
        var stopwatch1 = context1.Items[RequestTimingMiddleware.RequestStartTimeKey] as Stopwatch;
        var stopwatch2 = context2.Items[RequestTimingMiddleware.RequestStartTimeKey] as Stopwatch;

        stopwatch1.Should().NotBeNull();
        stopwatch2.Should().NotBeNull();
        stopwatch1.Should().NotBeSameAs(stopwatch2);
    }
}
