using Encina.Messaging.DeadLetter;
using Encina.Messaging.Recoverability;
using Encina.Messaging.Scheduling;
using Encina.Messaging.Serialization;
using Encina.Testing.Fakes.Models;
using Encina.Testing.Fakes.Stores;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;
using static LanguageExt.Prelude;
using EfScheduledMessageFactory = Encina.EntityFrameworkCore.Scheduling.ScheduledMessageFactory;

namespace Encina.UnitTests.Messaging.Encryption;

/// <summary>
/// <see cref="EncinaError.Message"/> (and exception messages) can carry personal data such as a
/// data-subject id. The scheduler, delayed retries and the dead-letter queue must keep only the
/// error code in what they log and store in plaintext (#1259 review, #1274). The outbox loop is
/// covered by <c>OutboxBatchProcessorTests</c> and <c>OutboxProcessorBaseTests</c>.
/// </summary>
public sealed class ErrorMessageLeakTests
{
    private const string PersonalData = "patient-123";
    private static readonly EncinaError SensitiveError =
        EncinaErrors.Create("consent.missing", $"No consent recorded for subject {PersonalData}");

    [Fact]
    public async Task Scheduler_DispatchReturnsLeft_StoresAndLogsOnlyTheErrorCode()
    {
        // Arrange
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 9, 23, 10, 0, 0, TimeSpan.Zero));
        var store = new FakeScheduledMessageStore(time);
        var logger = new FakeLogger<SchedulerOrchestrator>();
        var options = new SchedulingOptions();
        var orchestrator = new SchedulerOrchestrator(
            store,
            options,
            logger,
            new EfScheduledMessageFactory(),
            new ExponentialBackoffRetryPolicy(options),
            new JsonMessageSerializer(),
            cronParser: null,
            timeProvider: time);

        var messageId = (await orchestrator.ScheduleAsync(new ReminderRequest("r-1"), TimeSpan.FromMinutes(1)))
            .Match(Right: id => id, Left: e => throw new InvalidOperationException(e.Message));
        time.Advance(TimeSpan.FromMinutes(2));

        // Act
        await orchestrator.ProcessDueMessagesAsync(
            (_, _, _, _) => ValueTask.FromResult(Left<EncinaError, Unit>(SensitiveError)));

        // Assert
        store.GetMessage(messageId)!.ErrorMessage.ShouldBe("consent.missing");
        var logs = logger.Collector.GetSnapshot();
        logs.ShouldContain(r => r.Message.Contains("consent.missing"));
        logs.ShouldAllBe(r => !r.Message.Contains(PersonalData));
    }

    [Fact]
    public async Task DelayedRetryScheduler_ContextWithLastError_StoresOnlyTheErrorCode()
    {
        // Arrange
        IDelayedRetryMessage? stored = null;
        var store = Substitute.For<IDelayedRetryStore>();
        await store.AddAsync(Arg.Do<IDelayedRetryMessage>(m => stored = m), Arg.Any<CancellationToken>());
        var factory = Substitute.For<IDelayedRetryMessageFactory>();
        factory.Create(Arg.Any<DelayedRetryMessageData>()).Returns(ci => ToMessage(ci.Arg<DelayedRetryMessageData>()));
        var scheduler = new DelayedRetryScheduler(store, factory, NullLogger<DelayedRetryScheduler>.Instance, new JsonMessageSerializer());

        var context = new RecoverabilityContext();
        context.RecordFailedAttempt(SensitiveError, exception: null, ErrorClassification.Transient);

        // Act
        await scheduler.ScheduleRetryAsync(new ReminderRequest("r-1"), context, TimeSpan.Zero, delayedRetryAttempt: 0);

        // Assert
        stored.ShouldNotBeNull();
        stored!.ContextContent.ShouldContain("consent.missing");
        stored.ContextContent.ShouldNotContain(PersonalData);
    }

    [Fact]
    public async Task DeadLetter_AddAsync_StoresAndLogsOnlyTheErrorCodeAndExceptionType()
    {
        // Arrange
        var store = new FakeDeadLetterStore();
        var logger = new FakeLogger<DeadLetterOrchestrator>();
        var orchestrator = new DeadLetterOrchestrator(
            store, new PassThroughDeadLetterMessageFactory(), new DeadLetterOptions(), logger, new JsonMessageSerializer());

        // Act
        var added = await orchestrator.AddAsync(
            new ReminderRequest("r-1"),
            new DeadLetterContext(
                SensitiveError,
                new InvalidOperationException($"Handler failed for {PersonalData}"),
                SourcePattern: "Recoverability",
                TotalRetryAttempts: 3,
                FirstFailedAtUtc: new DateTime(2026, 9, 23, 9, 0, 0, DateTimeKind.Utc)));

        // Assert
        var stored = store.GetMessage(added.Match(Right: m => m.Id, Left: e => throw new InvalidOperationException(e.Message)))!;
        stored.ErrorMessage.ShouldBe("consent.missing");
        stored.ExceptionType.ShouldBe(typeof(InvalidOperationException).FullName);
        stored.ExceptionMessage.ShouldBeNull();
        var logs = logger.Collector.GetSnapshot();
        logs.ShouldContain(r => r.Message.Contains("consent.missing"));
        logs.ShouldAllBe(r => !r.Message.Contains(PersonalData));
    }

    [Fact]
    public async Task DeadLetter_AddFromFailedMessageAsync_EncryptsTheRequestAndKeepsOnlyTheErrorCode()
    {
        // Arrange
        var serializer = RealMessageEncryption.CreateSerializer();
        var store = new FakeDeadLetterStore();
        var logger = new FakeLogger<DeadLetterOrchestrator>();
        var orchestrator = new DeadLetterOrchestrator(
            store, new PassThroughDeadLetterMessageFactory(), new DeadLetterOptions(), logger, serializer);

        var context = new RecoverabilityContext();
        context.RecordFailedAttempt(SensitiveError, exception: null, ErrorClassification.Permanent);
        var failedMessage = context.CreateFailedMessage(new ReminderRequest(PersonalData));

        // Act
        var added = await orchestrator.AddFromFailedMessageAsync(failedMessage, "Recoverability");

        // Assert
        var stored = store.GetMessage(added.Match(Right: m => m.Id, Left: e => throw new InvalidOperationException(e.Message)))!;
        stored.RequestContent.ShouldStartWith("ENC:v1:");
        stored.RequestContent.ShouldNotContain(PersonalData);
        serializer.Deserialize<ReminderRequest>(stored.RequestContent)!.SubjectId.ShouldBe(PersonalData);
        stored.ErrorMessage.ShouldBe("consent.missing");
        logger.Collector.GetSnapshot().ShouldAllBe(r => !r.Message.Contains(PersonalData));
    }

    private static IDelayedRetryMessage ToMessage(DelayedRetryMessageData data)
    {
        var message = Substitute.For<IDelayedRetryMessage>();
        message.Id.Returns(data.Id);
        message.RequestContent.Returns(data.RequestContent);
        message.ContextContent.Returns(data.ContextContent);
        return message;
    }

    public sealed record ReminderRequest(string SubjectId) : IRequest<string>;

    private sealed class PassThroughDeadLetterMessageFactory : IDeadLetterMessageFactory
    {
        public IDeadLetterMessage Create(DeadLetterData data) => new FakeDeadLetterMessage
        {
            Id = data.Id,
            RequestType = data.RequestType,
            RequestContent = data.RequestContent,
            ErrorMessage = data.ErrorMessage,
            ExceptionType = data.ExceptionType,
            ExceptionMessage = data.ExceptionMessage,
            ExceptionStackTrace = data.ExceptionStackTrace,
            SourcePattern = data.SourcePattern,
            TotalRetryAttempts = data.TotalRetryAttempts,
            CorrelationId = data.CorrelationId
        };
    }
}
