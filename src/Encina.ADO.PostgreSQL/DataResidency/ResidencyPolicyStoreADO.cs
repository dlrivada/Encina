using System.Data;
using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;
using Encina.Messaging;
using LanguageExt;
using Npgsql;
using static LanguageExt.Prelude;

namespace Encina.ADO.PostgreSQL.DataResidency;

/// <summary>
/// ADO.NET implementation of <see cref="IResidencyPolicyStore"/> for PostgreSQL.
/// Provides CRUD operations for residency policy descriptors per GDPR Articles 44-49
/// cross-border transfer compliance.
/// </summary>
/// <remarks>
/// <para>
/// Residency policy descriptors define which regions are allowed for each data category,
/// whether adequacy decisions are required (Article 45), and which transfer legal bases
/// are permitted (Articles 46-49). This store persists these policies for enforcement
/// by <see cref="IDataResidencyPolicy"/>.
/// </para>
/// <para>
/// Uses lowercase column names without quotes for PostgreSQL compatibility.
/// Boolean values use native PostgreSQL boolean type. DateTime values are written via
/// <c>.UtcDateTime</c> and read back using <c>new DateTimeOffset(reader.GetDateTime(ordinal), TimeSpan.Zero)</c>.
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
    /// <remarks>
    /// Creates a new residency policy descriptor in the PostgreSQL store.
    /// The policy is mapped to a persistence entity using <see cref="ResidencyPolicyMapper.ToEntity"/>
    /// before insertion. The <c>datacategory</c> column acts as the primary key.
    /// </remarks>
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
                (datacategory, allowedregioncodes, requireadequacydecision, allowedtransferbasesvalue, createdatutc, lastmodifiedatutc)
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
    /// <remarks>
    /// Retrieves the residency policy descriptor for a specific data category.
    /// Returns <c>None</c> if no policy exists for the category. Each data category
    /// should have at most one residency policy, which defines the allowed regions
    /// and transfer conditions per GDPR Articles 44-49.
    /// </remarks>
    public async ValueTask<Either<EncinaError, Option<ResidencyPolicyDescriptor>>> GetByCategoryAsync(
        string dataCategory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataCategory);

        try
        {
            var sql = $@"
                SELECT datacategory, allowedregioncodes, requireadequacydecision, allowedtransferbasesvalue, createdatutc, lastmodifiedatutc
                FROM {_tableName}
                WHERE datacategory = @DataCategory";

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
    /// <remarks>
    /// Retrieves all residency policy descriptors from the store. Primarily used for
    /// compliance reporting, dashboards, and administrative interfaces that require
    /// a complete view of all data category residency rules.
    /// </remarks>
    public async ValueTask<Either<EncinaError, IReadOnlyList<ResidencyPolicyDescriptor>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sql = $@"
                SELECT datacategory, allowedregioncodes, requireadequacydecision, allowedtransferbasesvalue, createdatutc, lastmodifiedatutc
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
    /// <remarks>
    /// Updates an existing residency policy descriptor identified by data category.
    /// The <c>lastmodifiedatutc</c> timestamp is set using the configured <see cref="TimeProvider"/>.
    /// Updates take effect immediately for all subsequent residency checks per GDPR Articles 44-49.
    /// </remarks>
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
                SET allowedregioncodes = @AllowedRegionCodes,
                    requireadequacydecision = @RequireAdequacyDecision,
                    allowedtransferbasesvalue = @AllowedTransferBasesValue,
                    lastmodifiedatutc = @NowUtc
                WHERE datacategory = @DataCategory";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@DataCategory", entity.DataCategory);
            AddParameter(command, "@AllowedRegionCodes", entity.AllowedRegionCodes);
            AddParameter(command, "@RequireAdequacyDecision", entity.RequireAdequacyDecision);
            AddParameter(command, "@AllowedTransferBasesValue", entity.AllowedTransferBasesValue);
            AddParameter(command, "@NowUtc", _timeProvider.GetUtcNow().UtcDateTime);

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
    /// <remarks>
    /// Deletes the residency policy descriptor for a specific data category.
    /// Removing a policy removes all region restrictions for the category. Consider
    /// recording the deletion in the <see cref="IResidencyAuditStore"/> before removing
    /// for compliance traceability per GDPR Article 5(2).
    /// </remarks>
    public async ValueTask<Either<EncinaError, Unit>> DeleteAsync(
        string dataCategory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataCategory);

        try
        {
            var sql = $"DELETE FROM {_tableName} WHERE datacategory = @DataCategory";

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
        return new ResidencyPolicyEntity
        {
            DataCategory = reader.GetString(0),
            AllowedRegionCodes = reader.GetString(1),
            RequireAdequacyDecision = reader.GetBoolean(2),
            AllowedTransferBasesValue = reader.IsDBNull(3) ? null : reader.GetString(3),
            CreatedAtUtc = new DateTimeOffset(reader.GetDateTime(4), TimeSpan.Zero),
            LastModifiedAtUtc = reader.IsDBNull(5) ? null : new DateTimeOffset(reader.GetDateTime(5), TimeSpan.Zero)
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
        if (command is NpgsqlCommand sqlCommand)
            return await sqlCommand.ExecuteReaderAsync(cancellationToken);

        return await Task.Run(command.ExecuteReader, cancellationToken);
    }

    private static async Task<int> ExecuteNonQueryAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is NpgsqlCommand sqlCommand)
            return await sqlCommand.ExecuteNonQueryAsync(cancellationToken);

        return await Task.Run(command.ExecuteNonQuery, cancellationToken);
    }

    private static async Task<bool> ReadAsync(IDataReader reader, CancellationToken cancellationToken)
    {
        if (reader is NpgsqlDataReader sqlReader)
            return await sqlReader.ReadAsync(cancellationToken);

        return await Task.Run(reader.Read, cancellationToken);
    }
}
