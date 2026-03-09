using System.Data;
using Encina.Security.ABAC;
using Encina.Security.ABAC.Persistence;
using LanguageExt;
using Microsoft.Data.SqlClient;

namespace Encina.ADO.SqlServer.ABAC;

/// <summary>
/// SQL Server ADO.NET implementation of <see cref="IPolicyStore"/> for persistent ABAC policy storage.
/// Uses raw <see cref="SqlCommand"/> and <see cref="SqlDataReader"/> for maximum performance.
/// </summary>
/// <remarks>
/// <para>
/// Stores XACML 3.0 policy sets and standalone policies in the <c>abac_policy_sets</c> and
/// <c>abac_policies</c> tables respectively. The full policy graph is serialized as JSON via
/// <see cref="IPolicySerializer"/>, while metadata columns (Id, Version, Description, IsEnabled,
/// Priority) are extracted for SQL-level filtering.
/// </para>
/// <para>
/// Save operations use SQL Server <c>MERGE</c> statements for upsert semantics.
/// DateTime values are stored natively as <c>DATETIME2(7)</c>.
/// </para>
/// </remarks>
public sealed class PolicyStoreADO : IPolicyStore
{
    private readonly IDbConnection _connection;
    private readonly IPolicySerializer _serializer;
    private readonly TimeProvider _timeProvider;

    private const string PolicySetsTable = "abac_policy_sets";
    private const string PoliciesTable = "abac_policies";

