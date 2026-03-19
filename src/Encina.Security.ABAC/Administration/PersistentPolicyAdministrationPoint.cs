using System.Text.Json;

using Encina.Security.Audit;
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
/// <b>Audit trail</b>:
/// When an <see cref="IAuditStore"/> is provided, all mutation operations (add, update, remove)
/// record audit entries with before/after state. Audit recording is fire-and-forget — failures
/// are logged but never block policy operations. This supports NIS2 Art. 10 and SOX §404
/// compliance requirements.
/// </para>
/// <para>
/// The store uses upsert semantics internally, while this PAP layer enforces business rules
/// such as duplicate detection and existence checks.
/// </para>
/// </remarks>
public sealed partial class PersistentPolicyAdministrationPoint : IPolicyAdministrationPoint
{
    private readonly IPolicyStore _store;
    private readonly IAuditStore? _auditStore;
    private readonly IRequestContext? _requestContext;
    private readonly ILogger<PersistentPolicyAdministrationPoint> _logger;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="PersistentPolicyAdministrationPoint"/> class.
    /// </summary>
    /// <param name="store">The policy store provider for persistent storage.</param>
    /// <param name="logger">Logger for structured PAP logging.</param>
    /// <param name="auditStore">
    /// Optional audit store for recording policy change events.
    /// When <c>null</c>, audit recording is disabled.
    /// </param>
    /// <param name="requestContext">
    /// Optional request context for resolving the actor (user ID) in audit entries.
    /// When <c>null</c>, audit entries record the actor as <c>"system"</c>.
    /// </param>
    public PersistentPolicyAdministrationPoint(
        IPolicyStore store,
        ILogger<PersistentPolicyAdministrationPoint> logger,
        IAuditStore? auditStore = null,
        IRequestContext? requestContext = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(logger);

        _store = store;
        _logger = logger;
        _auditStore = auditStore;
        _requestContext = requestContext;
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
            RecordAuditFireAndForget("PolicySetCreated", "PolicySet", policySet.Id, beforeState: null, afterState: policySet);
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

        // Capture before state for audit
        object? beforeState = null;
        if (_auditStore is not null)
        {
            var beforeResult = await _store.GetPolicySetAsync(policySet.Id, cancellationToken);
            if (beforeResult.IsRight)
            {
                beforeState = beforeResult.Match(
                    Right: opt => opt.Match(Some: ps => (object?)ps, None: () => null),
                    Left: _ => null);
            }
        }

        var saveResult = await _store.SavePolicySetAsync(policySet, cancellationToken);
        if (saveResult.IsRight)
        {
            _logger.LogDebug("Policy set '{PolicySetId}' updated", policySet.Id);
            RecordAuditFireAndForget("PolicySetUpdated", "PolicySet", policySet.Id, beforeState, afterState: policySet);
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

        // Capture before state for audit
        object? beforeState = null;
        if (_auditStore is not null)
        {
            var beforeResult = await _store.GetPolicySetAsync(policySetId, cancellationToken);
            if (beforeResult.IsRight)
            {
                beforeState = beforeResult.Match(
                    Right: opt => opt.Match(Some: ps => (object?)ps, None: () => null),
                    Left: _ => null);
            }
        }

        var deleteResult = await _store.DeletePolicySetAsync(policySetId, cancellationToken);
        if (deleteResult.IsRight)
        {
            _logger.LogDebug("Policy set '{PolicySetId}' removed", policySetId);
            RecordAuditFireAndForget("PolicySetRemoved", "PolicySet", policySetId, beforeState, afterState: null);
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
                RecordAuditFireAndForget("PolicyCreated", "Policy", policy.Id, beforeState: null, afterState: policy);
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
            RecordAuditFireAndForget("PolicyCreated", "Policy", policy.Id, beforeState: null, afterState: policy,
                new Dictionary<string, object?> { ["parentPolicySetId"] = parentPolicySetId });
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
            // Capture before state for audit
            object? beforeState = null;
            if (_auditStore is not null)
            {
                var beforeResult = await _store.GetPolicyAsync(policy.Id, cancellationToken);
                if (beforeResult.IsRight)
                {
                    beforeState = beforeResult.Match(
                        Right: opt => opt.Match(Some: p => (object?)p, None: () => null),
                        Left: _ => null);
                }
            }

            var saveResult = await _store.SavePolicyAsync(policy, cancellationToken);
            if (saveResult.IsRight)
            {
                _logger.LogDebug("Standalone policy '{PolicyId}' updated", policy.Id);
                RecordAuditFireAndForget("PolicyUpdated", "Policy", policy.Id, beforeState, afterState: policy);
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
            RecordAuditFireAndForget("PolicyUpdated", "Policy", policy.Id, found.Policy, afterState: policy,
                new Dictionary<string, object?> { ["parentPolicySetId"] = parentPolicySet.Id });
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
            // Capture before state for audit
            object? beforeState = null;
            if (_auditStore is not null)
            {
                var beforeResult = await _store.GetPolicyAsync(policyId, cancellationToken);
                if (beforeResult.IsRight)
                {
                    beforeState = beforeResult.Match(
                        Right: opt => opt.Match(Some: p => (object?)p, None: () => null),
                        Left: _ => null);
                }
            }

            var deleteResult = await _store.DeletePolicyAsync(policyId, cancellationToken);
            if (deleteResult.IsRight)
            {
                _logger.LogDebug("Standalone policy '{PolicyId}' removed", policyId);
                RecordAuditFireAndForget("PolicyRemoved", "Policy", policyId, beforeState, afterState: null);
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
            RecordAuditFireAndForget("PolicyRemoved", "Policy", policyId, found.Policy, afterState: null,
                new Dictionary<string, object?> { ["parentPolicySetId"] = parentPolicySet.Id });
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

    // ── Audit Recording (Fire-and-Forget) ───────────────────────────

    /// <summary>
    /// Records an audit entry asynchronously without blocking the calling operation.
    /// Failures are logged but never propagated.
    /// </summary>
    private void RecordAuditFireAndForget(
        string action,
        string entityType,
        string entityId,
        object? beforeState,
        object? afterState,
        Dictionary<string, object?>? additionalMetadata = null)
    {
        if (_auditStore is null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var userId = _requestContext?.UserId ?? "system";
        var correlationId = _requestContext?.CorrelationId ?? Guid.NewGuid().ToString();
        var tenantId = _requestContext?.TenantId;

        var metadata = new Dictionary<string, object?>
        {
            ["source"] = "PersistentPolicyAdministrationPoint"
        };

        if (beforeState is not null)
        {
            metadata["beforeState"] = SerializeState(beforeState);
        }

        if (afterState is not null)
        {
            metadata["afterState"] = SerializeState(afterState);
        }

        if (additionalMetadata is not null)
        {
            foreach (var kvp in additionalMetadata)
            {
                metadata[kvp.Key] = kvp.Value;
            }
        }

        var entry = new AuditEntry
        {
            Id = Guid.NewGuid(),
            CorrelationId = correlationId,
            UserId = userId,
            TenantId = tenantId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Outcome = AuditOutcome.Success,
            TimestampUtc = now.UtcDateTime,
            StartedAtUtc = now,
            CompletedAtUtc = now,
            Metadata = metadata
        };

        // Fire-and-forget: do not await, do not block
        _ = RecordAuditEntryAsync(entry);
    }

    private async Task RecordAuditEntryAsync(AuditEntry entry)
    {
        try
        {
            var result = await _auditStore!.RecordAsync(entry).ConfigureAwait(false);

            result.Match(
                Right: _ => { },
                Left: error => LogAuditRecordingFailed(
                    _logger, entry.Action, entry.EntityType, entry.EntityId ?? "unknown", error.Message));
        }
        catch (Exception ex)
        {
            LogAuditRecordingException(_logger, entry.Action, entry.EntityType, entry.EntityId ?? "unknown", ex);
        }
    }

    private static string? SerializeState(object state)
    {
        try
        {
            return JsonSerializer.Serialize(state, SerializerOptions);
        }
        catch
        {
            return null;
        }
    }

    [LoggerMessage(
        EventId = 100,
        Level = LogLevel.Warning,
        Message = "Failed to record audit entry for {Action} on {EntityType} '{EntityId}': {ErrorMessage}")]
    private static partial void LogAuditRecordingFailed(
        ILogger logger,
        string action,
        string entityType,
        string entityId,
        string errorMessage);

    [LoggerMessage(
        EventId = 101,
        Level = LogLevel.Warning,
        Message = "Exception while recording audit entry for {Action} on {EntityType} '{EntityId}'")]
    private static partial void LogAuditRecordingException(
        ILogger logger,
        string action,
        string entityType,
        string entityId,
        Exception exception);
}
