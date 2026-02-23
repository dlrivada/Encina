using Encina.Compliance.Consent.Diagnostics;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Compliance.Consent.Health;

/// <summary>
/// Health check that verifies consent compliance infrastructure is properly configured.
/// </summary>
/// <remarks>
/// <para>
/// This health check verifies:
/// <list type="bullet">
/// <item><description>The consent store is resolvable and accessible</description></item>
/// <item><description>Purpose definitions are configured when enforcement is enabled</description></item>
/// <item><description>The consent validator is registered</description></item>
/// <item><description>The consent version manager is registered</description></item>
/// </list>
/// </para>
/// <para>
/// Enable via <see cref="ConsentOptions.AddHealthCheck"/>:
/// <code>
/// services.AddEncinaConsent(options =>
/// {
///     options.AddHealthCheck = true;
///     options.DefinePurpose(ConsentPurposes.Marketing, p =>
///     {
///         p.Description = "Email marketing communications";
///     });
/// });
/// </code>
/// </para>
/// </remarks>
public sealed class ConsentHealthCheck : IHealthCheck
{
    /// <summary>
    /// Default health check name.
    /// </summary>
    public const string DefaultName = "encina-consent";

    private static readonly string[] DefaultTags = ["encina", "consent", "compliance", "gdpr", "ready"];

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ConsentHealthCheck> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsentHealthCheck"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve consent services.</param>
    /// <param name="logger">The logger instance.</param>
    public ConsentHealthCheck(IServiceProvider serviceProvider, ILogger<ConsentHealthCheck> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Gets the default tags for the consent health check.
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
        var options = scopedProvider.GetService<IOptions<ConsentOptions>>()?.Value;
        if (options is null)
        {
            return Task.FromResult(
                HealthCheckResult.Unhealthy("ConsentOptions are not configured. Call AddEncinaConsent() in DI setup."));
        }

        data["enforcementMode"] = options.EnforcementMode.ToString();

        // 2. Check purpose definitions
        var totalPurposes = options.PurposeDefinitions.Count;
        data["purposeCount"] = totalPurposes;

        if (totalPurposes == 0 && options.EnforcementMode != ConsentEnforcementMode.Disabled)
        {
            warnings.Add("No purpose definitions are configured. "
                         + "Add purposes via PurposeDefinitions or DefinePurpose() for proper consent tracking.");
        }

        // 3. Verify consent store is resolvable
        var consentStore = scopedProvider.GetService<IConsentStore>();
        if (consentStore is null)
        {
            return Task.FromResult(
                HealthCheckResult.Unhealthy(
                    "IConsentStore is not registered.",
                    data: data));
        }

        data["consentStoreType"] = consentStore.GetType().Name;

        // 4. Verify consent validator is resolvable
        if (scopedProvider.GetService<IConsentValidator>() is null)
        {
            warnings.Add("IConsentValidator is not registered.");
        }

        // 5. Verify consent version manager is resolvable
        if (scopedProvider.GetService<IConsentVersionManager>() is null)
        {
            warnings.Add("IConsentVersionManager is not registered.");
        }

        _logger.ConsentHealthCheckCompleted(
            warnings.Count == 0 ? "Healthy" : "Degraded",
            totalPurposes);

        if (warnings.Count > 0)
        {
            data["warnings"] = warnings;
            return Task.FromResult(
                HealthCheckResult.Degraded(
                    $"Consent compliance is partially configured: {string.Join("; ", warnings)}",
                    data: data));
        }

        return Task.FromResult(
            HealthCheckResult.Healthy(
                "Consent compliance infrastructure is fully configured.",
                data: data));
    }
}
