using System.Data;

using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;
using Encina.Messaging;

using LanguageExt;

using Microsoft.Data.SqlClient;

using static LanguageExt.Prelude;

namespace Encina.ADO.SqlServer.DataResidency;

/// <summary>
/// ADO.NET implementation of <see cref="IResidencyPolicyStore"/> for SQL Server.
/// Provides CRUD operations for residency policy descriptors per GDPR Article 44.
/// </summary>
/// <remarks>
/// <para>
/// Per GDPR Article 44 (general principle for transfers), any transfer of personal data
/// to a third country shall take place only if the conditions of Chapter V are complied with.
/// Residency policy descriptors encode these conditions as enforceable rules.
/// </para>
/// <para>
/// Per GDPR Articles 45-49, transfers may be based on adequacy decisions (Art. 45),
/// appropriate safeguards such as standard contractual clauses (Art. 46), binding corporate
/// rules (Art. 47), or specific derogations (Art. 49). This store persists the configuration
/// that determines which of these mechanisms are allowed for each data category.
/// </para>
/// </remarks>
public sealed class ResidencyPolicyStoreADO : IResidencyPolicyStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResidencyPolicyStoreADO"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The residency policies table name (default: ResidencyPolicies).</param>
    /// <param name="timeProvider">The time provider for UTC time (default: <see cref="TimeProvider.System"/>).</param>
    public ResidencyPolicyStoreADO(
        IDbConnection connection,
        string tableName = "ResidencyPolicies",
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _tableName = SqlIdentifierValidator.ValidateTableName(tableName);
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> CreateAsync(
        ResidencyPolicyDescriptor policy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policy);

        try
        {
            var entity = ResidencyPolicyMapper.ToEntity(policy);
            var sql = $@"
                INSERT INTO {_tableName}
                ([DataCategory], [AllowedRegionCodes], [RequireAdequacyDecision], [AllowedTransferBasesValue], [CreatedAtUtc], [LastModifiedAtUtc])
                VALUES
                (@DataCategory, @AllowedRegionCodes, @RequireAdequacyDecision, @AllowedTransferBasesValue, @CreatedAtUtc, @LastModifiedAtUtc)";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@DataCategory", entity.DataCategory);
            AddParameter(command, "@AllowedRegionCodes", entity.AllowedRegionCodes);
            AddParameter(command, "@RequireAdequacyDecision", entity.RequireAdequacyDecision);
            AddParameter(command, "@AllowedTransferBasesValue", entity.AllowedTransferBasesValue);
            AddParameter(command, "@CreatedAtUtc", entity.CreatedAtUtc.UtcDateTime);
            AddParameter(command, "@LastModifiedAtUtc", entity.LastModifiedAtUtc?.UtcDateTime);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            await ExecuteNonQueryAsync(command, cancellationToken);
            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "residency.store_error",
                message: $"Failed to create residency policy: {ex.Message}",
                details: new Dictionary<string, object?> { ["dataCategory"] = policy.DataCategory }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Option<ResidencyPolicyDescriptor>>> GetByCategoryAsync(
        string dataCategory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataCategory);

        try
        {
            var sql = $@"
                SELECT [DataCategory], [AllowedRegionCodes], [RequireAdequacyDecision], [AllowedTransferBasesValue], [CreatedAtUtc], [LastModifiedAtUtc]
                FROM {_tableName}
                WHERE [DataCategory] = @DataCategory";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@DataCategory", dataCategory);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            if (await ReadAsync(reader, cancellationToken))
            {
                var entity = MapToEntity(reader);
                var domain = ResidencyPolicyMapper.ToDomain(entity);
                return domain is not null
                    ? Right<EncinaError, Option<ResidencyPolicyDescriptor>>(Some(domain))
                    : Right<EncinaError, Option<ResidencyPolicyDescriptor>>(None);
            }

            return Right<EncinaError, Option<ResidencyPolicyDescriptor>>(None);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "residency.store_error",
                message: $"Failed to get residency policy by category: {ex.Message}",
                details: new Dictionary<string, object?> { ["dataCategory"] = dataCategory }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<ResidencyPolicyDescriptor>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sql = $@"
                SELECT [DataCategory], [AllowedRegionCodes], [RequireAdequacyDecision], [AllowedTransferBasesValue], [CreatedAtUtc], [LastModifiedAtUtc]
                FROM {_tableName}";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var policies = new List<ResidencyPolicyDescriptor>();
            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                var entity = MapToEntity(reader);
                var domain = ResidencyPolicyMapper.ToDomain(entity);
                if (domain is not null)
                    policies.Add(domain);
            }

            return Right<EncinaError, IReadOnlyList<ResidencyPolicyDescriptor>>(policies);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "residency.store_error",
                message: $"Failed to get all residency policies: {ex.Message}",
                details: new Dictionary<string, object?>()));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> UpdateAsync(
        ResidencyPolicyDescriptor policy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policy);

        try
        {
            var entity = ResidencyPolicyMapper.ToEntity(policy);
            var nowUtc = _timeProvider.GetUtcNow();
            var sql = $@"
                UPDATE {_tableName}
                SET [AllowedRegionCodes] = @AllowedRegionCodes,
                    [RequireAdequacyDecision] = @RequireAdequacyDecision,
                    [AllowedTransferBasesValue] = @AllowedTransferBasesValue,
                    [LastModifiedAtUtc] = @NowUtc
                WHERE [DataCategory] = @DataCategory";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@DataCategory", entity.DataCategory);
            AddParameter(command, "@AllowedRegionCodes", entity.AllowedRegionCodes);
            AddParameter(command, "@RequireAdequacyDecision", entity.RequireAdequacyDecision);
            AddParameter(command, "@AllowedTransferBasesValue", entity.AllowedTransferBasesValue);
            AddParameter(command, "@NowUtc", nowUtc.UtcDateTime);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            await ExecuteNonQueryAsync(command, cancellationToken);
            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "residency.store_error",
                message: $"Failed to update residency policy: {ex.Message}",
                details: new Dictionary<string, object?> { ["dataCategory"] = policy.DataCategory }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> DeleteAsync(
        string dataCategory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataCategory);

        try
        {
            var sql = $"DELETE FROM {_tableName} WHERE [DataCategory] = @DataCategory";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@DataCategory", dataCategory);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            await ExecuteNonQueryAsync(command, cancellationToken);
            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "residency.store_error",
                message: $"Failed to delete residency policy: {ex.Message}",
                details: new Dictionary<string, object?> { ["dataCategory"] = dataCategory }));
        }
    }

    private static ResidencyPolicyEntity MapToEntity(IDataReader reader)
    {
        var allowedTransferBasesOrd = reader.GetOrdinal("AllowedTransferBasesValue");
        var lastModifiedOrd = reader.GetOrdinal("LastModifiedAtUtc");

        return new ResidencyPolicyEntity
        {
            DataCategory = reader.GetString(reader.GetOrdinal("DataCategory")),
            AllowedRegionCodes = reader.GetString(reader.GetOrdinal("AllowedRegionCodes")),
            RequireAdequacyDecision = reader.GetBoolean(reader.GetOrdinal("RequireAdequacyDecision")),
            AllowedTransferBasesValue = reader.IsDBNull(allowedTransferBasesOrd)
                ? null
                : reader.GetString(allowedTransferBasesOrd),
            CreatedAtUtc = new DateTimeOffset(reader.GetDateTime(reader.GetOrdinal("CreatedAtUtc")), TimeSpan.Zero),
            LastModifiedAtUtc = reader.IsDBNull(lastModifiedOrd)
                ? null
                : new DateTimeOffset(reader.GetDateTime(lastModifiedOrd), TimeSpan.Zero)
        };
    }

    private static void AddParameter(IDbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private static Task OpenConnectionAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
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

    private static async Task<bool> ReadAsync(IDataReader reader, CancellationToken cancellationToken)
    {
        if (reader is SqlDataReader sqlReader)
            return await sqlReader.ReadAsync(cancellationToken);

        return await Task.Run(reader.Read, cancellationToken);
    }
}
