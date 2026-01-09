using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace Encina.Hangfire.Tests;

public class HangfireNotificationJobAdapterTests
{
    private readonly IEncina _encina;
    private readonly FakeLogger<HangfireNotificationJobAdapter<Fakers.TestNotificationData>> _logger;
    private readonly HangfireNotificationJobAdapter<Fakers.TestNotificationData> _adapter;

    public HangfireNotificationJobAdapterTests()
    {
        _encina = Substitute.For<IEncina>();
        _logger = new FakeLogger<HangfireNotificationJobAdapter<Fakers.TestNotificationData>>();
        _adapter = new HangfireNotificationJobAdapter<Fakers.TestNotificationData>(_encina, _logger);
    }

    [Fact]
    public async Task PublishAsync_PublishesNotification()
    {
        // Arrange
        var notification = new Fakers.TestNotificationFaker().WithMessage("test-message").Generate();

        // Act
        await _adapter.PublishAsync(notification);

        // Assert
        await _encina.Received(1).Publish(
            Arg.Is<Fakers.TestNotificationData>(n => n.Message == "test-message"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_LogsPublishingStart()
    {
        // Arrange
        var notification = new Fakers.TestNotificationFaker().WithMessage("test-message").Generate();

        // Act
        await _adapter.PublishAsync(notification);

        // Assert
        var logEntry = _logger.Collector.GetSnapshot()
            .FirstOrDefault(r => r.Message.Contains("Publishing Hangfire notification"));
        logEntry.ShouldNotBeNull();
        logEntry!.Level.ShouldBe(LogLevel.Information);
    }

    [Fact]
    public async Task PublishAsync_OnSuccess_LogsCompletion()
    {
        // Arrange
        var notification = new Fakers.TestNotificationFaker().WithMessage("test-message").Generate();

        // Act
        await _adapter.PublishAsync(notification);

        // Assert
        var logEntry = _logger.Collector.GetSnapshot()
            .FirstOrDefault(r => r.Message.Contains("completed successfully"));
        logEntry.ShouldNotBeNull();
        logEntry!.Level.ShouldBe(LogLevel.Information);
    }

    [Fact]
    public async Task PublishAsync_WhenExceptionThrown_LogsAndRethrows()
    {
        // Arrange
        var notification = new Fakers.TestNotificationFaker().WithMessage("test-message").Generate();
        var exception = new InvalidOperationException("Test exception");
        _encina.When(m => m.Publish(Arg.Any<Fakers.TestNotificationData>(), Arg.Any<CancellationToken>()))
            .Do(_ => throw exception);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _adapter.PublishAsync(notification));

        var logEntry = _logger.Collector.GetSnapshot()
            .FirstOrDefault(r => r.Message.Contains("Unhandled exception"));
        logEntry.ShouldNotBeNull();
        logEntry!.Level.ShouldBe(LogLevel.Error);
        logEntry.Exception.ShouldBe(exception);
    }

    [Fact]
    public async Task PublishAsync_PassesCancellationToken()
    {
        // Arrange
        var notification = new Fakers.TestNotificationFaker().WithMessage("test-message").Generate();
        using var cts = new CancellationTokenSource();

        // Act
        await _adapter.PublishAsync(notification, cts.Token);

        // Assert
        await _encina.Received(1).Publish(
            Arg.Any<Fakers.TestNotificationData>(),
            Arg.Is<CancellationToken>(ct => ct == cts.Token));
    }
}
