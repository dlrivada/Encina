using System.Text.Json;
using Encina.Diagnostics;
using Encina.Security.ABAC.Persistence;
using Encina.Security.Audit;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
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
/// <b>Principal</b>: every mutation (add, update, remove) is attributed to the principal of the
/// ambient <see cref="IRequestContext"/> and is refused with
/// <see cref="ABACErrors.PolicyChangePrincipalRequiredCode"/> when none can be resolved. The only
/// exception is the explicit system-actor scope that internal callers open (the policy seeding
/// hosted service at startup); the audit entry then records the actor as <c>system</c>.
/// </para>
/// <para>
/// <b>Lifetimes</b>: this PAP is a singleton. It never captures a scoped service: every operation
/// (a read or a mutation) opens its own async DI scope and resolves the <see cref="IPolicyStore"/>
/// from it, and a mutation resolves its <see cref="IOperationAuditStore"/> in a separate scope, so two
/// concurrent operations never share a store instance and the audit write never shares a unit of
/// work with the policy write (database stores are scoped).
/// </para>
/// <para>
/// <b>Audit trail (fail closed)</b>: when an <see cref="IOperationAuditStore"/> is registered, each mutation
/// awaits the audit write <em>before</em> the change is
/// applied. A <c>Left</c>, an exception or a timeout of that write fails the policy change with
/// <see cref="ABACErrors.PolicyChangeAuditFailedCode"/> and nothing is persisted, so no policy
/// change is ever committed without its audit record. When the change itself is then rejected by
/// the policy store, a second entry with outcome <see cref="AuditOutcome.Error"/> records that
/// the announced change did not happen. With no <see cref="IOperationAuditStore"/> registered, policy
/// change auditing is not configured and mutations are applied without a record, and a Warning (EventId 9097) says so once per instance. This supports
/// NIS2 Art. 10 and SOX §404 compliance requirements.
/// </para>
/// <para>
/// The store uses upsert semantics internally, while this PAP layer enforces business rules
/// such as duplicate detection and existence checks.
/// </para>
/// </remarks>
public sealed partial class PersistentPolicyAdministrationPoint : IPolicyAdministrationPoint
{
    private const string SystemActorId = "system";

    /// <summary>The longest an audit write may take before the policy change fails closed.</summary>
    private static readonly TimeSpan AuditWriteTimeout = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Func<IServiceProvider, IPolicyStore> _storeResolver;
    private readonly IRequestContextAccessor? _requestContextAccessor;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<PersistentPolicyAdministrationPoint> _logger;
    private int _unauditedWarningLogged;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly record struct PolicyActor(string UserId, string? TenantId, string CorrelationId, bool IsSystem);

    /// <summary>
    /// The state of one PAP operation: the policy store of the operation's own DI scope, plus the
    /// actor of a mutation.
    /// </summary>
    private readonly record struct PolicyOperation(IPolicyStore Store, PolicyActor Actor);

    private sealed record PolicyChange(
        string Action,
        string EntityType,
        string EntityId,
        object? After,
        Func<CancellationToken, ValueTask<object?>>? LoadBefore = null,
        IReadOnlyDictionary<string, object?>? Extra = null);

