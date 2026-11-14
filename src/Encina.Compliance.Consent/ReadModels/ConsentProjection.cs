using Encina.Compliance.Consent.Aggregates;
using Encina.Compliance.Consent.Events;
using Encina.Marten.Projections;

namespace Encina.Compliance.Consent.ReadModels;

/// <summary>
/// Marten inline projection that transforms consent aggregate events into <see cref="ConsentReadModel"/> state.
/// </summary>
/// <remarks>
/// <para>
/// This projection implements the "Query" side of CQRS for consent management. It handles all 8
/// consent event types, creating or updating the <see cref="ConsentReadModel"/> as events are applied.
/// </para>
/// <para>
/// <b>Act ordering (#3771)</b>: acts of consent (<see cref="ConsentRenewed"/>,
/// <see cref="ConsentReconsentProvided"/>, <see cref="ConsentRenewedByGrant"/>,
/// <see cref="ConsentRegranted"/>) older than <see cref="ConsentReadModel.LastActAtUtc"/> only
/// advance the version; withdrawals, expiries and version changes always apply and advance the
/// watermark. The rule is the same as in <see cref="ConsentAggregate"/>, so the read model and the
/// aggregate derive the same state from the same stream.
/// </para>
/// <para>
/// <b>Event Handling</b>:
/// <list type="bullet">
///   <item><description><see cref="ConsentGranted"/> — Creates a new read model (first event in stream)</description></item>
///   <item><description><see cref="ConsentWithdrawn"/> — Updates status to <see cref="ConsentStatus.Withdrawn"/></description></item>
///   <item><description><see cref="ConsentExpired"/> — Updates status to <see cref="ConsentStatus.Expired"/></description></item>
///   <item><description><see cref="ConsentRenewed"/> — Updates version, expiry, and source</description></item>
///   <item><description><see cref="ConsentVersionChanged"/> — Updates version; sets <see cref="ConsentStatus.RequiresReconsent"/> if needed</description></item>
///   <item><description><see cref="ConsentReconsentProvided"/> — Reactivates consent under new terms</description></item>
///   <item><description><see cref="ConsentRenewedByGrant"/> — Records a repeated grant on an active consent with its fresh proof</description></item>
///   <item><description><see cref="ConsentRegranted"/> — Reactivates a withdrawn or expired consent with fresh proof</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Idempotency</b>: The projection is idempotent — applying the same event multiple times
/// produces the same result. This enables safe replay and rebuild of the read model.
/// </para>
/// </remarks>
public sealed class ConsentProjection :
    IProjection<ConsentReadModel>,
    IProjectionCreator<ConsentGranted, ConsentReadModel>,
    IProjectionHandler<ConsentWithdrawn, ConsentReadModel>,
    IProjectionHandler<ConsentExpired, ConsentReadModel>,
    IProjectionHandler<ConsentRenewed, ConsentReadModel>,
    IProjectionHandler<ConsentVersionChanged, ConsentReadModel>,
    IProjectionHandler<ConsentReconsentProvided, ConsentReadModel>,
    IProjectionHandler<ConsentRenewedByGrant, ConsentReadModel>,
    IProjectionHandler<ConsentRegranted, ConsentReadModel>
{
    /// <inheritdoc />
    public string ProjectionName => "ConsentProjection";

    /// <summary>
    /// Creates a new <see cref="ConsentReadModel"/> from a <see cref="ConsentGranted"/> event.
    /// </summary>
    /// <remarks>
    /// This is the first event in a consent aggregate stream. It initializes all fields
    /// including the GDPR Article 7(1) proof data (source, IP address, proof of consent).
    /// </remarks>
    /// <param name="domainEvent">The consent granted event.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>A new <see cref="ConsentReadModel"/> in <see cref="ConsentStatus.Active"/> status.</returns>
    public ConsentReadModel Create(ConsentGranted domainEvent, ProjectionContext context)
    {
        return new ConsentReadModel
        {
            Id = domainEvent.ConsentId,
            DataSubjectId = domainEvent.DataSubjectId,
            Purpose = domainEvent.Purpose,
            Status = ConsentStatus.Active,
            ConsentVersionId = domainEvent.ConsentVersionId,
            GivenAtUtc = domainEvent.OccurredAtUtc,
            ExpiresAtUtc = domainEvent.ExpiresAtUtc,
            Source = domainEvent.Source,
            IpAddress = domainEvent.IpAddress,
            ProofOfConsent = domainEvent.ProofOfConsent,
            Metadata = domainEvent.Metadata,
            TenantId = domainEvent.TenantId,
            ModuleId = domainEvent.ModuleId,
            LastModifiedAtUtc = domainEvent.OccurredAtUtc,
            LastActAtUtc = domainEvent.OccurredAtUtc,
            Version = 1
        };
    }

    /// <summary>
    /// Updates the read model when consent is withdrawn.
    /// </summary>
    /// <remarks>
    /// Sets status to <see cref="ConsentStatus.Withdrawn"/> and records the withdrawal timestamp.
    /// Per GDPR Article 7(3), downstream systems should stop processing data for this purpose.
    /// </remarks>
    /// <param name="domainEvent">The consent withdrawn event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model.</returns>
    public ConsentReadModel Apply(ConsentWithdrawn domainEvent, ConsentReadModel current, ProjectionContext context)
    {
        current.Status = ConsentStatus.Withdrawn;
        current.WithdrawnAtUtc = domainEvent.OccurredAtUtc;
        current.LastModifiedAtUtc = domainEvent.OccurredAtUtc;
        current.LastActAtUtc = ConsentAggregate.AdvanceWatermark(current.LastActAtUtc, domainEvent.OccurredAtUtc);
        current.Version++;
        return current;
    }

    /// <summary>
    /// Updates the read model when consent expires.
    /// </summary>
    /// <remarks>
    /// Sets status to <see cref="ConsentStatus.Expired"/>. Unlike withdrawal, expiration
    /// is a passive process — the consent reached its time limit.
    /// </remarks>
    /// <param name="domainEvent">The consent expired event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model.</returns>
    public ConsentReadModel Apply(ConsentExpired domainEvent, ConsentReadModel current, ProjectionContext context)
    {
        current.Status = ConsentStatus.Expired;
        current.LastModifiedAtUtc = domainEvent.OccurredAtUtc;
        current.LastActAtUtc = ConsentAggregate.AdvanceWatermark(current.LastActAtUtc, domainEvent.ExpiredAtUtc);
        current.Version++;
        return current;
    }

    /// <summary>
    /// Updates the read model when consent is renewed.
    /// </summary>
    /// <remarks>
    /// Updates the consent version, expiration, and optionally the source.
    /// The consent remains in <see cref="ConsentStatus.Active"/> status.
    /// </remarks>
    /// <param name="domainEvent">The consent renewed event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model.</returns>
    public ConsentReadModel Apply(ConsentRenewed domainEvent, ConsentReadModel current, ProjectionContext context)
    {
        current.Version++;

        // A renewal older than the latest act is recorded without changing the state (#3771).
        if (ConsentAggregate.IsLateAct(domainEvent.OccurredAtUtc, current.LastActAtUtc, current.Status))
        {
            return current;
        }

        current.ConsentVersionId = domainEvent.ConsentVersionId;
        current.ExpiresAtUtc = domainEvent.NewExpiresAtUtc;
        if (domainEvent.Source is not null)
        {
            current.Source = domainEvent.Source;
        }

        current.LastModifiedAtUtc = domainEvent.OccurredAtUtc;
        current.LastActAtUtc = domainEvent.OccurredAtUtc;
        return current;
    }

    /// <summary>
    /// Updates the read model when consent terms change.
    /// </summary>
    /// <remarks>
    /// Updates the consent version. When <see cref="ConsentVersionChanged.RequiresReconsent"/>
    /// is <c>true</c>, transitions to <see cref="ConsentStatus.RequiresReconsent"/> — the data
    /// subject must provide fresh consent under the new terms before processing can continue.
    /// </remarks>
    /// <param name="domainEvent">The consent version changed event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model.</returns>
    public ConsentReadModel Apply(ConsentVersionChanged domainEvent, ConsentReadModel current, ProjectionContext context)
    {
        current.ConsentVersionId = domainEvent.NewVersionId;
        if (domainEvent.RequiresReconsent)
        {
            current.Status = ConsentStatus.RequiresReconsent;
        }

        current.LastModifiedAtUtc = domainEvent.OccurredAtUtc;
        current.LastActAtUtc = ConsentAggregate.AdvanceWatermark(current.LastActAtUtc, domainEvent.OccurredAtUtc);
        current.Version++;
        return current;
    }

    /// <summary>
    /// Updates the read model when the data subject provides reconsent under updated terms.
    /// </summary>
    /// <remarks>
    /// Transitions back to <see cref="ConsentStatus.Active"/> and captures fresh proof data
    /// as required by GDPR Article 7(1). Clears any previous withdrawal state.
    /// </remarks>
    /// <param name="domainEvent">The reconsent provided event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model.</returns>
    public ConsentReadModel Apply(ConsentReconsentProvided domainEvent, ConsentReadModel current, ProjectionContext context)
    {
        ApplyGrantAct(
            current, domainEvent.NewConsentVersionId, domainEvent.Source, domainEvent.IpAddress,
            domainEvent.ProofOfConsent, domainEvent.Metadata, domainEvent.ExpiresAtUtc, domainEvent.OccurredAtUtc);
        current.Version++;
        return current;
    }

    /// <summary>
    /// Updates the read model when a repeated grant renews an active consent (#3771).
    /// </summary>
    /// <remarks>
    /// Captures the fresh proof of the grant act. An act older than
    /// <see cref="ConsentReadModel.LastActAtUtc"/> only advances <see cref="ConsentReadModel.Version"/>.
    /// </remarks>
    /// <param name="domainEvent">The consent renewed by grant event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model.</returns>
    public ConsentReadModel Apply(ConsentRenewedByGrant domainEvent, ConsentReadModel current, ProjectionContext context)
    {
        ApplyGrantAct(
            current, domainEvent.ConsentVersionId, domainEvent.Source, domainEvent.IpAddress,
            domainEvent.ProofOfConsent, domainEvent.Metadata, domainEvent.ExpiresAtUtc, domainEvent.OccurredAtUtc);
        current.Version++;
        return current;
    }

    /// <summary>
    /// Updates the read model when a withdrawn or expired consent is granted again (#3771).
    /// </summary>
    /// <remarks>
    /// Returns the consent to <see cref="ConsentStatus.Active"/> with the fresh proof, version and
    /// expiry, clears the withdrawal timestamp and starts a new consent period
    /// (<see cref="ConsentReadModel.GivenAtUtc"/>). An act older than
    /// <see cref="ConsentReadModel.LastActAtUtc"/> only advances <see cref="ConsentReadModel.Version"/>,
    /// so a late grant never undoes a later withdrawal.
    /// </remarks>
    /// <param name="domainEvent">The consent regranted event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model.</returns>
    public ConsentReadModel Apply(ConsentRegranted domainEvent, ConsentReadModel current, ProjectionContext context)
    {
        if (ApplyGrantAct(
            current, domainEvent.ConsentVersionId, domainEvent.Source, domainEvent.IpAddress,
            domainEvent.ProofOfConsent, domainEvent.Metadata, domainEvent.ExpiresAtUtc, domainEvent.OccurredAtUtc))
        {
            current.GivenAtUtc = domainEvent.OccurredAtUtc;
        }

        current.Version++;
        return current;
    }

    /// <summary>
    /// Applies an act of consent carrying full proof unless it is older than the latest act
    /// (#3771). Returns whether the state changed.
    /// </summary>
    private static bool ApplyGrantAct(
        ConsentReadModel current,
        string consentVersionId,
        string source,
        string? ipAddress,
        string? proofOfConsent,
        IReadOnlyDictionary<string, object?> metadata,
        DateTimeOffset? expiresAtUtc,
        DateTimeOffset occurredAtUtc)
    {
        if (ConsentAggregate.IsLateAct(occurredAtUtc, current.LastActAtUtc, current.Status))
        {
            return false;
        }

        current.Status = ConsentStatus.Active;
        current.ConsentVersionId = consentVersionId;
        current.Source = source;
        current.IpAddress = ipAddress;
        current.ProofOfConsent = proofOfConsent;
        current.Metadata = metadata;
        current.ExpiresAtUtc = expiresAtUtc;
        current.WithdrawnAtUtc = null;
        current.LastModifiedAtUtc = occurredAtUtc;
        current.LastActAtUtc = occurredAtUtc;
        return true;
    }
}
