using Encina.Messaging.DeadLetter;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Recoverability;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;
using Encina.Testing.Fakes.Factories;
using Encina.Testing.Fakes.Stores;
using Encina.Testing.Shouldly;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;

#pragma warning disable CA2012 // NSubstitute setup of ValueTask-returning members

namespace Encina.UnitTests.Messaging.DeadLetter;

/// <summary>
/// The terminal failure of each built-in source is captured once into the dead letter queue while its
/// <c>IntegrateWith*</c> flag is on, never before it, and not at all while the flag is off (#1991).
/// </summary>
public sealed class DeadLetterSourcesCaptureTests
{
    public sealed record SampleCommand(int Value) : IRequest<int>;

    public sealed record SampleNotification(int Value) : INotification;

    public sealed record SampleSagaData(int Value);

    private static readonly EncinaError HandlerError = EncinaErrors.Create("sample.handler_failed", "failure");

    #region Recoverability

    [Fact]
    public async Task Recoverability_ImmediateRetriesExhausted_CapturesOnceAfterTheLastAttempt()
    {
        using var host = DeadLetterCaptureHost.Create();
        var behavior = RecoverabilityBehavior(host);
        var attemptsSeenEmpty = 0;

        var result = await behavior.Handle(new SampleCommand(1), RequestContext.Create(), () =>
        {
            if (host.Store.GetMessages().Count == 0)
                attemptsSeenEmpty++;
            return ValueTask.FromResult(Either<EncinaError, int>.Left(HandlerError));
        }, CancellationToken.None);

        result.ShouldBeLeft();
        attemptsSeenEmpty.ShouldBe(2);
        var deadLetter = host.DeadLettersOf(DeadLetterSourcePatterns.Recoverability).ShouldHaveSingleItem();
        deadLetter.ErrorCode.ShouldBe("sample.handler_failed");
        deadLetter.RequestType.ShouldContain(nameof(SampleCommand));
    }

    [Fact]
    public async Task Recoverability_SuccessAfterARetry_CapturesNothing()
    {
        using var host = DeadLetterCaptureHost.Create();
        var behavior = RecoverabilityBehavior(host);
        var calls = 0;

        var result = await behavior.Handle(new SampleCommand(1), RequestContext.Create(), () =>
            ValueTask.FromResult(++calls == 1
                ? Either<EncinaError, int>.Left(HandlerError)
                : Either<EncinaError, int>.Right(1)), CancellationToken.None);

        result.ShouldBeRight();
        host.Store.GetMessages().ShouldBeEmpty();
    }

    [Fact]
    public async Task Recoverability_FlagOff_CapturesNothing()
    {
        using var host = DeadLetterCaptureHost.Create(o => o.IntegrateWithRecoverability = false);
        var behavior = RecoverabilityBehavior(host);

        await behavior.Handle(new SampleCommand(1), RequestContext.Create(), () =>
            ValueTask.FromResult(Either<EncinaError, int>.Left(HandlerError)), CancellationToken.None);

        host.Store.GetMessages().ShouldBeEmpty();
    }

    [Fact]
    public async Task Recoverability_CancelledRequest_CapturesNothing()
    {
        using var host = DeadLetterCaptureHost.Create();
        var behavior = RecoverabilityBehavior(host);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await behavior.Handle(new SampleCommand(1), RequestContext.Create(), () =>
            ValueTask.FromResult(Either<EncinaError, int>.Left(HandlerError)), cts.Token);

        host.Store.GetMessages().ShouldBeEmpty();
    }

    [Fact]
    public async Task Recoverability_CaptureFails_StillReturnsTheRequestFailure()
    {
        using var host = DeadLetterCaptureHost.Create(store: FailingStore());
        var behavior = RecoverabilityBehavior(host);

        var result = await behavior.Handle(new SampleCommand(1), RequestContext.Create(), () =>
            ValueTask.FromResult(Either<EncinaError, int>.Left(HandlerError)), CancellationToken.None);

        result.ShouldBeErrorWithCode("sample.handler_failed");
    }

    private static RecoverabilityPipelineBehavior<SampleCommand, int> RecoverabilityBehavior(DeadLetterCaptureHost host) => new(
        new RecoverabilityOptions
        {
            ImmediateRetries = 1,
            ImmediateRetryDelay = TimeSpan.Zero,
            UseJitter = false,
            EnableDelayedRetries = false
        },
        NullLogger<RecoverabilityPipelineBehavior<SampleCommand, int>>.Instance,
        delayedRetryScheduler: null,
        timeProvider: host.Clock,
        deadLetterCapture: host.Capture);

