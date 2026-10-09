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
    /// Gets or sets the tenant to filter by.
    /// </summary>
    /// <remarks>
    /// A store returns every tenant unless this names one (the store contract is explicit).
    /// <see cref="IDeadLetterManager"/> fills it from the ambient tenant when it is null.
    /// </remarks>
    public string? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the source message identifier to filter by.
    /// </summary>
    public string? SourceMessageId { get; set; }

    /// <summary>
    /// Gets or sets the maximum expiry instant (inclusive): matches messages whose
    /// <c>ExpiresAtUtc</c> is set and <c>ExpiresAtUtc &lt;= ExpiresAtOrBeforeUtc</c>.
    /// </summary>
    public DateTime? ExpiresAtOrBeforeUtc { get; set; }

    /// <summary>
    /// Gets a value indicating whether <see cref="IDeadLetterManager"/> skips the ambient tenant default
    /// and works across every tenant (operator tooling).
    /// </summary>
    /// <remarks>
    /// Read by <see cref="IDeadLetterManager"/> only; stores ignore it and filter solely by <see cref="TenantId"/>.
    /// When multi-tenancy is in use and no tenant is resolved, this is the only way to read, replay or delete
    /// without naming <see cref="TenantId"/>; every use is logged (the manager otherwise denies with
    /// <see cref="DeadLetterErrorCodes.TenantRequired"/>).
    /// </remarks>
    public bool AllTenants { get; init; }

    /// <summary>
    /// Returns a copy of this filter with another tenant and replay state; the caller's instance is never changed.
    /// </summary>
    internal DeadLetterFilter CopyWith(string? tenantId, bool? excludeReplayed) => new()
    {
        SourcePattern = SourcePattern,
        RequestType = RequestType,
        ErrorCode = ErrorCode,
        CorrelationId = CorrelationId,
        ExcludeReplayed = excludeReplayed,
        DeadLetteredAfterUtc = DeadLetteredAfterUtc,
        DeadLetteredBeforeUtc = DeadLetteredBeforeUtc,
        TenantId = tenantId,
        SourceMessageId = SourceMessageId,
        ExpiresAtOrBeforeUtc = ExpiresAtOrBeforeUtc,
        AllTenants = AllTenants
    };

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
