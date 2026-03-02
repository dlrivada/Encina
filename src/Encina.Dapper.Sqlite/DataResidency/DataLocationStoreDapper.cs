using System.Data;
using System.Globalization;
using Dapper;
using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;
using Encina.Messaging;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dapper.Sqlite.DataResidency;

/// <summary>
/// Dapper implementation of <see cref="IDataLocationStore"/> for SQLite.
/// Provides persistence for data location records tracking where data entities are physically stored.
/// </summary>
/// <remarks>
/// <para>
/// Per GDPR Article 30(1)(e), the controller must maintain records of processing activities
/// including transfers of personal data to a third country. Data location records provide
/// the foundation for demonstrating that data resides only in approved regions.
/// </para>
/// <para>
/// SQLite-specific considerations:
/// <list type="bullet">
/// <item><description>DateTimeOffset values are stored as ISO 8601 text via <c>.ToString("O")</c>.</description></item>
/// <item><description>Integer values require <c>Convert.ToInt32()</c> for safe casting.</description></item>
/// <item><description>Never uses <c>datetime('now')</c>; always uses parameterized values.</description></item>
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
    /// <param name="timeProvider">The time provider for UTC time (unused, kept for constructor consistency).</param>
    public DataLocationStoreDapper(
        IDbConnection connection,
        string tableName = "DataLocations",
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _tableName = SqlIdentifierValidator.ValidateTableName(tableName);
        _ = timeProvider; // Unused but kept for constructor consistency across providers
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

            await _connection.ExecuteAsync(sql, new
            {
                entity.Id,
                entity.EntityId,
                entity.DataCategory,
                entity.RegionCode,
                entity.StorageTypeValue,
                StoredAtUtc = entity.StoredAtUtc.ToString("O"),
                LastVerifiedAtUtc = entity.LastVerifiedAtUtc?.ToString("O"),
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
                SELECT Id, EntityId, DataCategory, RegionCode, StorageTypeValue, StoredAtUtc, LastVerifiedAtUtc, Metadata
                FROM {_tableName}
                WHERE EntityId = @EntityId";

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
                SELECT Id, EntityId, DataCategory, RegionCode, StorageTypeValue, StoredAtUtc, LastVerifiedAtUtc, Metadata
                FROM {_tableName}
                WHERE RegionCode = @RegionCode";

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
                SELECT Id, EntityId, DataCategory, RegionCode, StorageTypeValue, StoredAtUtc, LastVerifiedAtUtc, Metadata
                FROM {_tableName}
                WHERE DataCategory = @DataCategory";

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
            var sql = $"DELETE FROM {_tableName} WHERE EntityId = @EntityId";
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
            StoredAtUtc = DateTimeOffset.Parse((string)row.StoredAtUtc, null, DateTimeStyles.RoundtripKind),
            LastVerifiedAtUtc = row.LastVerifiedAtUtc is null or DBNull
                ? null
                : DateTimeOffset.Parse((string)row.LastVerifiedAtUtc, null, DateTimeStyles.RoundtripKind),
            Metadata = row.Metadata is null or DBNull ? null : (string)row.Metadata
        };
    }
}