    #endregion

    #region Outbox

    [Fact]
    public async Task Outbox_FailureThatUsesUpMaxRetries_CapturesOnce_AndNotBefore()
    {
        using var host = DeadLetterCaptureHost.Create();
        var (store, orchestrator) = Outbox(host);
        await orchestrator.AddAsync(new SampleNotification(1));
        var message = store.GetMessages().Single();

        (await orchestrator.ProcessPendingMessagesAsync(FailingPublish)).ShouldBeRight();
        host.Store.GetMessages().ShouldBeEmpty();
        host.Clock.Advance(TimeSpan.FromHours(1));
        (await orchestrator.ProcessPendingMessagesAsync(FailingPublish)).ShouldBeRight();

        var deadLetter = host.DeadLettersOf(DeadLetterSourcePatterns.Outbox).ShouldHaveSingleItem();
        deadLetter.SourceMessageId.ShouldBe(message.Id.ToString("D"));
        deadLetter.RequestType.ShouldBe(message.NotificationType);
        deadLetter.RequestContent.ShouldBe(message.Content);
        deadLetter.ErrorCode.ShouldBe("sample.handler_failed");
        deadLetter.TotalRetryAttempts.ShouldBe(2);
        store.GetMessage(message.Id)!.RetryCount.ShouldBe(2);
    }

    [Fact]
    public async Task Outbox_RequeuedMessageExhaustedAgain_KeepsOneDeadLetter()
    {
        using var host = DeadLetterCaptureHost.Create();
        var (_, orchestrator) = Outbox(host);
        await orchestrator.AddAsync(new SampleNotification(1));
        await ExhaustOutboxAsync(host, orchestrator);

        (await orchestrator.RequeueExhaustedAsync()).ShouldBeRight().ShouldBe(1);
        await ExhaustOutboxAsync(host, orchestrator);

        host.DeadLettersOf(DeadLetterSourcePatterns.Outbox).Count.ShouldBe(1);
    }

    [Fact]
    public async Task Outbox_FlagOff_CapturesNothing()
    {
        using var host = DeadLetterCaptureHost.Create(o => o.IntegrateWithOutbox = false);
        var (store, orchestrator) = Outbox(host);
        await orchestrator.AddAsync(new SampleNotification(1));

        await ExhaustOutboxAsync(host, orchestrator);

        host.Store.GetMessages().ShouldBeEmpty();
        store.GetMessages().Single().RetryCount.ShouldBe(2);
    }

    [Fact]
    public async Task Outbox_CaptureFails_LeavesTheMessageUnexhaustedForALaterCycle()
    {
        using var host = DeadLetterCaptureHost.Create(store: FailingStore());
        var (store, orchestrator) = Outbox(host);
        await orchestrator.AddAsync(new SampleNotification(1));

        await ExhaustOutboxAsync(host, orchestrator);

        var message = store.GetMessages().Single();
        message.RetryCount.ShouldBe(1);
        (await orchestrator.GetPendingCountAsync()).ShouldBeRight().ShouldBe(1);
    }

    private static (FakeOutboxStore Store, OutboxOrchestrator Orchestrator) Outbox(DeadLetterCaptureHost host)
    {
        var store = new FakeOutboxStore(host.Clock);
        var orchestrator = new OutboxOrchestrator(
            store,
            new OutboxOptions { MaxRetries = 2 },
            NullLogger<OutboxOrchestrator>.Instance,
            new FakeOutboxMessageFactory(),
            host.Serializer,
            host.Clock,
            host.Capture);
        return (store, orchestrator);
    }

    private static async Task ExhaustOutboxAsync(DeadLetterCaptureHost host, OutboxOrchestrator orchestrator)
    {
        for (var cycle = 0; cycle < 2; cycle++)
        {
            (await orchestrator.ProcessPendingMessagesAsync(FailingPublish)).ShouldBeRight();
            host.Clock.Advance(TimeSpan.FromHours(1));
        }
    }

    private static ValueTask<Either<EncinaError, Unit>> FailingPublish(IOutboxMessage message, Type type, object notification)
        => ValueTask.FromResult(Either<EncinaError, Unit>.Left(HandlerError));

    #endregion

    #region Inbox

