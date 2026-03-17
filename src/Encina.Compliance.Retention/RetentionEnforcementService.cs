using System.Diagnostics;

using Encina.Compliance.Retention.Abstractions;
using Encina.Compliance.Retention.Diagnostics;
using Encina.Compliance.Retention.Model;
using Encina.Compliance.DataSubjectRights;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Compliance.Retention;

/// <summary>
/// Background hosted service that periodically enforces retention policies by identifying
/// expired data and delegating deletion to <see cref="IDataErasureExecutor"/> (optional).
/// </summary>
/// <remarks>
/// <para>
/// The service runs enforcement cycles at a configurable interval (default: 60 minutes),
/// each cycle performing the following steps:
/// <list type="number">
/// <item><description>Create a new <see cref="IServiceScope"/> to resolve scoped dependencies.</description></item>
/// <item><description>Resolve <see cref="IRetentionRecordService"/> and <see cref="ILegalHoldService"/> from the scoped service provider.</description></item>
/// <item><description>Query for expired records via <see cref="IRetentionRecordService.GetExpiredRecordsAsync"/>.</description></item>
/// <item><description>For each record, check legal holds via <see cref="ILegalHoldService.HasActiveHoldsAsync"/>.</description></item>
/// <item><description>For non-held records, delegate deletion to <see cref="IDataErasureExecutor"/> (if registered) and mark as deleted.</description></item>
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
/// When disabled, the service starts but does not execute enforcement cycles.
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

            var recordService = scope.ServiceProvider.GetRequiredService<IRetentionRecordService>();
            var legalHoldService = scope.ServiceProvider.GetRequiredService<ILegalHoldService>();
            var erasureExecutor = scope.ServiceProvider.GetService<IDataErasureExecutor>();

            _logger.RetentionEnforcementCycleStarting();

            // Step 1: Get expired records
            var expiredResult = await recordService
                .GetExpiredRecordsAsync(cancellationToken)
                .ConfigureAwait(false);

            if (expiredResult.IsLeft)
            {
                var error = (EncinaError)expiredResult;
                _logger.RetentionEnforcementCycleFailed(new InvalidOperationException(error.Message));
                RetentionDiagnostics.RecordFailed(activity, error.Message);
                RetentionDiagnostics.EnforcementCyclesTotal.Add(1,
                    new KeyValuePair<string, object?>(RetentionDiagnostics.TagOutcome, "failed"));
                return;
            }

            var expiredRecords = expiredResult.Match(
                Right: r => r,
                Left: _ => (IReadOnlyList<ReadModels.RetentionRecordReadModel>)[]);

            if (expiredRecords.Count == 0)
            {
                _logger.RetentionEnforcementCycleEmpty();
                RetentionDiagnostics.RecordCompleted(activity);
                RetentionDiagnostics.EnforcementCyclesTotal.Add(1,
                    new KeyValuePair<string, object?>(RetentionDiagnostics.TagOutcome, "completed"));
                return;
            }

            // Step 2: Process each expired record
            var recordsDeleted = 0;
            var recordsFailed = 0;
            var recordsUnderHold = 0;

            foreach (var record in expiredRecords)
            {
                try
                {
                    // Check for active legal holds
                    var hasHoldsResult = await legalHoldService
                        .HasActiveHoldsAsync(record.EntityId, cancellationToken)
                        .ConfigureAwait(false);

                    var hasHolds = hasHoldsResult.Match(Right: h => h, Left: _ => false);

                    if (hasHolds)
                    {
                        // Mark as held — the record service will transition status
                        await recordService.HoldRecordAsync(record.Id, Guid.Empty, cancellationToken)
                            .ConfigureAwait(false);
                        recordsUnderHold++;
                        continue;
                    }

                    // Execute physical deletion if erasure executor is available
                    if (erasureExecutor is not null)
                    {
                        var erasureScope = new ErasureScope
                        {
                            Reason = ErasureReason.NoLongerNecessary
                        };

                        await erasureExecutor.EraseAsync(
                            record.EntityId, erasureScope, cancellationToken)
                            .ConfigureAwait(false);
                    }

                    // Mark as deleted in the retention record aggregate
                    await recordService.MarkDeletedAsync(record.Id, cancellationToken)
                        .ConfigureAwait(false);
                    recordsDeleted++;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    recordsFailed++;
                    _logger.RetentionEnforcementCycleFailed(ex);
                }
            }

            var totalEvaluated = recordsDeleted + recordsFailed + recordsUnderHold;

            _logger.RetentionEnforcementCycleCompleted(recordsDeleted, recordsFailed, recordsUnderHold);

            RetentionDiagnostics.RecordsDeletedTotal.Add(
                recordsDeleted,
                new KeyValuePair<string, object?>(RetentionDiagnostics.TagOutcome, "completed"));
            RetentionDiagnostics.RecordsHeldTotal.Add(
                recordsUnderHold,
                new KeyValuePair<string, object?>(RetentionDiagnostics.TagOutcome, "held"));
            RetentionDiagnostics.RecordsFailedTotal.Add(
                recordsFailed,
                new KeyValuePair<string, object?>(RetentionDiagnostics.TagFailureReason, "enforcement"));

            RetentionDiagnostics.RecordCompleted(activity, totalEvaluated);
            RetentionDiagnostics.EnforcementCyclesTotal.Add(1,
                new KeyValuePair<string, object?>(RetentionDiagnostics.TagOutcome, "completed"));

            // Step 3: Check for expiring data and publish alerts
            await CheckExpiringDataAsync(recordService, scope.ServiceProvider, cancellationToken)
                .ConfigureAwait(false);
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
        IRetentionRecordService recordService,
        IServiceProvider scopedProvider,
        CancellationToken cancellationToken)
    {
        if (!_options.PublishNotifications)
        {
            return;
        }

        try
        {
            var encina = scopedProvider.GetService<IEncina>();
            if (encina is null)
            {
                return;
            }

            // Query records by active status — the read model contains ExpiresAtUtc for alert window checking
            var activeResult = await recordService
                .GetRecordsByStatusAsync(RetentionStatus.Active, cancellationToken)
                .ConfigureAwait(false);

            if (activeResult.IsLeft)
            {
                return;
            }

            var activeRecords = activeResult.Match(
                Right: r => r,
                Left: _ => (IReadOnlyList<ReadModels.RetentionRecordReadModel>)[]);

            var alertWindow = TimeSpan.FromDays(_options.AlertBeforeExpirationDays);
            var now = DateTimeOffset.UtcNow;
            var expiringCount = 0;

            foreach (var record in activeRecords)
            {
                var daysUntilExpiration = (record.ExpiresAtUtc - now).TotalDays;
                if (daysUntilExpiration is > 0 and <= double.MaxValue && daysUntilExpiration <= alertWindow.TotalDays)
                {
                    var notification = new DataExpiringNotification(
                        record.EntityId,
                        record.DataCategory,
                        record.ExpiresAtUtc,
                        (int)Math.Ceiling(daysUntilExpiration),
                        now);

                    await encina.Publish(notification, cancellationToken).ConfigureAwait(false);
                    expiringCount++;
                }
            }

            if (expiringCount > 0)
            {
                _logger.RetentionExpiringDataFound(expiringCount, _options.AlertBeforeExpirationDays);
            }
        }
        catch (Exception ex)
        {
            // Expiring data check failure should not prevent enforcement
            _logger.RetentionExpiringDataCheckFailed(ex);
        }
    }
}
