using Encina.Security.PII.Abstractions;

namespace Encina.Security.PII;

/// <summary>
/// Configuration options for the Encina PII masking pipeline.
/// </summary>
/// <remarks>
/// <para>
/// These options control the behavior of <c>PIIMaskingPipelineBehavior</c> and the
/// <see cref="IPIIMasker"/> implementation. Register via <c>AddEncinaPII(options => { ... })</c>.
/// </para>
/// <para>
/// By default, PII masking is applied in responses, logs, and audit trails.
/// Each context can be individually disabled.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaPII(options =>
/// {
///     options.MaskInResponses = true;
///     options.MaskInLogs = true;
///     options.MaskInAuditTrails = true;
///     options.DefaultMode = MaskingMode.Partial;
///     options.AddHealthCheck = true;
///
///     // Register custom masking strategy for phone numbers
///     options.AddStrategy&lt;CustomPhoneMaskingStrategy&gt;(PIIType.Phone);
///
///     // Add sensitive field patterns for automatic detection
///     options.AddSensitiveFieldPattern("ssn");
///     options.AddSensitiveFieldPattern("taxId");
///     options.AddSensitiveFieldPattern("passport");
/// });
/// </code>
/// </example>
public sealed class PIIOptions
{
    private readonly Dictionary<PIIType, Type> _customStrategies = [];
    private readonly HashSet<string> _sensitiveFieldPatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "token",
        "secret",
        "apikey",
        "api_key",
        "creditcard",
        "credit_card",
        "ssn",
        "socialSecurity"
    };

    /// <summary>
    /// Gets or sets whether to mask PII in response objects returned from handlers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>true</c> (default), properties decorated with PII attributes on response objects
    /// are masked before returning to the caller.
    /// </para>
    /// <para>
    /// Disable this when the consumer of the response needs access to the original values
    /// (e.g., internal service-to-service communication).
    /// </para>
    /// </remarks>
    public bool MaskInResponses { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to mask PII when objects are serialized for logging.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>true</c> (default), PII-decorated properties are masked in log output.
    /// This is critical for GDPR compliance as log files often lack the same access
    /// controls as primary data stores.
    /// </para>
    /// </remarks>
    public bool MaskInLogs { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to mask PII in audit trail payloads.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>true</c> (default), PII-decorated properties are masked when captured
    /// in audit entries via <c>Encina.Security.Audit</c>.
    /// </para>
    /// <para>
    /// This works in conjunction with <see cref="Audit.AuditOptions.GlobalSensitiveFields"/>
    /// to provide layered PII protection in audit trails.
    /// </para>
    /// </remarks>
    public bool MaskInAuditTrails { get; set; } = true;

    /// <summary>
    /// Gets or sets the default masking mode applied when no explicit mode is specified.
    /// </summary>
    /// <remarks>
    /// Default is <see cref="MaskingMode.Partial"/>. This is used as the fallback
    /// for <see cref="Attributes.PIIAttribute"/> instances that do not override
    /// <see cref="Attributes.PIIAttribute.Mode"/>.
    /// </remarks>
    public MaskingMode DefaultMode { get; set; } = MaskingMode.Partial;

    /// <summary>
    /// Gets or sets whether to register the PII masking health check.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, a health check is registered that verifies PII masking services
    /// are resolvable and operational.
    /// Default is <c>false</c>.
    /// </remarks>
    public bool AddHealthCheck { get; set; }

    /// <summary>
    /// Gets or sets whether to emit OpenTelemetry traces for PII masking operations.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, masking operations emit activities via the
    /// <c>Encina.Security.PII</c> ActivitySource.
    /// Default is <c>false</c>.
    /// </remarks>
    public bool EnableTracing { get; set; }

    /// <summary>
    /// Gets or sets whether to emit OpenTelemetry metrics for PII masking operations.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, counters and histograms are recorded for masking operations
    /// via the <c>Encina.Security.PII</c> Meter.
    /// Default is <c>false</c>.
    /// </remarks>
    public bool EnableMetrics { get; set; }

    /// <summary>
    /// Gets the registered custom masking strategies keyed by <see cref="PIIType"/>.
    /// </summary>
    internal IReadOnlyDictionary<PIIType, Type> CustomStrategies => _customStrategies;

    /// <summary>
    /// Gets the set of field name patterns used for automatic sensitive field detection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Field matching is case-insensitive and supports partial matches
    /// (e.g., "ssn" matches "SSN", "ssnNumber", "customerSsn").
    /// </para>
    /// <para>
    /// This follows the same pattern as <see cref="Audit.AuditOptions.GlobalSensitiveFields"/>
    /// for consistent sensitive field detection across the security pipeline.
    /// </para>
    /// </remarks>
    public IReadOnlySet<string> SensitiveFieldPatterns => _sensitiveFieldPatterns;

    /// <summary>
    /// Registers a custom <see cref="IMaskingStrategy"/> for the specified <see cref="PIIType"/>.
    /// </summary>
    /// <typeparam name="TStrategy">
    /// The masking strategy implementation type. Must implement <see cref="IMaskingStrategy"/>
    /// and have a parameterless constructor or be registered in the DI container.
    /// </typeparam>
    /// <param name="type">The PII type to associate with this strategy.</param>
    /// <returns>This instance for fluent chaining.</returns>
    /// <remarks>
    /// Custom strategies override the default strategy for the specified <see cref="PIIType"/>.
    /// The strategy type is resolved from the DI container at runtime.
    /// </remarks>
    /// <example>
    /// <code>
    /// options.AddStrategy&lt;CustomEmailMasker&gt;(PIIType.Email)
    ///        .AddStrategy&lt;CustomPhoneMasker&gt;(PIIType.Phone);
    /// </code>
    /// </example>
    public PIIOptions AddStrategy<TStrategy>(PIIType type) where TStrategy : class, IMaskingStrategy
    {
        _customStrategies[type] = typeof(TStrategy);
        return this;
    }

    /// <summary>
    /// Adds a field name pattern for automatic sensitive field detection.
    /// </summary>
    /// <param name="pattern">
    /// The field name pattern (case-insensitive). Properties whose names contain this
    /// pattern are automatically treated as sensitive.
    /// </param>
    /// <returns>This instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="pattern"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="pattern"/> is empty or whitespace.</exception>
    /// <example>
    /// <code>
    /// options.AddSensitiveFieldPattern("taxId")
    ///        .AddSensitiveFieldPattern("passport")
    ///        .AddSensitiveFieldPattern("bankAccount");
    /// </code>
    /// </example>
    public PIIOptions AddSensitiveFieldPattern(string pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        _sensitiveFieldPatterns.Add(pattern);
        return this;
    }

    /// <summary>
    /// Removes a field name pattern from the sensitive field detection set.
    /// </summary>
    /// <param name="pattern">The pattern to remove (case-insensitive).</param>
    /// <returns>This instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="pattern"/> is null.</exception>
    public PIIOptions RemoveSensitiveFieldPattern(string pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        _sensitiveFieldPatterns.Remove(pattern);
        return this;
    }
}
