using Encina.Compliance.Retention.ReadModels;
using LanguageExt;

namespace Encina.Compliance.Retention.Abstractions;

/// <summary>
/// Service interface for managing legal hold lifecycle operations via event-sourced aggregates.
/// </summary>
/// <remarks>
/// <para>
/// Provides a clean API for placing, lifting, and querying legal holds. The implementation
/// wraps the event-sourced <c>LegalHoldAggregate</c> via
/// <c>IAggregateRepository&lt;LegalHoldAggregate&gt;</c>, handling aggregate loading,
/// command execution, persistence, and cache management.
/// </para>
/// <para>
/// This service replaces the legacy <c>ILegalHoldManager</c> and <c>ILegalHoldStore</c>
/// interfaces with a single CQRS-oriented API. The event stream serves as the audit trail,
/// eliminating the need for separate audit recording per GDPR Article 5(2) accountability.
/// </para>
/// <para>
/// <b>Cross-Aggregate Coordination</b>: When a hold is placed, the service cascades a
/// <c>RetentionRecordHeld</c> event to all affected <c>RetentionRecordAggregate</c> instances
/// for the entity. When a hold is lifted, it cascades <c>RetentionRecordReleased</c> events
/// if no other active holds remain for the entity.
/// </para>
/// <para>
/// <b>Commands</b> (write operations via aggregate):
/// <list type="bullet">
///   <item><description><see cref="PlaceHoldAsync"/> — Places a new legal hold on an entity (Art. 17(3)(e))</description></item>
///   <item><description><see cref="LiftHoldAsync"/> — Lifts a legal hold, resuming normal retention (Art. 5(2))</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Queries</b> (read operations via read model repository):
/// <list type="bullet">
///   <item><description><see cref="GetHoldAsync"/> — Retrieves a hold by ID</description></item>
///   <item><description><see cref="GetActiveHoldsForEntityAsync"/> — Lists active holds for an entity</description></item>
///   <item><description><see cref="GetAllActiveHoldsAsync"/> — Lists all active holds</description></item>
///   <item><description><see cref="HasActiveHoldsAsync"/> — Checks if an entity has active holds</description></item>
///   <item><description><see cref="GetHoldHistoryAsync"/> — Retrieves full event history</description></item>
/// </list>
/// </para>
/// </remarks>
public interface ILegalHoldService
{
    // ========================================================================
    // Command operations (write-side via LegalHoldAggregate)
    // ========================================================================

    /// <summary>
    /// Places a new legal hold on a data entity, preventing deletion.
    /// </summary>
    /// <param name="entityId">The identifier of the data entity to hold.</param>
    /// <param name="reason">The legal reason for the hold (e.g., "Ongoing litigation - Case #12345").</param>
    /// <param name="appliedByUserId">Identifier of the user placing the hold (typically legal counsel).</param>
    /// <param name="tenantId">Optional tenant identifier for multi-tenancy.</param>
    /// <param name="moduleId">Optional module identifier for modular monolith.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or the identifier of the newly created legal hold aggregate.</returns>
    /// <remarks>
    /// <para>
    /// Per GDPR Article 17(3)(e), processing (including retention) is necessary "for the
    /// establishment, exercise or defence of legal claims." This operation creates a
    /// <see cref="Aggregates.LegalHoldAggregate"/> and cascades a hold to all affected
    /// <see cref="Aggregates.RetentionRecordAggregate"/> instances for the entity.
    /// </para>
    /// <para>
    /// Multiple holds may exist for the same entity (e.g., multiple ongoing litigation matters).
    /// The entity remains protected from deletion until ALL active holds are lifted.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, Guid>> PlaceHoldAsync(
        string entityId,
        string reason,
        string appliedByUserId,
        string? tenantId = null,
        string? moduleId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lifts a legal hold, allowing normal retention enforcement to resume for the entity.
    /// </summary>
    /// <param name="holdId">The legal hold aggregate identifier.</param>
    /// <param name="releasedByUserId">Identifier of the user lifting the hold.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    /// <remarks>
    /// <para>
    /// Once lifted, this hold no longer prevents deletion. If other active holds exist for the
    /// same entity, deletion remains suspended until all holds are lifted.
    /// </para>
    /// <para>
    /// When no other active holds remain, affected retention records are released (cascading
    /// <c>RetentionRecordReleased</c> events). The enforcement service will re-evaluate
    /// the records during its next sweep.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> LiftHoldAsync(
        Guid holdId,
        string releasedByUserId,
        CancellationToken cancellationToken = default);

    // ========================================================================
    // Query operations (read-side via LegalHoldReadModel)
    // ========================================================================

    /// <summary>
    /// Retrieves a legal hold by its aggregate identifier.
    /// </summary>
    /// <param name="holdId">The legal hold aggregate identifier.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error (including not-found) or the legal hold read model.</returns>
    ValueTask<Either<EncinaError, LegalHoldReadModel>> GetHoldAsync(
        Guid holdId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all active legal holds for a specific data entity.
    /// </summary>
    /// <param name="entityId">The identifier of the data entity.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A read-only list of active legal hold read models for the entity.</returns>
    ValueTask<Either<EncinaError, IReadOnlyList<LegalHoldReadModel>>> GetActiveHoldsForEntityAsync(
        string entityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all currently active legal holds.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A read-only list of all active legal hold read models.</returns>
    ValueTask<Either<EncinaError, IReadOnlyList<LegalHoldReadModel>>> GetAllActiveHoldsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a specific data entity has any active legal holds.
    /// </summary>
    /// <param name="entityId">The identifier of the data entity.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or <see langword="true"/> if the entity has active holds.</returns>
    ValueTask<Either<EncinaError, bool>> HasActiveHoldsAsync(
        string entityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the full event history for a legal hold aggregate.
    /// </summary>
    /// <param name="holdId">The legal hold aggregate identifier.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>
    /// Either an error or the list of domain events that have been applied to this hold,
    /// ordered chronologically. Provides a complete audit trail for GDPR Article 5(2) accountability
    /// — who placed the hold, when, and why.
    /// </returns>
    ValueTask<Either<EncinaError, IReadOnlyList<object>>> GetHoldHistoryAsync(
        Guid holdId,
        CancellationToken cancellationToken = default);
}
