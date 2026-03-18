using Encina.Compliance.DataResidency.Model;
using Encina.Compliance.DataResidency.ReadModels;
using LanguageExt;

namespace Encina.Compliance.DataResidency.Abstractions;

/// <summary>
/// Service interface for managing residency policy lifecycle operations via event-sourced aggregates,
/// and evaluating data residency rules for compliance with GDPR Chapter V (Articles 44–49).
/// </summary>
/// <remarks>
/// <para>
/// Provides a clean API for creating, updating, deleting, and querying residency policies, as well as
/// evaluating whether data of a specific category is allowed in a target region. The implementation
/// wraps the event-sourced <c>ResidencyPolicyAggregate</c> via <c>IAggregateRepository</c>, handling
/// aggregate loading, command execution, persistence, and cache management.
/// </para>
/// <para>
/// This service replaces the legacy <c>IResidencyPolicyStore</c> and absorbs the evaluation methods
/// from <c>IDataResidencyPolicy</c> (<see cref="IsAllowedAsync"/> and <see cref="GetAllowedRegionsAsync"/>)
/// into a single CQRS-oriented API. The event stream serves as the audit trail, eliminating the
/// need for a separate <c>IResidencyAuditStore</c> per GDPR Article 5(2) accountability.
/// </para>
/// <para>
/// <b>Commands</b> (write operations via aggregate):
/// <list type="bullet">
///   <item><description><see cref="CreatePolicyAsync"/> — Creates a new residency policy for a data category (Art. 44)</description></item>
///   <item><description><see cref="UpdatePolicyAsync"/> — Updates an existing policy's allowed regions and transfer bases</description></item>
///   <item><description><see cref="DeletePolicyAsync"/> — Soft-deletes a policy, stopping enforcement</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Queries</b> (read operations via read model repository):
/// <list type="bullet">
///   <item><description><see cref="GetPolicyAsync"/> — Retrieves a policy by ID</description></item>
///   <item><description><see cref="GetPolicyByCategoryAsync"/> — Retrieves a policy by data category</description></item>
///   <item><description><see cref="GetAllPoliciesAsync"/> — Lists all active policies</description></item>
///   <item><description><see cref="GetPolicyHistoryAsync"/> — Retrieves full event history</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Evaluation</b> (absorbed from <c>IDataResidencyPolicy</c>):
/// <list type="bullet">
///   <item><description><see cref="IsAllowedAsync"/> — Checks if a region is permitted for a data category</description></item>
///   <item><description><see cref="GetAllowedRegionsAsync"/> — Resolves the list of permitted regions for a data category</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IResidencyPolicyService
{
    // ========================================================================
    // Command operations (write-side via ResidencyPolicyAggregate)
    // ========================================================================

    /// <summary>
    /// Creates a new residency policy for a specific data category.
    /// </summary>
    /// <param name="dataCategory">The data category this policy applies to (e.g., "personal-data", "healthcare-data").</param>
    /// <param name="allowedRegionCodes">Region codes where data is allowed to be stored. Empty means no restrictions.</param>
    /// <param name="requireAdequacyDecision">Whether an EU adequacy decision is required under Article 45.</param>
    /// <param name="allowedTransferBases">Legal bases acceptable for cross-border transfers (Art. 46, 49).</param>
    /// <param name="tenantId">Optional tenant identifier for multi-tenancy.</param>
    /// <param name="moduleId">Optional module identifier for modular monolith.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or the identifier of the newly created policy aggregate.</returns>
    /// <remarks>
    /// Per GDPR Article 44, controllers must establish explicit residency policies for all
    /// categories of personal data that may be subject to international transfer.
    /// </remarks>
    ValueTask<Either<EncinaError, Guid>> CreatePolicyAsync(
        string dataCategory,
        IReadOnlyList<string> allowedRegionCodes,
        bool requireAdequacyDecision,
        IReadOnlyList<TransferLegalBasis> allowedTransferBases,
        string? tenantId = null,
        string? moduleId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing residency policy with new parameters.
    /// </summary>
    /// <param name="policyId">The residency policy aggregate identifier.</param>
    /// <param name="allowedRegionCodes">Updated list of allowed region codes.</param>
    /// <param name="requireAdequacyDecision">Updated adequacy decision requirement.</param>
    /// <param name="allowedTransferBases">Updated list of allowed transfer legal bases.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    /// <remarks>
    /// Only active policies can be updated. Per GDPR Article 5(2) accountability, the update
    /// is captured as an immutable event in the aggregate's event stream.
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> UpdatePolicyAsync(
        Guid policyId,
        IReadOnlyList<string> allowedRegionCodes,
        bool requireAdequacyDecision,
        IReadOnlyList<TransferLegalBasis> allowedTransferBases,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a residency policy, stopping enforcement for the data category.
    /// </summary>
    /// <param name="policyId">The residency policy aggregate identifier.</param>
    /// <param name="reason">The reason for deleting this policy.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    /// <remarks>
    /// Existing data locations are not affected. The event stream preserves the full policy
    /// history for GDPR Article 5(2) accountability and Article 58 supervisory authority inquiries.
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> DeletePolicyAsync(
        Guid policyId,
        string reason,
        CancellationToken cancellationToken = default);

    // ========================================================================
    // Query operations (read-side via ResidencyPolicyReadModel)
    // ========================================================================

    /// <summary>
    /// Retrieves a residency policy by its aggregate identifier.
    /// </summary>
    /// <param name="policyId">The residency policy aggregate identifier.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error (including not-found) or the policy read model.</returns>
    ValueTask<Either<EncinaError, ResidencyPolicyReadModel>> GetPolicyAsync(
        Guid policyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a residency policy by its data category.
    /// </summary>
    /// <param name="dataCategory">The data category to search for.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error (including not-found) or the policy read model.</returns>
    ValueTask<Either<EncinaError, ResidencyPolicyReadModel>> GetPolicyByCategoryAsync(
        string dataCategory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all active residency policies.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A read-only list of active policy read models.</returns>
    ValueTask<Either<EncinaError, IReadOnlyList<ResidencyPolicyReadModel>>> GetAllPoliciesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the full event history for a residency policy aggregate.
    /// </summary>
    /// <param name="policyId">The residency policy aggregate identifier.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>
    /// Either an error or the list of domain events that have been applied to this policy,
    /// ordered chronologically. Provides a complete audit trail for GDPR Article 5(2) accountability.
    /// </returns>
    ValueTask<Either<EncinaError, IReadOnlyList<object>>> GetPolicyHistoryAsync(
        Guid policyId,
        CancellationToken cancellationToken = default);

    // ========================================================================
    // Evaluation operations (absorbed from IDataResidencyPolicy)
    // ========================================================================

    /// <summary>
    /// Determines whether data of the specified category is allowed in the target region.
    /// </summary>
    /// <param name="dataCategory">The data category to evaluate (e.g., "healthcare-data", "financial-records").</param>
    /// <param name="targetRegion">The region where the data would be stored or processed.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <c>true</c> if the data category is allowed in the target region per the configured
    /// residency policy, <c>false</c> if the region is not permitted,
    /// or an <see cref="EncinaError"/> if no policy is defined for the category.
    /// </returns>
    /// <remarks>
    /// <para>
    /// If the policy has an empty <see cref="ResidencyPolicyReadModel.AllowedRegionCodes"/> list,
    /// all regions are considered allowed. Otherwise, the target region's code must be present
    /// in the allowed regions list (case-insensitive).
    /// </para>
    /// <para>
    /// This method does NOT evaluate cross-border transfer requirements. For full transfer
    /// validation including legal basis checks, use <see cref="ICrossBorderTransferValidator"/>.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, bool>> IsAllowedAsync(
        string dataCategory,
        Region targetRegion,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the list of regions where data of the specified category is allowed.
    /// </summary>
    /// <param name="dataCategory">The data category to look up (e.g., "healthcare-data", "financial-records").</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of <see cref="Region"/> instances where the data category is permitted,
    /// or an <see cref="EncinaError"/> if no policy is defined for the category.
    /// An empty list indicates no region restrictions (data allowed anywhere).
    /// </returns>
    /// <remarks>
    /// Resolves region codes from the policy to <see cref="Region"/> objects via
    /// <see cref="RegionRegistry.GetByCode"/>. Unrecognized region codes are skipped.
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<Region>>> GetAllowedRegionsAsync(
        string dataCategory,
        CancellationToken cancellationToken = default);
}
