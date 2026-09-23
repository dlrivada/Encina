using Encina.Hangfire;
using Encina.UnitTests.Hangfire.Fakers;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Hangfire;

public class HangfireNotificationJobAdapterTests
{
    private readonly IEncina _encina;
    private readonly FakeLogger<HangfireNotificationJobAdapter<TestNotificationData>> _logger;
    private readonly HangfireNotificationJobAdapter<TestNotificationData> _adapter;

    public HangfireNotificationJobAdapterTests()
    {
        _encina = Substitute.For<IEncina>();
        _logger = new FakeLogger<HangfireNotificationJobAdapter<TestNotificationData>>();
        _adapter = new HangfireNotificationJobAdapter<TestNotificationData>(_encina, _logger);

        // Default: publishing succeeds. NSubstitute returns default(ValueTask<Either<...>>) for
        // unconfigured calls, which wraps an uninitialized (Bottom) Either and now throws inside
        // PublishAsync's Match — every test must have a Right result unless it explicitly tests
        // the failure path.
#pragma warning disable CA2012 // Use ValueTasks correctly - required for NSubstitute mocking pattern
        _encina.Publish(Arg.Any<TestNotificationData>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit)));
#pragma warning restore CA2012
    }

    [Fact]
    public async Task PublishAsync_PublishesNotification()
    {
        // Arrange
        var notification = new TestNotificationFaker().WithMessage("test-message").Generate();

        // Act
        await _adapter.PublishAsync(notification);

        // Assert
        await _encina.Received(1).Publish(
            Arg.Is<TestNotificationData>(n => n.Message == "test-message"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_LogsPublishingStart()
    {
        // Arrange
        var notification = new TestNotificationFaker().WithMessage("test-message").Generate();

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
        var notification = new TestNotificationFaker().WithMessage("test-message").Generate();

        // Act
        await _adapter.PublishAsync(notification);

        // Assert
        var logEntry = _logger.Collector.GetSnapshot()
            .FirstOrDefault(r => r.Message.Contains("completed successfully"));
        logEntry.ShouldNotBeNull();
        logEntry!.Level.ShouldBe(LogLevel.Information);
    }

    [Fact]
    public async Task PublishAsync_WithFailedNotification_ThrowsEncinaJobFailedException()
    {
        // Arrange
        var notification = new TestNotificationFaker().WithMessage("test-message").Generate();
        var error = EncinaErrors.Create("test.error", "Handler rejected the notification");
#pragma warning disable CA2012 // Use ValueTasks correctly - required for NSubstitute mocking pattern
        _encina.Publish(Arg.Any<TestNotificationData>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Left<EncinaError, Unit>(error)));
#pragma warning restore CA2012

        // Act
        var exception = await Should.ThrowAsync<EncinaJobFailedException>(() =>
            _adapter.PublishAsync(notification));

        // Assert
        // A Left result from Publish must surface as a thrown exception so Hangfire marks the
        // job Failed and retries it.
        exception.ErrorCode.ShouldBe("test.error");
        exception.Message.ShouldContain("Handler rejected the notification");
    }

    [Fact]
    public async Task PublishAsync_WhenExceptionThrown_LogsAndRethrows()
    {
        // Arrange
        var notification = new TestNotificationFaker().WithMessage("test-message").Generate();
        var exception = new InvalidOperationException("Test exception");
        _encina.When(m => m.Publish(Arg.Any<TestNotificationData>(), Arg.Any<CancellationToken>()))
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
        var notification = new TestNotificationFaker().WithMessage("test-message").Generate();
        using var cts = new CancellationTokenSource();

        // Act
        await _adapter.PublishAsync(notification, cts.Token);

        // Assert
        await _encina.Received(1).Publish(
            Arg.Any<TestNotificationData>(),
            Arg.Is<CancellationToken>(ct => ct == cts.Token));
    }
}
