using Encina.Messaging.DeadLetter;
using Encina.Messaging.Recoverability;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;
using Encina.Messaging.Serialization;
using Encina.Testing.Fakes.Models;
using Encina.Testing.Fakes.Stores;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using static LanguageExt.Prelude;
using EfSagaStateFactory = Encina.EntityFrameworkCore.Sagas.SagaStateFactory;
using EfScheduledMessageFactory = Encina.EntityFrameworkCore.Scheduling.ScheduledMessageFactory;

namespace Encina.UnitTests.Messaging.Encryption;

/// <summary>
/// Encrypt-on-write / decrypt-on-read coverage for every store whose payload goes through
/// <see cref="IMessageSerializer"/> (#1168, #1259 review): saga data, scheduled messages,
/// dead letters and delayed retries, including the replay/requeue paths that read the payload
/// back. Uses the real <c>EncryptingMessageSerializer</c> (AES-256-GCM) over an in-memory key
/// provider.
/// </summary>
public sealed class StorePayloadEncryptionTests
{
    private const string Diagnosis = "F41.1 generalized anxiety disorder";

    #region Sagas

    [Fact]
    public async Task Saga_StartAdvanceGet_PersistsCiphertextAndReadsBackPlaintext()
    {
        // Arrange
        var serializer = RealMessageEncryption.CreateSerializer();
        var store = new FakeSagaStore();
        var orchestrator = new SagaOrchestrator(
            store,
            new SagaOptions(),
            NullLogger<SagaOrchestrator>.Instance,
            new EfSagaStateFactory(),
            serializer);

        // Act: start writes the data
        var sagaId = (await orchestrator.StartAsync("TherapySaga", new TherapySagaData { Diagnosis = Diagnosis }))
            .Match(Right: id => id, Left: e => throw new InvalidOperationException(e.Message));

        // Assert: the persisted column holds the encrypted envelope
        var started = store.GetSaga(sagaId);
        started.ShouldNotBeNull();
        started!.Data.ShouldStartWith("ENC:v1:");
        started.Data.ShouldNotContain("F41.1");

        // Act: advance reads (decrypts) and rewrites (re-encrypts) the data
        var advance = await orchestrator.AdvanceAsync<TherapySagaData>(
            sagaId,
            data => data with { Sessions = data.Sessions + 1 });

        // Assert
        var advanced = advance.Match(Right: r => r, Left: e => throw new InvalidOperationException(e.Message));
        advanced.Data.Diagnosis.ShouldBe(Diagnosis);
        advanced.Data.Sessions.ShouldBe(1);

        var updated = store.GetSaga(sagaId)!;
        updated.Data.ShouldStartWith("ENC:v1:");
        updated.Data.ShouldNotContain("F41.1");

        var snapshot = await orchestrator.GetAsync<TherapySagaData>(sagaId);
        snapshot.IsSome.ShouldBeTrue();
        snapshot.Match(Some: s => s.Data, None: () => null!).ShouldBe(new TherapySagaData { Diagnosis = Diagnosis, Sessions = 1 });
    }

    #endregion

    #region Scheduling

