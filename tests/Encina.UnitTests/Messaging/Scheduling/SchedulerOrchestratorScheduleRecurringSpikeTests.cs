using Encina.Messaging.Scheduling;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Messaging.Scheduling;

/// <summary>
/// Spike reproduction for finding C4 (verification spike, branch spike/verify-request-context):
/// <see cref="SchedulerOrchestrator.ScheduleRecurringAsync{TRequest}"/>
/// (src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs, ~line 212) discards the
/// <see cref="Either{EncinaError, Unit}"/> returned by <c>_store.AddAsync(message, cancellationToken)</c>
/// and unconditionally returns <c>message.Id</c> as a <c>Right</c>. This is inconsistent with the
/// non-recurring <see cref="SchedulerOrchestrator.ScheduleAsync{TRequest}(TRequest, DateTime, CancellationToken)"/>
/// a few lines above (~lines 127-129), which correctly checks <c>addResult.IsLeft</c> and propagates
/// the store's error.
/// </summary>
public sealed class SchedulerOrchestratorScheduleRecurringSpikeTests
{
    private sealed record TestRequest
    {
        public int Value { get; init; }
    }

    /// <summary>
    /// CONFIRMED (C4): when the store fails to persist the recurring scheduled message,
    /// <see cref="SchedulerOrchestrator.ScheduleRecurringAsync{TRequest}"/> should propagate that
    /// failure as a <c>Left</c>, exactly like <c>ScheduleAsync</c> does for the one-shot case.
    /// </summary>
    [Fact]
    public async Task ScheduleRecurringAsync_WhenStoreAddFails_ShouldReturnLeft()
    {
        // Arrange
        var store = Substitute.For<IScheduledMessageStore>();
        var messageFactory = Substitute.For<IScheduledMessageFactory>();
        var cronParser = Substitute.For<ICronParser>();
        var logger = NullLogger<SchedulerOrchestrator>.Instance;
        var options = new SchedulingOptions();
        var retryPolicy = new ExponentialBackoffRetryPolicy(options);

        var orchestrator = new SchedulerOrchestrator(store, options, logger, messageFactory, retryPolicy, cronParser);

        var nextExecution = DateTime.UtcNow.AddMinutes(1);
        cronParser.GetNextOccurrence(Arg.Any<string>(), Arg.Any<DateTime>())
            .Returns(Right<EncinaError, DateTime>(nextExecution));

        var mockMessage = Substitute.For<IScheduledMessage>();
        mockMessage.Id.Returns(Guid.NewGuid());
        messageFactory.Create(
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<DateTime>(),
            Arg.Any<DateTime>(),
            Arg.Any<bool>(),
            Arg.Any<string?>())
            .Returns(mockMessage);

        var storeError = EncinaErrors.Create("scheduling.store_failure", "Failed to persist recurring message");
        store.AddAsync(Arg.Any<IScheduledMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Left<EncinaError, Unit>(storeError)));

        // Act
        var result = await orchestrator.ScheduleRecurringAsync(new TestRequest { Value = 42 }, "* * * * *");

        // Assert
        // Expected (correct) behavior: the store failure should surface as a Left, mirroring
        // ScheduleAsync's addResult.IsLeft check.
        // Actual (current, buggy) behavior: the Either from _store.AddAsync is discarded, so the
        // orchestrator always returns Right(message.Id), silently pretending the schedule succeeded.
        result.IsLeft.ShouldBeTrue();
    }
}
