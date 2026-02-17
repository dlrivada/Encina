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
    /// <param name="cronParser">Optional cron parser for recurring messages.</param>
    /// <param name="timeProvider">Optional time provider for testability.</param>
    public SchedulerOrchestrator(
        IScheduledMessageStore store,
        SchedulingOptions options,
        ILogger<SchedulerOrchestrator> logger,
        IScheduledMessageFactory messageFactory,
        ICronParser? cronParser = null,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(messageFactory);

        _store = store;
        _options = options;
        _logger = logger;
        _messageFactory = messageFactory;
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
    /// <returns>The scheduled message ID.</returns>
    public async Task<Guid> ScheduleAsync<TRequest>(
        TRequest request,
        DateTime executeAt,
        CancellationToken cancellationToken = default)
        where TRequest : class
    {
        ArgumentNullException.ThrowIfNull(request);

        if (executeAt < _timeProvider.GetUtcNow().UtcDateTime)
        {
            throw new ArgumentException("Scheduled time must be in the future.", nameof(executeAt));
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

        await _store.AddAsync(message, cancellationToken).ConfigureAwait(false);

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
    /// <returns>The scheduled message ID.</returns>
    public Task<Guid> ScheduleAsync<TRequest>(
        TRequest request,
        TimeSpan delay,
        CancellationToken cancellationToken = default)
        where TRequest : class
    {
        if (delay <= TimeSpan.Zero)
        {
            throw new ArgumentException("Delay must be positive.", nameof(delay));
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
        if (nextExecutionResult.IsLeft)
        {
            return nextExecutionResult.Match(
                Right: _ => throw new InvalidOperationException(),
                Left: error => error);
        }

        var nextExecution = nextExecutionResult.Match(
            Right: dt => dt,
            Left: _ => throw new InvalidOperationException());

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

        return message.Id;
    }

    /// <summary>
    /// Cancels a scheduled message.
    /// </summary>
    /// <param name="messageId">The message ID to cancel.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    public async Task CancelAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        await _store.CancelAsync(messageId, cancellationToken).ConfigureAwait(false);
        Log.MessageCancelled(_logger, messageId);
    }

    /// <summary>
    /// Processes due scheduled messages.
    /// </summary>
    /// <param name="executeCallback">The callback to execute each message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of messages processed successfully.</returns>
    public async Task<int> ProcessDueMessagesAsync(
        Func<IScheduledMessage, Type, object, Task> executeCallback,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(executeCallback);

        var messages = await _store.GetDueMessagesAsync(
            _options.BatchSize,
            _options.MaxRetries,
            cancellationToken).ConfigureAwait(false);

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
                    await MarkAsFailedAsync(message.Id, $"Unknown request type: {message.RequestType}", cancellationToken).ConfigureAwait(false);
                    continue;
                }

                var request = JsonSerializer.Deserialize(message.Content, requestType, JsonOptions);
                if (request == null)
                {
                    Log.DeserializationFailed(_logger, message.Id, message.RequestType);
                    await MarkAsFailedAsync(message.Id, "Failed to deserialize request", cancellationToken).ConfigureAwait(false);
                    continue;
                }

                await executeCallback(message, requestType, request).ConfigureAwait(false);

                if (message.IsRecurring)
                {
                    await HandleRecurringMessageAsync(message, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await _store.MarkAsProcessedAsync(message.Id, cancellationToken).ConfigureAwait(false);
                }

                processedCount++;
                Log.MessageExecuted(_logger, message.Id);
            }
            catch (Exception ex)
            {
                Log.ExecutionFailed(_logger, ex, message.Id);
                await MarkAsFailedAsync(message.Id, ex.Message, cancellationToken).ConfigureAwait(false);
            }
        }

        return processedCount;
    }

    /// <summary>
    /// Gets the count of pending scheduled messages.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The count of pending messages.</returns>
    public async Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default)
    {
        var messages = await _store.GetDueMessagesAsync(
            int.MaxValue,
            _options.MaxRetries,
            cancellationToken).ConfigureAwait(false);

        return messages.Count();
    }

    private async Task HandleRecurringMessageAsync(IScheduledMessage message, CancellationToken cancellationToken)
    {
        if (_cronParser == null || string.IsNullOrEmpty(message.CronExpression))
        {
            await _store.MarkAsProcessedAsync(message.Id, cancellationToken).ConfigureAwait(false);
            return;
        }

        var nextExecutionResult = _cronParser.GetNextOccurrence(message.CronExpression, _timeProvider.GetUtcNow().UtcDateTime);

        if (nextExecutionResult.IsRight)
        {
            var nextExecution = nextExecutionResult.Match(
                Right: dt => dt,
                Left: _ => throw new InvalidOperationException());

            await _store.RescheduleRecurringMessageAsync(message.Id, nextExecution, cancellationToken).ConfigureAwait(false);
            Log.RecurringMessageRescheduled(_logger, message.Id, nextExecution);
        }
        else
        {
            await _store.MarkAsProcessedAsync(message.Id, cancellationToken).ConfigureAwait(false);
            Log.RecurringMessageEnded(_logger, message.Id);
        }
    }

    private async Task MarkAsFailedAsync(Guid messageId, string errorMessage, CancellationToken cancellationToken)
    {
        var nextRetryAt = _timeProvider.GetUtcNow().UtcDateTime.Add(_options.BaseRetryDelay);
        await _store.MarkAsFailedAsync(messageId, errorMessage, nextRetryAt, cancellationToken).ConfigureAwait(false);
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
}
