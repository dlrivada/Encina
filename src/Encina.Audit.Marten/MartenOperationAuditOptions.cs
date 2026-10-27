namespace Encina.Audit.Marten;

/// <summary>
/// Configuration options for the Marten event-sourced audit store.
/// </summary>
/// <remarks>
/// <para>
/// These options control the behavior of <c>MartenOperationAuditStore</c> and
/// <c>MartenReadAuditStore</c>, including temporal key management, encryption scope,
/// retention policies, and health monitoring.
/// </para>
/// <para>
/// Configure via <c>AddEncinaAuditMarten</c>:
/// <code>
/// services.AddEncinaAuditMarten(options =>
/// {
///     options.TemporalGranularity = TemporalKeyGranularity.Monthly;
///     options.EncryptionScope = AuditEncryptionScope.PiiFieldsOnly;
///     options.RetentionPeriod = TimeSpan.FromDays(2555); // 7 years (SOX)
///     options.EnableAutoPurge = true;
///     options.AddHealthCheck = true;
/// });
/// </code>
/// </para>
/// <para>
/// <b>Compliance mapping:</b>
/// <list type="bullet">
/// <item><b>SOX §404</b> — Set <see cref="RetentionPeriod"/> to 7 years (2555 days)</item>
/// <item><b>HIPAA</b> — Set <see cref="RetentionPeriod"/> to 6 years (2190 days)</item>
/// <item><b>GDPR Art. 5(1)(e)</b> — Enable <see cref="EnableAutoPurge"/> for data minimization</item>
/// <item><b>NIS2 Art. 10</b> — Use <see cref="AuditEncryptionScope.PiiFieldsOnly"/> for log integrity</item>
/// </list>
/// </para>
/// </remarks>
public sealed class MartenOperationAuditOptions
{
    /// <summary>
    /// Default placeholder value for PII fields in shredded audit entries.
    /// </summary>
    public const string DefaultShreddedPlaceholder = "[SHREDDED]";

    /// <summary>
    /// Default placeholder value for PII fields of audit entries whose keys were destroyed by a retention execution.
    /// </summary>
    public const string DefaultExpiredPlaceholder = "[EXPIRED]";

    /// <summary>
    /// Default retention period in days (~7 years for SOX compliance).
    /// </summary>
    public const int DefaultRetentionDays = 2555;

    /// <summary>
    /// Default auto-purge interval in hours (once daily).
    /// </summary>
    public const int DefaultPurgeIntervalHours = 24;

    /// <summary>
    /// Gets or sets the time-partitioning granularity for temporal encryption keys.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Determines how audit events are grouped into temporal key partitions.
    /// Finer granularity (monthly) provides more precise shredding boundaries
    /// but requires managing more keys over the retention period.
    /// </para>
    /// <para>
    /// Example: with <see cref="TemporalKeyGranularity.Monthly"/> and 7-year retention,
    /// the system manages ~84 temporal keys. With <see cref="TemporalKeyGranularity.Yearly"/>,
    /// only ~7 keys.
    /// </para>
    /// </remarks>
    /// <value>Defaults to <see cref="TemporalKeyGranularity.Monthly"/>.</value>
    public TemporalKeyGranularity TemporalGranularity { get; set; } = TemporalKeyGranularity.Monthly;

    /// <summary>
    /// Gets or sets which fields of audit entries are encrypted with temporal keys.
    /// </summary>
    /// <remarks>
    /// <para>
    /// With <see cref="AuditEncryptionScope.PiiFieldsOnly"/>, structural fields
    /// (Action, EntityType, Outcome, timestamps) remain queryable after crypto-shredding.
    /// With <see cref="AuditEncryptionScope.AllFields"/>, nothing is queryable after shredding.
    /// </para>
    /// </remarks>
    /// <value>Defaults to <see cref="AuditEncryptionScope.PiiFieldsOnly"/>.</value>
    public AuditEncryptionScope EncryptionScope { get; set; } = AuditEncryptionScope.PiiFieldsOnly;

    /// <summary>
    /// Gets or sets the retention period for audit entries.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Entries older than this period are eligible for crypto-shredding via
    /// <c>PurgeEntriesAsync</c>, which destroys temporal encryption keys
    /// rather than deleting events.
    /// </para>
    /// <para>
    /// Common compliance requirements:
    /// <list type="bullet">
    /// <item><b>SOX</b>: 7 years (2555 days)</item>
    /// <item><b>HIPAA</b>: 6 years (2190 days)</item>
    /// <item><b>PCI-DSS</b>: 1 year (365 days)</item>
    /// <item><b>GDPR</b>: as long as necessary for the stated purpose</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <value>Defaults to 2555 days (~7 years for SOX compliance).</value>
    public TimeSpan RetentionPeriod { get; set; } = TimeSpan.FromDays(DefaultRetentionDays);

