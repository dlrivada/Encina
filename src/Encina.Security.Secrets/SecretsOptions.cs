namespace Encina.Security.Secrets;

/// <summary>
/// Configuration options for the Encina secrets management system.
/// </summary>
/// <remarks>
/// <para>
/// Register via <c>AddEncinaSecrets(options => { ... })</c>.
/// All features are opt-in following Encina's pay-for-what-you-use philosophy.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaSecrets(options =>
/// {
///     options.EnableCaching = true;
///     options.DefaultCacheDuration = TimeSpan.FromMinutes(10);
///     options.ProviderHealthCheck = true;
/// });
/// </code>
/// </example>
public sealed class SecretsOptions
{
    /// <summary>
    /// Gets or sets whether in-memory caching is enabled for secret reads.
    /// </summary>
    /// <remarks>
    /// When <c>true</c> (default), a <see cref="Caching.CachedSecretReaderDecorator"/> wraps
    /// the registered <see cref="Abstractions.ISecretReader"/> to reduce calls to the backing provider.
    /// </remarks>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Gets or sets the default cache time-to-live for cached secrets.
    /// </summary>
    /// <remarks>
    /// Default is 5 minutes. Only applies when <see cref="EnableCaching"/> is <c>true</c>.
    /// </remarks>
    public TimeSpan DefaultCacheDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets whether automatic secret rotation is enabled.
    /// </summary>
    /// <remarks>
    /// Default is <c>false</c>. When enabled, registered <see cref="Abstractions.ISecretRotationHandler"/>
    /// instances will be invoked on rotation events.
    /// </remarks>
    public bool EnableAutoRotation { get; set; }

    /// <summary>
    /// Gets or sets the interval for checking rotation schedules.
    /// </summary>
    /// <remarks>
    /// Only meaningful when <see cref="EnableAutoRotation"/> is <c>true</c>.
    /// </remarks>
    public TimeSpan? RotationCheckInterval { get; set; }

    /// <summary>
    /// Gets or sets an optional prefix for secret key names.
    /// </summary>
    /// <remarks>
    /// When set, secret names are prefixed with this value when querying the provider.
    /// Useful for multi-tenant or environment-scoped secrets (e.g., <c>"production/"</c>).
    /// </remarks>
    public string? KeyPrefix { get; set; }

    /// <summary>
    /// Gets or sets whether to register the secrets health check.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, a health check is registered that verifies the secret reader
    /// is resolvable and operational.
    /// Default is <c>false</c>.
    /// </remarks>
    public bool ProviderHealthCheck { get; set; }

    /// <summary>
    /// Gets or sets whether access auditing is enabled for secret operations.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, secret access operations are recorded via <c>Encina.Security.Audit</c>.
    /// Requires <c>IAuditStore</c> to be registered in DI.
    /// Default is <c>false</c>.
    /// </remarks>
    public bool EnableAccessAuditing { get; set; }

    /// <summary>
    /// Gets or sets whether multi-provider failover is enabled.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, multiple <see cref="Abstractions.ISecretReader"/> implementations
    /// can be registered and will be tried in order until one succeeds.
    /// Default is <c>false</c>.
    /// </remarks>
    public bool EnableFailover { get; set; }

    /// <summary>
    /// Gets or sets whether OpenTelemetry tracing is enabled for secret operations.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, activities are created for secret operations using the
    /// <c>Encina.Security.Secrets</c> ActivitySource. Default is <c>false</c>.
    /// </remarks>
    public bool EnableTracing { get; set; }

    /// <summary>
    /// Gets or sets whether OpenTelemetry metrics are enabled for secret operations.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, counters and histograms are recorded for secret operations
    /// using the <c>Encina.Security.Secrets</c> Meter. Default is <c>false</c>.
    /// </remarks>
    public bool EnableMetrics { get; set; }

    /// <summary>
    /// Gets or sets whether the secret injection pipeline behavior is registered.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, requests with properties decorated with
    /// <see cref="InjectSecretAttribute"/> will have secrets automatically injected
    /// before handler execution. Default is <c>false</c> (opt-in).
    /// </remarks>
    public bool EnableSecretInjection { get; set; }

    /// <summary>
    /// Gets or sets the name of a secret to probe during health checks.
    /// </summary>
    /// <remarks>
    /// When set and <see cref="ProviderHealthCheck"/> is <c>true</c>, the health check
    /// will attempt to read this secret to verify provider connectivity.
    /// If <c>null</c>, the health check only verifies that <c>ISecretReader</c> is resolvable.
    /// Default is <c>null</c>.
    /// </remarks>
    public string? HealthCheckSecretName { get; set; }
}
