using Encina.Security.ABAC.Persistence;

using LanguageExt;

using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace Encina.Security.ABAC.Administration;

/// <summary>
/// Database-backed implementation of <see cref="IPolicyAdministrationPoint"/> that delegates
/// persistence to an <see cref="IPolicyStore"/> provider.
/// </summary>
/// <remarks>
/// <para>
/// This implementation replicates the behavior of <see cref="InMemoryPolicyAdministrationPoint"/>
/// using a persistent store instead of in-memory dictionaries. Policies can be standalone
/// (stored in the <c>abac_policies</c> table) or nested within a <see cref="PolicySet"/>
/// (embedded in the parent policy set's serialized JSON).
/// </para>
/// <para>
/// <b>Parent-child relationship handling</b>:
/// <list type="bullet">
/// <item><description>
/// <see cref="AddPolicyAsync"/> with a <c>parentPolicySetId</c> loads the parent policy set,
/// appends the policy to its <see cref="PolicySet.Policies"/> list, and saves the updated
/// policy set back to the store.
/// </description></item>
/// <item><description>
/// <see cref="GetPolicyAsync"/> searches standalone policies first, then scans all policy sets
/// for a matching nested policy.
/// </description></item>
/// <item><description>
/// <see cref="UpdatePolicyAsync"/> and <see cref="RemovePolicyAsync"/> follow the same
/// search-then-mutate pattern, modifying the parent policy set when the target policy is nested.
/// </description></item>
/// </list>
/// </para>
/// <para>
/// The store uses upsert semantics internally, while this PAP layer enforces business rules
/// such as duplicate detection and existence checks.
/// </para>
/// </remarks>
public sealed class PersistentPolicyAdministrationPoint : IPolicyAdministrationPoint
{
    private readonly IPolicyStore _store;
    private readonly ILogger<PersistentPolicyAdministrationPoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PersistentPolicyAdministrationPoint"/> class.
    /// </summary>
    /// <param name="store">The policy store provider for persistent storage.</param>
    /// <param name="logger">Logger for structured PAP logging.</param>
    public PersistentPolicyAdministrationPoint(
        IPolicyStore store,
        ILogger<PersistentPolicyAdministrationPoint> logger)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(logger);

