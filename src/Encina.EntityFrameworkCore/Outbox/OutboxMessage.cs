using Encina.Messaging.Outbox;

namespace Encina.EntityFrameworkCore.Outbox;

/// <summary>
/// Entity Framework Core implementation of <see cref="IOutboxMessage"/>.
/// </summary>
/// <remarks>
/// <para>
/// The Outbox Pattern ensures that domain events/notifications are reliably published even in the
/// face of system failures. Instead of publishing events immediately, they are stored in the
/// database as part of the same transaction that modifies the aggregate, then processed by
/// a background worker.
/// </para>
/// <para>
/// <b>Lifecycle</b>:
/// <list type="number">
/// <item><description>Event occurs and handler is invoked</description></item>
/// <item><description>OutboxMessage is saved to database (same transaction as aggregate)</description></item>
/// <item><description>Transaction commits</description></item>
/// <item><description>Background processor publishes event from outbox</description></item>
/// <item><description>Message marked as processed</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class OutboxMessage : IOutboxMessage
{
    /// <summary>
    /// Gets or sets the unique identifier for the outbox message.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the fully qualified type name of the notification.
    /// </summary>
    /// <remarks>
    /// Used for deserialization when processing the outbox.
    /// Format: "Namespace.TypeName, AssemblyName"
    /// </remarks>
    public required string NotificationType { get; set; }

    /// <summary>
    /// Gets or sets the JSON-serialized notification content.
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the message was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the message was processed.
    /// </summary>
    /// <value>
    /// <c>null</c> if the message has not been processed yet.
    /// </value>
    public DateTime? ProcessedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the error message if processing failed.
    /// </summary>
    /// <value>
    /// <c>null</c> if no error occurred or message hasn't been processed.
    /// </value>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the number of times processing has been attempted.
    /// </summary>
    /// <remarks>
    /// Used for retry logic and dead letter queue thresholds.
    /// </remarks>
    public int RetryCount { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the next retry should be attempted.
    /// </summary>
    /// <value>
    /// <c>null</c> if no retry is scheduled or message has been processed successfully.
    /// </value>
    public DateTime? NextRetryAtUtc { get; set; }

    /// <summary>
    /// Gets a value indicating whether this message has been processed successfully.
    /// </summary>
    public bool IsProcessed => ProcessedAtUtc.HasValue && ErrorMessage == null;

    /// <summary>
    /// Gets a value indicating whether this message has failed and should not be retried.
    /// </summary>
    /// <param name="maxRetries">The maximum number of retries allowed.</param>
    /// <returns><c>true</c> if the message has exceeded the retry limit; otherwise, <c>false</c>.</returns>
    public bool IsDeadLettered(int maxRetries) => RetryCount >= maxRetries && !IsProcessed;
}
