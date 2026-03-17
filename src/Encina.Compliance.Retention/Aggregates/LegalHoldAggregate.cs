using Encina.Compliance.Retention.Events;
using Encina.DomainModeling;

namespace Encina.Compliance.Retention.Aggregates;

/// <summary>
/// Event-sourced aggregate representing a legal hold placed on a data entity to prevent deletion.
/// </summary>
/// <remarks>
/// <para>
/// A legal hold suspends all deletion activity for the specified entity regardless of whether
/// the data's retention period has expired. This supports GDPR Article 17(3)(e): processing
/// is necessary "for the establishment, exercise or defence of legal claims."
/// </para>
/// <para>
/// The lifecycle is simple: <c>Active → Lifted</c>. Multiple holds may be active simultaneously
/// for the same entity (e.g., multiple ongoing litigation matters). The entity remains protected
/// from deletion until ALL active holds are lifted.
/// </para>
/// <para>
/// When a hold is placed or lifted, the <c>ILegalHoldService</c> performs cross-aggregate
/// coordination, raising <c>RetentionRecordHeld</c> or <c>RetentionRecordReleased</c> events
/// on the affected <see cref="RetentionRecordAggregate"/> instances.
/// </para>
/// <para>
/// All state changes are captured as immutable events, providing a complete audit trail
/// for GDPR Article 5(2) accountability — who placed the hold, when, and why.
/// </para>
/// </remarks>
public sealed class LegalHoldAggregate : AggregateBase
{
    /// <summary>
    /// The identifier of the data entity this hold applies to.
    /// </summary>
    public string EntityId { get; private set; } = string.Empty;

    /// <summary>
    /// The legal reason for the hold (e.g., "Ongoing litigation - Case #12345").
    /// </summary>
    public string Reason { get; private set; } = string.Empty;

    /// <summary>
    /// Identifier of the user who placed the hold (typically legal counsel).
    /// </summary>
    public string AppliedByUserId { get; private set; } = string.Empty;

    /// <summary>
    /// Whether this hold is currently active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Identifier of the user who lifted the hold, or <see langword="null"/> if still active.
    /// </summary>
    public string? ReleasedByUserId { get; private set; }

    /// <summary>
    /// Timestamp when the hold was placed (UTC).
    /// </summary>
    public DateTimeOffset AppliedAtUtc { get; private set; }

    /// <summary>
    /// Timestamp when the hold was lifted (UTC), or <see langword="null"/> if still active.
    /// </summary>
    public DateTimeOffset? ReleasedAtUtc { get; private set; }

    /// <summary>
    /// Tenant identifier for multi-tenancy scoping.
    /// </summary>
    public string? TenantId { get; private set; }

    /// <summary>
    /// Module identifier for modular monolith scoping.
    /// </summary>
    public string? ModuleId { get; private set; }

    /// <summary>
    /// Places a new legal hold on a data entity, preventing deletion.
    /// </summary>
    /// <param name="id">Unique identifier for the new legal hold aggregate.</param>
    /// <param name="entityId">The identifier of the data entity to hold.</param>
    /// <param name="reason">The legal reason for the hold.</param>
    /// <param name="appliedByUserId">Identifier of the user placing the hold.</param>
    /// <param name="appliedAtUtc">Timestamp when the hold was placed (UTC).</param>
    /// <param name="tenantId">Optional tenant identifier for multi-tenancy scoping.</param>
    /// <param name="moduleId">Optional module identifier for modular monolith scoping.</param>
    /// <returns>A new <see cref="LegalHoldAggregate"/> in active status.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="entityId"/>, <paramref name="reason"/>, or <paramref name="appliedByUserId"/> is null or whitespace.</exception>
    public static LegalHoldAggregate Place(
        Guid id,
        string entityId,
        string reason,
        string appliedByUserId,
        DateTimeOffset appliedAtUtc,
        string? tenantId = null,
        string? moduleId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        ArgumentException.ThrowIfNullOrWhiteSpace(appliedByUserId);

        var aggregate = new LegalHoldAggregate();
        aggregate.RaiseEvent(new LegalHoldPlaced(
            id, entityId, reason, appliedByUserId, appliedAtUtc, tenantId, moduleId));
        return aggregate;
    }

    /// <summary>
    /// Lifts the legal hold, allowing normal retention enforcement to resume for this entity.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Once lifted, this hold no longer prevents deletion of the associated entity. However,
    /// if other active holds exist for the same entity, deletion remains suspended until
    /// all holds are lifted.
    /// </para>
    /// <para>
    /// Per GDPR Article 5(2) accountability, this event records who released the hold and
    /// when, providing a complete audit trail of the hold lifecycle.
    /// </para>
    /// </remarks>
    /// <param name="releasedByUserId">Identifier of the user lifting the hold.</param>
    /// <param name="releasedAtUtc">Timestamp when the hold was lifted (UTC).</param>
    /// <exception cref="InvalidOperationException">Thrown when the hold has already been lifted.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="releasedByUserId"/> is null or whitespace.</exception>
    public void Lift(string releasedByUserId, DateTimeOffset releasedAtUtc)
    {
        if (!IsActive)
        {
            throw new InvalidOperationException(
                $"Cannot lift legal hold '{Id}' because it has already been lifted.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(releasedByUserId);

        RaiseEvent(new LegalHoldLifted(Id, EntityId, releasedByUserId, releasedAtUtc));
    }

    /// <inheritdoc />
    protected override void Apply(object domainEvent)
    {
        switch (domainEvent)
        {
            case LegalHoldPlaced e:
                Id = e.HoldId;
                EntityId = e.EntityId;
                Reason = e.Reason;
                AppliedByUserId = e.AppliedByUserId;
                IsActive = true;
                AppliedAtUtc = e.AppliedAtUtc;
                TenantId = e.TenantId;
                ModuleId = e.ModuleId;
                break;

            case LegalHoldLifted e:
                IsActive = false;
                ReleasedByUserId = e.ReleasedByUserId;
                ReleasedAtUtc = e.ReleasedAtUtc;
                break;
        }
    }
}
