using Encina.Compliance.LawfulBasis.Events;
using Encina.DomainModeling;

namespace Encina.Compliance.LawfulBasis.Aggregates;

/// <summary>
/// Event-sourced aggregate representing the lifecycle of a lawful basis registration
/// for a specific request type under GDPR Article 6(1).
/// </summary>
/// <remarks>
/// <para>
/// Each aggregate instance maps an Encina request type to one of the six lawful bases
/// defined in Article 6(1), with optional references to supporting documentation
/// (LIA reference for legitimate interests, legal reference for legal obligations,
/// contract reference for contractual necessity).
/// </para>
/// <para>
/// The lifecycle progresses through: Active (registered) → optionally Changed → Revoked.
/// Once revoked, the registration cannot be reactivated — a new registration must be
/// created if the request type requires a lawful basis again.
/// </para>
/// <para>
/// All state changes are captured as immutable events, providing a complete audit trail
/// for GDPR Art. 5(2) accountability requirements.
/// </para>
/// </remarks>
public sealed class LawfulBasisAggregate : AggregateBase
{
    /// <summary>
    /// The assembly-qualified name of the request type this registration applies to.
    /// </summary>
    public string RequestTypeName { get; private set; } = string.Empty;

    /// <summary>
    /// The current lawful basis for processing under Article 6(1).
    /// </summary>
    public GDPR.LawfulBasis Basis { get; private set; }

    /// <summary>
    /// The purpose of the processing.
    /// </summary>
    public string? Purpose { get; private set; }

    /// <summary>
    /// Reference to a Legitimate Interest Assessment, when basis is
    /// <see cref="GDPR.LawfulBasis.LegitimateInterests"/>.
    /// </summary>
    public string? LIAReference { get; private set; }

    /// <summary>
    /// Reference to the specific legal provision, when basis is
    /// <see cref="GDPR.LawfulBasis.LegalObligation"/>.
    /// </summary>
    public string? LegalReference { get; private set; }

    /// <summary>
    /// Reference to the contract or pre-contractual steps, when basis is
    /// <see cref="GDPR.LawfulBasis.Contract"/>.
    /// </summary>
    public string? ContractReference { get; private set; }

    /// <summary>
    /// Whether this registration has been revoked.
    /// </summary>
    public bool IsRevoked { get; private set; }

    /// <summary>
    /// Reason for revocation, if applicable.
    /// </summary>
    public string? RevocationReason { get; private set; }

    /// <summary>
    /// Tenant identifier for multi-tenancy scoping.
    /// </summary>
    public string? TenantId { get; private set; }

    /// <summary>
    /// Module identifier for modular monolith scoping.
    /// </summary>
    public string? ModuleId { get; private set; }

    /// <summary>
    /// Registers a new lawful basis for a specific request type under GDPR Article 6(1).
    /// </summary>
    /// <param name="registrationId">Unique identifier for the new registration.</param>
    /// <param name="requestTypeName">The assembly-qualified name of the request type.</param>
    /// <param name="basis">The lawful basis for processing.</param>
    /// <param name="purpose">The purpose of the processing, or <c>null</c> if not specified.</param>
    /// <param name="liaReference">LIA reference, required when <paramref name="basis"/> is <see cref="GDPR.LawfulBasis.LegitimateInterests"/>.</param>
    /// <param name="legalReference">Legal reference, expected when <paramref name="basis"/> is <see cref="GDPR.LawfulBasis.LegalObligation"/>.</param>
    /// <param name="contractReference">Contract reference, expected when <paramref name="basis"/> is <see cref="GDPR.LawfulBasis.Contract"/>.</param>
    /// <param name="registeredAtUtc">Timestamp when the registration is created (UTC).</param>
    /// <param name="tenantId">Optional tenant identifier for multi-tenancy scoping.</param>
    /// <param name="moduleId">Optional module identifier for modular monolith scoping.</param>
    /// <returns>A new <see cref="LawfulBasisAggregate"/> in active state.</returns>
    public static LawfulBasisAggregate Register(
        Guid registrationId,
        string requestTypeName,
        GDPR.LawfulBasis basis,
        string? purpose,
        string? liaReference,
        string? legalReference,
        string? contractReference,
        DateTimeOffset registeredAtUtc,
        string? tenantId = null,
        string? moduleId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestTypeName);

