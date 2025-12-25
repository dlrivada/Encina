namespace Encina.Messaging.Health;

/// <summary>
/// Configuration options for provider-specific health checks.
/// </summary>
/// <remarks>
/// <para>
/// When a messaging provider (e.g., Dapper, EF Core, MongoDB) is registered,
/// it can automatically register a health check for the underlying infrastructure
/// (database, message broker, cache, etc.).
/// </para>
/// <para>
/// This behavior is <b>opt-out</b>: health checks are registered by default
/// but can be disabled via <see cref="Enabled"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaDapper&lt;PostgreSqlConnection&gt;(config =&gt;
/// {
///     config.UseOutbox = true;
///
///     // Customize health check behavior
///     config.ProviderHealthCheck.Enabled = true;  // Default
///     config.ProviderHealthCheck.Timeout = TimeSpan.FromSeconds(5);
///     config.ProviderHealthCheck.Tags = ["critical", "database"];
/// });
/// </code>
/// </example>
public sealed class ProviderHealthCheckOptions
{
    /// <summary>
    /// Gets or sets whether to automatically register a health check for the provider.
    /// </summary>
    /// <value>Default: true (opt-out design)</value>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the timeout for the health check query.
    /// </summary>
    /// <remarks>
    /// If the health check query takes longer than this timeout, it will be considered unhealthy.
    /// </remarks>
    /// <value>Default: 5 seconds</value>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the name of the health check.
    /// </summary>
    /// <remarks>
    /// If not specified, a default name will be generated based on the provider type
    /// (e.g., "encina-postgresql", "encina-sqlserver").
    /// </remarks>
    /// <value>Default: null (auto-generated)</value>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the tags to apply to the health check.
    /// </summary>
    /// <remarks>
    /// Tags can be used to filter health checks in Kubernetes probes or monitoring systems.
    /// Common tags: "ready", "live", "database", "messaging", "critical".
    /// </remarks>
    /// <value>Default: ["encina", "database", "ready"]</value>
    public IReadOnlyCollection<string> Tags { get; set; } = ["encina", "database", "ready"];

    /// <summary>
    /// Gets or sets the failure status when the health check fails.
    /// </summary>
    /// <remarks>
    /// Determines whether a failure should be reported as <see cref="HealthStatus.Unhealthy"/>
    /// or <see cref="HealthStatus.Degraded"/>.
    /// </remarks>
    /// <value>Default: Unhealthy</value>
    public HealthStatus FailureStatus { get; set; } = HealthStatus.Unhealthy;
}
