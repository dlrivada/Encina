namespace Encina.Security.Audit;

/// <summary>
/// Configuration options for the Encina read audit trail system.
/// </summary>
/// <remarks>
/// <para>
/// These options control which entities are audited for read access and how
/// the audit trail is managed. Read auditing operates at the repository level
/// via the <c>AuditedRepository</c> decorator, separate from the CQRS-level
/// auditing controlled by <see cref="AuditOptions"/>.
/// </para>
/// <para>
/// <b>Compliance Considerations:</b>
/// <list type="bullet">
/// <item><see cref="RetentionDays"/> defaults to 365 (1 year) — adjust per regulatory requirements</item>
/// <item><see cref="RequirePurpose"/> supports GDPR Art. 15 by enforcing access purpose documentation</item>
/// <item>Per-entity sampling rates allow high-traffic scenarios without overwhelming storage</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaReadAuditing(options =>
/// {
///     // Register entity types for read auditing
///     options.AuditReadsFor&lt;Patient&gt;();
///     options.AuditReadsFor&lt;FinancialRecord&gt;();
///
///     // High-traffic entity with 10% sampling
///     options.AuditReadsFor&lt;AuditLog&gt;(samplingRate: 0.1);
///
///     // Enforce GDPR Art. 15 purpose declaration
///     options.RequirePurpose = true;
///
///     // Skip system/background access
///     options.ExcludeSystemAccess = true;
///
///     // Retention and purge settings
///     options.RetentionDays = 2555; // 7 years for SOX
///     options.EnableAutoPurge = true;
///     options.PurgeIntervalHours = 24;
/// });
/// </code>
/// </example>
public sealed class ReadAuditOptions
{
    private readonly Dictionary<Type, double> _auditedEntityTypes = [];

    /// <summary>
    /// Gets or sets whether read auditing is globally enabled.
    /// </summary>
    /// <remarks>
    /// Default is <c>true</c>. When <c>false</c>, no read operations are audited
    /// regardless of individual entity type registrations.
    /// </remarks>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to exclude system-initiated or background access from auditing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Default is <c>false</c>. When <c>true</c>, read operations without a
    /// <c>UserId</c> in the request context are not audited.
    /// </para>
    /// <para>
    /// Useful for reducing noise from background jobs, health checks, and
    /// system-level data access that is not user-initiated.
    /// </para>
    /// </remarks>
    public bool ExcludeSystemAccess { get; set; }

    /// <summary>
    /// Gets or sets whether a declared access purpose is required for audited reads.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Default is <c>false</c>. When <c>true</c>, a warning is logged for read operations
    /// that do not provide an access purpose via <c>IReadAuditContext</c>.
    /// </para>
    /// <para>
    /// Supports GDPR Art. 15 compliance — when a data subject exercises their right of access,
    /// the controller must provide information about the purposes of processing.
    /// </para>
    /// </remarks>
    public bool RequirePurpose { get; set; }

    /// <summary>
    /// Gets or sets the number of entries to batch before writing to the store.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Default is 1 (immediate write). Higher values reduce write frequency
    /// at the cost of potential data loss if the process terminates before flushing.
    /// </para>
    /// <para>
    /// Only applies to implementations that support batching. The <see cref="InMemoryReadAuditStore"/>
    /// always writes immediately regardless of this setting.
    /// </para>
    /// </remarks>
    public int BatchSize { get; set; } = 1;

    /// <summary>
    /// Gets or sets the retention period for read audit entries in days.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Default is 365 days (1 year). Shorter than CUD audit retention because
    /// read audit volumes are typically much higher.
    /// </para>
    /// <para>
    /// <b>Common compliance requirements:</b>
    /// <list type="bullet">
    /// <item>SOX: 7 years (2555 days)</item>
    /// <item>HIPAA: 6 years (2190 days)</item>
    /// <item>PCI-DSS: 1 year (365 days)</item>
    /// <item>GDPR: As long as necessary for the stated purpose</item>
    /// </list>
    /// </para>
    /// </remarks>
    public int RetentionDays { get; set; } = 365;

