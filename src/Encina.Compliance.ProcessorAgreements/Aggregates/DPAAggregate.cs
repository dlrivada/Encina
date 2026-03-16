using Encina.Compliance.ProcessorAgreements.Events;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.DomainModeling;

namespace Encina.Compliance.ProcessorAgreements.Aggregates;

/// <summary>
/// Event-sourced aggregate representing a Data Processing Agreement (DPA) between a controller and a processor.
/// </summary>
/// <remarks>
/// <para>
/// Per GDPR Article 28(3), processing by a processor shall be governed by a contract that sets out
/// the subject-matter, duration, nature and purpose of the processing, the type of personal data,
/// categories of data subjects, and the obligations and rights of the controller.
/// </para>
/// <para>
/// The DPA lifecycle progresses through:
/// <c>Active → PendingRenewal → Active</c> (renewal), or
/// <c>Active → Expired</c> (lapsed), or
/// <c>Active → Terminated</c> (explicit termination).
/// </para>
/// <para>
/// The <see cref="MandatoryTerms"/> property tracks compliance with the eight mandatory
/// contractual clauses defined in Article 28(3)(a)-(h). Use <see cref="DPAMandatoryTerms.IsFullyCompliant"/>
/// to verify all terms are present.
/// </para>
/// <para>
/// All state changes are captured as immutable events, providing a complete audit trail
/// for GDPR Art. 5(2) accountability requirements.
/// </para>
/// </remarks>
public sealed class DPAAggregate : AggregateBase
{
    private readonly List<string> _processingPurposes = [];

    /// <summary>
    /// The identifier of the processor this agreement covers.
    /// </summary>
    public Guid ProcessorId { get; private set; }

    /// <summary>
    /// The current lifecycle status of this agreement.
    /// </summary>
    public DPAStatus Status { get; private set; }

    /// <summary>
    /// The compliance status of the eight mandatory contractual terms per Article 28(3)(a)-(h).
    /// </summary>
    public DPAMandatoryTerms MandatoryTerms { get; private set; } = null!;

    /// <summary>
    /// Whether Standard Contractual Clauses are included per Articles 46(2)(c)/(d).
    /// </summary>
    public bool HasSCCs { get; private set; }

    /// <summary>
    /// The documented processing purposes covered by this agreement.
    /// </summary>
    /// <remarks>
    /// Per Article 28(3), the contract must set out "the nature and purpose of the processing."
    /// </remarks>
    public IReadOnlyList<string> ProcessingPurposes => _processingPurposes.AsReadOnly();

    /// <summary>
    /// The UTC timestamp when this agreement was signed by both parties.
    /// </summary>
    public DateTimeOffset SignedAtUtc { get; private set; }

    /// <summary>
    /// The UTC expiration date, or <see langword="null"/> for indefinite agreements.
    /// </summary>
    /// <remarks>
    /// When set, the expiration monitoring system publishes notifications as the date approaches.
    /// </remarks>
    public DateTimeOffset? ExpiresAtUtc { get; private set; }

    /// <summary>
    /// Tenant identifier for multi-tenancy scoping.
    /// </summary>
    public string? TenantId { get; private set; }

    /// <summary>
    /// Module identifier for modular monolith scoping.
    /// </summary>
    public string? ModuleId { get; private set; }

    /// <summary>
    /// The UTC timestamp when this agreement was created.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; private set; }

    /// <summary>
    /// The UTC timestamp when this agreement was last updated.
    /// </summary>
    public DateTimeOffset LastUpdatedAtUtc { get; private set; }

    /// <summary>
    /// Executes a new Data Processing Agreement between a controller and a processor.
    /// </summary>
    /// <param name="id">Unique identifier for the new agreement.</param>
    /// <param name="processorId">The identifier of the processor this agreement covers.</param>
    /// <param name="mandatoryTerms">The compliance status of the eight mandatory terms.</param>
    /// <param name="hasSCCs">Whether Standard Contractual Clauses are included.</param>
    /// <param name="processingPurposes">The processing purposes covered by this agreement.</param>
    /// <param name="signedAtUtc">The UTC timestamp when the agreement was signed.</param>
    /// <param name="expiresAtUtc">The UTC expiration date, or <see langword="null"/> for indefinite agreements.</param>
    /// <param name="occurredAtUtc">The UTC timestamp when this event occurred.</param>
    /// <param name="tenantId">Optional tenant identifier for multi-tenancy scoping.</param>
    /// <param name="moduleId">Optional module identifier for modular monolith scoping.</param>
    /// <returns>A new <see cref="DPAAggregate"/> in <see cref="DPAStatus.Active"/> status.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="mandatoryTerms"/> or <paramref name="processingPurposes"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="processingPurposes"/> is empty.</exception>
    public static DPAAggregate Execute(
        Guid id,
        Guid processorId,
        DPAMandatoryTerms mandatoryTerms,
        bool hasSCCs,
        IReadOnlyList<string> processingPurposes,
        DateTimeOffset signedAtUtc,
        DateTimeOffset? expiresAtUtc,
        DateTimeOffset occurredAtUtc,
        string? tenantId = null,
        string? moduleId = null)
    {
        ArgumentNullException.ThrowIfNull(mandatoryTerms);
        ArgumentNullException.ThrowIfNull(processingPurposes);

        if (processingPurposes.Count == 0)
        {
            throw new ArgumentException("At least one processing purpose must be specified.", nameof(processingPurposes));
        }

        var aggregate = new DPAAggregate();
        aggregate.RaiseEvent(new DPAExecuted(
            id, processorId, mandatoryTerms, hasSCCs, processingPurposes,
            signedAtUtc, expiresAtUtc, occurredAtUtc, tenantId, moduleId));
        return aggregate;
    }

