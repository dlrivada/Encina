using Encina.Compliance.CrossBorderTransfer.Events;
using Encina.Compliance.CrossBorderTransfer.Model;
using Encina.DomainModeling;

namespace Encina.Compliance.CrossBorderTransfer.Aggregates;

/// <summary>
/// Event-sourced aggregate representing a Transfer Impact Assessment (TIA) lifecycle.
/// </summary>
/// <remarks>
/// <para>
/// A TIA evaluates whether the legal framework of a third country provides "essentially equivalent"
/// protection to the EU/EEA standard, as required by the Schrems II judgment (CJEU C-311/18).
/// The assessment considers government surveillance laws, data protection authorities,
/// judicial redress mechanisms, and other factors.
/// </para>
/// <para>
/// The lifecycle progresses through: <see cref="TIAStatus.Draft"/> → <see cref="TIAStatus.InProgress"/>
/// → <see cref="TIAStatus.PendingDPOReview"/> → <see cref="TIAStatus.Completed"/> (or back to
/// <see cref="TIAStatus.InProgress"/> if rejected by DPO). A completed TIA may later transition
/// to <see cref="TIAStatus.Expired"/>.
/// </para>
/// <para>
/// All state changes are captured as immutable events, providing a complete audit trail
/// for GDPR Art. 5(2) accountability requirements.
/// </para>
/// </remarks>
public sealed class TIAAggregate : AggregateBase
{
    private readonly List<SupplementaryMeasure> _requiredSupplementaryMeasures = [];

    /// <summary>
    /// ISO 3166-1 alpha-2 country code of the data exporter.
    /// </summary>
    public string SourceCountryCode { get; private set; } = string.Empty;

    /// <summary>
    /// ISO 3166-1 alpha-2 country code of the data importer.
    /// </summary>
    public string DestinationCountryCode { get; private set; } = string.Empty;

    /// <summary>
    /// Category of personal data being assessed for transfer.
    /// </summary>
    public string DataCategory { get; private set; } = string.Empty;

    /// <summary>
    /// Risk score assigned during assessment, between 0.0 (no risk) and 1.0 (maximum risk).
    /// </summary>
    /// <remarks>
    /// <c>null</c> until risk assessment is performed.
    /// </remarks>
    public double? RiskScore { get; private set; }

    /// <summary>
    /// Current lifecycle status of the TIA.
    /// </summary>
    public TIAStatus Status { get; private set; }

    /// <summary>
    /// Summary of risk assessment findings.
    /// </summary>
    /// <remarks>
    /// <c>null</c> until risk assessment is performed.
    /// </remarks>
    public string? Findings { get; private set; }

    /// <summary>
    /// Identifier of the person who performed the risk assessment.
    /// </summary>
    public string? AssessorId { get; private set; }

    /// <summary>
    /// Timestamp when the DPO completed their review (UTC).
    /// </summary>
    public DateTimeOffset? DPOReviewedAtUtc { get; private set; }

    /// <summary>
    /// Timestamp when the TIA was completed (UTC).
    /// </summary>
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    /// <summary>
    /// Supplementary measures identified as required during the assessment.
    /// </summary>
    public IReadOnlyList<SupplementaryMeasure> RequiredSupplementaryMeasures => _requiredSupplementaryMeasures.AsReadOnly();

    /// <summary>
    /// Tenant identifier for multi-tenancy scoping.
    /// </summary>
    public string? TenantId { get; private set; }

    /// <summary>
    /// Module identifier for modular monolith scoping.
    /// </summary>
    public string? ModuleId { get; private set; }

    /// <summary>
    /// Creates a new Transfer Impact Assessment for the specified transfer route.
    /// </summary>
    /// <param name="id">Unique identifier for the new TIA.</param>
    /// <param name="sourceCountryCode">ISO 3166-1 alpha-2 country code of the data exporter.</param>
    /// <param name="destinationCountryCode">ISO 3166-1 alpha-2 country code of the data importer.</param>
    /// <param name="dataCategory">Category of personal data being assessed.</param>
    /// <param name="createdBy">Identifier of the user initiating the TIA.</param>
    /// <param name="tenantId">Optional tenant identifier for multi-tenancy scoping.</param>
    /// <param name="moduleId">Optional module identifier for modular monolith scoping.</param>
    /// <returns>A new <see cref="TIAAggregate"/> in <see cref="TIAStatus.Draft"/> status.</returns>
    public static TIAAggregate Create(
        Guid id,
        string sourceCountryCode,
        string destinationCountryCode,
        string dataCategory,
        string createdBy,
        string? tenantId = null,
        string? moduleId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceCountryCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationCountryCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(dataCategory);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);