    /// <summary>
    /// Gets or sets whether to enable automatic purging of old read audit entries.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Default is <c>false</c>. When enabled, a background service will periodically
    /// purge entries older than <see cref="RetentionDays"/>.
    /// </para>
    /// </remarks>
    public bool EnableAutoPurge { get; set; }

    /// <summary>
    /// Gets or sets the interval in hours between automatic purge operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Default is 24 hours (once daily). Only applies when <see cref="EnableAutoPurge"/>
    /// is <c>true</c>.
    /// </para>
    /// </remarks>
    public int PurgeIntervalHours { get; set; } = 24;

    /// <summary>
    /// Registers an entity type for read auditing with full (100%) sampling.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to audit. Should implement <c>IReadAuditable</c>.</typeparam>
    /// <returns>This instance for fluent chaining.</returns>
    /// <example>
    /// <code>
    /// options.AuditReadsFor&lt;Patient&gt;()
    ///        .AuditReadsFor&lt;FinancialRecord&gt;();
    /// </code>
    /// </example>
    public ReadAuditOptions AuditReadsFor<TEntity>()
    {
        _auditedEntityTypes[typeof(TEntity)] = 1.0;
        return this;
    }

    /// <summary>
    /// Registers an entity type for read auditing with a specific sampling rate.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to audit. Should implement <c>IReadAuditable</c>.</typeparam>
    /// <param name="samplingRate">
    /// The probability (0.0 to 1.0) of auditing each read operation.
    /// <c>1.0</c> means every read is audited; <c>0.1</c> means approximately 10% of reads are audited.
    /// </param>
    /// <returns>This instance for fluent chaining.</returns>
    /// <remarks>
    /// <para>
    /// Sampling is useful for high-traffic entities where auditing every read would
    /// overwhelm storage. The trade-off is reduced visibility in exchange for lower overhead.
    /// </para>
    /// <para>
    /// Values outside the 0.0-1.0 range are clamped: values &lt; 0.0 become 0.0,
    /// values &gt; 1.0 become 1.0.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Audit only 10% of reads for high-traffic entity
    /// options.AuditReadsFor&lt;AuditLog&gt;(samplingRate: 0.1);
    /// </code>
    /// </example>
    public ReadAuditOptions AuditReadsFor<TEntity>(double samplingRate)
    {
        _auditedEntityTypes[typeof(TEntity)] = Math.Clamp(samplingRate, 0.0, 1.0);
        return this;
    }

    /// <summary>
    /// Determines whether a given entity type is registered for read auditing.
    /// </summary>
    /// <param name="entityType">The entity type to check.</param>
    /// <returns><c>true</c> if the type is registered for read auditing; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// Returns <c>false</c> if <see cref="Enabled"/> is <c>false</c>, regardless of registrations.
    /// </remarks>
    public bool IsAuditable(Type entityType)
    {
        ArgumentNullException.ThrowIfNull(entityType);
        return Enabled && _auditedEntityTypes.ContainsKey(entityType);
    }

    /// <summary>
    /// Gets the sampling rate for a given entity type.
    /// </summary>
    /// <param name="entityType">The entity type to query.</param>
    /// <returns>
    /// The sampling rate (0.0 to 1.0) if the type is registered; otherwise, <c>0.0</c>.
    /// </returns>
    /// <remarks>
    /// Returns <c>0.0</c> if the entity type is not registered or if <see cref="Enabled"/> is <c>false</c>.
    /// </remarks>
    public double GetSamplingRate(Type entityType)
    {
        ArgumentNullException.ThrowIfNull(entityType);

        if (!Enabled)
        {
            return 0.0;
        }

        return _auditedEntityTypes.TryGetValue(entityType, out var rate) ? rate : 0.0;
    }

    /// <summary>
    /// Gets the set of entity types registered for read auditing.
    /// </summary>
    /// <remarks>
    /// Returns the types regardless of the <see cref="Enabled"/> flag.
    /// Use <see cref="IsAuditable"/> to check effective auditability.
    /// </remarks>
    public IReadOnlyDictionary<Type, double> AuditedEntityTypes => _auditedEntityTypes;
}
