using Encina.Compliance.GDPR.Diagnostics;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Encina.Compliance.GDPR.Health;

/// <summary>
/// Health check that verifies lawful basis infrastructure is properly configured.
/// </summary>
/// <remarks>
/// <para>
/// This health check verifies:
/// <list type="bullet">
/// <item><description>The <see cref="ILawfulBasisRegistry"/> is resolvable and populated.</description></item>
/// <item><description>The <see cref="ILIAStore"/> is resolvable and reports pending reviews.</description></item>
/// <item><description>The <see cref="ILegitimateInterestAssessment"/> is registered.</description></item>
/// </list>
/// </para>
/// <para>
/// Enable via <see cref="LawfulBasisOptions.AddHealthCheck"/>:
/// <code>
/// services.AddEncinaLawfulBasis(options =>
/// {
///     options.AddHealthCheck = true;
/// });
/// </code>
/// </para>
/// </remarks>
public sealed class LawfulBasisHealthCheck : IHealthCheck
{
    /// <summary>
    /// Default health check name.
    /// </summary>
    public const string DefaultName = "encina-lawful-basis";

    private static readonly string[] DefaultTags = ["encina", "gdpr", "lawful-basis", "ready"];

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LawfulBasisHealthCheck> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LawfulBasisHealthCheck"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve lawful basis services.</param>
    /// <param name="logger">The logger instance.</param>
    public LawfulBasisHealthCheck(IServiceProvider serviceProvider, ILogger<LawfulBasisHealthCheck> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Gets the default tags for the lawful basis health check.
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

        // 1. Verify registry is resolvable
        var registry = scopedProvider.GetService<ILawfulBasisRegistry>();
        if (registry is null)
        {
            return HealthCheckResult.Unhealthy(
                "ILawfulBasisRegistry is not registered. Call AddEncinaLawfulBasis() in DI setup.",
                data: data);
        }

        // 2. Count registrations
        try
        {
            var registrationsResult = await registry.GetAllAsync(cancellationToken).ConfigureAwait(false);

            registrationsResult.Match(
                Right: registrations =>
                {
                    data["registrations_count"] = registrations.Count;

                    if (registrations.Count == 0)
                    {
                        warnings.Add(
                            "No lawful basis registrations found. "
                            + "Ensure request types are decorated with [LawfulBasis] or registered via DefaultBases.");
                    }
                },
                Left: error =>
                {
                    warnings.Add($"Failed to query lawful basis registry: {error.Message}");
                });
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded(
                "Failed to access ILawfulBasisRegistry.",
                exception: ex,
                data: data);
        }

        // 3. Check LIA store
        var liaStore = scopedProvider.GetService<ILIAStore>();
        if (liaStore is not null)
        {
            try
            {
                var pendingResult = await liaStore
                    .GetPendingReviewAsync(cancellationToken)
                    .ConfigureAwait(false);

                pendingResult.Match(
                    Right: pending =>
                    {
                        data["lia_pending_review_count"] = pending.Count;

                        if (pending.Count > 0)
                        {
                            warnings.Add(
                                $"{pending.Count} Legitimate Interest Assessment(s) pending review.");
                        }
                    },
                    Left: error =>
                    {
                        warnings.Add($"Failed to query LIA store: {error.Message}");
                    });
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to access ILIAStore: {ex.Message}");
            }
        }
        else
        {
            data["lia_store_registered"] = false;
        }

        // 4. Check LIA assessment service
        var liaAssessment = scopedProvider.GetService<ILegitimateInterestAssessment>();
        data["lia_assessment_registered"] = liaAssessment is not null;

        _logger.LawfulBasisHealthCheckCompleted(
            warnings.Count == 0 ? "Healthy" : "Degraded",
            data.TryGetValue("registrations_count", out var count) ? (int)count : 0);

        if (warnings.Count > 0)
        {
            data["warnings"] = warnings;
            return HealthCheckResult.Degraded(
                $"Lawful basis infrastructure is partially configured: {string.Join("; ", warnings)}",
                data: data);
        }

        return HealthCheckResult.Healthy(
            "Lawful basis infrastructure is fully configured.",
            data: data);
    }
}
