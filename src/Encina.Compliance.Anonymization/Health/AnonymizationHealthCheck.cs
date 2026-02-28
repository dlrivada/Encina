using Encina.Compliance.Anonymization.Diagnostics;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Compliance.Anonymization.Health;

/// <summary>
/// Health check that verifies anonymization infrastructure is properly configured
/// and required services are available.
/// </summary>
/// <remarks>
/// <para>
/// This health check verifies:
/// <list type="bullet">
/// <item><description>The anonymization options are configured</description></item>
/// <item><description>The anonymization audit store (<see cref="IAnonymizationAuditStore"/>) is resolvable</description></item>
/// <item><description>The key provider (<see cref="IKeyProvider"/>) is resolvable</description></item>
/// <item><description>The pseudonymizer (<see cref="IPseudonymizer"/>) is resolvable</description></item>
/// <item><description>The tokenizer (<see cref="ITokenizer"/>) is resolvable (optional, Degraded if missing)</description></item>
/// <item><description>At least one <see cref="IAnonymizationTechnique"/> is registered</description></item>
/// </list>
/// </para>
/// <para>
/// Enable via <see cref="AnonymizationOptions.AddHealthCheck"/>:
/// <code>
/// services.AddEncinaAnonymization(options =>
/// {
///     options.AddHealthCheck = true;
/// });
/// </code>
/// </para>
/// </remarks>
public sealed class AnonymizationHealthCheck : IHealthCheck
{
    /// <summary>
    /// Default health check name.
    /// </summary>
    public const string DefaultName = "encina-anonymization";

    private static readonly string[] DefaultTags = ["encina", "gdpr", "anonymization", "compliance", "ready"];

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AnonymizationHealthCheck> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnonymizationHealthCheck"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve anonymization services.</param>
    /// <param name="logger">The logger instance.</param>
    public AnonymizationHealthCheck(IServiceProvider serviceProvider, ILogger<AnonymizationHealthCheck> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Gets the default tags for the anonymization health check.
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
        var options = scopedProvider.GetService<IOptions<AnonymizationOptions>>()?.Value;
        if (options is null)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "AnonymizationOptions are not configured. Call AddEncinaAnonymization() in DI setup."));
        }

        data["enforcementMode"] = options.EnforcementMode.ToString();
        data["trackAuditTrail"] = options.TrackAuditTrail;

        // 2. Verify key provider is resolvable
        var keyProvider = scopedProvider.GetService<IKeyProvider>();
        if (keyProvider is null)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "IKeyProvider is not registered.",
                data: data));
        }

        data["keyProviderType"] = keyProvider.GetType().Name;

        // 3. Verify pseudonymizer is resolvable
        var pseudonymizer = scopedProvider.GetService<IPseudonymizer>();
        if (pseudonymizer is null)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "IPseudonymizer is not registered.",
                data: data));
        }

        data["pseudonymizerType"] = pseudonymizer.GetType().Name;

        // 4. Verify anonymization techniques are registered
        var techniques = scopedProvider.GetService<IEnumerable<IAnonymizationTechnique>>();
        var techniqueCount = techniques?.Count() ?? 0;
        data["registeredTechniques"] = techniqueCount;

        if (techniqueCount == 0)
        {
            warnings.Add("No IAnonymizationTechnique implementations registered. "
                        + "Anonymization pipeline will fail for fields decorated with [Anonymize].");
        }

        // 5. Verify tokenizer is resolvable (optional, degraded if missing)
        if (scopedProvider.GetService<ITokenizer>() is null)
        {
            warnings.Add("ITokenizer is not registered. "
                        + "Tokenization will not be available for fields decorated with [Tokenize].");
        }

        // 6. Verify audit store is resolvable (optional, degraded if missing)
        if (options.TrackAuditTrail && scopedProvider.GetService<IAnonymizationAuditStore>() is null)
        {
            warnings.Add("IAnonymizationAuditStore is not registered but TrackAuditTrail is enabled. "
                        + "Anonymization audit trail will not be recorded.");
        }

        _logger.AnonymizationHealthCheckCompleted(
            warnings.Count == 0 ? "Healthy" : "Degraded",
            techniqueCount);

        if (warnings.Count > 0)
        {
            data["warnings"] = warnings;
            return Task.FromResult(HealthCheckResult.Degraded(
                $"Anonymization infrastructure is partially configured: {string.Join("; ", warnings)}",
                data: data));
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            "Anonymization infrastructure is fully configured.",
            data: data));
    }
}
