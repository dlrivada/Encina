using System.Data;
using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;
using Encina.Messaging;
using LanguageExt;
using Npgsql;
using static LanguageExt.Prelude;

namespace Encina.ADO.PostgreSQL.DataResidency;

/// <summary>
/// ADO.NET implementation of <see cref="IDataLocationStore"/> for PostgreSQL.
/// Provides persistence operations for data location records to track where data entities
/// are physically stored, supporting GDPR Articles 44-49 cross-border transfer compliance.
/// </summary>
/// <remarks>
/// <para>
/// This store records the physical locations of data entities across regions and storage types,
/// enabling the system to verify compliance with residency policies and provide evidence of
/// data location for regulatory audits per GDPR Article 30(1)(e).
/// </para>
/// <para>
/// Uses lowercase column names without quotes for PostgreSQL compatibility.
/// DateTime values are written via <c>.UtcDateTime</c> and read back using
/// <c>new DateTimeOffset(reader.GetDateTime(ordinal), TimeSpan.Zero)</c>.
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
    /// <remarks>
    /// Persists a data location record to the PostgreSQL store. The location is mapped
    /// to a persistence entity using <see cref="DataLocationMapper.ToEntity"/> before insertion.
    /// Supports GDPR Article 30(1)(e) record-keeping for data storage locations.
    /// </remarks>
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
                (id, entityid, datacategory, regioncode, storagetypevalue, storedatutc, lastverifiedatutc, metadata)
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
    /// <remarks>
    /// Retrieves all data location records for a specific entity identifier.
    /// An entity may have multiple location records across regions and storage types
    /// (e.g., primary in EU, replica in US). Results support cross-border transfer
    /// validation per GDPR Articles 44-49.
    /// </remarks>
    public async ValueTask<Either<EncinaError, IReadOnlyList<DataLocation>>> GetByEntityAsync(
        string entityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        try
        {
            var sql = $@"
                SELECT id, entityid, datacategory, regioncode, storagetypevalue, storedatutc, lastverifiedatutc, metadata
                FROM {_tableName}
                WHERE entityid = @EntityId";

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
    /// <remarks>
    /// Retrieves all data location records stored in a specific region. Useful for
    /// compliance audits to identify all data stored in a particular jurisdiction,
    /// or for data migration planning when a region's adequacy status changes
    /// under GDPR Article 45.
    /// </remarks>
    public async ValueTask<Either<EncinaError, IReadOnlyList<DataLocation>>> GetByRegionAsync(
        Region region,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(region);

        try
        {
            var sql = $@"
                SELECT id, entityid, datacategory, regioncode, storagetypevalue, storedatutc, lastverifiedatutc, metadata
                FROM {_tableName}
                WHERE regioncode = @RegionCode";

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
    /// <remarks>
    /// Retrieves all data location records for a specific data category. Useful for
    /// generating category-specific compliance reports and verifying that all instances
    /// of a data category comply with their residency policy per GDPR Articles 44-49.
    /// </remarks>
    public async ValueTask<Either<EncinaError, IReadOnlyList<DataLocation>>> GetByCategoryAsync(
        string dataCategory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataCategory);

        try
        {
            var sql = $@"
                SELECT id, entityid, datacategory, regioncode, storagetypevalue, storedatutc, lastverifiedatutc, metadata
                FROM {_tableName}
                WHERE datacategory = @DataCategory";

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
    /// <remarks>
    /// Deletes all data location records for a specific entity. Typically called after
    /// a data subject erasure request (GDPR Art. 17) to remove location tracking records
    /// once the actual data has been deleted.
    /// </remarks>
    public async ValueTask<Either<EncinaError, Unit>> DeleteByEntityAsync(
        string entityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        try
        {
            var sql = $"DELETE FROM {_tableName} WHERE entityid = @EntityId";

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
        return new DataLocationEntity
        {
            Id = reader.GetString(0),
            EntityId = reader.GetString(1),
            DataCategory = reader.GetString(2),
            RegionCode = reader.GetString(3),
            StorageTypeValue = reader.GetInt32(4),
            StoredAtUtc = new DateTimeOffset(reader.GetDateTime(5), TimeSpan.Zero),
            LastVerifiedAtUtc = reader.IsDBNull(6) ? null : new DateTimeOffset(reader.GetDateTime(6), TimeSpan.Zero),
            Metadata = reader.IsDBNull(7) ? null : reader.GetString(7)
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