    /// <summary>
    /// Gets or sets the identifier of the approved retention policy that the audit purges name in the
    /// <see cref="Crypto.RetentionReference"/> they record for every key they destroy.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>null</c>, the policy id is derived from <see cref="RetentionPeriod"/> as
    /// <c>audit-retention:&lt;ISO 8601 duration&gt;</c> (for example <c>audit-retention:P2555D</c>). Operators who run
    /// purges manually (<c>PurgeEntriesAsync</c>) should set this to the identifier of the retention policy their
    /// organisation approved, so the destroyed-key marker (GDPR Art. 5(2) evidence) names that policy. Both the
    /// automatic and the manual purge use it.
    /// </para>
    /// <para>
    /// When set, it must not be empty or whitespace; <c>AddEncinaAuditMarten</c> rejects it otherwise.
    /// </para>
    /// </remarks>
    /// <value>Defaults to <c>null</c> (derived from <see cref="RetentionPeriod"/>).</value>
    public string? RetentionPolicyId { get; set; }

    /// <summary>
    /// Gets or sets whether the audit projections may start when Marten's store-wide projection error flags skip
    /// failing events. This is an explicit opt-out of a fail-closed check.
    /// </summary>
    /// <remarks>
    /// <para>
    /// At startup the audit projections require <c>StoreOptions.Projections.Errors</c> and
    /// <c>StoreOptions.Projections.RebuildErrors</c> to have <c>SkipApplyErrors</c> and <c>SkipSerializationErrors</c>
    /// set to <c>false</c> by the application, so an audit event that cannot be projected pauses the shard instead
    /// of vanishing from the read model. Marten has no per-projection policy: those flags are store-wide and also
    /// govern the application's own projections. Marten skips by default, so the application must set them.
    /// </para>
    /// <para>
    /// Setting this to <c>true</c> lets the host start with skipping enabled and logs a warning at startup. An
    /// audit event skipped that way is lost from the read model without a stop; use it only when the application
    /// needs skipping for its other projections and accepts that risk.
    /// </para>
    /// </remarks>
    /// <value>Defaults to <c>false</c> (fail closed).</value>
    public bool AllowSkippedProjectionErrors { get; set; }

    /// <summary>
    /// Gets or sets whether to enable automatic crypto-shredding of expired audit entries.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, a background service periodically destroys temporal encryption keys
    /// for periods older than <see cref="RetentionPeriod"/>. The events themselves are
    /// preserved (immutability), but their PII fields become permanently unreadable.
    /// </para>
    /// <para>
    /// The purge interval is controlled by <see cref="PurgeIntervalHours"/>.
    /// </para>
    /// </remarks>
    /// <value>Defaults to <c>false</c>.</value>
    public bool EnableAutoPurge { get; set; }

    /// <summary>
    /// Gets or sets the interval in hours between automatic purge (crypto-shredding) operations.
    /// </summary>
    /// <remarks>
    /// Only applies when <see cref="EnableAutoPurge"/> is <c>true</c>.
    /// Consider running purges during off-peak hours by adjusting this interval.
    /// </remarks>
    /// <value>Defaults to 24 hours (once daily).</value>
    public int PurgeIntervalHours { get; set; } = DefaultPurgeIntervalHours;

    /// <summary>
    /// Gets or sets the placeholder value substituted for PII fields in shredded audit entries.
    /// </summary>
    /// <remarks>
    /// When a temporal encryption key has been destroyed and an encrypted PII field is
    /// encountered during projection or query, this placeholder replaces the unrecoverable
    /// field value.
    /// </remarks>
    /// <value>Defaults to <c>"[SHREDDED]"</c>.</value>
    public string ShreddedPlaceholder { get; set; } = DefaultShreddedPlaceholder;

    /// <summary>
    /// Gets or sets the placeholder value substituted for PII fields of audit entries whose temporal keys were
    /// destroyed by a retention execution.
    /// </summary>
    /// <remarks>
    /// Distinct from <see cref="ShreddedPlaceholder"/>, which is reserved for a destruction that answers a recorded
    /// erasure request. A retention expiry is an expected, lawful event: the projection shows this placeholder and
    /// keeps running (issue #2770).
    /// </remarks>
    /// <value>Defaults to <c>"[EXPIRED]"</c>.</value>
    public string ExpiredPlaceholder { get; set; } = DefaultExpiredPlaceholder;

    /// <summary>
    /// Gets or sets whether to register a health check for the Marten audit store.
    /// </summary>
    /// <remarks>
    /// When enabled, registers a health check that verifies Marten/PostgreSQL connectivity,
    /// temporal key provider availability, and async projection high-water mark (lag detection).
    /// </remarks>
    /// <value>Defaults to <c>false</c>.</value>
    public bool AddHealthCheck { get; set; }
}
