using Encina.Hangfire;
using Microsoft.Extensions.Logging;

namespace Encina.Hangfire.Tests;

public class HangfireNotificationJobAdapterTests
{
    private readonly IMediator _mediator;
    private readonly ILogger<HangfireNotificationJobAdapter<TestNotification>> _logger;
    private readonly HangfireNotificationJobAdapter<TestNotification> _adapter;

    public HangfireNotificationJobAdapterTests()
    {
        _mediator = Substitute.For<IMediator>();
        _logger = Substitute.For<ILogger<HangfireNotificationJobAdapter<TestNotification>>>();
        _adapter = new HangfireNotificationJobAdapter<TestNotification>(_mediator, _logger);
    }

    [Fact]
    public async Task PublishAsync_PublishesNotification()
    {
        // Arrange
        var notification = new TestNotification("test-message");

        // Act
        await _adapter.PublishAsync(notification);

        // Assert
        await _mediator.Received(1).Publish(
            Arg.Is<TestNotification>(n => n.Message == "test-message"),
            Arg.Any<CancellationToken>());
    }

    [Fact(Skip = "Issue #6: LoggerMessage delegates incompatible with NSubstitute.Received()")]
    public async Task PublishAsync_LogsPublishingStart()
    {
        // Arrange
        var notification = new TestNotification("test-message");

        // Act
        await _adapter.PublishAsync(notification);

        // Assert
        _logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Publishing Hangfire notification")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact(Skip = "Issue #6: LoggerMessage delegates incompatible with NSubstitute.Received()")]
    public async Task PublishAsync_OnSuccess_LogsCompletion()
    {
        // Arrange
        var notification = new TestNotification("test-message");

        // Act
        await _adapter.PublishAsync(notification);

        // Assert
        _logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("completed successfully")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact(Skip = "Issue #6: LoggerMessage delegates incompatible with NSubstitute.Received()")]
    public async Task PublishAsync_WhenExceptionThrown_LogsAndRethrows()
    {
        // Arrange
        var notification = new TestNotification("test-message");
        var exception = new InvalidOperationException("Test exception");
        _mediator.When(m => m.Publish(Arg.Any<TestNotification>(), Arg.Any<CancellationToken>()))
            .Do(_ => throw exception);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _adapter.PublishAsync(notification));

        _logger.Received().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Unhandled exception")),
            exception,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task PublishAsync_PassesCancellationToken()
    {
        // Arrange
        var notification = new TestNotification("test-message");
        var cts = new CancellationTokenSource();

        // Act
        await _adapter.PublishAsync(notification, cts.Token);

        // Assert
        await _mediator.Received(1).Publish(
            Arg.Any<TestNotification>(),
            Arg.Is<CancellationToken>(ct => ct == cts.Token));
    }

    // Test type
    public record TestNotification(string Message) : INotification;
}
