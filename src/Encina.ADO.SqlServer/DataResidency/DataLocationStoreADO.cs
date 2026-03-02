using System.Data;

using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;
using Encina.Messaging;

using LanguageExt;

using Microsoft.Data.SqlClient;

using static LanguageExt.Prelude;

namespace Encina.ADO.SqlServer.DataResidency;

/// <summary>
/// ADO.NET implementation of <see cref="IDataLocationStore"/> for SQL Server.
/// Records and queries the physical locations of data entities for residency compliance.
/// </summary>
/// <remarks>
/// <para>
/// Per GDPR Article 30(1)(e), controllers must maintain records of processing activities
/// including transfers of personal data to a third country. This store tracks where data
/// entities are physically stored, enabling compliance audits and regulatory reporting.
/// </para>
/// <para>
/// Per GDPR Articles 44-49 (Chapter V), transfers of personal data to third countries
/// are subject to appropriate safeguards. Data location tracking is essential for
/// verifying that all data copies comply with residency policies.
/// </para>
/// </remarks>
public sealed class DataLocationStoreADO : IDataLocationStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataLocationStoreADO"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The data locations table name (default: DataLocations).</param>
    /// <param name="timeProvider">The time provider for UTC time (default: <see cref="TimeProvider.System"/>).</param>
    public DataLocationStoreADO(
        IDbConnection connection,
        string tableName = "DataLocations",
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _tableName = SqlIdentifierValidator.ValidateTableName(tableName);
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> RecordAsync(
        DataLocation location,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(location);

        try
        {
            var entity = DataLocationMapper.ToEntity(location);
            var sql = $@"
                INSERT INTO {_tableName}
                ([Id], [EntityId], [DataCategory], [RegionCode], [StorageTypeValue], [StoredAtUtc], [LastVerifiedAtUtc], [Metadata])
                VALUES
                (@Id, @EntityId, @DataCategory, @RegionCode, @StorageTypeValue, @StoredAtUtc, @LastVerifiedAtUtc, @Metadata)";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", entity.Id);
            AddParameter(command, "@EntityId", entity.EntityId);
            AddParameter(command, "@DataCategory", entity.DataCategory);
            AddParameter(command, "@RegionCode", entity.RegionCode);
            AddParameter(command, "@StorageTypeValue", entity.StorageTypeValue);
            AddParameter(command, "@StoredAtUtc", entity.StoredAtUtc.UtcDateTime);
            AddParameter(command, "@LastVerifiedAtUtc", entity.LastVerifiedAtUtc?.UtcDateTime);
            AddParameter(command, "@Metadata", entity.Metadata);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            await ExecuteNonQueryAsync(command, cancellationToken);
            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "residency.store_error",
                message: $"Failed to record data location: {ex.Message}",
                details: new Dictionary<string, object?> { ["entityId"] = location.EntityId }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<DataLocation>>> GetByEntityAsync(
        string entityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        try
        {
            var sql = $@"
                SELECT [Id], [EntityId], [DataCategory], [RegionCode], [StorageTypeValue], [StoredAtUtc], [LastVerifiedAtUtc], [Metadata]
                FROM {_tableName}
                WHERE [EntityId] = @EntityId";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@EntityId", entityId);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var locations = new List<DataLocation>();
            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                var entity = MapToEntity(reader);
                var domain = DataLocationMapper.ToDomain(entity);
                if (domain is not null)
                    locations.Add(domain);
            }

            return Right<EncinaError, IReadOnlyList<DataLocation>>(locations);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "residency.store_error",
                message: $"Failed to get data locations by entity: {ex.Message}",
                details: new Dictionary<string, object?> { ["entityId"] = entityId }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<DataLocation>>> GetByRegionAsync(
        Region region,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(region);

        try
        {
            var sql = $@"
                SELECT [Id], [EntityId], [DataCategory], [RegionCode], [StorageTypeValue], [StoredAtUtc], [LastVerifiedAtUtc], [Metadata]
                FROM {_tableName}
                WHERE [RegionCode] = @RegionCode";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@RegionCode", region.Code);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var locations = new List<DataLocation>();
            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                var entity = MapToEntity(reader);
                var domain = DataLocationMapper.ToDomain(entity);
                if (domain is not null)
                    locations.Add(domain);
            }

            return Right<EncinaError, IReadOnlyList<DataLocation>>(locations);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "residency.store_error",
                message: $"Failed to get data locations by region: {ex.Message}",
                details: new Dictionary<string, object?> { ["regionCode"] = region.Code }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<DataLocation>>> GetByCategoryAsync(
        string dataCategory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataCategory);

        try
        {
            var sql = $@"
                SELECT [Id], [EntityId], [DataCategory], [RegionCode], [StorageTypeValue], [StoredAtUtc], [LastVerifiedAtUtc], [Metadata]
                FROM {_tableName}
                WHERE [DataCategory] = @DataCategory";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@DataCategory", dataCategory);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var locations = new List<DataLocation>();
            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                var entity = MapToEntity(reader);
                var domain = DataLocationMapper.ToDomain(entity);
                if (domain is not null)
                    locations.Add(domain);
            }

            return Right<EncinaError, IReadOnlyList<DataLocation>>(locations);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "residency.store_error",
                message: $"Failed to get data locations by category: {ex.Message}",
                details: new Dictionary<string, object?> { ["dataCategory"] = dataCategory }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> DeleteByEntityAsync(
        string entityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        try
        {
            var sql = $"DELETE FROM {_tableName} WHERE [EntityId] = @EntityId";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@EntityId", entityId);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            await ExecuteNonQueryAsync(command, cancellationToken);
            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "residency.store_error",
                message: $"Failed to delete data locations by entity: {ex.Message}",
                details: new Dictionary<string, object?> { ["entityId"] = entityId }));
        }
    }

    private static DataLocationEntity MapToEntity(IDataReader reader)
    {
        var lastVerifiedOrd = reader.GetOrdinal("LastVerifiedAtUtc");
        var metadataOrd = reader.GetOrdinal("Metadata");

        return new DataLocationEntity
        {
            Id = reader.GetString(reader.GetOrdinal("Id")),
            EntityId = reader.GetString(reader.GetOrdinal("EntityId")),
            DataCategory = reader.GetString(reader.GetOrdinal("DataCategory")),
            RegionCode = reader.GetString(reader.GetOrdinal("RegionCode")),
            StorageTypeValue = reader.GetInt32(reader.GetOrdinal("StorageTypeValue")),
            StoredAtUtc = new DateTimeOffset(reader.GetDateTime(reader.GetOrdinal("StoredAtUtc")), TimeSpan.Zero),
            LastVerifiedAtUtc = reader.IsDBNull(lastVerifiedOrd)
                ? null
                : new DateTimeOffset(reader.GetDateTime(lastVerifiedOrd), TimeSpan.Zero),
            Metadata = reader.IsDBNull(metadataOrd)
                ? null
                : reader.GetString(metadataOrd)
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
