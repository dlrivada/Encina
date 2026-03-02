using System.Diagnostics;
using Encina.Compliance.Retention.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Compliance.Retention;

/// <summary>
/// Background hosted service that periodically enforces retention policies by identifying
/// expired data and delegating deletion to <see cref="IRetentionEnforcer"/>.
/// </summary>
/// <remarks>
/// <para>
/// The service runs enforcement cycles at a configurable interval (default: 60 minutes),
/// each cycle performing the following steps:
/// <list type="number">
/// <item><description>Create a new <see cref="IServiceScope"/> to resolve scoped dependencies.</description></item>
/// <item><description>Resolve <see cref="IRetentionEnforcer"/> from the scoped service provider.</description></item>
/// <item><description>Check for data expiring within the <see cref="RetentionOptions.AlertBeforeExpirationDays"/> window.</description></item>
/// <item><description>Execute <see cref="IRetentionEnforcer.EnforceRetentionAsync"/> to process expired records.</description></item>
/// <item><description>Publish <see cref="DataExpiringNotification"/> events for upcoming expirations.</description></item>
/// </list>
/// </para>
/// <para>
/// The service uses <see cref="PeriodicTimer"/> for scheduling (introduced in .NET 6),
/// which is more efficient than <c>Task.Delay</c> for periodic work as it doesn't drift
/// and handles elapsed periods gracefully.
/// </para>
/// <para>
/// Graceful error handling: individual enforcement cycle failures are logged but never crash
/// the host. The service continues running and attempts enforcement again on the next cycle.
/// </para>
/// <para>
/// Controlled by <see cref="RetentionOptions.EnableAutomaticEnforcement"/> (default: <c>true</c>).
/// When disabled, the service starts but does not execute enforcement cycles, allowing
/// manual enforcement via <see cref="IRetentionEnforcer.EnforceRetentionAsync"/>.
/// </para>
/// <para>
/// Per GDPR Article 5(1)(e) (storage limitation), automated enforcement ensures personal data
/// is not retained longer than necessary. Per Recital 39, the controller should establish
/// appropriate time limits for erasure or for a periodic review.
/// </para>
/// </remarks>
public sealed class RetentionEnforcementService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RetentionOptions _options;
    private readonly ILogger<RetentionEnforcementService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetentionEnforcementService"/> class.
    /// </summary>
    /// <param name="scopeFactory">Factory for creating service scopes to resolve scoped dependencies.</param>
    /// <param name="options">Retention configuration options controlling enforcement behavior.</param>
    /// <param name="logger">Logger for diagnostic messages.</param>
    public RetentionEnforcementService(
        IServiceScopeFactory scopeFactory,
        IOptions<RetentionOptions> options,
        ILogger<RetentionEnforcementService> logger)
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
        if (!_options.EnableAutomaticEnforcement)
        {
            _logger.RetentionEnforcementServiceDisabled();
            return;
        }

        _logger.RetentionEnforcementServiceStarted(_options.EnforcementInterval);

        using var timer = new PeriodicTimer(_options.EnforcementInterval);

        // Execute first cycle immediately, then wait for timer ticks
        await ExecuteEnforcementCycleAsync(stoppingToken).ConfigureAwait(false);

        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            await ExecuteEnforcementCycleAsync(stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task ExecuteEnforcementCycleAsync(CancellationToken cancellationToken)
    {
        var startTimestamp = Stopwatch.GetTimestamp();
        using var activity = RetentionDiagnostics.StartEnforcementCycle();

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();

            var enforcer = scope.ServiceProvider.GetRequiredService<IRetentionEnforcer>();

            _logger.RetentionEnforcementCycleStarting();

            // Step 1: Check for expiring data and publish alerts
            await CheckExpiringDataAsync(enforcer, scope.ServiceProvider, cancellationToken)
                .ConfigureAwait(false);

            // Step 2: Enforce retention (delete expired records)
            var result = await enforcer.EnforceRetentionAsync(cancellationToken).ConfigureAwait(false);

            result.Match(
                Right: deletionResult =>
                {
                    if (deletionResult.TotalRecordsEvaluated > 0)
                    {
                        _logger.RetentionEnforcementCycleCompleted(
                            deletionResult.RecordsDeleted,
                            deletionResult.RecordsFailed,
                            deletionResult.RecordsUnderHold);

                        RetentionDiagnostics.RecordsDeletedTotal.Add(
                            deletionResult.RecordsDeleted,
                            new KeyValuePair<string, object?>(RetentionDiagnostics.TagOutcome, "completed"));
                        RetentionDiagnostics.RecordsHeldTotal.Add(
                            deletionResult.RecordsUnderHold,
                            new KeyValuePair<string, object?>(RetentionDiagnostics.TagOutcome, "held"));
                        RetentionDiagnostics.RecordsFailedTotal.Add(
                            deletionResult.RecordsFailed,
                            new KeyValuePair<string, object?>(RetentionDiagnostics.TagFailureReason, "enforcement"));

                        RetentionDiagnostics.RecordCompleted(activity, deletionResult.TotalRecordsEvaluated);
                    }
                    else
                    {
                        _logger.RetentionEnforcementCycleEmpty();
                        RetentionDiagnostics.RecordCompleted(activity);
                    }

                    RetentionDiagnostics.EnforcementCyclesTotal.Add(1,
                        new KeyValuePair<string, object?>(RetentionDiagnostics.TagOutcome, "completed"));
                },
                Left: error =>
                {
                    _logger.RetentionEnforcementCycleFailed(
                        new InvalidOperationException(error.Message));

                    RetentionDiagnostics.RecordFailed(activity, error.Message);
                    RetentionDiagnostics.EnforcementCyclesTotal.Add(1,
                        new KeyValuePair<string, object?>(RetentionDiagnostics.TagOutcome, "failed"));
                });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.RetentionEnforcementCycleCancelled();
            RetentionDiagnostics.RecordFailed(activity, "cancelled");
            RetentionDiagnostics.EnforcementCyclesTotal.Add(1,
                new KeyValuePair<string, object?>(RetentionDiagnostics.TagOutcome, "cancelled"));
        }
        catch (Exception ex)
        {
            // Graceful error handling: log + continue, never crash the host
            _logger.RetentionEnforcementCycleFailed(ex);
            RetentionDiagnostics.RecordFailed(activity, ex.Message);
            RetentionDiagnostics.EnforcementCyclesTotal.Add(1,
                new KeyValuePair<string, object?>(RetentionDiagnostics.TagOutcome, "failed"));
        }
        finally
        {
            var elapsedMs = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
            RetentionDiagnostics.EnforcementDuration.Record(elapsedMs);
        }
    }

    private async Task CheckExpiringDataAsync(
        IRetentionEnforcer enforcer,
        IServiceProvider scopedProvider,
        CancellationToken cancellationToken)
    {
        try
        {
            var alertWindow = TimeSpan.FromDays(_options.AlertBeforeExpirationDays);
            var expiringResult = await enforcer.GetExpiringDataAsync(alertWindow, cancellationToken)
                .ConfigureAwait(false);

            expiringResult.Match(
                Right: expiringData =>
                {
                    if (expiringData.Count > 0)
                    {
                        _logger.RetentionExpiringDataFound(expiringData.Count, _options.AlertBeforeExpirationDays);

                        // Publish individual expiring data notifications if Encina is available
                        if (_options.PublishNotifications)
                        {
                            _ = PublishExpiringNotificationsAsync(
                                expiringData, scopedProvider, cancellationToken);
                        }
                    }
                },
                Left: error =>
                {
                    _logger.RetentionExpiringDataCheckError(error.Message);
                });
        }
        catch (Exception ex)
        {
            // Expiring data check failure should not prevent enforcement
            _logger.RetentionExpiringDataCheckFailed(ex);
        }
    }

    private async Task PublishExpiringNotificationsAsync(
        IReadOnlyList<Model.ExpiringData> expiringData,
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
            var now = DateTimeOffset.UtcNow;

            foreach (var data in expiringData)
            {
                var notification = new DataExpiringNotification(
                    data.EntityId,
                    data.DataCategory,
                    data.ExpiresAtUtc,
                    data.DaysUntilExpiration,
                    now);

                await encina.Publish(notification, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            // Notification publishing should never fail the enforcement cycle
            _logger.RetentionExpiringNotificationsFailed(ex);
        }
    }

}
