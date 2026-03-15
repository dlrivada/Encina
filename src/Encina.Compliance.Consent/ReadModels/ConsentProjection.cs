using Encina.Compliance.Consent.Events;
using Encina.Marten.Projections;

namespace Encina.Compliance.Consent.ReadModels;

/// <summary>
/// Marten inline projection that transforms consent aggregate events into <see cref="ConsentReadModel"/> state.
/// </summary>
/// <remarks>
/// <para>
/// This projection implements the "Query" side of CQRS for consent management. It handles all 6
/// consent event types, creating or updating the <see cref="ConsentReadModel"/> as events are applied.
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
    IProjectionHandler<ConsentReconsentProvided, ConsentReadModel>
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
        current.ConsentVersionId = domainEvent.ConsentVersionId;
        current.ExpiresAtUtc = domainEvent.NewExpiresAtUtc;
        if (domainEvent.Source is not null)
        {
            current.Source = domainEvent.Source;
        }

        current.LastModifiedAtUtc = domainEvent.OccurredAtUtc;
        current.Version++;
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
        current.Status = ConsentStatus.Active;
        current.ConsentVersionId = domainEvent.NewConsentVersionId;
        current.Source = domainEvent.Source;
        current.IpAddress = domainEvent.IpAddress;
        current.ProofOfConsent = domainEvent.ProofOfConsent;
        current.Metadata = domainEvent.Metadata;
        current.ExpiresAtUtc = domainEvent.ExpiresAtUtc;
        current.WithdrawnAtUtc = null;
        current.LastModifiedAtUtc = domainEvent.OccurredAtUtc;
        current.Version++;
        return current;
    }
}
