using Encina.Compliance.BreachNotification.Abstractions;
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
/// <item><description>Resolve <see cref="IBreachNotificationService"/> from the scoped service provider.</description></item>
/// <item><description>Query breaches approaching their deadline via
/// <see cref="IBreachNotificationService.GetApproachingDeadlineBreachesAsync"/>.</description></item>
/// <item><description>Publish <see cref="DeadlineWarningNotification"/> for each matching breach.</description></item>
/// <item><description>Log overdue breaches.</description></item>
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
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<BreachDeadlineMonitorService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BreachDeadlineMonitorService"/> class.
    /// </summary>
    /// <param name="scopeFactory">Factory for creating service scopes to resolve scoped dependencies.</param>
    /// <param name="options">Breach notification options controlling monitoring behavior.</param>
    /// <param name="timeProvider">The time provider for UTC timestamps.</param>
    /// <param name="logger">Logger for diagnostic messages.</param>
    public BreachDeadlineMonitorService(
        IServiceScopeFactory scopeFactory,
        IOptions<BreachNotificationOptions> options,
        TimeProvider timeProvider,
        ILogger<BreachDeadlineMonitorService> logger)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _scopeFactory = scopeFactory;
        _options = options.Value;
        _timeProvider = timeProvider;
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

            var breachService = scope.ServiceProvider.GetRequiredService<IBreachNotificationService>();

            // Query breaches approaching deadline (within 24h or overdue)
            var approachingResult = await breachService
                .GetApproachingDeadlineBreachesAsync(cancellationToken)
                .ConfigureAwait(false);

            approachingResult.Match(
                Right: approaching =>
                {
                    if (approaching.Count == 0)
                    {
                        return;
                    }

                    var now = _timeProvider.GetUtcNow();

                    foreach (var breach in approaching)
                    {
                        var remainingHours = (breach.DeadlineUtc - now).TotalHours;

                        if (remainingHours <= 0)
                        {
                            // Overdue
                            _logger.LogError(
                                "Breach '{BreachId}' has exceeded the {Deadline}-hour notification deadline "
                                + "by {OverdueHours:F1} hours. Delay reasons must be documented per Article 33(1).",
                                breach.Id, _options.NotificationDeadlineHours, Math.Abs(remainingHours));
                        }
                        else
                        {
                            // Check each alert threshold
                            foreach (var threshold in _options.AlertAtHoursRemaining)
                            {
                                if (remainingHours <= threshold)
                                {
                                    _logger.LogWarning(
                                        "Breach '{BreachId}' approaching notification deadline "
                                        + "(within {Hours}h remaining, {RemainingHours:F1}h left)",
                                        breach.Id, threshold, remainingHours);
                                    break; // Only log the most urgent threshold
                                }
                            }
                        }
                    }

                    // Publish notifications if enabled
                    if (_options.PublishNotifications)
                    {
                        _ = PublishDeadlineWarningsAsync(approaching, scope.ServiceProvider, now, cancellationToken);
                    }
                },
                Left: error =>
                {
                    _logger.LogWarning(
                        "Failed to query approaching deadline breaches: {ErrorMessage}",
                        error.Message);
                });
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

    private async Task PublishDeadlineWarningsAsync(
        IReadOnlyList<ReadModels.BreachReadModel> approaching,
        IServiceProvider scopedProvider,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var encina = scopedProvider.GetService<IEncina>();
        if (encina is null)
        {
            return;
        }

        try
        {
            foreach (var breach in approaching)
            {
                var remainingHours = (breach.DeadlineUtc - now).TotalHours;
                var notification = new DeadlineWarningNotification(
                    breach.Id.ToString(),
                    remainingHours,
                    breach.DeadlineUtc);

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