    /// <summary>
    /// Amends the agreement with updated terms.
    /// </summary>
    /// <param name="updatedTerms">The updated mandatory terms after the amendment.</param>
    /// <param name="hasSCCs">Whether Standard Contractual Clauses are included after the amendment.</param>
    /// <param name="processingPurposes">The updated processing purposes.</param>
    /// <param name="amendmentReason">The reason for amending the agreement.</param>
    /// <param name="occurredAtUtc">The UTC timestamp when the amendment occurred.</param>
    /// <exception cref="InvalidOperationException">Thrown when the agreement is not in <see cref="DPAStatus.Active"/> or <see cref="DPAStatus.PendingRenewal"/> status.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="updatedTerms"/> or <paramref name="processingPurposes"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="amendmentReason"/> is null or whitespace, or <paramref name="processingPurposes"/> is empty.</exception>
    public void Amend(
        DPAMandatoryTerms updatedTerms,
        bool hasSCCs,
        IReadOnlyList<string> processingPurposes,
        string amendmentReason,
        DateTimeOffset occurredAtUtc)
    {
        if (Status is not (DPAStatus.Active or DPAStatus.PendingRenewal))
        {
            throw new InvalidOperationException(
                $"Cannot amend agreement '{Id}' because it is in '{Status}' status. Amendments are only allowed for Active or PendingRenewal agreements.");
        }

        ArgumentNullException.ThrowIfNull(updatedTerms);
        ArgumentNullException.ThrowIfNull(processingPurposes);
        ArgumentException.ThrowIfNullOrWhiteSpace(amendmentReason);

        if (processingPurposes.Count == 0)
        {
            throw new ArgumentException("At least one processing purpose must be specified.", nameof(processingPurposes));
        }

        RaiseEvent(new DPAAmended(Id, updatedTerms, hasSCCs, processingPurposes, amendmentReason, occurredAtUtc));
    }

