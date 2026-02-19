using System.Text.Json;
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
/// <param name="FirstFailedAtUtc">When the message first failed.</param>
/// <param name="CorrelationId">Optional correlation ID for tracing.</param>
public sealed record DeadLetterContext(
    EncinaError Error,
    Exception? Exception,
    string SourcePattern,
    int TotalRetryAttempts,
    DateTime FirstFailedAtUtc,
    string? CorrelationId = null);

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
/// </remarks>
public sealed class DeadLetterOrchestrator
{
    private readonly IDeadLetterStore _store;
    private readonly IDeadLetterMessageFactory _messageFactory;
    private readonly DeadLetterOptions _options;
    private readonly ILogger<DeadLetterOrchestrator> _logger;
    private readonly TimeProvider _timeProvider;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="DeadLetterOrchestrator"/> class.
    /// </summary>
    /// <param name="store">The dead letter store.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="options">The DLQ options.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">Optional time provider for testability.</param>
    public DeadLetterOrchestrator(
        IDeadLetterStore store,
        IDeadLetterMessageFactory messageFactory,
        DeadLetterOptions options,
        ILogger<DeadLetterOrchestrator> logger,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(messageFactory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _store = store;
        _messageFactory = messageFactory;
        _options = options;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Adds a message to the Dead Letter Queue.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <param name="request">The failed request.</param>
    /// <param name="context">The dead letter context with failure details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created dead letter message, or an error.</returns>
    public async Task<Either<EncinaError, IDeadLetterMessage>> AddAsync<TRequest>(
        TRequest request,
        DeadLetterContext context,
        CancellationToken cancellationToken = default)
        where TRequest : notnull
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrEmpty(context.SourcePattern);

        var requestType = typeof(TRequest).AssemblyQualifiedName ?? typeof(TRequest).FullName ?? typeof(TRequest).Name;
        var requestContent = JsonSerializer.Serialize(request, JsonOptions);
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var expiresAt = _options.RetentionPeriod.HasValue
            ? now.Add(_options.RetentionPeriod.Value)
            : (DateTime?)null;

        var data = new DeadLetterData(
            Id: Guid.NewGuid(),
            RequestType: requestType,
            RequestContent: requestContent,
            ErrorMessage: context.Error.Message,
            SourcePattern: context.SourcePattern,
            TotalRetryAttempts: context.TotalRetryAttempts,
            FirstFailedAtUtc: context.FirstFailedAtUtc,
            DeadLetteredAtUtc: now,
            ExpiresAtUtc: expiresAt,
            CorrelationId: context.CorrelationId,
            ExceptionType: context.Exception?.GetType().FullName,
            ExceptionMessage: context.Exception?.Message,
            ExceptionStackTrace: context.Exception?.StackTrace);

        var message = _messageFactory.Create(data);

        await _store.AddAsync(message, cancellationToken);
        await _store.SaveChangesAsync(cancellationToken);

        DeadLetterLog.MessageAddedToDLQ(
            _logger,
            message.Id,
            requestType,
            context.SourcePattern,
            context.Error.Message,
            context.TotalRetryAttempts,
            context.CorrelationId);

        // Invoke custom callback if configured
        if (_options.OnDeadLetter is not null)
        {
            try
            {
                await _options.OnDeadLetter(message, cancellationToken);
            }
            catch (Exception ex)
            {
                DeadLetterLog.OnDeadLetterCallbackFailed(_logger, ex, message.Id);
            }
        }

        return Either<EncinaError, IDeadLetterMessage>.Right(message);
    }

    /// <summary>
    /// Adds a message to the DLQ from a FailedMessage record.
    /// </summary>
    /// <param name="failedMessage">The failed message from recoverability.</param>
    /// <param name="sourcePattern">The source pattern.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created dead letter message, or an error.</returns>
    public async Task<Either<EncinaError, IDeadLetterMessage>> AddFromFailedMessageAsync(
        Recoverability.FailedMessage failedMessage,
        string sourcePattern,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(failedMessage);
        ArgumentException.ThrowIfNullOrEmpty(sourcePattern);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var expiresAt = _options.RetentionPeriod.HasValue
            ? now.Add(_options.RetentionPeriod.Value)
            : (DateTime?)null;

        var message = _messageFactory.CreateFromFailedMessage(failedMessage, sourcePattern, expiresAt);

        await _store.AddAsync(message, cancellationToken);
        await _store.SaveChangesAsync(cancellationToken);

        DeadLetterLog.MessageAddedToDLQ(
            _logger,
            message.Id,
            message.RequestType,
            sourcePattern,
            message.ErrorMessage,
            failedMessage.TotalAttempts,
            failedMessage.CorrelationId);

        // Invoke custom callback if configured
        if (_options.OnDeadLetter is not null)
        {
            try
            {
                await _options.OnDeadLetter(message, cancellationToken);
            }
            catch (Exception ex)
            {
                DeadLetterLog.OnDeadLetterCallbackFailed(_logger, ex, message.Id);
            }
        }

        return Either<EncinaError, IDeadLetterMessage>.Right(message);
    }

    /// <summary>
    /// Gets a message from the DLQ.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The message if found.</returns>
    public Task<IDeadLetterMessage?> GetAsync(
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
        var count = await _store.GetCountAsync(
            new DeadLetterFilter { ExcludeReplayed = true },
            cancellationToken);

        return count;
    }

    /// <summary>
    /// Gets statistics about the DLQ.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>DLQ statistics, or an error.</returns>
    public async Task<Either<EncinaError, DeadLetterStatistics>> GetStatisticsAsync(
        CancellationToken cancellationToken = default)
    {
        var totalCount = await _store.GetCountAsync(null, cancellationToken);
        var pendingCount = await _store.GetCountAsync(
            new DeadLetterFilter { ExcludeReplayed = true },
            cancellationToken);
        // Get count by source pattern
        var sourcePatterns = new[]
        {
            DeadLetterSourcePatterns.Recoverability,
            DeadLetterSourcePatterns.Outbox,
            DeadLetterSourcePatterns.Inbox,
            DeadLetterSourcePatterns.Scheduling,
            DeadLetterSourcePatterns.Saga,
            DeadLetterSourcePatterns.Choreography
        };

        var countBySource = new Dictionary<string, int>();
        foreach (var pattern in sourcePatterns)
        {
            var count = await _store.GetCountAsync(
                new DeadLetterFilter { SourcePattern = pattern, ExcludeReplayed = true },
                cancellationToken);
            if (count > 0)
            {
                countBySource[pattern] = count;
            }
        }

        // Get oldest and newest pending messages
        var pendingMessages = await _store.GetMessagesAsync(
            new DeadLetterFilter { ExcludeReplayed = true },
            skip: 0,
            take: 1,
            cancellationToken);
        var oldestPending = pendingMessages.FirstOrDefault();

        DateTime? oldestPendingAtUtc = oldestPending?.DeadLetteredAtUtc;
        DateTime? newestPendingAtUtc = null;

        if (pendingCount > 1)
        {
            var newestMessages = await _store.GetMessagesAsync(
                new DeadLetterFilter { ExcludeReplayed = true },
                skip: pendingCount - 1,
                take: 1,
                cancellationToken);
            newestPendingAtUtc = newestMessages.FirstOrDefault()?.DeadLetteredAtUtc;
        }
        else if (pendingCount == 1)
        {
            newestPendingAtUtc = oldestPendingAtUtc;
        }

        // Count expired (non-replayed messages past expiration)
        var expiredMessages = await _store.GetMessagesAsync(
            new DeadLetterFilter { ExcludeReplayed = true },
            skip: 0,
            take: int.MaxValue,
            cancellationToken);
        var expiredCount = expiredMessages.Count(m => m.IsExpired);

        return new DeadLetterStatistics
        {
            TotalCount = totalCount,
            PendingCount = pendingCount,
            ReplayedCount = totalCount - pendingCount,
            ExpiredCount = expiredCount,
            CountBySource = countBySource,
            OldestPendingAtUtc = oldestPendingAtUtc,
            NewestPendingAtUtc = newestPendingAtUtc
        };
    }

    /// <summary>
    /// Cleans up expired messages.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of messages deleted, or an error.</returns>
    public async Task<Either<EncinaError, int>> CleanupExpiredAsync(CancellationToken cancellationToken = default)
    {
        var count = await _store.DeleteExpiredAsync(cancellationToken);

        if (count > 0)
        {
            DeadLetterLog.ExpiredMessagesCleanedUp(_logger, count);
        }

        return count;
    }
}
