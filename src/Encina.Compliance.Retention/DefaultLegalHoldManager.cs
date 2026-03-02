using System.Diagnostics;

using Encina.Compliance.Retention.Diagnostics;
using Encina.Compliance.Retention.Model;

using LanguageExt;

using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace Encina.Compliance.Retention;

/// <summary>
/// Default implementation of <see cref="ILegalHoldManager"/> that coordinates legal hold
/// lifecycle across retention stores with cascading status updates.
/// </summary>
/// <remarks>
/// <para>
/// The legal hold manager provides the application-level logic for managing legal holds:
/// <list type="bullet">
/// <item><description><b>Apply hold</b>: Creates the hold record, updates all matching retention records
/// for the entity to <see cref="RetentionStatus.UnderLegalHold"/>, records audit entries,
/// and publishes a <see cref="LegalHoldAppliedNotification"/>.</description></item>
/// <item><description><b>Release hold</b>: Releases the hold record, recalculates retention record
/// statuses (expired or active based on current time), records audit entries,
/// and publishes a <see cref="LegalHoldReleasedNotification"/>.</description></item>
/// </list>
/// </para>
/// <para>
/// Per GDPR Article 17(3)(e), the right to erasure does not apply when processing is
/// necessary for the establishment, exercise, or defence of legal claims. This manager
/// implements that exemption by suspending automatic deletion for entities under hold.
/// </para>
/// <para>
/// When multiple holds exist for the same entity, the retention record status remains
/// <see cref="RetentionStatus.UnderLegalHold"/> until <b>all</b> holds are released.
/// </para>
/// </remarks>
public sealed class DefaultLegalHoldManager : ILegalHoldManager
{
    private readonly ILegalHoldStore _holdStore;
    private readonly IRetentionRecordStore _recordStore;
    private readonly IRetentionAuditStore _auditStore;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DefaultLegalHoldManager> _logger;
    private readonly IEncina? _encina;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultLegalHoldManager"/> class.
    /// </summary>
    /// <param name="holdStore">Store for legal hold persistence.</param>
    /// <param name="recordStore">Store for retention record queries and updates.</param>
    /// <param name="auditStore">Store for recording audit trail entries.</param>
    /// <param name="timeProvider">Time provider for deterministic time-based operations.</param>
    /// <param name="logger">Logger for diagnostic messages.</param>
    /// <param name="encina">Optional Encina instance for publishing domain notifications.</param>
    public DefaultLegalHoldManager(
        ILegalHoldStore holdStore,
        IRetentionRecordStore recordStore,
        IRetentionAuditStore auditStore,
        TimeProvider timeProvider,
        ILogger<DefaultLegalHoldManager> logger,
        IEncina? encina = null)
    {
        ArgumentNullException.ThrowIfNull(holdStore);
        ArgumentNullException.ThrowIfNull(recordStore);
        ArgumentNullException.ThrowIfNull(auditStore);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _holdStore = holdStore;
        _recordStore = recordStore;
        _auditStore = auditStore;
        _timeProvider = timeProvider;
        _logger = logger;
        _encina = encina;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> ApplyHoldAsync(
        string entityId,
        LegalHold hold,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);
        ArgumentNullException.ThrowIfNull(hold);

        using var activity = RetentionDiagnostics.StartLegalHoldOperation(hold.Id, entityId);

        _logger.LegalHoldApplying(hold.Id, entityId, hold.Reason);

        // Check if entity already has an active hold
        var existingHoldResult = await _holdStore.IsUnderHoldAsync(entityId, cancellationToken);
        var alreadyHeld = existingHoldResult.Match(Right: held => held, Left: _ => false);

        if (alreadyHeld)
        {
            _logger.LegalHoldAlreadyActive(entityId);
            RetentionDiagnostics.RecordFailed(activity, "already_active");
            return Left<EncinaError, Unit>(RetentionErrors.HoldAlreadyActive(entityId));
        }

        // Step 1: Persist the legal hold record
        var createResult = await _holdStore.CreateAsync(hold, cancellationToken);
        if (createResult.IsLeft)
        {
            RetentionDiagnostics.RecordFailed(activity, "create_failed");
            return createResult;
        }

        // Step 2: Update all retention records for this entity to UnderLegalHold
        await CascadeHoldStatusAsync(entityId, RetentionStatus.UnderLegalHold, cancellationToken);

        // Step 3: Record audit entry
        await RecordAuditAsync(
            "LegalHoldApplied",
            entityId: entityId,
            detail: $"Legal hold '{hold.Id}' applied: {hold.Reason}",
            performedByUserId: hold.AppliedByUserId,
            cancellationToken: cancellationToken);

        // Step 4: Publish notification
        await PublishNotificationAsync(
            new LegalHoldAppliedNotification(hold.Id, entityId, hold.Reason, hold.AppliedAtUtc),
            cancellationToken);

        _logger.LegalHoldApplied(hold.Id, entityId);
        RetentionDiagnostics.RecordCompleted(activity);
        RetentionDiagnostics.LegalHoldsAppliedTotal.Add(1,
            new KeyValuePair<string, object?>(RetentionDiagnostics.TagHoldId, hold.Id),
            new KeyValuePair<string, object?>(RetentionDiagnostics.TagEntityId, entityId));

        return Right<EncinaError, Unit>(unit);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> ReleaseHoldAsync(
        string holdId,
        string? releasedByUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(holdId);

        using var activity = RetentionDiagnostics.StartLegalHoldOperation(holdId, "pending");

        _logger.LegalHoldReleasing(holdId);

        // Step 1: Get the hold to identify the entity
        var holdResult = await _holdStore.GetByIdAsync(holdId, cancellationToken);
        var holdOption = holdResult.Match(
            Right: opt => opt,
            Left: _ => Option<LegalHold>.None);

        if (holdOption.IsNone)
        {
            _logger.LegalHoldNotFound(holdId);
            RetentionDiagnostics.RecordFailed(activity, "not_found");
            return Left<EncinaError, Unit>(RetentionErrors.HoldNotFound(holdId));
        }

        var hold = holdOption.Match(Some: h => h, None: () => throw new InvalidOperationException());
        activity?.SetTag(RetentionDiagnostics.TagEntityId, hold.EntityId);

        if (!hold.IsActive)
        {
            _logger.LegalHoldAlreadyReleased(holdId);
            RetentionDiagnostics.RecordFailed(activity, "already_released");
            return Left<EncinaError, Unit>(RetentionErrors.HoldAlreadyReleased(holdId));
        }

        var releasedAtUtc = _timeProvider.GetUtcNow();

        // Step 2: Release the hold in the store
        var releaseResult = await _holdStore.ReleaseAsync(holdId, releasedByUserId, releasedAtUtc, cancellationToken);
        if (releaseResult.IsLeft)
        {
            RetentionDiagnostics.RecordFailed(activity, "release_failed");
            return releaseResult;
        }

        // Step 3: Check if any other active holds remain for this entity
        var stillHeldResult = await _holdStore.IsUnderHoldAsync(hold.EntityId, cancellationToken);
        var stillHeld = stillHeldResult.Match(Right: h => h, Left: _ => false);

        if (!stillHeld)
        {
            // No more active holds — recalculate retention record statuses
            await RecalculateRecordStatusesAsync(hold.EntityId, cancellationToken);
        }
        else
        {
            _logger.LegalHoldOtherHoldsRemain(hold.EntityId);
        }

        // Step 4: Record audit entry
        await RecordAuditAsync(
            "LegalHoldReleased",
            entityId: hold.EntityId,
            detail: $"Legal hold '{holdId}' released",
            performedByUserId: releasedByUserId,
            cancellationToken: cancellationToken);

        // Step 5: Publish notification
        await PublishNotificationAsync(
            new LegalHoldReleasedNotification(holdId, hold.EntityId, releasedAtUtc),
            cancellationToken);

        _logger.LegalHoldReleased(holdId, releasedByUserId);
        RetentionDiagnostics.RecordCompleted(activity);
        RetentionDiagnostics.LegalHoldsReleasedTotal.Add(1,
            new KeyValuePair<string, object?>(RetentionDiagnostics.TagHoldId, holdId),
            new KeyValuePair<string, object?>(RetentionDiagnostics.TagEntityId, hold.EntityId));

        return Right<EncinaError, Unit>(unit);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, bool>> IsUnderHoldAsync(
        string entityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);
        return await _holdStore.IsUnderHoldAsync(entityId, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<LegalHold>>> GetActiveHoldsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _holdStore.GetActiveHoldsAsync(cancellationToken);
    }

    /// <summary>
    /// Updates all retention records for the specified entity to the given status.
    /// </summary>
    private async ValueTask CascadeHoldStatusAsync(
        string entityId,
        RetentionStatus newStatus,
        CancellationToken cancellationToken)
    {
        var recordsResult = await _recordStore.GetByEntityIdAsync(entityId, cancellationToken);

        await recordsResult.MatchAsync(
            RightAsync: async records =>
            {
                foreach (var record in records.Where(r => r.Status != RetentionStatus.Deleted))
                {
                    await _recordStore.UpdateStatusAsync(record.Id, newStatus, cancellationToken);
                }

                _logger.LegalHoldStatusCascaded(entityId, newStatus.ToString(), records.Count);

                return unit;
            },
            Left: error =>
            {
                _logger.LegalHoldCascadeFailed(entityId, error.Message);
                return unit;
            });
    }

    /// <summary>
    /// Recalculates retention record statuses after all legal holds are released.
    /// Records past expiration become Expired; records still within period become Active.
    /// </summary>
    private async ValueTask RecalculateRecordStatusesAsync(
        string entityId,
        CancellationToken cancellationToken)
    {
        var recordsResult = await _recordStore.GetByEntityIdAsync(entityId, cancellationToken);
        var now = _timeProvider.GetUtcNow();

        await recordsResult.MatchAsync(
            RightAsync: async records =>
            {
                foreach (var record in records.Where(r => r.Status == RetentionStatus.UnderLegalHold))
                {
                    var newStatus = record.ExpiresAtUtc < now
                        ? RetentionStatus.Expired
                        : RetentionStatus.Active;

                    await _recordStore.UpdateStatusAsync(record.Id, newStatus, cancellationToken);

                    _logger.RetentionRecordStatusRecalculated(record.Id, newStatus.ToString());
                }

                return unit;
            },
            Left: error =>
            {
                _logger.RetentionRecordRecalculationFailed(entityId, error.Message);
                return unit;
            });
    }

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
        if (_encina is null)
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
