using Encina.Compliance.ProcessorAgreements.Abstractions;
using Encina.Compliance.ProcessorAgreements.Model;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Compliance.ProcessorAgreements.Health;

/// <summary>
/// Health check that verifies processor agreement infrastructure is properly configured
/// and reports on missing, expired, or incomplete DPAs.
/// </summary>
/// <remarks>
/// <para>
/// This health check verifies:
/// <list type="bullet">
/// <item><description>The processor agreement options are configured</description></item>
/// <item><description>The processor service (<see cref="IProcessorService"/>) is resolvable</description></item>
/// <item><description>The DPA service (<see cref="IDPAService"/>) is resolvable</description></item>
/// <item><description>Expired DPA count (Degraded if any exist)</description></item>
/// </list>
/// </para>
/// <para>
/// Enable via <see cref="ProcessorAgreementOptions.AddHealthCheck"/>:
/// <code>
/// services.AddEncinaProcessorAgreements(options =>
/// {
///     options.AddHealthCheck = true;
/// });
/// </code>
/// </para>
/// </remarks>
public sealed class ProcessorAgreementHealthCheck : IHealthCheck
{
    /// <summary>
    /// Default health check name.
    /// </summary>
    public const string DefaultName = "encina-processor-agreements";

    private static readonly string[] DefaultTags =
        ["encina", "gdpr", "processor-agreements", "dpa", "compliance", "ready"];

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProcessorAgreementHealthCheck> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessorAgreementHealthCheck"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve processor agreement services.</param>
    /// <param name="logger">The logger instance.</param>
    public ProcessorAgreementHealthCheck(
        IServiceProvider serviceProvider,
        ILogger<ProcessorAgreementHealthCheck> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Gets the default tags for the processor agreement health check.
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
        var options = scopedProvider.GetService<IOptions<ProcessorAgreementOptions>>()?.Value;
        if (options is null)
        {
            return HealthCheckResult.Unhealthy(
                "ProcessorAgreementOptions are not configured. "
                + "Call AddEncinaProcessorAgreements() in DI setup.");
        }

        data["enforcementMode"] = options.EnforcementMode.ToString();
        data["expirationMonitoringEnabled"] = options.EnableExpirationMonitoring;
        data["maxSubProcessorDepth"] = options.MaxSubProcessorDepth;

        // 2. Verify processor service is resolvable
        var processorService = scopedProvider.GetService<IProcessorService>();
        if (processorService is null)
        {
            return HealthCheckResult.Unhealthy(
                "IProcessorService is not registered. "
                + "Call AddEncinaProcessorAgreements() and AddProcessorAgreementAggregates() in DI setup.",
                data: data);
        }

        data["processorServiceType"] = processorService.GetType().Name;

        // 3. Verify DPA service is resolvable
        var dpaService = scopedProvider.GetService<IDPAService>();
        if (dpaService is null)
        {
            return HealthCheckResult.Unhealthy(
                "IDPAService is not registered. "
                + "Call AddEncinaProcessorAgreements() and AddProcessorAgreementAggregates() in DI setup.",
                data: data);
        }

        data["dpaServiceType"] = dpaService.GetType().Name;

        // 4. Check for expired agreements (degraded if any)
        try
        {
            var expiredResult = await dpaService
                .GetDPAsByStatusAsync(DPAStatus.Expired, cancellationToken)
                .ConfigureAwait(false);

            expiredResult.Match(
                Right: expired =>
                {
                    data["expiredDPACount"] = expired.Count;

                    if (expired.Count > 0)
                    {
                        warnings.Add(
                            $"{expired.Count} DPA(s) have expired and require renewal. "
                            + "Per GDPR Article 28(3), processing without a valid DPA is non-compliant.");
                    }
                },
                Left: error =>
                {
                    warnings.Add($"Unable to query expired DPAs: {error.Message}");
                });
        }
        catch (Exception ex)
        {
            warnings.Add($"Error querying expired DPAs: {ex.Message}");
        }

        _logger.LogDebug(
            "Processor agreement health check completed: {Status} ({WarningCount} warnings)",
            warnings.Count == 0 ? "Healthy" : "Degraded",
            warnings.Count);

        if (warnings.Count > 0)
        {
            data["warnings"] = warnings;
            return HealthCheckResult.Degraded(
                $"Processor agreement infrastructure has warnings: {string.Join("; ", warnings)}",
                data: data);
        }

        return HealthCheckResult.Healthy(
            "Processor agreement infrastructure is fully configured.",
            data: data);
    }
}
