namespace Encina.Messaging.Outbox;

/// <summary>
/// Represents a message in the outbox pattern for reliable event publishing.
/// </summary>
/// <remarks>
/// <para>
/// This interface provides a provider-agnostic abstraction for outbox messages.
/// Implementations can use Entity Framework Core, Dapper, ADO.NET, or any custom storage.
/// </para>
/// <para>
/// The Outbox Pattern ensures that domain events are published reliably:
/// <list type="number">
/// <item><description>Events are saved to the database in the same transaction as domain changes</description></item>
/// <item><description>Background processor publishes events from the outbox</description></item>
/// <item><description>Retry logic handles transient failures</description></item>
/// <item><description>Guarantees: At-least-once delivery, durability, ordering</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IOutboxMessage
{
    /// <summary>
    /// Gets or sets the unique identifier for the outbox message.
    /// </summary>
    Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the fully qualified type name of the notification.
    /// </summary>
    string NotificationType { get; set; }

    /// <summary>
    /// Gets or sets the serialized notification content.
    /// </summary>
    string Content { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the message was created.
    /// </summary>
    DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the message was processed.
    /// </summary>
    DateTime? ProcessedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the error message if processing failed.
    /// </summary>
    string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the number of retry attempts.
    /// </summary>
    int RetryCount { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp for the next retry attempt.
    /// </summary>
    DateTime? NextRetryAtUtc { get; set; }

    /// <summary>
    /// Gets a value indicating whether this message has been processed successfully.
    /// </summary>
    bool IsProcessed { get; }

    /// <summary>
    /// Gets a value indicating whether this message should go to dead letter queue.
    /// </summary>
    bool IsDeadLettered(int maxRetries);
}
