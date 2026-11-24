using Encina.Messaging.DeadLetter;
using Encina.Messaging.Inbox;
using Encina.Messaging.Recoverability;
using Encina.Messaging.Serialization;
using Encina.Recoverability;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using Shouldly;

using static LanguageExt.Prelude;

#pragma warning disable CA2012 // NSubstitute setup of ValueTask-returning members

namespace Encina.UnitTests.Messaging;

/// <summary>
/// The category of every error the dead letter queue, inbox, outbox and recoverability producers return
/// (ADR-062, #3724 slice M2a), read through <c>GetCategory()</c> from the real producers.
/// </summary>
/// <remarks>
/// Not reachable through a public result, so the completeness static scan (<c>ErrorCategoryCompletenessStaticScanTests</c>)
/// is their check: <c>outbox.unknown_notification_type</c> (consumed by the batch processor, which keeps only its code),
/// the variable-code error of <c>OutboxBatchProcessor</c> and the <c>inbox.processing_failed</c> error of the inbox
/// dead letter capture (both embedded in a <c>DeadLetterContext</c>, read for their code alone) and
/// <c>recoverability.schedule_retry_failed</c> (the delayed retry processor logs its code and answers a boolean).
/// <c>dlq.capture_rejected</c>, <c>inbox.max_retries_exceeded</c>, <c>inbox.cached_error</c> and
/// <c>recoverability.retry_after_exceeds_ceiling</c> are pending a maintainer decision and keep the category they
/// have today, <c>Internal</c>.
/// </remarks>
public sealed class MessagingInfrastructureErrorCategoryTests
{
    private static readonly DateTime FixedUtcNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    public sealed record ReplayedCommand(int Value) : IRequest<int>;

    private sealed record InboxRequest(string Value);

    private sealed record InboxResponse(string Value);

    private sealed record DeadLetterRequest(string Value);

    private sealed record RecoveredRequest(int Value) : IRequest<string>;

    #region Dead letter manager

    [Fact]
    public async Task DeadLetterManager_MultiTenancyAndNoTenant_IsInvalidRequest()
    {
        var (manager, _, _) = NewManager(tenancy: true);

        var result = await manager.GetMessagesAsync(new DeadLetterFilter());

        CodeOf(result).ShouldBe(DeadLetterErrorCodes.TenantRequired);
        CategoryOf(result).ShouldBe(ErrorCategory.InvalidRequest);
    }

    [Fact]
    public async Task DeadLetterManager_ReplayOfAnUnknownMessage_IsNotFound()
    {
        var (manager, store, _) = NewManager();
        var id = Guid.NewGuid();
        store.GetAsync(id, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Option<IDeadLetterMessage>>(Option<IDeadLetterMessage>.None));

        var result = await manager.ReplayAsync(id);

        CodeOf(result).ShouldBe(DeadLetterErrorCodes.NotFound);
        CategoryOf(result).ShouldBe(ErrorCategory.NotFound);
    }

    [Fact]
    public async Task DeadLetterManager_ReplayOfAnAlreadyReplayedMessage_IsConflict()
    {
        var (manager, store, _) = NewManager();
        var message = Message(isReplayed: true);
        Arrange(store, message);

        var result = await manager.ReplayAsync(message.Id);

        CodeOf(result).ShouldBe(DeadLetterErrorCodes.AlreadyReplayed);
        CategoryOf(result).ShouldBe(ErrorCategory.Conflict);
    }

    [Fact]
    public async Task DeadLetterManager_ReplayOfAnExpiredMessage_IsConflict()
    {
        var (manager, store, _) = NewManager();
        var message = Message(isExpired: true);
        Arrange(store, message);

        var result = await manager.ReplayAsync(message.Id);

        CodeOf(result).ShouldBe(DeadLetterErrorCodes.Expired);
        CategoryOf(result).ShouldBe(ErrorCategory.Conflict);
    }

    [Fact]
    public async Task DeadLetterManager_ReplayOfAnUnresolvableType_IsInternal()
    {
        var (manager, store, _) = NewManager();
        var message = Message(requestType: "NonExistent.Type, NonExistent.Assembly");
        Arrange(store, message);

        var result = await manager.ReplayAsync(message.Id);

        CodeOf(result).ShouldBe(DeadLetterErrorCodes.DeserializationFailed);
        CategoryOf(result).ShouldBe(ErrorCategory.Internal);
    }

