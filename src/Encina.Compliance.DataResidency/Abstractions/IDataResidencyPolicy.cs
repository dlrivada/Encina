using Encina.Compliance.DataResidency.Model;

using LanguageExt;

namespace Encina.Compliance.DataResidency;

/// <summary>
/// Service for evaluating data residency policies and determining allowed regions.
/// </summary>
/// <remarks>
/// <para>
/// The data residency policy service provides the application-level logic for determining
/// whether data of a specific category is allowed to be stored or processed in a given region,
/// and for resolving the set of permitted regions for each category. It sits above the
/// <see cref="IResidencyPolicyStore"/> (raw persistence) and adds business rules such as
/// adequacy decision requirements and transfer legal basis evaluation.
/// </para>
/// <para>
/// Per GDPR Article 44 (general principle for transfers), any transfer of personal data
/// to a third country or international organisation shall take place only if the conditions
/// laid down in Chapter V are complied with. This service enables controllers to enforce
/// those conditions programmatically before processing requests.
/// </para>
/// <para>
/// All methods follow Railway Oriented Programming (ROP) using <c>Either&lt;EncinaError, T&gt;</c>
/// to provide explicit error handling without exceptions for business logic.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Check if healthcare data can be processed in Germany
/// var allowed = await residencyPolicy.IsAllowedAsync(
///     "healthcare-data", RegionRegistry.Germany, cancellationToken);
///
/// // Get all regions where financial data can be stored
/// var regions = await residencyPolicy.GetAllowedRegionsAsync(
///     "financial-records", cancellationToken);
/// </code>
/// </example>
public interface IDataResidencyPolicy
{
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
    /// Evaluates the <see cref="ResidencyPolicyDescriptor"/> for the given category:
    /// if the policy has an empty <see cref="ResidencyPolicyDescriptor.AllowedRegions"/> list,
    /// all regions are considered allowed. Otherwise, the target region must be present in
    /// the allowed regions list.
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
    /// <para>
    /// Returns the <see cref="ResidencyPolicyDescriptor.AllowedRegions"/> for the category.
    /// An empty list means the policy imposes no geographic restrictions (all regions allowed).
    /// For strict EU-only policies, the list will contain <see cref="RegionRegistry.EUMemberStates"/>.
    /// </para>
    /// <para>
    /// Primarily used for UI display (region selectors), compliance dashboards, and
    /// pre-validation before initiating data transfers.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<Region>>> GetAllowedRegionsAsync(
        string dataCategory,
        CancellationToken cancellationToken = default);
}
