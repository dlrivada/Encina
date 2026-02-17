using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Encina.Messaging.Recoverability;

/// <summary>
/// Default implementation of <see cref="IDelayedRetryScheduler"/> using the delayed retry store.
/// </summary>
/// <remarks>
/// This scheduler persists retry requests to <see cref="IDelayedRetryStore"/> and relies on
/// a background processor to execute them when due.
/// </remarks>
public sealed class DelayedRetryScheduler : IDelayedRetryScheduler
{
    private readonly IDelayedRetryStore _store;
    private readonly IDelayedRetryMessageFactory _messageFactory;
    private readonly ILogger<DelayedRetryScheduler> _logger;
    private readonly TimeProvider _timeProvider;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="DelayedRetryScheduler"/> class.
    /// </summary>
    /// <param name="store">The delayed retry store.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">Optional time provider for testability.</param>
    public DelayedRetryScheduler(
        IDelayedRetryStore store,
        IDelayedRetryMessageFactory messageFactory,
        ILogger<DelayedRetryScheduler> logger,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(messageFactory);
        ArgumentNullException.ThrowIfNull(logger);

        _store = store;
        _messageFactory = messageFactory;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async Task ScheduleRetryAsync<TRequest>(
        TRequest request,
        RecoverabilityContext context,
        TimeSpan delay,
        int delayedRetryAttempt,
        CancellationToken cancellationToken = default)
        where TRequest : notnull
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        var requestType = typeof(TRequest).AssemblyQualifiedName
            ?? typeof(TRequest).FullName
            ?? typeof(TRequest).Name;

        var requestContent = JsonSerializer.Serialize(request, JsonOptions);
        var contextContent = SerializeContext(context);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var executeAt = now.Add(delay);

        var messageData = new DelayedRetryMessageData(
            Id: Guid.NewGuid(),
            RecoverabilityContextId: context.Id,
            RequestType: requestType,
            RequestContent: requestContent,
            ContextContent: contextContent,
            DelayedRetryAttempt: delayedRetryAttempt,
            ScheduledAtUtc: now,
            ExecuteAtUtc: executeAt,
            CorrelationId: context.CorrelationId);

        var message = _messageFactory.Create(messageData);

        await _store.AddAsync(message, cancellationToken).ConfigureAwait(false);

        DelayedRetryLog.RetryScheduled(
            _logger,
            context.CorrelationId ?? "unknown",
            requestType,
            delayedRetryAttempt + 1,
            executeAt);
    }

    /// <inheritdoc />
    public async Task<bool> CancelScheduledRetryAsync(
        Guid recoverabilityContextId,
        CancellationToken cancellationToken = default)
    {
        var deleted = await _store.DeleteByContextIdAsync(recoverabilityContextId, cancellationToken).ConfigureAwait(false);

        if (deleted)
        {
            DelayedRetryLog.RetryCancelled(_logger, recoverabilityContextId);
        }

        return deleted;
    }

    private static string SerializeContext(RecoverabilityContext context)
    {
        // Serialize only the essential context data needed for retry
        var serializableContext = new SerializableRecoverabilityContext
        {
            Id = context.Id,
            StartedAtUtc = context.StartedAtUtc,
            ImmediateRetryCount = context.ImmediateRetryCount,
            DelayedRetryCount = context.DelayedRetryCount,
            CorrelationId = context.CorrelationId,
            IdempotencyKey = context.IdempotencyKey,
            RequestTypeName = context.RequestTypeName,
            LastErrorMessage = context.LastError?.Message
        };

        return JsonSerializer.Serialize(serializableContext, JsonOptions);
    }
}

/// <summary>
/// Serializable version of <see cref="RecoverabilityContext"/> for persistence.
/// </summary>
internal sealed class SerializableRecoverabilityContext
{
    public Guid Id { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public int ImmediateRetryCount { get; set; }
    public int DelayedRetryCount { get; set; }
    public string? CorrelationId { get; set; }
    public string? IdempotencyKey { get; set; }
    public string? RequestTypeName { get; set; }
    public string? LastErrorMessage { get; set; }
}

/// <summary>
/// LoggerMessage definitions for delayed retry scheduling.
/// </summary>
[ExcludeFromCodeCoverage]
internal static partial class DelayedRetryLog
{
    [LoggerMessage(
        EventId = 220,
        Level = LogLevel.Information,
        Message = "[{CorrelationId}] {RequestType} delayed retry #{Attempt} scheduled for {ExecuteAt:O}")]
    public static partial void RetryScheduled(
        ILogger logger, string correlationId, string requestType, int attempt, DateTime executeAt);

    [LoggerMessage(
        EventId = 221,
        Level = LogLevel.Debug,
        Message = "Delayed retry for context {ContextId} cancelled")]
    public static partial void RetryCancelled(ILogger logger, Guid contextId);
}
