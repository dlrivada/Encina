using Encina.Compliance.Consent.Events;
using Encina.DomainModeling;

namespace Encina.Compliance.Consent.Aggregates;

/// <summary>
/// Event-sourced aggregate representing the full lifecycle of a data subject's consent
/// for a specific processing purpose.
/// </summary>
/// <remarks>
/// <para>
/// Each aggregate instance represents consent for a single (DataSubjectId, Purpose) pair,
/// following GDPR Article 6(1)(a) which requires consent to be granular and purpose-specific.
/// The aggregate ID is deterministic from the subject and purpose, enabling natural lookup.
/// </para>
/// <para>
/// The lifecycle progresses through: <see cref="ConsentStatus.Active"/> →
/// <see cref="ConsentStatus.Withdrawn"/> or <see cref="ConsentStatus.Expired"/>,
/// with optional transitions through <see cref="ConsentStatus.RequiresReconsent"/>
/// when consent terms change.
/// </para>
/// <para>
/// All state changes are captured as immutable events, providing a complete audit trail
/// for GDPR Article 5(2) accountability requirements. Events implement <see cref="INotification"/>
/// and are automatically published by <c>EventPublishingPipelineBehavior</c> after successful
/// Marten commit, enabling downstream handlers to react to consent changes.
/// </para>
/// <para>
/// Version management is integrated into the aggregate — when consent terms change,
/// a <see cref="ConsentVersionChanged"/> event is raised, potentially requiring the data
/// subject to provide fresh consent via <see cref="ConsentReconsentProvided"/>.
/// </para>
/// </remarks>
public sealed class ConsentAggregate : AggregateBase
{
    /// <summary>
    /// Identifier of the data subject who gave consent.
    /// </summary>
    /// <remarks>
    /// Stable identifier for the data subject (e.g., user ID, customer number).
    /// Used together with <see cref="Purpose"/> to form the natural key for this aggregate.
    /// </remarks>
    public string DataSubjectId { get; private set; } = string.Empty;

    /// <summary>
    /// The specific processing purpose for which consent was given.
    /// </summary>
    /// <remarks>
    /// Purposes should be granular and specific as required by GDPR Article 6(1)(a).
    /// Use constants from <see cref="ConsentPurposes"/> for standard purposes,
    /// or define custom purpose strings for domain-specific needs.
    /// </remarks>
    public string Purpose { get; private set; } = string.Empty;

    /// <summary>
    /// Current lifecycle status of this consent.
    /// </summary>
    public ConsentStatus Status { get; private set; }

    /// <summary>
    /// Identifier of the consent version the data subject agreed to.
    /// </summary>
    /// <remarks>
    /// Updated when the data subject renews consent or provides reconsent under new terms.
    /// Links this consent to the specific terms the data subject was presented with.
    /// </remarks>
    public string ConsentVersionId { get; private set; } = string.Empty;

    /// <summary>
    /// The source or channel through which consent was most recently collected.
    /// </summary>
    /// <example>"web-form", "api", "mobile-app", "in-person", "email"</example>
    public string Source { get; private set; } = string.Empty;

    /// <summary>
    /// The IP address of the data subject at the time consent was most recently given.
    /// </summary>
    /// <remarks>
    /// <c>null</c> when consent is collected through channels where IP address is not available.
    /// </remarks>
    public string? IpAddress { get; private set; }

    /// <summary>
    /// Hash or reference to the consent form shown to the data subject.
    /// </summary>
    /// <remarks>
    /// Proof of what the data subject was presented with when they gave consent,
    /// as required by GDPR Article 7(1) for demonstrating consent.
    /// </remarks>
    public string? ProofOfConsent { get; private set; }

    /// <summary>
    /// Additional metadata associated with this consent record.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Metadata { get; private set; } = new Dictionary<string, object?>();

    /// <summary>
    /// Timestamp when consent was originally granted (UTC).
    /// </summary>
    public DateTimeOffset GivenAtUtc { get; private set; }

    /// <summary>
    /// Timestamp when the data subject withdrew consent (UTC).
    /// </summary>
    /// <remarks>
    /// <c>null</c> if consent has not been withdrawn.
    /// </remarks>
    public DateTimeOffset? WithdrawnAtUtc { get; private set; }

    /// <summary>
    /// Timestamp when this consent expires (UTC).
    /// </summary>
    /// <remarks>
    /// <c>null</c> if no expiration is set. Updated on renewal or reconsent.
    /// </remarks>
    public DateTimeOffset? ExpiresAtUtc { get; private set; }

