using SimpleMediator.Messaging.Inbox;

namespace SimpleMediator.EntityFrameworkCore.Inbox;

/// <summary>
/// Entity Framework Core implementation of <see cref="IInboxMessage"/>.
/// Represents a message stored in the inbox pattern for idempotent message processing.
/// </summary>
/// <remarks>
/// <para>
/// The Inbox Pattern ensures that incoming messages (commands, events from external systems)
/// are processed exactly once, even if they are received multiple times due to retries or
/// network duplications.
/// </para>
/// <para>
/// <b>Lifecycle</b>:
/// <list type="number">
/// <item><description>Message arrives (from queue, webhook, API, etc.)</description></item>
/// <item><description>Check if MessageId exists in inbox</description></item>
/// <item><description>If exists, return cached response (idempotent)</description></item>
/// <item><description>If not, store in inbox and process</description></item>
/// <item><description>Cache response for future duplicate requests</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Key Difference from Outbox</b>:
/// <list type="bullet">
/// <item><description><b>Inbox</b>: For INCOMING messages (idempotent consumers)</description></item>
/// <item><description><b>Outbox</b>: For OUTGOING messages (reliable publishing)</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class InboxMessage : IInboxMessage
{
    /// <summary>
    /// Gets or sets the unique identifier for the incoming message.
    /// </summary>
    /// <remarks>
    /// This is typically provided by the message source (queue message ID, webhook ID, etc.).
    /// Must be globally unique across all messages.
    /// </remarks>
    public required string MessageId { get; set; }

    /// <summary>
    /// Gets or sets the fully qualified type name of the request/command.
    /// </summary>
    /// <remarks>
    /// Used for deserialization and logging.
    /// Format: "Namespace.TypeName, AssemblyName"
    /// </remarks>
    public required string RequestType { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the message was received.
    /// </summary>
    public DateTime ReceivedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the message was processed.
    /// </summary>
    /// <value>
    /// <c>null</c> if the message has not been processed yet.
    /// </value>
    public DateTime? ProcessedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the cached response from processing the message.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Stored as JSON to support idempotent responses. If the same message
    /// arrives again, this response is returned without re-processing.
    /// </para>
    /// <para>
    /// For commands that return <c>Either&lt;MediatorError, T&gt;</c>, this
    /// contains the serialized result (both Left and Right cases).
    /// </para>
    /// </remarks>
    public string? Response { get; set; }

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
    public int RetryCount { get; set; }

    /// <summary>
    /// Gets or sets when to retry processing (for failed messages).
    /// </summary>
    public DateTime? NextRetryAtUtc { get; set; }

    /// <summary>
    /// Gets or sets optional metadata about the message source.
    /// </summary>
    /// <remarks>
    /// Can store correlation IDs, source system, tenant ID, etc.
    /// Stored as JSON.
    /// </remarks>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when this record expires and can be purged.
    /// </summary>
    /// <remarks>
    /// Inbox messages should be retained for a reasonable period (e.g., 7-30 days)
    /// to handle delayed duplicates, then purged to prevent unbounded growth.
    /// </remarks>
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>
    /// Gets a value indicating whether this message has been processed successfully.
    /// </summary>
    public bool IsProcessed => ProcessedAtUtc.HasValue && ErrorMessage == null;

    /// <summary>
    /// Gets a value indicating whether this message has expired.
    /// </summary>
    /// <returns>True if the message has expired.</returns>
    public bool IsExpired() => ExpiresAtUtc <= DateTime.UtcNow;
}
