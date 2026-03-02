using System.Diagnostics;

using Encina.Compliance.DataSubjectRights;
using Encina.Compliance.Retention.Diagnostics;
using Encina.Compliance.Retention.Model;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using static LanguageExt.Prelude;

namespace Encina.Compliance.Retention;

/// <summary>
/// Default implementation of <see cref="IRetentionEnforcer"/> that orchestrates automated
/// retention enforcement with optional data erasure delegation.
/// </summary>
/// <remarks>
/// <para>
/// The enforcer executes the following flow for each enforcement cycle:
/// <list type="number">
/// <item><description>Query expired records via <see cref="IRetentionRecordStore.GetExpiredRecordsAsync"/>.</description></item>
/// <item><description>For each record, check legal hold status via <see cref="ILegalHoldStore.IsUnderHoldAsync"/>.</description></item>
/// <item><description>For non-held records, delegate deletion to <see cref="IDataErasureExecutor"/> (if registered).</description></item>
/// <item><description>Update record statuses, record audit entries, publish notifications.</description></item>
/// <item><description>Return a comprehensive <see cref="DeletionResult"/> with all outcomes.</description></item>
/// </list>
/// </para>
/// <para>
/// If <see cref="IDataErasureExecutor"/> is not registered in the service container (i.e.,
/// <c>Encina.Compliance.DataSubjectRights</c> is not configured), the enforcer operates in
/// <b>degraded mode</b>: records are marked as <see cref="RetentionStatus.Deleted"/> but
/// no physical data deletion occurs. A warning is logged indicating that DSR integration
/// is not configured.
/// </para>
/// <para>
/// Per GDPR Article 5(1)(e), personal data shall be kept for no longer than is necessary.
/// Per Article 17(3)(e), legal holds exempt data from deletion during litigation.
/// </para>
/// </remarks>
public sealed class DefaultRetentionEnforcer : IRetentionEnforcer
{
    private readonly IRetentionRecordStore _recordStore;
    private readonly ILegalHoldStore _legalHoldStore;
    private readonly IRetentionAuditStore _auditStore;
    private readonly RetentionOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DefaultRetentionEnforcer> _logger;
    private readonly IDataErasureExecutor? _erasureExecutor;
    private readonly IEncina? _encina;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultRetentionEnforcer"/> class.
    /// </summary>
    /// <param name="recordStore">Store for retention record queries and updates.</param>
    /// <param name="legalHoldStore">Store for legal hold status checks.</param>
    /// <param name="auditStore">Store for recording audit trail entries.</param>
    /// <param name="options">Configuration options for the retention module.</param>
    /// <param name="serviceProvider">Service provider for resolving optional dependencies.</param>
    /// <param name="timeProvider">Time provider for deterministic time-based operations.</param>
    /// <param name="logger">Logger for diagnostic messages.</param>
    public DefaultRetentionEnforcer(
        IRetentionRecordStore recordStore,
        ILegalHoldStore legalHoldStore,
        IRetentionAuditStore auditStore,
        IOptions<RetentionOptions> options,
        IServiceProvider serviceProvider,
        TimeProvider timeProvider,
        ILogger<DefaultRetentionEnforcer> logger)
    {
        ArgumentNullException.ThrowIfNull(recordStore);
        ArgumentNullException.ThrowIfNull(legalHoldStore);
        ArgumentNullException.ThrowIfNull(auditStore);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _recordStore = recordStore;
        _legalHoldStore = legalHoldStore;
        _auditStore = auditStore;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;

        // Optional dependencies resolved via IServiceProvider
        _erasureExecutor = serviceProvider.GetService<IDataErasureExecutor>();
        _encina = serviceProvider.GetService<IEncina>();

        if (_erasureExecutor is null)
        {
            _logger.RetentionErasureExecutorMissing();
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, DeletionResult>> EnforceRetentionAsync(
        CancellationToken cancellationToken = default)
    {
        using var activity = RetentionDiagnostics.StartEnforcementCycle();
        var startTimestamp = Stopwatch.GetTimestamp();

        _logger.RetentionEnforcementCycleStarting();

        var expiredResult = await _recordStore.GetExpiredRecordsAsync(cancellationToken);

        return await expiredResult.MatchAsync(
            RightAsync: async records =>
            {
                if (records.Count == 0)
                {
                    _logger.RetentionNoExpiredRecords();
                    RetentionDiagnostics.RecordCompleted(activity, records.Count);
                    RetentionDiagnostics.EnforcementDuration.Record(Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds);
                    RetentionDiagnostics.EnforcementCyclesTotal.Add(1,
                        new KeyValuePair<string, object?>(RetentionDiagnostics.TagOutcome, "completed"));
                    return Right<EncinaError, DeletionResult>(CreateEmptyResult());
                }

                _logger.RetentionExpiredRecordsFound(records.Count);

                var details = new List<DeletionDetail>();
                var deleted = 0;
                var held = 0;
                var failed = 0;

                foreach (var record in records)
                {
                    var detail = await ProcessExpiredRecordAsync(record, cancellationToken);
                    details.Add(detail);

                    switch (detail.Outcome)
                    {
                        case DeletionOutcome.Deleted:
                            deleted++;
                            break;
                        case DeletionOutcome.HeldByLegalHold:
                            held++;
                            break;
                        case DeletionOutcome.Failed:
                            failed++;
                            break;
                    }
                }

                var result = new DeletionResult
                {
                    TotalRecordsEvaluated = records.Count,
                    RecordsDeleted = deleted,
                    RecordsRetained = 0,
                    RecordsFailed = failed,
                    RecordsUnderHold = held,
                    Details = details.AsReadOnly(),
                    ExecutedAtUtc = _timeProvider.GetUtcNow()
                };

                // Record audit entry for enforcement cycle
                if (_options.TrackAuditTrail)
                {
                    await RecordAuditAsync(
                        "EnforcementExecuted",
                        detail: $"Evaluated {result.TotalRecordsEvaluated} records: " +
                                $"{deleted} deleted, {held} under legal hold, {failed} failed",
                        cancellationToken: cancellationToken);
                }

                // Publish enforcement completed notification
                await PublishNotificationAsync(
                    new RetentionEnforcementCompletedNotification(result, _timeProvider.GetUtcNow()),
                    cancellationToken);

                _logger.RetentionEnforcementCycleCompleted(deleted, failed, held);

                RetentionDiagnostics.RecordCompleted(activity, records.Count);
                RetentionDiagnostics.EnforcementDuration.Record(Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds);
                RetentionDiagnostics.EnforcementCyclesTotal.Add(1,
                    new KeyValuePair<string, object?>(RetentionDiagnostics.TagOutcome, "completed"));

                return Right<EncinaError, DeletionResult>(result);
            },
            Left: error =>
            {
                _logger.RetentionExpiredRecordsRetrievalFailed(error.Message);
                RetentionDiagnostics.RecordFailed(activity, error.Message);
                RetentionDiagnostics.EnforcementDuration.Record(Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds);
                RetentionDiagnostics.EnforcementCyclesTotal.Add(1,
                    new KeyValuePair<string, object?>(RetentionDiagnostics.TagOutcome, "failed"));
                return Left<EncinaError, DeletionResult>(
                    RetentionErrors.EnforcementFailed("Failed to retrieve expired records"));
            });
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<ExpiringData>>> GetExpiringDataAsync(
        TimeSpan within,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var expiringResult = await _recordStore.GetExpiringWithinAsync(within, cancellationToken);

        return expiringResult.Match(
            Right: records =>
            {
                var result = records
                    .Select(r => new ExpiringData
                    {
                        EntityId = r.EntityId,
                        DataCategory = r.DataCategory,
                        ExpiresAtUtc = r.ExpiresAtUtc,
                        PolicyId = r.PolicyId,
                        DaysUntilExpiration = (int)(r.ExpiresAtUtc - now).TotalDays
                    })
                    .ToList()
                    .AsReadOnly();

                _logger.RetentionExpiringDataChecked(result.Count, within);

                return Right<EncinaError, IReadOnlyList<ExpiringData>>(result);
            },
            Left: error => Left<EncinaError, IReadOnlyList<ExpiringData>>(error));
    }

    private async ValueTask<DeletionDetail> ProcessExpiredRecordAsync(
        RetentionRecord record,
        CancellationToken cancellationToken)
    {
        using var deletionActivity = RetentionDiagnostics.StartRecordDeletion(record.EntityId, record.DataCategory);

        // Step 1: Check legal hold status
        var holdResult = await _legalHoldStore.IsUnderHoldAsync(record.EntityId, cancellationToken);

        var isHeld = holdResult.Match(
            Right: held => held,
            Left: _ => false); // If hold check fails, assume not held and attempt deletion

        if (isHeld)
        {
            // Update record status to UnderLegalHold
            await _recordStore.UpdateStatusAsync(record.Id, RetentionStatus.UnderLegalHold, cancellationToken);

            _logger.RetentionDeletionSkippedLegalHold(record.EntityId);

            if (_options.TrackAuditTrail)
            {
                await RecordAuditAsync(
                    "DeletionSkippedLegalHold",
                    entityId: record.EntityId,
                    dataCategory: record.DataCategory,
                    detail: "Deletion suspended due to active legal hold (Article 17(3)(e))",
                    cancellationToken: cancellationToken);
            }

            RetentionDiagnostics.RecordHeld(deletionActivity, record.EntityId);
            RetentionDiagnostics.RecordsHeldTotal.Add(1,
                new KeyValuePair<string, object?>(RetentionDiagnostics.TagEntityId, record.EntityId));

            return new DeletionDetail
            {
                EntityId = record.EntityId,
                DataCategory = record.DataCategory,
                Outcome = DeletionOutcome.HeldByLegalHold,
                Reason = "Under active legal hold (Article 17(3)(e))"
            };
        }

        // Step 2: Attempt data erasure (if executor is registered)
        if (_erasureExecutor is not null)
        {
            try
            {
                var scope = new ErasureScope
                {
                    Reason = ErasureReason.NoLongerNecessary
                };

                var erasureResult = await _erasureExecutor.EraseAsync(record.EntityId, scope, cancellationToken);

                var erasureFailed = erasureResult.Match(
                    Right: _ => false,
                    Left: _ => true);

                if (erasureFailed)
                {
                    var errorMsg = erasureResult.Match(
                        Right: _ => string.Empty,
                        Left: e => e.Message);

                    _logger.RetentionErasureFailed(record.EntityId, errorMsg);

                    if (_options.TrackAuditTrail)
                    {
                        await RecordAuditAsync(
                            "DeletionFailed",
                            entityId: record.EntityId,
                            dataCategory: record.DataCategory,
                            detail: $"Erasure failed: {errorMsg}",
                            cancellationToken: cancellationToken);
                    }

                    RetentionDiagnostics.RecordFailed(deletionActivity, errorMsg);
                    RetentionDiagnostics.RecordsFailedTotal.Add(1,
                        new KeyValuePair<string, object?>(RetentionDiagnostics.TagFailureReason, errorMsg));

                    return new DeletionDetail
                    {
                        EntityId = record.EntityId,
                        DataCategory = record.DataCategory,
                        Outcome = DeletionOutcome.Failed,
                        Reason = errorMsg
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.RetentionErasureException(record.EntityId, ex);

                if (_options.TrackAuditTrail)
                {
                    await RecordAuditAsync(
                        "DeletionFailed",
                        entityId: record.EntityId,
                        dataCategory: record.DataCategory,
                        detail: $"Exception during erasure: {ex.Message}",
                        cancellationToken: cancellationToken);
                }

                RetentionDiagnostics.RecordFailed(deletionActivity, ex.Message);
                RetentionDiagnostics.RecordsFailedTotal.Add(1,
                    new KeyValuePair<string, object?>(RetentionDiagnostics.TagFailureReason, ex.Message));

                return new DeletionDetail
                {
                    EntityId = record.EntityId,
                    DataCategory = record.DataCategory,
                    Outcome = DeletionOutcome.Failed,
                    Reason = ex.Message
                };
            }
        }
        else
        {
            _logger.RetentionNoErasureExecutor(record.EntityId);
        }

        // Step 3: Update record status to Deleted
        await _recordStore.UpdateStatusAsync(record.Id, RetentionStatus.Deleted, cancellationToken);

        var deletedAtUtc = _timeProvider.GetUtcNow();

        // Step 4: Record audit entry
        if (_options.TrackAuditTrail)
        {
            await RecordAuditAsync(
                "RecordDeleted",
                entityId: record.EntityId,
                dataCategory: record.DataCategory,
                detail: _erasureExecutor is not null
                    ? "Data erased and record marked as deleted"
                    : "Record marked as deleted (no erasure executor registered)",
                cancellationToken: cancellationToken);
        }

        // Step 5: Publish deletion notification
        await PublishNotificationAsync(
            new DataDeletedNotification(record.EntityId, record.DataCategory, deletedAtUtc, record.PolicyId),
            cancellationToken);

        RetentionDiagnostics.RecordCompleted(deletionActivity);
        RetentionDiagnostics.RecordsDeletedTotal.Add(1,
            new KeyValuePair<string, object?>(RetentionDiagnostics.TagOutcome, "completed"));

        return new DeletionDetail
        {
            EntityId = record.EntityId,
            DataCategory = record.DataCategory,
            Outcome = DeletionOutcome.Deleted
        };
    }

    private DeletionResult CreateEmptyResult() =>
        new()
        {
            TotalRecordsEvaluated = 0,
            RecordsDeleted = 0,
            RecordsRetained = 0,
            RecordsFailed = 0,
            RecordsUnderHold = 0,
            Details = [],
            ExecutedAtUtc = _timeProvider.GetUtcNow()
        };

    private async ValueTask RecordAuditAsync(
        string action,
        string? entityId = null,
        string? dataCategory = null,
        string? detail = null,
        string? performedByUserId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entry = RetentionAuditEntry.Create(
                action: action,
                entityId: entityId,
                dataCategory: dataCategory,
                detail: detail,
                performedByUserId: performedByUserId);

            await _auditStore.RecordAsync(entry, cancellationToken);
        }
        catch (Exception ex)
        {
            // Audit recording should never fail the main operation
            _logger.RetentionAuditEntryFailed(action, ex);
        }
    }

    private async ValueTask PublishNotificationAsync<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken)
        where TNotification : INotification
    {
        if (_encina is null || !_options.PublishNotifications)
        {
            return;
        }

        try
        {
            await _encina.Publish(notification, cancellationToken);
        }
        catch (Exception ex)
        {
            // Notification publishing should never fail the main operation
            _logger.RetentionNotificationFailed(typeof(TNotification).Name, ex);
        }
    }
}