        _store = store;
        _logger = logger;
    }

    // ── PolicySet CRUD ──────────────────────────────────────────────

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<PolicySet>>> GetPolicySetsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _store.GetAllPolicySetsAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Option<PolicySet>>> GetPolicySetAsync(
        string policySetId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policySetId);

        return await _store.GetPolicySetAsync(policySetId, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> AddPolicySetAsync(
        PolicySet policySet,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policySet);

        // Check for duplicate policy set
        var existsResult = await _store.ExistsPolicySetAsync(policySet.Id, cancellationToken);
        if (existsResult.IsLeft)
        {
            return existsResult.Map(_ => unit);
        }

        var exists = existsResult.Match(Right: v => v, Left: _ => false);
        if (exists)
        {
            return ABACErrors.DuplicatePolicySet(policySet.Id);
        }

        var saveResult = await _store.SavePolicySetAsync(policySet, cancellationToken);
        if (saveResult.IsRight)
        {
            _logger.LogDebug("Policy set '{PolicySetId}' added", policySet.Id);
        }

        return saveResult;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> UpdatePolicySetAsync(
        PolicySet policySet,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policySet);

        // Check that the policy set exists
        var existsResult = await _store.ExistsPolicySetAsync(policySet.Id, cancellationToken);
        if (existsResult.IsLeft)
        {
            return existsResult.Map(_ => unit);
        }

        var exists = existsResult.Match(Right: v => v, Left: _ => false);
        if (!exists)
        {
            return ABACErrors.PolicySetNotFound(policySet.Id);
        }

        var saveResult = await _store.SavePolicySetAsync(policySet, cancellationToken);
        if (saveResult.IsRight)
        {
            _logger.LogDebug("Policy set '{PolicySetId}' updated", policySet.Id);
        }

        return saveResult;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> RemovePolicySetAsync(
        string policySetId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policySetId);

        // Check that the policy set exists
        var existsResult = await _store.ExistsPolicySetAsync(policySetId, cancellationToken);
        if (existsResult.IsLeft)
        {
            return existsResult.Map(_ => unit);
        }

        var exists = existsResult.Match(Right: v => v, Left: _ => false);
        if (!exists)
        {
            return ABACErrors.PolicySetNotFound(policySetId);
        }

        var deleteResult = await _store.DeletePolicySetAsync(policySetId, cancellationToken);
        if (deleteResult.IsRight)
        {
            _logger.LogDebug("Policy set '{PolicySetId}' removed", policySetId);
        }

        return deleteResult;
    }

    // ── Policy CRUD ─────────────────────────────────────────────────

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<Policy>>> GetPoliciesAsync(
        string? policySetId,
        CancellationToken cancellationToken = default)
    {
        if (policySetId is null)
        {
            // Return all standalone policies
            return await _store.GetAllStandalonePoliciesAsync(cancellationToken);
        }

        // Return policies within the specified policy set
        var policySetResult = await _store.GetPolicySetAsync(policySetId, cancellationToken);
        if (policySetResult.IsLeft)
        {
            return policySetResult.Map<IReadOnlyList<Policy>>(_ => []);
        }

        var optionPs = policySetResult.Match(Right: v => v, Left: _ => None);
        return optionPs.Match(
            Some: ps => Right<EncinaError, IReadOnlyList<Policy>>(ps.Policies),
            None: () => Left<EncinaError, IReadOnlyList<Policy>>(
                ABACErrors.PolicySetNotFound(policySetId)));
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Option<Policy>>> GetPolicyAsync(
        string policyId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyId);

        // Check standalone policies first
        var standaloneResult = await _store.GetPolicyAsync(policyId, cancellationToken);
        if (standaloneResult.IsLeft)
        {
            return standaloneResult;
        }

        var standaloneOption = standaloneResult.Match(Right: v => v, Left: _ => None);
        if (standaloneOption.IsSome)
        {
            return Right<EncinaError, Option<Policy>>(standaloneOption);
        }

        // Search through all policy sets for a nested policy
        var searchResult = await FindPolicyInPolicySetsAsync(policyId, cancellationToken);
        if (searchResult.IsLeft)
        {
            return searchResult.Map<Option<Policy>>(_ => None);
        }

        var foundOption = searchResult.Match(
            Right: v => v,
            Left: _ => Option<(PolicySet Parent, Policy Policy)>.None);

        return foundOption.Match(
            Some: found => Right<EncinaError, Option<Policy>>(Some(found.Policy)),
            None: () => Right<EncinaError, Option<Policy>>(None));
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> AddPolicyAsync(
        Policy policy,
        string? parentPolicySetId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policy);

        // Check for duplicates across standalone policies
        var standaloneExistsResult = await _store.ExistsPolicyAsync(policy.Id, cancellationToken);
        if (standaloneExistsResult.IsLeft)
        {
            return standaloneExistsResult.Map(_ => unit);
        }

        var standaloneExists = standaloneExistsResult.Match(Right: v => v, Left: _ => false);
        if (standaloneExists)
        {
            return ABACErrors.DuplicatePolicy(policy.Id);
        }

        // Check for duplicates across nested policies in policy sets
        var nestedSearchResult = await FindPolicyInPolicySetsAsync(policy.Id, cancellationToken);
        if (nestedSearchResult.IsLeft)
        {
            return nestedSearchResult.Map(_ => unit);
        }

        var nestedFoundOption = nestedSearchResult.Match(
            Right: v => v,
            Left: _ => Option<(PolicySet Parent, Policy Policy)>.None);

        if (nestedFoundOption.IsSome)
        {
            return ABACErrors.DuplicatePolicy(policy.Id);
        }

        if (parentPolicySetId is null)
        {
            // Add as standalone policy
            var saveResult = await _store.SavePolicyAsync(policy, cancellationToken);
            if (saveResult.IsRight)
            {
                _logger.LogDebug("Standalone policy '{PolicyId}' added", policy.Id);
            }

            return saveResult;
        }

        // Add to the specified parent policy set
        var parentResult = await _store.GetPolicySetAsync(parentPolicySetId, cancellationToken);
        if (parentResult.IsLeft)
        {
            return parentResult.Map(_ => unit);
        }

        var parentOption = parentResult.Match(Right: v => v, Left: _ => None);
        if (parentOption.IsNone)
        {
            return ABACErrors.PolicySetNotFound(parentPolicySetId);
        }

        var parentPolicySet = parentOption.Match(
            Some: v => v,
            None: () => throw new InvalidOperationException("Unreachable: IsNone was checked above."));

        var updatedPolicies = new List<Policy>(parentPolicySet.Policies) { policy };
        var updatedPolicySet = parentPolicySet with { Policies = updatedPolicies };

        var addToParentResult = await _store.SavePolicySetAsync(updatedPolicySet, cancellationToken);
        if (addToParentResult.IsRight)
        {
            _logger.LogDebug(
                "Policy '{PolicyId}' added to policy set '{PolicySetId}'",
                policy.Id,
                parentPolicySetId);
        }

        return addToParentResult;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> UpdatePolicyAsync(
        Policy policy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policy);

        // Check standalone policies first
        var standaloneExistsResult = await _store.ExistsPolicyAsync(policy.Id, cancellationToken);
        if (standaloneExistsResult.IsLeft)
        {
            return standaloneExistsResult.Map(_ => unit);
        }

        var standaloneExists = standaloneExistsResult.Match(Right: v => v, Left: _ => false);
        if (standaloneExists)
        {
            var saveResult = await _store.SavePolicyAsync(policy, cancellationToken);
            if (saveResult.IsRight)
            {
                _logger.LogDebug("Standalone policy '{PolicyId}' updated", policy.Id);
            }

            return saveResult;
        }

        // Search for the policy in policy sets
        var searchResult = await FindPolicyInPolicySetsAsync(policy.Id, cancellationToken);
        if (searchResult.IsLeft)
        {
            return searchResult.Map(_ => unit);
        }

        var foundOption = searchResult.Match(
            Right: v => v,
            Left: _ => Option<(PolicySet Parent, Policy Policy)>.None);

        if (foundOption.IsNone)
        {
            return ABACErrors.PolicyNotFound(policy.Id);
        }

        // Update the policy within the parent policy set
        var found = foundOption.Match(
            Some: v => v,
            None: () => throw new InvalidOperationException("Unreachable: IsNone was checked above."));

        var parentPolicySet = found.Parent;
        var updatedPolicies = parentPolicySet.Policies
            .Select(p => p.Id == policy.Id ? policy : p)
            .ToList();

        var updatedPolicySet = parentPolicySet with { Policies = updatedPolicies };
        var updateResult = await _store.SavePolicySetAsync(updatedPolicySet, cancellationToken);
        if (updateResult.IsRight)
        {
            _logger.LogDebug(
                "Policy '{PolicyId}' updated in policy set '{PolicySetId}'",
                policy.Id,
                parentPolicySet.Id);
        }

        return updateResult;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> RemovePolicyAsync(
        string policyId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyId);

        // Try removing from standalone policies first
        var standaloneExistsResult = await _store.ExistsPolicyAsync(policyId, cancellationToken);
        if (standaloneExistsResult.IsLeft)
        {
            return standaloneExistsResult.Map(_ => unit);
        }

        var standaloneExists = standaloneExistsResult.Match(Right: v => v, Left: _ => false);
        if (standaloneExists)
        {
            var deleteResult = await _store.DeletePolicyAsync(policyId, cancellationToken);
            if (deleteResult.IsRight)
            {
                _logger.LogDebug("Standalone policy '{PolicyId}' removed", policyId);
            }

            return deleteResult;
        }

        // Search for the policy in policy sets
        var searchResult = await FindPolicyInPolicySetsAsync(policyId, cancellationToken);
        if (searchResult.IsLeft)
        {
            return searchResult.Map(_ => unit);
        }

        var foundOption = searchResult.Match(
            Right: v => v,
            Left: _ => Option<(PolicySet Parent, Policy Policy)>.None);

        if (foundOption.IsNone)
        {
            return ABACErrors.PolicyNotFound(policyId);
        }

        // Remove the policy from the parent policy set
        var found = foundOption.Match(
            Some: v => v,
            None: () => throw new InvalidOperationException("Unreachable: IsNone was checked above."));

        var parentPolicySet = found.Parent;
        var updatedPolicies = parentPolicySet.Policies
            .Where(p => p.Id != policyId)
            .ToList();

        var updatedPolicySet = parentPolicySet with { Policies = updatedPolicies };
        var saveResult = await _store.SavePolicySetAsync(updatedPolicySet, cancellationToken);
        if (saveResult.IsRight)
        {
            _logger.LogDebug(
                "Policy '{PolicyId}' removed from policy set '{PolicySetId}'",
                policyId,
                parentPolicySet.Id);
        }

        return saveResult;
    }

    // ── Private Helpers ─────────────────────────────────────────────

    /// <summary>
    /// Searches all top-level policy sets for a policy with the specified identifier.
    /// Returns <see cref="Option{T}.None"/> if the policy is not found in any policy set.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="Option{T}"/> (a value type) instead of a nullable tuple because
    /// LanguageExt's <c>Map</c>/<c>Match&lt;Ret&gt;</c> throws <c>ValueIsNullException</c>
    /// when the mapping function returns <c>null</c> for reference types.
    /// </remarks>
    private async ValueTask<Either<EncinaError, Option<(PolicySet Parent, Policy Policy)>>> FindPolicyInPolicySetsAsync(
        string policyId,
        CancellationToken cancellationToken)
    {
        var policySetsResult = await _store.GetAllPolicySetsAsync(cancellationToken);

        // LanguageExt's Map internally calls Either.Right(result), which throws
        // ValueIsNullException when result is null. We unpack manually instead.
        if (policySetsResult.IsLeft)
        {
            var error = policySetsResult.Match(
                Left: err => err,
                Right: _ => EncinaError.New("Unreachable"));
            return Either<EncinaError, Option<(PolicySet Parent, Policy Policy)>>.Left(error);
        }

        var policySets = policySetsResult.Match(
            Right: v => v,
            Left: _ => (IReadOnlyList<PolicySet>)System.Array.Empty<PolicySet>());

        foreach (var policySet in policySets)
        {
            var policy = policySet.Policies.FirstOrDefault(p => p.Id == policyId);
            if (policy is not null)
            {
                return Some((Parent: policySet, Policy: policy));
            }
        }

        return Option<(PolicySet Parent, Policy Policy)>.None;
    }
}