        var aggregate = new LawfulBasisAggregate();
        aggregate.RaiseEvent(new LawfulBasisRegistered(
            registrationId,
            requestTypeName,
            basis,
            purpose,
            liaReference,
            legalReference,
            contractReference,
            registeredAtUtc,
            tenantId,
            moduleId));
        return aggregate;
    }

    /// <summary>
    /// Changes the lawful basis for this registration to a different Article 6(1) ground.
    /// </summary>
    /// <remarks>
    /// GDPR guidance indicates that once a lawful basis is determined, it should generally not
    /// be swapped. However, in pre-production systems or when processing purposes genuinely evolve,
    /// basis changes are legitimate and must be documented for accountability (Art. 5(2)).
    /// </remarks>
    /// <param name="newBasis">The new lawful basis being applied.</param>
    /// <param name="purpose">The updated purpose, or <c>null</c> if unchanged.</param>
    /// <param name="liaReference">Updated LIA reference, or <c>null</c> if not applicable.</param>
    /// <param name="legalReference">Updated legal reference, or <c>null</c> if not applicable.</param>
    /// <param name="contractReference">Updated contract reference, or <c>null</c> if not applicable.</param>
    /// <param name="changedAtUtc">Timestamp when the basis was changed (UTC).</param>
    /// <exception cref="InvalidOperationException">Thrown when the registration has been revoked.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the new basis is the same as the current basis.</exception>
    public void ChangeBasis(
        GDPR.LawfulBasis newBasis,
        string? purpose,
        string? liaReference,
        string? legalReference,
        string? contractReference,
        DateTimeOffset changedAtUtc)
    {
        if (IsRevoked)
        {
            throw new InvalidOperationException(
                "Cannot change the lawful basis of a revoked registration. Create a new registration instead.");
        }

        if (newBasis == Basis)
        {
            throw new InvalidOperationException(
                $"The new basis '{newBasis}' is the same as the current basis. No change is needed.");
        }

        RaiseEvent(new LawfulBasisChanged(
            Id,
            Basis,
            newBasis,
            purpose,
            liaReference,
            legalReference,
            contractReference,
            changedAtUtc,
            TenantId,
            ModuleId));
    }

    /// <summary>
    /// Revokes this lawful basis registration, indicating that the request type
    /// no longer has a declared legal ground for processing.
    /// </summary>
    /// <remarks>
    /// Revocation is a terminal state — once revoked, the registration cannot be reactivated.
    /// Processing operations for the affected request type should cease or be re-evaluated.
    /// </remarks>
    /// <param name="reason">Explanation of why the registration is being revoked.</param>
    /// <param name="revokedAtUtc">Timestamp when the registration is revoked (UTC).</param>
    /// <exception cref="InvalidOperationException">Thrown when the registration has already been revoked.</exception>
    public void Revoke(string reason, DateTimeOffset revokedAtUtc)
    {
        if (IsRevoked)
        {
            throw new InvalidOperationException(
                "This lawful basis registration has already been revoked.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        RaiseEvent(new LawfulBasisRevoked(Id, reason, revokedAtUtc, TenantId, ModuleId));
    }

    /// <inheritdoc />
    protected override void Apply(object domainEvent)
    {
        switch (domainEvent)
        {
            case LawfulBasisRegistered e:
                Id = e.RegistrationId;
                RequestTypeName = e.RequestTypeName;
                Basis = e.Basis;
                Purpose = e.Purpose;
                LIAReference = e.LIAReference;
                LegalReference = e.LegalReference;
                ContractReference = e.ContractReference;
                TenantId = e.TenantId;
                ModuleId = e.ModuleId;
                break;

            case LawfulBasisChanged e:
                Basis = e.NewBasis;
                Purpose = e.Purpose;
                LIAReference = e.LIAReference;
                LegalReference = e.LegalReference;
                ContractReference = e.ContractReference;
                break;

            case LawfulBasisRevoked e:
                IsRevoked = true;
                RevocationReason = e.Reason;
                break;
        }
    }
}
