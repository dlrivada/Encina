using System.Collections.Concurrent;

using LanguageExt;

using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace Encina.Security.ABAC.Administration;

/// <summary>
/// In-memory implementation of <see cref="IPolicyAdministrationPoint"/> for development,
/// testing, and simple deployments.
/// </summary>
/// <remarks>
/// <para>
/// Policies and policy sets are stored in <see cref="ConcurrentDictionary{TKey,TValue}"/>
/// instances, ensuring thread-safe concurrent access. Standalone policies (those not
/// belonging to any policy set) are stored separately from policies nested within policy sets.
/// </para>
/// <para>
/// <b>Not suitable for production</b>: Records are lost when the process restarts.
/// For production use, consider database-backed implementations.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var pap = new InMemoryPolicyAdministrationPoint(logger);
/// await pap.AddPolicySetAsync(policySet);
/// await pap.AddPolicyAsync(policy, parentPolicySetId: "access-control");
/// var policySets = await pap.GetPolicySetsAsync();
/// </code>
/// </example>
public sealed class InMemoryPolicyAdministrationPoint : IPolicyAdministrationPoint
{
    private readonly ConcurrentDictionary<string, PolicySet> _policySets = new();
    private readonly ConcurrentDictionary<string, Policy> _standalonePolicies = new();
    private readonly ConcurrentDictionary<string, string> _policyToParent = new();
    private readonly ILogger<InMemoryPolicyAdministrationPoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryPolicyAdministrationPoint"/> class.
    /// </summary>
    /// <param name="logger">Logger for structured PAP logging.</param>
    public InMemoryPolicyAdministrationPoint(ILogger<InMemoryPolicyAdministrationPoint> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    // ── PolicySet CRUD ──────────────────────────────────────────────

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<PolicySet>>> GetPolicySetsAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PolicySet> result = _policySets.Values.ToList().AsReadOnly();
        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<PolicySet>>>(Right(result));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Option<PolicySet>>> GetPolicySetAsync(
        string policySetId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policySetId);

        if (!_policySets.TryGetValue(policySetId, out var policySet))
        {
            return ValueTask.FromResult<Either<EncinaError, Option<PolicySet>>>(
                Right<EncinaError, Option<PolicySet>>(None));
        }

        return ValueTask.FromResult<Either<EncinaError, Option<PolicySet>>>(
            Right<EncinaError, Option<PolicySet>>(Some(policySet)));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> AddPolicySetAsync(
        PolicySet policySet,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policySet);

        if (!_policySets.TryAdd(policySet.Id, policySet))
        {
            return ValueTask.FromResult<Either<EncinaError, Unit>>(
                ABACErrors.DuplicatePolicySet(policySet.Id));
        }

        // Track policies within this policy set
        foreach (var policy in policySet.Policies)
        {
            _policyToParent[policy.Id] = policySet.Id;
        }

        _logger.LogDebug("Policy set '{PolicySetId}' added", policySet.Id);

        return ValueTask.FromResult<Either<EncinaError, Unit>>(unit);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> UpdatePolicySetAsync(
        PolicySet policySet,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policySet);

        if (!_policySets.ContainsKey(policySet.Id))
        {
            return ValueTask.FromResult<Either<EncinaError, Unit>>(
                ABACErrors.PolicySetNotFound(policySet.Id));
        }

        // Remove old policy-to-parent mappings
        if (_policySets.TryGetValue(policySet.Id, out var oldPolicySet))
        {
            foreach (var oldPolicy in oldPolicySet.Policies)
            {
                _policyToParent.TryRemove(oldPolicy.Id, out _);
            }
        }

        _policySets[policySet.Id] = policySet;

        // Re-track policies
        foreach (var policy in policySet.Policies)
        {
            _policyToParent[policy.Id] = policySet.Id;
        }

        _logger.LogDebug("Policy set '{PolicySetId}' updated", policySet.Id);

        return ValueTask.FromResult<Either<EncinaError, Unit>>(unit);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> RemovePolicySetAsync(
        string policySetId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policySetId);

        if (!_policySets.TryRemove(policySetId, out var removed))
        {
            return ValueTask.FromResult<Either<EncinaError, Unit>>(
                ABACErrors.PolicySetNotFound(policySetId));
        }

        // Remove policy-to-parent mappings for contained policies
        foreach (var policy in removed.Policies)
        {
            _policyToParent.TryRemove(policy.Id, out _);
        }

        _logger.LogDebug("Policy set '{PolicySetId}' removed", policySetId);

        return ValueTask.FromResult<Either<EncinaError, Unit>>(unit);
    }

