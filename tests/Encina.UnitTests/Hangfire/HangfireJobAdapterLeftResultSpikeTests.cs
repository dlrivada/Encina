using Encina.Hangfire;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using static LanguageExt.Prelude;

#pragma warning disable CA2012 // Use ValueTasks correctly - Required for NSubstitute mocking pattern

namespace Encina.UnitTests.Hangfire;

/// <summary>
/// Spike reproduction for finding C3 (verification spike, branch spike/verify-request-context):
/// neither Hangfire job adapter ever throws when the underlying handler reports a domain failure
/// via <c>Either&lt;EncinaError, T&gt;.Left</c>, so Hangfire always records the job as succeeded and
/// never retries it - unlike <c>Encina.Quartz.QuartzRequestJob</c>
/// (src/Encina.Quartz/QuartzRequestJob.cs, ~lines 66-72), which explicitly throws a
/// <c>JobExecutionException</c> on <c>Left</c> to trigger Quartz's retry mechanism.
/// </summary>
public sealed class HangfireJobAdapterLeftResultSpikeTests
{
    /// <summary>
    /// CONFIRMED (C3, request adapter): <see cref="HangfireRequestJobAdapter{TRequest,TResponse}.ExecuteAsync"/>
    /// (src/Encina.Hangfire/HangfireRequestJobAdapter.cs, ~lines 39-64) logs the failure via
    /// <c>Log.RequestJobFailed</c> but then simply <c>return</c>s the <c>Left</c> result as the method's
    /// normal (non-exceptional) return value. Hangfire only ever sees a method that completed without
    /// throwing, so it marks the job Succeeded and never schedules a retry.
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
        // Expected (correct) behavior: a Left result must surface as a thrown exception (mirroring
        // QuartzRequestJob's JobExecutionException) so Hangfire marks the job Failed and retries it.
        // Actual (current, buggy) behavior: ExecuteAsync returns normally with the Left wrapped in its
        // Task<Either<...>> result, so no exception is thrown and Hangfire records success.
        exception.ShouldNotBeNull();
    }

    /// <summary>
    /// CONFIRMED (C3, notification adapter): <see cref="HangfireNotificationJobAdapter{TNotification}.PublishAsync"/>
    /// (src/Encina.Hangfire/HangfireNotificationJobAdapter.cs, ~lines 37-58) does not even inspect the
    /// <see cref="Either{EncinaError, Unit}"/> returned by <c>IEncina.Publish</c> - it is awaited and
    /// discarded outright, so there is no way for this adapter to ever fail a job on a handler-level
    /// <c>Left</c>, regardless of what Hangfire does with the return value.
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
        // Expected (correct) behavior: a Left result from Publish must surface as a thrown exception
        // so Hangfire marks the job Failed and retries it.
        // Actual (current, buggy) behavior: PublishAsync never inspects the Either at all, so it
        // always completes normally and Hangfire always records success.
        exception.ShouldNotBeNull();
    }

    public sealed record SpikeRequest(string Payload) : IRequest<SpikeResponse>;

    public sealed record SpikeResponse(string Result);

    public sealed record SpikeNotification(string Payload) : INotification;
}