    [Fact]
    public async Task DeadLetterManager_ReplayWithoutIEncina_IsInternal()
    {
        var (manager, store, _) = NewManager();
        var message = Message(requestType: typeof(ReplayedCommand).AssemblyQualifiedName!);
        Arrange(store, message);

        var result = await manager.ReplayAsync(message.Id);

        CodeOf(result).ShouldBe(DeadLetterErrorCodes.ReplayFailed);
        CategoryOf(result).ShouldBe(ErrorCategory.Internal);
    }

    [Fact]
    public async Task DeadLetterManager_DeleteOfAMissingMessage_IsNotFound()
    {
        var (manager, store, _) = NewManager();
        var id = Guid.NewGuid();
        store.DeleteAsync(id, Arg.Any<CancellationToken>()).Returns(Right<EncinaError, bool>(false));

        var result = await manager.DeleteAsync(id);

        CodeOf(result).ShouldBe(DeadLetterErrorCodes.DeleteFailed);
        CategoryOf(result).ShouldBe(ErrorCategory.NotFound);
    }

    #endregion

    #region Dead letter orchestrator and source capture

    [Fact]
    public async Task DeadLetterOrchestrator_DuplicateWhoseOriginalWasDeleted_IsInternal()
    {
        var store = Substitute.For<IDeadLetterStore>();
        var factory = Substitute.For<IDeadLetterMessageFactory>();
        var incoming = Message();
        factory.Create(Arg.Any<DeadLetterData>()).Returns(incoming);
        store.AddAsync(incoming, Arg.Any<CancellationToken>()).Returns(Right<EncinaError, bool>(false));
        store.GetMessagesAsync(Arg.Any<DeadLetterFilter>(), 0, 1, false, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IEnumerable<IDeadLetterMessage>>(System.Array.Empty<IDeadLetterMessage>()));
        var orchestrator = new DeadLetterOrchestrator(
            store, factory, new DeadLetterOptions(), NullLogger<DeadLetterOrchestrator>.Instance,
            new JsonMessageSerializer(), Substitute.For<IRequestContextAccessor>());

        var context = new DeadLetterContext(EncinaErrors.Create("e", "m"), null, "Outbox", 1, FixedUtcNow);
        var result = await orchestrator.AddAsync(new DeadLetterRequest("v"), context);

        CodeOf(result).ShouldBe(DeadLetterErrorCodes.StoreFailed);
        CategoryOf(result).ShouldBe(ErrorCategory.Internal);
    }

    [Fact]
    public async Task DeadLetterSourceCapture_WhenTheOrchestratorIsNotRegistered_IsInternal()
    {
        var capture = NewCapture(new DeadLetterOptions { IntegrateWithOutbox = true });

        var result = await capture.CaptureAsync(
            new DeadLetterRequest("v"),
            new DeadLetterContext(EncinaErrors.Create("e", "m"), null, DeadLetterSourcePatterns.Outbox, 1, FixedUtcNow));

        CodeOf(result).ShouldBe(DeadLetterErrorCodes.CaptureFailed);
        CategoryOf(result).ShouldBe(ErrorCategory.Internal);
    }

    [Fact]
    public async Task DeadLetterSourceCapture_ForASourcePatternWithoutAFlag_IsInternalAndPendingDecision()
    {
        var capture = NewCapture(new DeadLetterOptions());

        var result = await capture.CaptureAsync(
            new DeadLetterRequest("v"),
            new DeadLetterContext(EncinaErrors.Create("e", "m"), null, "Custom", 1, FixedUtcNow));

        CodeOf(result).ShouldBe(DeadLetterErrorCodes.CaptureRejected);
        CategoryOf(result).ShouldBe(ErrorCategory.Internal);
    }

    #endregion

    #region Inbox

    [Fact]
    public void InboxOrchestrator_WithoutAMessageId_IsInvalidRequest()
    {
        var orchestrator = NewInbox(Substitute.For<IInboxStore>());

        var error = orchestrator.ValidateMessageId(null, "Req", "corr")
            .IfNone(() => throw new InvalidOperationException("Expected an error."));

        error.GetCode().IfNone(string.Empty).ShouldBe(InboxErrorCodes.MissingMessageId);
        error.GetCategory().ShouldBe(ErrorCategory.InvalidRequest);
    }

    [Fact]
    public async Task InboxOrchestrator_WhenTheHandlerThrows_IsInternal()
    {
        var store = Substitute.For<IInboxStore>();
        store.GetMessageAsync("m-1", Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Option<IInboxMessage>>(Option<IInboxMessage>.None));
        store.AddAsync(Arg.Any<IInboxMessage>(), Arg.Any<CancellationToken>()).Returns(Right<EncinaError, Unit>(unit));
        store.MarkAsFailedAsync("m-1", Arg.Any<string>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(unit));
        var orchestrator = NewInbox(store);

        var result = await orchestrator.ProcessAsync<InboxResponse>(
            new InboxRequest("v"), "m-1", "Req", "corr", null, () => throw new InvalidOperationException("boom"));

        CodeOf(result).ShouldBe("inbox.processing_failed");
        CategoryOf(result).ShouldBe(ErrorCategory.Internal);
    }