    [Fact]
    public async Task Inbox_AttemptThatUsesUpMaxRetries_CapturesOnce_AndARejectedRedeliveryKeepsOne()
    {
        using var host = DeadLetterCaptureHost.Create();
        var orchestrator = Inbox(host);

        (await ProcessInboxAsync(orchestrator)).ShouldBeErrorWithCode("inbox.processing_failed");
        host.Store.GetMessages().ShouldBeEmpty();
        (await ProcessInboxAsync(orchestrator)).ShouldBeErrorWithCode("inbox.processing_failed");
        (await ProcessInboxAsync(orchestrator)).ShouldBeErrorWithCode(InboxErrorCodes.MaxRetriesExceeded);

        var deadLetter = host.DeadLettersOf(DeadLetterSourcePatterns.Inbox).ShouldHaveSingleItem();
        deadLetter.SourceMessageId.ShouldBe("inbox-1");
        deadLetter.TenantId.ShouldBe("tenant-a");
        deadLetter.CorrelationId.ShouldBe("corr-1");
        deadLetter.ExceptionType.ShouldBe(typeof(InvalidOperationException).FullName);
        deadLetter.RequestType.ShouldContain(nameof(SampleCommand));
        deadLetter.TotalRetryAttempts.ShouldBe(2);
    }

    [Fact]
    public async Task Inbox_FlagOff_CapturesNothing()
    {
        using var host = DeadLetterCaptureHost.Create(o => o.IntegrateWithInbox = false);
        var orchestrator = Inbox(host);

        for (var attempt = 0; attempt < 3; attempt++)
        {
            await ProcessInboxAsync(orchestrator);
        }

        host.Store.GetMessages().ShouldBeEmpty();
    }

    [Fact]
    public async Task Inbox_CaptureFails_TheAttemptReturnsTheCaptureError()
    {
        using var host = DeadLetterCaptureHost.Create(store: FailingStore());
        var orchestrator = Inbox(host);

        await ProcessInboxAsync(orchestrator);
        (await ProcessInboxAsync(orchestrator)).ShouldBeErrorWithCode(DeadLetterErrorCodes.StoreFailed);
        (await ProcessInboxAsync(orchestrator)).ShouldBeErrorWithCode(DeadLetterErrorCodes.StoreFailed);
    }

    private static InboxOrchestrator Inbox(DeadLetterCaptureHost host) => new(
        new FakeInboxStore(host.Clock),
        new InboxOptions { MaxRetries = 2 },
        NullLogger<InboxOrchestrator>.Instance,
        new DeadLetterCaptureHost.InboxFactory(),
        host.Serializer,
        host.Clock,
        host.Capture);

    private static async Task<Either<EncinaError, int>> ProcessInboxAsync(InboxOrchestrator orchestrator)
        => await orchestrator.ProcessAsync<int>(
            new SampleCommand(5),
            "inbox-1",
            typeof(SampleCommand).AssemblyQualifiedName!,
            "corr-1",
            new InboxMetadata { TenantId = "tenant-a", CorrelationId = "corr-1" },
            () => throw new InvalidOperationException("handler crashed"));

    #endregion

    #region Scheduling

    [Fact]
    public async Task Scheduling_FailureTheRetryPolicyDeadLetters_CapturesOnce_AndNotBefore()
    {
        using var host = DeadLetterCaptureHost.Create();
        var (store, orchestrator) = Scheduler(host);
        var id = (await orchestrator.ScheduleAsync(new SampleCommand(2), TimeSpan.FromMinutes(1))).ShouldBeRight();

        host.Clock.Advance(TimeSpan.FromMinutes(2));
        await orchestrator.ProcessDueMessagesAsync(FailingDispatch);
        host.Store.GetMessages().ShouldBeEmpty();
        host.Clock.Advance(TimeSpan.FromHours(1));
        await orchestrator.ProcessDueMessagesAsync(FailingDispatch);

        var message = store.GetMessage(id)!;
        var deadLetter = host.DeadLettersOf(DeadLetterSourcePatterns.Scheduling).ShouldHaveSingleItem();
        deadLetter.SourceMessageId.ShouldBe(id.ToString("D"));
        deadLetter.RequestType.ShouldBe(message.RequestType);
        deadLetter.RequestContent.ShouldBe(message.Content);
        deadLetter.ErrorCode.ShouldBe("sample.handler_failed");
        message.RetryCount.ShouldBe(2);
    }

