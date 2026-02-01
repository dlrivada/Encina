namespace Encina.Security.Audit;

/// <summary>
/// Configuration options for the Encina audit trail system.
/// </summary>
/// <remarks>
/// <para>
/// These options control global audit behavior across all requests.
/// Individual requests can override these settings using the <see cref="AuditableAttribute"/>.
/// </para>
/// <para>
/// <b>Compliance Considerations:</b>
/// <list type="bullet">
/// <item><see cref="RetentionDays"/> defaults to 2555 (7 years) for SOX compliance</item>
/// <item>HIPAA typically requires 6 years of audit records</item>
/// <item>GDPR requires retention periods appropriate to the purpose</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaAudit(options =>
/// {
///     options.AuditAllCommands = true;
///     options.AuditAllQueries = false;
///     options.IncludePayloadHash = true;
///     options.RetentionDays = 2555; // 7 years for SOX
///
///     // Exclude internal infrastructure commands
///     options.ExcludeType&lt;RefreshCacheCommand&gt;();
///     options.ExcludeType&lt;HealthCheckQuery&gt;();
///
///     // Include specific sensitive queries
///     options.IncludeQueryType&lt;GetUserPersonalDataQuery&gt;();
/// });
/// </code>
/// </example>
public sealed class AuditOptions
{
    private readonly HashSet<Type> _excludedTypes = [];
    private readonly HashSet<Type> _includedQueryTypes = [];

    /// <summary>
    /// Gets or sets whether to audit all commands by default.
    /// </summary>
    /// <remarks>
    /// Default is <c>true</c>. Commands can opt-out using <c>[Auditable(Skip = true)]</c>
    /// or by adding them to the excluded types collection.
    /// </remarks>
    public bool AuditAllCommands { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to audit all queries by default.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Default is <c>false</c>. Queries are typically high-volume and don't mutate state,
    /// so auditing all queries can be expensive.
    /// </para>
    /// <para>
    /// Individual queries can be included using <c>[Auditable]</c> attribute
    /// or by adding them to the included query types collection.
    /// </para>
    /// </remarks>
    public bool AuditAllQueries { get; set; }

    /// <summary>
    /// Gets or sets whether to compute and include SHA-256 hash of request payloads.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Default is <c>true</c>. The payload hash provides tamper detection without storing
    /// sensitive data. The hash is computed from the JSON-serialized request after PII masking.
    /// </para>
    /// <para>
    /// Set to <c>false</c> to disable payload hashing globally for performance reasons.
    /// </para>
    /// </remarks>
    public bool IncludePayloadHash { get; set; } = true;

    /// <summary>
    /// Gets or sets the retention period for audit entries in days.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Default is 2555 days (approximately 7 years) for SOX compliance.
    /// </para>
    /// <para>
    /// This value is informational and should be enforced by the <see cref="IAuditStore"/> implementation
    /// or by a separate cleanup process.
    /// </para>
    /// <para>
    /// <b>Common compliance requirements:</b>
    /// <list type="bullet">
    /// <item>SOX (Sarbanes-Oxley): 7 years</item>
    /// <item>HIPAA: 6 years</item>
    /// <item>PCI-DSS: 1 year</item>
    /// <item>GDPR: As long as necessary for the stated purpose</item>
    /// </list>
    /// </para>
    /// </remarks>
    public int RetentionDays { get; set; } = 2555; // ~7 years for SOX compliance

    /// <summary>
    /// Gets the collection of request types that are excluded from auditing.
    /// </summary>
    public IReadOnlySet<Type> ExcludedTypes => _excludedTypes;

    /// <summary>
    /// Gets the collection of query types that are explicitly included for auditing.
    /// </summary>
    /// <remarks>
    /// These queries will be audited even when <see cref="AuditAllQueries"/> is <c>false</c>.
    /// </remarks>
    public IReadOnlySet<Type> IncludedQueryTypes => _includedQueryTypes;

    /// <summary>
    /// Excludes a request type from auditing.
    /// </summary>
    /// <typeparam name="TRequest">The request type to exclude.</typeparam>
    /// <returns>This instance for fluent chaining.</returns>
    /// <remarks>
    /// Use this for internal infrastructure commands that don't need audit trails,
    /// such as cache refresh, health checks, or internal synchronization commands.
    /// </remarks>
    /// <example>
    /// <code>
    /// options.ExcludeType&lt;RefreshCacheCommand&gt;()
    ///        .ExcludeType&lt;HealthCheckQuery&gt;();
    /// </code>
    /// </example>
    public AuditOptions ExcludeType<TRequest>()
    {
        _excludedTypes.Add(typeof(TRequest));
        return this;
    }

    /// <summary>
    /// Excludes a request type from auditing.
    /// </summary>
    /// <param name="requestType">The request type to exclude.</param>
    /// <returns>This instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="requestType"/> is null.</exception>
    public AuditOptions ExcludeType(Type requestType)
    {
        ArgumentNullException.ThrowIfNull(requestType);
        _excludedTypes.Add(requestType);
        return this;
    }

    /// <summary>
    /// Includes a query type for auditing even when <see cref="AuditAllQueries"/> is <c>false</c>.
    /// </summary>
    /// <typeparam name="TQuery">The query type to include.</typeparam>
    /// <returns>This instance for fluent chaining.</returns>
    /// <remarks>
    /// Use this for sensitive queries that access personal data or financial information
    /// where an audit trail is required for compliance.
    /// </remarks>
    /// <example>
    /// <code>
    /// options.IncludeQueryType&lt;GetUserPersonalDataQuery&gt;()
    ///        .IncludeQueryType&lt;GetAccountBalanceQuery&gt;();
    /// </code>
    /// </example>
    public AuditOptions IncludeQueryType<TQuery>()
    {
        _includedQueryTypes.Add(typeof(TQuery));
        return this;
    }

    /// <summary>
    /// Includes a query type for auditing even when <see cref="AuditAllQueries"/> is <c>false</c>.
    /// </summary>
    /// <param name="queryType">The query type to include.</param>
    /// <returns>This instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="queryType"/> is null.</exception>
    public AuditOptions IncludeQueryType(Type queryType)
    {
        ArgumentNullException.ThrowIfNull(queryType);
        _includedQueryTypes.Add(queryType);
        return this;
    }

    /// <summary>
    /// Determines whether a request type is explicitly excluded from auditing.
    /// </summary>
    /// <param name="requestType">The request type to check.</param>
    /// <returns><c>true</c> if the type is excluded; otherwise, <c>false</c>.</returns>
    public bool IsExcluded(Type requestType)
    {
        return _excludedTypes.Contains(requestType);
    }

    /// <summary>
    /// Determines whether a query type is explicitly included for auditing.
    /// </summary>
    /// <param name="queryType">The query type to check.</param>
    /// <returns><c>true</c> if the type is explicitly included; otherwise, <c>false</c>.</returns>
    public bool IsQueryIncluded(Type queryType)
    {
        return _includedQueryTypes.Contains(queryType);
    }
}
