using Encina.Compliance.Retention.Model;

using LanguageExt;

namespace Encina.Compliance.Retention;

/// <summary>
/// Store for managing retention policy lifecycle and persistence.
/// </summary>
/// <remarks>
/// <para>
/// The retention policy store provides CRUD operations for <see cref="RetentionPolicy"/> records,
/// enabling category-based retention period management. Each policy defines how long data
/// in a specific category should be retained and whether automatic deletion is enabled.
/// </para>
/// <para>
/// Per GDPR Article 5(1)(e) (storage limitation), personal data shall be kept for no longer
/// than is necessary for the purposes for which it is processed. Retention policies formalize
/// this principle by defining explicit, auditable retention periods per data category.
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
/// // Create a new retention policy
/// var policy = RetentionPolicy.Create(
///     dataCategory: "financial-records",
///     retentionPeriod: RetentionPolicy.FromYears(7),
///     autoDelete: true,
///     reason: "German tax law (AO section 147)",
///     legalBasis: "Legal obligation (Art. 6(1)(c))");
///
/// await policyStore.CreateAsync(policy, cancellationToken);
///
/// // Retrieve policy for a data category
/// var result = await policyStore.GetByCategoryAsync("financial-records", cancellationToken);
/// </code>
/// </example>
public interface IRetentionPolicyStore
{
    /// <summary>
    /// Creates a new retention policy record.
    /// </summary>
    /// <param name="policy">The retention policy to persist.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> if the policy
    /// could not be stored (e.g., duplicate ID or category).
    /// </returns>
    ValueTask<Either<EncinaError, Unit>> CreateAsync(
        RetentionPolicy policy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a retention policy by its unique identifier.
    /// </summary>
    /// <param name="policyId">The unique identifier of the policy.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <c>Some(policy)</c> if a policy with the given ID exists,
    /// <c>None</c> if no policy is found, or an <see cref="EncinaError"/> on failure.
    /// </returns>
    ValueTask<Either<EncinaError, Option<RetentionPolicy>>> GetByIdAsync(
        string policyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the retention policy for a specific data category.
    /// </summary>
    /// <param name="dataCategory">The data category to look up.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <c>Some(policy)</c> if a policy exists for the category,
    /// <c>None</c> if no policy is found, or an <see cref="EncinaError"/> on failure.
    /// </returns>
    /// <remarks>
    /// Each data category should have at most one retention policy. This method
    /// is the primary lookup for policy resolution during enforcement.
    /// </remarks>
    ValueTask<Either<EncinaError, Option<RetentionPolicy>>> GetByCategoryAsync(
        string dataCategory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all retention policies.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of all retention policies, or an <see cref="EncinaError"/> on failure.
    /// </returns>
    /// <remarks>
    /// Primarily used for compliance reporting, dashboards, and administrative interfaces.
    /// For large datasets, consider implementing pagination in provider-specific extensions.
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<RetentionPolicy>>> GetAllAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing retention policy.
    /// </summary>
    /// <param name="policy">The updated retention policy. The <see cref="RetentionPolicy.Id"/> must match an existing policy.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> if the policy
    /// was not found or the update failed.
    /// </returns>
    /// <remarks>
    /// Updating a policy does not retroactively modify existing retention records.
    /// Existing records retain their original expiration dates.
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> UpdateAsync(
        RetentionPolicy policy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a retention policy by its unique identifier.
    /// </summary>
    /// <param name="policyId">The unique identifier of the policy to delete.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> if the policy
    /// was not found or the deletion failed.
    /// </returns>
    /// <remarks>
    /// Deleting a policy does not automatically delete associated retention records.
    /// Existing records retain their expiration dates and continue to be enforced.
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> DeleteAsync(
        string policyId,
        CancellationToken cancellationToken = default);
}
