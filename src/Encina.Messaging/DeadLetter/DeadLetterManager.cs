using Encina.Diagnostics;
using Encina.Messaging.Diagnostics;
using Encina.Messaging.Serialization;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.Messaging.DeadLetter;

/// <summary>
/// Default implementation of <see cref="IDeadLetterManager"/>.
/// </summary>
/// <remarks>
/// <para>
/// Every store <c>Left</c> fails the operation that received it; none is turned into "not found" or
/// "not deleted". The replay outcome stored in the queue is an outcome code only, never error text.
/// </para>
/// <para>
/// Reads, replays and deletes default to the ambient <c>IRequestContext.TenantId</c> when there is one;
/// <see cref="DeadLetterFilter.AllTenants"/> opts out for operator tooling (logged by event, never silent).
/// </para>
/// <para>
/// Tenancy fails closed: when multi-tenancy is in use (<see cref="MultiTenancyMarker"/> is registered, which
/// <c>AddEncinaTenancy</c> does) and no tenant is resolved, an operation that names no tenant and does not set
/// <see cref="DeadLetterFilter.AllTenants"/> is denied with <see cref="DeadLetterErrorCodes.TenantRequired"/>
/// (an <c>encina.authorization.*</c> code, 403). Operations by message id and
/// <see cref="GetStatisticsAsync"/> have no filter to opt out with, so they are denied too. With no tenancy
/// registered the manager works across the whole queue when there is no ambient tenant.
/// </para>
/// </remarks>
public sealed class DeadLetterManager : IDeadLetterManager
{
    private readonly IDeadLetterStore _store;
    private readonly DeadLetterOrchestrator _orchestrator;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DeadLetterManager> _logger;
    private readonly IMessageSerializer _messageSerializer;
    private readonly DeadLetterOptions _options;
    private readonly IRequestContextAccessor _requestContextAccessor;
    private readonly TimeProvider _timeProvider;
    private readonly MultiTenancyMarker? _tenancy;

    private const string OpReplay = "replay";
    private const string OpReplayAll = "replay_all";
    private const string OpGet = "get";
    private const string OpGetMessages = "get_messages";
    private const string OpCount = "count";
    private const string OpStatistics = "statistics";
    private const string OpDelete = "delete";
    private const string OpDeleteAll = "delete_all";

    /// <summary>
    /// Initializes a new instance of the <see cref="DeadLetterManager"/> class.
    /// </summary>
    /// <param name="store">The dead letter store.</param>
    /// <param name="orchestrator">The orchestrator.</param>
    /// <param name="serviceProvider">The service provider for resolving IEncina.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="messageSerializer">
    /// The message serializer used to read back the persisted request payload, matching
    /// whatever serializer (plain or encrypting) wrote it.
    /// </param>
    /// <param name="options">The DLQ options (supplies <see cref="DeadLetterOptions.ReplayClaimTimeout"/>).</param>
    /// <param name="requestContextAccessor">The ambient request context accessor (tenant default).</param>
    /// <param name="timeProvider">Optional time provider for testability.</param>
    /// <param name="tenancy">
    /// The multi-tenancy marker, present when <c>AddEncinaTenancy</c> ran; when it is, a missing tenant denies
    /// (see the class remarks).
    /// </param>
    public DeadLetterManager(
        IDeadLetterStore store,
        DeadLetterOrchestrator orchestrator,
        IServiceProvider serviceProvider,
        ILogger<DeadLetterManager> logger,
        IMessageSerializer messageSerializer,
        DeadLetterOptions options,
        IRequestContextAccessor requestContextAccessor,
        TimeProvider? timeProvider = null,
        MultiTenancyMarker? tenancy = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(orchestrator);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(messageSerializer);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(requestContextAccessor);

        _store = store;
        _orchestrator = orchestrator;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _messageSerializer = messageSerializer;
        _options = options;
        _requestContextAccessor = requestContextAccessor;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _tenancy = tenancy;
    }

