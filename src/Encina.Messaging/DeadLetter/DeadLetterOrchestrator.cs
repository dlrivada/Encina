using Encina.Diagnostics;
using Encina.Messaging.Diagnostics;
using Encina.Messaging.Serialization;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.Messaging.DeadLetter;

/// <summary>
/// Context for adding a message to the Dead Letter Queue.
/// </summary>
/// <param name="Error">The error that caused the failure.</param>
/// <param name="Exception">The exception, if any.</param>
/// <param name="SourcePattern">The source pattern (e.g., Recoverability, Outbox).</param>
/// <param name="TotalRetryAttempts">Total retry attempts made before dead-lettering.</param>
/// <param name="FirstFailedAtUtc">
/// When the message first failed; its <see cref="DateTime.Kind"/> must be <see cref="DateTimeKind.Utc"/>
/// (capture throws <see cref="ArgumentException"/> otherwise, for every provider).
/// </param>
/// <param name="CorrelationId">Optional correlation ID for tracing.</param>
/// <param name="SourceMessageId">
/// Optional identifier of the source message; with <paramref name="SourcePattern"/> it makes the capture
/// idempotent. Defaults to the new dead letter id, which never blocks unrelated messages. It must not start
/// or end with white space (capture throws <see cref="ArgumentException"/>), because providers compare
/// trailing spaces differently.
/// </param>
/// <param name="TenantId">
/// Optional tenant override, for sources that restore a persisted tenant. Defaults to the ambient
/// <c>IRequestContext.TenantId</c>.
/// </param>
public sealed record DeadLetterContext(
    EncinaError Error,
    Exception? Exception,
    string SourcePattern,
    int TotalRetryAttempts,
    DateTime FirstFailedAtUtc,
    string? CorrelationId = null,
    string? SourceMessageId = null,
    string? TenantId = null);

/// <summary>
/// Orchestrates Dead Letter Queue operations.
/// </summary>
/// <remarks>
/// <para>
/// This class provides the core DLQ functionality:
/// <list type="bullet">
/// <item><description>Adding failed messages to DLQ</description></item>
/// <item><description>Integrating with other messaging patterns</description></item>
/// <item><description>Managing message lifecycle</description></item>
/// </list>
/// </para>
/// <para>
/// Capture is idempotent: a second capture of the same <c>(SourcePattern, SourceMessageId)</c> is ignored,
/// returns the existing dead letter and does not invoke <see cref="DeadLetterOptions.OnDeadLetter"/>.
/// </para>
/// </remarks>
public sealed class DeadLetterOrchestrator
{
    private static readonly string[] SourcePatternsCounted =
    [
        DeadLetterSourcePatterns.Recoverability,
        DeadLetterSourcePatterns.Outbox,
        DeadLetterSourcePatterns.Inbox,
        DeadLetterSourcePatterns.Scheduling,
        DeadLetterSourcePatterns.Saga,
        DeadLetterSourcePatterns.Choreography
    ];

    private const int ExceptionTypeMaxLength = 512;

