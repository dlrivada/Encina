using Encina.Compliance.Retention.Diagnostics;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Compliance.Retention.Health;

/// <summary>
/// Health check that verifies retention infrastructure is properly configured
/// and required services are available.
/// </summary>
/// <remarks>
/// <para>
/// This health check verifies:
/// <list type="bullet">
/// <item><description>The retention options are configured</description></item>
/// <item><description>The retention record store (<see cref="IRetentionRecordStore"/>) is resolvable</description></item>
/// <item><description>The retention policy store (<see cref="IRetentionPolicyStore"/>) is resolvable</description></item>
/// <item><description>The retention enforcer (<see cref="IRetentionEnforcer"/>) is resolvable</description></item>
/// <item><description>The legal hold store (<see cref="ILegalHoldStore"/>) is resolvable (optional, Degraded if missing)</description></item>
/// <item><description>The audit store (<see cref="IRetentionAuditStore"/>) is resolvable when TrackAuditTrail is enabled</description></item>
/// </list>
/// </para>
/// <para>
/// Enable via <see cref="RetentionOptions.AddHealthCheck"/>:
/// <code>
/// services.AddEncinaRetention(options =>
/// {
///     options.AddHealthCheck = true;
/// });
/// </code>
/// </para>
/// </remarks>
public sealed class RetentionHealthCheck : IHealthCheck
{
    /// <summary>
    /// Default health check name.
    /// </summary>
    public const string DefaultName = "encina-retention";

    private static readonly string[] DefaultTags = ["encina", "gdpr", "retention", "compliance", "ready"];

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RetentionHealthCheck> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetentionHealthCheck"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve retention services.</param>
    /// <param name="logger">The logger instance.</param>
    public RetentionHealthCheck(IServiceProvider serviceProvider, ILogger<RetentionHealthCheck> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Gets the default tags for the retention health check.
    /// </summary>
    internal static IEnumerable<string> Tags => DefaultTags;

    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>();
        var warnings = new List<string>();
        var storesVerified = 0;

        using var scope = _serviceProvider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        // 1. Verify options are valid
        var options = scopedProvider.GetService<IOptions<RetentionOptions>>()?.Value;
        if (options is null)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "RetentionOptions are not configured. Call AddEncinaRetention() in DI setup."));
        }

        data["enforcementMode"] = options.EnforcementMode.ToString();
        data["enableAutomaticEnforcement"] = options.EnableAutomaticEnforcement;
        data["enforcementInterval"] = options.EnforcementInterval.ToString();

        // 2. Verify retention record store is resolvable
        var recordStore = scopedProvider.GetService<IRetentionRecordStore>();
        if (recordStore is null)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "IRetentionRecordStore is not registered.",
                data: data));
        }

        data["recordStoreType"] = recordStore.GetType().Name;
        storesVerified++;

        // 3. Verify retention policy store is resolvable
        var policyStore = scopedProvider.GetService<IRetentionPolicyStore>();
        if (policyStore is null)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "IRetentionPolicyStore is not registered.",
                data: data));
        }

        data["policyStoreType"] = policyStore.GetType().Name;
        storesVerified++;

        // 4. Verify retention enforcer is resolvable
        var enforcer = scopedProvider.GetService<IRetentionEnforcer>();
        if (enforcer is null)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "IRetentionEnforcer is not registered.",
                data: data));
        }

        data["enforcerType"] = enforcer.GetType().Name;
        storesVerified++;

        // 5. Verify legal hold store (optional, degraded if missing)
        var legalHoldStore = scopedProvider.GetService<ILegalHoldStore>();
        if (legalHoldStore is null)
        {
            warnings.Add("ILegalHoldStore is not registered. "
                        + "Legal hold functionality will not be available.");
        }
        else
        {
            data["legalHoldStoreType"] = legalHoldStore.GetType().Name;
            storesVerified++;
        }

        // 6. Verify audit store when TrackAuditTrail is enabled (optional, degraded if missing)
        if (options.TrackAuditTrail)
        {
            var auditStore = scopedProvider.GetService<IRetentionAuditStore>();
            if (auditStore is null)
            {
                warnings.Add("IRetentionAuditStore is not registered but TrackAuditTrail is enabled. "
                            + "Retention audit trail will not be recorded.");
            }
            else
            {
                data["auditStoreType"] = auditStore.GetType().Name;
                storesVerified++;
            }
        }

        _logger.RetentionHealthCheckCompleted(
            warnings.Count == 0 ? "Healthy" : "Degraded",
            storesVerified);

        if (warnings.Count > 0)
        {
            data["warnings"] = warnings;
            return Task.FromResult(HealthCheckResult.Degraded(
                $"Retention infrastructure is partially configured: {string.Join("; ", warnings)}",
                data: data));
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            "Retention infrastructure is fully configured.",
            data: data));
    }
}
