using Encina.Compliance.PrivacyByDesign.Diagnostics;
using Encina.Compliance.PrivacyByDesign.Model;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Compliance.PrivacyByDesign.Health;

/// <summary>
/// Health check that verifies Privacy by Design infrastructure is properly configured.
/// </summary>
/// <remarks>
/// <para>
/// This health check verifies:
/// <list type="bullet">
/// <item><description>The Privacy by Design options are configured</description></item>
/// <item><description>The <see cref="IPrivacyByDesignValidator"/> is resolvable</description></item>
/// <item><description>The <see cref="IPurposeRegistry"/> is resolvable</description></item>
/// <item><description>The <see cref="IDataMinimizationAnalyzer"/> is resolvable</description></item>
/// </list>
/// </para>
/// <para>
/// Enable via <see cref="PrivacyByDesignOptions.AddHealthCheck"/>:
/// <code>
/// services.AddEncinaPrivacyByDesign(options =>
/// {
///     options.AddHealthCheck = true;
/// });
/// </code>
/// </para>
/// </remarks>
public sealed class PrivacyByDesignHealthCheck : IHealthCheck
{
    /// <summary>
    /// Default health check name.
    /// </summary>
    public const string DefaultName = "encina-privacy-by-design";

    private static readonly string[] DefaultTags =
        ["encina", "gdpr", "privacy-by-design", "compliance", "ready"];

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PrivacyByDesignHealthCheck> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PrivacyByDesignHealthCheck"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve PbD services.</param>
    /// <param name="logger">The logger instance.</param>
    public PrivacyByDesignHealthCheck(
        IServiceProvider serviceProvider,
        ILogger<PrivacyByDesignHealthCheck> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Gets the default tags for the Privacy by Design health check.
    /// </summary>
    internal static IEnumerable<string> Tags => DefaultTags;

    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>();
        var warnings = new List<string>();

        using var scope = _serviceProvider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        // 1. Verify options are valid
        var options = scopedProvider.GetService<IOptions<PrivacyByDesignOptions>>()?.Value;
        if (options is null)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "PrivacyByDesignOptions are not configured. "
                + "Call AddEncinaPrivacyByDesign() in DI setup."));
        }

        data["enforcementMode"] = options.EnforcementMode.ToString();
        data["privacyLevel"] = options.PrivacyLevel.ToString();
        data["minimizationScoreThreshold"] = options.MinimizationScoreThreshold;

        // 2. Verify validator is resolvable
        var validator = scopedProvider.GetService<IPrivacyByDesignValidator>();
        if (validator is null)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "IPrivacyByDesignValidator is not registered.",
                data: data));
        }

        data["validatorType"] = validator.GetType().Name;

        // 3. Verify purpose registry is resolvable
        var purposeRegistry = scopedProvider.GetService<IPurposeRegistry>();
        if (purposeRegistry is null)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "IPurposeRegistry is not registered.",
                data: data));
        }

        data["purposeRegistryType"] = purposeRegistry.GetType().Name;

        // 4. Verify analyzer is resolvable
        var analyzer = scopedProvider.GetService<IDataMinimizationAnalyzer>();
        if (analyzer is null)
        {
            warnings.Add(
                "IDataMinimizationAnalyzer is not registered. "
                + "Data minimization analysis will not be available.");
        }
        else
        {
            data["analyzerType"] = analyzer.GetType().Name;
        }

        var status = warnings.Count == 0 ? "Healthy" : "Degraded";
        _logger.PbDHealthCheckCompleted(status, warnings.Count);

        if (warnings.Count > 0)
        {
            data["warnings"] = warnings;
            return Task.FromResult(HealthCheckResult.Degraded(
                $"Privacy by Design infrastructure has warnings: {string.Join("; ", warnings)}",
                data: data));
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            "Privacy by Design infrastructure is fully configured.",
            data: data));
    }
}
