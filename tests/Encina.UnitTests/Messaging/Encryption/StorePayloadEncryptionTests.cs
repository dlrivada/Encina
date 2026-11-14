using System.Security.Cryptography;
using Encina.Messaging.DeadLetter;
using Encina.Messaging.Encryption;
using Encina.Messaging.Encryption.Attributes;
using Encina.Messaging.Encryption.Serialization;
using Encina.Messaging.Outbox;
using Encina.Messaging.Recoverability;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;
using Encina.Messaging.Serialization;
using Encina.Security.Encryption;
using Encina.Security.Encryption.Algorithms;
using Encina.Testing.Fakes.Factories;
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
            serializer,
            Substitute.For<IRequestContextAccessor>());

        var encina = Substitute.For<IEncina>();
        encina.Send(Arg.Any<SessionReminderRequest>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, string>("sent"));
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IEncina)).Returns(encina);

        var manager = new DeadLetterManager(
            store, orchestrator, serviceProvider, NullLogger<DeadLetterManager>.Instance, serializer,
            new DeadLetterOptions(), Substitute.For<IRequestContextAccessor>());

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
            Options.Create(new RecoverabilityOptions { EnableDelayedRetries = true, DelayedRetries = [TimeSpan.Zero, TimeSpan.Zero] }),
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

    #region Runtime type at the persistence call sites (#3780)

    // These tests use a serializer that encrypts only [EncryptedMessage] types, so the stored payload is
    // encrypted only when the orchestrator serializes the concrete runtime type: the static type the
    // caller uses (an interface or an unmarked base) carries no attribute.

    [Fact]
    public async Task Outbox_AddAsync_MarkedNotificationTypedAsInterface_StoresCiphertextAndRuntimeTypeName()
    {
        // Arrange
        var serializer = CreateAttributeOnlySerializer();
        var store = new FakeOutboxStore();
        var orchestrator = CreateOutboxOrchestrator(store, serializer);
        INotification notification = new MarkedDiagnosisNotification(Diagnosis);

        // Act
        (await orchestrator.AddAsync(notification)).IsRight.ShouldBeTrue();

        // Assert
        var stored = store.GetAddedMessages().ShouldHaveSingleItem();
        stored.Content.ShouldStartWith("ENC:v1:");
        stored.Content.ShouldNotContain("F41.1");
        stored.NotificationType.ShouldBe(typeof(MarkedDiagnosisNotification).AssemblyQualifiedName);

        // Act: the batch processor rebuilds the notification from the stored type name
        Type? publishedType = null;
        object? published = null;
        var processed = await orchestrator.ProcessPendingMessagesAsync((_, type, value) =>
        {
            publishedType = type;
            published = value;
            return ValueTask.FromResult(Right<EncinaError, Unit>(unit));
        });

        // Assert
        processed.Match(Right: count => count, Left: e => throw new InvalidOperationException(e.Message)).ShouldBe(1);
        publishedType.ShouldBe(typeof(MarkedDiagnosisNotification));
        published.ShouldBeOfType<MarkedDiagnosisNotification>().Diagnosis.ShouldBe(Diagnosis);
    }

    [Fact]
    public async Task Outbox_AddAsync_UnmarkedNotificationTypedAsInterface_StaysPlain()
    {
        // Arrange
        var serializer = CreateAttributeOnlySerializer();
        var store = new FakeOutboxStore();
        var orchestrator = CreateOutboxOrchestrator(store, serializer);
        INotification notification = new PlainNoteNotification("public note");

        // Act
        (await orchestrator.AddAsync(notification)).IsRight.ShouldBeTrue();

        // Assert
        var stored = store.GetAddedMessages().ShouldHaveSingleItem();
        stored.Content.ShouldNotStartWith("ENC:");
        stored.Content.ShouldContain("public note");
        stored.NotificationType.ShouldBe(typeof(PlainNoteNotification).AssemblyQualifiedName);
    }

    [Fact]
    public async Task Outbox_AddAsync_EncryptionFails_ThrowsAndStoresNothing()
    {
        // Arrange
        var serializer = CreateAttributeOnlySerializer();
        var store = new FakeOutboxStore();
        var orchestrator = CreateOutboxOrchestrator(store, serializer);
        INotification notification = new UnknownKeyNotification(Diagnosis);

        // Act
        var act = () => orchestrator.AddAsync(notification);

        // Assert: the encryption failure propagates; nothing reaches the store, in plaintext or otherwise
        await act.ShouldThrowAsync<InvalidOperationException>();
        store.GetAddedMessages().ShouldBeEmpty();
    }

    [Fact]
    public async Task Scheduler_ScheduleAsync_MarkedRequestTypedAsInterface_StoresCiphertextAndRuntimeTypeName()
    {
        // Arrange
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 11, 12, 10, 0, 0, TimeSpan.Zero));
        var serializer = CreateAttributeOnlySerializer();
        var store = new FakeScheduledMessageStore(time);
        var orchestrator = CreateSchedulerOrchestrator(store, serializer, time);
        IRequest<string> request = new MarkedReminderRequest(Diagnosis);

        // Act
        var messageId = (await orchestrator.ScheduleAsync(request, TimeSpan.FromMinutes(5)))
            .Match(Right: id => id, Left: e => throw new InvalidOperationException(e.Message));

        // Assert
        var stored = store.GetMessage(messageId)!;
        stored.Content.ShouldStartWith("ENC:v1:");
        stored.Content.ShouldNotContain("F41.1");
        stored.RequestType.ShouldBe(typeof(MarkedReminderRequest).AssemblyQualifiedName);

        // Act: once due, the processor rebuilds the request from the stored type name
        time.Advance(TimeSpan.FromMinutes(10));
        object? dispatched = null;
        var processed = await orchestrator.ProcessDueMessagesAsync((_, _, value, _) =>
        {
            dispatched = value;
            return ValueTask.FromResult(Right<EncinaError, Unit>(unit));
        });

        // Assert
        processed.Match(Right: count => count, Left: e => throw new InvalidOperationException(e.Message)).ShouldBe(1);
        dispatched.ShouldBeOfType<MarkedReminderRequest>().Diagnosis.ShouldBe(Diagnosis);
    }

    [Fact]
    public async Task Scheduler_ScheduleAsync_UnmarkedRequestTypedAsInterface_StaysPlain()
    {
        // Arrange
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 11, 12, 10, 0, 0, TimeSpan.Zero));
        var store = new FakeScheduledMessageStore(time);
        var orchestrator = CreateSchedulerOrchestrator(store, CreateAttributeOnlySerializer(), time);
        IRequest<string> request = new PlainReminderRequest("public note");

        // Act
        var messageId = (await orchestrator.ScheduleAsync(request, TimeSpan.FromMinutes(5)))
            .Match(Right: id => id, Left: e => throw new InvalidOperationException(e.Message));

        // Assert
        var stored = store.GetMessage(messageId)!;
        stored.Content.ShouldNotStartWith("ENC:");
        stored.Content.ShouldContain("public note");
        stored.RequestType.ShouldBe(typeof(PlainReminderRequest).AssemblyQualifiedName);
    }

    [Fact]
    public async Task Scheduler_ScheduleAsync_EncryptionFails_ThrowsAndStoresNothing()
    {
        // Arrange
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 11, 12, 10, 0, 0, TimeSpan.Zero));
        var store = new FakeScheduledMessageStore(time);
        var orchestrator = CreateSchedulerOrchestrator(store, CreateAttributeOnlySerializer(), time);
        IRequest<string> request = new UnknownKeyRequest(Diagnosis);

        // Act
        var act = () => orchestrator.ScheduleAsync(request, TimeSpan.FromMinutes(5));

        // Assert
        await act.ShouldThrowAsync<InvalidOperationException>();
        store.GetAddedMessages().ShouldBeEmpty();
    }

    [Fact]
    public async Task Scheduler_ScheduleRecurringAsync_MarkedRequestTypedAsBaseClass_StoresCiphertextAndRuntimeTypeName()
    {
        // Arrange
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 11, 12, 10, 0, 0, TimeSpan.Zero));
        var store = new FakeScheduledMessageStore(time);
        var orchestrator = CreateSchedulerOrchestrator(store, CreateAttributeOnlySerializer(), time, EveryFiveMinutes(time));
        ReminderRequestBase request = new MarkedDerivedReminderRequest(Diagnosis);

        // Act
        var messageId = (await orchestrator.ScheduleRecurringAsync(request, "*/5 * * * *"))
            .Match(Right: id => id, Left: e => throw new InvalidOperationException(e.Message));

        // Assert
        var stored = store.GetMessage(messageId)!;
        stored.IsRecurring.ShouldBeTrue();
        stored.Content.ShouldStartWith("ENC:v1:");
        stored.Content.ShouldNotContain("F41.1");
        stored.RequestType.ShouldBe(typeof(MarkedDerivedReminderRequest).AssemblyQualifiedName);

        // Act: once due, the processor rebuilds the derived request from the stored type name
        time.Advance(TimeSpan.FromMinutes(10));
        object? dispatched = null;
        await orchestrator.ProcessDueMessagesAsync((_, _, value, _) =>
        {
            dispatched = value;
            return ValueTask.FromResult(Right<EncinaError, Unit>(unit));
        });

        // Assert
        dispatched.ShouldBeOfType<MarkedDerivedReminderRequest>().Diagnosis.ShouldBe(Diagnosis);
    }

    [Fact]
    public async Task Scheduler_ScheduleRecurringAsync_UnmarkedRequestTypedAsInterface_StaysPlain()
    {
        // Arrange
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 11, 12, 10, 0, 0, TimeSpan.Zero));
        var store = new FakeScheduledMessageStore(time);
        var orchestrator = CreateSchedulerOrchestrator(store, CreateAttributeOnlySerializer(), time, EveryFiveMinutes(time));
        IRequest<string> request = new PlainReminderRequest("public note");

        // Act
        var messageId = (await orchestrator.ScheduleRecurringAsync(request, "*/5 * * * *"))
            .Match(Right: id => id, Left: e => throw new InvalidOperationException(e.Message));

        // Assert
        var stored = store.GetMessage(messageId)!;
        stored.Content.ShouldNotStartWith("ENC:");
        stored.RequestType.ShouldBe(typeof(PlainReminderRequest).AssemblyQualifiedName);
    }

    [Fact]
    public async Task Scheduler_ScheduleRecurringAsync_EncryptionFails_ThrowsAndStoresNothing()
    {
        // Arrange
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 11, 12, 10, 0, 0, TimeSpan.Zero));
        var store = new FakeScheduledMessageStore(time);
        var orchestrator = CreateSchedulerOrchestrator(store, CreateAttributeOnlySerializer(), time, EveryFiveMinutes(time));
        IRequest<string> request = new UnknownKeyRequest(Diagnosis);

        // Act
        var act = () => orchestrator.ScheduleRecurringAsync(request, "*/5 * * * *");

        // Assert
        await act.ShouldThrowAsync<InvalidOperationException>();
        store.GetAddedMessages().ShouldBeEmpty();
    }

    [Fact]
    public async Task DelayedRetry_ScheduleRetryAsync_MarkedRequestTypedAsInterface_StoresCiphertextAndRuntimeTypeNameAtEveryHop()
    {
        // Arrange
        var serializer = CreateAttributeOnlySerializer();
        var store = new InMemoryDelayedRetryStore();
        var scheduler = new DelayedRetryScheduler(
            store,
            new PassThroughDelayedRetryMessageFactory(),
            NullLogger<DelayedRetryScheduler>.Instance,
            serializer);
        IRequest<string> request = new MarkedReminderRequest(Diagnosis);

        // Act
        (await scheduler.ScheduleRetryAsync(
            request,
            new RecoverabilityContext { CorrelationId = "corr-3780" },
            TimeSpan.Zero,
            delayedRetryAttempt: 0)).IsRight.ShouldBeTrue();

        // Assert
        var first = store.Messages.ShouldHaveSingleItem();
        first.RequestContent.ShouldStartWith("ENC:v1:");
        first.RequestContent.ShouldNotContain("F41.1");
        first.RequestType.ShouldBe(typeof(MarkedReminderRequest).AssemblyQualifiedName);

        // Arrange: the processor rebuilds the request from the stored type name; the failing dispatch
        // makes it schedule the next retry through the same scheduler.
        var encina = Substitute.For<IEncina>();
        encina.Send(Arg.Any<MarkedReminderRequest>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, string>(EncinaErrors.Create("reminder.failed", "still failing")));
        var services = new ServiceCollection();
        services.AddSingleton<IDelayedRetryStore>(store);
        services.AddSingleton(encina);
        services.AddSingleton<IMessageSerializer>(serializer);
        services.AddSingleton<IDelayedRetryScheduler>(scheduler);
        await using var provider = services.BuildServiceProvider();

        var processor = new DelayedRetryProcessor(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new RecoverabilityOptions { EnableDelayedRetries = true, DelayedRetries = [TimeSpan.Zero, TimeSpan.Zero] }),
            NullLogger<DelayedRetryProcessor>.Instance)
        {
            ProcessingInterval = TimeSpan.FromMinutes(5)
        };

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await processor.StartAsync(cts.Token);
        await store.WaitForMessageCountAsync(2, cts.Token);
        await processor.StopAsync(CancellationToken.None);

        // Assert: the decrypted concrete request reached IEncina, and the next hop is stored the same way
        await encina.Received().Send(
            Arg.Is<MarkedReminderRequest>(r => r.Diagnosis == Diagnosis),
            Arg.Any<CancellationToken>());
        var second = store.Messages[1];
        second.RequestContent.ShouldStartWith("ENC:v1:");
        second.RequestType.ShouldBe(typeof(MarkedReminderRequest).AssemblyQualifiedName);
        serializer.Deserialize<MarkedReminderRequest>(second.RequestContent)!.Diagnosis.ShouldBe(Diagnosis);
    }

    [Fact]
    public async Task DelayedRetry_ScheduleRetryAsync_UnmarkedRequestTypedAsInterface_StaysPlain()
    {
        // Arrange
        var store = new InMemoryDelayedRetryStore();
        var scheduler = new DelayedRetryScheduler(
            store,
            new PassThroughDelayedRetryMessageFactory(),
            NullLogger<DelayedRetryScheduler>.Instance,
            CreateAttributeOnlySerializer());
        IRequest<string> request = new PlainReminderRequest("public note");

        // Act
        (await scheduler.ScheduleRetryAsync(request, new RecoverabilityContext(), TimeSpan.Zero, delayedRetryAttempt: 0))
            .IsRight.ShouldBeTrue();

        // Assert
        var stored = store.Messages.ShouldHaveSingleItem();
        stored.RequestContent.ShouldNotStartWith("ENC:");
        stored.RequestContent.ShouldContain("public note");
        stored.RequestType.ShouldBe(typeof(PlainReminderRequest).AssemblyQualifiedName);
    }

    [Fact]
    public async Task DelayedRetry_ScheduleRetryAsync_EncryptionFails_ThrowsAndStoresNothing()
    {
        // Arrange
        var store = new InMemoryDelayedRetryStore();
        var scheduler = new DelayedRetryScheduler(
            store,
            new PassThroughDelayedRetryMessageFactory(),
            NullLogger<DelayedRetryScheduler>.Instance,
            CreateAttributeOnlySerializer());
        IRequest<string> request = new UnknownKeyRequest(Diagnosis);

        // Act
        var act = () => scheduler.ScheduleRetryAsync(request, new RecoverabilityContext(), TimeSpan.Zero, delayedRetryAttempt: 0);

        // Assert
        await act.ShouldThrowAsync<InvalidOperationException>();
        store.Messages.ShouldBeEmpty();
    }

    [Fact]
    public async Task Saga_StartAsync_MarkedDataTypedAsBaseClass_StoresCiphertext()
    {
        // Arrange
        var serializer = CreateAttributeOnlySerializer();
        var store = new FakeSagaStore();
        var orchestrator = CreateSagaOrchestrator(store, serializer);
        SagaDataBase data = new MarkedSagaData { Diagnosis = Diagnosis };

        // Act
        var sagaId = (await orchestrator.StartAsync("TherapySaga", data))
            .Match(Right: id => id, Left: e => throw new InvalidOperationException(e.Message));

        // Assert: encrypted, and the ciphertext holds the complete concrete payload
        var stored = store.GetSaga(sagaId)!;
        stored.Data.ShouldStartWith("ENC:v1:");
        stored.Data.ShouldNotContain("F41.1");
        serializer.Deserialize<MarkedSagaData>(stored.Data)!.Diagnosis.ShouldBe(Diagnosis);

        // Act: the saga reads back through the unchanged read side (as the base type, #3789)
        var snapshot = await orchestrator.GetAsync<SagaDataBase>(sagaId);

        // Assert
        snapshot.IsSome.ShouldBeTrue();
    }

    [Fact]
    public async Task Saga_StartAsync_UnmarkedDataTypedAsBaseClass_StaysPlain()
    {
        // Arrange
        var store = new FakeSagaStore();
        var orchestrator = CreateSagaOrchestrator(store, CreateAttributeOnlySerializer());
        SagaDataBase data = new PlainSagaData { Note = "public note" };

        // Act
        var sagaId = (await orchestrator.StartAsync("TherapySaga", data))
            .Match(Right: id => id, Left: e => throw new InvalidOperationException(e.Message));

        // Assert: the derived property is stored too, the payload is complete
        var stored = store.GetSaga(sagaId)!;
        stored.Data.ShouldNotStartWith("ENC:");
        stored.Data.ShouldContain("public note");
    }

    [Fact]
    public async Task Saga_StartAsync_EncryptionFails_ThrowsAndStoresNothing()
    {
        // Arrange
        var store = new FakeSagaStore();
        var orchestrator = CreateSagaOrchestrator(store, CreateAttributeOnlySerializer());
        SagaDataBase data = new UnknownKeySagaData { Diagnosis = Diagnosis };

        // Act
        var act = () => orchestrator.StartAsync("TherapySaga", data);

        // Assert
        await act.ShouldThrowAsync<InvalidOperationException>();
        store.GetAddedSagas().ShouldBeEmpty();
    }

    [Fact]
    public async Task Saga_AdvanceAsync_UpdateReturnsMarkedDataTypedAsBaseClass_StoresCiphertext()
    {
        // Arrange: the saga starts with unmarked data, stored plain
        var serializer = CreateAttributeOnlySerializer();
        var store = new FakeSagaStore();
        var orchestrator = CreateSagaOrchestrator(store, serializer);
        var sagaId = (await orchestrator.StartAsync<SagaDataBase>("TherapySaga", new SagaDataBase()))
            .Match(Right: id => id, Left: e => throw new InvalidOperationException(e.Message));
        store.GetSaga(sagaId)!.Data.ShouldNotStartWith("ENC:");

        // Act: the update returns marked data typed as the base class
        var advance = await orchestrator.AdvanceAsync<SagaDataBase>(
            sagaId,
            current => new MarkedSagaData { Diagnosis = Diagnosis, Sessions = current.Sessions + 1 });

        // Assert
        advance.IsRight.ShouldBeTrue();
        var stored = store.GetSaga(sagaId)!;
        stored.Data.ShouldStartWith("ENC:v1:");
        stored.Data.ShouldNotContain("F41.1");
        var decrypted = serializer.Deserialize<MarkedSagaData>(stored.Data)!;
        decrypted.Diagnosis.ShouldBe(Diagnosis);
        decrypted.Sessions.ShouldBe(1);
    }

    [Fact]
    public async Task Saga_AdvanceAsync_UpdateReturnsUnmarkedData_StaysPlain()
    {
        // Arrange
        var store = new FakeSagaStore();
        var orchestrator = CreateSagaOrchestrator(store, CreateAttributeOnlySerializer());
        var sagaId = (await orchestrator.StartAsync<SagaDataBase>("TherapySaga", new SagaDataBase()))
            .Match(Right: id => id, Left: e => throw new InvalidOperationException(e.Message));

        // Act
        var advance = await orchestrator.AdvanceAsync<SagaDataBase>(
            sagaId,
            current => new PlainSagaData { Note = "public note", Sessions = current.Sessions + 1 });

        // Assert
        advance.IsRight.ShouldBeTrue();
        var stored = store.GetSaga(sagaId)!;
        stored.Data.ShouldNotStartWith("ENC:");
        stored.Data.ShouldContain("public note");
    }

    [Fact]
    public async Task Saga_AdvanceAsync_EncryptionFails_ThrowsAndLeavesTheStoredDataUnchanged()
    {
        // Arrange
        var store = new FakeSagaStore();
        var orchestrator = CreateSagaOrchestrator(store, CreateAttributeOnlySerializer());
        var sagaId = (await orchestrator.StartAsync<SagaDataBase>("TherapySaga", new SagaDataBase()))
            .Match(Right: id => id, Left: e => throw new InvalidOperationException(e.Message));
        var before = store.GetSaga(sagaId)!.Data;

        // Act
        var act = () => orchestrator.AdvanceAsync<SagaDataBase>(
            sagaId,
            _ => new UnknownKeySagaData { Diagnosis = Diagnosis });

        // Assert: no plaintext fallback, no update
        await act.ShouldThrowAsync<InvalidOperationException>();
        var after = store.GetSaga(sagaId)!;
        after.Data.ShouldBe(before);
        after.CurrentStep.ShouldBe(0);
    }

    private static EncryptingMessageSerializer CreateAttributeOnlySerializer()
    {
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);

        var keyProvider = new InMemoryKeyProvider();
        keyProvider.AddKey(RealMessageEncryption.KeyId, key);
        keyProvider.SetCurrentKey(RealMessageEncryption.KeyId);

        return new EncryptingMessageSerializer(
            new JsonMessageSerializer(),
            new DefaultMessageEncryptionProvider(new AesGcmFieldEncryptor(keyProvider), keyProvider),
            Options.Create(new MessageEncryptionOptions { Enabled = true, EncryptAllMessages = false }),
            NullLogger<EncryptingMessageSerializer>.Instance);
    }

    private static OutboxOrchestrator CreateOutboxOrchestrator(FakeOutboxStore store, IMessageSerializer serializer) => new(
        store,
        new OutboxOptions(),
        NullLogger<OutboxOrchestrator>.Instance,
        new FakeOutboxMessageFactory(),
        serializer);

    private static SchedulerOrchestrator CreateSchedulerOrchestrator(
        FakeScheduledMessageStore store,
        IMessageSerializer serializer,
        TimeProvider time,
        ICronParser? cronParser = null)
    {
        var options = new SchedulingOptions();
        return new SchedulerOrchestrator(
            store,
            options,
            NullLogger<SchedulerOrchestrator>.Instance,
            new EfScheduledMessageFactory(),
            new ExponentialBackoffRetryPolicy(options),
            serializer,
            cronParser,
            time);
    }

    private static SagaOrchestrator CreateSagaOrchestrator(FakeSagaStore store, IMessageSerializer serializer) => new(
        store,
        new SagaOptions(),
        NullLogger<SagaOrchestrator>.Instance,
        new EfSagaStateFactory(),
        serializer);

    private static ICronParser EveryFiveMinutes(TimeProvider time)
    {
        var cronParser = Substitute.For<ICronParser>();
        cronParser.GetNextOccurrence(Arg.Any<string>(), Arg.Any<DateTime>())
            .Returns(Right<EncinaError, DateTime>(time.GetUtcNow().UtcDateTime.AddMinutes(5)));
        return cronParser;
    }

    #endregion

    #region Test types

    [EncryptedMessage]
    public sealed record MarkedDiagnosisNotification(string Diagnosis) : INotification;

    public sealed record PlainNoteNotification(string Note) : INotification;

    [EncryptedMessage(KeyId = "missing-key")]
    public sealed record UnknownKeyNotification(string Diagnosis) : INotification;

    [EncryptedMessage]
    public sealed record MarkedReminderRequest(string Diagnosis) : IRequest<string>;

    public sealed record PlainReminderRequest(string Note) : IRequest<string>;

    [EncryptedMessage(KeyId = "missing-key")]
    public sealed record UnknownKeyRequest(string Diagnosis) : IRequest<string>;

    public abstract record ReminderRequestBase : IRequest<string>;

    [EncryptedMessage]
    public sealed record MarkedDerivedReminderRequest(string Diagnosis) : ReminderRequestBase;

    public record SagaDataBase
    {
        public int Sessions { get; init; }
    }

    [EncryptedMessage]
    public sealed record MarkedSagaData : SagaDataBase
    {
        public string Diagnosis { get; init; } = string.Empty;
    }

    public sealed record PlainSagaData : SagaDataBase
    {
        public string Note { get; init; } = string.Empty;
    }

    [EncryptedMessage(KeyId = "missing-key")]
    public sealed record UnknownKeySagaData : SagaDataBase
    {
        public string Diagnosis { get; init; } = string.Empty;
    }

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
            ErrorCode = data.ErrorCode,
            SourcePattern = data.SourcePattern,
            SourceMessageId = data.SourceMessageId,
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