    [Fact]
    public async Task InboxOrchestrator_WhenRetriesAreUsedUp_IsInternalAndPendingDecision()
    {
        var store = Substitute.For<IInboxStore>();
        var existing = StoredInbox(retryCount: 3, isProcessed: false, response: null);
        store.GetMessageAsync("m-1", Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Option<IInboxMessage>>(Option<IInboxMessage>.Some(existing)));
        var orchestrator = NewInbox(store);

        var result = await orchestrator.ProcessAsync<InboxResponse>(
            new InboxRequest("v"), "m-1", "Req", "corr", null,
            () => ValueTask.FromResult<Either<EncinaError, InboxResponse>>(new InboxResponse("ok")));

        CodeOf(result).ShouldBe(InboxErrorCodes.MaxRetriesExceeded);
        CategoryOf(result).ShouldBe(ErrorCategory.Internal);
    }

    [Fact]
    public async Task InboxOrchestrator_WhenTheCachedResponseCannotBeDeserialized_IsInternal()
    {
        var store = Substitute.For<IInboxStore>();
        var existing = StoredInbox(retryCount: 0, isProcessed: true, response: "null");
        store.GetMessageAsync("m-1", Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Option<IInboxMessage>>(Option<IInboxMessage>.Some(existing)));
        var orchestrator = NewInbox(store);

        var result = await orchestrator.ProcessAsync<InboxResponse>(
            new InboxRequest("v"), "m-1", "Req", "corr", null,
            () => ValueTask.FromResult<Either<EncinaError, InboxResponse>>(new InboxResponse("ok")));

        CodeOf(result).ShouldBe(InboxErrorCodes.DeserializationFailed);
        CategoryOf(result).ShouldBe(ErrorCategory.Internal);
    }

    [Fact]
    public async Task InboxOrchestrator_ReplayOfACachedHandlerLeft_IsInternalAndPendingDecision()
    {
        var store = Substitute.For<IInboxStore>();
        var cached = new JsonMessageSerializer().Serialize(new { IsSuccess = false, ErrorMessage = "biz.rule" });
        var existing = StoredInbox(retryCount: 0, isProcessed: true, response: cached);
        store.GetMessageAsync("m-1", Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Option<IInboxMessage>>(Option<IInboxMessage>.Some(existing)));
        var orchestrator = NewInbox(store);

        var result = await orchestrator.ProcessAsync<InboxResponse>(
            new InboxRequest("v"), "m-1", "Req", "corr", null,
            () => ValueTask.FromResult<Either<EncinaError, InboxResponse>>(new InboxResponse("ok")));

        CodeOf(result).ShouldBe(InboxErrorCodes.CachedError);
        CategoryOf(result).ShouldBe(ErrorCategory.Internal);
    }

    #endregion

    #region Recoverability pipeline

    [Fact]
    public async Task RecoverabilityPipeline_WhenTheHandlerThrows_IsInternal()
    {
        var behavior = NewBehavior(new RecoverabilityOptions { ImmediateRetries = 0, EnableDelayedRetries = false });

        var result = await behavior.Handle(
            new RecoveredRequest(1), Context(), () => throw new TimeoutException("boom"), CancellationToken.None);

        CodeOf(result).ShouldBe(RecoverabilityErrorCodes.ExceptionThrown);
        CategoryOf(result).ShouldBe(ErrorCategory.Internal);
    }

    [Fact]
    public async Task RecoverabilityPipeline_WhenRetryAfterExceedsTheCeiling_IsInternalAndPendingDecision()
    {
        var scheduler = Substitute.For<IDelayedRetryScheduler>();
        var options = new RecoverabilityOptions
        {
            ImmediateRetries = 0,
            EnableDelayedRetries = true,
            DelayedRetries = [TimeSpan.FromMinutes(1)],
            MaxRetryAfter = TimeSpan.FromHours(1)
        };
        var behavior = NewBehavior(options, scheduler);
        var details = new Dictionary<string, object?>
        {
            [OutboundErrorMetadataKeys.Transient] = true,
            [OutboundErrorMetadataKeys.HttpRetryAfter] = TimeSpan.FromHours(2)
        };

        var result = await behavior.Handle(
            new RecoveredRequest(1),
            Context(),
            () => ValueTask.FromResult<Either<EncinaError, string>>(
                EncinaErrors.Create("upstream.busy", ErrorCategory.Unavailable, "busy", details: details)),
            CancellationToken.None);

        CodeOf(result).ShouldBe(RecoverabilityErrorCodes.RetryAfterExceedsCeiling);
        CategoryOf(result).ShouldBe(ErrorCategory.Internal);
    }

