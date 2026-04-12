using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Encina.Messaging.Scheduling;

/// <summary>
/// Orchestrates the Scheduling Pattern for delayed and recurring message execution.
/// </summary>
/// <remarks>
/// <para>
/// This orchestrator contains all domain logic for the Scheduling Pattern, delegating
/// persistence operations to <see cref="IScheduledMessageStore"/>. It handles both
/// one-time delayed execution and recurring schedules using cron expressions.
/// </para>
/// <para>
/// <b>Processing Flow</b>:
/// <list type="number">
/// <item><description>Schedule message for future execution</description></item>
/// <item><description>Background processor retrieves due messages</description></item>
/// <item><description>Execute each message via the configured callback</description></item>
/// <item><description>For recurring: reschedule based on cron expression</description></item>
/// <item><description>For one-time: mark as processed or schedule retry</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class SchedulerOrchestrator
{
    private readonly IScheduledMessageStore _store;
    private readonly SchedulingOptions _options;
    private readonly ILogger<SchedulerOrchestrator> _logger;
    private readonly IScheduledMessageFactory _messageFactory;
    private readonly IScheduledMessageRetryPolicy _retryPolicy;
    private readonly ICronParser? _cronParser;
    private readonly TimeProvider _timeProvider;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="SchedulerOrchestrator"/> class.
    /// </summary>
    /// <param name="store">The scheduled message store for persistence.</param>
    /// <param name="options">The scheduling options.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="messageFactory">Factory to create scheduled messages.</param>
    /// <param name="retryPolicy">
    /// Pluggable retry policy used to compute the next attempt time (or dead-letter)
    /// when a scheduled message dispatch fails. The default registration in DI is
    /// <see cref="ExponentialBackoffRetryPolicy"/>; users can swap in their own
    /// implementation by registering it before <c>AddEncina*()</c>.
    /// </param>
    /// <param name="cronParser">Optional cron parser for recurring messages.</param>
    /// <param name="timeProvider">Optional time provider for testability.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any required dependency (<paramref name="store"/>,
    /// <paramref name="options"/>, <paramref name="logger"/>,
    /// <paramref name="messageFactory"/>, or <paramref name="retryPolicy"/>) is
    /// <see langword="null"/>.
    /// </exception>
    public SchedulerOrchestrator(
        IScheduledMessageStore store,
        SchedulingOptions options,
        ILogger<SchedulerOrchestrator> logger,
        IScheduledMessageFactory messageFactory,
        IScheduledMessageRetryPolicy retryPolicy,
        ICronParser? cronParser = null,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(messageFactory);
        ArgumentNullException.ThrowIfNull(retryPolicy);

        _store = store;
        _options = options;
        _logger = logger;
        _messageFactory = messageFactory;
        _retryPolicy = retryPolicy;
        _cronParser = cronParser;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Schedules a request for delayed execution.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <param name="request">The request to schedule.</param>
    /// <param name="executeAt">When to execute the request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The scheduled message ID, or an error if scheduling failed.</returns>
    public async Task<Either<EncinaError, Guid>> ScheduleAsync<TRequest>(
        TRequest request,
        DateTime executeAt,
        CancellationToken cancellationToken = default)
        where TRequest : class
    {
        ArgumentNullException.ThrowIfNull(request);

        if (executeAt < _timeProvider.GetUtcNow().UtcDateTime)
        {
            return EncinaErrors.Create(
                SchedulingErrorCodes.InvalidScheduleTime,
                "Scheduled time must be in the future.");
        }

        var requestType = typeof(TRequest).AssemblyQualifiedName
            ?? typeof(TRequest).FullName
            ?? typeof(TRequest).Name;

        var content = JsonSerializer.Serialize(request, JsonOptions);

        var message = _messageFactory.Create(
            Guid.NewGuid(),
            requestType,
            content,
            executeAt,
            _timeProvider.GetUtcNow().UtcDateTime,
            isRecurring: false,
            cronExpression: null);

        var addResult = await _store.AddAsync(message, cancellationToken).ConfigureAwait(false);
        if (addResult.IsLeft)
            return addResult.LeftToArray()[0];

        Log.MessageScheduled(_logger, message.Id, requestType, executeAt);

        return message.Id;
    }

    /// <summary>
    /// Schedules a request for delayed execution using a delay timespan.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <param name="request">The request to schedule.</param>
    /// <param name="delay">The delay before execution.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The scheduled message ID, or an error if scheduling failed.</returns>
    public Task<Either<EncinaError, Guid>> ScheduleAsync<TRequest>(
        TRequest request,
        TimeSpan delay,
        CancellationToken cancellationToken = default)
        where TRequest : class
    {
        if (delay <= TimeSpan.Zero)
        {
            return Task.FromResult<Either<EncinaError, Guid>>(
                EncinaErrors.Create(
                    SchedulingErrorCodes.InvalidDelay,
                    "Delay must be positive."));
        }

        return ScheduleAsync(request, _timeProvider.GetUtcNow().UtcDateTime.Add(delay), cancellationToken);
    }

    /// <summary>
    /// Schedules a recurring request using a cron expression.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <param name="request">The request to schedule.</param>
    /// <param name="cronExpression">The cron expression defining the schedule.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The scheduled message ID.</returns>
    public async Task<Either<EncinaError, Guid>> ScheduleRecurringAsync<TRequest>(
        TRequest request,
        string cronExpression,
        CancellationToken cancellationToken = default)
        where TRequest : class
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(cronExpression);

        if (!_options.EnableRecurringMessages)
        {
            return EncinaErrors.Create(
                SchedulingErrorCodes.RecurringDisabled,
                "Recurring messages are disabled");
        }

        if (_cronParser == null)
        {
            return EncinaErrors.Create(
                SchedulingErrorCodes.NoCronParser,
                "No cron parser configured for recurring messages");
        }

        var nextExecutionResult = _cronParser.GetNextOccurrence(cronExpression, _timeProvider.GetUtcNow().UtcDateTime);

        return await nextExecutionResult.MatchAsync(
            RightAsync: async nextExecution =>
            {
                var requestType = typeof(TRequest).AssemblyQualifiedName
                    ?? typeof(TRequest).FullName
                    ?? typeof(TRequest).Name;

                var content = JsonSerializer.Serialize(request, JsonOptions);

                var message = _messageFactory.Create(
                    Guid.NewGuid(),
                    requestType,
                    content,
                    nextExecution,
                    _timeProvider.GetUtcNow().UtcDateTime,
                    isRecurring: true,
                    cronExpression: cronExpression);

                await _store.AddAsync(message, cancellationToken).ConfigureAwait(false);

                Log.RecurringMessageScheduled(_logger, message.Id, requestType, cronExpression, nextExecution);

                return (Either<EncinaError, Guid>)message.Id;
            },
            Left: error => (Either<EncinaError, Guid>)error).ConfigureAwait(false);
    }

    /// <summary>
    /// Cancels a scheduled message.
    /// </summary>
    /// <param name="messageId">The message ID to cancel.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Unit on success, or an error if cancellation failed.</returns>
    public async Task<Either<EncinaError, Unit>> CancelAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var cancelResult = await _store.CancelAsync(messageId, cancellationToken).ConfigureAwait(false);
        if (cancelResult.IsLeft)
            return cancelResult.LeftToArray()[0];

        Log.MessageCancelled(_logger, messageId);

        return Unit.Default;
    }

    /// <summary>
    /// Processes due scheduled messages by retrieving them from the store, deserializing
    /// each payload, invoking the supplied dispatch callback, and updating the store with
    /// the outcome (mark-as-processed, reschedule recurring, or delegate to the retry
    /// policy on failure).
    /// </summary>
    /// <param name="executeCallback">
    /// The callback that dispatches a single deserialized request. The callback follows
    /// Railway Oriented Programming and returns
    /// <see cref="Either{L,R}"/> of <see cref="EncinaError"/> and <see cref="Unit"/>:
    /// <list type="bullet">
    /// <item><description><c>Right(Unit.Default)</c> — dispatch succeeded; the message
    /// will be marked processed (or rescheduled if recurring).</description></item>
    /// <item><description><c>Left(error)</c> — dispatch failed in a controlled manner;
    /// the orchestrator delegates the next-retry decision to the configured
    /// <see cref="IScheduledMessageRetryPolicy"/> and updates the store via
    /// <see cref="IScheduledMessageStore.MarkAsFailedAsync"/>.</description></item>
    /// </list>
    /// The callback receives the active <see cref="CancellationToken"/> so host shutdown
    /// promptly aborts in-flight dispatch operations.
    /// </param>
    /// <param name="cancellationToken">Cancellation token honored between messages.</param>
    /// <returns>
    /// On success, the number of messages dispatched successfully in this batch (i.e.
    /// callbacks that returned <c>Right</c>). Failed messages still update the store but
    /// do not increment the count. On store retrieval failure, returns
    /// <c>Left(EncinaError)</c>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <b>Exception safety net</b>: although the contract is ROP, the orchestrator
    /// still wraps the callback in a try/catch as a safety net for true bugs (handler
    /// crashes, <see cref="AccessViolationException"/>, etc.). Caught exceptions are
    /// converted into a synthetic failure routed through the same retry-policy path,
    /// so the retry behavior is consistent regardless of how the failure is signalled.
    /// <see cref="OperationCanceledException"/> is rethrown so cancellation propagates.
    /// </para>
    /// <para>
    /// <b>Retry policy delegation</b>: this method never computes retry timing inline.
    /// Every failure (controlled <c>Left</c>, exception, unknown type, deserialization
    /// failure) flows through the private <c>MarkAsFailedAsync</c> helper, which
    /// delegates entirely to <see cref="IScheduledMessageRetryPolicy.Compute"/>.
    /// </para>
    /// </remarks>
    public async Task<Either<EncinaError, int>> ProcessDueMessagesAsync(
        Func<IScheduledMessage, Type, object, CancellationToken, ValueTask<Either<EncinaError, Unit>>> executeCallback,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(executeCallback);

        var messagesResult = await _store.GetDueMessagesAsync(
            _options.BatchSize,
            _options.MaxRetries,
            cancellationToken).ConfigureAwait(false);

        if (messagesResult.IsLeft)
            return messagesResult.LeftToArray()[0];

        var messages = messagesResult.Match(Right: m => m, Left: _ => Enumerable.Empty<IScheduledMessage>());
        var processedCount = 0;

        foreach (var message in messages)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                var requestType = Type.GetType(message.RequestType);
                if (requestType == null)
                {
                    Log.UnknownRequestType(_logger, message.Id, message.RequestType);
                    await MarkAsFailedAsync(message, $"Unknown request type: {message.RequestType}", cancellationToken).ConfigureAwait(false);
                    continue;
                }

                var request = JsonSerializer.Deserialize(message.Content, requestType, JsonOptions);
                if (request == null)
                {
                    Log.DeserializationFailed(_logger, message.Id, message.RequestType);
                    await MarkAsFailedAsync(message, "Failed to deserialize request", cancellationToken).ConfigureAwait(false);
                    continue;
                }

                var dispatchResult = await executeCallback(message, requestType, request, cancellationToken).ConfigureAwait(false);
                if (dispatchResult.IsLeft)
                {
                    var error = dispatchResult.LeftToArray()[0];
                    var errorCode = error.GetCode().IfNone("unknown");
                    Log.DispatchFailed(_logger, message.Id, errorCode, error.Message);
                    await MarkAsFailedAsync(message, error.Message, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                if (message.IsRecurring)
                {
                    await HandleRecurringMessageAsync(message, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    var markResult = await _store.MarkAsProcessedAsync(message.Id, cancellationToken).ConfigureAwait(false);
                    if (markResult.IsLeft)
                    {
                        Log.StoreMarkAsFailedError(_logger, message.Id, markResult.LeftToArray()[0].Message);
                    }
                }

                processedCount++;
                Log.MessageExecuted(_logger, message.Id);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
#pragma warning disable CA1031 // Do not catch general exception types — intentional safety net for handler bugs
            catch (Exception ex)
#pragma warning restore CA1031
            {
                // Safety net for true bugs (handler crashes, AVE, etc.).
                // Real failures use the Either path above.
                Log.ExecutionFailed(_logger, ex, message.Id);
                await MarkAsFailedAsync(message, ex.Message, cancellationToken).ConfigureAwait(false);
            }
        }

        return processedCount;
    }

    /// <summary>
    /// Gets the count of pending scheduled messages.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The count of pending messages, or an error.</returns>
    public async Task<Either<EncinaError, int>> GetPendingCountAsync(CancellationToken cancellationToken = default)
    {
        var messagesResult = await _store.GetDueMessagesAsync(
            int.MaxValue,
            _options.MaxRetries,
            cancellationToken).ConfigureAwait(false);

        return messagesResult.Map(messages => messages.Count());
    }

    private async Task HandleRecurringMessageAsync(IScheduledMessage message, CancellationToken cancellationToken)
    {
        if (_cronParser == null || string.IsNullOrEmpty(message.CronExpression))
        {
            var result = await _store.MarkAsProcessedAsync(message.Id, cancellationToken).ConfigureAwait(false);
            if (result.IsLeft)
                Log.StoreMarkAsFailedError(_logger, message.Id, result.LeftToArray()[0].Message);
            return;
        }

        var nextExecutionResult = _cronParser.GetNextOccurrence(message.CronExpression, _timeProvider.GetUtcNow().UtcDateTime);

        await nextExecutionResult.MatchAsync(
            RightAsync: async nextExecution =>
            {
                var rescheduleResult = await _store.RescheduleRecurringMessageAsync(message.Id, nextExecution, cancellationToken).ConfigureAwait(false);
                if (rescheduleResult.IsLeft)
                    Log.StoreMarkAsFailedError(_logger, message.Id, rescheduleResult.LeftToArray()[0].Message);
                else
                    Log.RecurringMessageRescheduled(_logger, message.Id, nextExecution);
                return Unit.Default;
            },
            LeftAsync: async _ =>
            {
                var markResult = await _store.MarkAsProcessedAsync(message.Id, cancellationToken).ConfigureAwait(false);
                if (markResult.IsLeft)
                    Log.StoreMarkAsFailedError(_logger, message.Id, markResult.LeftToArray()[0].Message);
                else
                    Log.RecurringMessageEnded(_logger, message.Id);
                return Unit.Default;
            }).ConfigureAwait(false);
    }

    private async Task MarkAsFailedAsync(IScheduledMessage message, string errorMessage, CancellationToken cancellationToken)
    {
        var decision = _retryPolicy.Compute(message.RetryCount, _options.MaxRetries, _timeProvider.GetUtcNow().UtcDateTime);
        var storeResult = await _store.MarkAsFailedAsync(message.Id, errorMessage, decision.NextRetryAtUtc, cancellationToken).ConfigureAwait(false);
        if (storeResult.IsLeft)
        {
            var storeError = storeResult.LeftToArray()[0];
            Log.StoreMarkAsFailedError(_logger, message.Id, storeError.Message);
        }
    }
}

/// <summary>
/// Factory interface for creating scheduled messages.
/// </summary>
/// <remarks>
/// Each provider (EF Core, Dapper, ADO.NET) implements this to create their specific message type.
/// </remarks>
public interface IScheduledMessageFactory
{
    /// <summary>
    /// Creates a new scheduled message.
    /// </summary>
    /// <param name="id">The message ID.</param>
    /// <param name="requestType">The request type.</param>
    /// <param name="content">The serialized content.</param>
    /// <param name="scheduledAtUtc">When to execute.</param>
    /// <param name="createdAtUtc">When created.</param>
    /// <param name="isRecurring">Whether this is recurring.</param>
    /// <param name="cronExpression">The cron expression for recurring messages.</param>
    /// <returns>A new scheduled message instance.</returns>
    IScheduledMessage Create(
        Guid id,
        string requestType,
        string content,
        DateTime scheduledAtUtc,
        DateTime createdAtUtc,
        bool isRecurring,
        string? cronExpression);
}

/// <summary>
/// Interface for parsing cron expressions.
/// </summary>
/// <remarks>
/// Implementations can use libraries like NCrontab, Cronos, or custom parsers.
/// </remarks>
public interface ICronParser
{
    /// <summary>
    /// Gets the next occurrence after the specified time.
    /// </summary>
    /// <param name="cronExpression">The cron expression.</param>
    /// <param name="after">Get the next occurrence after this time.</param>
    /// <returns>The next occurrence or an error if the expression is invalid.</returns>
    Either<EncinaError, DateTime> GetNextOccurrence(string cronExpression, DateTime after);
}

/// <summary>
/// Error codes for scheduling operations.
/// </summary>
public static class SchedulingErrorCodes
{
    /// <summary>
    /// Unknown request type during processing.
    /// </summary>
    public const string UnknownRequestType = "scheduling.unknown_request_type";

    /// <summary>
    /// Failed to deserialize request.
    /// </summary>
    public const string DeserializationFailed = "scheduling.deserialization_failed";

    /// <summary>
    /// Failed to execute request.
    /// </summary>
    public const string ExecutionFailed = "scheduling.execution_failed";

    /// <summary>
    /// Recurring messages are disabled.
    /// </summary>
    public const string RecurringDisabled = "scheduling.recurring_disabled";

    /// <summary>
    /// No cron parser configured.
    /// </summary>
    public const string NoCronParser = "scheduling.no_cron_parser";

    /// <summary>
    /// Invalid cron expression.
    /// </summary>
    public const string InvalidCronExpression = "scheduling.invalid_cron_expression";

    /// <summary>
    /// Maximum retries exceeded.
    /// </summary>
    public const string MaxRetriesExceeded = "scheduling.max_retries_exceeded";

    /// <summary>
    /// Scheduled time is in the past.
    /// </summary>
    public const string InvalidScheduleTime = "scheduling.invalid_schedule_time";

    /// <summary>
    /// Delay must be positive.
    /// </summary>
    public const string InvalidDelay = "scheduling.invalid_delay";

    /// <summary>
    /// Deserialized request type implements neither <see cref="IRequest{TResponse}"/>
    /// nor <see cref="INotification"/>.
    /// </summary>
    public const string UnknownRequestShape = "scheduling.unknown_request_shape";
}

/// <summary>
/// LoggerMessage definitions for high-performance logging.
/// </summary>
[ExcludeFromCodeCoverage]
internal static partial class Log
{
    [LoggerMessage(
        EventId = 301,
        Level = LogLevel.Debug,
        Message = "Message {MessageId} scheduled for {ScheduledAt} (type: {RequestType})")]
    public static partial void MessageScheduled(ILogger logger, Guid messageId, string requestType, DateTime scheduledAt);

    [LoggerMessage(
        EventId = 302,
        Level = LogLevel.Debug,
        Message = "Recurring message {MessageId} scheduled with cron '{CronExpression}', next: {NextExecution} (type: {RequestType})")]
    public static partial void RecurringMessageScheduled(ILogger logger, Guid messageId, string requestType, string cronExpression, DateTime nextExecution);

    [LoggerMessage(
        EventId = 303,
        Level = LogLevel.Debug,
        Message = "Message {MessageId} cancelled")]
    public static partial void MessageCancelled(ILogger logger, Guid messageId);

    [LoggerMessage(
        EventId = 304,
        Level = LogLevel.Debug,
        Message = "Message {MessageId} executed successfully")]
    public static partial void MessageExecuted(ILogger logger, Guid messageId);

    [LoggerMessage(
        EventId = 305,
        Level = LogLevel.Debug,
        Message = "Recurring message {MessageId} rescheduled for {NextExecution}")]
    public static partial void RecurringMessageRescheduled(ILogger logger, Guid messageId, DateTime nextExecution);

    [LoggerMessage(
        EventId = 306,
        Level = LogLevel.Information,
        Message = "Recurring message {MessageId} ended (no more occurrences)")]
    public static partial void RecurringMessageEnded(ILogger logger, Guid messageId);

    [LoggerMessage(
        EventId = 307,
        Level = LogLevel.Warning,
        Message = "Unknown request type for message {MessageId}: {RequestType}")]
    public static partial void UnknownRequestType(ILogger logger, Guid messageId, string requestType);

    [LoggerMessage(
        EventId = 308,
        Level = LogLevel.Warning,
        Message = "Failed to deserialize message {MessageId} of type {RequestType}")]
    public static partial void DeserializationFailed(ILogger logger, Guid messageId, string requestType);

    [LoggerMessage(
        EventId = 309,
        Level = LogLevel.Error,
        Message = "Failed to execute message {MessageId}")]
    public static partial void ExecutionFailed(ILogger logger, Exception ex, Guid messageId);

    [LoggerMessage(
        EventId = 310,
        Level = LogLevel.Warning,
        Message = "Dispatch returned failure for message {MessageId}: [{ErrorCode}] {ErrorMessage}")]
    public static partial void DispatchFailed(ILogger logger, Guid messageId, string errorCode, string errorMessage);

    [LoggerMessage(
        EventId = 311,
        Level = LogLevel.Error,
        Message = "Failed to update store for message {MessageId} after dispatch failure: {StoreErrorMessage}")]
    public static partial void StoreMarkAsFailedError(ILogger logger, Guid messageId, string storeErrorMessage);
}
