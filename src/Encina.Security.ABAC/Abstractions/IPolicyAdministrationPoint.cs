using LanguageExt;

namespace Encina.Security.ABAC;

/// <summary>
/// Policy Administration Point (PAP) — manages the lifecycle of XACML policies
/// and policy sets through hierarchical CRUD operations.
/// </summary>
/// <remarks>
/// <para>
/// XACML 3.0 §2 — The PAP provides storage and retrieval of <see cref="PolicySet"/>
/// and <see cref="Policy"/> objects. Policy sets form a hierarchy: a policy set can
/// contain other policy sets and/or individual policies.
/// </para>
/// <para>
/// All operations return <c>Either&lt;EncinaError, T&gt;</c> following Railway Oriented
/// Programming (ROP) for explicit error handling without exceptions.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Add a policy set
/// var result = await pap.AddPolicySetAsync(policySet, ct);
/// result.Match(
///     Right: _ => Console.WriteLine("Policy set added"),
///     Left: error => Console.WriteLine($"Failed: {error.Message}"));
///
/// // Add a policy to a specific policy set
/// await pap.AddPolicyAsync(policy, parentPolicySetId: "access-control", ct);
/// </code>
/// </example>
public interface IPolicyAdministrationPoint
{
    // ── PolicySet CRUD ──────────────────────────────────────────────

    /// <summary>
    /// Retrieves all policy sets.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A list of all registered policy sets, or an error.</returns>
    ValueTask<Either<EncinaError, IReadOnlyList<PolicySet>>> GetPolicySetsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific policy set by its identifier.
    /// </summary>
    /// <param name="policySetId">The unique identifier of the policy set.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The policy set if found (<c>Some</c>), <c>None</c> if not found, or an error.</returns>
    ValueTask<Either<EncinaError, Option<PolicySet>>> GetPolicySetAsync(
        string policySetId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new policy set to the repository.
    /// </summary>
    /// <param name="policySet">The policy set to add.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns><c>Unit</c> on success, or an error (e.g., duplicate ID).</returns>
    ValueTask<Either<EncinaError, Unit>> AddPolicySetAsync(
        PolicySet policySet,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing policy set.
    /// </summary>
    /// <param name="policySet">The policy set with updated values. The <see cref="PolicySet.Id"/> must match an existing policy set.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns><c>Unit</c> on success, or an error (e.g., policy set not found).</returns>
    ValueTask<Either<EncinaError, Unit>> UpdatePolicySetAsync(
        PolicySet policySet,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a policy set by its identifier.
    /// </summary>
    /// <param name="policySetId">The unique identifier of the policy set to remove.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns><c>Unit</c> on success, or an error (e.g., policy set not found).</returns>
    ValueTask<Either<EncinaError, Unit>> RemovePolicySetAsync(
        string policySetId,
        CancellationToken cancellationToken = default);

    // ── Policy CRUD ─────────────────────────────────────────────────

    /// <summary>
    /// Retrieves policies, optionally filtered by their parent policy set.
    /// </summary>
    /// <param name="policySetId">
    /// If specified, returns only policies within that policy set.
    /// If <c>null</c>, returns all policies.
    /// </param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A list of matching policies, or an error.</returns>
    ValueTask<Either<EncinaError, IReadOnlyList<Policy>>> GetPoliciesAsync(
        string? policySetId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific policy by its identifier.
    /// </summary>
    /// <param name="policyId">The unique identifier of the policy.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The policy if found (<c>Some</c>), <c>None</c> if not found, or an error.</returns>
    ValueTask<Either<EncinaError, Option<Policy>>> GetPolicyAsync(
        string policyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new policy, optionally placing it within a parent policy set.
    /// </summary>
    /// <param name="policy">The policy to add.</param>
    /// <param name="parentPolicySetId">
    /// The ID of the parent policy set, or <c>null</c> for a standalone policy.
    /// </param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns><c>Unit</c> on success, or an error (e.g., duplicate ID, parent not found).</returns>
    ValueTask<Either<EncinaError, Unit>> AddPolicyAsync(
        Policy policy,
        string? parentPolicySetId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing policy.
    /// </summary>
    /// <param name="policy">The policy with updated values. The <see cref="Policy.Id"/> must match an existing policy.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns><c>Unit</c> on success, or an error (e.g., policy not found).</returns>
    ValueTask<Either<EncinaError, Unit>> UpdatePolicyAsync(
        Policy policy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a policy by its identifier.
    /// </summary>
    /// <param name="policyId">The unique identifier of the policy to remove.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns><c>Unit</c> on success, or an error (e.g., policy not found).</returns>
    ValueTask<Either<EncinaError, Unit>> RemovePolicyAsync(
        string policyId,
        CancellationToken cancellationToken = default);
}
