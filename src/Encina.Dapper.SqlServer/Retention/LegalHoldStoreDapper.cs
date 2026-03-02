using System.Data;
using Dapper;
using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Model;
using Encina.Messaging;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dapper.SqlServer.Retention;

/// <summary>
/// Dapper implementation of <see cref="ILegalHoldStore"/> for SQL Server.
/// Manages legal hold (litigation hold) persistence and queries.
/// </summary>
/// <remarks>
/// <para>
/// Per GDPR Article 17(3)(e), the right to erasure does not apply when processing is
/// necessary for the establishment, exercise, or defence of legal claims. Legal holds
/// implement this exemption in a controlled, auditable manner.
/// </para>
/// <para>
/// SQL Server-specific considerations:
/// <list type="bullet">
/// <item><description>DateTimeOffset values are passed directly (native DATETIMEOFFSET support).</description></item>
/// <item><description>Existence checks use <c>CASE WHEN EXISTS ... THEN 1 ELSE 0 END</c>.</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class LegalHoldStoreDapper : ILegalHoldStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="LegalHoldStoreDapper"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The legal holds table name (default: LegalHolds).</param>
    /// <param name="timeProvider">The time provider for UTC time (default: <see cref="TimeProvider.System"/>).</param>
    public LegalHoldStoreDapper(
        IDbConnection connection,
        string tableName = "LegalHolds",
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _tableName = SqlIdentifierValidator.ValidateTableName(tableName);
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> CreateAsync(
        LegalHold hold,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(hold);

        try
        {
            var entity = LegalHoldMapper.ToEntity(hold);
            var sql = $@"
                INSERT INTO {_tableName}
                (Id, EntityId, Reason, AppliedByUserId, AppliedAtUtc, ReleasedAtUtc, ReleasedByUserId)
                VALUES
                (@Id, @EntityId, @Reason, @AppliedByUserId, @AppliedAtUtc, @ReleasedAtUtc, @ReleasedByUserId)";

            await _connection.ExecuteAsync(sql, new
            {
                entity.Id,
                entity.EntityId,
                entity.Reason,
                entity.AppliedByUserId,
                entity.AppliedAtUtc,
                entity.ReleasedAtUtc,
                entity.ReleasedByUserId
            });

            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to create legal hold: {ex.Message}",
                details: new Dictionary<string, object?> { ["holdId"] = hold.Id }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Option<LegalHold>>> GetByIdAsync(
        string holdId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(holdId);

        try
        {
            var sql = $@"
                SELECT Id, EntityId, Reason, AppliedByUserId, AppliedAtUtc, ReleasedAtUtc, ReleasedByUserId
                FROM {_tableName}
                WHERE Id = @Id";

            var rows = await _connection.QueryAsync(sql, new { Id = holdId });
            var row = rows.FirstOrDefault();

            if (row is null)
            {
                return Right<EncinaError, Option<LegalHold>>(None);
            }

            var entity = MapToEntity(row);
            var domain = LegalHoldMapper.ToDomain(entity);

            return Right<EncinaError, Option<LegalHold>>(Some(domain));
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to get legal hold: {ex.Message}",
                details: new Dictionary<string, object?> { ["holdId"] = holdId }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<LegalHold>>> GetByEntityIdAsync(
        string entityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        try
        {
            var sql = $@"
                SELECT Id, EntityId, Reason, AppliedByUserId, AppliedAtUtc, ReleasedAtUtc, ReleasedByUserId
                FROM {_tableName}
                WHERE EntityId = @EntityId";

            var rows = await _connection.QueryAsync(sql, new { EntityId = entityId });
            var holds = rows.Select(row => LegalHoldMapper.ToDomain(MapToEntity(row))).Cast<LegalHold>().ToList();

            return Right<EncinaError, IReadOnlyList<LegalHold>>(holds);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to get legal holds by entity: {ex.Message}",
                details: new Dictionary<string, object?> { ["entityId"] = entityId }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, bool>> IsUnderHoldAsync(
        string entityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        try
        {
            var sql = $@"
                SELECT CASE WHEN EXISTS (
                    SELECT 1 FROM {_tableName}
                    WHERE EntityId = @EntityId AND ReleasedAtUtc IS NULL
                ) THEN 1 ELSE 0 END";

            var result = await _connection.ExecuteScalarAsync<int>(sql, new { EntityId = entityId });

            return Right(result != 0);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to check legal hold status: {ex.Message}",
                details: new Dictionary<string, object?> { ["entityId"] = entityId }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<LegalHold>>> GetActiveHoldsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sql = $@"
                SELECT Id, EntityId, Reason, AppliedByUserId, AppliedAtUtc, ReleasedAtUtc, ReleasedByUserId
                FROM {_tableName}
                WHERE ReleasedAtUtc IS NULL";

            var rows = await _connection.QueryAsync(sql);
            var holds = rows.Select(row => LegalHoldMapper.ToDomain(MapToEntity(row))).Cast<LegalHold>().ToList();

            return Right<EncinaError, IReadOnlyList<LegalHold>>(holds);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to get active legal holds: {ex.Message}",
                details: new Dictionary<string, object?>()));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> ReleaseAsync(
        string holdId,
        string? releasedByUserId,
        DateTimeOffset releasedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(holdId);

        try
        {
            var sql = $@"
                UPDATE {_tableName}
                SET ReleasedAtUtc = @ReleasedAtUtc,
                    ReleasedByUserId = @ReleasedByUserId
                WHERE Id = @Id";

            await _connection.ExecuteAsync(sql, new
            {
                Id = holdId,
                ReleasedAtUtc = releasedAtUtc,
                ReleasedByUserId = releasedByUserId
            });

            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to release legal hold: {ex.Message}",
                details: new Dictionary<string, object?> { ["holdId"] = holdId }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<LegalHold>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sql = $@"
                SELECT Id, EntityId, Reason, AppliedByUserId, AppliedAtUtc, ReleasedAtUtc, ReleasedByUserId
                FROM {_tableName}";

            var rows = await _connection.QueryAsync(sql);
            var holds = rows.Select(row => LegalHoldMapper.ToDomain(MapToEntity(row))).Cast<LegalHold>().ToList();

            return Right<EncinaError, IReadOnlyList<LegalHold>>(holds);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to get all legal holds: {ex.Message}",
                details: new Dictionary<string, object?>()));
        }
    }

    private static LegalHoldEntity MapToEntity(dynamic row)
    {
        return new LegalHoldEntity
        {
            Id = (string)row.Id,
            EntityId = (string)row.EntityId,
            Reason = (string)row.Reason,
            AppliedByUserId = row.AppliedByUserId is null or DBNull ? null : (string)row.AppliedByUserId,
            AppliedAtUtc = (DateTimeOffset)row.AppliedAtUtc,
            ReleasedAtUtc = row.ReleasedAtUtc is null or DBNull ? null : (DateTimeOffset?)row.ReleasedAtUtc,
            ReleasedByUserId = row.ReleasedByUserId is null or DBNull ? null : (string)row.ReleasedByUserId
        };
    }
}
