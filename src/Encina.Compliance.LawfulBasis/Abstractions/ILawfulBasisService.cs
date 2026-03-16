using Encina.Compliance.LawfulBasis.ReadModels;
using LanguageExt;

namespace Encina.Compliance.LawfulBasis.Abstractions;

/// <summary>
/// Service interface for managing lawful basis registrations and Legitimate Interest Assessments (LIA).
/// </summary>
/// <remarks>
/// <para>
/// This unified service replaces the three separate interfaces from the entity-based model:
/// <c>ILawfulBasisRegistry</c>, <c>ILIAStore</c>, and <c>ILegitimateInterestAssessment</c>.
/// All operations are backed by event-sourced aggregates via Marten.
/// </para>
/// <para>
/// <b>Registration Commands</b>: Create, change, and revoke lawful basis registrations
/// that map request types to GDPR Article 6(1) legal grounds.
/// </para>
/// <para>
/// <b>LIA Commands</b>: Create, approve, reject, and schedule reviews for Legitimate
/// Interest Assessments following the EDPB three-part test.
/// </para>
/// <para>
/// <b>Queries</b>: Retrieve read models projected from event streams for efficient querying.
/// </para>
/// <para>
/// All methods follow Railway Oriented Programming, returning
/// <c>ValueTask&lt;Either&lt;EncinaError, T&gt;&gt;</c> for explicit error handling.
/// </para>
/// </remarks>
public interface ILawfulBasisService
{
    // ========================================================================
    // Registration Commands
    // ========================================================================

    /// <summary>
    /// Registers a new lawful basis for a specific request type under GDPR Article 6(1).
    /// </summary>
    /// <param name="id">Unique identifier for the new registration.</param>
    /// <param name="requestTypeName">The assembly-qualified name of the request type.</param>
    /// <param name="basis">The lawful basis for processing.</param>
    /// <param name="purpose">The purpose of the processing, or <c>null</c> if not specified.</param>
    /// <param name="liaReference">LIA reference, required when <paramref name="basis"/> is <see cref="GDPR.LawfulBasis.LegitimateInterests"/>.</param>
    /// <param name="legalReference">Legal reference, expected when <paramref name="basis"/> is <see cref="GDPR.LawfulBasis.LegalObligation"/>.</param>
    /// <param name="contractReference">Contract reference, expected when <paramref name="basis"/> is <see cref="GDPR.LawfulBasis.Contract"/>.</param>
    /// <param name="tenantId">Optional tenant identifier for multi-tenancy scoping.</param>
    /// <param name="moduleId">Optional module identifier for modular monolith scoping.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or the identifier of the newly registered lawful basis.</returns>
    ValueTask<Either<EncinaError, Guid>> RegisterAsync(
        Guid id,
        string requestTypeName,
        GDPR.LawfulBasis basis,
        string? purpose,
        string? liaReference,
        string? legalReference,
        string? contractReference,
        string? tenantId = null,
        string? moduleId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes the lawful basis for an existing registration to a different Article 6(1) ground.
    /// </summary>
    /// <param name="registrationId">The registration identifier to change.</param>
    /// <param name="newBasis">The new lawful basis being applied.</param>
    /// <param name="purpose">The updated purpose, or <c>null</c> if unchanged.</param>
    /// <param name="liaReference">Updated LIA reference, or <c>null</c> if not applicable.</param>
    /// <param name="legalReference">Updated legal reference, or <c>null</c> if not applicable.</param>
    /// <param name="contractReference">Updated contract reference, or <c>null</c> if not applicable.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    ValueTask<Either<EncinaError, Unit>> ChangeBasisAsync(
        Guid registrationId,
        GDPR.LawfulBasis newBasis,
        string? purpose,
        string? liaReference,
        string? legalReference,
        string? contractReference,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a lawful basis registration, indicating that the request type no longer
    /// has a declared legal ground for processing.
    /// </summary>
    /// <param name="registrationId">The registration identifier to revoke.</param>
    /// <param name="reason">Explanation of why the registration is being revoked.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    ValueTask<Either<EncinaError, Unit>> RevokeAsync(
        Guid registrationId,
        string reason,
        CancellationToken cancellationToken = default);

    // ========================================================================
    // LIA Commands
    // ========================================================================

