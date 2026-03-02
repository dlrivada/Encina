using Encina.Compliance.DataResidency.Model;

using LanguageExt;

namespace Encina.Compliance.DataResidency;

/// <summary>
/// Store for managing residency policy descriptor lifecycle and persistence.
/// </summary>
/// <remarks>
/// <para>
/// The residency policy store provides CRUD operations for <see cref="ResidencyPolicyDescriptor"/>
/// records, enabling category-based data residency management. Each policy defines which regions
/// are allowed for a specific data category, whether adequacy decisions are required, and which
/// transfer legal bases are permitted for cross-border transfers.
/// </para>
/// <para>
/// Per GDPR Article 44 (general principle for transfers), any transfer of personal data to a
/// third country shall take place only if the conditions of Chapter V are complied with.
/// Residency policy descriptors encode these conditions as enforceable rules that the
/// <see cref="IDataResidencyPolicy"/> service evaluates at runtime.
/// </para>
/// <para>
/// All methods follow Railway Oriented Programming (ROP) using <c>Either&lt;EncinaError, T&gt;</c>
/// to provide explicit error handling without exceptions for business logic.
/// </para>
/// <para>
/// Implementations may store policies in-memory (for development/testing), in a database
/// (for production), or in any other suitable backing store. All 13 database providers
/// are supported: ADO.NET (4), Dapper (4), EF Core (4), and MongoDB (1).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create a residency policy for healthcare data (EU-only)
/// var policy = ResidencyPolicyDescriptor.Create(
///     dataCategory: "healthcare-data",
///     allowedRegions: RegionRegistry.EUMemberStates,
///     requireAdequacyDecision: true);
///
/// await policyStore.CreateAsync(policy, cancellationToken);
///
/// // Retrieve the policy for a data category
/// var result = await policyStore.GetByCategoryAsync("healthcare-data", cancellationToken);
/// </code>
/// </example>
public interface IResidencyPolicyStore
{
    /// <summary>
    /// Creates a new residency policy descriptor.
    /// </summary>
    /// <param name="policy">The residency policy descriptor to persist.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> if the policy
    /// could not be stored (e.g., duplicate data category).
    /// </returns>
    ValueTask<Either<EncinaError, Unit>> CreateAsync(
        ResidencyPolicyDescriptor policy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the residency policy descriptor for a specific data category.
    /// </summary>
    /// <param name="dataCategory">The data category to look up (e.g., "healthcare-data", "financial-records").</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <c>Some(policy)</c> if a policy exists for the category,
    /// <c>None</c> if no policy is found, or an <see cref="EncinaError"/> on failure.
    /// </returns>
    /// <remarks>
    /// Each data category should have at most one residency policy. This method
    /// is the primary lookup for policy resolution during enforcement.
    /// </remarks>
    ValueTask<Either<EncinaError, Option<ResidencyPolicyDescriptor>>> GetByCategoryAsync(
        string dataCategory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all residency policy descriptors.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of all residency policy descriptors, or an <see cref="EncinaError"/>
    /// on failure.
    /// </returns>
    /// <remarks>
    /// Primarily used for compliance reporting, dashboards, and administrative interfaces.
    /// For large datasets, consider implementing pagination in provider-specific extensions.
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<ResidencyPolicyDescriptor>>> GetAllAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing residency policy descriptor.
    /// </summary>
    /// <param name="policy">The updated residency policy descriptor. The <see cref="ResidencyPolicyDescriptor.DataCategory"/> must match an existing policy.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> if the policy
    /// was not found or the update failed.
    /// </returns>
    /// <remarks>
    /// Updating a policy takes effect immediately for all subsequent residency checks.
    /// Consider recording the change in the <see cref="IResidencyAuditStore"/> for
    /// compliance traceability.
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> UpdateAsync(
        ResidencyPolicyDescriptor policy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the residency policy descriptor for a specific data category.
    /// </summary>
    /// <param name="dataCategory">The data category whose policy should be removed.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> if the policy
    /// was not found or the deletion failed.
    /// </returns>
    /// <remarks>
    /// Deleting a policy removes all region restrictions for the category. Subsequent
    /// residency checks for this category will report no policy found. Consider recording
    /// the deletion in the <see cref="IResidencyAuditStore"/> before removing.
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> DeleteAsync(
        string dataCategory,
        CancellationToken cancellationToken = default);
}
