using System.Diagnostics.CodeAnalysis;

using Encina.Compliance.DPIA.Abstractions;
using Encina.Compliance.DPIA.Model;
using Encina.Compliance.DPIA.ReadModels;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Compliance.DPIA.Health;

/// <summary>
/// Health check that verifies DPIA infrastructure is properly configured
/// and reports on missing or expired assessments.
/// </summary>
/// <remarks>
/// <para>
/// This health check verifies:
/// <list type="bullet">
/// <item><description>The DPIA options are configured</description></item>
/// <item><description>The DPIA service (<see cref="IDPIAService"/>) is resolvable</description></item>
/// <item><description>The DPIA assessment engine (<see cref="IDPIAAssessmentEngine"/>) is resolvable</description></item>
/// <item><description>Expired assessments count (Degraded if any exist)</description></item>
/// <item><description>Draft assessments count (informational, Degraded if in Block mode)</description></item>
/// </list>
/// </para>
/// <para>
/// Enable via <see cref="DPIAOptions.AddHealthCheck"/>:
/// <code>
/// services.AddEncinaDPIA(options =>
/// {
///     options.AddHealthCheck = true;
/// });
/// </code>
/// </para>
/// </remarks>
public sealed class DPIAHealthCheck : IHealthCheck
{
    /// <summary>
    /// Default health check name.
    /// </summary>
    public const string DefaultName = "encina-dpia";

    private static readonly string[] DefaultTags =
        ["encina", "gdpr", "dpia", "compliance", "ready"];

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DPIAHealthCheck> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DPIAHealthCheck"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve DPIA services.</param>
    /// <param name="logger">The logger instance.</param>
    public DPIAHealthCheck(
        IServiceProvider serviceProvider,
        ILogger<DPIAHealthCheck> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Gets the default tags for the DPIA health check.
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

        // 1-3. Verify options, DPIA service and assessment engine are resolvable
        if (!TryResolveDependencies(scope.ServiceProvider, data, out var options, out var service, out var failure))
        {
            return failure.Value;
        }

        // 4. Check for expired assessments (degraded if any)
        await CheckExpiredAssessmentsAsync(service, data, warnings, cancellationToken)
            .ConfigureAwait(false);

        // 5. Check for draft assessments (informational, degraded in Block mode)
        await CheckDraftAssessmentsAsync(service, options, data, warnings, cancellationToken)
            .ConfigureAwait(false);

        _logger.LogDebug(
            "DPIA health check completed: {Status} ({WarningCount} warnings)",
            warnings.Count == 0 ? "Healthy" : "Degraded",
            warnings.Count);

        if (warnings.Count > 0)
        {
            data["warnings"] = warnings;
            return HealthCheckResult.Degraded(
                $"DPIA infrastructure has warnings: {string.Join("; ", warnings)}",
                data: data);
        }

        return HealthCheckResult.Healthy(
            "DPIA infrastructure is fully configured.",
            data: data);
    }

    /// <summary>
    /// Resolves the DPIA options, service and assessment engine from the scoped provider.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when every dependency resolved; otherwise <see langword="false"/>,
    /// with <paramref name="failure"/> set to the <see cref="HealthCheckResult.Unhealthy(string, Exception?, IReadOnlyDictionary{string, object}?)"/>
    /// result to return.
    /// </returns>
    private static bool TryResolveDependencies(
        IServiceProvider scopedProvider,
        Dictionary<string, object> data,
        [NotNullWhen(true)] out DPIAOptions? options,
        [NotNullWhen(true)] out IDPIAService? service,
        [NotNullWhen(false)] out HealthCheckResult? failure)
    {
        options = scopedProvider.GetService<IOptions<DPIAOptions>>()?.Value;
        if (options is null)
        {
            service = null;
            failure = HealthCheckResult.Unhealthy(
                "DPIAOptions are not configured. "
                + "Call AddEncinaDPIA() in DI setup.");
            return false;
        }

        data["enforcementMode"] = options.EnforcementMode.ToString();
        data["expirationMonitoringEnabled"] = options.EnableExpirationMonitoring;
        data["defaultReviewPeriodDays"] = options.DefaultReviewPeriod.TotalDays;

        service = scopedProvider.GetService<IDPIAService>();
        if (service is null)
        {
            failure = HealthCheckResult.Unhealthy(
                "IDPIAService is not registered.",
                data: data);
            return false;
        }

        data["serviceType"] = service.GetType().Name;

        var engine = scopedProvider.GetService<IDPIAAssessmentEngine>();
        if (engine is null)
        {
            failure = HealthCheckResult.Unhealthy(
                "IDPIAAssessmentEngine is not registered.",
                data: data);
            return false;
        }

        data["engineType"] = engine.GetType().Name;

        failure = null;
        return true;
    }

    /// <summary>
    /// Queries expired DPIA assessments and records a warning when any exist or the query fails.
    /// </summary>
    private static async Task CheckExpiredAssessmentsAsync(
        IDPIAService service,
        Dictionary<string, object> data,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        try
        {
            var expiredResult = await service
                .GetExpiredAssessmentsAsync(cancellationToken)
                .ConfigureAwait(false);

            expiredResult.Match(
                Right: expired =>
                {
                    data["expiredAssessmentCount"] = expired.Count;

                    if (expired.Count > 0)
                    {
                        warnings.Add(
                            $"{expired.Count} DPIA assessment(s) have expired and require review. "
                            + "Per GDPR Article 35(11), the controller must review assessments periodically.");
                    }
                },
                Left: error =>
                {
                    warnings.Add(
                        $"Unable to query expired assessments: {error.GetCode().IfNone("encina.unknown")}");
                });
        }
        catch (Exception ex)
        {
            warnings.Add($"Error querying expired assessments: {ex.GetType().Name}");
        }
    }

    /// <summary>
    /// Queries all DPIA assessments and records a warning when draft assessments would be
    /// blocked under the configured enforcement mode. Failures are informational and never
    /// fail or degrade the overall result.
    /// </summary>
    private static async Task CheckDraftAssessmentsAsync(
        IDPIAService service,
        DPIAOptions options,
        Dictionary<string, object> data,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        try
        {
            var allResult = await service
                .GetAllAssessmentsAsync(cancellationToken)
                .ConfigureAwait(false);

            allResult.Match(
                Right: assessments =>
                {
                    var draftCount = 0;
                    foreach (var assessment in assessments)
                    {
                        if (assessment.Status == DPIAAssessmentStatus.Draft)
                        {
                            draftCount++;
                        }
                    }

                    data["draftAssessmentCount"] = draftCount;
                    data["totalAssessmentCount"] = assessments.Count;

                    if (draftCount > 0 && options.EnforcementMode == DPIAEnforcementMode.Block)
                    {
                        warnings.Add(
                            $"{draftCount} DPIA assessment(s) are still in Draft status while "
                            + "enforcement mode is Block. These request types will be blocked "
                            + "until their assessments are approved.");
                    }
                },
                Left: _ => { });
        }
        catch
        {
            // Draft assessment check is informational — don't fail or degrade for this
        }
    }
}