    /// <summary>
    /// Records an audit conducted on this agreement per Article 28(3)(h).
    /// </summary>
    /// <param name="auditorId">The identifier of the person who conducted the audit.</param>
    /// <param name="auditFindings">Summary of the audit findings.</param>
    /// <param name="occurredAtUtc">The UTC timestamp when the audit was conducted.</param>
    /// <exception cref="InvalidOperationException">Thrown when the agreement is in <see cref="DPAStatus.Terminated"/> or <see cref="DPAStatus.Expired"/> status.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="auditorId"/> or <paramref name="auditFindings"/> is null or whitespace.</exception>
    public void Audit(string auditorId, string auditFindings, DateTimeOffset occurredAtUtc)
    {
        if (Status is DPAStatus.Terminated or DPAStatus.Expired)
        {
            throw new InvalidOperationException(
                $"Cannot audit agreement '{Id}' because it is in '{Status}' status. Audits are only allowed for active or pending renewal agreements.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(auditorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(auditFindings);

        RaiseEvent(new DPAAudited(Id, auditorId, auditFindings, occurredAtUtc));
    }

    /// <summary>
    /// Renews the agreement with a new expiration date.
    /// </summary>
    /// <param name="newExpiresAtUtc">The new UTC expiration date after renewal.</param>
    /// <param name="occurredAtUtc">The UTC timestamp when the renewal occurred.</param>
    /// <exception cref="InvalidOperationException">Thrown when the agreement is not in <see cref="DPAStatus.Active"/> or <see cref="DPAStatus.PendingRenewal"/> status.</exception>
    public void Renew(DateTimeOffset newExpiresAtUtc, DateTimeOffset occurredAtUtc)
    {
        if (Status is not (DPAStatus.Active or DPAStatus.PendingRenewal))
        {
            throw new InvalidOperationException(
                $"Cannot renew agreement '{Id}' because it is in '{Status}' status. Renewal is only allowed for Active or PendingRenewal agreements.");
        }

        RaiseEvent(new DPARenewed(Id, newExpiresAtUtc, occurredAtUtc));
    }

    /// <summary>
    /// Terminates the agreement, ending the processing relationship.
    /// </summary>
    /// <remarks>
    /// Per GDPR Article 28(3)(g), upon termination the processor must delete or return
    /// all personal data and certify compliance.
    /// </remarks>
    /// <param name="reason">The reason for terminating the agreement.</param>
    /// <param name="occurredAtUtc">The UTC timestamp when the termination occurred.</param>
    /// <exception cref="InvalidOperationException">Thrown when the agreement is already terminated or expired.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="reason"/> is null or whitespace.</exception>
    public void Terminate(string reason, DateTimeOffset occurredAtUtc)
    {
        if (Status is DPAStatus.Terminated or DPAStatus.Expired)
        {
            throw new InvalidOperationException(
                $"Cannot terminate agreement '{Id}' because it is already in '{Status}' status.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        RaiseEvent(new DPATerminated(Id, reason, occurredAtUtc));
    }

    /// <summary>
    /// Marks the agreement as expired due to passing its expiration date without renewal.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the agreement is already expired or terminated.</exception>
    public void MarkExpired(DateTimeOffset occurredAtUtc)
    {
        if (Status is DPAStatus.Expired or DPAStatus.Terminated)
        {
            throw new InvalidOperationException(
                $"Cannot mark agreement '{Id}' as expired because it is already in '{Status}' status.");
        }

        RaiseEvent(new DPAExpired(Id, occurredAtUtc));
    }

    /// <summary>
    /// Marks the agreement as pending renewal due to approaching expiration.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the agreement is not in <see cref="DPAStatus.Active"/> status.</exception>
    public void MarkPendingRenewal(DateTimeOffset occurredAtUtc)
    {
        if (Status != DPAStatus.Active)
        {
            throw new InvalidOperationException(
                $"Cannot mark agreement '{Id}' as pending renewal because it is in '{Status}' status. Only active agreements can be marked for renewal.");
        }

        RaiseEvent(new DPAMarkedPendingRenewal(Id, occurredAtUtc));
    }

    /// <summary>
    /// Determines whether this agreement is currently active and not expired.
    /// </summary>
    /// <remarks>
    /// An agreement is active when its <see cref="Status"/> is <see cref="DPAStatus.Active"/>
    /// and its <see cref="ExpiresAtUtc"/> has not passed (or is <see langword="null"/>).
    /// </remarks>
    /// <param name="nowUtc">The current UTC time for comparison against <see cref="ExpiresAtUtc"/>.</param>
    /// <returns><see langword="true"/> if the agreement is active and has not expired; otherwise, <see langword="false"/>.</returns>
    public bool IsActive(DateTimeOffset nowUtc) =>
        Status == DPAStatus.Active &&
        (ExpiresAtUtc is null || ExpiresAtUtc > nowUtc);

    /// <inheritdoc />
    protected override void Apply(object domainEvent)
    {
        switch (domainEvent)
        {
            case DPAExecuted e:
                Id = e.DPAId;
                ProcessorId = e.ProcessorId;
                Status = DPAStatus.Active;
                MandatoryTerms = e.MandatoryTerms;
                HasSCCs = e.HasSCCs;
                _processingPurposes.Clear();
                _processingPurposes.AddRange(e.ProcessingPurposes);
                SignedAtUtc = e.SignedAtUtc;
                ExpiresAtUtc = e.ExpiresAtUtc;
                TenantId = e.TenantId;
                ModuleId = e.ModuleId;
                CreatedAtUtc = e.OccurredAtUtc;
                LastUpdatedAtUtc = e.OccurredAtUtc;
                break;

            case DPAAmended e:
                MandatoryTerms = e.UpdatedTerms;
                HasSCCs = e.HasSCCs;
                _processingPurposes.Clear();
                _processingPurposes.AddRange(e.ProcessingPurposes);
                LastUpdatedAtUtc = e.OccurredAtUtc;
                break;

            case DPAAudited e:
                LastUpdatedAtUtc = e.OccurredAtUtc;
                break;

            case DPARenewed e:
                Status = DPAStatus.Active;
                ExpiresAtUtc = e.NewExpiresAtUtc;
                LastUpdatedAtUtc = e.OccurredAtUtc;
                break;

            case DPATerminated e:
                Status = DPAStatus.Terminated;
                LastUpdatedAtUtc = e.OccurredAtUtc;
                break;

            case DPAExpired e:
                Status = DPAStatus.Expired;
                LastUpdatedAtUtc = e.OccurredAtUtc;
                break;

            case DPAMarkedPendingRenewal e:
                Status = DPAStatus.PendingRenewal;
                LastUpdatedAtUtc = e.OccurredAtUtc;
                break;
        }
    }
}