    // ── Policy CRUD ─────────────────────────────────────────────────

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<Policy>>> GetPoliciesAsync(
        string? policySetId,
        CancellationToken cancellationToken = default)
    {
        if (policySetId is null)
        {
            // Return all standalone policies
            IReadOnlyList<Policy> standalone = _standalonePolicies.Values.ToList().AsReadOnly();
            return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<Policy>>>(Right(standalone));
        }

        // Return policies within the specified policy set
        if (!_policySets.TryGetValue(policySetId, out var policySet))
        {
            return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<Policy>>>(
                ABACErrors.PolicySetNotFound(policySetId));
        }

        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<Policy>>>(
            Right<EncinaError, IReadOnlyList<Policy>>(policySet.Policies));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Option<Policy>>> GetPolicyAsync(
        string policyId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyId);

        // Check standalone policies first
        if (_standalonePolicies.TryGetValue(policyId, out var standalonePolicy))
        {
            return ValueTask.FromResult<Either<EncinaError, Option<Policy>>>(
                Right<EncinaError, Option<Policy>>(Some(standalonePolicy)));
        }

        // Check if policy belongs to a policy set
        if (_policyToParent.TryGetValue(policyId, out var parentId) &&
            _policySets.TryGetValue(parentId, out var parentPolicySet))
        {
            var policy = parentPolicySet.Policies.FirstOrDefault(p => p.Id == policyId);
            if (policy is not null)
            {
                return ValueTask.FromResult<Either<EncinaError, Option<Policy>>>(
                    Right<EncinaError, Option<Policy>>(Some(policy)));
            }
        }

        return ValueTask.FromResult<Either<EncinaError, Option<Policy>>>(
            Right<EncinaError, Option<Policy>>(None));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> AddPolicyAsync(
        Policy policy,
        string? parentPolicySetId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policy);

        // Check for duplicates across standalone and nested policies
        if (_standalonePolicies.ContainsKey(policy.Id) || _policyToParent.ContainsKey(policy.Id))
        {
            return ValueTask.FromResult<Either<EncinaError, Unit>>(
                ABACErrors.DuplicatePolicy(policy.Id));
        }

        if (parentPolicySetId is null)
        {
            // Add as standalone policy
            if (!_standalonePolicies.TryAdd(policy.Id, policy))
            {
                return ValueTask.FromResult<Either<EncinaError, Unit>>(
                    ABACErrors.DuplicatePolicy(policy.Id));
            }

            _logger.LogDebug("Standalone policy '{PolicyId}' added", policy.Id);
        }
        else
        {
            // Add to the specified parent policy set
            if (!_policySets.TryGetValue(parentPolicySetId, out var parentPolicySet))
            {
                return ValueTask.FromResult<Either<EncinaError, Unit>>(
                    ABACErrors.PolicySetNotFound(parentPolicySetId));
            }

            var updatedPolicies = new List<Policy>(parentPolicySet.Policies) { policy };
            var updatedPolicySet = parentPolicySet with { Policies = updatedPolicies };
            _policySets[parentPolicySetId] = updatedPolicySet;
            _policyToParent[policy.Id] = parentPolicySetId;

            _logger.LogDebug(
                "Policy '{PolicyId}' added to policy set '{PolicySetId}'",
                policy.Id,
                parentPolicySetId);
        }

        return ValueTask.FromResult<Either<EncinaError, Unit>>(unit);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> UpdatePolicyAsync(
        Policy policy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policy);

        // Check standalone policies
        if (_standalonePolicies.ContainsKey(policy.Id))
        {
            _standalonePolicies[policy.Id] = policy;
            _logger.LogDebug("Standalone policy '{PolicyId}' updated", policy.Id);
            return ValueTask.FromResult<Either<EncinaError, Unit>>(unit);
        }

        // Check nested policies
        if (_policyToParent.TryGetValue(policy.Id, out var parentId) &&
            _policySets.TryGetValue(parentId, out var parentPolicySet))
        {
            var updatedPolicies = parentPolicySet.Policies
                .Select(p => p.Id == policy.Id ? policy : p)
                .ToList();

            _policySets[parentId] = parentPolicySet with { Policies = updatedPolicies };

            _logger.LogDebug(
                "Policy '{PolicyId}' updated in policy set '{PolicySetId}'",
                policy.Id,
                parentId);

            return ValueTask.FromResult<Either<EncinaError, Unit>>(unit);
        }

        return ValueTask.FromResult<Either<EncinaError, Unit>>(
            ABACErrors.PolicyNotFound(policy.Id));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> RemovePolicyAsync(
        string policyId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyId);

        // Try removing from standalone policies
        if (_standalonePolicies.TryRemove(policyId, out _))
        {
            _logger.LogDebug("Standalone policy '{PolicyId}' removed", policyId);
            return ValueTask.FromResult<Either<EncinaError, Unit>>(unit);
        }

        // Try removing from a parent policy set
        if (_policyToParent.TryRemove(policyId, out var parentId) &&
            _policySets.TryGetValue(parentId, out var parentPolicySet))
        {
            var updatedPolicies = parentPolicySet.Policies
                .Where(p => p.Id != policyId)
                .ToList();

            _policySets[parentId] = parentPolicySet with { Policies = updatedPolicies };

            _logger.LogDebug(
                "Policy '{PolicyId}' removed from policy set '{PolicySetId}'",
                policyId,
                parentId);

            return ValueTask.FromResult<Either<EncinaError, Unit>>(unit);
        }

        return ValueTask.FromResult<Either<EncinaError, Unit>>(
            ABACErrors.PolicyNotFound(policyId));
    }

    // ── Test Helpers ────────────────────────────────────────────────

    /// <summary>
    /// Clears all policy sets and policies from the store.
    /// </summary>
    /// <remarks>Intended for testing only to reset state between tests.</remarks>
    public void Clear()
    {
        _policySets.Clear();
        _standalonePolicies.Clear();
        _policyToParent.Clear();
    }

    /// <summary>
    /// Gets the total number of policy sets in the store.
    /// </summary>
    public int PolicySetCount => _policySets.Count;

    /// <summary>
    /// Gets the total number of standalone policies in the store.
    /// </summary>
    public int StandalonePolicyCount => _standalonePolicies.Count;
}
