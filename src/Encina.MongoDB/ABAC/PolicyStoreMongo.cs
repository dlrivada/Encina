using Encina.Security.ABAC;
using Encina.Security.ABAC.Persistence;
using LanguageExt;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Encina.MongoDB.ABAC;

/// <summary>
/// MongoDB implementation of <see cref="IPolicyStore"/> for persistent ABAC policy storage
/// using native BSON document storage.
/// </summary>
/// <remarks>
/// <para>
/// Unlike the relational providers (EF Core, Dapper, ADO.NET), this implementation stores
/// the full <see cref="PolicySet"/> and <see cref="Policy"/> domain models as native BSON
/// subdocuments — no JSON serialization via <see cref="IPolicySerializer"/> is needed.
/// </para>
/// <para>
/// Document wrappers (<see cref="PolicySetDocument"/>, <see cref="PolicyDocument"/>) hold
/// extracted metadata fields (Id, IsEnabled, Priority, timestamps) at the document root
/// for efficient MongoDB queries, while the complete policy graph is embedded as a BSON
/// subdocument.
/// </para>
/// <para>
/// Save operations use <see cref="IMongoCollection{TDocument}.ReplaceOneAsync(FilterDefinition{TDocument}, TDocument, ReplaceOptions, CancellationToken)"/>
/// with <c>IsUpsert = true</c> for atomic upsert semantics.
/// </para>
/// <para>
/// BSON class maps and discriminators for polymorphic <see cref="IExpression"/> types must be
/// registered before use via <see cref="ABACBsonClassMapRegistration.EnsureRegistered"/>.
/// </para>
/// </remarks>
public sealed class PolicyStoreMongo : IPolicyStore
{
    private readonly IMongoCollection<PolicySetDocument> _policySetsCollection;
    private readonly IMongoCollection<PolicyDocument> _policiesCollection;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="PolicyStoreMongo"/> class.
    /// </summary>
    /// <param name="mongoClient">The MongoDB client.</param>
    /// <param name="options">The MongoDB options.</param>
    /// <param name="timeProvider">The time provider. Defaults to <see cref="TimeProvider.System"/> if not specified.</param>
    public PolicyStoreMongo(
        IMongoClient mongoClient,
        IOptions<EncinaMongoDbOptions> options,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(mongoClient);
        ArgumentNullException.ThrowIfNull(options);

        var config = options.Value;
        var database = mongoClient.GetDatabase(config.DatabaseName);
        _policySetsCollection = database.GetCollection<PolicySetDocument>(config.Collections.ABACPolicySets);
        _policiesCollection = database.GetCollection<PolicyDocument>(config.Collections.ABACPolicies);
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    // ── PolicySet Operations ─────────────────────────────────────────

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<PolicySet>>> GetAllPolicySetsAsync(
        CancellationToken cancellationToken = default)
    {
        return await EitherHelpers.TryAsync(async () =>
        {
            var documents = await _policySetsCollection
                .Find(Builders<PolicySetDocument>.Filter.Empty)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return (IReadOnlyList<PolicySet>)documents
                .Select(d => d.PolicySet)
                .ToList();
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
            var filter = Builders<PolicySetDocument>.Filter.Eq(d => d.Id, policySetId);

            var document = await _policySetsCollection
                .Find(filter)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            return document is null
                ? Option<PolicySet>.None
                : Option<PolicySet>.Some(document.PolicySet);
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
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var filter = Builders<PolicySetDocument>.Filter.Eq(d => d.Id, policySet.Id);

            // Preserve CreatedAtUtc on update
            var existing = await _policySetsCollection
                .Find(filter)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            var document = new PolicySetDocument
            {
                Id = policySet.Id,
                IsEnabled = policySet.IsEnabled,
                Priority = policySet.Priority,
                CreatedAtUtc = existing?.CreatedAtUtc ?? now,
                UpdatedAtUtc = now,
                PolicySet = policySet
            };

            await _policySetsCollection
                .ReplaceOneAsync(filter, document, new ReplaceOptions { IsUpsert = true }, cancellationToken)
                .ConfigureAwait(false);
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
            var filter = Builders<PolicySetDocument>.Filter.Eq(d => d.Id, policySetId);

            await _policySetsCollection
                .DeleteOneAsync(filter, cancellationToken)
                .ConfigureAwait(false);
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
            var filter = Builders<PolicySetDocument>.Filter.Eq(d => d.Id, policySetId);

            var count = await _policySetsCollection
                .CountDocumentsAsync(filter, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return count > 0;
        }, "abac_store.exists_set_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, int>> GetPolicySetCountAsync(
        CancellationToken cancellationToken = default)
    {
        return await EitherHelpers.TryAsync(async () =>
        {
            var count = await _policySetsCollection
                .CountDocumentsAsync(Builders<PolicySetDocument>.Filter.Empty, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return (int)count;
        }, "abac_store.count_sets_failed").ConfigureAwait(false);
    }

    // ── Standalone Policy Operations ─────────────────────────────────

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<Policy>>> GetAllStandalonePoliciesAsync(
        CancellationToken cancellationToken = default)
    {
        return await EitherHelpers.TryAsync(async () =>
        {
            var documents = await _policiesCollection
                .Find(Builders<PolicyDocument>.Filter.Empty)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return (IReadOnlyList<Policy>)documents
                .Select(d => d.Policy)
                .ToList();
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
            var filter = Builders<PolicyDocument>.Filter.Eq(d => d.Id, policyId);

            var document = await _policiesCollection
                .Find(filter)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            return document is null
                ? Option<Policy>.None
                : Option<Policy>.Some(document.Policy);
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
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var filter = Builders<PolicyDocument>.Filter.Eq(d => d.Id, policy.Id);

            // Preserve CreatedAtUtc on update
            var existing = await _policiesCollection
                .Find(filter)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            var document = new PolicyDocument
            {
                Id = policy.Id,
                IsEnabled = policy.IsEnabled,
                Priority = policy.Priority,
                CreatedAtUtc = existing?.CreatedAtUtc ?? now,
                UpdatedAtUtc = now,
                Policy = policy
            };

            await _policiesCollection
                .ReplaceOneAsync(filter, document, new ReplaceOptions { IsUpsert = true }, cancellationToken)
                .ConfigureAwait(false);
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
            var filter = Builders<PolicyDocument>.Filter.Eq(d => d.Id, policyId);

            await _policiesCollection
                .DeleteOneAsync(filter, cancellationToken)
                .ConfigureAwait(false);
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
            var filter = Builders<PolicyDocument>.Filter.Eq(d => d.Id, policyId);

            var count = await _policiesCollection
                .CountDocumentsAsync(filter, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return count > 0;
        }, "abac_store.exists_policy_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, int>> GetPolicyCountAsync(
        CancellationToken cancellationToken = default)
    {
        return await EitherHelpers.TryAsync(async () =>
        {
            var count = await _policiesCollection
                .CountDocumentsAsync(Builders<PolicyDocument>.Filter.Empty, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return (int)count;
        }, "abac_store.count_policies_failed").ConfigureAwait(false);
    }
}