    [Fact]
    public async Task Scheduler_ScheduleThenProcessDue_PersistsCiphertextAndDispatchesPlaintext()
    {
        // Arrange
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 9, 23, 10, 0, 0, TimeSpan.Zero));
        var serializer = RealMessageEncryption.CreateSerializer();
        var store = new FakeScheduledMessageStore(time);
        var options = new SchedulingOptions();
        var orchestrator = new SchedulerOrchestrator(
            store,
            options,
            NullLogger<SchedulerOrchestrator>.Instance,
            new EfScheduledMessageFactory(),
            new ExponentialBackoffRetryPolicy(options),
            serializer,
            cronParser: null,
            timeProvider: time);

        // Act: schedule writes the request
        var messageId = (await orchestrator.ScheduleAsync(new SessionReminderRequest(Diagnosis), TimeSpan.FromMinutes(5)))
            .Match(Right: id => id, Left: e => throw new InvalidOperationException(e.Message));

        // Assert: stored encrypted
        var stored = store.GetMessage(messageId);
        stored.ShouldNotBeNull();
        stored!.Content.ShouldStartWith("ENC:v1:");
        stored.Content.ShouldNotContain("F41.1");

        // Act: once due, the processor reads the request back through the same serializer
        time.Advance(TimeSpan.FromMinutes(10));
        object? dispatched = null;
        var processed = await orchestrator.ProcessDueMessagesAsync((_, _, request, _) =>
        {
            dispatched = request;
            return ValueTask.FromResult(Right<EncinaError, Unit>(unit));
        });

        // Assert
        processed.Match(Right: count => count, Left: e => throw new InvalidOperationException(e.Message)).ShouldBe(1);
        dispatched.ShouldBeOfType<SessionReminderRequest>().Diagnosis.ShouldBe(Diagnosis);
    }

    #endregion

    #region Dead letter

    [Fact]
    public async Task DeadLetter_AddThenReplay_PersistsCiphertextAndReplaysPlaintext()
    {
        // Arrange
        var serializer = RealMessageEncryption.CreateSerializer();
        var store = new FakeDeadLetterStore();
        var orchestrator = new DeadLetterOrchestrator(
            store,
            new PassThroughDeadLetterMessageFactory(),
            new DeadLetterOptions(),
            NullLogger<DeadLetterOrchestrator>.Instance,
            serializer);

        var encina = Substitute.For<IEncina>();
        encina.Send(Arg.Any<SessionReminderRequest>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, string>("sent"));
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IEncina)).Returns(encina);

        var manager = new DeadLetterManager(
            store, orchestrator, serviceProvider, NullLogger<DeadLetterManager>.Instance, serializer);

        // Act: dead-letter the failed request
        var added = await orchestrator.AddAsync(
            new SessionReminderRequest(Diagnosis),
            new DeadLetterContext(
                EncinaErrors.Create("reminder.failed", "Reminder delivery failed"),
                Exception: null,
                SourcePattern: "Recoverability",
                TotalRetryAttempts: 3,
                FirstFailedAtUtc: new DateTime(2026, 9, 23, 9, 0, 0, DateTimeKind.Utc)));
        var messageId = added.Match(Right: m => m.Id, Left: e => throw new InvalidOperationException(e.Message));

        // Assert: stored encrypted
        var stored = store.GetMessage(messageId);
        stored.ShouldNotBeNull();
        stored!.RequestContent.ShouldStartWith("ENC:v1:");
        stored.RequestContent.ShouldNotContain("F41.1");

        // Act: replay reads the request back
        var replay = await manager.ReplayAsync(messageId);

        // Assert: the decrypted request reached IEncina
        replay.IsRight.ShouldBeTrue();
        await encina.Received(1).Send(
            Arg.Is<SessionReminderRequest>(r => r.Diagnosis == Diagnosis),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Delayed retry

    [Fact]
    public async Task DelayedRetry_ScheduleProcessAndReschedule_PersistsCiphertextAtEveryHop()
    {
        // Arrange
        var serializer = RealMessageEncryption.CreateSerializer();
        var store = new InMemoryDelayedRetryStore();
        var scheduler = new DelayedRetryScheduler(
            store,
            new PassThroughDelayedRetryMessageFactory(),
            NullLogger<DelayedRetryScheduler>.Instance,
            serializer);

        // Act: the recoverability pipeline schedules the first delayed retry
        (await scheduler.ScheduleRetryAsync(
            new SessionReminderRequest(Diagnosis),
            new RecoverabilityContext { CorrelationId = "corr-1" },
            TimeSpan.Zero,
            delayedRetryAttempt: 0)).IsRight.ShouldBeTrue();

        // Assert: stored encrypted
        var first = store.Messages.ShouldHaveSingleItem();
        first.RequestContent.ShouldStartWith("ENC:v1:");
        first.RequestContent.ShouldNotContain("F41.1");

        // Arrange: the processor resolves its collaborators from a scope, like in production.
        // The Left stub below is what makes the dispatch fail: DelayedRetryProcessor dispatches
        // through RuntimeTypeRequestDispatcher, so the failed Either takes the reschedule path.
        // Send is still invoked with the decrypted request, which is what this test checks.
        var encina = Substitute.For<IEncina>();
        encina.Send(Arg.Any<SessionReminderRequest>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, string>(EncinaErrors.Create("reminder.failed", "still failing")));
        var services = new ServiceCollection();
        services.AddSingleton<IDelayedRetryStore>(store);
        services.AddSingleton(encina);
        services.AddSingleton<IMessageSerializer>(serializer);
        services.AddSingleton<IDelayedRetryScheduler>(scheduler);
        await using var provider = services.BuildServiceProvider();

        var processor = new DelayedRetryProcessor(
            provider.GetRequiredService<IServiceScopeFactory>(),
            new RecoverabilityOptions { DelayedRetries = [TimeSpan.Zero, TimeSpan.Zero] },
            NullLogger<DelayedRetryProcessor>.Instance)
        {
            ProcessingInterval = TimeSpan.FromMinutes(5)
        };

        // Act: one processing pass decrypts the request, dispatches it and, because the dispatch
        // fails (the Left stub above), schedules the next delayed retry, which re-encrypts it.
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await processor.StartAsync(cts.Token);
        await store.WaitForMessageCountAsync(2, cts.Token);
        await processor.StopAsync(CancellationToken.None);

        // Assert: the decrypted request reached IEncina ...
        await encina.Received().Send(
            Arg.Is<SessionReminderRequest>(r => r.Diagnosis == Diagnosis),
            Arg.Any<CancellationToken>());

        // ... and the rescheduled retry is stored encrypted again and still decrypts.
        var second = store.Messages[1];
        second.DelayedRetryAttempt.ShouldBe(1);
        second.RequestContent.ShouldStartWith("ENC:v1:");
        second.RequestContent.ShouldNotContain("F41.1");
        serializer.Deserialize<SessionReminderRequest>(second.RequestContent)!.Diagnosis.ShouldBe(Diagnosis);
    }

    #endregion

    #region Test types

    public sealed record TherapySagaData
    {
        public string Diagnosis { get; init; } = string.Empty;

        public int Sessions { get; init; }
    }

    public sealed record SessionReminderRequest(string Diagnosis) : IRequest<string>;

    private sealed class PassThroughDeadLetterMessageFactory : IDeadLetterMessageFactory
    {
        public IDeadLetterMessage Create(DeadLetterData data) => new FakeDeadLetterMessage
        {
            Id = data.Id,
            RequestType = data.RequestType,
            RequestContent = data.RequestContent,
            ErrorMessage = data.ErrorMessage,
            SourcePattern = data.SourcePattern,
            TotalRetryAttempts = data.TotalRetryAttempts,
            FirstFailedAtUtc = data.FirstFailedAtUtc,
            DeadLetteredAtUtc = data.DeadLetteredAtUtc,
            ExpiresAtUtc = data.ExpiresAtUtc,
            CorrelationId = data.CorrelationId
        };
    }

    private sealed record DelayedRetryMessage(
        Guid Id,
        Guid RecoverabilityContextId,
        string RequestType,
        string RequestContent,
        string ContextContent,
        int DelayedRetryAttempt,
        DateTime ScheduledAtUtc,
        DateTime ExecuteAtUtc,
        string? CorrelationId) : IDelayedRetryMessage
    {
        public DateTime? ProcessedAtUtc { get; set; }

        public string? ErrorMessage { get; set; }
    }

    private sealed class PassThroughDelayedRetryMessageFactory : IDelayedRetryMessageFactory
    {
        public IDelayedRetryMessage Create(DelayedRetryMessageData data) => new DelayedRetryMessage(
            data.Id,
            data.RecoverabilityContextId,
            data.RequestType,
            data.RequestContent,
            data.ContextContent,
            data.DelayedRetryAttempt,
            data.ScheduledAtUtc,
            data.ExecuteAtUtc,
            data.CorrelationId);
    }

    private sealed class InMemoryDelayedRetryStore : IDelayedRetryStore
    {
        private readonly List<DelayedRetryMessage> _messages = [];
        private readonly Lock _gate = new();

        public IReadOnlyList<IDelayedRetryMessage> Messages
        {
            get
            {
                lock (_gate)
                {
                    return [.. _messages];
                }
            }
        }

        public Task AddAsync(IDelayedRetryMessage message, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                _messages.Add((DelayedRetryMessage)message);
            }

            return Task.CompletedTask;
        }

        public Task<IEnumerable<IDelayedRetryMessage>> GetPendingMessagesAsync(int batchSize, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                IEnumerable<IDelayedRetryMessage> pending =
                    [.. _messages.Where(m => m.ProcessedAtUtc is null && m.ErrorMessage is null).Take(batchSize)];
                return Task.FromResult(pending);
            }
        }

        public Task MarkAsProcessedAsync(Guid id, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                _messages.Single(m => m.Id == id).ProcessedAtUtc = DateTime.UtcNow;
            }

            return Task.CompletedTask;
        }

        public Task MarkAsFailedAsync(Guid id, string errorMessage, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                _messages.Single(m => m.Id == id).ErrorMessage = errorMessage;
            }

            return Task.CompletedTask;
        }

        public Task<bool> DeleteByContextIdAsync(Guid recoverabilityContextId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public async Task WaitForMessageCountAsync(int count, CancellationToken cancellationToken)
        {
            while (Messages.Count < count)
            {
                await Task.Delay(10, cancellationToken);
            }
        }
    }

    #endregion
}