    // An empty tenant id is no tenant.
    private string? AmbientTenantId()
    {
        var tenantId = _requestContextAccessor.RequestContext?.TenantId;
        if (string.IsNullOrEmpty(tenantId))
            return null;

        // A padded tenant id would be one tenant on SQL Server and MySQL and another on PostgreSQL and MongoDB.
        DeadLetterInputs.RequireTrimmed(tenantId, "tenantId");
        return tenantId;
    }

    private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;

    /// <summary>
    /// Returns a copy of <paramref name="filter"/> (never the caller's instance) with the ambient tenant
    /// applied when the filter names no tenant, does not opt out and an ambient tenant exists; denies when
    /// tenancy is in use and no tenant is resolved. Throws <see cref="ArgumentException"/> for a non-UTC
    /// instant or an identity value with edge white space.
    /// </summary>
    private Either<EncinaError, DeadLetterFilter> Scoped(DeadLetterFilter? filter, string operation, bool? excludeReplayed = null)
    {
        var source = filter ?? new DeadLetterFilter();
        DeadLetterInputs.ValidateFilter(source);

        if (!TryResolveTenant(source, operation, out var tenantId))
        {
            return DenyTenantRequired(operation);
        }

        return source.CopyWith(tenantId, excludeReplayed ?? source.ExcludeReplayed);
    }

    // False when the operation must be denied: tenancy in use, no tenant named, no ambient tenant, no opt-out.
    private bool TryResolveTenant(DeadLetterFilter source, string operation, out string? tenantId)
    {
        // An empty tenant id names no tenant (the stores skip it), so it cannot satisfy the gate.
        tenantId = string.IsNullOrEmpty(source.TenantId) ? null : source.TenantId;
        if (tenantId is not null)
            return true;

        if (source.AllTenants)
        {
            if (_tenancy is not null)
            {
                DeadLetterLog.AllTenantsOptOut(_logger, operation);
            }

            return true;
        }

        tenantId = AmbientTenantId();
        return tenantId is not null || _tenancy is null;
    }

    // Operations by id and the statistics have no filter to opt out with.
    private bool TenantMissing() => _tenancy is not null && AmbientTenantId() is null;

    private EncinaError DenyTenantRequired(string operation)
    {
        DeadLetterLog.TenantRequiredDenied(_logger, operation, DeadLetterErrorCodes.TenantRequired);
        return EncinaErrors.Create(
            DeadLetterErrorCodes.TenantRequired,
            "Multi-tenancy is in use and no tenant is resolved");
    }

