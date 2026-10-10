using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Encina.Diagnostics;
using Encina.Messaging.DeadLetter;
using Encina.Messaging.Serialization;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Encina.Messaging.Inbox;

/// <summary>
/// Orchestrates the Inbox Pattern for idempotent message processing.
/// </summary>
/// <remarks>
/// <para>
/// This orchestrator contains all domain logic for the Inbox Pattern, delegating
/// persistence operations to <see cref="IInboxStore"/>. It ensures exactly-once
/// processing semantics by tracking processed messages.
/// </para>
/// <para>
/// <b>Processing Flow</b>:
/// <list type="number">
/// <item><description>Check if MessageId exists in context (required for idempotent requests)</description></item>
/// <item><description>Look up message in inbox by MessageId</description></item>
/// <item><description>If found and processed, return cached response</description></item>
/// <item><description>If not found, create inbox entry</description></item>
/// <item><description>Process request via callback</description></item>
/// <item><description>Store response in inbox</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class InboxOrchestrator
{
    // The error code of a processing attempt that threw.
    private const string InboxProcessingFailed = "inbox.processing_failed";

    private readonly IInboxStore _store;
    private readonly InboxOptions _options;
    private readonly ILogger<InboxOrchestrator> _logger;
    private readonly IInboxMessageFactory _messageFactory;
    private readonly IMessageSerializer _messageSerializer;
    private readonly TimeProvider _timeProvider;
    private readonly DeadLetterSourceCapture? _deadLetterCapture;

    /// <summary>
    /// Initializes a new instance of the <see cref="InboxOrchestrator"/> class.
    /// </summary>
    /// <param name="store">The inbox store for persistence.</param>
    /// <param name="options">The inbox options.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="messageFactory">Factory to create inbox messages.</param>
    /// <param name="messageSerializer">The message serializer for response caching.</param>
    /// <param name="timeProvider">Optional time provider for testability.</param>
    /// <param name="deadLetterCapture">
    /// Optional dead letter capture, registered with the dead letter queue: while
    /// <c>DeadLetterOptions.IntegrateWithInbox</c> is on, the failed attempt that uses up
    /// <see cref="InboxOptions.MaxRetries"/> captures the request (keyed by the inbox message id), and a
    /// redelivery rejected afterwards captures it again idempotently, so a failed capture is repaired.
    /// </param>
    public InboxOrchestrator(
        IInboxStore store,
        InboxOptions options,
        ILogger<InboxOrchestrator> logger,
        IInboxMessageFactory messageFactory,
        IMessageSerializer messageSerializer,
        TimeProvider? timeProvider = null,
        DeadLetterSourceCapture? deadLetterCapture = null)
    {
        _deadLetterCapture = deadLetterCapture;
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(messageFactory);
        ArgumentNullException.ThrowIfNull(messageSerializer);

        _store = store;
        _options = options;
        _logger = logger;
        _messageFactory = messageFactory;
        _messageSerializer = messageSerializer;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Processes a request idempotently using the Inbox Pattern.
    /// </summary>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="request">
    /// The request being processed; it is what the dead letter queue stores when the message uses up its retries.
    /// </param>
    /// <param name="messageId">The message ID (idempotency key).</param>
    /// <param name="requestType">The type of the request.</param>
    /// <param name="correlationId">The correlation ID for logging.</param>
    /// <param name="metadata">Additional metadata to store.</param>
    /// <param name="processCallback">The callback to process the request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The result of processing or the cached response. When the dead letter capture of a message that used
    /// up its retries fails, its <c>Left</c> is returned instead of the processing error.
    /// </returns>
    public async ValueTask<Either<EncinaError, TResponse>> ProcessAsync<TResponse>(
        object request,
        string messageId,
        string requestType,
        string correlationId,
        InboxMetadata? metadata,
        Func<ValueTask<Either<EncinaError, TResponse>>> processCallback,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestType);
        ArgumentNullException.ThrowIfNull(processCallback);

        Log.ProcessingIdempotentRequest(_logger, requestType, messageId, correlationId);

        // Check if message already exists in inbox
        var existingResult = await _store.GetMessageAsync(messageId, cancellationToken).ConfigureAwait(false);

        if (existingResult.IsLeft)
            return existingResult.LeftToArray()[0];

        var existingOption = existingResult.Match(Right: o => o, Left: _ => Option<IInboxMessage>.None);

        if (existingOption.IsSome)
        {
            var existingMessage = existingOption.Match(Some: m => m, None: () => default!);
            var attempt = new InboxAttempt(
                request, messageId, correlationId, metadata?.TenantId, existingMessage.RetryCount, existingMessage.ReceivedAtUtc);
            return await HandleExistingMessageAsync(
                existingMessage, attempt, processCallback, cancellationToken).ConfigureAwait(false);
        }

        // Create new inbox entry
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var newMessage = _messageFactory.Create(
            messageId,
            requestType,
            now,
            now.Add(_options.MessageRetentionPeriod),
            metadata);

        var addResult = await _store.AddAsync(newMessage, cancellationToken).ConfigureAwait(false);
        if (addResult.IsLeft)
            return addResult.LeftToArray()[0];

        return await ProcessAndCacheResponseAsync(
            new InboxAttempt(request, messageId, correlationId, metadata?.TenantId, FailedAttemptsBefore: 0, now),
            processCallback,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// One processing attempt of an inbox message: what a dead letter of it records.
    /// </summary>
    private sealed record InboxAttempt(
        object Request,
        string MessageId,
        string CorrelationId,
        string? TenantId,
        int FailedAttemptsBefore,
        DateTime ReceivedAtUtc);

    /// <summary>
    /// Validates that a message ID is present for idempotent processing.
    /// </summary>
    /// <param name="messageId">The message ID to validate.</param>
    /// <param name="requestType">The request type for error messages.</param>
    /// <param name="correlationId">The correlation ID for logging.</param>
    /// <returns>An error if the message ID is missing, otherwise None.</returns>
    public Option<EncinaError> ValidateMessageId(string? messageId, string requestType, string correlationId)
    {
        if (string.IsNullOrWhiteSpace(messageId))
        {
            Log.MissingIdempotencyKey(_logger, requestType, correlationId);
            return Some(EncinaErrors.Create(
                InboxErrorCodes.MissingMessageId,
                "Idempotent requests require a MessageId (IdempotencyKey)"));
        }

        return None;
    }

    private async ValueTask<Either<EncinaError, TResponse>> HandleExistingMessageAsync<TResponse>(
        IInboxMessage existingMessage,
        InboxAttempt attempt,
        Func<ValueTask<Either<EncinaError, TResponse>>> processCallback,
        CancellationToken cancellationToken)
    {
        var messageId = attempt.MessageId;
        var correlationId = attempt.CorrelationId;

        // Message already processed - return cached response
        if (existingMessage.IsProcessed && existingMessage.Response != null)
        {
            Log.ReturningCachedResponse(_logger, messageId, correlationId);
            return DeserializeResponse<TResponse>(existingMessage.Response);
        }

        // RetryCount counts failed attempts (the store increments it in MarkAsFailedAsync, once per
        // failed attempt). Once it reaches MaxRetries the handler has already run MaxRetries times.
        if (existingMessage.RetryCount >= _options.MaxRetries)
        {
            Log.MaxRetriesExceeded(_logger, messageId, _options.MaxRetries, correlationId);

            // Captured again, idempotently: a capture that failed on the attempt that used up the retries is
            // repaired by the next redelivery, and an existing dead letter is kept as it is.
            var captured = await CaptureDeadLetterAsync(
                attempt, existingMessage.RetryCount, exception: null, cancellationToken).ConfigureAwait(false);
            if (captured.IsLeft)
                return captured.LeftToArray()[0];

            return EncinaErrors.Create(
                InboxErrorCodes.MaxRetriesExceeded,
                $"Message has failed {existingMessage.RetryCount} times and will not be retried");
        }

        return await ProcessAndCacheResponseAsync(attempt, processCallback, cancellationToken).ConfigureAwait(false);
    }

    // Captures the request of a message that used up its retries; Right when there is nothing to capture.
    // Only a thrown exception consumes a retry, so the dead letter's error code is inbox.processing_failed.
    private Task<Either<EncinaError, Unit>> CaptureDeadLetterAsync(
        InboxAttempt attempt,
        int failedAttempts,
        Exception? exception,
        CancellationToken cancellationToken)
    {
        if (_deadLetterCapture is null)
            return Task.FromResult<Either<EncinaError, Unit>>(Unit.Default);

        return _deadLetterCapture.CaptureAsync(
            attempt.Request,
            new DeadLetterContext(
                EncinaErrors.Create(InboxProcessingFailed, "Inbox message used up its retries"),
                exception,
                DeadLetterSourcePatterns.Inbox,
                failedAttempts,
                DeadLetterInputs.AsUtc(attempt.ReceivedAtUtc),
                attempt.CorrelationId,
                attempt.MessageId,
                string.IsNullOrEmpty(attempt.TenantId) ? null : attempt.TenantId),
            cancellationToken);
    }

    // A thrown exception is a failed attempt: the store records it and increments RetryCount, and the
    // attempt that uses up MaxRetries is the terminal failure whose request is dead-lettered.
    private async Task<EncinaError> FailAttemptAsync(InboxAttempt attempt, Exception ex, CancellationToken cancellationToken)
    {
        Log.ErrorProcessingMessage(_logger, ex.ForLogging(), attempt.MessageId, attempt.CorrelationId);

        var failed = await _store.MarkAsFailedAsync(
            attempt.MessageId,
            ex.GetType().FullName ?? ex.GetType().Name, // type only: the message may carry personal data
            _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(1), // Simple backoff, can be made configurable
            cancellationToken).ConfigureAwait(false);

        if (failed.IsLeft)
            return failed.LeftToArray()[0];

        var failedAttempts = attempt.FailedAttemptsBefore + 1;
        if (failedAttempts >= _options.MaxRetries)
        {
            var captured = await CaptureDeadLetterAsync(attempt, failedAttempts, ex, cancellationToken).ConfigureAwait(false);
            if (captured.IsLeft)
                return captured.LeftToArray()[0];
        }

        return EncinaErrors.FromException(InboxProcessingFailed, ex,
            $"Error processing inbox message {attempt.MessageId}");
    }

    private async ValueTask<Either<EncinaError, TResponse>> ProcessAndCacheResponseAsync<TResponse>(
        InboxAttempt attempt,
        Func<ValueTask<Either<EncinaError, TResponse>>> processCallback,
        CancellationToken cancellationToken)
    {
        var messageId = attempt.MessageId;
        var correlationId = attempt.CorrelationId;

        Either<EncinaError, TResponse> result;
        string serializedResponse;

        try
        {
            result = await processCallback().ConfigureAwait(false);
            serializedResponse = SerializeResponse(result);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The caller cancelled: that is not a failed attempt and must not consume a retry.
            throw;
        }
        catch (Exception ex)
        {
            return await FailAttemptAsync(attempt, ex, cancellationToken).ConfigureAwait(false);
        }

        // A handler Left is a business outcome (ADR-001): it is cached as the processed response and
        // does not consume retries. The business transaction rolls back on a Left, so the cached Left is
        // written outside it; a Right is marked processed atomically with the business effect (ADR-048).
        // A store Left fails the operation instead of reporting success.
        var processed = result.IsRight
            ? await _store.MarkAsProcessedAsync(messageId, serializedResponse, cancellationToken).ConfigureAwait(false)
            : await _store.CacheHandlerErrorAsync(messageId, serializedResponse, cancellationToken).ConfigureAwait(false);
        if (processed.IsLeft)
            return processed.LeftToArray()[0];

        Log.ProcessedAndCachedMessage(_logger, messageId, correlationId);

        return result;
    }

    private string SerializeResponse<TResponse>(Either<EncinaError, TResponse> response)
    {
        // Only the error code: EncinaError.Message can carry personal data (#1259 review), and the
        // envelope is serialized and cached in the inbox store's response payload.
        var envelope = response.Match(
            Right: value => new ResponseEnvelope<TResponse> { IsSuccess = true, Value = value },
            Left: error => new ResponseEnvelope<TResponse> { IsSuccess = false, ErrorMessage = error.GetCode().IfNone("encina.unknown") });

        return _messageSerializer.Serialize(envelope);
    }

    private Either<EncinaError, TResponse> DeserializeResponse<TResponse>(string json)
    {
        var envelope = _messageSerializer.Deserialize<ResponseEnvelope<TResponse>>(json);
        if (envelope == null)
        {
            return EncinaErrors.Create(
                InboxErrorCodes.DeserializationFailed,
                "Failed to deserialize cached response");
        }

        if (envelope.IsSuccess)
        {
            // A successful response equal to default(TResponse) (0, false, Unit) is still the cached success.
            return Right<EncinaError, TResponse>(envelope.Value!); // NOSONAR S6966: LanguageExt Right is a pure function
        }

        return EncinaErrors.Create(
            InboxErrorCodes.CachedError,
            envelope.ErrorMessage ?? "Unknown error in cached response");
    }

    private sealed class ResponseEnvelope<T>
    {
        public bool IsSuccess { get; set; }
        public T? Value { get; set; }
        public string? ErrorMessage { get; set; }
    }
}

/// <summary>
/// Metadata to store with an inbox message.
/// </summary>
public sealed class InboxMetadata
{
    /// <summary>
    /// Gets or sets the correlation ID.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the tenant ID.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
}

/// <summary>
/// Factory interface for creating inbox messages.
/// </summary>
/// <remarks>
/// Each provider (EF Core, Dapper, ADO.NET) implements this to create their specific message type.
/// </remarks>
public interface IInboxMessageFactory
{
    /// <summary>
    /// Creates a new inbox message.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <param name="requestType">The request type.</param>
    /// <param name="receivedAtUtc">When the message was received.</param>
    /// <param name="expiresAtUtc">When the message expires.</param>
    /// <param name="metadata">Optional metadata.</param>
    /// <returns>A new inbox message instance.</returns>
    IInboxMessage Create(
        string messageId,
        string requestType,
        DateTime receivedAtUtc,
        DateTime expiresAtUtc,
        InboxMetadata? metadata);
}

/// <summary>
/// Error codes for inbox operations.
/// </summary>
public static class InboxErrorCodes
{
    /// <summary>
    /// Missing message ID for idempotent request.
    /// </summary>
    public const string MissingMessageId = "inbox.missing_message_id";

    /// <summary>
    /// Maximum retries exceeded.
    /// </summary>
    public const string MaxRetriesExceeded = "inbox.max_retries_exceeded";

    /// <summary>
    /// Failed to deserialize cached response.
    /// </summary>
    public const string DeserializationFailed = "inbox.deserialization_failed";

    /// <summary>
    /// Cached error from previous processing.
    /// </summary>
    public const string CachedError = "inbox.cached_error";
}

/// <summary>
/// LoggerMessage definitions for high-performance logging.
/// </summary>
[ExcludeFromCodeCoverage]
internal static partial class Log
{
    [LoggerMessage(
        EventId = 2812,
        Level = LogLevel.Warning,
        Message = "Missing IdempotencyKey for idempotent request {RequestType} (correlation: {CorrelationId})")]
    public static partial void MissingIdempotencyKey(ILogger logger, string requestType, string correlationId);

    [LoggerMessage(
        EventId = 2813,
        Level = LogLevel.Debug,
        Message = "Processing idempotent request {RequestType} with MessageId {MessageId} (correlation: {CorrelationId})")]
    public static partial void ProcessingIdempotentRequest(ILogger logger, string requestType, string messageId, string correlationId);

    [LoggerMessage(
        EventId = 2814,
        Level = LogLevel.Debug,
        Message = "Returning cached response for MessageId {MessageId} (correlation: {CorrelationId})")]
    public static partial void ReturningCachedResponse(ILogger logger, string messageId, string correlationId);

    [LoggerMessage(
        EventId = 2815,
        Level = LogLevel.Warning,
        Message = "Max retries ({MaxRetries}) exceeded for MessageId {MessageId} (correlation: {CorrelationId})")]
    public static partial void MaxRetriesExceeded(ILogger logger, string messageId, int maxRetries, string correlationId);

    [LoggerMessage(
        EventId = 2816,
        Level = LogLevel.Debug,
        Message = "Processed and cached response for MessageId {MessageId} (correlation: {CorrelationId})")]
    public static partial void ProcessedAndCachedMessage(ILogger logger, string messageId, string correlationId);

    [LoggerMessage(
        EventId = 2817,
        Level = LogLevel.Error,
        Message = "Error processing message {MessageId} (correlation: {CorrelationId})")]
    public static partial void ErrorProcessingMessage(ILogger logger, Exception ex, string messageId, string correlationId);
}