    [Fact]
    public async Task Scheduling_DeadLetteredMessageFailingAgain_KeepsOneDeadLetter()
    {
        using var host = DeadLetterCaptureHost.Create();
        var (store, orchestrator) = Scheduler(host);
        var id = (await orchestrator.ScheduleAsync(new SampleCommand(2), TimeSpan.FromMinutes(1))).ShouldBeRight();
        await ExhaustScheduledAsync(host, orchestrator);

        // A requeue (operator action) brings it back with one retry left.
        var message = store.GetMessage(id)!;
        message.RetryCount = 1;
        message.NextRetryAtUtc = null;
        await orchestrator.ProcessDueMessagesAsync(FailingDispatch);

        host.DeadLettersOf(DeadLetterSourcePatterns.Scheduling).Count.ShouldBe(1);
    }

    [Fact]
    public async Task Scheduling_FlagOff_CapturesNothing()
    {
        using var host = DeadLetterCaptureHost.Create(o => o.IntegrateWithScheduling = false);
        var (store, orchestrator) = Scheduler(host);
        await orchestrator.ScheduleAsync(new SampleCommand(2), TimeSpan.FromMinutes(1));

        await ExhaustScheduledAsync(host, orchestrator);

        host.Store.GetMessages().ShouldBeEmpty();
        store.GetMessages().Single().RetryCount.ShouldBe(2);
    }

    [Fact]
    public async Task Scheduling_CaptureFails_LeavesTheMessageDueForALaterCycle()
    {
        using var host = DeadLetterCaptureHost.Create(store: FailingStore());
        var (store, orchestrator) = Scheduler(host);
        await orchestrator.ScheduleAsync(new SampleCommand(2), TimeSpan.FromMinutes(1));

        await ExhaustScheduledAsync(host, orchestrator);

        store.GetMessages().Single().RetryCount.ShouldBe(1);
        (await orchestrator.GetPendingCountAsync()).ShouldBeRight().ShouldBe(1);
    }

    [Fact]
    public async Task Scheduling_UnknownRequestType_CapturesWithTheUnknownTypeCode()
    {
        using var host = DeadLetterCaptureHost.Create();
        var (store, orchestrator) = Scheduler(host);
        var id = (await orchestrator.ScheduleAsync(new SampleCommand(2), TimeSpan.FromMinutes(1))).ShouldBeRight();
        store.GetMessage(id)!.RequestType = "No.Such.Type, NoSuchAssembly";

        await ExhaustScheduledAsync(host, orchestrator);

        var deadLetter = host.DeadLettersOf(DeadLetterSourcePatterns.Scheduling).ShouldHaveSingleItem();
        deadLetter.ErrorCode.ShouldBe(SchedulingErrorCodes.UnknownRequestType);
        deadLetter.RequestType.ShouldBe("No.Such.Type, NoSuchAssembly");
    }

    private static (FakeScheduledMessageStore Store, SchedulerOrchestrator Orchestrator) Scheduler(DeadLetterCaptureHost host)
    {
        var options = new SchedulingOptions { MaxRetries = 2 };
        var store = new FakeScheduledMessageStore(host.Clock);
        var orchestrator = new SchedulerOrchestrator(
            store,
            options,
            NullLogger<SchedulerOrchestrator>.Instance,
            new DeadLetterCaptureHost.ScheduledFactory(),
            new ExponentialBackoffRetryPolicy(options),
            host.Serializer,
            cronParser: null,
            timeProvider: host.Clock,
            deadLetterCapture: host.Capture);
        return (store, orchestrator);
    }

    private static async Task ExhaustScheduledAsync(DeadLetterCaptureHost host, SchedulerOrchestrator orchestrator)
    {
        for (var cycle = 0; cycle < 2; cycle++)
        {
            host.Clock.Advance(TimeSpan.FromHours(1));
            await orchestrator.ProcessDueMessagesAsync(FailingDispatch);
        }
    }

    private static ValueTask<Either<EncinaError, Unit>> FailingDispatch(IScheduledMessage message, Type type, object request, CancellationToken cancellationToken)
        => ValueTask.FromResult(Either<EncinaError, Unit>.Left(HandlerError));

    #endregion

    #region Sagas

