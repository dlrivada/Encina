using Encina.Compliance.DPIA.Model;

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
/// <item><description>The DPIA store (<see cref="IDPIAStore"/>) is resolvable</description></item>
/// <item><description>The DPIA assessment engine (<see cref="IDPIAAssessmentEngine"/>) is resolvable</description></item>
/// <item><description>The DPIA audit store (<see cref="IDPIAAuditStore"/>) is resolvable when TrackAuditTrail is enabled (optional, Degraded if missing)</description></item>
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
        var scopedProvider = scope.ServiceProvider;

        // 1. Verify options are valid
        var options = scopedProvider.GetService<IOptions<DPIAOptions>>()?.Value;
        if (options is null)
        {
            return HealthCheckResult.Unhealthy(
                "DPIAOptions are not configured. "
                + "Call AddEncinaDPIA() in DI setup.");
        }

        data["enforcementMode"] = options.EnforcementMode.ToString();
        data["expirationMonitoringEnabled"] = options.EnableExpirationMonitoring;
        data["defaultReviewPeriodDays"] = options.DefaultReviewPeriod.TotalDays;

        // 2. Verify DPIA store is resolvable
        var store = scopedProvider.GetService<IDPIAStore>();
        if (store is null)
        {
            return HealthCheckResult.Unhealthy(
                "IDPIAStore is not registered.",
                data: data);
        }

        data["storeType"] = store.GetType().Name;

        // 3. Verify assessment engine is resolvable
        var engine = scopedProvider.GetService<IDPIAAssessmentEngine>();
        if (engine is null)
        {
            return HealthCheckResult.Unhealthy(
                "IDPIAAssessmentEngine is not registered.",
                data: data);
        }

        data["engineType"] = engine.GetType().Name;

        // 4. Verify audit store when TrackAuditTrail is enabled (optional, degraded if missing)
        if (options.TrackAuditTrail)
        {
            var auditStore = scopedProvider.GetService<IDPIAAuditStore>();
            if (auditStore is null)
            {
                warnings.Add(
                    "IDPIAAuditStore is not registered but TrackAuditTrail is enabled. "
                    + "DPIA audit trail will not be recorded.");
            }
            else
            {
                data["auditStoreType"] = auditStore.GetType().Name;
            }
        }

        // 5. Check for expired assessments (degraded if any)
        try
        {
            var timeProvider = scopedProvider.GetService<TimeProvider>() ?? TimeProvider.System;
            var nowUtc = timeProvider.GetUtcNow();

            var expiredResult = await store
                .GetExpiredAssessmentsAsync(nowUtc, cancellationToken)
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
                    warnings.Add($"Unable to query expired assessments: {error.Message}");
                });
        }
        catch (Exception ex)
        {
            warnings.Add($"Error querying expired assessments: {ex.Message}");
        }

        // 6. Check for draft assessments (informational, degraded in Block mode)
        try
        {
            var allResult = await store
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
}
