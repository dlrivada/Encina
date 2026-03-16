using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Compliance.ProcessorAgreements.ReadModels;
using LanguageExt;

namespace Encina.Compliance.ProcessorAgreements.Abstractions;

/// <summary>
/// Service interface for managing Data Processing Agreement lifecycle operations.
/// </summary>
/// <remarks>
/// <para>
/// Provides a clean CQRS API for executing, amending, auditing, renewing, terminating, and querying DPAs.
/// The implementation wraps the event-sourced <see cref="Aggregates.DPAAggregate"/> via
/// <c>IAggregateRepository&lt;DPAAggregate&gt;</c> (command side) and
/// <c>IReadModelRepository&lt;DPAReadModel&gt;</c> (query side).
/// </para>
/// <para>
/// Per GDPR Article 28(3), processing by a processor shall be governed by a contract that sets out
/// the subject-matter, duration, nature and purpose of the processing, the type of personal data,
/// categories of data subjects, and the obligations and rights of the controller.
/// </para>
/// <para>
/// DPA lifecycle: Active → PendingRenewal → Active (renewal), or Active → Expired, or Active → Terminated.
/// </para>
/// </remarks>
public interface IDPAService
{
    // ========================================================================
    // Command operations
    // ========================================================================

    /// <summary>
    /// Executes a new Data Processing Agreement between a controller and a processor.
    /// </summary>
    /// <param name="processorId">The identifier of the processor this agreement covers.</param>
    /// <param name="mandatoryTerms">Compliance status of the eight mandatory terms per Article 28(3)(a)-(h).</param>
    /// <param name="hasSCCs">Whether Standard Contractual Clauses are included per Articles 46(2)(c)/(d).</param>
    /// <param name="processingPurposes">The documented processing purposes covered by this agreement.</param>
    /// <param name="signedAtUtc">The UTC timestamp when the agreement was signed by both parties.</param>
    /// <param name="expiresAtUtc">The UTC expiration date, or <c>null</c> for indefinite agreements.</param>
    /// <param name="tenantId">Optional tenant identifier for multi-tenancy scoping.</param>
    /// <param name="moduleId">Optional module identifier for modular monolith scoping.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or the identifier of the newly created DPA.</returns>
    ValueTask<Either<EncinaError, Guid>> ExecuteDPAAsync(
        Guid processorId,
        DPAMandatoryTerms mandatoryTerms,
        bool hasSCCs,
        IReadOnlyList<string> processingPurposes,
        DateTimeOffset signedAtUtc,
        DateTimeOffset? expiresAtUtc,
        string? tenantId = null,
        string? moduleId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Amends an existing DPA with updated terms.
    /// </summary>
    /// <param name="dpaId">The identifier of the agreement to amend.</param>
    /// <param name="updatedTerms">The updated mandatory terms after the amendment.</param>
    /// <param name="hasSCCs">Whether Standard Contractual Clauses are included after the amendment.</param>
    /// <param name="processingPurposes">The updated processing purposes.</param>
    /// <param name="amendmentReason">The reason for amending the agreement.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    ValueTask<Either<EncinaError, Unit>> AmendDPAAsync(
        Guid dpaId,
        DPAMandatoryTerms updatedTerms,
        bool hasSCCs,
        IReadOnlyList<string> processingPurposes,
        string amendmentReason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records an audit conducted on a DPA per Article 28(3)(h).
    /// </summary>
    /// <param name="dpaId">The identifier of the agreement being audited.</param>
    /// <param name="auditorId">The identifier of the person who conducted the audit.</param>
    /// <param name="auditFindings">Summary of the audit findings.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    ValueTask<Either<EncinaError, Unit>> AuditDPAAsync(
        Guid dpaId,
        string auditorId,
        string auditFindings,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Renews a DPA with a new expiration date.
    /// </summary>
    /// <param name="dpaId">The identifier of the agreement to renew.</param>
    /// <param name="newExpiresAtUtc">The new UTC expiration date after renewal.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    ValueTask<Either<EncinaError, Unit>> RenewDPAAsync(
        Guid dpaId,
        DateTimeOffset newExpiresAtUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Terminates a DPA, ending the processing relationship.
    /// </summary>
    /// <remarks>
    /// Per GDPR Article 28(3)(g), upon termination the processor must delete or return all personal data
    /// and certify that it has done so.
    /// </remarks>
    /// <param name="dpaId">The identifier of the agreement to terminate.</param>
    /// <param name="reason">The reason for terminating the agreement.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    ValueTask<Either<EncinaError, Unit>> TerminateDPAAsync(
        Guid dpaId,
        string reason,
        CancellationToken cancellationToken = default);

    // ========================================================================
    // Query operations
    // ========================================================================

    /// <summary>
    /// Retrieves a DPA by its identifier.
    /// </summary>
    /// <param name="dpaId">The DPA identifier.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error (including not-found) or the DPA read model.</returns>
    ValueTask<Either<EncinaError, DPAReadModel>> GetDPAAsync(
        Guid dpaId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all DPAs for a specific processor.
    /// </summary>
    /// <param name="processorId">The processor identifier.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or the list of DPAs for the processor.</returns>
    ValueTask<Either<EncinaError, IReadOnlyList<DPAReadModel>>> GetDPAsByProcessorIdAsync(
        Guid processorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the currently active DPA for a specific processor.
    /// </summary>
    /// <remarks>
    /// Returns the DPA in <see cref="DPAStatus.Active"/> status whose <see cref="DPAReadModel.ExpiresAtUtc"/>
    /// has not passed. Returns a not-found error if no active agreement exists.
    /// </remarks>
    /// <param name="processorId">The processor identifier.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error (including not-found) or the active DPA read model.</returns>
    ValueTask<Either<EncinaError, DPAReadModel>> GetActiveDPAByProcessorIdAsync(
        Guid processorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all DPAs with a specific lifecycle status.
    /// </summary>
    /// <param name="status">The DPA status to filter by.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or the list of DPAs with the specified status.</returns>
    ValueTask<Either<EncinaError, IReadOnlyList<DPAReadModel>>> GetDPAsByStatusAsync(
        DPAStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves DPAs that are approaching expiration within the configured warning period.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or the list of DPAs approaching expiration.</returns>
    ValueTask<Either<EncinaError, IReadOnlyList<DPAReadModel>>> GetExpiringDPAsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fast-path check for whether a processor has a valid, active DPA.
    /// </summary>
    /// <remarks>
    /// This method is optimized for use in the <c>ProcessorValidationPipelineBehavior</c>.
    /// It avoids loading the full read model and uses cache-first lookup.
    /// </remarks>
    /// <param name="processorId">The processor identifier.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or <c>true</c> if a valid active DPA exists.</returns>
    ValueTask<Either<EncinaError, bool>> HasValidDPAAsync(
        Guid processorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a processor's DPA compliance in detail.
    /// </summary>
    /// <remarks>
    /// Returns a comprehensive <see cref="DPAValidationResult"/> including mandatory terms compliance,
    /// expiration warnings, SCC requirements, and days until expiration. Used for detailed compliance
    /// reporting and enforcement.
    /// </remarks>
    /// <param name="processorId">The processor identifier.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or the detailed validation result.</returns>
    ValueTask<Either<EncinaError, DPAValidationResult>> ValidateDPAAsync(
        Guid processorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the event history for a DPA aggregate.
    /// </summary>
    /// <param name="dpaId">The DPA identifier.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or the list of historical events.</returns>
    ValueTask<Either<EncinaError, IReadOnlyList<object>>> GetDPAHistoryAsync(
        Guid dpaId,
        CancellationToken cancellationToken = default);
}
