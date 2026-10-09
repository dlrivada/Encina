using Encina.Diagnostics;
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
/// <see cref="DeadLetterFilter.AllTenants"/> opts out for operator tooling. With no ambient tenant the
/// manager works across the whole queue.
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
    public DeadLetterManager(
        IDeadLetterStore store,
        DeadLetterOrchestrator orchestrator,
        IServiceProvider serviceProvider,
        ILogger<DeadLetterManager> logger,
        IMessageSerializer messageSerializer,
        DeadLetterOptions options,
        IRequestContextAccessor requestContextAccessor,
        TimeProvider? timeProvider = null)
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
    }

    // An empty tenant id is no tenant.
    private string? AmbientTenantId()
    {
        var tenantId = _requestContextAccessor.RequestContext?.TenantId;
        return string.IsNullOrEmpty(tenantId) ? null : tenantId;
    }

    private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;

    /// <summary>
    /// Returns a copy of <paramref name="filter"/> (never the caller's instance) with the ambient tenant
    /// applied when the filter names no tenant, does not opt out and an ambient tenant exists.
    /// </summary>
    private DeadLetterFilter Scoped(DeadLetterFilter? filter, bool? excludeReplayed = null)
    {
        var tenantId = filter?.TenantId;
        if (tenantId is null && filter?.AllTenants != true)
        {
            tenantId = AmbientTenantId();
        }

        return new DeadLetterFilter
        {
            SourcePattern = filter?.SourcePattern,
            RequestType = filter?.RequestType,
            ErrorCode = filter?.ErrorCode,
            CorrelationId = filter?.CorrelationId,
            ExcludeReplayed = excludeReplayed ?? filter?.ExcludeReplayed,
            DeadLetteredAfterUtc = filter?.DeadLetteredAfterUtc,
            DeadLetteredBeforeUtc = filter?.DeadLetteredBeforeUtc,
            TenantId = tenantId,
            SourceMessageId = filter?.SourceMessageId,
            ExpiresAtOrBeforeUtc = filter?.ExpiresAtOrBeforeUtc,
            AllTenants = filter?.AllTenants ?? false
        };
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

        return await ReplayLoadedAsync(message, cancellationToken).ConfigureAwait(false);
    }

    private static EncinaError NotFoundError(Guid messageId)
        => EncinaErrors.Create(DeadLetterErrorCodes.NotFound, $"Dead letter message {messageId} not found");

    private async Task<Either<EncinaError, ReplayResult>> ReplayLoadedAsync(
        IDeadLetterMessage message,
        CancellationToken cancellationToken)
    {
        var messageId = message.Id;
        var now = UtcNow();

        if (IsNotReplayable(message, messageId, now, out var rejection))
        {
            return rejection;
        }

        var claim = await _store.TryClaimForReplayAsync(
            messageId,
            now - _options.ReplayClaimTimeout,
            cancellationToken).ConfigureAwait(false);
        if (claim.IsLeft)
            return claim.LeftToArray()[0];

        // Another replay holds the claim: do not dispatch a second time.
        if (!claim.RightToArray()[0])
        {
            return ReplayResult.Failed(messageId, DeadLetterErrorCodes.ReplayInProgress);
        }

        DeadLetterLog.ReplayingMessage(_logger, messageId, message.RequestType);

        try
        {
            return await ReplayStoredMessageAsync(message, messageId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            DeadLetterLog.MessageReplayException(_logger, ex.ForLogging(), messageId);

            var errorMessage = $"[{DeadLetterErrorCodes.ReplayFailed}] Exception during replay: {ex.GetType().FullName}";
            return await FinishReplayAsync(
                messageId,
                ReplayResult.Failed(messageId, errorMessage),
                DeadLetterErrorCodes.ReplayFailed,
                cancellationToken).ConfigureAwait(false);
        }
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
            return LogOutcomeNotRecorded(messageId, marked.LeftToArray()[0]);

        var saved = await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        if (saved.IsLeft)
            return LogOutcomeNotRecorded(messageId, saved.LeftToArray()[0]);

        return marked.RightToArray()[0];
    }

    private EncinaError LogOutcomeNotRecorded(Guid messageId, EncinaError error)
    {
        DeadLetterLog.ReplayOutcomeNotRecorded(_logger, messageId, error.GetCode().IfNone("encina.unknown"));
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

        return recorded.RightToArray()[0]
            ? result
            : ReplayResult.Failed(messageId, DeadLetterErrorCodes.AlreadyReplayed);
    }

    private async Task<Either<EncinaError, ReplayResult>> ReplayStoredMessageAsync(
        IDeadLetterMessage message,
        Guid messageId,
        CancellationToken cancellationToken)
    {
        var plan = PrepareReplay(message);
        if (plan.ErrorCode is not null)
        {
            var recorded = await RecordReplayOutcomeAsync(messageId, plan.ErrorCode, cancellationToken).ConfigureAwait(false);
            if (recorded.IsLeft)
                return recorded.LeftToArray()[0];

            return EncinaErrors.Create(plan.ErrorCode, plan.ErrorText!);
        }

        // Replay through IEncina.Send, typed by the request's runtime type
        var attempt = await ReplayRequestAsync(plan.Encina!, plan.Request!, messageId, cancellationToken).ConfigureAwait(false);

        return await FinishReplayAsync(messageId, attempt.Result, attempt.OutcomeCode, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Deserializes the stored request and resolves <see cref="IEncina"/>; <c>ErrorCode</c> is set
    /// (and the other members are not) when the message cannot be replayed.
    /// </summary>
    private ReplayPlan PrepareReplay(IDeadLetterMessage message)
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
            var outcome = await RuntimeTypeRequestDispatcher.SendAsync(encina, request, cancellationToken).ConfigureAwait(false);

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
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var innerException = ex.InnerException ?? ex;
            return FailedAttempt(messageId, $"Replay failed: {innerException.GetType().FullName}", DeadLetterErrorCodes.ReplayFailed);
        }
    }

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
        var scoped = Scoped(filter, excludeReplayed: true);
        var take = Math.Min(maxMessages, DeadLetterStoreLimits.MaxPageSize);

        var messagesResult = await _store.GetMessagesAsync(scoped, 0, take, false, cancellationToken).ConfigureAwait(false);
        if (messagesResult.IsLeft)
            return messagesResult.LeftToArray()[0];

        var messages = messagesResult.RightToArray()[0].ToList();
        var results = new List<ReplayResult>();

        DeadLetterLog.BatchReplayStarted(_logger, messages.Count);

        foreach (var message in messages)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var result = await ReplayLoadedAsync(message, cancellationToken).ConfigureAwait(false);
            results.Add(result.Match(
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

    /// <inheritdoc />
    public async Task<Either<EncinaError, Option<IDeadLetterMessage>>> GetMessageAsync(
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
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
        return await _store.GetMessagesAsync(Scoped(filter), skip, take, false, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, int>> GetCountAsync(
        DeadLetterFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        return await _store.GetCountAsync(Scoped(filter), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, DeadLetterStatistics>> GetStatisticsAsync(
        CancellationToken cancellationToken = default)
    {
        return _orchestrator.GetStatisticsAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> DeleteAsync(
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
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

        return deleteResult.RightToArray()[0] ? Unit.Default : NotDeletedError(messageId);
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

        var deleted = await _store.DeleteManyAsync(Scoped(filter), cancellationToken).ConfigureAwait(false);
        if (deleted.IsLeft)
            return deleted.LeftToArray()[0];

        var saved = await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        if (saved.IsLeft)
            return saved.LeftToArray()[0];

        var count = deleted.RightToArray()[0];
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