    [Fact]
    public async Task Saga_FailAsync_CapturesTheFailedSagaOnce()
    {
        using var host = DeadLetterCaptureHost.Create();
        var (store, orchestrator) = Saga(host);
        var sagaId = (await orchestrator.StartAsync("OrderSaga", new SampleSagaData(9))).ShouldBeRight();
        host.Store.GetMessages().ShouldBeEmpty();

        (await orchestrator.FailAsync(sagaId, "saga.compensation_failed")).ShouldBeRight();
        (await orchestrator.FailAsync(sagaId, "saga.compensation_failed")).ShouldBeRight();

        var deadLetter = host.DeadLettersOf(DeadLetterSourcePatterns.Saga).ShouldHaveSingleItem();
        deadLetter.SourceMessageId.ShouldBe(sagaId.ToString("D"));
        deadLetter.RequestType.ShouldBe("OrderSaga");
        deadLetter.RequestContent.ShouldBe(store.GetSaga(sagaId)!.Data);
        deadLetter.ErrorCode.ShouldBe("saga.compensation_failed");
    }

    [Fact]
    public async Task Saga_CompensatedSaga_IsNotCaptured()
    {
        using var host = DeadLetterCaptureHost.Create();
        var (_, orchestrator) = Saga(host);
        var sagaId = (await orchestrator.StartAsync("OrderSaga", new SampleSagaData(9))).ShouldBeRight();

        (await orchestrator.StartCompensationAsync(sagaId, "saga.step_failed")).ShouldBeRight();
        (await orchestrator.CompensateStepAsync(sagaId)).ShouldBeRight().ShouldBe(-1);

        host.Store.GetMessages().ShouldBeEmpty();
    }

    [Fact]
    public async Task Saga_FlagOff_CapturesNothing()
    {
        using var host = DeadLetterCaptureHost.Create(o => o.IntegrateWithSagas = false);
        var (_, orchestrator) = Saga(host);
        var sagaId = (await orchestrator.StartAsync("OrderSaga", new SampleSagaData(9))).ShouldBeRight();

        (await orchestrator.FailAsync(sagaId, "saga.compensation_failed")).ShouldBeRight();

        host.Store.GetMessages().ShouldBeEmpty();
    }

    [Fact]
    public async Task Saga_CaptureFails_FailAsyncReturnsTheErrorAfterStoringFailed()
    {
        using var host = DeadLetterCaptureHost.Create(store: FailingStore());
        var (store, orchestrator) = Saga(host);
        var sagaId = (await orchestrator.StartAsync("OrderSaga", new SampleSagaData(9))).ShouldBeRight();

        var result = await orchestrator.FailAsync(sagaId, "saga.compensation_failed");

        result.ShouldBeErrorWithCode(DeadLetterErrorCodes.StoreFailed);
        store.GetSaga(sagaId)!.Status.ShouldBe(SagaStatus.Failed);
    }

    private static (FakeSagaStore Store, SagaOrchestrator Orchestrator) Saga(DeadLetterCaptureHost host)
    {
        var store = new FakeSagaStore(host.Clock);
        var orchestrator = new SagaOrchestrator(
            store,
            new SagaOptions(),
            NullLogger<SagaOrchestrator>.Instance,
            new DeadLetterCaptureHost.SagaFactory(),
            host.Serializer,
            host.Clock,
            host.Capture);
        return (store, orchestrator);
    }

    #endregion

    #region Replay

    [Fact]
    public async Task Replay_OutboxDeadLetter_PublishesTheNotificationAgain()
    {
        var encina = Substitute.For<IEncina>();
        encina.Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Unit.Default));
        using var host = DeadLetterCaptureHost.Create(encina: encina);
        var (_, orchestrator) = Outbox(host);
        await orchestrator.AddAsync(new SampleNotification(4));
        await ExhaustOutboxAsync(host, orchestrator);
        var deadLetter = host.DeadLettersOf(DeadLetterSourcePatterns.Outbox).ShouldHaveSingleItem();

        using var scope = host.Provider.CreateScope();
        var replay = await scope.ServiceProvider.GetRequiredService<IDeadLetterManager>().ReplayAsync(deadLetter.Id);

        replay.ShouldBeRight().Success.ShouldBeTrue();
        await encina.Received(1).Publish(
            Arg.Is<INotification>(n => n is SampleNotification && ((SampleNotification)n).Value == 4),
            Arg.Any<CancellationToken>());
    }

    #endregion

    private static IDeadLetterStore FailingStore()
    {
        var store = Substitute.For<IDeadLetterStore>();
        store.AddAsync(Arg.Any<IDeadLetterMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Either<EncinaError, bool>>(EncinaErrors.Create(DeadLetterErrorCodes.StoreFailed, "down")));
        return store;
    }
}
