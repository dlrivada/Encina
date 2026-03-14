using Encina.Compliance.CrossBorderTransfer.Abstractions;
using Encina.Compliance.CrossBorderTransfer.Diagnostics;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Compliance.CrossBorderTransfer.Health;

/// <summary>
/// Health check that verifies cross-border transfer compliance infrastructure is properly configured.
/// </summary>
/// <remarks>
/// <para>
/// This health check verifies:
/// <list type="bullet">
/// <item><description>The <see cref="CrossBorderTransferOptions"/> are configured and valid</description></item>
/// <item><description>The <see cref="ITransferValidator"/> is registered and resolvable</description></item>
/// <item><description>The <see cref="ITIAService"/> is registered and resolvable</description></item>
/// <item><description>The <see cref="ISCCService"/> is registered and resolvable</description></item>
/// <item><description>The <see cref="IApprovedTransferService"/> is registered and resolvable</description></item>
/// </list>
/// </para>
/// <para>
/// Enable via <see cref="CrossBorderTransferOptions.AddHealthCheck"/>:
/// <code>
/// services.AddEncinaCrossBorderTransfer(options =>
/// {
///     options.AddHealthCheck = true;
///     options.EnforcementMode = CrossBorderTransferEnforcementMode.Block;
///     options.DefaultSourceCountryCode = "DE";
/// });
/// </code>
/// </para>
/// </remarks>
public sealed class CrossBorderTransferHealthCheck : IHealthCheck
{
    /// <summary>
    /// Default health check name.
    /// </summary>
    public const string DefaultName = "encina-cross-border-transfer";

    private static readonly string[] DefaultTags = ["encina", "cross-border-transfer", "compliance", "gdpr", "ready"];

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CrossBorderTransferHealthCheck> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CrossBorderTransferHealthCheck"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve cross-border transfer services.</param>
    /// <param name="logger">The logger instance.</param>
    public CrossBorderTransferHealthCheck(IServiceProvider serviceProvider, ILogger<CrossBorderTransferHealthCheck> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Gets the default tags for the cross-border transfer health check.
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
        var options = scopedProvider.GetService<IOptions<CrossBorderTransferOptions>>()?.Value;
        if (options is null)
        {
            return Task.FromResult(
                HealthCheckResult.Unhealthy("CrossBorderTransferOptions are not configured. Call AddEncinaCrossBorderTransfer() in DI setup."));
        }

        data["enforcementMode"] = options.EnforcementMode.ToString();
        data["defaultSourceCountryCode"] = options.DefaultSourceCountryCode;
        data["tiaRiskThreshold"] = options.TIARiskThreshold;

        // 2. Verify transfer validator is resolvable
        var transferValidator = scopedProvider.GetService<ITransferValidator>();
        if (transferValidator is null)
        {
            return Task.FromResult(
                HealthCheckResult.Unhealthy(
                    "ITransferValidator is not registered.",
                    data: data));
        }

        data["transferValidatorType"] = transferValidator.GetType().Name;

        // 3. Verify TIA service is resolvable
        var tiaService = scopedProvider.GetService<ITIAService>();
        if (tiaService is null)
        {
            warnings.Add("ITIAService is not registered. TIA-based transfer validation will not be available.");
        }
        else
        {
            data["tiaServiceType"] = tiaService.GetType().Name;
        }

        // 4. Verify SCC service is resolvable
        var sccService = scopedProvider.GetService<ISCCService>();
        if (sccService is null)
        {
            warnings.Add("ISCCService is not registered. SCC-based transfer validation will not be available.");
        }
        else
        {
            data["sccServiceType"] = sccService.GetType().Name;
        }

        // 5. Verify approved transfer service is resolvable
        var transferService = scopedProvider.GetService<IApprovedTransferService>();
        if (transferService is null)
        {
            warnings.Add("IApprovedTransferService is not registered. Approved transfer lookups will not be available.");
        }
        else
        {
            data["approvedTransferServiceType"] = transferService.GetType().Name;
        }

        _logger.HealthCheckCompleted(
            warnings.Count == 0 ? "Healthy" : "Degraded",
            options.EnforcementMode.ToString());

        if (warnings.Count > 0)
        {
            data["warnings"] = warnings;
            return Task.FromResult(
                HealthCheckResult.Degraded(
                    $"Cross-border transfer compliance is partially configured: {string.Join("; ", warnings)}",
                    data: data));
        }

        return Task.FromResult(
            HealthCheckResult.Healthy(
                "Cross-border transfer compliance infrastructure is fully configured.",
                data: data));
    }
}
