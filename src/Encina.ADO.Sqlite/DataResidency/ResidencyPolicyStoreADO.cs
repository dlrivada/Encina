using System.Data;
using System.Globalization;
using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;
using Encina.Messaging;
using LanguageExt;
using Microsoft.Data.Sqlite;
using static LanguageExt.Prelude;

namespace Encina.ADO.Sqlite.DataResidency;

/// <summary>
/// ADO.NET implementation of <see cref="IResidencyPolicyStore"/> for SQLite.
/// Provides CRUD operations for residency policy descriptors per GDPR Article 44
/// (general principle for transfers).
/// </summary>
/// <remarks>
/// <para>
/// This store persists <see cref="ResidencyPolicyDescriptor"/> records using raw ADO.NET
/// commands against a SQLite database. DateTime values are stored as ISO 8601 TEXT using
/// the round-trip ("O") format specifier, and boolean values are stored as INTEGER (0/1).
/// </para>
/// <para>
/// The <see cref="ResidencyPolicyEntity.DataCategory"/> column serves as the primary key.
/// Each data category should have at most one residency policy.
/// </para>
/// <para>
/// All methods follow Railway Oriented Programming (ROP) using <c>Either&lt;EncinaError, T&gt;</c>
/// to provide explicit error handling without exceptions for business logic.
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
                (DataCategory, AllowedRegionCodes, RequireAdequacyDecision, AllowedTransferBasesValue, CreatedAtUtc, LastModifiedAtUtc)
                VALUES
                (@DataCategory, @AllowedRegionCodes, @RequireAdequacyDecision, @AllowedTransferBasesValue, @CreatedAtUtc, @LastModifiedAtUtc)";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@DataCategory", entity.DataCategory);
            AddParameter(command, "@AllowedRegionCodes", entity.AllowedRegionCodes);
            AddParameter(command, "@RequireAdequacyDecision", entity.RequireAdequacyDecision ? 1 : 0);
            AddParameter(command, "@AllowedTransferBasesValue", entity.AllowedTransferBasesValue);
            AddParameter(command, "@CreatedAtUtc", entity.CreatedAtUtc.ToString("O"));
            AddParameter(command, "@LastModifiedAtUtc", entity.LastModifiedAtUtc?.ToString("O"));

            var wasOpen = _connection.State == ConnectionState.Open;
            if (!wasOpen) _connection.Open();
            try
            {
                await ExecuteNonQueryAsync(command, cancellationToken);
                return Right(Unit.Default);
            }
            finally
            {
                if (!wasOpen) _connection.Close();
            }
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
                SELECT DataCategory, AllowedRegionCodes, RequireAdequacyDecision, AllowedTransferBasesValue, CreatedAtUtc, LastModifiedAtUtc
                FROM {_tableName}
                WHERE DataCategory = @DataCategory";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@DataCategory", dataCategory);

            var wasOpen = _connection.State == ConnectionState.Open;
            if (!wasOpen) _connection.Open();
            try
            {
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
            finally
            {
                if (!wasOpen) _connection.Close();
            }
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
                SELECT DataCategory, AllowedRegionCodes, RequireAdequacyDecision, AllowedTransferBasesValue, CreatedAtUtc, LastModifiedAtUtc
                FROM {_tableName}";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;

            var wasOpen = _connection.State == ConnectionState.Open;
            if (!wasOpen) _connection.Open();
            try
            {
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
            finally
            {
                if (!wasOpen) _connection.Close();
            }
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
            var sql = $@"
                UPDATE {_tableName}
                SET AllowedRegionCodes = @AllowedRegionCodes,
                    RequireAdequacyDecision = @RequireAdequacyDecision,
                    AllowedTransferBasesValue = @AllowedTransferBasesValue,
                    LastModifiedAtUtc = @LastModifiedAtUtc
                WHERE DataCategory = @DataCategory";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@DataCategory", entity.DataCategory);
            AddParameter(command, "@AllowedRegionCodes", entity.AllowedRegionCodes);
            AddParameter(command, "@RequireAdequacyDecision", entity.RequireAdequacyDecision ? 1 : 0);
            AddParameter(command, "@AllowedTransferBasesValue", entity.AllowedTransferBasesValue);
            AddParameter(command, "@LastModifiedAtUtc", _timeProvider.GetUtcNow().ToString("O"));

            var wasOpen = _connection.State == ConnectionState.Open;
            if (!wasOpen) _connection.Open();
            try
            {
                await ExecuteNonQueryAsync(command, cancellationToken);
                return Right(Unit.Default);
            }
            finally
            {
                if (!wasOpen) _connection.Close();
            }
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
            var sql = $"DELETE FROM {_tableName} WHERE DataCategory = @DataCategory";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@DataCategory", dataCategory);

            var wasOpen = _connection.State == ConnectionState.Open;
            if (!wasOpen) _connection.Open();
            try
            {
                await ExecuteNonQueryAsync(command, cancellationToken);
                return Right(Unit.Default);
            }
            finally
            {
                if (!wasOpen) _connection.Close();
            }
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
        var lastModifiedOrd = 5;
        return new ResidencyPolicyEntity
        {
            DataCategory = reader.GetString(0),
            AllowedRegionCodes = reader.GetString(1),
            RequireAdequacyDecision = reader.GetInt32(2) != 0,
            AllowedTransferBasesValue = reader.IsDBNull(3) ? null : reader.GetString(3),
            CreatedAtUtc = DateTimeOffset.Parse(reader.GetString(4), null, DateTimeStyles.RoundtripKind),
            LastModifiedAtUtc = reader.IsDBNull(lastModifiedOrd)
                ? null
                : DateTimeOffset.Parse(reader.GetString(lastModifiedOrd), null, DateTimeStyles.RoundtripKind)
        };
    }

    private static void AddParameter(IDbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private static async Task<IDataReader> ExecuteReaderAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is SqliteCommand sqliteCommand)
            return await sqliteCommand.ExecuteReaderAsync(cancellationToken);

        return await Task.Run(command.ExecuteReader, cancellationToken);
    }

    private static async Task<int> ExecuteNonQueryAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is SqliteCommand sqliteCommand)
            return await sqliteCommand.ExecuteNonQueryAsync(cancellationToken);

        return await Task.Run(command.ExecuteNonQuery, cancellationToken);
    }

    private static async Task<bool> ReadAsync(IDataReader reader, CancellationToken cancellationToken)
    {
        if (reader is SqliteDataReader sqliteReader)
            return await sqliteReader.ReadAsync(cancellationToken);

        return await Task.Run(reader.Read, cancellationToken);
    }
}
