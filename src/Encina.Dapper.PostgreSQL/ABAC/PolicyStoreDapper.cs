using System.Data;
using Dapper;
using Encina.Security.ABAC;
using Encina.Security.ABAC.Persistence;
using LanguageExt;

namespace Encina.Dapper.PostgreSQL.ABAC;

/// <summary>
/// PostgreSQL Dapper implementation of <see cref="IPolicyStore"/> for persistent ABAC policy storage.
/// Uses Dapper extension methods for concise SQL execution with automatic parameter and result mapping.
/// </summary>
/// <remarks>
/// <para>
/// Stores XACML 3.0 policy sets and standalone policies in the <c>abac_policy_sets</c> and
/// <c>abac_policies</c> tables respectively. The full policy graph is serialized as JSON via
/// <see cref="IPolicySerializer"/> and stored in a <c>JSONB</c> column, while metadata columns
/// (Id, Version, Description, IsEnabled, Priority) are extracted for SQL-level filtering.
/// </para>
/// <para>
/// Save operations use PostgreSQL <c>INSERT ... ON CONFLICT DO UPDATE</c> for upsert semantics.
/// Column names use quoted PascalCase identifiers to match the table definition.
/// </para>
/// </remarks>
public sealed class PolicyStoreDapper : IPolicyStore
{
    private readonly IDbConnection _connection;
    private readonly IPolicySerializer _serializer;
    private readonly TimeProvider _timeProvider;

    private const string PolicySetsTable = "\"abac_policy_sets\"";
    private const string PoliciesTable = "\"abac_policies\"";

