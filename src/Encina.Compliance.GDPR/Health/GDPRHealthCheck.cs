using Encina.Compliance.GDPR.Diagnostics;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Compliance.GDPR.Health;

/// <summary>
/// Health check that verifies GDPR compliance infrastructure is properly configured.
/// </summary>
/// <remarks>
/// <para>
/// This health check verifies:
/// <list type="bullet">
/// <item><description>The processing activity registry is resolvable and populated</description></item>
/// <item><description>Controller information is configured (Article 30(1)(a))</description></item>
/// <item><description>DPO is configured when required by the organization</description></item>
/// <item><description>The compliance validator is registered</description></item>
/// </list>
/// </para>
/// <para>
/// Enable via <see cref="GDPROptions.AddHealthCheck"/>:
/// <code>
/// services.AddEncinaGDPR(options =>
/// {
///     options.AddHealthCheck = true;
///     options.ControllerName = "Acme Corp";
///     options.ControllerEmail = "privacy@acme.com";
/// });
/// </code>
/// </para>
/// </remarks>
public sealed class GDPRHealthCheck : IHealthCheck
{
    /// <summary>
    /// Default health check name.
    /// </summary>
    public const string DefaultName = "encina-gdpr";

    private static readonly string[] DefaultTags = ["encina", "gdpr", "compliance", "ready"];

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<GDPRHealthCheck> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GDPRHealthCheck"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve GDPR services.</param>
    /// <param name="logger">The logger instance.</param>
    public GDPRHealthCheck(IServiceProvider serviceProvider, ILogger<GDPRHealthCheck> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Gets the default tags for the GDPR health check.
    /// </summary>
    internal static IEnumerable<string> Tags => DefaultTags;

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>();
        var warnings = new List<string>();

        using var scope = _serviceProvider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        // 1. Verify options are valid
        var options = scopedProvider.GetService<IOptions<GDPROptions>>()?.Value;
        if (options is null)
        {
            return HealthCheckResult.Unhealthy("GDPROptions are not configured. Call AddEncinaGDPR() in DI setup.");
        }

        data["enforcementMode"] = options.EnforcementMode.ToString();

        // 2. Check controller info
        if (string.IsNullOrWhiteSpace(options.ControllerName))
        {
            warnings.Add("ControllerName is not set (required by Article 30(1)(a)).");
        }
        else
        {
            data["controllerName"] = options.ControllerName;
        }

        if (string.IsNullOrWhiteSpace(options.ControllerEmail))
        {
            warnings.Add("ControllerEmail is not set (required by Article 30(1)(a)).");
        }

        // 3. Check DPO (informational, not required for all organizations)
        if (options.DataProtectionOfficer is not null)
        {
            data["dpoConfigured"] = true;
        }
        else
        {
            data["dpoConfigured"] = false;
        }

        // 4. Verify registry is resolvable and has activities
        var registry = scopedProvider.GetService<IProcessingActivityRegistry>();
        if (registry is null)
        {
            return HealthCheckResult.Unhealthy(
                "IProcessingActivityRegistry is not registered.",
                data: data);
        }

        var activitiesResult = await registry.GetAllActivitiesAsync(cancellationToken);

        activitiesResult.Match(
            Right: activities =>
            {
                data["registeredActivities"] = activities.Count;

                if (activities.Count == 0)
                {
                    warnings.Add("No processing activities are registered in the RoPA. " +
                                 "Ensure activities are registered via attributes or manual registration.");
                }
            },
            Left: error =>
            {
                warnings.Add($"Failed to query processing activity registry: {error.Message}");
            });

        // 5. Verify validator is resolvable
        if (scopedProvider.GetService<IGDPRComplianceValidator>() is null)
        {
            warnings.Add("IGDPRComplianceValidator is not registered.");
        }

        _logger.HealthCheckCompleted(
            warnings.Count == 0 ? "Healthy" : "Degraded",
            data.TryGetValue("registeredActivities", out var count) ? (int)count : 0);

        if (warnings.Count > 0)
        {
            data["warnings"] = warnings;
            return HealthCheckResult.Degraded(
                $"GDPR compliance is partially configured: {string.Join("; ", warnings)}",
                data: data);
        }

        return HealthCheckResult.Healthy(
            "GDPR compliance infrastructure is fully configured.",
            data: data);
    }
}
