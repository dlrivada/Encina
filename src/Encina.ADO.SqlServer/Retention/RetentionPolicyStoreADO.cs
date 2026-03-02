using System.Data;
using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Model;
using Encina.Messaging;
using LanguageExt;
using Microsoft.Data.SqlClient;
using static LanguageExt.Prelude;

namespace Encina.ADO.SqlServer.Retention;

/// <summary>
/// ADO.NET implementation of <see cref="IRetentionPolicyStore"/> for SQL Server.
/// Provides CRUD operations for retention policies per GDPR Article 5(1)(e) storage limitation.
/// </summary>
public sealed class RetentionPolicyStoreADO : IRetentionPolicyStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetentionPolicyStoreADO"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The retention policies table name (default: RetentionPolicies).</param>
    /// <param name="timeProvider">The time provider for UTC time (default: <see cref="TimeProvider.System"/>).</param>
    public RetentionPolicyStoreADO(
        IDbConnection connection,
        string tableName = "RetentionPolicies",
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _tableName = SqlIdentifierValidator.ValidateTableName(tableName);
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> CreateAsync(
        RetentionPolicy policy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policy);

        try
        {
            var entity = RetentionPolicyMapper.ToEntity(policy);
            var sql = $@"
                INSERT INTO {_tableName}
                (Id, DataCategory, RetentionPeriodTicks, AutoDelete, Reason, LegalBasis, PolicyTypeValue, CreatedAtUtc, LastModifiedAtUtc)
                VALUES
                (@Id, @DataCategory, @RetentionPeriodTicks, @AutoDelete, @Reason, @LegalBasis, @PolicyTypeValue, @CreatedAtUtc, @LastModifiedAtUtc)";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", entity.Id);
            AddParameter(command, "@DataCategory", entity.DataCategory);
            AddParameter(command, "@RetentionPeriodTicks", entity.RetentionPeriodTicks);
            AddParameter(command, "@AutoDelete", entity.AutoDelete);
            AddParameter(command, "@Reason", entity.Reason);
            AddParameter(command, "@LegalBasis", entity.LegalBasis);
            AddParameter(command, "@PolicyTypeValue", entity.PolicyTypeValue);
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
                code: "retention.store_error",
                message: $"Failed to create retention policy: {ex.Message}",
                details: new Dictionary<string, object?> { ["policyId"] = policy.Id }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Option<RetentionPolicy>>> GetByIdAsync(
        string policyId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyId);

        try
        {
            var sql = $@"
                SELECT Id, DataCategory, RetentionPeriodTicks, AutoDelete, Reason, LegalBasis, PolicyTypeValue, CreatedAtUtc, LastModifiedAtUtc
                FROM {_tableName}
                WHERE Id = @Id";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", policyId);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            if (await ReadAsync(reader, cancellationToken))
            {
                var entity = MapToEntity(reader);
                var domain = RetentionPolicyMapper.ToDomain(entity);
                return domain is not null
                    ? Right<EncinaError, Option<RetentionPolicy>>(Some(domain))
                    : Right<EncinaError, Option<RetentionPolicy>>(None);
            }

            return Right<EncinaError, Option<RetentionPolicy>>(None);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to get retention policy: {ex.Message}",
                details: new Dictionary<string, object?> { ["policyId"] = policyId }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Option<RetentionPolicy>>> GetByCategoryAsync(
        string dataCategory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataCategory);

        try
        {
            var sql = $@"
                SELECT Id, DataCategory, RetentionPeriodTicks, AutoDelete, Reason, LegalBasis, PolicyTypeValue, CreatedAtUtc, LastModifiedAtUtc
                FROM {_tableName}
                WHERE DataCategory = @DataCategory";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@DataCategory", dataCategory);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            if (await ReadAsync(reader, cancellationToken))
            {
                var entity = MapToEntity(reader);
                var domain = RetentionPolicyMapper.ToDomain(entity);
                return domain is not null
                    ? Right<EncinaError, Option<RetentionPolicy>>(Some(domain))
                    : Right<EncinaError, Option<RetentionPolicy>>(None);
            }

            return Right<EncinaError, Option<RetentionPolicy>>(None);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to get retention policy by category: {ex.Message}",
                details: new Dictionary<string, object?> { ["dataCategory"] = dataCategory }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<RetentionPolicy>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sql = $@"
                SELECT Id, DataCategory, RetentionPeriodTicks, AutoDelete, Reason, LegalBasis, PolicyTypeValue, CreatedAtUtc, LastModifiedAtUtc
                FROM {_tableName}";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var policies = new List<RetentionPolicy>();
            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                var entity = MapToEntity(reader);
                var domain = RetentionPolicyMapper.ToDomain(entity);
                if (domain is not null)
                    policies.Add(domain);
            }

            return Right<EncinaError, IReadOnlyList<RetentionPolicy>>(policies);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to get all retention policies: {ex.Message}",
                details: new Dictionary<string, object?>()));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> UpdateAsync(
        RetentionPolicy policy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policy);

        try
        {
            var entity = RetentionPolicyMapper.ToEntity(policy);
            var sql = $@"
                UPDATE {_tableName}
                SET DataCategory = @DataCategory,
                    RetentionPeriodTicks = @RetentionPeriodTicks,
                    AutoDelete = @AutoDelete,
                    Reason = @Reason,
                    LegalBasis = @LegalBasis,
                    PolicyTypeValue = @PolicyTypeValue,
                    LastModifiedAtUtc = @LastModifiedAtUtc
                WHERE Id = @Id";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", entity.Id);
            AddParameter(command, "@DataCategory", entity.DataCategory);
            AddParameter(command, "@RetentionPeriodTicks", entity.RetentionPeriodTicks);
            AddParameter(command, "@AutoDelete", entity.AutoDelete);
            AddParameter(command, "@Reason", entity.Reason);
            AddParameter(command, "@LegalBasis", entity.LegalBasis);
            AddParameter(command, "@PolicyTypeValue", entity.PolicyTypeValue);
            AddParameter(command, "@LastModifiedAtUtc", entity.LastModifiedAtUtc?.UtcDateTime);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            await ExecuteNonQueryAsync(command, cancellationToken);
            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to update retention policy: {ex.Message}",
                details: new Dictionary<string, object?> { ["policyId"] = policy.Id }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> DeleteAsync(
        string policyId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyId);

        try
        {
            var sql = $"DELETE FROM {_tableName} WHERE Id = @Id";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", policyId);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            await ExecuteNonQueryAsync(command, cancellationToken);
            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to delete retention policy: {ex.Message}",
                details: new Dictionary<string, object?> { ["policyId"] = policyId }));
        }
    }

    private static RetentionPolicyEntity MapToEntity(IDataReader reader)
    {
        var lastModifiedOrd = reader.GetOrdinal("LastModifiedAtUtc");
        return new RetentionPolicyEntity
        {
            Id = reader.GetString(reader.GetOrdinal("Id")),
            DataCategory = reader.GetString(reader.GetOrdinal("DataCategory")),
            RetentionPeriodTicks = reader.GetInt64(reader.GetOrdinal("RetentionPeriodTicks")),
            AutoDelete = reader.GetBoolean(reader.GetOrdinal("AutoDelete")),
            Reason = reader.IsDBNull(reader.GetOrdinal("Reason"))
                ? null
                : reader.GetString(reader.GetOrdinal("Reason")),
            LegalBasis = reader.IsDBNull(reader.GetOrdinal("LegalBasis"))
                ? null
                : reader.GetString(reader.GetOrdinal("LegalBasis")),
            PolicyTypeValue = reader.GetInt32(reader.GetOrdinal("PolicyTypeValue")),
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
