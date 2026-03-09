using LanguageExt;

namespace Encina.Security.ABAC.Persistence;

/// <summary>
/// Abstraction for persistent storage and retrieval of XACML policy sets and standalone policies.
/// </summary>
/// <remarks>
/// <para>
/// This interface defines the persistence contract for <see cref="PolicySet"/> and <see cref="Policy"/>
/// objects. It is the database abstraction that providers implement — the
/// <see cref="Administration.PersistentPolicyAdministrationPoint"/> wraps an <c>IPolicyStore</c>
/// with administration logic (parent-child tracking, duplicate detection).
/// </para>
/// <para>
/// Implementations are provided by each database provider:
/// <list type="bullet">
/// <item><description><b>Entity Framework Core</b>: Full ORM with change tracking and migrations</description></item>
/// <item><description><b>Dapper</b>: Lightweight micro-ORM with explicit SQL</description></item>
/// <item><description><b>ADO.NET</b>: Maximum performance with raw SQL</description></item>
/// <item><description><b>MongoDB</b>: Native document storage with BSON serialization</description></item>
/// </list>
/// </para>
/// <para>
/// Save operations use <b>upsert</b> semantics: insert if the entity does not exist,
/// update if it does. This simplifies the contract and avoids separate insert/update methods
/// at the store level (the PAP handles duplicate detection).
/// </para>
/// <para>
/// All methods return <c>ValueTask&lt;Either&lt;EncinaError, T&gt;&gt;</c> following the
/// Railway Oriented Programming (ROP) pattern. Infrastructure failures are captured as
/// <c>Left</c> values instead of throwing exceptions.
/// </para>
/// </remarks>
public interface IPolicyStore
{
    // ── PolicySet Operations ─────────────────────────────────────────

    /// <summary>
    /// Retrieves all policy sets from the store.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// <c>Right</c> containing all stored policy sets on success,
    /// or <c>Left</c> containing an <see cref="EncinaError"/> on infrastructure failure.
    /// </returns>
    ValueTask<Either<EncinaError, IReadOnlyList<PolicySet>>> GetAllPolicySetsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific policy set by its identifier.
    /// </summary>
    /// <param name="policySetId">The unique identifier of the policy set.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// <c>Right(Some)</c> if the policy set exists, <c>Right(None)</c> if not found,
    /// or <c>Left</c> on infrastructure failure.
    /// </returns>
    ValueTask<Either<EncinaError, Option<PolicySet>>> GetPolicySetAsync(
        string policySetId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a policy set using upsert semantics (insert if new, update if exists).
    /// </summary>
    /// <param name="policySet">The policy set to save.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// <c>Right(Unit)</c> on success, or <c>Left</c> on infrastructure failure.
    /// </returns>
    ValueTask<Either<EncinaError, Unit>> SavePolicySetAsync(
        PolicySet policySet,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a policy set by its identifier.
    /// </summary>
    /// <param name="policySetId">The unique identifier of the policy set to delete.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// <c>Right(Unit)</c> on success (including when the policy set did not exist),
    /// or <c>Left</c> on infrastructure failure.
    /// </returns>
    ValueTask<Either<EncinaError, Unit>> DeletePolicySetAsync(
        string policySetId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a policy set with the specified identifier exists.
    /// </summary>
    /// <param name="policySetId">The unique identifier of the policy set.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// <c>Right(true)</c> if the policy set exists, <c>Right(false)</c> if not,
    /// or <c>Left</c> on infrastructure failure.
    /// </returns>
    ValueTask<Either<EncinaError, bool>> ExistsPolicySetAsync(
        string policySetId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the total number of policy sets in the store.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// <c>Right</c> containing the count on success,
    /// or <c>Left</c> on infrastructure failure.
    /// </returns>
    /// <remarks>
    /// Used by health checks to verify the store is operational and report policy set counts.
    /// </remarks>
    ValueTask<Either<EncinaError, int>> GetPolicySetCountAsync(
        CancellationToken cancellationToken = default);

    // ── Standalone Policy Operations ─────────────────────────────────

    /// <summary>
    /// Retrieves all standalone policies from the store.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// <c>Right</c> containing all standalone policies on success,
    /// or <c>Left</c> containing an <see cref="EncinaError"/> on infrastructure failure.
    /// </returns>
    /// <remarks>
    /// Standalone policies are those stored directly in the <c>abac_policies</c> table,
    /// not embedded within a <see cref="PolicySet"/>'s JSON. Policies nested inside
    /// a policy set are retrieved via <see cref="GetPolicySetAsync"/> and accessed
    /// through <see cref="PolicySet.Policies"/>.
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<Policy>>> GetAllStandalonePoliciesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific standalone policy by its identifier.
    /// </summary>
    /// <param name="policyId">The unique identifier of the policy.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// <c>Right(Some)</c> if the policy exists, <c>Right(None)</c> if not found,
    /// or <c>Left</c> on infrastructure failure.
    /// </returns>
    ValueTask<Either<EncinaError, Option<Policy>>> GetPolicyAsync(
        string policyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a standalone policy using upsert semantics (insert if new, update if exists).
    /// </summary>
    /// <param name="policy">The policy to save.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// <c>Right(Unit)</c> on success, or <c>Left</c> on infrastructure failure.
    /// </returns>
    ValueTask<Either<EncinaError, Unit>> SavePolicyAsync(
        Policy policy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a standalone policy by its identifier.
    /// </summary>
    /// <param name="policyId">The unique identifier of the policy to delete.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// <c>Right(Unit)</c> on success (including when the policy did not exist),
    /// or <c>Left</c> on infrastructure failure.
    /// </returns>
    ValueTask<Either<EncinaError, Unit>> DeletePolicyAsync(
        string policyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a standalone policy with the specified identifier exists.
    /// </summary>
    /// <param name="policyId">The unique identifier of the policy.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// <c>Right(true)</c> if the policy exists, <c>Right(false)</c> if not,
    /// or <c>Left</c> on infrastructure failure.
    /// </returns>
    ValueTask<Either<EncinaError, bool>> ExistsPolicyAsync(
        string policyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the total number of standalone policies in the store.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// <c>Right</c> containing the count on success,
    /// or <c>Left</c> on infrastructure failure.
    /// </returns>
    /// <remarks>
    /// Used by health checks to verify the store is operational and report policy counts.
    /// </remarks>
    ValueTask<Either<EncinaError, int>> GetPolicyCountAsync(
        CancellationToken cancellationToken = default);
}