    private bool BelongsToAmbientTenant(IDeadLetterMessage message)
    {
        var tenantId = AmbientTenantId();
        return tenantId is null || string.Equals(message.TenantId, tenantId, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, ReplayResult>> ReplayAsync(
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        if (TenantMissing())
            return DenyTenantRequired(OpReplay);

        var messageResult = await _store.GetAsync(messageId, cancellationToken).ConfigureAwait(false);
        if (messageResult.IsLeft)
            return messageResult.LeftToArray()[0];

        var messageOpt = messageResult.RightToArray()[0];
        if (messageOpt.IsNone)
        {
            return NotFoundError(messageId);
        }

        var message = messageOpt.Match(Some: m => m, None: () => default!);

        // A message of another tenant is reported as not found, like a message that does not exist.
        if (!BelongsToAmbientTenant(message))
        {
            return NotFoundError(messageId);
        }

        var now = UtcNow();
        if (IsNotReplayable(message, message.Id, now, out var rejection))
        {
            return rejection;
        }

        return (await ClaimAndReplayAsync(message, now, cancellationToken).ConfigureAwait(false)).Outcome;
    }

    private static EncinaError NotFoundError(Guid messageId)
        => EncinaErrors.Create(DeadLetterErrorCodes.NotFound, $"Dead letter message {messageId} not found");

    /// <summary>
    /// The outcome of claiming and replaying one message. <c>MessageRejected</c> marks a <c>Left</c> that
    /// describes the message (it cannot be deserialized, there is no <c>IEncina</c>); any other <c>Left</c>
    /// is a store failure.
    /// </summary>
    private readonly record struct ReplayStep(Either<EncinaError, ReplayResult> Outcome, bool MessageRejected = false);

    // True when the exception is the caller's own cancellation; any other exception (including an
    // OperationCanceledException raised by the handler itself) is a failed replay.
    private static bool IsCallerCancellation(Exception ex, CancellationToken cancellationToken)
        => ex is OperationCanceledException && cancellationToken.IsCancellationRequested;

    private async Task<ReplayStep> ClaimAndReplayAsync(
        IDeadLetterMessage message,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var messageId = message.Id;

        var claim = await _store.TryClaimForReplayAsync(
            messageId,
            now - _options.ReplayClaimTimeout,
            cancellationToken).ConfigureAwait(false);
        if (claim.IsLeft)
            return new ReplayStep(claim.LeftToArray()[0]);

        // Another replay holds the claim: do not dispatch a second time.
        if (!claim.RightToArray()[0])
        {
            return new ReplayStep(ReplayResult.Failed(messageId, DeadLetterErrorCodes.ReplayInProgress));
        }

        DeadLetterLog.ReplayingMessage(_logger, messageId, message.RequestType);

        // Only the handler and the payload preparation turn an exception into a failed replay (below); an
        // exception thrown by the store is a store failure and propagates, so it cannot be recorded as a
        // handler failure of a replay that ran.
        return await ReplayStoredMessageAsync(message, messageId, cancellationToken).ConfigureAwait(false);
    }

    private static bool IsNotReplayable(IDeadLetterMessage message, Guid messageId, DateTime now, out EncinaError rejection)
    {
        if (message.IsReplayed)
        {
            rejection = EncinaErrors.Create(DeadLetterErrorCodes.AlreadyReplayed, $"Message {messageId} has already been replayed");
            return true;
        }

        if (message.IsExpiredAt(now))
        {
            rejection = EncinaErrors.Create(DeadLetterErrorCodes.Expired, $"Message {messageId} has expired");
            return true;
        }

        rejection = default;
        return false;
    }

    // Right(true) when the outcome was recorded; Right(false) when the row already held an outcome;
    // Left when the store failed (logged by error code only).
    private async Task<Either<EncinaError, bool>> RecordReplayOutcomeAsync(
        Guid messageId,
        string outcomeCode,
        CancellationToken cancellationToken)
    {
        var marked = await _store.MarkAsReplayedAsync(messageId, outcomeCode, cancellationToken).ConfigureAwait(false);
        if (marked.IsLeft)
            return LogOutcomeNotRecorded(messageId, marked.LeftToArray()[0], "mark_replayed");

        var saved = await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        if (saved.IsLeft)
            return LogOutcomeNotRecorded(messageId, saved.LeftToArray()[0], "save_changes");

        return marked.RightToArray()[0];
    }

    private EncinaError LogOutcomeNotRecorded(Guid messageId, EncinaError error, string operation)
    {
        var errorCode = error.GetCode().IfNone("encina.unknown");
        DeadLetterMetrics.RecordStoreFailure(operation, errorCode);
        DeadLetterLog.ReplayOutcomeNotRecorded(_logger, messageId, errorCode);
        return error;
    }

    // Records the outcome, then returns the replay result; a store Left fails the replay, and a row that
    // already held an outcome (a concurrent replay recorded first) reports dlq.already_replayed.
    private async Task<Either<EncinaError, ReplayResult>> FinishReplayAsync(
        Guid messageId,
        ReplayResult result,
        string outcomeCode,
        CancellationToken cancellationToken)
    {
        var recorded = await RecordReplayOutcomeAsync(messageId, outcomeCode, cancellationToken).ConfigureAwait(false);
        if (recorded.IsLeft)
            return recorded.LeftToArray()[0];

        if (!recorded.RightToArray()[0])
            return ReplayResult.Failed(messageId, DeadLetterErrorCodes.AlreadyReplayed);

        DeadLetterMetrics.RecordReplayed(result.Success);
        return result;
    }

    private async Task<ReplayStep> ReplayStoredMessageAsync(
        IDeadLetterMessage message,
        Guid messageId,
        CancellationToken cancellationToken)
    {
        var plan = PrepareReplay(message);
        if (plan.ErrorCode is not null)
        {
            var recorded = await RecordReplayOutcomeAsync(messageId, plan.ErrorCode, cancellationToken).ConfigureAwait(false);
            if (recorded.IsLeft)
                return new ReplayStep(recorded.LeftToArray()[0]);

            DeadLetterMetrics.RecordReplayed(succeeded: false);
            return new ReplayStep(EncinaErrors.Create(plan.ErrorCode, plan.ErrorText!), MessageRejected: true);
        }

        // Replay through IEncina.Publish (notifications) or IEncina.Send, typed by the runtime type
        var attempt = await ReplayRequestAsync(plan.Encina!, plan.Request!, messageId, cancellationToken).ConfigureAwait(false);

        return new ReplayStep(await FinishReplayAsync(messageId, attempt.Result, attempt.OutcomeCode, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Deserializes the stored request and resolves <see cref="IEncina"/>; <c>ErrorCode</c> is set
    /// (and the other members are not) when the message cannot be replayed.
    /// </summary>
    private ReplayPlan PrepareReplay(IDeadLetterMessage message)
    {
        try
        {
            return PrepareReplayCore(message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            DeadLetterLog.MessageReplayException(_logger, ex.ForLogging(), message.Id);
            return ReplayPlan.Rejected(DeadLetterErrorCodes.ReplayFailed, $"Exception during replay: {ex.GetType().FullName}");
        }
    }

    private ReplayPlan PrepareReplayCore(IDeadLetterMessage message)
    {
        // Deserialize the request
        var requestType = Type.GetType(message.RequestType);
        if (requestType is null)
        {
            return ReplayPlan.Rejected(DeadLetterErrorCodes.DeserializationFailed, $"Cannot resolve type: {message.RequestType}");
        }

        var request = _messageSerializer.Deserialize(message.RequestContent, requestType);
        if (request is null)
        {
            return ReplayPlan.Rejected(DeadLetterErrorCodes.DeserializationFailed, "Failed to deserialize request content");
        }

        // Get IEncina to replay the request
        var encina = _serviceProvider.GetService(typeof(IEncina)) as IEncina;
        if (encina is null)
        {
            return ReplayPlan.Rejected(DeadLetterErrorCodes.ReplayFailed, "IEncina service not available");
        }

        return new ReplayPlan(request, encina, null, null);
    }

    private readonly record struct ReplayPlan(object? Request, IEncina? Encina, string? ErrorCode, string? ErrorText)
    {
        public static ReplayPlan Rejected(string errorCode, string errorText) => new(null, null, errorCode, errorText);
    }

    private readonly record struct ReplayAttempt(ReplayResult Result, string OutcomeCode);

    private async Task<ReplayAttempt> ReplayRequestAsync(
        IEncina encina,
        object request,
        Guid messageId,
        CancellationToken cancellationToken)
    {
        try
        {
            var outcome = await DispatchAsync(encina, request, cancellationToken).ConfigureAwait(false);

            // A Left outcome is a failed replay: the request ran and its handler (or a behavior) failed.
            // Only the error code travels: EncinaError.Message can carry personal data, and this
            // reaches the log, the stored outcome and the ReplayResult returned to the caller (#1259 review).
            if (outcome.IsLeft)
            {
                var errorCode = outcome.Match(Right: _ => string.Empty, Left: error => error.GetCode().IfNone("unknown"));
                return FailedAttempt(messageId, $"Replay failed: {errorCode}", errorCode);
            }

            DeadLetterLog.MessageReplayedSuccessfully(_logger, messageId);
            return new ReplayAttempt(ReplayResult.Succeeded(messageId), DeadLetterErrorCodes.ReplaySucceeded);
        }
        catch (Exception ex) when (!IsCallerCancellation(ex, cancellationToken))
        {
            var innerException = ex.InnerException ?? ex;
            return FailedAttempt(messageId, $"Replay failed: {innerException.GetType().FullName}", DeadLetterErrorCodes.ReplayFailed);
        }
    }

    // A notification (an outbox dead letter) is published again; anything else is sent as a request.
    private static ValueTask<Either<EncinaError, Unit>> DispatchAsync(IEncina encina, object request, CancellationToken cancellationToken)
        => request is INotification notification
            ? encina.Publish(notification, cancellationToken)
            : RuntimeTypeRequestDispatcher.SendAsync(encina, request, cancellationToken);

    private ReplayAttempt FailedAttempt(Guid messageId, string error, string outcomeCode)
    {
        DeadLetterLog.MessageReplayFailed(_logger, messageId, error);
        return new ReplayAttempt(ReplayResult.Failed(messageId, error), Truncate(outcomeCode));
    }

    private static string Truncate(string outcomeCode)
        => outcomeCode.Length <= DeadLetterStoreLimits.ReplayResultMaxLength
            ? outcomeCode
            : outcomeCode[..DeadLetterStoreLimits.ReplayResultMaxLength];

    /// <inheritdoc />
    public async Task<Either<EncinaError, BatchReplayResult>> ReplayAllAsync(
        DeadLetterFilter filter,
        int maxMessages = 100,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxMessages, 1);

        // Only non-replayed messages; the caller's filter is copied, never mutated.
        var scoped = Scoped(filter, OpReplayAll, excludeReplayed: true);
        if (scoped.IsLeft)
            return scoped.LeftToArray()[0];

        var take = Math.Min(maxMessages, DeadLetterStoreLimits.MaxPageSize);

        var messagesResult = await _store.GetMessagesAsync(scoped.RightToArray()[0], 0, take, false, cancellationToken).ConfigureAwait(false);
        if (messagesResult.IsLeft)
            return messagesResult.LeftToArray()[0];

        var messages = messagesResult.RightToArray()[0].ToList();
        var results = new List<ReplayResult>();

        DeadLetterLog.BatchReplayStarted(_logger, messages.Count);

        foreach (var message in messages)
        {
            // The caller's cancellation propagates, between messages as well as inside one.
            cancellationToken.ThrowIfCancellationRequested();

            var step = await ReplayOneOfBatchAsync(message, cancellationToken).ConfigureAwait(false);

            // A store failure is not a per-message result: it fails the whole operation with that error,
            // after the messages already replayed (their outcomes are recorded) are logged.
            if (step.Outcome.IsLeft && !step.MessageRejected)
            {
                var error = step.Outcome.LeftToArray()[0];
                DeadLetterLog.BatchReplayAborted(
                    _logger,
                    results.Count,
                    results.Count(r => r.Success),
                    results.Count(r => !r.Success),
                    error.GetCode().IfNone(DeadLetterErrorCodes.ReplayFailed));
                return error;
            }

            results.Add(step.Outcome.Match(
                Right: r => r,
                // Only the error code: EncinaError.Message can carry personal data (#1259 review).
                Left: error => ReplayResult.Failed(message.Id, error.GetCode().IfNone(DeadLetterErrorCodes.ReplayFailed))));
        }

        var batchResult = new BatchReplayResult
        {
            TotalProcessed = results.Count,
            SuccessCount = results.Count(r => r.Success),
            FailureCount = results.Count(r => !r.Success),
            Results = results
        };

        DeadLetterLog.BatchReplayCompleted(_logger, batchResult.TotalProcessed, batchResult.SuccessCount, batchResult.FailureCount);

        return batchResult;
    }

    // A message that is already replayed or expired is a per-message rejection in a batch, like any other.
    private async Task<ReplayStep> ReplayOneOfBatchAsync(IDeadLetterMessage message, CancellationToken cancellationToken)
    {
        var now = UtcNow();
        return IsNotReplayable(message, message.Id, now, out var rejection)
            ? new ReplayStep(rejection, MessageRejected: true)
            : await ClaimAndReplayAsync(message, now, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Option<IDeadLetterMessage>>> GetMessageAsync(
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        if (TenantMissing())
            return DenyTenantRequired(OpGet);

        var result = await _store.GetAsync(messageId, cancellationToken).ConfigureAwait(false);

        // A message of another tenant reads as not found.
        return result.Map(found => found.Filter(BelongsToAmbientTenant));
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, IEnumerable<IDeadLetterMessage>>> GetMessagesAsync(
        DeadLetterFilter? filter = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        var scoped = Scoped(filter, OpGetMessages);
        if (scoped.IsLeft)
            return scoped.LeftToArray()[0];

        return await _store.GetMessagesAsync(scoped.RightToArray()[0], skip, take, false, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, int>> GetCountAsync(
        DeadLetterFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        var scoped = Scoped(filter, OpCount);
        if (scoped.IsLeft)
            return scoped.LeftToArray()[0];

        return await _store.GetCountAsync(scoped.RightToArray()[0], cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, DeadLetterStatistics>> GetStatisticsAsync(
        CancellationToken cancellationToken = default)
    {
        if (TenantMissing())
        {
            return Task.FromResult<Either<EncinaError, DeadLetterStatistics>>(DenyTenantRequired(OpStatistics));
        }

        return _orchestrator.GetStatisticsAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> DeleteAsync(
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        if (TenantMissing())
            return DenyTenantRequired(OpDelete);

        var visible = await IsVisibleAsync(messageId, cancellationToken).ConfigureAwait(false);
        if (visible.IsLeft)
            return visible.LeftToArray()[0];

        if (!visible.RightToArray()[0])
        {
            return NotDeletedError(messageId);
        }

        var deleteResult = await _store.DeleteAsync(messageId, cancellationToken).ConfigureAwait(false);
        if (deleteResult.IsLeft)
            return deleteResult.LeftToArray()[0];

        if (!deleteResult.RightToArray()[0])
            return NotDeletedError(messageId);

        DeadLetterMetrics.RecordDeleted(1, DeadLetterMetrics.ReasonManual);
        return Unit.Default;
    }

    private static EncinaError NotDeletedError(Guid messageId)
        => EncinaErrors.Create(DeadLetterErrorCodes.DeleteFailed, $"Dead letter message {messageId} not found or could not be deleted");

    // True when there is no ambient tenant, the message does not exist (the delete reports that) or the
    // message belongs to the ambient tenant.
    private async Task<Either<EncinaError, bool>> IsVisibleAsync(Guid messageId, CancellationToken cancellationToken)
    {
        if (AmbientTenantId() is null)
            return true;

        var result = await _store.GetAsync(messageId, cancellationToken).ConfigureAwait(false);
        if (result.IsLeft)
            return result.LeftToArray()[0];

        return result.RightToArray()[0].Match(Some: BelongsToAmbientTenant, None: () => true);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, int>> DeleteAllAsync(
        DeadLetterFilter filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var scoped = Scoped(filter, OpDeleteAll);
        if (scoped.IsLeft)
            return scoped.LeftToArray()[0];

        var deleted = await _store.DeleteManyAsync(scoped.RightToArray()[0], cancellationToken).ConfigureAwait(false);
        if (deleted.IsLeft)
            return deleted.LeftToArray()[0];

        var saved = await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        if (saved.IsLeft)
            return saved.LeftToArray()[0];

        var count = deleted.RightToArray()[0];
        DeadLetterMetrics.RecordDeleted(count, DeadLetterMetrics.ReasonManual);
        if (count > 0)
        {
            DeadLetterLog.MessagesDeleted(_logger, count);
        }

        return count;
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, int>> CleanupExpiredAsync(
        CancellationToken cancellationToken = default)
    {
        return _orchestrator.CleanupExpiredAsync(cancellationToken);
    }
}