    /// <summary>
    /// Creates a new Legitimate Interest Assessment following the EDPB three-part test.
    /// </summary>
    /// <param name="id">Unique identifier for the new LIA.</param>
    /// <param name="reference">Document reference identifier (e.g., "LIA-2024-FRAUD-001").</param>
    /// <param name="name">Human-readable name for this LIA.</param>
    /// <param name="purpose">The processing purpose this LIA covers.</param>
    /// <param name="legitimateInterest">Description of the legitimate interest (Purpose Test).</param>
    /// <param name="benefits">Benefits of the processing (Purpose Test).</param>
    /// <param name="consequencesIfNotProcessed">Consequences of not processing (Purpose Test).</param>
    /// <param name="necessityJustification">Justification for necessity (Necessity Test).</param>
    /// <param name="alternativesConsidered">Alternatives evaluated (Necessity Test).</param>
    /// <param name="dataMinimisationNotes">Data minimisation measures (Necessity Test).</param>
    /// <param name="natureOfData">Nature of the personal data (Balancing Test).</param>
    /// <param name="reasonableExpectations">Data subject's reasonable expectations (Balancing Test).</param>
    /// <param name="impactAssessment">Impact on data subjects' rights (Balancing Test).</param>
    /// <param name="safeguards">Safeguards to mitigate impact (Balancing Test).</param>
    /// <param name="assessedBy">Name or role of the assessor.</param>
    /// <param name="dpoInvolvement">Whether the DPO was consulted.</param>
    /// <param name="conditions">Any conditions attached, or <c>null</c>.</param>
    /// <param name="tenantId">Optional tenant identifier for multi-tenancy scoping.</param>
    /// <param name="moduleId">Optional module identifier for modular monolith scoping.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or the identifier of the newly created LIA.</returns>
    ValueTask<Either<EncinaError, Guid>> CreateLIAAsync(
        Guid id,
        string reference,
        string name,
        string purpose,
        string legitimateInterest,
        string benefits,
        string consequencesIfNotProcessed,
        string necessityJustification,
        IReadOnlyList<string> alternativesConsidered,
        string dataMinimisationNotes,
        string natureOfData,
        string reasonableExpectations,
        string impactAssessment,
        IReadOnlyList<string> safeguards,
        string assessedBy,
        bool dpoInvolvement,
        string? conditions = null,
        string? tenantId = null,
        string? moduleId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Approves a LIA, confirming that the legitimate interest outweighs
    /// the data subject's rights and freedoms.
    /// </summary>
    /// <param name="liaId">The LIA identifier to approve.</param>
    /// <param name="conclusion">Summary conclusion of the assessment outcome.</param>
    /// <param name="approvedBy">Identifier of the person approving the LIA.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    ValueTask<Either<EncinaError, Unit>> ApproveLIAAsync(
        Guid liaId,
        string conclusion,
        string approvedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rejects a LIA, indicating that the data subject's rights and freedoms
    /// override the legitimate interest.
    /// </summary>
    /// <param name="liaId">The LIA identifier to reject.</param>
    /// <param name="conclusion">Summary conclusion explaining the rejection.</param>
    /// <param name="rejectedBy">Identifier of the person rejecting the LIA.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    ValueTask<Either<EncinaError, Unit>> RejectLIAAsync(
        Guid liaId,
        string conclusion,
        string rejectedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules a periodic review for an approved LIA.
    /// </summary>
    /// <param name="liaId">The LIA identifier to schedule review for.</param>
    /// <param name="nextReviewAtUtc">Timestamp when the next review is due (UTC).</param>
    /// <param name="scheduledBy">Identifier of the person scheduling the review.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    ValueTask<Either<EncinaError, Unit>> ScheduleLIAReviewAsync(
        Guid liaId,
        DateTimeOffset nextReviewAtUtc,
        string scheduledBy,
        CancellationToken cancellationToken = default);

    // ========================================================================
    // Queries
    // ========================================================================

    /// <summary>
    /// Retrieves a lawful basis registration by its identifier.
    /// </summary>
    /// <param name="registrationId">The registration identifier.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or the registration read model.</returns>
    ValueTask<Either<EncinaError, LawfulBasisReadModel>> GetRegistrationAsync(
        Guid registrationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a lawful basis registration by the request type name.
    /// </summary>
    /// <param name="requestTypeName">The assembly-qualified name of the request type.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error, or an option containing the registration if found.</returns>
    ValueTask<Either<EncinaError, Option<LawfulBasisReadModel>>> GetRegistrationByRequestTypeAsync(
        string requestTypeName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all registered lawful basis declarations.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or a list of all registration read models.</returns>
    ValueTask<Either<EncinaError, IReadOnlyList<LawfulBasisReadModel>>> GetAllRegistrationsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a LIA by its identifier.
    /// </summary>
    /// <param name="liaId">The LIA identifier.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or the LIA read model.</returns>
    ValueTask<Either<EncinaError, LIAReadModel>> GetLIAAsync(
        Guid liaId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a LIA by its document reference identifier.
    /// </summary>
    /// <param name="liaReference">The LIA reference (e.g., "LIA-2024-FRAUD-001").</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error, or an option containing the LIA if found.</returns>
    ValueTask<Either<EncinaError, Option<LIAReadModel>>> GetLIAByReferenceAsync(
        string liaReference,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all LIAs that require review.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or a list of LIA read models pending review.</returns>
    ValueTask<Either<EncinaError, IReadOnlyList<LIAReadModel>>> GetPendingLIAReviewsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether an approved LIA exists for the given reference.
    /// </summary>
    /// <param name="liaReference">The LIA reference to check.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or <c>true</c> if an approved LIA exists for the reference.</returns>
    ValueTask<Either<EncinaError, bool>> HasApprovedLIAAsync(
        string liaReference,
        CancellationToken cancellationToken = default);
}
