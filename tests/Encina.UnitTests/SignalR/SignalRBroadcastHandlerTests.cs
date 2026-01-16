using Encina.SignalR;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute.ExceptionExtensions;

namespace Encina.UnitTests.SignalR;

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
    public async Task Handle_ReturnsUnitAndCallsBroadcaster_OnSuccess()
    {
        // Arrange
        var notification = new TestHandlerNotification("Test Message");

        // Act
        var result = await _handler.Handle(notification, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(u => u.ShouldBe(LanguageExt.Unit.Default));
        await _broadcaster.Received(1).BroadcastAsync(notification, CancellationToken.None);
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
    public async Task Handle_WhenBroadcasterThrows_ReturnsSuccessAndLogsError()
    {
        // Arrange - Create test-local mocks with FakeLogger to verify logging behavior
        var broadcaster = Substitute.For<ISignalRNotificationBroadcaster>();
        var fakeLogger = new FakeLogger<SignalRBroadcastHandler<TestHandlerNotification>>();
        var handler = new SignalRBroadcastHandler<TestHandlerNotification>(broadcaster, fakeLogger);
        var notification = new TestHandlerNotification("Test");
        broadcaster.BroadcastAsync(notification, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Broadcast failed"));

        // Act
        var result = await handler.Handle(notification, CancellationToken.None);

        // Assert - handler should swallow exception and return success
        result.IsRight.ShouldBeTrue();

        // Verify error was logged with exception details
        var errorLog = fakeLogger.Collector.GetSnapshot()
            .FirstOrDefault(log => log.Level == LogLevel.Error);
        errorLog.ShouldNotBeNull("Expected an Error-level log entry");
        errorLog.Exception.ShouldBeOfType<InvalidOperationException>();
        errorLog.Exception!.Message.ShouldContain("Broadcast failed");
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
        // Arrange - Create test-local mocks to avoid shared state leakage
        var broadcaster = Substitute.For<ISignalRNotificationBroadcaster>();
        var logger = Substitute.For<ILogger<SignalRBroadcastHandler<TestHandlerNotification>>>();
        var handler = new SignalRBroadcastHandler<TestHandlerNotification>(broadcaster, logger);
        var notification1 = new TestHandlerNotification("Test1");
        var notification2 = new TestHandlerNotification("Test2");

        // Act
        await handler.Handle(notification1, CancellationToken.None);
        await handler.Handle(notification2, CancellationToken.None);

        // Assert
        await broadcaster.Received(2).BroadcastAsync(Arg.Any<TestHandlerNotification>(), Arg.Any<CancellationToken>());
    }

    // Test notification types - Must be public for NSubstitute proxy generation
    public sealed record TestHandlerNotification(string Message) : INotification;

    public sealed record AnotherNotification(int Value) : INotification;
}
