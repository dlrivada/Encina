namespace Encina.Messaging.Inbox;

/// <summary>
/// Represents a message in the inbox for idempotent processing.
/// </summary>
/// <remarks>
/// <para>
/// The Inbox Pattern ensures exactly-once processing of messages by storing
/// a record of processed messages. Before processing a message, the system
/// checks if it has already been processed.
/// </para>
/// <para>
/// <b>Key Properties</b>:
/// <list type="bullet">
/// <item><description><b>MessageId</b>: Unique identifier (typically from IdempotencyKey)</description></item>
/// <item><description><b>Response</b>: Cached response for duplicate requests</description></item>
/// <item><description><b>ExpiresAtUtc</b>: When to clean up old messages</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IInboxMessage
{
    /// <summary>
    /// Gets or sets the unique message identifier.
    /// </summary>
    /// <remarks>
    /// This is typically the IdempotencyKey from the request context.
    /// Can be any string format (GUID, UUID, correlation ID, etc.).
    /// </remarks>
    string MessageId { get; set; }

    /// <summary>
    /// Gets or sets the type of the request.
    /// </summary>
    string RequestType { get; set; }

    /// <summary>
    /// Gets or sets the serialized response.
    /// </summary>
    /// <remarks>
    /// Stores the successful response to return for duplicate requests.
    /// </remarks>
    string? Response { get; set; }

    /// <summary>
    /// Gets or sets the error message if processing failed.
    /// </summary>
    string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets when the message was received.
    /// </summary>
    DateTime ReceivedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets when the message was processed.
    /// </summary>
    DateTime? ProcessedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets when this message expires and can be cleaned up.
    /// </summary>
    DateTime ExpiresAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the number of processing attempts.
    /// </summary>
    int RetryCount { get; set; }

    /// <summary>
    /// Gets or sets when to retry processing (for failed messages).
    /// </summary>
    DateTime? NextRetryAtUtc { get; set; }

    /// <summary>
    /// Gets a value indicating whether the message has been processed successfully.
    /// </summary>
    bool IsProcessed { get; }

    /// <summary>
    /// Gets a value indicating whether the message has expired.
    /// </summary>
    /// <returns>True if the message has expired.</returns>
    bool IsExpired();
}
