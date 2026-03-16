using Encina.Compliance.GDPR;

// Event-sourced events implement INotification so they are automatically published
// by Encina.Marten's EventPublishingPipelineBehavior after successful command execution.
// This allows handlers to react to aggregate state changes without a separate notification layer.

namespace Encina.Compliance.LawfulBasis.Events;

/// <summary>
/// Raised when a lawful basis is registered for a specific request type under GDPR Article 6(1).
/// </summary>
/// <remarks>
/// <para>
/// Initiates the lawful basis registration lifecycle. Each registration maps an Encina request type
/// to one of the six lawful bases defined in Article 6(1), along with optional references
/// to supporting documentation (LIA reference for legitimate interests, legal reference for
/// legal obligations, contract reference for contractual necessity).
/// </para>
/// <para>
/// GDPR Article 5(2) accountability: this event provides an immutable record of when and why
/// a particular lawful basis was determined for a processing operation.
/// </para>
/// </remarks>
/// <param name="RegistrationId">Unique identifier for this lawful basis registration.</param>
/// <param name="RequestTypeName">The assembly-qualified name of the request type this registration applies to.</param>
/// <param name="Basis">The lawful basis for processing under Article 6(1).</param>
/// <param name="Purpose">The purpose of the processing, or <c>null</c> if not specified.</param>
/// <param name="LIAReference">Reference to a Legitimate Interest Assessment, required when <paramref name="Basis"/> is <see cref="GDPR.LawfulBasis.LegitimateInterests"/>.</param>
/// <param name="LegalReference">Reference to the specific legal provision, expected when <paramref name="Basis"/> is <see cref="GDPR.LawfulBasis.LegalObligation"/>.</param>
/// <param name="ContractReference">Reference to the contract or pre-contractual steps, expected when <paramref name="Basis"/> is <see cref="GDPR.LawfulBasis.Contract"/>.</param>
/// <param name="RegisteredAtUtc">Timestamp when the registration was created (UTC).</param>
/// <param name="TenantId">Tenant identifier for multi-tenancy scoping.</param>
/// <param name="ModuleId">Module identifier for modular monolith scoping.</param>
public sealed record LawfulBasisRegistered(
    Guid RegistrationId,
    string RequestTypeName,
    GDPR.LawfulBasis Basis,
    string? Purpose,
    string? LIAReference,
    string? LegalReference,
    string? ContractReference,
    DateTimeOffset RegisteredAtUtc,
    string? TenantId,
    string? ModuleId) : INotification;

/// <summary>
/// Raised when the lawful basis for a request type is changed to a different Article 6(1) ground.
/// </summary>
/// <remarks>
/// <para>
/// Records a change in the legal ground for processing. GDPR guidance indicates that once a
/// lawful basis is determined, it should generally not be swapped for another. However, in
/// pre-production systems or when processing purposes genuinely evolve, basis changes are
/// legitimate and must be documented for accountability (Art. 5(2)).
/// </para>
/// <para>
/// The event captures both the old and new basis to maintain a complete audit trail of
/// how the legal ground for a processing operation has evolved over time.
/// </para>
/// </remarks>
/// <param name="RegistrationId">The registration identifier.</param>
/// <param name="OldBasis">The previous lawful basis that was in effect.</param>
/// <param name="NewBasis">The new lawful basis being applied.</param>
/// <param name="Purpose">The updated purpose of the processing, or <c>null</c> if unchanged.</param>
/// <param name="LIAReference">Updated LIA reference, or <c>null</c> if not applicable to the new basis.</param>
/// <param name="LegalReference">Updated legal reference, or <c>null</c> if not applicable to the new basis.</param>
/// <param name="ContractReference">Updated contract reference, or <c>null</c> if not applicable to the new basis.</param>
/// <param name="ChangedAtUtc">Timestamp when the basis was changed (UTC).</param>
/// <param name="TenantId">Tenant identifier for multi-tenancy scoping.</param>
/// <param name="ModuleId">Module identifier for modular monolith scoping.</param>
public sealed record LawfulBasisChanged(
    Guid RegistrationId,
    GDPR.LawfulBasis OldBasis,
    GDPR.LawfulBasis NewBasis,
    string? Purpose,
    string? LIAReference,
    string? LegalReference,
    string? ContractReference,
    DateTimeOffset ChangedAtUtc,
    string? TenantId,
    string? ModuleId) : INotification;

/// <summary>
/// Raised when a lawful basis registration is revoked, indicating that the request type
/// no longer has a declared legal ground for processing.
/// </summary>
/// <remarks>
/// <para>
/// Revocation is a terminal state — once revoked, the registration cannot be reactivated.
/// A new registration must be created if the request type requires a lawful basis again.
/// </para>
/// <para>
/// Processing operations for the affected request type should cease or be re-evaluated
/// after revocation, as there is no longer a declared lawful basis under Article 6(1).
/// </para>
/// </remarks>
/// <param name="RegistrationId">The registration identifier.</param>
/// <param name="Reason">Explanation of why the lawful basis registration was revoked.</param>
/// <param name="RevokedAtUtc">Timestamp when the registration was revoked (UTC).</param>
/// <param name="TenantId">Tenant identifier for multi-tenancy scoping.</param>
/// <param name="ModuleId">Module identifier for modular monolith scoping.</param>
public sealed record LawfulBasisRevoked(
    Guid RegistrationId,
    string Reason,
    DateTimeOffset RevokedAtUtc,
    string? TenantId,
    string? ModuleId) : INotification;
