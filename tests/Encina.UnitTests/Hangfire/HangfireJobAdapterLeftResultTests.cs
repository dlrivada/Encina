using Encina.Hangfire;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using static LanguageExt.Prelude;

#pragma warning disable CA2012 // Use ValueTasks correctly - Required for NSubstitute mocking pattern

namespace Encina.UnitTests.Hangfire;

/// <summary>
/// Regression tests for #1152: neither Hangfire job adapter used to throw when the underlying
/// handler reported a domain failure via <c>Either&lt;EncinaError, T&gt;.Left</c>, so Hangfire
/// always recorded the job as succeeded and never retried it - unlike
/// <c>Encina.Quartz.QuartzRequestJob</c> (src/Encina.Quartz/QuartzRequestJob.cs, ~lines 66-72),
/// which explicitly throws a <c>JobExecutionException</c> on <c>Left</c> to trigger Quartz's
/// retry mechanism. Both adapters now throw <see cref="EncinaJobFailedException"/> on <c>Left</c>.
/// </summary>
public sealed class HangfireJobAdapterLeftResultTests
{
    /// <summary>
    /// <see cref="HangfireRequestJobAdapter{TRequest,TResponse}.ExecuteAsync"/> must throw when the
    /// handler returns <c>Left</c>, so Hangfire marks the job Failed and retries it.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WhenHandlerReturnsLeft_ShouldThrow_SoHangfireRetries()
    {
        // Arrange
        var encina = Substitute.For<IEncina>();
        var error = EncinaErrors.Create("test.error", "Handler rejected the request");
        encina.Send(Arg.Any<SpikeRequest>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, SpikeResponse>(error));

        var logger = NullLogger<HangfireRequestJobAdapter<SpikeRequest, SpikeResponse>>.Instance;
        var adapter = new HangfireRequestJobAdapter<SpikeRequest, SpikeResponse>(encina, logger);

        // Act
        var exception = await Record.ExceptionAsync(() => adapter.ExecuteAsync(new SpikeRequest("payload")));

        // Assert
        exception.ShouldNotBeNull();
        exception.ShouldBeOfType<EncinaJobFailedException>();
        ((EncinaJobFailedException)exception).ErrorCode.ShouldBe("test.error");
    }

    /// <summary>
    /// <see cref="HangfireNotificationJobAdapter{TNotification}.PublishAsync"/> must inspect the
    /// <see cref="Either{EncinaError, Unit}"/> returned by <c>IEncina.Publish</c> and throw on
    /// <c>Left</c>, so Hangfire marks the job Failed and retries it.
    /// </summary>
    [Fact]
    public async Task PublishAsync_WhenHandlerReturnsLeft_ShouldThrow_SoHangfireRetries()
    {
        // Arrange
        var encina = Substitute.For<IEncina>();
        var error = EncinaErrors.Create("test.error", "Handler rejected the notification");
        encina.Publish(Arg.Any<SpikeNotification>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Left<EncinaError, Unit>(error)));

        var logger = NullLogger<HangfireNotificationJobAdapter<SpikeNotification>>.Instance;
        var adapter = new HangfireNotificationJobAdapter<SpikeNotification>(encina, logger);

        // Act
        var exception = await Record.ExceptionAsync(() => adapter.PublishAsync(new SpikeNotification("payload")));

        // Assert
        exception.ShouldNotBeNull();
        exception.ShouldBeOfType<EncinaJobFailedException>();
        ((EncinaJobFailedException)exception).ErrorCode.ShouldBe("test.error");
    }

    public sealed record SpikeRequest(string Payload) : IRequest<SpikeResponse>;

    public sealed record SpikeResponse(string Result);

    public sealed record SpikeNotification(string Payload) : INotification;
}
