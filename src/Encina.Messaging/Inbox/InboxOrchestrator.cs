using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
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
    private readonly IInboxStore _store;
    private readonly InboxOptions _options;
    private readonly ILogger<InboxOrchestrator> _logger;
    private readonly IInboxMessageFactory _messageFactory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="InboxOrchestrator"/> class.
    /// </summary>
    /// <param name="store">The inbox store for persistence.</param>
    /// <param name="options">The inbox options.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="messageFactory">Factory to create inbox messages.</param>
    public InboxOrchestrator(
        IInboxStore store,
        InboxOptions options,
        ILogger<InboxOrchestrator> logger,
        IInboxMessageFactory messageFactory)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(messageFactory);

        _store = store;
        _options = options;
        _logger = logger;
        _messageFactory = messageFactory;
    }

    /// <summary>
    /// Processes a request idempotently using the Inbox Pattern.
    /// </summary>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="messageId">The message ID (idempotency key).</param>
    /// <param name="requestType">The type of the request.</param>
    /// <param name="correlationId">The correlation ID for logging.</param>
    /// <param name="metadata">Additional metadata to store.</param>
    /// <param name="processCallback">The callback to process the request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of processing or the cached response.</returns>
    public async ValueTask<Either<EncinaError, TResponse>> ProcessAsync<TResponse>(
        string messageId,
        string requestType,
        string correlationId,
        InboxMetadata? metadata,
        Func<ValueTask<Either<EncinaError, TResponse>>> processCallback,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestType);
        ArgumentNullException.ThrowIfNull(processCallback);

        Log.ProcessingIdempotentRequest(_logger, requestType, messageId, correlationId);

        // Check if message already exists in inbox
        var existingMessage = await _store.GetMessageAsync(messageId, cancellationToken).ConfigureAwait(false);

        if (existingMessage != null)
        {
            return await HandleExistingMessageAsync<TResponse>(
                existingMessage, messageId, correlationId, processCallback, cancellationToken).ConfigureAwait(false);
        }

        // Create new inbox entry
        var now = DateTime.UtcNow;
        var newMessage = _messageFactory.Create(
            messageId,
            requestType,
            now,
            now.Add(_options.MessageRetentionPeriod),
            metadata);

        await _store.AddAsync(newMessage, cancellationToken).ConfigureAwait(false);

        return await ProcessAndCacheResponseAsync(
            messageId, correlationId, processCallback, cancellationToken).ConfigureAwait(false);
    }

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
        string messageId,
        string correlationId,
        Func<ValueTask<Either<EncinaError, TResponse>>> processCallback,
        CancellationToken cancellationToken)
    {
        // Message already processed - return cached response
        if (existingMessage.IsProcessed && existingMessage.Response != null)
        {
            Log.ReturningCachedResponse(_logger, messageId, correlationId);
            return DeserializeResponse<TResponse>(existingMessage.Response);
        }

        // Message exists but failed - retry if within limit
        if (existingMessage.RetryCount >= _options.MaxRetries)
        {
            Log.MaxRetriesExceeded(_logger, messageId, _options.MaxRetries, correlationId);
            return EncinaErrors.Create(
                InboxErrorCodes.MaxRetriesExceeded,
                $"Message has failed {existingMessage.RetryCount} times and will not be retried");
        }

        // Increment retry count and process
        await _store.IncrementRetryCountAsync(messageId, cancellationToken).ConfigureAwait(false);

        return await ProcessAndCacheResponseAsync(
            messageId, correlationId, processCallback, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<Either<EncinaError, TResponse>> ProcessAndCacheResponseAsync<TResponse>(
        string messageId,
        string correlationId,
        Func<ValueTask<Either<EncinaError, TResponse>>> processCallback,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await processCallback().ConfigureAwait(false);

            // Store response in inbox
            var serializedResponse = SerializeResponse(result);
            await _store.MarkAsProcessedAsync(messageId, serializedResponse, cancellationToken).ConfigureAwait(false);

            Log.ProcessedAndCachedMessage(_logger, messageId, correlationId);

            return result;
        }
        catch (Exception ex)
        {
            Log.ErrorProcessingMessage(_logger, ex, messageId, correlationId);

            await _store.MarkAsFailedAsync(
                messageId,
                ex.Message,
                DateTime.UtcNow.AddMinutes(1), // Simple backoff, can be made configurable
                cancellationToken).ConfigureAwait(false);

            throw;
        }
    }

    private static string SerializeResponse<TResponse>(Either<EncinaError, TResponse> response)
    {
        var envelope = response.Match(
            Right: value => new ResponseEnvelope<TResponse> { IsSuccess = true, Value = value },
            Left: error => new ResponseEnvelope<TResponse> { IsSuccess = false, ErrorMessage = error.Message });

        return JsonSerializer.Serialize(envelope, JsonOptions);
    }

    private static Either<EncinaError, TResponse> DeserializeResponse<TResponse>(string json)
    {
        var envelope = JsonSerializer.Deserialize<ResponseEnvelope<TResponse>>(json, JsonOptions);
        if (envelope == null)
        {
            return EncinaErrors.Create(
                InboxErrorCodes.DeserializationFailed,
                "Failed to deserialize cached response");
        }

        var value = envelope.Value;

        if (envelope.IsSuccess && !EqualityComparer<TResponse>.Default.Equals(value, default!))
        {
            return Right<EncinaError, TResponse>(value!);
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
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Missing IdempotencyKey for idempotent request {RequestType} (correlation: {CorrelationId})")]
    public static partial void MissingIdempotencyKey(ILogger logger, string requestType, string correlationId);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "Processing idempotent request {RequestType} with MessageId {MessageId} (correlation: {CorrelationId})")]
    public static partial void ProcessingIdempotentRequest(ILogger logger, string requestType, string messageId, string correlationId);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "Returning cached response for MessageId {MessageId} (correlation: {CorrelationId})")]
    public static partial void ReturningCachedResponse(ILogger logger, string messageId, string correlationId);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Warning,
        Message = "Max retries ({MaxRetries}) exceeded for MessageId {MessageId} (correlation: {CorrelationId})")]
    public static partial void MaxRetriesExceeded(ILogger logger, string messageId, int maxRetries, string correlationId);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Debug,
        Message = "Processed and cached response for MessageId {MessageId} (correlation: {CorrelationId})")]
    public static partial void ProcessedAndCachedMessage(ILogger logger, string messageId, string correlationId);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Error,
        Message = "Error processing message {MessageId} (correlation: {CorrelationId})")]
    public static partial void ErrorProcessingMessage(ILogger logger, Exception ex, string messageId, string correlationId);
}
