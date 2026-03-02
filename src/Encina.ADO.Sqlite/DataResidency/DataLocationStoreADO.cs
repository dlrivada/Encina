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
/// ADO.NET implementation of <see cref="IDataLocationStore"/> for SQLite.
/// Tracks where data entities are physically stored and processed,
/// enabling verification of compliance with residency policies per GDPR Article 30(1)(e).
/// </summary>
/// <remarks>
/// <para>
/// This store persists <see cref="DataLocation"/> records using raw ADO.NET commands
/// against a SQLite database. DateTime values are stored as ISO 8601 TEXT using the
/// round-trip ("O") format specifier, and boolean values are stored as INTEGER (0/1).
/// </para>
/// <para>
/// All methods follow Railway Oriented Programming (ROP) using <c>Either&lt;EncinaError, T&gt;</c>
/// to provide explicit error handling without exceptions for business logic.
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
                (Id, EntityId, DataCategory, RegionCode, StorageTypeValue, StoredAtUtc, LastVerifiedAtUtc, Metadata)
                VALUES
                (@Id, @EntityId, @DataCategory, @RegionCode, @StorageTypeValue, @StoredAtUtc, @LastVerifiedAtUtc, @Metadata)";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", entity.Id);
            AddParameter(command, "@EntityId", entity.EntityId);
            AddParameter(command, "@DataCategory", entity.DataCategory);
            AddParameter(command, "@RegionCode", entity.RegionCode);
            AddParameter(command, "@StorageTypeValue", entity.StorageTypeValue);
            AddParameter(command, "@StoredAtUtc", entity.StoredAtUtc.ToString("O"));
            AddParameter(command, "@LastVerifiedAtUtc", entity.LastVerifiedAtUtc?.ToString("O"));
            AddParameter(command, "@Metadata", entity.Metadata);

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
                SELECT Id, EntityId, DataCategory, RegionCode, StorageTypeValue, StoredAtUtc, LastVerifiedAtUtc, Metadata
                FROM {_tableName}
                WHERE EntityId = @EntityId";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@EntityId", entityId);

            var wasOpen = _connection.State == ConnectionState.Open;
            if (!wasOpen) _connection.Open();
            try
            {
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
            finally
            {
                if (!wasOpen) _connection.Close();
            }
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
                SELECT Id, EntityId, DataCategory, RegionCode, StorageTypeValue, StoredAtUtc, LastVerifiedAtUtc, Metadata
                FROM {_tableName}
                WHERE RegionCode = @RegionCode";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@RegionCode", region.Code);

            var wasOpen = _connection.State == ConnectionState.Open;
            if (!wasOpen) _connection.Open();
            try
            {
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
            finally
            {
                if (!wasOpen) _connection.Close();
            }
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
                SELECT Id, EntityId, DataCategory, RegionCode, StorageTypeValue, StoredAtUtc, LastVerifiedAtUtc, Metadata
                FROM {_tableName}
                WHERE DataCategory = @DataCategory";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@DataCategory", dataCategory);

            var wasOpen = _connection.State == ConnectionState.Open;
            if (!wasOpen) _connection.Open();
            try
            {
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
            finally
            {
                if (!wasOpen) _connection.Close();
            }
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
            var sql = $"DELETE FROM {_tableName} WHERE EntityId = @EntityId";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@EntityId", entityId);

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
            StoredAtUtc = DateTimeOffset.Parse(reader.GetString(5), null, DateTimeStyles.RoundtripKind),
            LastVerifiedAtUtc = reader.IsDBNull(6)
                ? null
                : DateTimeOffset.Parse(reader.GetString(6), null, DateTimeStyles.RoundtripKind),
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