    private readonly IDeadLetterStore _store;
    private readonly IDeadLetterMessageFactory _messageFactory;
    private readonly DeadLetterOptions _options;
    private readonly ILogger<DeadLetterOrchestrator> _logger;
    private readonly IMessageSerializer _messageSerializer;
    private readonly IRequestContextAccessor _requestContextAccessor;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeadLetterOrchestrator"/> class.
    /// </summary>
    /// <param name="store">The dead letter store.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="options">The DLQ options.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="messageSerializer">
    /// The message serializer used to persist the failed request payload, so that decorators
    /// such as <c>EncryptingMessageSerializer</c> apply to dead-lettered content too.
    /// </param>
    /// <param name="requestContextAccessor">
    /// The ambient request context accessor; its <c>TenantId</c> is stamped on captured messages and
    /// scopes <see cref="GetStatisticsAsync"/>.
    /// </param>
    /// <param name="timeProvider">Optional time provider for testability.</param>
    public DeadLetterOrchestrator(
        IDeadLetterStore store,
        IDeadLetterMessageFactory messageFactory,
        DeadLetterOptions options,
        ILogger<DeadLetterOrchestrator> logger,
        IMessageSerializer messageSerializer,
        IRequestContextAccessor requestContextAccessor,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(messageFactory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(messageSerializer);
        ArgumentNullException.ThrowIfNull(requestContextAccessor);

        _store = store;
        _messageFactory = messageFactory;
        _options = options;
        _logger = logger;
        _messageSerializer = messageSerializer;
        _requestContextAccessor = requestContextAccessor;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Adds a message to the Dead Letter Queue.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <param name="request">The failed request.</param>
    /// <param name="context">The dead letter context with failure details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The created dead letter message, the existing one when the source message was already captured,
    /// or an error.
    /// </returns>
    public async Task<Either<EncinaError, IDeadLetterMessage>> AddAsync<TRequest>(
        TRequest request,
        DeadLetterContext context,
        CancellationToken cancellationToken = default)
        where TRequest : notnull
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrEmpty(context.SourcePattern);

        var runtimeType = request.GetType();
        var requestType = runtimeType.AssemblyQualifiedName ?? runtimeType.FullName ?? runtimeType.Name;
        var requestContent = _messageSerializer.SerializeAsRuntimeType(request);

        // EncinaError.Message and Exception.Message can carry personal data (e.g. a data-subject
        // id), so the record and the log keep only the error code and the exception type (#1274).
        var (exceptionType, exceptionStackTrace) = DescribeException(context.Exception);
        var data = NewData(
            requestType,
            requestContent,
            ErrorCodeOf(context.Error),
            context.SourcePattern,
            context.SourceMessageId,
            context.TenantId,
            context.TotalRetryAttempts,
            context.FirstFailedAtUtc,
            context.CorrelationId,
            exceptionType,
            exceptionStackTrace);

        return await CaptureAsync(data, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Adds a message to the DLQ from a FailedMessage record.
    /// </summary>
    /// <param name="failedMessage">The failed message from recoverability.</param>
    /// <param name="sourcePattern">The source pattern.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The created dead letter message, the existing one when the failed message was already captured,
    /// or an error.
    /// </returns>
    public async Task<Either<EncinaError, IDeadLetterMessage>> AddFromFailedMessageAsync(
        Recoverability.FailedMessage failedMessage,
        string sourcePattern,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(failedMessage);
        ArgumentException.ThrowIfNullOrEmpty(sourcePattern);

        // The record is built here, not by the provider factory, so the request goes through
        // IMessageSerializer (encryption applies, using the request's runtime type) and only the
        // error code and exception type are kept (#1274).
        var (exceptionType, exceptionStackTrace) = DescribeException(failedMessage.Exception);
        var data = NewData(
            failedMessage.RequestType,
            _messageSerializer.SerializeAsRuntimeType(failedMessage.Request),
            ErrorCodeOf(failedMessage.Error),
            sourcePattern,
            failedMessage.Id.ToString("D"),
            tenantId: null,
            failedMessage.TotalAttempts,
            failedMessage.FirstAttemptAtUtc,
            failedMessage.CorrelationId,
            exceptionType,
            exceptionStackTrace);

        return await CaptureAsync(data, cancellationToken).ConfigureAwait(false);
    }

    private DeadLetterData NewData(
        string requestType,
        string requestContent,
        string errorCode,
        string sourcePattern,
        string? sourceMessageId,
        string? tenantId,
        int totalRetryAttempts,
        DateTime firstFailedAtUtc,
        string? correlationId,
        string? exceptionType,
        string? exceptionStackTrace)
    {
        // Identity values are never truncated (a cut value would collide with, or point at, another row);
        // diagnostic values are cut to the column size so that an oversized one cannot lose the dead letter.
        RequireWithin(requestType, DeadLetterStoreLimits.RequestTypeMaxLength, nameof(requestType));
        RequireWithin(sourcePattern, DeadLetterStoreLimits.SourcePatternMaxLength, nameof(sourcePattern));
        RequireWithin(sourceMessageId, DeadLetterStoreLimits.SourceMessageIdMaxLength, nameof(sourceMessageId));
        RequireWithin(tenantId, DeadLetterStoreLimits.TenantIdMaxLength, nameof(tenantId));

        // The same input rules on all ten providers: trimmed identity values, UTC instants (DeadLetterInputs).
        DeadLetterInputs.RequireTrimmed(requestType, nameof(requestType));
        DeadLetterInputs.RequireTrimmed(sourcePattern, nameof(sourcePattern));
        DeadLetterInputs.RequireTrimmed(sourceMessageId, nameof(sourceMessageId));
        DeadLetterInputs.RequireTrimmed(tenantId, nameof(tenantId));
        DeadLetterInputs.RequireUtc(firstFailedAtUtc, nameof(firstFailedAtUtc));
        errorCode = Cut(errorCode, DeadLetterStoreLimits.ErrorCodeMaxLength)!;
        correlationId = Cut(correlationId, DeadLetterStoreLimits.CorrelationIdMaxLength);
        exceptionType = Cut(exceptionType, ExceptionTypeMaxLength);

        var id = Guid.NewGuid();
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        return new DeadLetterData(
            Id: id,
            RequestType: requestType,
            RequestContent: requestContent,
            ErrorCode: errorCode,
            SourcePattern: sourcePattern,
            SourceMessageId: sourceMessageId ?? id.ToString("D"),
            TotalRetryAttempts: totalRetryAttempts,
            FirstFailedAtUtc: firstFailedAtUtc,
            DeadLetteredAtUtc: now,
            ExpiresAtUtc: _options.RetentionPeriod.HasValue ? now.Add(_options.RetentionPeriod.Value) : null,
            CorrelationId: correlationId,
            ExceptionType: exceptionType,
            ExceptionStackTrace: exceptionStackTrace,
            TenantId: tenantId ?? AmbientTenantId());
    }

    private static void RequireWithin(string? value, int maxLength, string name)
    {
        if (value is not null && value.Length > maxLength)
        {
            throw new ArgumentException($"The value is longer than the {maxLength} characters the dead letter queue stores.", name);
        }
    }

    private static string? Cut(string? value, int maxLength)
        => value is not null && value.Length > maxLength ? value[..maxLength] : value;

    // An empty tenant id is no tenant.
    private string? AmbientTenantId()
    {
        var tenantId = _requestContextAccessor.RequestContext?.TenantId;
        return string.IsNullOrEmpty(tenantId) ? null : tenantId;
    }

    private static (string? Type, string? StackTrace) DescribeException(Exception? exception)
        => (exception?.GetType().FullName, exception?.StackTrace);

    private async Task<Either<EncinaError, IDeadLetterMessage>> CaptureAsync(
        DeadLetterData data,
        CancellationToken cancellationToken)
    {
        var message = _messageFactory.Create(data);

        var stored = await PersistAsync(message, data.SourcePattern, cancellationToken).ConfigureAwait(false);
        if (stored.IsLeft)
            return stored.LeftToArray()[0];

        if (!stored.RightToArray()[0])
            return await ExistingAsync(data, cancellationToken).ConfigureAwait(false);

        DeadLetterMetrics.RecordAdded(data.SourcePattern);
        DeadLetterLog.MessageAddedToDLQ(
            _logger,
            message.Id,
            data.RequestType,
            data.SourcePattern,
            data.ErrorCode,
            data.TotalRetryAttempts,
            data.CorrelationId);

        await InvokeOnDeadLetterAsync(message, cancellationToken).ConfigureAwait(false);

        return Either<EncinaError, IDeadLetterMessage>.Right(message);
    }

    // Right(true) when the message was stored; Right(false) when the source message was already captured.
    private async Task<Either<EncinaError, bool>> PersistAsync(
        IDeadLetterMessage message,
        string sourcePattern,
        CancellationToken cancellationToken)
    {
        var addResult = await _store.AddAsync(message, cancellationToken).ConfigureAwait(false);
        if (addResult.IsLeft)
            return LogStoreWriteFailed(addResult.LeftToArray()[0], sourcePattern, "add");

        if (!addResult.RightToArray()[0])
            return false;

        var saveResult = await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        if (saveResult.IsLeft)
            return LogStoreWriteFailed(saveResult.LeftToArray()[0], sourcePattern, "save_changes");

        return true;
    }

    private EncinaError LogStoreWriteFailed(EncinaError error, string sourcePattern, string operation)
    {
        var errorCode = ErrorCodeOf(error);
        DeadLetterMetrics.RecordStoreFailure(operation, errorCode);
        DeadLetterLog.StoreWriteFailed(_logger, sourcePattern, errorCode);
        return error;
    }

    private async Task<Either<EncinaError, IDeadLetterMessage>> ExistingAsync(
        DeadLetterData data,
        CancellationToken cancellationToken)
    {
        var filter = new DeadLetterFilter
        {
            SourcePattern = data.SourcePattern,
            SourceMessageId = data.SourceMessageId
        };

        var existing = await _store.GetMessagesAsync(filter, 0, 1, false, cancellationToken).ConfigureAwait(false);
        if (existing.IsLeft)
            return existing.LeftToArray()[0];

        var first = existing.RightToArray()[0].FirstOrDefault();
        if (first is null)
        {
            // The earlier dead letter was deleted between the insert and this read.
            return EncinaErrors.Create(
                DeadLetterErrorCodes.StoreFailed,
                "The dead letter of the source message disappeared during capture");
        }

        DeadLetterMetrics.RecordDuplicateIgnored(data.SourcePattern);
        DeadLetterLog.DuplicateIgnored(_logger, first.Id, data.SourcePattern);
        return Either<EncinaError, IDeadLetterMessage>.Right(first);
    }

    // Invoke custom callback if configured; a failing callback never fails the dead-lettering.
    private async Task InvokeOnDeadLetterAsync(IDeadLetterMessage message, CancellationToken cancellationToken)
    {
        if (_options.OnDeadLetter is null)
            return;

        try
        {
            await _options.OnDeadLetter(message, cancellationToken);
        }
        catch (Exception ex)
        {
            DeadLetterLog.OnDeadLetterCallbackFailed(_logger, ex.ForLogging(), message.Id);
        }
    }

    /// <summary>
    /// Gets a message from the DLQ.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The message if found, or an error.</returns>
    public Task<Either<EncinaError, Option<IDeadLetterMessage>>> GetAsync(
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        return _store.GetAsync(messageId, cancellationToken);
    }

    /// <summary>
    /// Gets the count of pending (non-replayed) messages in the DLQ.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The count of pending messages, or an error.</returns>
    public async Task<Either<EncinaError, int>> GetPendingCountAsync(CancellationToken cancellationToken = default)
    {
        return await _store.GetCountAsync(
            new DeadLetterFilter { ExcludeReplayed = true },
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets statistics about the DLQ.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// DLQ statistics, or an error. The statistics are scoped to the ambient tenant when there is one
    /// and cover the whole queue otherwise. No operation loads the queue: counts and two single-row
    /// reads in a defined order.
    /// </returns>
    public async Task<Either<EncinaError, DeadLetterStatistics>> GetStatisticsAsync(
        CancellationToken cancellationToken = default)
    {
        var tenantId = AmbientTenantId();

        var counts = await TotalsAsync(tenantId, cancellationToken).ConfigureAwait(false);
        if (counts.IsLeft)
            return counts.LeftToArray()[0];

        var countBySource = await CountBySourceAsync(tenantId, cancellationToken).ConfigureAwait(false);
        if (countBySource.IsLeft)
            return countBySource.LeftToArray()[0];

        var bounds = await PendingBoundsAsync(tenantId, cancellationToken).ConfigureAwait(false);
        if (bounds.IsLeft)
            return bounds.LeftToArray()[0];

        var (total, pending, expired) = counts.RightToArray()[0];
        var (oldest, newest) = bounds.RightToArray()[0];

        return new DeadLetterStatistics
        {
            TotalCount = total,
            PendingCount = pending,
            ReplayedCount = total - pending,
            ExpiredCount = expired,
            CountBySource = countBySource.RightToArray()[0],
            OldestPendingAtUtc = ToNullable(oldest),
            NewestPendingAtUtc = ToNullable(newest)
        };
    }

    private async Task<Either<EncinaError, (int Total, int Pending, int Expired)>> TotalsAsync(
        string? tenantId,
        CancellationToken cancellationToken)
    {
        var total = await CountAsync(tenantId, null, null, cancellationToken).ConfigureAwait(false);
        if (total.IsLeft)
            return total.LeftToArray()[0];

        var pending = await CountAsync(tenantId, true, null, cancellationToken).ConfigureAwait(false);
        if (pending.IsLeft)
            return pending.LeftToArray()[0];

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var expired = await CountAsync(tenantId, true, now, cancellationToken).ConfigureAwait(false);
        if (expired.IsLeft)
            return expired.LeftToArray()[0];

        return (total.RightToArray()[0], pending.RightToArray()[0], expired.RightToArray()[0]);
    }

    private async Task<Either<EncinaError, (Option<DateTime> Oldest, Option<DateTime> Newest)>> PendingBoundsAsync(
        string? tenantId,
        CancellationToken cancellationToken)
    {
        var oldest = await PendingTimestampAsync(tenantId, newestFirst: false, cancellationToken).ConfigureAwait(false);
        if (oldest.IsLeft)
            return oldest.LeftToArray()[0];

        var newest = await PendingTimestampAsync(tenantId, newestFirst: true, cancellationToken).ConfigureAwait(false);
        if (newest.IsLeft)
            return newest.LeftToArray()[0];

        return (oldest.RightToArray()[0], newest.RightToArray()[0]);
    }

    private Task<Either<EncinaError, int>> CountAsync(
        string? tenantId,
        bool? excludeReplayed,
        DateTime? expiresAtOrBeforeUtc,
        CancellationToken cancellationToken)
        => _store.GetCountAsync(
            new DeadLetterFilter
            {
                TenantId = tenantId,
                ExcludeReplayed = excludeReplayed,
                ExpiresAtOrBeforeUtc = expiresAtOrBeforeUtc
            },
            cancellationToken);

    private async Task<Either<EncinaError, IReadOnlyDictionary<string, int>>> CountBySourceAsync(
        string? tenantId,
        CancellationToken cancellationToken)
    {
        var countBySource = new Dictionary<string, int>();
        foreach (var pattern in SourcePatternsCounted)
        {
            var countResult = await _store.GetCountAsync(
                new DeadLetterFilter { TenantId = tenantId, SourcePattern = pattern, ExcludeReplayed = true },
                cancellationToken).ConfigureAwait(false);
            if (countResult.IsLeft)
                return countResult.LeftToArray()[0];

            var count = countResult.RightToArray()[0];
            if (count > 0)
            {
                countBySource[pattern] = count;
            }
        }

        return countBySource;
    }

    // The first row of the defined order: the oldest pending, or the newest pending when newestFirst.
    private async Task<Either<EncinaError, Option<DateTime>>> PendingTimestampAsync(
        string? tenantId,
        bool newestFirst,
        CancellationToken cancellationToken)
    {
        var result = await _store.GetMessagesAsync(
            new DeadLetterFilter { TenantId = tenantId, ExcludeReplayed = true },
            skip: 0,
            take: 1,
            newestFirst,
            cancellationToken).ConfigureAwait(false);
        if (result.IsLeft)
            return result.LeftToArray()[0];

        var first = result.RightToArray()[0].FirstOrDefault();
        return first is null ? Option<DateTime>.None : Option<DateTime>.Some(first.DeadLetteredAtUtc);
    }

    private static DateTime? ToNullable(Option<DateTime> timestamp)
        => timestamp.IsSome ? timestamp.IfNone(default(DateTime)) : null;

    /// <summary>
    /// Cleans up expired messages.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of messages deleted, or an error.</returns>
    public async Task<Either<EncinaError, int>> CleanupExpiredAsync(CancellationToken cancellationToken = default)
    {
        var countResult = await _store.DeleteExpiredAsync(cancellationToken).ConfigureAwait(false);
        if (countResult.IsLeft)
            return countResult;

        var count = countResult.Match(Right: c => c, Left: _ => 0);

        DeadLetterMetrics.RecordDeleted(count, DeadLetterMetrics.ReasonExpired);
        if (count > 0)
        {
            DeadLetterLog.ExpiredMessagesCleanedUp(_logger, count);
        }

        return count;
    }

    private static string ErrorCodeOf(EncinaError error) => error.GetCode().IfNone("encina.unknown");
}
