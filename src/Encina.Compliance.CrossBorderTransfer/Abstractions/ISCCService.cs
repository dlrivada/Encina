using Encina.Compliance.CrossBorderTransfer.Model;
using Encina.Compliance.CrossBorderTransfer.ReadModels;
using LanguageExt;

namespace Encina.Compliance.CrossBorderTransfer.Abstractions;

/// <summary>
/// Service interface for managing Standard Contractual Clauses (SCC) agreement lifecycle operations.
/// </summary>
/// <remarks>
/// <para>
/// Provides a clean API for registering, managing, and validating SCC agreements.
/// The implementation wraps the event-sourced <c>SCCAgreementAggregate</c> via
/// <c>IAggregateRepository&lt;SCCAgreementAggregate&gt;</c>.
/// </para>
/// <para>
/// Per GDPR Article 46(2)(c), Standard Contractual Clauses are pre-approved contractual terms
/// that bind the data importer to EU-equivalent data protection standards. Post-Schrems II,
/// SCCs alone may not be sufficient — a TIA and supplementary measures may also be required.
/// </para>
/// </remarks>
public interface ISCCService
{
    /// <summary>
    /// Registers a new SCC agreement.
    /// </summary>
    /// <param name="processorId">Identifier of the data processor/importer.</param>
    /// <param name="sccModule">The SCC module applicable to this transfer relationship.</param>
    /// <param name="version">Version of the SCC clauses used.</param>
    /// <param name="executedAtUtc">Timestamp when the agreement was executed.</param>
    /// <param name="expiresAtUtc">Optional expiration date of the agreement.</param>
    /// <param name="tenantId">Optional tenant identifier for multi-tenancy scoping.</param>
    /// <param name="moduleId">Optional module identifier for modular monolith scoping.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or the identifier of the newly registered agreement.</returns>
    ValueTask<Either<EncinaError, Guid>> RegisterAgreementAsync(
        string processorId,
        SCCModule sccModule,
        string version,
        DateTimeOffset executedAtUtc,
        DateTimeOffset? expiresAtUtc = null,
        string? tenantId = null,
        string? moduleId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a supplementary measure to an SCC agreement.
    /// </summary>
    /// <param name="agreementId">The SCC agreement identifier.</param>
    /// <param name="type">Category of the supplementary measure.</param>
    /// <param name="description">Description of the measure.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    ValueTask<Either<EncinaError, Unit>> AddSupplementaryMeasureAsync(
        Guid agreementId,
        SupplementaryMeasureType type,
        string description,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes an SCC agreement.
    /// </summary>
    /// <param name="agreementId">The SCC agreement identifier.</param>
    /// <param name="reason">Explanation of why the agreement is being revoked.</param>
    /// <param name="revokedBy">Identifier of the person revoking the agreement.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    ValueTask<Either<EncinaError, Unit>> RevokeAgreementAsync(
        Guid agreementId,
        string reason,
        string revokedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an SCC agreement by its identifier.
    /// </summary>
    /// <param name="agreementId">The SCC agreement identifier.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error (including not-found), or the SCC agreement read model.</returns>
    ValueTask<Either<EncinaError, SCCAgreementReadModel>> GetAgreementAsync(
        Guid agreementId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates whether a valid SCC agreement exists for a processor and module combination.
    /// </summary>
    /// <param name="processorId">Identifier of the data processor/importer.</param>
    /// <param name="sccModule">The required SCC module.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or the SCC validation result.</returns>
    ValueTask<Either<EncinaError, SCCValidationResult>> ValidateAgreementAsync(
        string processorId,
        SCCModule sccModule,
        CancellationToken cancellationToken = default);
}
