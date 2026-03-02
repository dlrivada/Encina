using System.Data;
using Dapper;
using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;
using Encina.Messaging;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dapper.MySQL.DataResidency;

/// <summary>
/// Dapper implementation of <see cref="IDataLocationStore"/> for MySQL.
/// Provides tracking of data location records for residency compliance.
/// </summary>
/// <remarks>
/// <para>
/// Per GDPR Article 30(1)(e), the controller must maintain records of processing activities
/// including transfers of personal data to a third country. This store tracks where data
/// entities are physically stored and processed, enabling compliance verification.
/// </para>
/// <para>
/// MySQL-specific considerations:
/// <list type="bullet">
/// <item><description>DateTimeOffset values are written via <c>.UtcDateTime</c> (DATETIME type).</description></item>
/// <item><description>Integer values require <c>Convert.ToInt32()</c> for safe casting from dynamic rows.</description></item>
/// <item><description>PascalCase column names in SQL queries.</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class DataLocationStoreDapper : IDataLocationStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataLocationStoreDapper"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The data locations table name (default: DataLocations).</param>
    /// <param name="timeProvider">The time provider for UTC time (default: <see cref="TimeProvider.System"/>). Reserved for future use.</param>
    public DataLocationStoreDapper(
        IDbConnection connection,
        string tableName = "DataLocations",
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _tableName = SqlIdentifierValidator.ValidateTableName(tableName);
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
                (`Id`, `EntityId`, `DataCategory`, `RegionCode`, `StorageTypeValue`, `StoredAtUtc`, `LastVerifiedAtUtc`, `Metadata`)
                VALUES
                (@Id, @EntityId, @DataCategory, @RegionCode, @StorageTypeValue, @StoredAtUtc, @LastVerifiedAtUtc, @Metadata)";

            await _connection.ExecuteAsync(sql, new
            {
                entity.Id,
                entity.EntityId,
                entity.DataCategory,
                entity.RegionCode,
                entity.StorageTypeValue,
                StoredAtUtc = entity.StoredAtUtc.UtcDateTime,
                LastVerifiedAtUtc = entity.LastVerifiedAtUtc?.UtcDateTime,
                entity.Metadata
            });

            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "residency.store_error",
                message: $"Failed to record data location: {ex.Message}",
                details: new Dictionary<string, object?> { ["locationId"] = location.Id }));
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
                SELECT `Id`, `EntityId`, `DataCategory`, `RegionCode`, `StorageTypeValue`, `StoredAtUtc`, `LastVerifiedAtUtc`, `Metadata`
                FROM {_tableName}
                WHERE `EntityId` = @EntityId";

            var rows = await _connection.QueryAsync(sql, new { EntityId = entityId });
            var locations = rows
                .Select(row => DataLocationMapper.ToDomain(MapToEntity(row)))
                .Where(l => l is not null)
                .Cast<DataLocation>()
                .ToList();

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
                SELECT `Id`, `EntityId`, `DataCategory`, `RegionCode`, `StorageTypeValue`, `StoredAtUtc`, `LastVerifiedAtUtc`, `Metadata`
                FROM {_tableName}
                WHERE `RegionCode` = @RegionCode";

            var rows = await _connection.QueryAsync(sql, new { RegionCode = region.Code });
            var locations = rows
                .Select(row => DataLocationMapper.ToDomain(MapToEntity(row)))
                .Where(l => l is not null)
                .Cast<DataLocation>()
                .ToList();

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
                SELECT `Id`, `EntityId`, `DataCategory`, `RegionCode`, `StorageTypeValue`, `StoredAtUtc`, `LastVerifiedAtUtc`, `Metadata`
                FROM {_tableName}
                WHERE `DataCategory` = @DataCategory";

            var rows = await _connection.QueryAsync(sql, new { DataCategory = dataCategory });
            var locations = rows
                .Select(row => DataLocationMapper.ToDomain(MapToEntity(row)))
                .Where(l => l is not null)
                .Cast<DataLocation>()
                .ToList();

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
            var sql = $"DELETE FROM {_tableName} WHERE `EntityId` = @EntityId";
            await _connection.ExecuteAsync(sql, new { EntityId = entityId });

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

    private static DataLocationEntity MapToEntity(dynamic row)
    {
        return new DataLocationEntity
        {
            Id = (string)row.Id,
            EntityId = (string)row.EntityId,
            DataCategory = (string)row.DataCategory,
            RegionCode = (string)row.RegionCode,
            StorageTypeValue = Convert.ToInt32(row.StorageTypeValue),
            StoredAtUtc = new DateTimeOffset((DateTime)row.StoredAtUtc, TimeSpan.Zero),
            LastVerifiedAtUtc = row.LastVerifiedAtUtc is null or DBNull
                ? null
                : new DateTimeOffset((DateTime)row.LastVerifiedAtUtc, TimeSpan.Zero),
            Metadata = row.Metadata is null or DBNull ? null : (string)row.Metadata
        };
    }
}