    #endregion

    #region Helpers

    private static (DeadLetterManager Manager, IDeadLetterStore Store, IServiceProvider ServiceProvider) NewManager(bool tenancy = false)
    {
        var store = Substitute.For<IDeadLetterStore>();
        store.TryClaimForReplayAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, bool>(true));
        store.MarkAsReplayedAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, bool>(true));
        store.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Right<EncinaError, Unit>(unit));

        var accessor = Substitute.For<IRequestContextAccessor>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        var orchestrator = new DeadLetterOrchestrator(
            store, Substitute.For<IDeadLetterMessageFactory>(), new DeadLetterOptions(),
            NullLogger<DeadLetterOrchestrator>.Instance, new JsonMessageSerializer(), accessor);
        var manager = new DeadLetterManager(
            store,
            orchestrator,
            serviceProvider,
            NullLogger<DeadLetterManager>.Instance,
            new JsonMessageSerializer(),
            new DeadLetterOptions(),
            accessor,
            new FakeTimeProvider(new DateTimeOffset(FixedUtcNow)),
            tenancy ? new MultiTenancyMarker() : null);

        return (manager, store, serviceProvider);
    }

    private static IDeadLetterMessage Message(
        bool isReplayed = false,
        bool isExpired = false,
        string requestType = "System.Object, System.Runtime")
    {
        var message = Substitute.For<IDeadLetterMessage>();
        message.Id.Returns(Guid.NewGuid());
        message.IsReplayed.Returns(isReplayed);
        message.IsExpiredAt(Arg.Any<DateTime>()).Returns(isExpired);
        message.RequestType.Returns(requestType);
        message.RequestContent.Returns("{\"value\":5}");
        message.ErrorCode.Returns("test.error");
        message.DeadLetteredAtUtc.Returns(FixedUtcNow);
        return message;
    }

    private static void Arrange(IDeadLetterStore store, IDeadLetterMessage message)
        => store.GetAsync(message.Id, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Option<IDeadLetterMessage>>(Option<IDeadLetterMessage>.Some(message)));

    private static DeadLetterSourceCapture NewCapture(DeadLetterOptions options)
        => new(
            new ServiceCollection().BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
            options,
            NullLogger<DeadLetterSourceCapture>.Instance);

    private static InboxOrchestrator NewInbox(IInboxStore store)
    {
        var factory = Substitute.For<IInboxMessageFactory>();
        factory.Create(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<InboxMetadata?>())
            .Returns(_ => Substitute.For<IInboxMessage>());

        return new InboxOrchestrator(
            store,
            new InboxOptions { MaxRetries = 3 },
            NullLogger<InboxOrchestrator>.Instance,
            factory,
            new JsonMessageSerializer());
    }

    private static IInboxMessage StoredInbox(int retryCount, bool isProcessed, string? response)
    {
        var message = Substitute.For<IInboxMessage>();
        message.RetryCount.Returns(retryCount);
        message.IsProcessed.Returns(isProcessed);
        message.Response.Returns(response);
        message.ReceivedAtUtc.Returns(FixedUtcNow);
        return message;
    }

    private static RecoverabilityPipelineBehavior<RecoveredRequest, string> NewBehavior(
        RecoverabilityOptions options,
        IDelayedRetryScheduler? scheduler = null)
        => new(
            Options.Create(options),
            NullLogger<RecoverabilityPipelineBehavior<RecoveredRequest, string>>.Instance,
            scheduler);

    private static IRequestContext Context()
    {
        var context = Substitute.For<IRequestContext>();
        context.CorrelationId.Returns("test-correlation");
        context.IdempotencyKey.Returns("test-idempotency-key");
        return context;
    }

    private static string CodeOf<T>(Either<EncinaError, T> result)
        => result.Match(Right: _ => throw new InvalidOperationException("Expected a Left."), Left: e => e.GetCode().IfNone(string.Empty));

    private static ErrorCategory CategoryOf<T>(Either<EncinaError, T> result)
        => result.Match(Right: _ => throw new InvalidOperationException("Expected a Left."), Left: e => e.GetCategory());

    #endregion
}