        var aggregate = new TIAAggregate();
        aggregate.RaiseEvent(new TIACreated(id, sourceCountryCode, destinationCountryCode, dataCategory, createdBy, tenantId, moduleId));
        return aggregate;
    }

    /// <summary>
    /// Records the risk assessment results for this TIA.
    /// </summary>
    /// <param name="riskScore">Risk score between 0.0 and 1.0.</param>
    /// <param name="findings">Summary of risk assessment findings.</param>
    /// <param name="assessorId">Identifier of the person who performed the assessment.</param>
    /// <exception cref="InvalidOperationException">Thrown when the TIA is not in <see cref="TIAStatus.Draft"/> or <see cref="TIAStatus.InProgress"/> status.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="riskScore"/> is not between 0.0 and 1.0.</exception>
    public void AssessRisk(double riskScore, string? findings, string assessorId)
    {
        if (Status is not (TIAStatus.Draft or TIAStatus.InProgress))
        {
            throw new InvalidOperationException($"Cannot assess risk when TIA is in '{Status}' status. Risk assessment is only allowed in Draft or InProgress status.");
        }

        if (riskScore is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(riskScore), riskScore, "Risk score must be between 0.0 and 1.0.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(assessorId);

        RaiseEvent(new TIARiskAssessed(Id, riskScore, findings, assessorId));
    }

    /// <summary>
    /// Identifies a supplementary measure as required for this transfer route.
    /// </summary>
    /// <param name="measureId">Unique identifier for the supplementary measure.</param>
    /// <param name="type">Category of the measure.</param>
    /// <param name="description">Human-readable description of the required measure.</param>
    /// <exception cref="InvalidOperationException">Thrown when the TIA is not in <see cref="TIAStatus.InProgress"/> status.</exception>
    public void RequireSupplementaryMeasure(Guid measureId, SupplementaryMeasureType type, string description)
    {
        if (Status != TIAStatus.InProgress)
        {
            throw new InvalidOperationException($"Cannot add supplementary measures when TIA is in '{Status}' status. Measures can only be added during InProgress status.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        RaiseEvent(new TIASupplementaryMeasureRequired(Id, measureId, type, description));
    }

    /// <summary>
    /// Submits the TIA for review by the Data Protection Officer.
    /// </summary>
    /// <param name="submittedBy">Identifier of the person submitting the TIA for review.</param>
    /// <exception cref="InvalidOperationException">Thrown when the TIA is not in <see cref="TIAStatus.InProgress"/> status.</exception>
    public void SubmitForDPOReview(string submittedBy)
    {
        if (Status != TIAStatus.InProgress)
        {
            throw new InvalidOperationException($"Cannot submit for DPO review when TIA is in '{Status}' status. Submission is only allowed from InProgress status.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(submittedBy);

        RaiseEvent(new TIASubmittedForDPOReview(Id, submittedBy));
    }

    /// <summary>
    /// Records the DPO's approval of the TIA.
    /// </summary>
    /// <param name="reviewedBy">Identifier of the DPO who approved the assessment.</param>
    /// <exception cref="InvalidOperationException">Thrown when the TIA is not in <see cref="TIAStatus.PendingDPOReview"/> status.</exception>
    public void ApproveDPOReview(string reviewedBy)
    {
        if (Status != TIAStatus.PendingDPOReview)
        {
            throw new InvalidOperationException($"Cannot approve DPO review when TIA is in '{Status}' status. Approval is only allowed from PendingDPOReview status.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reviewedBy);

        RaiseEvent(new TIADPOApproved(Id, reviewedBy));
    }

    /// <summary>
    /// Records the DPO's rejection of the TIA, returning it for revision.
    /// </summary>
    /// <param name="reviewedBy">Identifier of the DPO who rejected the assessment.</param>
    /// <param name="reason">Explanation of why the assessment was rejected.</param>
    /// <exception cref="InvalidOperationException">Thrown when the TIA is not in <see cref="TIAStatus.PendingDPOReview"/> status.</exception>
    public void RejectDPOReview(string reviewedBy, string reason)
    {
        if (Status != TIAStatus.PendingDPOReview)
        {
            throw new InvalidOperationException($"Cannot reject DPO review when TIA is in '{Status}' status. Rejection is only allowed from PendingDPOReview status.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reviewedBy);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        RaiseEvent(new TIADPORejected(Id, reviewedBy, reason));
    }

    /// <summary>
    /// Completes the TIA after DPO approval, making it available for authorizing transfers.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the TIA has not been approved by the DPO.</exception>
    public void Complete()
    {
        if (Status != TIAStatus.PendingDPOReview)
        {
            throw new InvalidOperationException($"Cannot complete TIA when it is in '{Status}' status. Completion requires DPO approval (PendingDPOReview status with prior approval).");
        }

        RaiseEvent(new TIACompleted(Id));
    }

    /// <summary>
    /// Expires the TIA, invalidating it for future transfer authorizations.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the TIA is not in <see cref="TIAStatus.Completed"/> status.</exception>
    public void Expire()
    {
        if (Status != TIAStatus.Completed)
        {
            throw new InvalidOperationException($"Cannot expire TIA when it is in '{Status}' status. Only completed TIAs can expire.");
        }

        RaiseEvent(new TIAExpired(Id));
    }

    /// <inheritdoc />
    protected override void Apply(object domainEvent)
    {
        switch (domainEvent)
        {
            case TIACreated e:
                Id = e.TIAId;
                SourceCountryCode = e.SourceCountryCode;
                DestinationCountryCode = e.DestinationCountryCode;
                DataCategory = e.DataCategory;
                Status = TIAStatus.Draft;
                TenantId = e.TenantId;
                ModuleId = e.ModuleId;
                break;

            case TIARiskAssessed e:
                RiskScore = e.RiskScore;
                Findings = e.Findings;
                AssessorId = e.AssessorId;
                Status = TIAStatus.InProgress;
                break;

            case TIASupplementaryMeasureRequired e:
                _requiredSupplementaryMeasures.Add(new SupplementaryMeasure
                {
                    Id = e.MeasureId,
                    Type = e.MeasureType,
                    Description = e.Description,
                    IsImplemented = false
                });
                break;

            case TIASubmittedForDPOReview:
                Status = TIAStatus.PendingDPOReview;
                break;

            case TIADPOApproved e:
                DPOReviewedAtUtc = DateTimeOffset.UtcNow;
                break;

            case TIADPORejected:
                Status = TIAStatus.InProgress;
                DPOReviewedAtUtc = DateTimeOffset.UtcNow;
                break;

            case TIACompleted:
                Status = TIAStatus.Completed;
                CompletedAtUtc = DateTimeOffset.UtcNow;
                break;

            case TIAExpired:
                Status = TIAStatus.Expired;
                break;
        }
    }
}
