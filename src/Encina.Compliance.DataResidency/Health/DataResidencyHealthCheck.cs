using Encina.Compliance.DataResidency.Abstractions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Compliance.DataResidency.Health;

/// <summary>
/// Health check that verifies data residency infrastructure is properly configured
/// and required services are available.
/// </summary>
/// <remarks>
/// <para>
/// This health check verifies:
/// <list type="bullet">
/// <item><description>The data residency options are configured</description></item>
/// <item><description>The residency policy service (<see cref="IResidencyPolicyService"/>) is resolvable</description></item>
/// <item><description>The data location service (<see cref="IDataLocationService"/>) is resolvable</description></item>
/// <item><description>The region context provider (<see cref="IRegionContextProvider"/>) is resolvable</description></item>
/// <item><description>The cross-border transfer validator (<see cref="ICrossBorderTransferValidator"/>) is resolvable</description></item>
/// </list>
/// </para>
/// <para>
/// Enable via <see cref="DataResidencyOptions.AddHealthCheck"/>:
/// <code>
/// services.AddEncinaDataResidency(options =>
/// {
///     options.AddHealthCheck = true;
/// });
/// </code>
/// </para>
/// </remarks>
public sealed class DataResidencyHealthCheck : IHealthCheck
{
    /// <summary>
    /// Default health check name.
    /// </summary>
    public const string DefaultName = "encina-data-residency";

    private static readonly string[] DefaultTags = ["encina", "gdpr", "data-residency", "compliance", "ready"];

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DataResidencyHealthCheck> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataResidencyHealthCheck"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve data residency services.</param>
    /// <param name="logger">The logger instance.</param>
    public DataResidencyHealthCheck(IServiceProvider serviceProvider, ILogger<DataResidencyHealthCheck> logger)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Gets the default tags for the data residency health check.
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
        var options = scopedProvider.GetService<IOptions<DataResidencyOptions>>()?.Value;
        if (options is null)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "DataResidencyOptions are not configured. Call AddEncinaDataResidency() in DI setup."));
        }

        data["enforcementMode"] = options.EnforcementMode.ToString();
        data["trackDataLocations"] = options.TrackDataLocations;

        // 2. Verify residency policy service is resolvable
        var policyService = scopedProvider.GetService<IResidencyPolicyService>();
        if (policyService is null)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "IResidencyPolicyService is not registered.",
                data: data));
        }

        data["policyServiceType"] = policyService.GetType().Name;
        servicesVerified++;

        // 3. Verify data location service is resolvable
        var locationService = scopedProvider.GetService<IDataLocationService>();
        if (locationService is null)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "IDataLocationService is not registered.",
                data: data));
        }

        data["locationServiceType"] = locationService.GetType().Name;
        servicesVerified++;

        // 4. Verify region context provider is resolvable
        var regionProvider = scopedProvider.GetService<IRegionContextProvider>();
        if (regionProvider is null)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "IRegionContextProvider is not registered.",
                data: data));
        }

        data["regionProviderType"] = regionProvider.GetType().Name;
        servicesVerified++;

        // 5. Verify cross-border transfer validator (optional, degraded if missing)
        var transferValidator = scopedProvider.GetService<ICrossBorderTransferValidator>();
        if (transferValidator is null)
        {
            warnings.Add("ICrossBorderTransferValidator is not registered. "
                        + "Cross-border transfer validation will not be available.");
        }
        else
        {
            data["transferValidatorType"] = transferValidator.GetType().Name;
            servicesVerified++;
        }

        _logger.LogDebug(
            "Data residency health check completed: status={Status}, servicesVerified={ServicesVerified}",
            warnings.Count == 0 ? "Healthy" : "Degraded",
            servicesVerified);

        if (warnings.Count > 0)
        {
            data["warnings"] = warnings;
            return Task.FromResult(HealthCheckResult.Degraded(
                $"Data residency infrastructure is partially configured: {string.Join("; ", warnings)}",
                data: data));
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            "Data residency infrastructure is fully configured.",
            data: data));
    }
}
