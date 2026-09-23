using System.Diagnostics;

using Encina.Compliance.Retention.Abstractions;
using Encina.Compliance.Retention.Diagnostics;
using Encina.Compliance.Retention.Model;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Compliance.Retention;

/// <summary>
/// Background hosted service that periodically enforces retention policies by identifying
/// expired data and delegating its erasure to <see cref="IRetentionDataEraser"/> (optional).
/// </summary>
/// <remarks>
/// <para>
/// The service runs enforcement cycles at a configurable interval (default: 60 minutes),
/// each cycle performing the following steps:
/// <list type="number">
/// <item><description>Create a new <see cref="IServiceScope"/> to resolve scoped dependencies.</description></item>
/// <item><description>Resolve <see cref="IRetentionRecordService"/> and <see cref="ILegalHoldService"/> from the scoped service provider.</description></item>
/// <item><description>Query for expired records via <see cref="IRetentionRecordService.GetExpiredRecordsAsync"/>
/// (active records past their expiry, and expired records left over by an earlier failed cycle).</description></item>
/// <item><description>For each record, check legal holds via <see cref="ILegalHoldService.HasActiveHoldsAsync"/>.
/// If the hold status cannot be determined, the record is skipped, logged and counted as failed (fail closed).</description></item>
/// <item><description>For non-held records, mark the record expired, re-check legal holds, ask
/// <see cref="IRetentionDataEraser"/> to erase the data of the record's category for the record's entity
/// (and nothing else) and, only after a successful erasure, mark it deleted.
/// Any failed step is logged and counted as failed, never as deleted, and the record is retried on the next cycle.
/// Without a registered <see cref="IRetentionDataEraser"/> nothing is erased: records stay expired, are counted
/// as failed and a warning is logged once per cycle.</description></item>
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
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetentionEnforcementService"/> class.
    /// </summary>
    /// <param name="scopeFactory">Factory for creating service scopes to resolve scoped dependencies.</param>
    /// <param name="options">Retention configuration options controlling enforcement behavior.</param>
    /// <param name="logger">Logger for diagnostic messages.</param>
    /// <param name="timeProvider">
    /// Source of the current time for the expiry alert window. Defaults to <see cref="TimeProvider.System"/>.
    /// </param>
    public RetentionEnforcementService(
        IServiceScopeFactory scopeFactory,
        IOptions<RetentionOptions> options,
        ILogger<RetentionEnforcementService> logger,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// The clock in use: the injected <see cref="TimeProvider"/>, or <see cref="TimeProvider.System"/>
    /// when none was supplied. Internal so that tests can verify the default.
    /// </summary>
    internal TimeProvider Clock => _timeProvider;

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

    /// <summary>
    /// Runs a single enforcement cycle. Internal so that tests can drive one cycle deterministically
    /// instead of waiting on the periodic timer.
    /// </summary>
    /// <param name="cancellationToken">Token that stops the cycle.</param>
    internal async Task ExecuteEnforcementCycleAsync(CancellationToken cancellationToken)
    {
        var startTimestamp = Stopwatch.GetTimestamp();
        using var activity = RetentionDiagnostics.StartEnforcementCycle();

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();

            var recordService = scope.ServiceProvider.GetRequiredService<IRetentionRecordService>();
            var legalHoldService = scope.ServiceProvider.GetRequiredService<ILegalHoldService>();
            var dataEraser = scope.ServiceProvider.GetService<IRetentionDataEraser>();

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
            var dataEraserMissingLogged = false;

            foreach (var record in expiredRecords)
            {
                RecordOutcome outcome;

                try
                {
                    outcome = await ProcessRecordAsync(
                        record, recordService, legalHoldService, dataEraser, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
                {
                    // Only the cycle's own cancellation stops the cycle. Any other exception, including a
                    // cancellation raised inside a dependency (e.g. an HTTP timeout during erasure), fails
                    // this record only and the cycle moves on to the next one.
                    _logger.RetentionEnforcementCycleFailed(ex);
                    outcome = RecordOutcome.Failed;
                }

                switch (outcome)
                {
                    case RecordOutcome.Deleted:
                        recordsDeleted++;
                        break;
                    case RecordOutcome.Held:
                        recordsUnderHold++;
                        break;
                    case RecordOutcome.ErasureUnavailable:
                        if (!dataEraserMissingLogged)
                        {
                            _logger.RetentionDataEraserMissing();
                            dataEraserMissingLogged = true;
                        }

                        recordsFailed++;
                        break;
                    default:
                        recordsFailed++;
                        break;
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

    /// <summary>
    /// Moves one expired record through <c>Active → Expired → Deleted</c>, erasing its data in between.
    /// </summary>
    /// <remarks>
    /// Every step checks its <c>Either</c> result; the first <c>Left</c> stops the record and reports it
    /// as <see cref="RecordOutcome.Failed"/>, leaving it in a state that the next cycle picks up again:
    /// <list type="bullet">
    /// <item><description>Legal-hold lookup error (before marking expired or right before erasing): the record
    /// is skipped without erasure (fail closed).</description></item>
    /// <item><description>No <see cref="IRetentionDataEraser"/> registered, erasure or <c>MarkDeleted</c> error:
    /// the record stays <see cref="RetentionStatus.Expired"/> and is returned again by
    /// <see cref="IRetentionRecordService.GetExpiredRecordsAsync"/>.</description></item>
    /// </list>
    /// A hold found by the re-check right before erasure moves the record to
    /// <see cref="RetentionStatus.UnderLegalHold"/> and nothing is erased.
    /// Erasure is scoped to the record: the eraser receives the record's entity and data category, so
    /// the data of the entity's other categories, still within their own retention periods, is untouched.
    /// Records that reached <see cref="RetentionStatus.Deleted"/> are never returned again, so their
    /// data is not erased twice.
    /// </remarks>
    private async ValueTask<RecordOutcome> ProcessRecordAsync(
        ReadModels.RetentionRecordReadModel record,
        IRetentionRecordService recordService,
        ILegalHoldService legalHoldService,
        IRetentionDataEraser? dataEraser,
        CancellationToken cancellationToken)
    {
        // Legal hold check — fail closed: if the hold status is unknown, nothing is erased.
        var holdStatus = await CheckLegalHoldAsync(record, legalHoldService, cancellationToken)
            .ConfigureAwait(false);

        if (holdStatus == HoldStatus.Unknown)
        {
            return RecordOutcome.Failed;
        }

        if (holdStatus == HoldStatus.Held)
        {
            return await HoldRecordAsync(record, recordService, cancellationToken).ConfigureAwait(false);
        }

        // Active → Expired before erasure, so that a failed erasure leaves the record Expired
        // and the next cycle retries it.
        if (record.Status == RetentionStatus.Active)
        {
            var expiredResult = await recordService
                .MarkExpiredAsync(record.Id, cancellationToken)
                .ConfigureAwait(false);

            if (expiredResult.IsLeft)
            {
                _logger.RetentionEnforcementTransitionFailed(record.Id, "MarkExpired", ((EncinaError)expiredResult).Message);
                return RecordOutcome.Failed;
            }
        }

        // Without an eraser nothing can be erased, so the record must never reach Deleted:
        // it stays Expired, is counted as failed and is retried once an eraser is registered.
        if (dataEraser is null)
        {
            return RecordOutcome.ErasureUnavailable;
        }

        // Re-check the hold immediately before erasing: a hold placed after the first check (while
        // the record was being marked expired) must still prevent erasure. This narrows the window
        // but does not close it; closing it fully needs a per-entity lock shared with
        // ILegalHoldService.PlaceHoldAsync, which is tracked separately.
        holdStatus = await CheckLegalHoldAsync(record, legalHoldService, cancellationToken)
            .ConfigureAwait(false);

        if (holdStatus == HoldStatus.Unknown)
        {
            return RecordOutcome.Failed;
        }

        if (holdStatus == HoldStatus.Held)
        {
            return await HoldRecordAsync(record, recordService, cancellationToken).ConfigureAwait(false);
        }

        // Erase only what this record governs: its category of data for its entity. Other records of
        // the same entity (other categories, other retention periods) are not affected.
        var target = new RetentionErasureTarget
        {
            RecordId = record.Id,
            EntityId = record.EntityId,
            DataCategory = record.DataCategory,
            ExpiresAtUtc = record.ExpiresAtUtc,
            TenantId = record.TenantId,
            ModuleId = record.ModuleId
        };

        var erasureResult = await dataEraser
            .EraseAsync(target, cancellationToken)
            .ConfigureAwait(false);

        if (erasureResult.IsLeft)
        {
            _logger.RetentionErasureFailed(record.EntityId, record.DataCategory, ((EncinaError)erasureResult).Message);
            return RecordOutcome.Failed;
        }

        // Expired → Deleted, only after a successful erasure.
        var deletedResult = await recordService
            .MarkDeletedAsync(record.Id, cancellationToken)
            .ConfigureAwait(false);

        if (deletedResult.IsLeft)
        {
            _logger.RetentionEnforcementTransitionFailed(record.Id, "MarkDeleted", ((EncinaError)deletedResult).Message);
            return RecordOutcome.Failed;
        }

        return RecordOutcome.Deleted;
    }

    /// <summary>
    /// Asks the legal hold service whether the record's entity is held, failing closed: an error
    /// result or an exception (other than the cycle's own cancellation) yields <see cref="HoldStatus.Unknown"/>.
    /// </summary>
    private async ValueTask<HoldStatus> CheckLegalHoldAsync(
        ReadModels.RetentionRecordReadModel record,
        ILegalHoldService legalHoldService,
        CancellationToken cancellationToken)
    {
        Either<EncinaError, bool> hasHoldsResult;

        try
        {
            hasHoldsResult = await legalHoldService
                .HasActiveHoldsAsync(record.EntityId, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.RetentionLegalHoldCheckFailed(record.Id, record.EntityId, ex.Message);
            return HoldStatus.Unknown;
        }

        if (hasHoldsResult.IsLeft)
        {
            _logger.RetentionLegalHoldCheckFailed(record.Id, record.EntityId, ((EncinaError)hasHoldsResult).Message);
            return HoldStatus.Unknown;
        }

        return (bool)hasHoldsResult ? HoldStatus.Held : HoldStatus.None;
    }

    /// <summary>
    /// Moves a record whose entity is under legal hold to <see cref="RetentionStatus.UnderLegalHold"/>.
    /// </summary>
    private async ValueTask<RecordOutcome> HoldRecordAsync(
        ReadModels.RetentionRecordReadModel record,
        IRetentionRecordService recordService,
        CancellationToken cancellationToken)
    {
        _logger.RetentionDeletionSkippedLegalHold(record.EntityId);

        var holdResult = await recordService
            .HoldRecordAsync(record.Id, Guid.Empty, cancellationToken)
            .ConfigureAwait(false);

        if (holdResult.IsLeft)
        {
            _logger.RetentionEnforcementTransitionFailed(record.Id, "HoldRecord", ((EncinaError)holdResult).Message);
            return RecordOutcome.Failed;
        }

        return RecordOutcome.Held;
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
            var now = _timeProvider.GetUtcNow();
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

    /// <summary>
    /// Result of processing one expired record in an enforcement cycle.
    /// </summary>
    private enum RecordOutcome
    {
        /// <summary>The data was erased and the record reached <see cref="RetentionStatus.Deleted"/>.</summary>
        Deleted,

        /// <summary>The entity is under legal hold; nothing was erased.</summary>
        Held,

        /// <summary>A step failed; nothing was counted as deleted and the record is retried next cycle.</summary>
        Failed,

        /// <summary>
        /// No <see cref="IRetentionDataEraser"/> is registered: the record was left
        /// <see cref="RetentionStatus.Expired"/> without erasure and is counted as failed.
        /// </summary>
        ErasureUnavailable
    }

    /// <summary>
    /// Result of a legal hold lookup for one record.
    /// </summary>
    private enum HoldStatus
    {
        /// <summary>No active hold on the entity.</summary>
        None,

        /// <summary>The entity is under at least one active hold.</summary>
        Held,

        /// <summary>The lookup failed; treated as held for erasure purposes (fail closed).</summary>
        Unknown
    }
}
