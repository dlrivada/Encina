using Encina.Messaging.Inbox;

namespace Encina.ADO.SqlServer.Inbox;

/// <summary>
/// Dapper implementation of inbox message for idempotent request processing.
/// Tracks processed messages to prevent duplicate processing.
/// </summary>
public sealed class InboxMessage : IInboxMessage
{
    /// <summary>
    /// Gets or sets the unique identifier for the message (typically from external system).
    /// </summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the full type name of the request.
    /// </summary>
    public string RequestType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the message was received (UTC).
    /// </summary>
    public DateTime ReceivedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets when the message was successfully processed (UTC).
    /// Null if not yet processed.
    /// </summary>
    public DateTime? ProcessedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets when the message expires and can be purged (UTC).
    /// </summary>
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the cached response JSON.
    /// </summary>
    public string? Response { get; set; }

    /// <summary>
    /// Gets or sets the error message if processing failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the number of retry attempts.
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Gets or sets when the next retry should be attempted (UTC).
    /// Null if no retry is scheduled.
    /// </summary>
    public DateTime? NextRetryAtUtc { get; set; }

    /// <summary>
    /// Gets or sets optional metadata (JSON) about the request context.
    /// </summary>
    public string? Metadata { get; set; }

    /// <inheritdoc />
    public bool IsProcessed => ProcessedAtUtc.HasValue && ErrorMessage == null;

    /// <inheritdoc />
    public bool IsExpired() => DateTime.UtcNow > ExpiresAtUtc;
}
