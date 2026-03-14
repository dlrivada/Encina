using Encina.Compliance.CrossBorderTransfer.Model;
using Encina.Compliance.CrossBorderTransfer.ReadModels;
using LanguageExt;

namespace Encina.Compliance.CrossBorderTransfer.Abstractions;

/// <summary>
/// Service interface for managing Transfer Impact Assessment (TIA) lifecycle operations.
/// </summary>
/// <remarks>
/// <para>
/// Provides a clean API for creating, progressing, and querying TIAs. The implementation
/// wraps the event-sourced <c>TIAAggregate</c> via <c>IAggregateRepository&lt;TIAAggregate&gt;</c>,
/// handling aggregate loading, command execution, persistence, and cache management.
/// </para>
/// <para>
/// A TIA is required under the Schrems II judgment (CJEU C-311/18) for transfers based on
/// SCCs or BCRs to countries without an adequacy decision. The TIA evaluates whether the
/// destination country's legal framework provides "essentially equivalent" protection.
/// </para>
/// <para>
/// TIA lifecycle: Draft → InProgress (risk assessed) → PendingDPOReview → Completed → Expired.
/// </para>
/// </remarks>
public interface ITIAService
{
    /// <summary>
    /// Creates a new Transfer Impact Assessment for the specified transfer route.
    /// </summary>
    /// <param name="sourceCountryCode">ISO 3166-1 alpha-2 country code of the data exporter.</param>
    /// <param name="destinationCountryCode">ISO 3166-1 alpha-2 country code of the data importer.</param>
    /// <param name="dataCategory">Category of personal data being assessed.</param>
    /// <param name="createdBy">Identifier of the user initiating the TIA.</param>
    /// <param name="tenantId">Optional tenant identifier for multi-tenancy scoping.</param>
    /// <param name="moduleId">Optional module identifier for modular monolith scoping.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or the identifier of the newly created TIA.</returns>
    ValueTask<Either<EncinaError, Guid>> CreateTIAAsync(
        string sourceCountryCode,
        string destinationCountryCode,
        string dataCategory,
        string createdBy,
        string? tenantId = null,
        string? moduleId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records the risk assessment results for a TIA.
    /// </summary>
    /// <param name="tiaId">The TIA identifier.</param>
    /// <param name="riskScore">Risk score between 0.0 (no risk) and 1.0 (maximum risk).</param>
    /// <param name="findings">Summary of risk assessment findings.</param>
    /// <param name="assessorId">Identifier of the person performing the assessment.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    ValueTask<Either<EncinaError, Unit>> AssessRiskAsync(
        Guid tiaId,
        double riskScore,
        string? findings,
        string assessorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Identifies a supplementary measure as required for a TIA.
    /// </summary>
    /// <param name="tiaId">The TIA identifier.</param>
    /// <param name="type">Category of the supplementary measure.</param>
    /// <param name="description">Description of the required measure.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    ValueTask<Either<EncinaError, Unit>> RequireSupplementaryMeasureAsync(
        Guid tiaId,
        SupplementaryMeasureType type,
        string description,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Submits a TIA for DPO review.
    /// </summary>
    /// <param name="tiaId">The TIA identifier.</param>
    /// <param name="submittedBy">Identifier of the person submitting the TIA.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    ValueTask<Either<EncinaError, Unit>> SubmitForDPOReviewAsync(
        Guid tiaId,
        string submittedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes the DPO review of a TIA, approving or rejecting it.
    /// </summary>
    /// <param name="tiaId">The TIA identifier.</param>
    /// <param name="approved">Whether the DPO approved the assessment.</param>
    /// <param name="reviewedBy">Identifier of the DPO performing the review.</param>
    /// <param name="reason">Reason for rejection (required when <paramref name="approved"/> is <c>false</c>).</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    ValueTask<Either<EncinaError, Unit>> CompleteDPOReviewAsync(
        Guid tiaId,
        bool approved,
        string reviewedBy,
        string? reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a TIA by its identifier.
    /// </summary>
    /// <param name="tiaId">The TIA identifier.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error (including not-found), or the TIA read model.</returns>
    ValueTask<Either<EncinaError, TIAReadModel>> GetTIAAsync(
        Guid tiaId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a TIA by its transfer route (source, destination, data category).
    /// </summary>
    /// <param name="sourceCountryCode">ISO 3166-1 alpha-2 country code of the data exporter.</param>
    /// <param name="destinationCountryCode">ISO 3166-1 alpha-2 country code of the data importer.</param>
    /// <param name="dataCategory">Category of personal data.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error (including not-found), or the matching TIA read model.</returns>
    ValueTask<Either<EncinaError, TIAReadModel>> GetTIAByRouteAsync(
        string sourceCountryCode,
        string destinationCountryCode,
        string dataCategory,
        CancellationToken cancellationToken = default);
}