    /// <summary>
    /// Initializes a new instance of the <see cref="PolicyStoreADO"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="serializer">The serializer for policy graph JSON conversion.</param>
    /// <param name="timeProvider">The time provider for UTC timestamps (default: <see cref="TimeProvider.System"/>).</param>
    public PolicyStoreADO(
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
            var sql = $"SELECT Id, Version, Description, PolicyJson, IsEnabled, Priority, CreatedAtUtc, UpdatedAtUtc FROM {PolicySetsTable}";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;

            await EnsureOpenAsync(cancellationToken);

            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            var policySets = new List<PolicySet>();

            while (await ReadAsync(reader, cancellationToken))
            {
                var entity = ReadPolicySetEntity(reader);
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
            var sql = $"SELECT Id, Version, Description, PolicyJson, IsEnabled, Priority, CreatedAtUtc, UpdatedAtUtc FROM {PolicySetsTable} WHERE Id = @Id";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", policySetId);

            await EnsureOpenAsync(cancellationToken);

            using var reader = await ExecuteReaderAsync(command, cancellationToken);

            if (!await ReadAsync(reader, cancellationToken))
                return Option<PolicySet>.None;

            var entity = ReadPolicySetEntity(reader);
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
                MERGE INTO {PolicySetsTable} AS target
                USING (SELECT @Id AS Id) AS source
                ON target.Id = source.Id
                WHEN MATCHED THEN
                    UPDATE SET
                        Version = @Version,
                        Description = @Description,
                        PolicyJson = @PolicyJson,
                        IsEnabled = @IsEnabled,
                        Priority = @Priority,
                        UpdatedAtUtc = @UpdatedAtUtc
                WHEN NOT MATCHED THEN
                    INSERT (Id, Version, Description, PolicyJson, IsEnabled, Priority, CreatedAtUtc, UpdatedAtUtc)
                    VALUES (@Id, @Version, @Description, @PolicyJson, @IsEnabled, @Priority, @CreatedAtUtc, @UpdatedAtUtc);";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddPolicySetParameters(command, entity);

            await EnsureOpenAsync(cancellationToken);
            await ExecuteNonQueryAsync(command, cancellationToken);
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
            var sql = $"DELETE FROM {PolicySetsTable} WHERE Id = @Id";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", policySetId);

            await EnsureOpenAsync(cancellationToken);
            await ExecuteNonQueryAsync(command, cancellationToken);
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
            var sql = $"SELECT COUNT(1) FROM {PolicySetsTable} WHERE Id = @Id";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", policySetId);

            await EnsureOpenAsync(cancellationToken);
            var result = await ExecuteScalarAsync(command, cancellationToken);
            return Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture) > 0;
        }, "abac_store.exists_set_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, int>> GetPolicySetCountAsync(
        CancellationToken cancellationToken = default)
    {
        return await EitherHelpers.TryAsync(async () =>
        {
            var sql = $"SELECT COUNT(*) FROM {PolicySetsTable}";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;

            await EnsureOpenAsync(cancellationToken);
            var result = await ExecuteScalarAsync(command, cancellationToken);
            return Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture);
        }, "abac_store.count_sets_failed").ConfigureAwait(false);
    }

    // ── Standalone Policy Operations ─────────────────────────────────

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<Policy>>> GetAllStandalonePoliciesAsync(
        CancellationToken cancellationToken = default)
    {
        return await EitherHelpers.TryAsync(async () =>
        {
            var sql = $"SELECT Id, Version, Description, PolicyJson, IsEnabled, Priority, CreatedAtUtc, UpdatedAtUtc FROM {PoliciesTable}";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;

            await EnsureOpenAsync(cancellationToken);

            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            var policies = new List<Policy>();

            while (await ReadAsync(reader, cancellationToken))
            {
                var entity = ReadPolicyEntity(reader);
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
            var sql = $"SELECT Id, Version, Description, PolicyJson, IsEnabled, Priority, CreatedAtUtc, UpdatedAtUtc FROM {PoliciesTable} WHERE Id = @Id";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", policyId);

            await EnsureOpenAsync(cancellationToken);

            using var reader = await ExecuteReaderAsync(command, cancellationToken);

            if (!await ReadAsync(reader, cancellationToken))
                return Option<Policy>.None;

            var entity = ReadPolicyEntity(reader);
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
                MERGE INTO {PoliciesTable} AS target
                USING (SELECT @Id AS Id) AS source
                ON target.Id = source.Id
                WHEN MATCHED THEN
                    UPDATE SET
                        Version = @Version,
                        Description = @Description,
                        PolicyJson = @PolicyJson,
                        IsEnabled = @IsEnabled,
                        Priority = @Priority,
                        UpdatedAtUtc = @UpdatedAtUtc
                WHEN NOT MATCHED THEN
                    INSERT (Id, Version, Description, PolicyJson, IsEnabled, Priority, CreatedAtUtc, UpdatedAtUtc)
                    VALUES (@Id, @Version, @Description, @PolicyJson, @IsEnabled, @Priority, @CreatedAtUtc, @UpdatedAtUtc);";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddPolicyParameters(command, entity);

            await EnsureOpenAsync(cancellationToken);
            await ExecuteNonQueryAsync(command, cancellationToken);
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
            var sql = $"DELETE FROM {PoliciesTable} WHERE Id = @Id";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", policyId);

            await EnsureOpenAsync(cancellationToken);
            await ExecuteNonQueryAsync(command, cancellationToken);
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
            var sql = $"SELECT COUNT(1) FROM {PoliciesTable} WHERE Id = @Id";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", policyId);

            await EnsureOpenAsync(cancellationToken);
            var result = await ExecuteScalarAsync(command, cancellationToken);
            return Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture) > 0;
        }, "abac_store.exists_policy_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, int>> GetPolicyCountAsync(
        CancellationToken cancellationToken = default)
    {
        return await EitherHelpers.TryAsync(async () =>
        {
            var sql = $"SELECT COUNT(*) FROM {PoliciesTable}";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;

            await EnsureOpenAsync(cancellationToken);
            var result = await ExecuteScalarAsync(command, cancellationToken);
            return Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture);
        }, "abac_store.count_policies_failed").ConfigureAwait(false);
    }

    // ── Entity Reading Helpers ───────────────────────────────────────

    private static PolicySetEntity ReadPolicySetEntity(IDataReader reader) => new()
    {
        Id = reader.GetString(reader.GetOrdinal("Id")),
        Version = reader.IsDBNull(reader.GetOrdinal("Version"))
            ? null : reader.GetString(reader.GetOrdinal("Version")),
        Description = reader.IsDBNull(reader.GetOrdinal("Description"))
            ? null : reader.GetString(reader.GetOrdinal("Description")),
        PolicyJson = reader.GetString(reader.GetOrdinal("PolicyJson")),
        IsEnabled = reader.GetBoolean(reader.GetOrdinal("IsEnabled")),
        Priority = reader.GetInt32(reader.GetOrdinal("Priority")),
        CreatedAtUtc = reader.GetDateTime(reader.GetOrdinal("CreatedAtUtc")),
        UpdatedAtUtc = reader.GetDateTime(reader.GetOrdinal("UpdatedAtUtc")),
    };

    private static PolicyEntity ReadPolicyEntity(IDataReader reader) => new()
    {
        Id = reader.GetString(reader.GetOrdinal("Id")),
        Version = reader.IsDBNull(reader.GetOrdinal("Version"))
            ? null : reader.GetString(reader.GetOrdinal("Version")),
        Description = reader.IsDBNull(reader.GetOrdinal("Description"))
            ? null : reader.GetString(reader.GetOrdinal("Description")),
        PolicyJson = reader.GetString(reader.GetOrdinal("PolicyJson")),
        IsEnabled = reader.GetBoolean(reader.GetOrdinal("IsEnabled")),
        Priority = reader.GetInt32(reader.GetOrdinal("Priority")),
        CreatedAtUtc = reader.GetDateTime(reader.GetOrdinal("CreatedAtUtc")),
        UpdatedAtUtc = reader.GetDateTime(reader.GetOrdinal("UpdatedAtUtc")),
    };

    // ── Parameter Helpers ────────────────────────────────────────────

    private static void AddPolicySetParameters(IDbCommand command, PolicySetEntity entity)
    {
        AddParameter(command, "@Id", entity.Id);
        AddParameter(command, "@Version", entity.Version);
        AddParameter(command, "@Description", entity.Description);
        AddParameter(command, "@PolicyJson", entity.PolicyJson);
        AddParameter(command, "@IsEnabled", entity.IsEnabled);
        AddParameter(command, "@Priority", entity.Priority);
        AddParameter(command, "@CreatedAtUtc", entity.CreatedAtUtc);
        AddParameter(command, "@UpdatedAtUtc", entity.UpdatedAtUtc);
    }

    private static void AddPolicyParameters(IDbCommand command, PolicyEntity entity)
    {
        AddParameter(command, "@Id", entity.Id);
        AddParameter(command, "@Version", entity.Version);
        AddParameter(command, "@Description", entity.Description);
        AddParameter(command, "@PolicyJson", entity.PolicyJson);
        AddParameter(command, "@IsEnabled", entity.IsEnabled);
        AddParameter(command, "@Priority", entity.Priority);
        AddParameter(command, "@CreatedAtUtc", entity.CreatedAtUtc);
        AddParameter(command, "@UpdatedAtUtc", entity.UpdatedAtUtc);
    }

    private static void AddParameter(IDbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    // ── Async ADO.NET Helpers ────────────────────────────────────────

    private async Task EnsureOpenAsync(CancellationToken cancellationToken)
    {
        if (_connection.State != ConnectionState.Open)
        {
            if (_connection is SqlConnection sqlConnection)
                await sqlConnection.OpenAsync(cancellationToken);
            else
                _connection.Open();
        }
    }

    private static async Task<IDataReader> ExecuteReaderAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is SqlCommand sqlCommand)
            return await sqlCommand.ExecuteReaderAsync(cancellationToken);

        return await Task.Run(command.ExecuteReader, cancellationToken);
    }

    private static async Task<int> ExecuteNonQueryAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is SqlCommand sqlCommand)
            return await sqlCommand.ExecuteNonQueryAsync(cancellationToken);

        return await Task.Run(command.ExecuteNonQuery, cancellationToken);
    }

    private static async Task<object?> ExecuteScalarAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is SqlCommand sqlCommand)
            return await sqlCommand.ExecuteScalarAsync(cancellationToken);

        return await Task.Run(command.ExecuteScalar, cancellationToken);
    }

    private static async Task<bool> ReadAsync(IDataReader reader, CancellationToken cancellationToken)
    {
        if (reader is SqlDataReader sqlReader)
            return await sqlReader.ReadAsync(cancellationToken);

        return await Task.Run(reader.Read, cancellationToken);
    }
}