    /// <summary>
    /// Reason provided by the data subject for withdrawing consent.
    /// </summary>
    /// <remarks>
    /// <c>null</c> if consent has not been withdrawn or no reason was provided.
    /// </remarks>
    public string? WithdrawalReason { get; private set; }

    /// <summary>
    /// Tenant identifier for multi-tenancy scoping.
    /// </summary>
    public string? TenantId { get; private set; }

    /// <summary>
    /// Module identifier for modular monolith scoping.
    /// </summary>
    public string? ModuleId { get; private set; }

    /// <summary>
    /// Grants consent for a specific processing purpose on behalf of a data subject.
    /// </summary>
    /// <remarks>
    /// Creates a new consent aggregate capturing all proof data required by GDPR Article 7(1).
    /// The aggregate starts in <see cref="ConsentStatus.Active"/> status.
    /// </remarks>
    /// <param name="id">Unique identifier for the new consent aggregate.</param>
    /// <param name="dataSubjectId">Identifier of the data subject giving consent.</param>
    /// <param name="purpose">The processing purpose for which consent is given.</param>
    /// <param name="consentVersionId">The version of consent terms the data subject agrees to.</param>
    /// <param name="source">The channel through which consent is collected.</param>
    /// <param name="ipAddress">IP address of the data subject, or <c>null</c> if unavailable.</param>
    /// <param name="proofOfConsent">Hash or reference to the consent form shown, or <c>null</c>.</param>
    /// <param name="metadata">Additional key-value metadata for this consent.</param>
    /// <param name="expiresAtUtc">When the consent expires (UTC), or <c>null</c> for no expiration.</param>
    /// <param name="grantedBy">Identifier of the actor recording the consent.</param>
    /// <param name="occurredAtUtc">Timestamp when consent was granted (UTC).</param>
    /// <param name="tenantId">Optional tenant identifier for multi-tenancy scoping.</param>
    /// <param name="moduleId">Optional module identifier for modular monolith scoping.</param>
    /// <returns>A new <see cref="ConsentAggregate"/> in <see cref="ConsentStatus.Active"/> status.</returns>
    public static ConsentAggregate Grant(
        Guid id,
        string dataSubjectId,
        string purpose,
        string consentVersionId,
        string source,
        string? ipAddress,
        string? proofOfConsent,
        IReadOnlyDictionary<string, object?> metadata,
        DateTimeOffset? expiresAtUtc,
        string grantedBy,
        DateTimeOffset occurredAtUtc,
        string? tenantId = null,
        string? moduleId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataSubjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
        ArgumentException.ThrowIfNullOrWhiteSpace(consentVersionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentException.ThrowIfNullOrWhiteSpace(grantedBy);

        var aggregate = new ConsentAggregate();
        aggregate.RaiseEvent(new ConsentGranted(
            id,
            dataSubjectId,
            purpose,
            consentVersionId,
            source,
            ipAddress,
            proofOfConsent,
            metadata,
            expiresAtUtc,
            grantedBy,
            occurredAtUtc,
            tenantId,
            moduleId));
        return aggregate;
    }

    /// <summary>
    /// Withdraws this consent on behalf of the data subject.
    /// </summary>
    /// <remarks>
    /// Per GDPR Article 7(3), withdrawing consent must be as easy as giving it.
    /// Transitions from <see cref="ConsentStatus.Active"/> to <see cref="ConsentStatus.Withdrawn"/>.
    /// </remarks>
    /// <param name="withdrawnBy">Identifier of the actor recording the withdrawal.</param>
    /// <param name="reason">Optional reason provided by the data subject for withdrawal.</param>
    /// <param name="occurredAtUtc">Timestamp when the withdrawal was recorded (UTC).</param>
    /// <exception cref="InvalidOperationException">Thrown when consent is not in <see cref="ConsentStatus.Active"/> or <see cref="ConsentStatus.RequiresReconsent"/> status.</exception>
    public void Withdraw(string withdrawnBy, string? reason, DateTimeOffset occurredAtUtc)
    {
        if (Status is not (ConsentStatus.Active or ConsentStatus.RequiresReconsent))
        {
            throw new InvalidOperationException(
                $"Cannot withdraw consent when it is in '{Status}' status. Withdrawal is only allowed from Active or RequiresReconsent status.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(withdrawnBy);

        RaiseEvent(new ConsentWithdrawn(Id, DataSubjectId, Purpose, withdrawnBy, reason, occurredAtUtc));
    }

    /// <summary>
    /// Expires this consent after it has passed its expiration date.
    /// </summary>
    /// <remarks>
    /// Typically triggered by a background processor that detects expired consents.
    /// Transitions from <see cref="ConsentStatus.Active"/> to <see cref="ConsentStatus.Expired"/>.
    /// </remarks>
    /// <param name="occurredAtUtc">Timestamp when the expiration was detected (UTC).</param>
    /// <exception cref="InvalidOperationException">Thrown when consent is not in <see cref="ConsentStatus.Active"/> status.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no expiration date is set on this consent.</exception>
    public void Expire(DateTimeOffset occurredAtUtc)
    {
        if (Status != ConsentStatus.Active)
        {
            throw new InvalidOperationException(
                $"Cannot expire consent when it is in '{Status}' status. Expiration is only allowed from Active status.");
        }

        if (ExpiresAtUtc is null)
        {
            throw new InvalidOperationException(
                "Cannot expire consent that has no expiration date set.");
        }

        RaiseEvent(new ConsentExpired(Id, DataSubjectId, Purpose, ExpiresAtUtc.Value, occurredAtUtc));
    }

    /// <summary>
    /// Renews the data subject's consent, extending its validity.
    /// </summary>
    /// <remarks>
    /// The data subject proactively re-confirms their consent, optionally updating the
    /// consent version and expiration date. This is distinct from
    /// <see cref="ProvideReconsent"/> which responds to a version change.
    /// GDPR Article 7(1) requires demonstrable proof — renewal provides fresh evidence.
    /// </remarks>
    /// <param name="consentVersionId">The consent version under which renewal is provided.</param>
    /// <param name="newExpiresAtUtc">The new expiration date (UTC), or <c>null</c> for no expiration.</param>
    /// <param name="renewedBy">Identifier of the actor recording the renewal.</param>
    /// <param name="source">Channel through which renewal was collected, or <c>null</c> if unchanged.</param>
    /// <param name="occurredAtUtc">Timestamp when the renewal was recorded (UTC).</param>
    /// <exception cref="InvalidOperationException">Thrown when consent is not in <see cref="ConsentStatus.Active"/> status.</exception>
    public void Renew(
        string consentVersionId,
        DateTimeOffset? newExpiresAtUtc,
        string renewedBy,
        string? source,
        DateTimeOffset occurredAtUtc)
    {
        if (Status != ConsentStatus.Active)
        {
            throw new InvalidOperationException(
                $"Cannot renew consent when it is in '{Status}' status. Renewal is only allowed from Active status.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(consentVersionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(renewedBy);

        RaiseEvent(new ConsentRenewed(Id, DataSubjectId, Purpose, consentVersionId, newExpiresAtUtc, renewedBy, source, occurredAtUtc));
    }

    /// <summary>
    /// Records a change in consent terms, potentially requiring the data subject to provide reconsent.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <paramref name="requiresReconsent"/> is <c>true</c>, the consent transitions to
    /// <see cref="ConsentStatus.RequiresReconsent"/> and the data subject must provide fresh
    /// consent via <see cref="ProvideReconsent"/> before processing can continue.
    /// </para>
    /// <para>
    /// When <c>false</c>, the consent remains <see cref="ConsentStatus.Active"/> (e.g., for
    /// minor clarifications that don't affect the scope of processing).
    /// </para>
    /// </remarks>
    /// <param name="newVersionId">The newly published consent version identifier.</param>
    /// <param name="description">Human-readable description of what changed.</param>
    /// <param name="requiresReconsent">Whether existing consent must be explicitly renewed under the new terms.</param>
    /// <param name="changedBy">Identifier of the actor who published the new version.</param>
    /// <param name="occurredAtUtc">Timestamp when the version change was recorded (UTC).</param>
    /// <exception cref="InvalidOperationException">Thrown when consent is not in <see cref="ConsentStatus.Active"/> status.</exception>
    public void ChangeVersion(
        string newVersionId,
        string description,
        bool requiresReconsent,
        string changedBy,
        DateTimeOffset occurredAtUtc)
    {
        if (Status != ConsentStatus.Active)
        {
            throw new InvalidOperationException(
                $"Cannot change consent version when it is in '{Status}' status. Version changes are only allowed from Active status.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(newVersionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(changedBy);

        RaiseEvent(new ConsentVersionChanged(
            Id, DataSubjectId, Purpose, ConsentVersionId, newVersionId, description, requiresReconsent, changedBy, occurredAtUtc));
    }

    /// <summary>
    /// Records the data subject providing fresh consent under updated terms after a version change.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Transitions the consent from <see cref="ConsentStatus.RequiresReconsent"/> back to
    /// <see cref="ConsentStatus.Active"/>. This event captures the same proof data as
    /// <see cref="ConsentGranted"/> because it represents a new act of consent that must be
    /// independently demonstrable under GDPR Article 7(1).
    /// </para>
    /// </remarks>
    /// <param name="newConsentVersionId">The new consent version the data subject agrees to.</param>
    /// <param name="source">The channel through which reconsent was collected.</param>
    /// <param name="ipAddress">IP address of the data subject, or <c>null</c> if unavailable.</param>
    /// <param name="proofOfConsent">Hash or reference to the consent form shown, or <c>null</c>.</param>
    /// <param name="metadata">Additional key-value metadata for this reconsent.</param>
    /// <param name="expiresAtUtc">When the reconsent expires (UTC), or <c>null</c> for no expiration.</param>
    /// <param name="grantedBy">Identifier of the actor recording the reconsent.</param>
    /// <param name="occurredAtUtc">Timestamp when reconsent was provided (UTC).</param>
    /// <exception cref="InvalidOperationException">Thrown when consent is not in <see cref="ConsentStatus.RequiresReconsent"/> status.</exception>
    public void ProvideReconsent(
        string newConsentVersionId,
        string source,
        string? ipAddress,
        string? proofOfConsent,
        IReadOnlyDictionary<string, object?> metadata,
        DateTimeOffset? expiresAtUtc,
        string grantedBy,
        DateTimeOffset occurredAtUtc)
    {
        if (Status != ConsentStatus.RequiresReconsent)
        {
            throw new InvalidOperationException(
                $"Cannot provide reconsent when consent is in '{Status}' status. Reconsent is only allowed from RequiresReconsent status.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(newConsentVersionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentException.ThrowIfNullOrWhiteSpace(grantedBy);

        RaiseEvent(new ConsentReconsentProvided(
            Id, DataSubjectId, Purpose, newConsentVersionId, source, ipAddress, proofOfConsent, metadata, expiresAtUtc, grantedBy, occurredAtUtc));
    }

    /// <inheritdoc />
    protected override void Apply(object domainEvent)
    {
        switch (domainEvent)
        {
            case ConsentGranted e:
                Id = e.ConsentId;
                DataSubjectId = e.DataSubjectId;
                Purpose = e.Purpose;
                ConsentVersionId = e.ConsentVersionId;
                Source = e.Source;
                IpAddress = e.IpAddress;
                ProofOfConsent = e.ProofOfConsent;
                Metadata = e.Metadata;
                ExpiresAtUtc = e.ExpiresAtUtc;
                GivenAtUtc = e.OccurredAtUtc;
                Status = ConsentStatus.Active;
                TenantId = e.TenantId;
                ModuleId = e.ModuleId;
                break;

            case ConsentWithdrawn e:
                Status = ConsentStatus.Withdrawn;
                WithdrawnAtUtc = e.OccurredAtUtc;
                WithdrawalReason = e.Reason;
                break;

            case ConsentExpired:
                Status = ConsentStatus.Expired;
                break;

            case ConsentRenewed e:
                ConsentVersionId = e.ConsentVersionId;
                ExpiresAtUtc = e.NewExpiresAtUtc;
                if (e.Source is not null)
                {
                    Source = e.Source;
                }

                break;

            case ConsentVersionChanged e:
                ConsentVersionId = e.NewVersionId;
                if (e.RequiresReconsent)
                {
                    Status = ConsentStatus.RequiresReconsent;
                }

                break;

            case ConsentReconsentProvided e:
                Status = ConsentStatus.Active;
                ConsentVersionId = e.NewConsentVersionId;
                Source = e.Source;
                IpAddress = e.IpAddress;
                ProofOfConsent = e.ProofOfConsent;
                Metadata = e.Metadata;
                ExpiresAtUtc = e.ExpiresAtUtc;
                WithdrawnAtUtc = null;
                WithdrawalReason = null;
                break;
        }
    }
}
