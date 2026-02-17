using Encina.Messaging.Health;

namespace Encina.Security.Audit.Health;

/// <summary>
/// Health check that verifies audit store accessibility.
/// </summary>
/// <remarks>
/// <para>
/// Performs a lightweight query against the audit store to verify connectivity.
/// Returns <see cref="HealthCheckResult"/> with <see cref="HealthStatus.Healthy"/> when the store
/// responds successfully, <see cref="HealthStatus.Degraded"/> when the store returns an error,
/// and <see cref="HealthStatus.Unhealthy"/> when the store is unreachable.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register via ASP.NET Core health checks:
/// builder.Services
///     .AddHealthChecks()
///     .AddEncinaAuditHealthCheck();
/// </code>
/// </example>
public sealed class AuditStoreHealthCheck : EncinaHealthCheck
{
    private readonly IAuditStore _auditStore;

    /// <summary>
    /// The default name for this health check.
    /// </summary>
    public const string DefaultName = "encina-audit";

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditStoreHealthCheck"/> class.
    /// </summary>
    /// <param name="auditStore">The audit store to check.</param>
    public AuditStoreHealthCheck(IAuditStore auditStore)
        : base(DefaultName, ["audit", "security", "ready"])
    {
        ArgumentNullException.ThrowIfNull(auditStore);
        _auditStore = auditStore;
    }

    /// <inheritdoc />
    protected override async Task<HealthCheckResult> CheckHealthCoreAsync(CancellationToken cancellationToken)
    {
        var result = await _auditStore.GetByEntityAsync(
            "__health_check__", "__none__", cancellationToken);

        return result.Match(
            Right: _ => HealthCheckResult.Healthy("Audit store is accessible."),
            Left: error => HealthCheckResult.Degraded(
                $"Audit store returned an error: {error.Message}",
                data: new Dictionary<string, object>
                {
                    ["error"] = error.Message
                }));
    }
}
