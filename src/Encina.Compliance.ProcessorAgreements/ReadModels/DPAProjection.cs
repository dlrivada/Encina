using Encina.Compliance.ProcessorAgreements.Events;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Marten.Projections;

namespace Encina.Compliance.ProcessorAgreements.ReadModels;

/// <summary>
/// Marten inline projection that transforms DPA aggregate events into <see cref="DPAReadModel"/> state.
/// </summary>
/// <remarks>
/// <para>
/// This projection implements the "Query" side of CQRS for Data Processing Agreement management.
/// It handles all 7 DPA event types, creating or updating the <see cref="DPAReadModel"/> as events are applied.
/// </para>
/// <para>
/// <b>Event Handling</b>:
/// <list type="bullet">
///   <item><description><see cref="DPAExecuted"/> — Creates a new read model in <see cref="DPAStatus.Active"/> status (first event in stream)</description></item>
///   <item><description><see cref="DPAAmended"/> — Updates mandatory terms, SCCs, and processing purposes</description></item>
///   <item><description><see cref="DPAAudited"/> — Appends audit record to <see cref="DPAReadModel.AuditHistory"/></description></item>
///   <item><description><see cref="DPARenewed"/> — Updates expiration date and transitions to <see cref="DPAStatus.Active"/></description></item>
///   <item><description><see cref="DPATerminated"/> — Records termination reason and transitions to <see cref="DPAStatus.Terminated"/></description></item>
///   <item><description><see cref="DPAExpired"/> — Transitions to <see cref="DPAStatus.Expired"/></description></item>
///   <item><description><see cref="DPAMarkedPendingRenewal"/> — Transitions to <see cref="DPAStatus.PendingRenewal"/></description></item>
/// </list>
/// </para>
/// <para>
/// <b>Idempotency</b>: The projection is idempotent — applying the same event multiple times
/// produces the same result. This enables safe replay and rebuild of the read model.
/// </para>
/// </remarks>
public sealed class DPAProjection :
    IProjection<DPAReadModel>,
    IProjectionCreator<DPAExecuted, DPAReadModel>,
    IProjectionHandler<DPAAmended, DPAReadModel>,
    IProjectionHandler<DPAAudited, DPAReadModel>,
    IProjectionHandler<DPARenewed, DPAReadModel>,
    IProjectionHandler<DPATerminated, DPAReadModel>,
    IProjectionHandler<DPAExpired, DPAReadModel>,
    IProjectionHandler<DPAMarkedPendingRenewal, DPAReadModel>
{
    /// <inheritdoc />
    public string ProjectionName => "DPAProjection";

    /// <summary>
    /// Creates a new <see cref="DPAReadModel"/> from a <see cref="DPAExecuted"/> event.
    /// </summary>
    /// <remarks>
    /// This is the first event in a DPA aggregate stream. It initializes all fields
    /// including mandatory terms compliance and processing purposes per Article 28(3).
    /// </remarks>
    /// <param name="domainEvent">The DPA executed event.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>A new <see cref="DPAReadModel"/> in <see cref="DPAStatus.Active"/> status.</returns>
    public DPAReadModel Create(DPAExecuted domainEvent, ProjectionContext context)
    {
        return new DPAReadModel
        {
            Id = domainEvent.DPAId,
            ProcessorId = domainEvent.ProcessorId,
            Status = DPAStatus.Active,
            MandatoryTerms = domainEvent.MandatoryTerms,
            HasSCCs = domainEvent.HasSCCs,
            ProcessingPurposes = [.. domainEvent.ProcessingPurposes],
            SignedAtUtc = domainEvent.SignedAtUtc,
            ExpiresAtUtc = domainEvent.ExpiresAtUtc,
            TenantId = domainEvent.TenantId,
            ModuleId = domainEvent.ModuleId,
            CreatedAtUtc = domainEvent.OccurredAtUtc,
            LastModifiedAtUtc = domainEvent.OccurredAtUtc,
            Version = 1
        };
    }

    /// <summary>
    /// Updates the read model when a DPA is amended with updated terms.
    /// </summary>
    /// <remarks>
    /// Amendments may update mandatory terms, SCC inclusion, or processing purposes.
    /// Per GDPR Article 28(9), the contract must be in writing, including in electronic form.
    /// </remarks>
    /// <param name="domainEvent">The DPA amended event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model.</returns>
    public DPAReadModel Apply(DPAAmended domainEvent, DPAReadModel current, ProjectionContext context)
    {
        current.MandatoryTerms = domainEvent.UpdatedTerms;
        current.HasSCCs = domainEvent.HasSCCs;
        current.ProcessingPurposes = [.. domainEvent.ProcessingPurposes];
        current.LastModifiedAtUtc = domainEvent.OccurredAtUtc;
        current.Version++;
        return current;
    }

    /// <summary>
    /// Updates the read model when an audit is conducted on the agreement.
    /// </summary>
    /// <remarks>
    /// Per Article 28(3)(h), the processor must make available to the controller all information
    /// necessary to demonstrate compliance and allow for and contribute to audits.
    /// Each audit is appended to the <see cref="DPAReadModel.AuditHistory"/> list.
    /// </remarks>
    /// <param name="domainEvent">The DPA audited event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model.</returns>
    public DPAReadModel Apply(DPAAudited domainEvent, DPAReadModel current, ProjectionContext context)
    {
        current.AuditHistory.Add(new AuditRecord(
            domainEvent.AuditorId,
            domainEvent.AuditFindings,
            domainEvent.OccurredAtUtc));
        current.LastModifiedAtUtc = domainEvent.OccurredAtUtc;
        current.Version++;
        return current;
    }

    /// <summary>
    /// Updates the read model when the agreement is renewed with a new expiration date.
    /// </summary>
    /// <remarks>
    /// Renewal transitions the agreement back to <see cref="DPAStatus.Active"/> with a new expiration date.
    /// The renewal event provides a complete audit trail of the contractual timeline.
    /// </remarks>
    /// <param name="domainEvent">The DPA renewed event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model in <see cref="DPAStatus.Active"/> status.</returns>
    public DPAReadModel Apply(DPARenewed domainEvent, DPAReadModel current, ProjectionContext context)
    {
        current.Status = DPAStatus.Active;
        current.ExpiresAtUtc = domainEvent.NewExpiresAtUtc;
        current.LastModifiedAtUtc = domainEvent.OccurredAtUtc;
        current.Version++;
        return current;
    }

    /// <summary>
    /// Updates the read model when the agreement is explicitly terminated.
    /// </summary>
    /// <remarks>
    /// Per GDPR Article 28(3)(g), upon termination the processor must, at the choice of the controller,
    /// delete or return all personal data and certify that it has done so.
    /// </remarks>
    /// <param name="domainEvent">The DPA terminated event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model in <see cref="DPAStatus.Terminated"/> status.</returns>
    public DPAReadModel Apply(DPATerminated domainEvent, DPAReadModel current, ProjectionContext context)
    {
        current.Status = DPAStatus.Terminated;
        current.TerminationReason = domainEvent.Reason;
        current.TerminatedAtUtc = domainEvent.OccurredAtUtc;
        current.LastModifiedAtUtc = domainEvent.OccurredAtUtc;
        current.Version++;
        return current;
    }

    /// <summary>
    /// Updates the read model when the agreement has passed its expiration date without renewal.
    /// </summary>
    /// <remarks>
    /// Transitions the agreement to <see cref="DPAStatus.Expired"/>. Processing operations relying
    /// on this agreement should be blocked or warned by the <c>ProcessorValidationPipelineBehavior</c>
    /// until a new agreement is signed.
    /// </remarks>
    /// <param name="domainEvent">The DPA expired event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model in <see cref="DPAStatus.Expired"/> status.</returns>
    public DPAReadModel Apply(DPAExpired domainEvent, DPAReadModel current, ProjectionContext context)
    {
        current.Status = DPAStatus.Expired;
        current.LastModifiedAtUtc = domainEvent.OccurredAtUtc;
        current.Version++;
        return current;
    }

    /// <summary>
    /// Updates the read model when the agreement is marked as pending renewal.
    /// </summary>
    /// <remarks>
    /// Transitions the agreement to <see cref="DPAStatus.PendingRenewal"/>. This status triggers
    /// <c>DPAExpiringNotification</c> to alert compliance teams about upcoming renewal deadlines.
    /// The agreement remains valid for processing operations while in this status.
    /// </remarks>
    /// <param name="domainEvent">The DPA marked pending renewal event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model in <see cref="DPAStatus.PendingRenewal"/> status.</returns>
    public DPAReadModel Apply(DPAMarkedPendingRenewal domainEvent, DPAReadModel current, ProjectionContext context)
    {
        current.Status = DPAStatus.PendingRenewal;
        current.LastModifiedAtUtc = domainEvent.OccurredAtUtc;
        current.Version++;
        return current;
    }
}
