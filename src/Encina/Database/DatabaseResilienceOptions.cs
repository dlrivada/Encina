namespace Encina.Database;

/// <summary>
/// Configuration options for database resilience features including circuit breaking,
/// connection pool monitoring, health checks, and connection warm-up.
/// </summary>
/// <remarks>
/// <para>
/// All resilience features are <b>opt-in</b> and disabled by default, following Encina's
/// pay-for-what-you-use philosophy.
/// </para>
/// <para>
/// These options are consumed by each of the 13 database provider implementations
/// (ADO.NET ×4, Dapper ×4, EF Core ×4, MongoDB ×1) and should be configured
/// via the provider-specific <c>AddEncina*</c> extension methods.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaEntityFrameworkCore&lt;AppDbContext&gt;(config =>
/// {
///     config.UseResilience(options =>
///     {
///         options.EnablePoolMonitoring = true;
///         options.EnableCircuitBreaker = true;
///         options.CircuitBreaker.FailureThreshold = 0.3;
///         options.CircuitBreaker.BreakDuration = TimeSpan.FromMinutes(1);
///         options.WarmUpConnections = 5;
///         options.HealthCheckInterval = TimeSpan.FromSeconds(30);
///     });
/// });
/// </code>
/// </example>
public sealed class DatabaseResilienceOptions
{
    /// <summary>
    /// Gets or sets whether connection pool monitoring is enabled.
    /// </summary>
    /// <remarks>
    /// When enabled, the provider registers an <see cref="IDatabaseHealthMonitor"/> implementation
    /// that exposes <see cref="ConnectionPoolStats"/> via <see cref="IDatabaseHealthMonitor.GetPoolStatistics"/>.
    /// </remarks>
    /// <value>Default: false (opt-in).</value>
    public bool EnablePoolMonitoring { get; set; }

    /// <summary>
    /// Gets or sets whether the database circuit breaker is enabled.
    /// </summary>
    /// <remarks>
    /// When enabled, repeated database failures will trip the circuit breaker,
    /// causing subsequent operations to fail fast without attempting a connection.
    /// Configure the circuit breaker behavior via <see cref="CircuitBreaker"/>.
    /// </remarks>
    /// <value>Default: false (opt-in).</value>
    public bool EnableCircuitBreaker { get; set; }

    /// <summary>
    /// Gets the circuit breaker configuration options.
    /// </summary>
    /// <remarks>
    /// These options are only applied when <see cref="EnableCircuitBreaker"/> is <c>true</c>.
    /// </remarks>
    public DatabaseCircuitBreakerOptions CircuitBreaker { get; } = new();

    /// <summary>
    /// Gets or sets the number of connections to warm up on application startup.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Warming up connections during startup reduces the latency of the first requests
    /// by ensuring connections are already established and pooled.
    /// </para>
    /// <para>
    /// A value of 0 disables warm-up. The value should not exceed the provider's
    /// maximum pool size.
    /// </para>
    /// </remarks>
    /// <value>Default: 0 (disabled).</value>
    public int WarmUpConnections { get; set; }

    /// <summary>
    /// Gets or sets the interval between periodic health checks.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When set to a positive value, the provider will periodically execute a
    /// lightweight health check query to proactively detect connectivity issues.
    /// </para>
    /// <para>
    /// A value of <see cref="TimeSpan.Zero"/> or negative disables periodic health checks.
    /// Health checks can still be triggered on-demand via
    /// <see cref="IDatabaseHealthMonitor.CheckHealthAsync"/>.
    /// </para>
    /// </remarks>
    /// <value>Default: <see cref="TimeSpan.Zero"/> (disabled).</value>
    public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.Zero;
}