    /// <summary>
    /// Initializes a new instance of the <see cref="PersistentPolicyAdministrationPoint"/> class.
    /// </summary>
    /// <param name="scopeFactory">
    /// Scope factory used to open one DI scope per operation (and a separate one for the
    /// <see cref="IOperationAuditStore"/> of a mutation). The <see cref="IPolicyStore"/> (a scoped service
    /// in every database provider) is resolved from the operation's scope, so this singleton
    /// never captures a scoped service. When no <see cref="IOperationAuditStore"/> is registered, policy change auditing is
    /// not configured.
    /// </param>
    /// <param name="logger">Logger for structured PAP logging.</param>
    /// <param name="requestContextAccessor">
    /// Optional accessor for the ambient request context, used to resolve the principal of each
    /// change at the moment it is made (this PAP is registered as a singleton, so the context
    /// cannot be captured at construction time). Without a resolvable principal every mutation is
    /// refused, except inside the internal system-actor scope.
    /// </param>
    /// <param name="timeProvider">
    /// Optional time provider for every timestamp and the audit write timeout.
    /// Defaults to <see cref="TimeProvider.System"/>.
    /// </param>
    /// <param name="storeResolver">
    /// Optional function that returns the <see cref="IPolicyStore"/> of an operation from the
    /// operation's scope (the registration uses it to wrap the scoped store with the caching
    /// decorator). Defaults to resolving the registered <see cref="IPolicyStore"/> as is.
    /// </param>
    public PersistentPolicyAdministrationPoint(
        IServiceScopeFactory scopeFactory,
        ILogger<PersistentPolicyAdministrationPoint> logger,
        IRequestContextAccessor? requestContextAccessor = null,
        TimeProvider? timeProvider = null,
        Func<IServiceProvider, IPolicyStore>? storeResolver = null)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(logger);

