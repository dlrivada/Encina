using Encina.Compliance.BreachNotification.Model;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Compliance.BreachNotification;

/// <summary>
/// Background hosted service that periodically monitors breach notification deadlines
/// and publishes <see cref="DeadlineWarningNotification"/> alerts at configurable thresholds.
/// </summary>
/// <remarks>
/// <para>
/// The service runs monitoring cycles at a configurable interval (default: 15 minutes),
/// each cycle performing the following steps:
/// <list type="number">
/// <item><description>Create a new <see cref="IServiceScope"/> to resolve scoped dependencies.</description></item>
/// <item><description>Resolve <see cref="IBreachRecordStore"/> from the scoped service provider.</description></item>
/// <item><description>For each configured alert threshold in <see cref="BreachNotificationOptions.AlertAtHoursRemaining"/>,
/// query breaches approaching their deadline.</description></item>
/// <item><description>Publish <see cref="DeadlineWarningNotification"/> for each matching breach.</description></item>
/// <item><description>Query and log overdue breaches.</description></item>
/// </list>
/// </para>
/// <para>
/// The service uses <see cref="PeriodicTimer"/> for scheduling, which is more efficient
/// than <c>Task.Delay</c> for periodic work as it doesn't drift and handles elapsed
/// periods gracefully.
/// </para>
/// <para>
/// Graceful error handling: individual monitoring cycle failures are logged but never crash
/// the host. The service continues running and attempts monitoring again on the next cycle.
/// </para>
/// <para>
/// Controlled by <see cref="BreachNotificationOptions.EnableDeadlineMonitoring"/> (default: <c>false</c>).
/// When disabled, the service is not registered. When registered, the service starts immediately
/// and runs until the host shuts down.
/// </para>
/// <para>
/// Per GDPR Article 33(1), the controller must notify the supervisory authority
/// "not later than 72 hours after having become aware of it." This service ensures
/// proactive deadline tracking so notifications are not missed.
/// </para>
/// </remarks>
internal sealed class BreachDeadlineMonitorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly BreachNotificationOptions _options;
    private readonly ILogger<BreachDeadlineMonitorService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BreachDeadlineMonitorService"/> class.
    /// </summary>
    /// <param name="scopeFactory">Factory for creating service scopes to resolve scoped dependencies.</param>
    /// <param name="options">Breach notification options controlling monitoring behavior.</param>
    /// <param name="logger">Logger for diagnostic messages.</param>
    public BreachDeadlineMonitorService(
        IServiceScopeFactory scopeFactory,
        IOptions<BreachNotificationOptions> options,
        ILogger<BreachDeadlineMonitorService> logger)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Breach deadline monitoring service started with check interval {Interval}",
            _options.DeadlineCheckInterval);

        using var timer = new PeriodicTimer(_options.DeadlineCheckInterval);

        // Execute first cycle immediately, then wait for timer ticks
        await ExecuteMonitoringCycleAsync(stoppingToken).ConfigureAwait(false);

        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            await ExecuteMonitoringCycleAsync(stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task ExecuteMonitoringCycleAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();

            var recordStore = scope.ServiceProvider.GetRequiredService<IBreachRecordStore>();

            // Check each alert threshold
            foreach (var threshold in _options.AlertAtHoursRemaining)
            {
                await CheckDeadlineThresholdAsync(
                    recordStore, scope.ServiceProvider, threshold, cancellationToken)
                    .ConfigureAwait(false);
            }

            // Check for overdue breaches
            await CheckOverdueBreachesAsync(recordStore, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Breach deadline monitoring cycle cancelled");
        }
        catch (Exception ex)
        {
            // Graceful error handling: log + continue, never crash the host
            _logger.LogError(ex, "Unhandled exception during breach deadline monitoring cycle");
        }
    }

    private async Task CheckDeadlineThresholdAsync(
        IBreachRecordStore recordStore,
        IServiceProvider scopedProvider,
        int hoursRemaining,
        CancellationToken cancellationToken)
    {
        try
        {
            var approachingResult = await recordStore
                .GetApproachingDeadlineAsync(hoursRemaining, cancellationToken)
                .ConfigureAwait(false);

            approachingResult.Match(
                Right: approaching =>
                {
                    if (approaching.Count > 0)
                    {
                        _logger.LogWarning(
                            "{Count} breach(es) approaching notification deadline "
                            + "(within {Hours}h remaining)",
                            approaching.Count, hoursRemaining);

                        if (_options.PublishNotifications)
                        {
                            _ = PublishDeadlineWarningsAsync(
                                approaching, scopedProvider, cancellationToken);
                        }
                    }
                },
                Left: error =>
                {
                    _logger.LogWarning(
                        "Failed to query approaching deadline breaches for {Hours}h threshold: {ErrorMessage}",
                        hoursRemaining, error.Message);
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error checking deadline threshold at {Hours}h remaining", hoursRemaining);
        }
    }

    private async Task CheckOverdueBreachesAsync(
        IBreachRecordStore recordStore,
        CancellationToken cancellationToken)
    {
        try
        {
            var overdueResult = await recordStore
                .GetOverdueBreachesAsync(cancellationToken)
                .ConfigureAwait(false);

            overdueResult.Match(
                Right: overdueBreaches =>
                {
                    if (overdueBreaches.Count > 0)
                    {
                        _logger.LogError(
                            "{Count} breach(es) have exceeded the {Deadline}-hour notification deadline. "
                            + "Delay reasons must be documented per Article 33(1).",
                            overdueBreaches.Count, _options.NotificationDeadlineHours);
                    }
                },
                Left: error =>
                {
                    _logger.LogWarning(
                        "Failed to query overdue breaches: {ErrorMessage}", error.Message);
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for overdue breaches");
        }
    }

    private async Task PublishDeadlineWarningsAsync(
        IReadOnlyList<DeadlineStatus> approaching,
        IServiceProvider scopedProvider,
        CancellationToken cancellationToken)
    {
        var encina = scopedProvider.GetService<IEncina>();
        if (encina is null)
        {
            return;
        }

        try
        {
            foreach (var status in approaching)
            {
                var notification = new DeadlineWarningNotification(
                    status.BreachId,
                    status.RemainingHours,
                    status.DeadlineUtc);

                await encina.Publish(notification, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            // Notification publishing should never fail the monitoring cycle
            _logger.LogWarning(ex, "Failed to publish deadline warning notifications");
        }
    }
}
