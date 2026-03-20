using Encina.Compliance.AIAct.Abstractions;
using Encina.Compliance.AIAct.Diagnostics;
using Encina.Compliance.AIAct.Model;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Compliance.AIAct.Health;

/// <summary>
/// Health check that verifies AI Act compliance infrastructure is properly configured.
/// </summary>
/// <remarks>
/// <para>
/// This health check verifies:
/// <list type="bullet">
/// <item><description><see cref="AIActOptions"/> are configured and valid</description></item>
/// <item><description><see cref="IAISystemRegistry"/> is resolvable and populated</description></item>
/// <item><description><see cref="IAIActClassifier"/> is registered</description></item>
/// <item><description><see cref="IHumanOversightEnforcer"/> is registered</description></item>
/// <item><description><see cref="IAIActComplianceValidator"/> is registered</description></item>
/// <item><description>At least one enforcement feature is enabled (not <see cref="AIActEnforcementMode.Disabled"/>)</description></item>
/// </list>
/// </para>
/// <para>
/// The result maps to:
/// <list type="bullet">
/// <item><description><see cref="HealthStatus.Healthy"/> — All services registered, systems present, enforcement active.</description></item>
/// <item><description><see cref="HealthStatus.Degraded"/> — Services registered but warnings exist (no systems, disabled mode).</description></item>
/// <item><description><see cref="HealthStatus.Unhealthy"/> — Critical services missing or options not configured.</description></item>
/// </list>
/// </para>
/// <para>
/// Enable via <see cref="AIActOptions.AddHealthCheck"/>:
/// <code>
/// services.AddEncinaAIAct(options =>
/// {
///     options.AddHealthCheck = true;
/// });
/// </code>
/// </para>
/// </remarks>
public sealed class AIActHealthCheck : IHealthCheck
{
    /// <summary>
    /// Default health check name.
    /// </summary>
    public const string DefaultName = "encina-aiact";

    private static readonly string[] DefaultTags = ["encina", "aiact", "compliance", "ready"];

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AIActHealthCheck> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIActHealthCheck"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve AI Act services.</param>
    /// <param name="logger">The logger instance.</param>
    public AIActHealthCheck(IServiceProvider serviceProvider, ILogger<AIActHealthCheck> logger)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Gets the default tags for the AI Act health check.
    /// </summary>
    internal static IEnumerable<string> Tags => DefaultTags;

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>();
        var warnings = new List<string>();

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var scopedProvider = scope.ServiceProvider;

            // 1. Verify options are valid
            var options = scopedProvider.GetService<IOptions<AIActOptions>>()?.Value;
            if (options is null)
            {
                return HealthCheckResult.Unhealthy(
                    "AIActOptions are not configured. Call AddEncinaAIAct() in DI setup.",
                    data: data);
            }

            data["enforcementMode"] = options.EnforcementMode.ToString();
            data["autoRegisterFromAttributes"] = options.AutoRegisterFromAttributes;

            // 2. Check if enforcement is disabled (informational warning)
            if (options.EnforcementMode == AIActEnforcementMode.Disabled)
            {
                warnings.Add("Enforcement mode is Disabled — no AI Act compliance checks are active.");
            }

            // 3. Verify registry is resolvable and has systems
            var registry = scopedProvider.GetService<IAISystemRegistry>();
            if (registry is null)
            {
                return HealthCheckResult.Unhealthy(
                    "IAISystemRegistry is not registered.",
                    data: data);
            }

            var systemsResult = await registry.GetAllSystemsAsync(cancellationToken).ConfigureAwait(false);

            systemsResult.Match(
                Right: systems =>
                {
                    data["registeredSystems"] = systems.Count;

                    if (systems.Count == 0)
                    {
                        warnings.Add("No AI systems are registered. "
                            + "Ensure systems are registered via [HighRiskAI] attributes or manual registration.");
                    }
                },
                Left: error =>
                {
                    warnings.Add($"Failed to query AI system registry: {error.Message}");
                });

            // 4. Verify classifier is resolvable
            if (scopedProvider.GetService<IAIActClassifier>() is null)
            {
                warnings.Add("IAIActClassifier is not registered.");
            }
            else
            {
                data["classifierRegistered"] = true;
            }

            // 5. Verify human oversight enforcer is resolvable
            if (scopedProvider.GetService<IHumanOversightEnforcer>() is null)
            {
                warnings.Add("IHumanOversightEnforcer is not registered.");
            }
            else
            {
                data["oversightEnforcerRegistered"] = true;
            }

            // 6. Verify compliance validator is resolvable
            if (scopedProvider.GetService<IAIActComplianceValidator>() is null)
            {
                warnings.Add("IAIActComplianceValidator is not registered.");
            }
            else
            {
                data["complianceValidatorRegistered"] = true;
            }

            // 7. Log and return result
            var systemCount = data.TryGetValue("registeredSystems", out var count) ? (int)count : 0;
            var status = warnings.Count == 0 ? "Healthy" : "Degraded";
            _logger.HealthCheckCompleted(status, systemCount);

            if (warnings.Count > 0)
            {
                data["warnings"] = warnings;
                return HealthCheckResult.Degraded(
                    $"AI Act compliance is partially configured: {string.Join("; ", warnings)}",
                    data: data);
            }

            return HealthCheckResult.Healthy(
                "AI Act compliance infrastructure is fully configured.",
                data: data);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.HealthCheckCompleted("Unhealthy", 0);

            return HealthCheckResult.Unhealthy(
                $"AI Act compliance health check exception: {ex.Message}",
                ex,
                data);
        }
    }
}
