using Encina.Compliance.LawfulBasis.Events;
using Encina.Marten.Projections;

namespace Encina.Compliance.LawfulBasis.ReadModels;

/// <summary>
/// Marten inline projection that transforms lawful basis aggregate events into
/// <see cref="LawfulBasisReadModel"/> state.
/// </summary>
/// <remarks>
/// <para>
/// This projection implements the "Query" side of CQRS for lawful basis management.
/// It handles all 3 lawful basis event types, creating or updating the
/// <see cref="LawfulBasisReadModel"/> as events are applied.
/// </para>
/// <para>
/// <b>Event Handling</b>:
/// <list type="bullet">
///   <item><description><see cref="LawfulBasisRegistered"/> — Creates a new read model (first event in stream)</description></item>
///   <item><description><see cref="LawfulBasisChanged"/> — Updates basis, purpose, and supporting references</description></item>
///   <item><description><see cref="LawfulBasisRevoked"/> — Marks the registration as revoked (terminal state)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Idempotency</b>: The projection is idempotent — applying the same event multiple times
/// produces the same result. This enables safe replay and rebuild of the read model.
/// </para>
/// </remarks>
public sealed class LawfulBasisProjection :
    IProjection<LawfulBasisReadModel>,
    IProjectionCreator<LawfulBasisRegistered, LawfulBasisReadModel>,
    IProjectionHandler<LawfulBasisChanged, LawfulBasisReadModel>,
    IProjectionHandler<LawfulBasisRevoked, LawfulBasisReadModel>
{
    /// <inheritdoc />
    public string ProjectionName => "LawfulBasisProjection";

    /// <summary>
    /// Creates a new <see cref="LawfulBasisReadModel"/> from a <see cref="LawfulBasisRegistered"/> event.
    /// </summary>
    /// <remarks>
    /// This is the first event in a lawful basis aggregate stream. It initializes all fields
    /// including the Article 6(1) basis and any supporting documentation references.
    /// </remarks>
    /// <param name="domainEvent">The lawful basis registered event.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>A new <see cref="LawfulBasisReadModel"/> in active state.</returns>
    public LawfulBasisReadModel Create(LawfulBasisRegistered domainEvent, ProjectionContext context)
    {
        return new LawfulBasisReadModel
        {
            Id = domainEvent.RegistrationId,
            RequestTypeName = domainEvent.RequestTypeName,
            Basis = domainEvent.Basis,
            Purpose = domainEvent.Purpose,
            LIAReference = domainEvent.LIAReference,
            LegalReference = domainEvent.LegalReference,
            ContractReference = domainEvent.ContractReference,
            RegisteredAtUtc = domainEvent.RegisteredAtUtc,
            TenantId = domainEvent.TenantId,
            ModuleId = domainEvent.ModuleId,
            LastModifiedAtUtc = domainEvent.RegisteredAtUtc,
            Version = 1
        };
    }

    /// <summary>
    /// Updates the read model when the lawful basis is changed to a different Article 6(1) ground.
    /// </summary>
    /// <remarks>
    /// Updates the basis, purpose, and all supporting references (LIA, legal, contract).
    /// The old basis is captured in the event for audit trail purposes but is not stored
    /// in the read model — the full history is available from the event stream.
    /// </remarks>
    /// <param name="domainEvent">The lawful basis changed event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model.</returns>
    public LawfulBasisReadModel Apply(LawfulBasisChanged domainEvent, LawfulBasisReadModel current, ProjectionContext context)
    {
        current.Basis = domainEvent.NewBasis;
        current.Purpose = domainEvent.Purpose;
        current.LIAReference = domainEvent.LIAReference;
        current.LegalReference = domainEvent.LegalReference;
        current.ContractReference = domainEvent.ContractReference;
        current.LastModifiedAtUtc = domainEvent.ChangedAtUtc;
        current.Version++;
        return current;
    }

    /// <summary>
    /// Updates the read model when the lawful basis registration is revoked.
    /// </summary>
    /// <remarks>
    /// Marks the registration as revoked with the provided reason.
    /// Revocation is a terminal state — processing for the affected request type
    /// should cease as there is no longer a declared lawful basis under Article 6(1).
    /// </remarks>
    /// <param name="domainEvent">The lawful basis revoked event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model.</returns>
    public LawfulBasisReadModel Apply(LawfulBasisRevoked domainEvent, LawfulBasisReadModel current, ProjectionContext context)
    {
        current.IsRevoked = true;
        current.RevocationReason = domainEvent.Reason;
        current.LastModifiedAtUtc = domainEvent.RevokedAtUtc;
        current.Version++;
        return current;
    }
}
