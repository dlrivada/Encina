using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Compliance.BreachNotification.Health;

/// <summary>
/// Health check that verifies breach notification infrastructure is properly configured
/// and reports on approaching or overdue notification deadlines.
/// </summary>
/// <remarks>
/// <para>
/// This health check verifies:
/// <list type="bullet">
/// <item><description>The breach notification options are configured</description></item>
/// <item><description>The breach record store (<see cref="IBreachRecordStore"/>) is resolvable</description></item>
/// <item><description>The breach detector (<see cref="IBreachDetector"/>) is resolvable</description></item>
/// <item><description>The breach notifier (<see cref="IBreachNotifier"/>) is resolvable</description></item>
/// <item><description>The breach audit store (<see cref="IBreachAuditStore"/>) is resolvable when TrackAuditTrail is enabled (optional, Degraded if missing)</description></item>
/// <item><description>Overdue breaches count (Degraded if any exist)</description></item>
/// <item><description>Approaching deadline breaches (Degraded if any exist)</description></item>
/// </list>
/// </para>
/// <para>
/// Enable via <see cref="BreachNotificationOptions.AddHealthCheck"/>:
/// <code>
/// services.AddEncinaBreachNotification(options =>
/// {
///     options.AddHealthCheck = true;
/// });
/// </code>
/// </para>
/// </remarks>
public sealed class BreachNotificationHealthCheck : IHealthCheck
{
    /// <summary>
    /// Default health check name.
    /// </summary>
    public const string DefaultName = "encina-breach-notification";

    private static readonly string[] DefaultTags =
        ["encina", "gdpr", "breach", "compliance", "ready"];

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BreachNotificationHealthCheck> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BreachNotificationHealthCheck"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve breach notification services.</param>
    /// <param name="logger">The logger instance.</param>
    public BreachNotificationHealthCheck(
        IServiceProvider serviceProvider,
        ILogger<BreachNotificationHealthCheck> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Gets the default tags for the breach notification health check.
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
        var options = scopedProvider.GetService<IOptions<BreachNotificationOptions>>()?.Value;
        if (options is null)
        {
            return HealthCheckResult.Unhealthy(
                "BreachNotificationOptions are not configured. "
                + "Call AddEncinaBreachNotification() in DI setup.");
        }

        data["enforcementMode"] = options.EnforcementMode.ToString();
        data["deadlineMonitoringEnabled"] = options.EnableDeadlineMonitoring;
        data["notificationDeadlineHours"] = options.NotificationDeadlineHours;

        // 2. Verify breach record store is resolvable
        var recordStore = scopedProvider.GetService<IBreachRecordStore>();
        if (recordStore is null)
        {
            return HealthCheckResult.Unhealthy(
                "IBreachRecordStore is not registered.",
                data: data);
        }

        data["recordStoreType"] = recordStore.GetType().Name;

        // 3. Verify breach detector is resolvable
        var detector = scopedProvider.GetService<IBreachDetector>();
        if (detector is null)
        {
            return HealthCheckResult.Unhealthy(
                "IBreachDetector is not registered.",
                data: data);
        }

        data["detectorType"] = detector.GetType().Name;

        // 4. Verify breach notifier is resolvable
        var notifier = scopedProvider.GetService<IBreachNotifier>();
        if (notifier is null)
        {
            return HealthCheckResult.Unhealthy(
                "IBreachNotifier is not registered.",
                data: data);
        }

        data["notifierType"] = notifier.GetType().Name;

        // 5. Verify audit store when TrackAuditTrail is enabled (optional, degraded if missing)
        if (options.TrackAuditTrail)
        {
            var auditStore = scopedProvider.GetService<IBreachAuditStore>();
            if (auditStore is null)
            {
                warnings.Add(
                    "IBreachAuditStore is not registered but TrackAuditTrail is enabled. "
                    + "Breach audit trail will not be recorded.");
            }
            else
            {
                data["auditStoreType"] = auditStore.GetType().Name;
            }
        }

        // 6. Check for overdue breaches (degraded if any)
        try
        {
            var overdueResult = await recordStore
                .GetOverdueBreachesAsync(cancellationToken)
                .ConfigureAwait(false);

            overdueResult.Match(
                Right: overdueBreaches =>
                {
                    data["overdueBreachCount"] = overdueBreaches.Count;

                    if (overdueBreaches.Count > 0)
                    {
                        warnings.Add(
                            $"{overdueBreaches.Count} breach(es) have exceeded the "
                            + $"{options.NotificationDeadlineHours}-hour notification deadline. "
                            + "Delay reasons must be documented per Article 33(1).");
                    }
                },
                Left: error =>
                {
                    warnings.Add($"Unable to query overdue breaches: {error.Message}");
                });
        }
        catch (Exception ex)
        {
            warnings.Add($"Error querying overdue breaches: {ex.Message}");
        }

        // 7. Check for approaching deadline breaches (informational)
        try
        {
            // Use the smallest alert threshold (most urgent) for the health check
            var urgentThreshold = options.AlertAtHoursRemaining.Length > 0
                ? options.AlertAtHoursRemaining.Min()
                : 12;

            var approachingResult = await recordStore
                .GetApproachingDeadlineAsync(urgentThreshold, cancellationToken)
                .ConfigureAwait(false);

            approachingResult.Match(
                Right: approaching =>
                {
                    data["approachingDeadlineCount"] = approaching.Count;

                    if (approaching.Count > 0)
                    {
                        warnings.Add(
                            $"{approaching.Count} breach(es) approaching notification deadline "
                            + $"(within {urgentThreshold}h remaining).");
                    }
                },
                Left: _ => { });
        }
        catch
        {
            // Approaching deadline check is informational — don't fail or degrade for this
        }

        _logger.LogDebug(
            "Breach notification health check completed: {Status} ({WarningCount} warnings)",
            warnings.Count == 0 ? "Healthy" : "Degraded",
            warnings.Count);

        if (warnings.Count > 0)
        {
            data["warnings"] = warnings;
            return HealthCheckResult.Degraded(
                $"Breach notification infrastructure has warnings: {string.Join("; ", warnings)}",
                data: data);
        }

        return HealthCheckResult.Healthy(
            "Breach notification infrastructure is fully configured.",
            data: data);
    }
}
