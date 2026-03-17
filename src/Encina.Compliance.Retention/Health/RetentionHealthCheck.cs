using Encina.Compliance.Retention.Abstractions;
using Encina.Compliance.Retention.Diagnostics;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Compliance.Retention.Health;

/// <summary>
/// Health check that verifies retention infrastructure is properly configured
/// and required event-sourced services are available.
/// </summary>
/// <remarks>
/// <para>
/// This health check verifies:
/// <list type="bullet">
/// <item><description>The retention options are configured</description></item>
/// <item><description>The <see cref="IRetentionRecordService"/> is resolvable</description></item>
/// <item><description>The <see cref="IRetentionPolicyService"/> is resolvable</description></item>
/// <item><description>The <see cref="ILegalHoldService"/> is resolvable (optional, Degraded if missing)</description></item>
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
        var servicesVerified = 0;

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

        // 2. Verify retention record service is resolvable
        var recordService = scopedProvider.GetService<IRetentionRecordService>();
        if (recordService is null)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "IRetentionRecordService is not registered.",
                data: data));
        }

        data["recordServiceType"] = recordService.GetType().Name;
        servicesVerified++;

        // 3. Verify retention policy service is resolvable
        var policyService = scopedProvider.GetService<IRetentionPolicyService>();
        if (policyService is null)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "IRetentionPolicyService is not registered.",
                data: data));
        }

        data["policyServiceType"] = policyService.GetType().Name;
        servicesVerified++;

        // 4. Verify legal hold service (optional, degraded if missing)
        var legalHoldService = scopedProvider.GetService<ILegalHoldService>();
        if (legalHoldService is null)
        {
            warnings.Add("ILegalHoldService is not registered. "
                        + "Legal hold functionality will not be available.");
        }
        else
        {
            data["legalHoldServiceType"] = legalHoldService.GetType().Name;
            servicesVerified++;
        }

        _logger.RetentionHealthCheckCompleted(
            warnings.Count == 0 ? "Healthy" : "Degraded",
            servicesVerified);

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