    /// <summary>
    /// Initializes a new instance of the <see cref="PolicyStoreDapper"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="serializer">The serializer for policy graph JSON conversion.</param>
    /// <param name="timeProvider">The time provider for UTC timestamps (default: <see cref="TimeProvider.System"/>).</param>
    public PolicyStoreDapper(
        IDbConnection connection,
        IPolicySerializer serializer,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(serializer);
        _connection = connection;
        _serializer = serializer;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    // ── PolicySet Operations ─────────────────────────────────────────

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<PolicySet>>> GetAllPolicySetsAsync(
        CancellationToken cancellationToken = default)
    {
        return await EitherHelpers.TryAsync(async () =>
        {
            var sql = $"SELECT \"Id\", \"Version\", \"Description\", \"PolicyJson\", \"IsEnabled\", \"Priority\", \"CreatedAtUtc\", \"UpdatedAtUtc\" FROM {PolicySetsTable}";

            var entities = await _connection.QueryAsync<PolicySetEntity>(sql);
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
            var sql = $"SELECT \"Id\", \"Version\", \"Description\", \"PolicyJson\", \"IsEnabled\", \"Priority\", \"CreatedAtUtc\", \"UpdatedAtUtc\" FROM {PolicySetsTable} WHERE \"Id\" = @Id";

            var entity = await _connection.QuerySingleOrDefaultAsync<PolicySetEntity>(sql, new { Id = policySetId });

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
            var entity = PolicyEntityMapper.ToPolicySetEntity(policySet, _serializer, _timeProvider);

            var sql = $@"
                INSERT INTO {PolicySetsTable}
                    (""Id"", ""Version"", ""Description"", ""PolicyJson"", ""IsEnabled"", ""Priority"", ""CreatedAtUtc"", ""UpdatedAtUtc"")
                VALUES
                    (@Id, @Version, @Description, CAST(@PolicyJson AS jsonb), @IsEnabled, @Priority, @CreatedAtUtc, @UpdatedAtUtc)
                ON CONFLICT (""Id"") DO UPDATE SET
                    ""Version"" = EXCLUDED.""Version"",
                    ""Description"" = EXCLUDED.""Description"",
                    ""PolicyJson"" = EXCLUDED.""PolicyJson"",
                    ""IsEnabled"" = EXCLUDED.""IsEnabled"",
                    ""Priority"" = EXCLUDED.""Priority"",
                    ""UpdatedAtUtc"" = EXCLUDED.""UpdatedAtUtc""";

            await _connection.ExecuteAsync(sql, new
            {
                entity.Id,
                entity.Version,
                entity.Description,
                entity.PolicyJson,
                entity.IsEnabled,
                entity.Priority,
                entity.CreatedAtUtc,
                entity.UpdatedAtUtc
            });
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
            var sql = $"DELETE FROM {PolicySetsTable} WHERE \"Id\" = @Id";
            await _connection.ExecuteAsync(sql, new { Id = policySetId });
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
            var sql = $"SELECT COUNT(1) FROM {PolicySetsTable} WHERE \"Id\" = @Id";
            var count = await _connection.ExecuteScalarAsync<int>(sql, new { Id = policySetId });
            return count > 0;
        }, "abac_store.exists_set_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, int>> GetPolicySetCountAsync(
        CancellationToken cancellationToken = default)
    {
        return await EitherHelpers.TryAsync(async () =>
        {
            var sql = $"SELECT COUNT(*) FROM {PolicySetsTable}";
            return await _connection.ExecuteScalarAsync<int>(sql);
        }, "abac_store.count_sets_failed").ConfigureAwait(false);
    }

    // ── Standalone Policy Operations ─────────────────────────────────

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<Policy>>> GetAllStandalonePoliciesAsync(
        CancellationToken cancellationToken = default)
    {
        return await EitherHelpers.TryAsync(async () =>
        {
            var sql = $"SELECT \"Id\", \"Version\", \"Description\", \"PolicyJson\", \"IsEnabled\", \"Priority\", \"CreatedAtUtc\", \"UpdatedAtUtc\" FROM {PoliciesTable}";

            var entities = await _connection.QueryAsync<PolicyEntity>(sql);
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
            var sql = $"SELECT \"Id\", \"Version\", \"Description\", \"PolicyJson\", \"IsEnabled\", \"Priority\", \"CreatedAtUtc\", \"UpdatedAtUtc\" FROM {PoliciesTable} WHERE \"Id\" = @Id";

            var entity = await _connection.QuerySingleOrDefaultAsync<PolicyEntity>(sql, new { Id = policyId });

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
            var entity = PolicyEntityMapper.ToPolicyEntity(policy, _serializer, _timeProvider);

            var sql = $@"
                INSERT INTO {PoliciesTable}
                    (""Id"", ""Version"", ""Description"", ""PolicyJson"", ""IsEnabled"", ""Priority"", ""CreatedAtUtc"", ""UpdatedAtUtc"")
                VALUES
                    (@Id, @Version, @Description, CAST(@PolicyJson AS jsonb), @IsEnabled, @Priority, @CreatedAtUtc, @UpdatedAtUtc)
                ON CONFLICT (""Id"") DO UPDATE SET
                    ""Version"" = EXCLUDED.""Version"",
                    ""Description"" = EXCLUDED.""Description"",
                    ""PolicyJson"" = EXCLUDED.""PolicyJson"",
                    ""IsEnabled"" = EXCLUDED.""IsEnabled"",
                    ""Priority"" = EXCLUDED.""Priority"",
                    ""UpdatedAtUtc"" = EXCLUDED.""UpdatedAtUtc""";

            await _connection.ExecuteAsync(sql, new
            {
                entity.Id,
                entity.Version,
                entity.Description,
                entity.PolicyJson,
                entity.IsEnabled,
                entity.Priority,
                entity.CreatedAtUtc,
                entity.UpdatedAtUtc
            });
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
            var sql = $"DELETE FROM {PoliciesTable} WHERE \"Id\" = @Id";
            await _connection.ExecuteAsync(sql, new { Id = policyId });
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
            var sql = $"SELECT COUNT(1) FROM {PoliciesTable} WHERE \"Id\" = @Id";
            var count = await _connection.ExecuteScalarAsync<int>(sql, new { Id = policyId });
            return count > 0;
        }, "abac_store.exists_policy_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, int>> GetPolicyCountAsync(
        CancellationToken cancellationToken = default)
    {
        return await EitherHelpers.TryAsync(async () =>
        {
            var sql = $"SELECT COUNT(*) FROM {PoliciesTable}";
            return await _connection.ExecuteScalarAsync<int>(sql);
        }, "abac_store.count_policies_failed").ConfigureAwait(false);
    }
}
