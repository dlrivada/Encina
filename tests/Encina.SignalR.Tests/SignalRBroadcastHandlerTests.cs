using Encina.SignalR;
using LanguageExt;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Encina.SignalR.Tests;

/// <summary>
/// Tests for the <see cref="SignalRBroadcastHandler{TNotification}"/> class.
/// </summary>
public sealed class SignalRBroadcastHandlerTests
{
    private readonly ISignalRNotificationBroadcaster _broadcaster;
    private readonly ILogger<SignalRBroadcastHandler<TestHandlerNotification>> _logger;
    private readonly SignalRBroadcastHandler<TestHandlerNotification> _handler;

    public SignalRBroadcastHandlerTests()
    {
        _broadcaster = Substitute.For<ISignalRNotificationBroadcaster>();
        _logger = Substitute.For<ILogger<SignalRBroadcastHandler<TestHandlerNotification>>>();
        _handler = new SignalRBroadcastHandler<TestHandlerNotification>(_broadcaster, _logger);
    }

    [Fact]
    public void Constructor_CreatesInstance()
    {
        // Assert
        _handler.ShouldNotBeNull();
    }

    [Fact]
    public async Task Handle_CallsBroadcaster()
    {
        // Arrange
        var notification = new TestHandlerNotification("Test Message");

        // Act
        var result = await _handler.Handle(notification, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        await _broadcaster.Received(1).BroadcastAsync(notification, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsUnitOnSuccess()
    {
        // Arrange
        var notification = new TestHandlerNotification("Test");

        // Act
        var result = await _handler.Handle(notification, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(u => u.ShouldBe(Unit.Default));
    }

    [Fact]
    public async Task Handle_PropagatesCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var notification = new TestHandlerNotification("Test");

        // Act
        await _handler.Handle(notification, cts.Token);

        // Assert
        await _broadcaster.Received(1).BroadcastAsync(notification, cts.Token);
    }

    [Fact]
    public async Task Handle_WhenBroadcasterThrows_ReturnsSuccess()
    {
        // Arrange
        var notification = new TestHandlerNotification("Test");
        _broadcaster.BroadcastAsync(notification, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Broadcast failed"));

        // Act
        var result = await _handler.Handle(notification, CancellationToken.None);

        // Assert - handler should not fail the notification even if broadcast fails
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WhenBroadcasterThrows_HandlesGracefully()
    {
        // Arrange
        var notification = new TestHandlerNotification("Test");
        _broadcaster.BroadcastAsync(notification, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Broadcast failed"));

        // Act
        var result = await _handler.Handle(notification, CancellationToken.None);

        // Assert - handler should swallow exception and return success
        result.IsRight.ShouldBeTrue();
        // The exception is logged internally but we verify the handler doesn't fail
    }

    [Fact]
    public async Task Handle_WithDifferentNotificationType_Works()
    {
        // Arrange
        var broadcaster = Substitute.For<ISignalRNotificationBroadcaster>();
        var logger = Substitute.For<ILogger<SignalRBroadcastHandler<AnotherNotification>>>();
        var handler = new SignalRBroadcastHandler<AnotherNotification>(broadcaster, logger);
        var notification = new AnotherNotification(123);

        // Act
        var result = await handler.Handle(notification, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        await broadcaster.Received(1).BroadcastAsync(notification, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CalledMultipleTimes_EachCallBroadcasts()
    {
        // Arrange
        var notification1 = new TestHandlerNotification("Test1");
        var notification2 = new TestHandlerNotification("Test2");

        // Act
        await _handler.Handle(notification1, CancellationToken.None);
        await _handler.Handle(notification2, CancellationToken.None);

        // Assert
        await _broadcaster.Received(2).BroadcastAsync(Arg.Any<TestHandlerNotification>(), Arg.Any<CancellationToken>());
    }

    // Test notification types
    public sealed record TestHandlerNotification(string Message) : INotification;

    public sealed record AnotherNotification(int Value) : INotification;
}