        _scopeFactory = scopeFactory;
        _logger = logger;
        _requestContextAccessor = requestContextAccessor;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _storeResolver = storeResolver ?? (static services => services.GetRequiredService<IPolicyStore>());
    }

    /// <summary>
    /// Runs one read in its own DI scope. The store lives exactly as long as the read, so two
    /// concurrent operations never share a store instance.
    /// </summary>
    private async ValueTask<T> ReadAsync<T>(Func<IPolicyStore, ValueTask<T>> read)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        return await read(_storeResolver(scope.ServiceProvider));
    }

    // ── PolicySet CRUD ──────────────────────────────────────────────

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<PolicySet>>> GetPolicySetsAsync(
        CancellationToken cancellationToken = default)
    {
        return await ReadAsync(store => store.GetAllPolicySetsAsync(cancellationToken));
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Option<PolicySet>>> GetPolicySetAsync(
        string policySetId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policySetId);

        return await ReadAsync(store => store.GetPolicySetAsync(policySetId, cancellationToken));
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> AddPolicySetAsync(
        PolicySet policySet,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policySet);

        return await RunAsActorAsync(actor => AddPolicySetCoreAsync(actor, policySet, cancellationToken));
    }

    private async ValueTask<Either<EncinaError, Unit>> AddPolicySetCoreAsync(
        PolicyOperation op,
        PolicySet policySet,
        CancellationToken cancellationToken)
    {
        var existsResult = await op.Store.ExistsPolicySetAsync(policySet.Id, cancellationToken);
        if (existsResult.IsLeft)
        {
            return existsResult.Map(_ => unit);
        }

        if (existsResult.Match(Right: v => v, Left: _ => false))
        {
            return ABACErrors.DuplicatePolicySet(policySet.Id);
        }

        var change = new PolicyChange("PolicySetCreated", "PolicySet", policySet.Id, policySet);
        return await ApplyAsync(op, change, () => op.Store.SavePolicySetAsync(policySet, cancellationToken), cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> UpdatePolicySetAsync(
        PolicySet policySet,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policySet);

        return await RunAsActorAsync(actor => UpdatePolicySetCoreAsync(actor, policySet, cancellationToken));
    }

    private async ValueTask<Either<EncinaError, Unit>> UpdatePolicySetCoreAsync(
        PolicyOperation op,
        PolicySet policySet,
        CancellationToken cancellationToken)
    {
        var existsResult = await op.Store.ExistsPolicySetAsync(policySet.Id, cancellationToken);
        if (existsResult.IsLeft)
        {
            return existsResult.Map(_ => unit);
        }

        if (!existsResult.Match(Right: v => v, Left: _ => false))
        {
            return ABACErrors.PolicySetNotFound(policySet.Id);
        }

        var change = new PolicyChange(
            "PolicySetUpdated", "PolicySet", policySet.Id, policySet, LoadBefore: ct => LoadPolicySetStateAsync(op.Store, policySet.Id, ct));
        return await ApplyAsync(op, change, () => op.Store.SavePolicySetAsync(policySet, cancellationToken), cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> RemovePolicySetAsync(
        string policySetId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policySetId);

        return await RunAsActorAsync(actor => RemovePolicySetCoreAsync(actor, policySetId, cancellationToken));
    }

    private async ValueTask<Either<EncinaError, Unit>> RemovePolicySetCoreAsync(
        PolicyOperation op,
        string policySetId,
        CancellationToken cancellationToken)
    {
        var existsResult = await op.Store.ExistsPolicySetAsync(policySetId, cancellationToken);
        if (existsResult.IsLeft)
        {
            return existsResult.Map(_ => unit);
        }

        if (!existsResult.Match(Right: v => v, Left: _ => false))
        {
            return ABACErrors.PolicySetNotFound(policySetId);
        }

        var change = new PolicyChange(
            "PolicySetRemoved", "PolicySet", policySetId, After: null, LoadBefore: ct => LoadPolicySetStateAsync(op.Store, policySetId, ct));
        return await ApplyAsync(op, change, () => op.Store.DeletePolicySetAsync(policySetId, cancellationToken), cancellationToken);
    }

    // ── Policy CRUD ─────────────────────────────────────────────────

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<Policy>>> GetPoliciesAsync(
        string? policySetId,
        CancellationToken cancellationToken = default)
    {
        return await ReadAsync(store => GetPoliciesCoreAsync(store, policySetId, cancellationToken));
    }

    private static async ValueTask<Either<EncinaError, IReadOnlyList<Policy>>> GetPoliciesCoreAsync(
        IPolicyStore store,
        string? policySetId,
        CancellationToken cancellationToken)
    {
        if (policySetId is null)
        {
            // Return all standalone policies
            return await store.GetAllStandalonePoliciesAsync(cancellationToken);
        }

        // Return policies within the specified policy set
        var policySetResult = await store.GetPolicySetAsync(policySetId, cancellationToken);
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

        return await ReadAsync(store => GetPolicyCoreAsync(store, policyId, cancellationToken));
    }

    private static async ValueTask<Either<EncinaError, Option<Policy>>> GetPolicyCoreAsync(
        IPolicyStore store,
        string policyId,
        CancellationToken cancellationToken)
    {
        // Check standalone policies first
        var standaloneResult = await store.GetPolicyAsync(policyId, cancellationToken);
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
        var searchResult = await FindPolicyInPolicySetsAsync(store, policyId, cancellationToken);
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

        return await RunAsActorAsync(actor => AddPolicyCoreAsync(actor, policy, parentPolicySetId, cancellationToken));
    }

    private async ValueTask<Either<EncinaError, Unit>> AddPolicyCoreAsync(
        PolicyOperation op,
        Policy policy,
        string? parentPolicySetId,
        CancellationToken cancellationToken)
    {
        var free = await EnsurePolicyIdIsFreeAsync(op.Store, policy.Id, cancellationToken);
        if (free.IsLeft)
        {
            return free;
        }

        if (parentPolicySetId is not null)
        {
            return await AddPolicyToParentAsync(op, policy, parentPolicySetId, cancellationToken);
        }

        var change = new PolicyChange("PolicyCreated", "Policy", policy.Id, policy);
        return await ApplyAsync(op, change, () => op.Store.SavePolicyAsync(policy, cancellationToken), cancellationToken);
    }

    private async ValueTask<Either<EncinaError, Unit>> AddPolicyToParentAsync(
        PolicyOperation op,
        Policy policy,
        string parentPolicySetId,
        CancellationToken cancellationToken)
    {
        var parentResult = await op.Store.GetPolicySetAsync(parentPolicySetId, cancellationToken);
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

        var updatedPolicySet = parentPolicySet with { Policies = [.. parentPolicySet.Policies, policy] };
        var change = new PolicyChange(
            "PolicyCreated", "Policy", policy.Id, policy, Extra: ParentMetadata(parentPolicySetId));
        return await ApplyAsync(op, change, () => op.Store.SavePolicySetAsync(updatedPolicySet, cancellationToken), cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> UpdatePolicyAsync(
        Policy policy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policy);

        return await RunAsActorAsync(actor => UpdatePolicyCoreAsync(actor, policy, cancellationToken));
    }

    private async ValueTask<Either<EncinaError, Unit>> UpdatePolicyCoreAsync(
        PolicyOperation op,
        Policy policy,
        CancellationToken cancellationToken)
    {
        var standaloneExistsResult = await op.Store.ExistsPolicyAsync(policy.Id, cancellationToken);
        if (standaloneExistsResult.IsLeft)
        {
            return standaloneExistsResult.Map(_ => unit);
        }

        if (!standaloneExistsResult.Match(Right: v => v, Left: _ => false))
        {
            return await ChangeNestedPolicyAsync(op, policy.Id, "PolicyUpdated", policy, cancellationToken);
        }

        var change = new PolicyChange(
            "PolicyUpdated", "Policy", policy.Id, policy, LoadBefore: ct => LoadPolicyStateAsync(op.Store, policy.Id, ct));
        return await ApplyAsync(op, change, () => op.Store.SavePolicyAsync(policy, cancellationToken), cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> RemovePolicyAsync(
        string policyId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyId);

        return await RunAsActorAsync(actor => RemovePolicyCoreAsync(actor, policyId, cancellationToken));
    }

    private async ValueTask<Either<EncinaError, Unit>> RemovePolicyCoreAsync(
        PolicyOperation op,
        string policyId,
        CancellationToken cancellationToken)
    {
        var standaloneExistsResult = await op.Store.ExistsPolicyAsync(policyId, cancellationToken);
        if (standaloneExistsResult.IsLeft)
        {
            return standaloneExistsResult.Map(_ => unit);
        }

        if (!standaloneExistsResult.Match(Right: v => v, Left: _ => false))
        {
            return await ChangeNestedPolicyAsync(op, policyId, "PolicyRemoved", replacement: null, cancellationToken);
        }

        var change = new PolicyChange(
            "PolicyRemoved", "Policy", policyId, After: null, LoadBefore: ct => LoadPolicyStateAsync(op.Store, policyId, ct));
        return await ApplyAsync(op, change, () => op.Store.DeletePolicyAsync(policyId, cancellationToken), cancellationToken);
    }

    // ── Private Helpers ─────────────────────────────────────────────

    /// <summary>
    /// Replaces (when <paramref name="replacement"/> is set) or removes a policy nested in a
    /// policy set by saving the rewritten parent policy set.
    /// </summary>
    private async ValueTask<Either<EncinaError, Unit>> ChangeNestedPolicyAsync(
        PolicyOperation op,
        string policyId,
        string action,
        Policy? replacement,
        CancellationToken cancellationToken)
    {
        var searchResult = await FindPolicyInPolicySetsAsync(op.Store, policyId, cancellationToken);
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

        var found = foundOption.Match(
            Some: v => v,
            None: () => throw new InvalidOperationException("Unreachable: IsNone was checked above."));

        var rewritten = replacement is null
            ? found.Parent.Policies.Where(p => p.Id != policyId).ToList()
            : found.Parent.Policies.Select(p => p.Id == policyId ? replacement : p).ToList();

        var updatedPolicySet = found.Parent with { Policies = rewritten };
        var change = new PolicyChange(
            action, "Policy", policyId, replacement,
            LoadBefore: _ => ValueTask.FromResult<object?>(found.Policy),
            Extra: ParentMetadata(found.Parent.Id));
        return await ApplyAsync(op, change, () => op.Store.SavePolicySetAsync(updatedPolicySet, cancellationToken), cancellationToken);
    }

    /// <summary>Returns an error when the policy identifier is already used by a standalone or nested policy.</summary>
    private static async ValueTask<Either<EncinaError, Unit>> EnsurePolicyIdIsFreeAsync(
        IPolicyStore store,
        string policyId,
        CancellationToken cancellationToken)
    {
        var standaloneExistsResult = await store.ExistsPolicyAsync(policyId, cancellationToken);
        if (standaloneExistsResult.IsLeft)
        {
            return standaloneExistsResult.Map(_ => unit);
        }

        if (standaloneExistsResult.Match(Right: v => v, Left: _ => false))
        {
            return ABACErrors.DuplicatePolicy(policyId);
        }

        var nestedSearchResult = await FindPolicyInPolicySetsAsync(store, policyId, cancellationToken);
        if (nestedSearchResult.IsLeft)
        {
            return nestedSearchResult.Map(_ => unit);
        }

        if (nestedSearchResult.Match(Right: found => found.IsSome, Left: _ => false))
        {
            return ABACErrors.DuplicatePolicy(policyId);
        }

        return unit;
    }

    private static Dictionary<string, object?> ParentMetadata(string parentPolicySetId) =>
        new Dictionary<string, object?> { ["parentPolicySetId"] = parentPolicySetId };

    private static async ValueTask<object?> LoadPolicySetStateAsync(IPolicyStore store, string policySetId, CancellationToken cancellationToken)
    {
        var result = await store.GetPolicySetAsync(policySetId, cancellationToken);
        return result.Match(
            Right: opt => opt.Match(Some: ps => (object?)ps, None: () => null),
            Left: _ => null);
    }

    private static async ValueTask<object?> LoadPolicyStateAsync(IPolicyStore store, string policyId, CancellationToken cancellationToken)
    {
        var result = await store.GetPolicyAsync(policyId, cancellationToken);
        return result.Match(
            Right: opt => opt.Match(Some: p => (object?)p, None: () => null),
            Left: _ => null);
    }

    /// <summary>
    /// Searches all top-level policy sets for a policy with the specified identifier.
    /// Returns <see cref="Option{T}.None"/> if the policy is not found in any policy set.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="Option{T}"/> (a value type) instead of a nullable tuple because
    /// LanguageExt's <c>Map</c>/<c>Match&lt;Ret&gt;</c> throws <c>ValueIsNullException</c>
    /// when the mapping function returns <c>null</c> for reference types.
    /// </remarks>
    private static async ValueTask<Either<EncinaError, Option<(PolicySet Parent, Policy Policy)>>> FindPolicyInPolicySetsAsync(
        IPolicyStore store,
        string policyId,
        CancellationToken cancellationToken)
    {
        var policySetsResult = await store.GetAllPolicySetsAsync(cancellationToken);

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

    // ── Principal and audited application of a change ───────────────

    /// <summary>
    /// Runs one mutation for the resolved actor in its own DI scope. The policy store and every
    /// read the mutation makes come from that one scope; the audit store has a scope of its own
    /// (see <see cref="ApplyAsync"/>).
    /// </summary>
    private async ValueTask<Either<EncinaError, Unit>> RunAsActorAsync(
        Func<PolicyOperation, ValueTask<Either<EncinaError, Unit>>> body)
    {
        if (!TryResolveActor(out var actor))
        {
            return ABACErrors.PolicyChangePrincipalRequired();
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        return await body(new PolicyOperation(_storeResolver(scope.ServiceProvider), actor));
    }

    private bool TryResolveActor(out PolicyActor actor)
    {
        var context = _requestContextAccessor?.RequestContext;
        var isSystem = PolicyChangeActorScope.IsSystemActorActive;
        var userId = isSystem ? SystemActorId : UserIdOf(context);
        if (string.IsNullOrWhiteSpace(userId))
        {
            actor = default;
            return false;
        }

        actor = new PolicyActor(userId, TenantIdOf(context), CorrelationIdOf(context), isSystem);
        return true;
    }

    private static string? UserIdOf(IRequestContext? context) => context?.UserId;

    private static string? TenantIdOf(IRequestContext? context) => context?.TenantId;

    private static string CorrelationIdOf(IRequestContext? context) =>
        context?.CorrelationId ?? Guid.NewGuid().ToString();

    /// <summary>
    /// Applies a change after its audit record is written. The audit store is resolved in its own
    /// scope, separate from the one holding the policy store: with a shared unit of work (an EF Core
    /// <c>DbContext</c>), a failed policy save would leave its entity tracked and the audit write
    /// of the failure would retry it. A failed audit write stops the change (fail closed).
    /// </summary>
    private async ValueTask<Either<EncinaError, Unit>> ApplyAsync(
        PolicyOperation op,
        PolicyChange change,
        Func<ValueTask<Either<EncinaError, Unit>>> apply,
        CancellationToken cancellationToken)
    {
        var actor = op.Actor;
        await using var auditScope = _scopeFactory.CreateAsyncScope();
        var resolved = ResolveAuditStore(auditScope.ServiceProvider, change, out var auditStore);
        if (resolved.IsLeft)
        {
            return resolved;
        }

        if (auditStore is null)
        {
            WarnUnauditedOnce("no IOperationAuditStore is registered");
            return await apply();
        }

        return await ApplyAuditedAsync(auditStore, actor, change, apply, cancellationToken);
    }

    /// <summary>Logs, once per instance, that changes are being applied without an audit record.</summary>
    private void WarnUnauditedOnce(string condition)
    {
        if (Interlocked.Exchange(ref _unauditedWarningLogged, 1) == 0)
        {
            LogChangesUnaudited(_logger, condition);
        }
    }

    /// <summary>
    /// Resolves the audit store from the per-write scope. A store whose own dependencies cannot be
    /// built fails the change closed, like a failed write, instead of surfacing a raw exception.
    /// </summary>
    private Either<EncinaError, Unit> ResolveAuditStore(IServiceProvider scopedProvider, PolicyChange change, out IOperationAuditStore? auditStore)
    {
        try
        {
            auditStore = scopedProvider.GetService<IOperationAuditStore>();
            return unit;
        }
        catch (Exception ex)
        {
            auditStore = null;
            LogAuditWriteException(_logger, change.Action, change.EntityType, change.EntityId, ex.ForLogging());
            return ABACErrors.PolicyChangeAuditFailed(ex.GetType().Name);
        }
    }

    private async ValueTask<Either<EncinaError, Unit>> ApplyAuditedAsync(
        IOperationAuditStore auditStore,
        PolicyActor actor,
        PolicyChange change,
        Func<ValueTask<Either<EncinaError, Unit>>> apply,
        CancellationToken cancellationToken)
    {
        var beforeState = change.LoadBefore is null ? null : await change.LoadBefore(cancellationToken);
        var entry = BuildEntry(actor, change, beforeState, AuditOutcome.Success, errorCode: null);
        var writeAheadEntryId = entry.Id;
        var recorded = await RecordAuditAsync(auditStore, entry, cancellationToken);
        if (recorded.IsLeft)
        {
            return recorded;
        }

        Either<EncinaError, Unit> result;
        try
        {
            result = await apply();
        }
        catch (Exception ex)
        {
            await RecordFailedChangeAsync(auditStore, actor, change, beforeState, ex.GetType().Name, writeAheadEntryId);
            throw;
        }

        if (result.IsLeft)
        {
            var errorCode = result.Match(Right: _ => string.Empty, Left: e => e.GetCode().IfNone("encina.unknown"));
            await RecordFailedChangeAsync(auditStore, actor, change, beforeState, errorCode, writeAheadEntryId);
        }

        return result;
    }

    /// <summary>
    /// Records that a change announced by a write-ahead entry did not happen. It ignores the
    /// caller's cancellation so the trail is completed, and it never replaces the original outcome.
    /// </summary>
    private async ValueTask RecordFailedChangeAsync(
        IOperationAuditStore auditStore,
        PolicyActor actor,
        PolicyChange change,
        object? beforeState,
        string errorCode,
        Guid writeAheadEntryId)
    {
        var entry = BuildEntry(actor, change, beforeState, AuditOutcome.Error, errorCode, writeAheadEntryId);
        await RecordAuditAsync(auditStore, entry, CancellationToken.None);
    }

    private async ValueTask<Either<EncinaError, Unit>> RecordAuditAsync(
        IOperationAuditStore auditStore,
        OperationAuditEntry entry,
        CancellationToken cancellationToken)
    {
        using var timeout = new CancellationTokenSource(AuditWriteTimeout, _timeProvider);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        try
        {
            var result = await auditStore.RecordAsync(entry, linked.Token).ConfigureAwait(false);
            if (result.IsRight)
            {
                return unit;
            }

            var errorCode = result.Match(Right: _ => string.Empty, Left: e => e.GetCode().IfNone("encina.unknown"));
            LogAuditWriteFailed(_logger, entry.Action, entry.EntityType, entry.EntityId ?? "unknown", errorCode);
            return ABACErrors.PolicyChangeAuditFailed(errorCode);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            LogAuditWriteException(_logger, entry.Action, entry.EntityType, entry.EntityId ?? "unknown", ex.ForLogging());
            return ABACErrors.PolicyChangeAuditFailed(ex.GetType().Name);
        }
    }

    private OperationAuditEntry BuildEntry(
        PolicyActor actor,
        PolicyChange change,
        object? beforeState,
        AuditOutcome outcome,
        string? errorCode,
        Guid? writeAheadEntryId = null)
    {
        var now = _timeProvider.GetUtcNow();

        return new OperationAuditEntry
        {
            Id = Guid.NewGuid(),
            CorrelationId = actor.CorrelationId,
            UserId = actor.UserId,
            TenantId = actor.TenantId,
            Action = change.Action,
            EntityType = change.EntityType,
            EntityId = change.EntityId,
            Outcome = outcome,
            ErrorMessage = errorCode,
            TimestampUtc = now.UtcDateTime,
            StartedAtUtc = now,
            CompletedAtUtc = now,
            Metadata = BuildMetadata(actor, change, beforeState, writeAheadEntryId)
        };
    }

    private static Dictionary<string, object?> BuildMetadata(PolicyActor actor, PolicyChange change, object? beforeState, Guid? writeAheadEntryId)
    {
        var metadata = new Dictionary<string, object?>
        {
            ["source"] = "PersistentPolicyAdministrationPoint",
            ["actor"] = actor.IsSystem ? SystemActorId : "principal"
        };

        if (writeAheadEntryId is { } writeAheadId)
        {
            metadata["writeAheadEntryId"] = writeAheadId;
        }

        AddState(metadata, "beforeState", beforeState);
        AddState(metadata, "afterState", change.After);

        foreach (var (key, value) in change.Extra ?? new Dictionary<string, object?>())
        {
            metadata[key] = value;
        }

        return metadata;
    }

    private static void AddState(Dictionary<string, object?> metadata, string key, object? state)
    {
        if (state is not null)
        {
            metadata[key] = SerializeState(state);
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
        EventId = 9094,
        Level = LogLevel.Error,
        Message = "Audit write failed for policy change {Action} on {EntityType} '{EntityId}': {ErrorCode}")]
    private static partial void LogAuditWriteFailed(
        ILogger logger,
        string action,
        string entityType,
        string entityId,
        string errorCode);

    [LoggerMessage(
        EventId = 9095,
        Level = LogLevel.Error,
        Message = "Exception during the audit write for policy change {Action} on {EntityType} '{EntityId}'")]
    private static partial void LogAuditWriteException(
        ILogger logger,
        string action,
        string entityType,
        string entityId,
        Exception exception);

    [LoggerMessage(
        EventId = 9097,
        Level = LogLevel.Warning,
        Message = "ABAC policy changes are being applied without an audit record: {Condition}")]
    private static partial void LogChangesUnaudited(ILogger logger, string condition);
}
