using Encina.Security.ABAC;
using Encina.Security.ABAC.Persistence;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Encina.EntityFrameworkCore.ABAC;

/// <summary>
/// Entity Framework Core implementation of <see cref="IPolicyStore"/> for persistent ABAC policy storage.
/// </summary>
/// <remarks>
/// <para>
/// This is a single, provider-agnostic implementation that works with all EF Core database providers
/// (SQLite, SQL Server, PostgreSQL, MySQL). EF Core's database provider abstraction handles
/// SQL dialect differences transparently.
/// </para>
/// <para>
/// Stores XACML 3.0 policy sets and standalone policies in the <c>abac_policy_sets</c> and
/// <c>abac_policies</c> tables respectively. The full policy graph is serialized as JSON via
/// <see cref="IPolicySerializer"/>, while metadata columns (Id, Version, Description, IsEnabled,
/// Priority) are extracted for SQL-level filtering.
/// </para>
/// <para>
/// Save operations implement upsert semantics by checking for existing entities via
/// <see cref="DbContext.FindAsync{TEntity}(object?[])"/>. When an existing entity is found,
/// the <see cref="PolicySetEntity.CreatedAtUtc"/> is preserved; otherwise a new timestamp
/// is generated.
/// </para>
/// <para>
/// Changes are committed immediately via <see cref="DbContext.SaveChangesAsync(System.Threading.CancellationToken)"/>
/// within each method. If the caller has already started a transaction (e.g., via
/// <c>TransactionPipelineBehavior</c>), EF Core automatically enlists in that transaction.
/// </para>
/// </remarks>
public sealed class PolicyStoreEF : IPolicyStore
{
    private readonly DbContext _dbContext;
    private readonly IPolicySerializer _serializer;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="PolicyStoreEF"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="serializer">The serializer for policy graph JSON conversion.</param>
    /// <param name="timeProvider">The time provider for UTC timestamps (default: <see cref="TimeProvider.System"/>).</param>
    public PolicyStoreEF(
        DbContext dbContext,
        IPolicySerializer serializer,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(serializer);
        _dbContext = dbContext;
        _serializer = serializer;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    // -- PolicySet Operations -------------------------------------------------

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<PolicySet>>> GetAllPolicySetsAsync(
        CancellationToken cancellationToken = default)
    {
        return await EitherHelpers.TryAsync(async () =>
        {
            var entities = await _dbContext.Set<PolicySetEntity>()
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var policySets = new List<PolicySet>();

            foreach (var entity in entities)
            {
                var mapped = PolicyEntityMapper.ToPolicySet(entity, _serializer);
                policySets.Add(mapped.Match(
                    Right: ps => ps,
                    Left: e => throw new InvalidOperationException(
                        $"Failed to deserialize policy set '{entity.Id}': {e.Message}")));
            }

            return (IReadOnlyList<PolicySet>)policySets;
        }, "abac_store.get_all_sets_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Option<PolicySet>>> GetPolicySetAsync(
        string policySetId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policySetId);

        return await EitherHelpers.TryAsync(async () =>
        {
            var entity = await _dbContext.Set<PolicySetEntity>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == policySetId, cancellationToken);

            if (entity is null)
                return Option<PolicySet>.None;

            var mapped = PolicyEntityMapper.ToPolicySet(entity, _serializer);

            return mapped.Match<Option<PolicySet>>(
                Right: ps => ps,
                Left: e => throw new InvalidOperationException(
                    $"Failed to deserialize policy set '{entity.Id}': {e.Message}"));
        }, "abac_store.get_set_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> SavePolicySetAsync(
        PolicySet policySet,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policySet);

        return await EitherHelpers.TryAsync(async () =>
        {
            var existingEntity = await _dbContext.Set<PolicySetEntity>()
                .FindAsync([policySet.Id], cancellationToken);

            var entity = PolicyEntityMapper.ToPolicySetEntity(policySet, _serializer, _timeProvider, existingEntity);

            if (existingEntity is not null)
            {
                // Update: copy values to the tracked entity
                _dbContext.Entry(existingEntity).CurrentValues.SetValues(entity);
            }
            else
            {
                // Insert: add new entity
                await _dbContext.Set<PolicySetEntity>().AddAsync(entity, cancellationToken);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }, "abac_store.save_set_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> DeletePolicySetAsync(
        string policySetId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policySetId);

        return await EitherHelpers.TryAsync(async () =>
        {
            var entity = await _dbContext.Set<PolicySetEntity>()
                .FindAsync([policySetId], cancellationToken);

            if (entity is not null)
            {
                _dbContext.Set<PolicySetEntity>().Remove(entity);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }, "abac_store.delete_set_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, bool>> ExistsPolicySetAsync(
        string policySetId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policySetId);

        return await EitherHelpers.TryAsync(async () =>
        {
            return await _dbContext.Set<PolicySetEntity>()
                .AsNoTracking()
                .AnyAsync(x => x.Id == policySetId, cancellationToken);
        }, "abac_store.exists_set_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, int>> GetPolicySetCountAsync(
        CancellationToken cancellationToken = default)
    {
        return await EitherHelpers.TryAsync(async () =>
        {
            return await _dbContext.Set<PolicySetEntity>()
                .AsNoTracking()
                .CountAsync(cancellationToken);
        }, "abac_store.count_sets_failed").ConfigureAwait(false);
    }

    // -- Standalone Policy Operations -----------------------------------------

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<Policy>>> GetAllStandalonePoliciesAsync(
        CancellationToken cancellationToken = default)
    {
        return await EitherHelpers.TryAsync(async () =>
        {
            var entities = await _dbContext.Set<PolicyEntity>()
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var policies = new List<Policy>();

            foreach (var entity in entities)
            {
                var mapped = PolicyEntityMapper.ToPolicy(entity, _serializer);
                policies.Add(mapped.Match(
                    Right: p => p,
                    Left: e => throw new InvalidOperationException(
                        $"Failed to deserialize policy '{entity.Id}': {e.Message}")));
            }

            return (IReadOnlyList<Policy>)policies;
        }, "abac_store.get_all_policies_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Option<Policy>>> GetPolicyAsync(
        string policyId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyId);

        return await EitherHelpers.TryAsync(async () =>
        {
            var entity = await _dbContext.Set<PolicyEntity>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == policyId, cancellationToken);

            if (entity is null)
                return Option<Policy>.None;

            var mapped = PolicyEntityMapper.ToPolicy(entity, _serializer);

            return mapped.Match<Option<Policy>>(
                Right: p => p,
                Left: e => throw new InvalidOperationException(
                    $"Failed to deserialize policy '{entity.Id}': {e.Message}"));
        }, "abac_store.get_policy_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> SavePolicyAsync(
        Policy policy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policy);

        return await EitherHelpers.TryAsync(async () =>
        {
            var existingEntity = await _dbContext.Set<PolicyEntity>()
                .FindAsync([policy.Id], cancellationToken);

            var entity = PolicyEntityMapper.ToPolicyEntity(policy, _serializer, _timeProvider, existingEntity);

            if (existingEntity is not null)
            {
                // Update: copy values to the tracked entity
                _dbContext.Entry(existingEntity).CurrentValues.SetValues(entity);
            }
            else
            {
                // Insert: add new entity
                await _dbContext.Set<PolicyEntity>().AddAsync(entity, cancellationToken);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }, "abac_store.save_policy_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> DeletePolicyAsync(
        string policyId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyId);

        return await EitherHelpers.TryAsync(async () =>
        {
            var entity = await _dbContext.Set<PolicyEntity>()
                .FindAsync([policyId], cancellationToken);

            if (entity is not null)
            {
                _dbContext.Set<PolicyEntity>().Remove(entity);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }, "abac_store.delete_policy_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, bool>> ExistsPolicyAsync(
        string policyId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyId);

        return await EitherHelpers.TryAsync(async () =>
        {
            return await _dbContext.Set<PolicyEntity>()
                .AsNoTracking()
                .AnyAsync(x => x.Id == policyId, cancellationToken);
        }, "abac_store.exists_policy_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, int>> GetPolicyCountAsync(
        CancellationToken cancellationToken = default)
    {
        return await EitherHelpers.TryAsync(async () =>
        {
            return await _dbContext.Set<PolicyEntity>()
                .AsNoTracking()
                .CountAsync(cancellationToken);
        }, "abac_store.count_policies_failed").ConfigureAwait(false);
    }
}
