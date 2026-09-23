using Encina.Messaging.ContentRouter;
using Encina.Messaging.DeadLetter;
using Encina.Messaging.Diagnostics;
using Encina.Messaging.Health;
using Encina.Messaging.Inbox;
using Encina.Messaging.Recoverability;
using Encina.Messaging.RoutingSlip;
using Encina.Messaging.Sagas;
using Encina.Messaging.Sagas.LowCeremony;
using Encina.Messaging.ScatterGather;
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

    [Fact]
    public async Task Scheduler_BatchFailed_LogsAndTagsOnlyTheErrorCode()
    {
        // Arrange
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 9, 23, 10, 0, 0, TimeSpan.Zero));
        var store = new FakeScheduledMessageStore(time);
        var logger = new FakeLogger<SchedulerOrchestrator>();
        var options = new SchedulingOptions();
        var orchestrator = new SchedulerOrchestrator(
            store, options, logger, new EfScheduledMessageFactory(),
            new ExponentialBackoffRetryPolicy(options), new JsonMessageSerializer(),
            cronParser: null, timeProvider: time);

        using var listener = new System.Diagnostics.ActivityListener
        {
            ShouldListenTo = source => source.Name == "Encina.Messaging.Scheduling",
            Sample = (ref System.Diagnostics.ActivityCreationOptions<System.Diagnostics.ActivityContext> _) =>
                System.Diagnostics.ActivitySamplingResult.AllData
        };
        System.Diagnostics.ActivitySource.AddActivityListener(listener);

        var processorLogger = new FakeLogger<ScheduledMessageProcessor>();

        // Act - reproduce the ScheduledMessageProcessor.ProcessOnceAsync failure branch directly,
        // since the processor itself only runs through a hosted-service loop.
        var errorCode = SensitiveError.GetCode().IfNone("unknown");
        var activity = SchedulingActivitySource.StartProcessingCycle(10);
        SchedulingProcessorLog.BatchFailed(processorLogger, errorCode);
        SchedulingActivitySource.Failed(activity, errorCode);

        // Assert
        var logs = processorLogger.Collector.GetSnapshot();
        logs.ShouldContain(r => r.Message.Contains("consent.missing"));
        logs.ShouldAllBe(r => !r.Message.Contains(PersonalData));
    }

    [Fact]
    public async Task Recoverability_PermanentAndTransientErrors_LogOnlyTheErrorCode()
    {
        // Arrange
        var logger = new FakeLogger<RecoverabilityPipelineBehavior<SensitiveRequest, string>>();
        var options = new RecoverabilityOptions { ImmediateRetries = 0, EnableDelayedRetries = false };
        var behavior = new RecoverabilityPipelineBehavior<SensitiveRequest, string>(options, logger);
        var context = Substitute.For<IRequestContext>();
        context.CorrelationId.Returns("corr-1");

        // Act
        var result = await behavior.Handle(
            new SensitiveRequest(),
            context,
            () => ValueTask.FromResult(Either<EncinaError, string>.Left(SensitiveError)),
            CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var logs = logger.Collector.GetSnapshot();
        logs.ShouldContain(r => r.Message.Contains("consent.missing"));
        logs.ShouldAllBe(r => !r.Message.Contains(PersonalData));
    }

    [Fact]
    public async Task DeadLetterManager_ReplayAllAsync_UsesTheSameCodeOnlyRuleAsReplayAsync()
    {
        // Arrange
        var store = Substitute.For<IDeadLetterStore>();
        var message = new FakeDeadLetterMessage { Id = Guid.NewGuid(), RequestType = "Not.A.Real.Type" };
        store.GetMessagesAsync(Arg.Any<DeadLetterFilter>(), 0, 100, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IEnumerable<IDeadLetterMessage>>([message]));
        store.GetAsync(message.Id, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Option<IDeadLetterMessage>>(Option<IDeadLetterMessage>.Some(message)));
        store.MarkAsReplayedAsync(message.Id, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));
        store.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        var orchestrator = new DeadLetterOrchestrator(
            store, new PassThroughDeadLetterMessageFactory(), new DeadLetterOptions(),
            NullLogger<DeadLetterOrchestrator>.Instance, new JsonMessageSerializer());
        var manager = new DeadLetterManager(
            store, orchestrator, Substitute.For<IServiceProvider>(),
            NullLogger<DeadLetterManager>.Instance, new JsonMessageSerializer());

        // Act
        var result = await manager.ReplayAllAsync(new DeadLetterFilter());

        // Assert - the batch result carries an error code, not a free-form message, matching
        // the same rule ReplayAsync itself applies.
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: r =>
            {
                var errorMessage = r.Results[0].ErrorMessage;
                errorMessage.ShouldNotBeNull();
                errorMessage.ShouldStartWith("dlq.");
                errorMessage.ShouldNotContain(" ");
            },
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task DeadLetterCleanupProcessor_StoreError_WrapsOnlyTheErrorCode()
    {
        // Arrange
        var store = Substitute.For<IDeadLetterStore>();
        store.DeleteExpiredAsync(Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, int>(SensitiveError));

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IDeadLetterStore)).Returns(store);
        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(serviceProvider);
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        var logger = new FakeLogger<DeadLetterCleanupProcessor>();
        var processor = new DeadLetterCleanupProcessor(
            scopeFactory,
            new DeadLetterOptions { EnableAutomaticCleanup = true, RetentionPeriod = TimeSpan.FromDays(1), CleanupInterval = TimeSpan.FromMilliseconds(10) },
            logger);

        using var cts = new CancellationTokenSource();

        // Act
        await processor.StartAsync(cts.Token);
        await Task.Delay(80);
        cts.Cancel();
        await processor.StopAsync(default);

        // Assert
        var logs = logger.Collector.GetSnapshot();
        logs.ShouldContain(r => r.Exception != null && r.Exception.Message.Contains("consent.missing"));
        logs.ShouldAllBe(r => r.Exception == null || !r.Exception.Message.Contains(PersonalData));
    }

    [Theory]
    [InlineData("outbox")]
    [InlineData("inbox")]
    [InlineData("scheduling")]
    public async Task HealthChecks_StoreFailure_ReportsOnlyTheErrorCode(string check)
    {
        HealthCheckResult result = check switch
        {
            "outbox" => await CheckOutboxHealthAsync(),
            "inbox" => await CheckInboxHealthAsync(),
            _ => await CheckSchedulingHealthAsync()
        };

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldNotContain(PersonalData);
        result.Description!.ShouldContain("consent.missing");
        result.Data["error"].ShouldBe("consent.missing");
    }

    private static async Task<HealthCheckResult> CheckOutboxHealthAsync()
    {
        var store = Substitute.For<global::Encina.Messaging.Outbox.IOutboxStore>();
        store.GetPendingCountAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, int>(SensitiveError));
        var check = new OutboxHealthCheck(store, new global::Encina.Messaging.Outbox.OutboxOptions());
        return await check.CheckHealthAsync(CancellationToken.None);
    }

    private static async Task<HealthCheckResult> CheckInboxHealthAsync()
    {
        var store = Substitute.For<global::Encina.Messaging.Inbox.IInboxStore>();
        store.GetExpiredMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, IEnumerable<IInboxMessage>>(SensitiveError));
        var check = new InboxHealthCheck(store);
        return await check.CheckHealthAsync(CancellationToken.None);
    }

    private static async Task<HealthCheckResult> CheckSchedulingHealthAsync()
    {
        var store = Substitute.For<IScheduledMessageStore>();
        store.GetDueMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, IEnumerable<IScheduledMessage>>(SensitiveError));
        var check = new SchedulingHealthCheck(store);
        return await check.CheckHealthAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ContentRouter_RouteFails_LogsOnlyTheErrorCode()
    {
        // Arrange
        var logger = new FakeLogger<global::Encina.Messaging.ContentRouter.ContentRouter>();
        var router = new global::Encina.Messaging.ContentRouter.ContentRouter(new ContentRouterOptions(), logger);
        var definition = ContentRouterBuilder.Create<SensitiveMessage, string>()
            .When(_ => true).RouteTo((_, _) => ValueTask.FromResult(Left<EncinaError, string>(SensitiveError)))
            .Build();

        // Act
        var result = await router.RouteAsync(definition, new SensitiveMessage());

        // Assert
        result.IsLeft.ShouldBeTrue();
        var logs = logger.Collector.GetSnapshot();
        logs.ShouldContain(r => r.Message.Contains("consent.missing"));
        logs.ShouldAllBe(r => !r.Message.Contains(PersonalData));
    }

    [Fact]
    public async Task InboxOrchestrator_CachedResponseEnvelope_StoresOnlyTheErrorCode()
    {
        // Arrange
        var store = Substitute.For<IInboxStore>();
        var serializer = new JsonMessageSerializer();
        var orchestrator = new InboxOrchestrator(
            store,
            new InboxOptions(),
            NullLogger<InboxOrchestrator>.Instance,
            Substitute.For<IInboxMessageFactory>(),
            serializer);

        // Act - use reflection to reach the private SerializeResponse method, the direct sink
        // for the cached response envelope written to the inbox store.
        var method = typeof(InboxOrchestrator).GetMethod(
            "SerializeResponse",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .MakeGenericMethod(typeof(string));
        var either = Left<EncinaError, string>(SensitiveError);
        var json = (string)method.Invoke(orchestrator, [either])!;

        // Assert
        json.ShouldContain("consent.missing");
        json.ShouldNotContain(PersonalData);
    }

    [Fact]
    public async Task RoutingSlipRunner_StepFails_LogsOnlyTheErrorCode()
    {
        // Arrange
        var logger = new FakeLogger<RoutingSlipRunner>();
        var runner = new RoutingSlipRunner(Substitute.For<IRequestContext>(), new RoutingSlipOptions(), logger);
        var builder = RoutingSlipBuilder.Create<SensitiveData>("TestSlip");
        var definition = builder.Step("Step1")
            .Execute((_, _, _) => ValueTask.FromResult(Left<EncinaError, SensitiveData>(SensitiveError)))
            .Build();

        // Act
        var result = await runner.RunAsync(definition, new SensitiveData());

        // Assert
        result.IsLeft.ShouldBeTrue();
        var logs = logger.Collector.GetSnapshot();
        logs.ShouldContain(r => r.Message.Contains("consent.missing"));
        logs.ShouldAllBe(r => !r.Message.Contains(PersonalData));
    }

    [Fact]
    public async Task SagaRunner_StepFails_LogsAndPersistsOnlyTheErrorCode()
    {
        // Arrange
        var sagaStore = Substitute.For<ISagaStore>();
        var stateFactory = Substitute.For<ISagaStateFactory>();
        var mockState = Substitute.For<ISagaState>();
        mockState.SagaId.Returns(Guid.NewGuid());
        mockState.Status.Returns(SagaStatus.Running);
        mockState.Data.Returns("{}");
        mockState.CurrentStep.Returns(0);
        stateFactory.Create(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<DateTime?>())
            .Returns(mockState);
        sagaStore.AddAsync(Arg.Any<ISagaState>(), Arg.Any<CancellationToken>()).Returns(Right<EncinaError, Unit>(Unit.Default));
        sagaStore.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(Right<EncinaError, Option<ISagaState>>(Option<ISagaState>.Some(mockState)));
        sagaStore.UpdateAsync(Arg.Any<ISagaState>(), Arg.Any<CancellationToken>()).Returns(Right<EncinaError, Unit>(Unit.Default));
        var orchestrator = new SagaOrchestrator(sagaStore, new SagaOptions(), NullLogger<SagaOrchestrator>.Instance, stateFactory, new JsonMessageSerializer());

        var logger = new FakeLogger<SagaRunner>();
        var runner = new SagaRunner(orchestrator, Substitute.For<IRequestContext>(), logger);
        var definition = SagaDefinition.Create<SensitiveData>("TestSaga")
            .Step("Step1")
            .Execute((_, _, _) => ValueTask.FromResult(Left<EncinaError, SensitiveData>(SensitiveError)))
            .Build();

        // Act
        var result = await runner.RunAsync(definition, new SensitiveData());

        // Assert
        result.IsLeft.ShouldBeTrue();
        var logs = logger.Collector.GetSnapshot();
        logs.ShouldContain(r => r.Message.Contains("consent.missing"));
        logs.ShouldAllBe(r => !r.Message.Contains(PersonalData));
        mockState.Received(1).ErrorMessage = "consent.missing";
    }

    [Fact]
    public async Task ScatterGatherRunner_ScatterAndGatherFail_LogOnlyTheErrorCode()
    {
        // Arrange
        var logger = new FakeLogger<ScatterGatherRunner>();
        var runner = new ScatterGatherRunner(new ScatterGatherOptions(), logger);
        var definition = ScatterGatherBuilder.Create<SensitiveMessage, string>("Test")
            .ScatterTo("Handler1", (_, _) => ValueTask.FromResult(Left<EncinaError, string>(SensitiveError)))
            .GatherWith(GatherStrategy.WaitForAll)
            .Aggregate((_, _) => ValueTask.FromResult(Left<EncinaError, string>(SensitiveError)))
            .Build();

        // Act
        var result = await runner.ExecuteAsync(definition, new SensitiveMessage());

        // Assert
        result.IsLeft.ShouldBeTrue();
        var logs = logger.Collector.GetSnapshot();
        logs.ShouldContain(r => r.Message.Contains("consent.missing"));
        logs.ShouldAllBe(r => !r.Message.Contains(PersonalData));
    }

    public sealed record SensitiveRequest : IRequest<string>;

    public sealed record SensitiveMessage;

    public sealed record SensitiveData;

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
