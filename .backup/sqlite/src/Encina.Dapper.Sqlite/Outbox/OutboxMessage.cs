using Encina.Messaging.Outbox;

namespace Encina.Dapper.Sqlite.Outbox;

/// <summary>
/// Dapper implementation of outbox message for reliable event publishing.
/// Simple POCO optimized for Dapper's object mapper.
/// </summary>
public sealed class OutboxMessage : IOutboxMessage
{
    /// <summary>
    /// Gets or sets the unique identifier for the message.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the full type name of the notification.
    /// </summary>
    public string NotificationType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the JSON serialized content of the notification.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the message was created (UTC).
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets when the message was successfully processed (UTC).
    /// Null if not yet processed.
    /// </summary>
    public DateTime? ProcessedAtUtc { get; set; }

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

    /// <inheritdoc />
    public bool IsProcessed => ProcessedAtUtc.HasValue && ErrorMessage == null;

    /// <inheritdoc />
    public bool IsDeadLettered(int maxRetries) => RetryCount >= maxRetries && !IsProcessed;
}
