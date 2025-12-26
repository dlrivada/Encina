namespace Encina.Messaging.DeadLetter;

/// <summary>
/// Filter criteria for querying dead letter messages.
/// </summary>
public sealed class DeadLetterFilter
{
    /// <summary>
    /// Gets or sets the source pattern to filter by.
    /// </summary>
    /// <remarks>
    /// Examples: "Outbox", "Inbox", "Recoverability", "Saga", "Scheduling".
    /// </remarks>
    public string? SourcePattern { get; set; }

    /// <summary>
    /// Gets or sets the request type to filter by.
    /// </summary>
    public string? RequestType { get; set; }

    /// <summary>
    /// Gets or sets the error code to filter by.
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Gets or sets the correlation ID to filter by.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets whether to include only non-replayed messages.
    /// </summary>
    /// <value>
    /// true to include only non-replayed messages,
    /// false to include only replayed messages,
    /// null to include all messages.
    /// </value>
    public bool? ExcludeReplayed { get; set; }

    /// <summary>
    /// Gets or sets the minimum dead letter timestamp (inclusive).
    /// </summary>
    public DateTime? DeadLetteredAfterUtc { get; set; }

    /// <summary>
    /// Gets or sets the maximum dead letter timestamp (inclusive).
    /// </summary>
    public DateTime? DeadLetteredBeforeUtc { get; set; }

    /// <summary>
    /// Creates an empty filter (returns all messages).
    /// </summary>
    public static DeadLetterFilter All => new();

    /// <summary>
    /// Creates a filter for non-replayed messages from a specific source pattern.
    /// </summary>
    /// <param name="sourcePattern">The source pattern to filter by.</param>
    /// <returns>A new filter instance.</returns>
    public static DeadLetterFilter FromSource(string sourcePattern) => new()
    {
        SourcePattern = sourcePattern,
        ExcludeReplayed = true
    };

    /// <summary>
    /// Creates a filter for messages dead-lettered within a time window.
    /// </summary>
    /// <param name="since">The start of the time window.</param>
    /// <returns>A new filter instance.</returns>
    public static DeadLetterFilter Since(DateTime since) => new()
    {
        DeadLetteredAfterUtc = since,
        ExcludeReplayed = true
    };

    /// <summary>
    /// Creates a filter for messages matching a specific correlation ID.
    /// </summary>
    /// <param name="correlationId">The correlation ID to filter by.</param>
    /// <returns>A new filter instance.</returns>
    public static DeadLetterFilter ByCorrelationId(string correlationId) => new()
    {
        CorrelationId = correlationId
    };
}
