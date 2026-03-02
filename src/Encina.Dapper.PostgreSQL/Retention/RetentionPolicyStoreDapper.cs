using System.Data;
using Dapper;
using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Model;
using Encina.Messaging;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dapper.PostgreSQL.Retention;

/// <summary>
/// Dapper implementation of <see cref="IRetentionPolicyStore"/> for PostgreSQL.
/// Provides CRUD operations for retention policy lifecycle management.
/// </summary>
/// <remarks>
/// <para>
/// Per GDPR Article 5(1)(e) (storage limitation), retention policies formalize
/// explicit, auditable retention periods per data category.
/// </para>
/// <para>
/// PostgreSQL-specific considerations:
/// <list type="bullet">
/// <item><description>Lowercase unquoted column identifiers (PostgreSQL folds to lowercase).</description></item>
/// <item><description>DateTimeOffset values are written via <c>.UtcDateTime</c> (TIMESTAMP type).</description></item>
/// <item><description>Boolean values are passed directly (native bool support).</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class RetentionPolicyStoreDapper : IRetentionPolicyStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetentionPolicyStoreDapper"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The retention policies table name (default: RetentionPolicies).</param>
    /// <param name="timeProvider">The time provider for UTC time (default: <see cref="TimeProvider.System"/>).</param>
    public RetentionPolicyStoreDapper(
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
                (id, datacategory, retentionperiodticks, autodelete, reason, legalbasis, policytypevalue, createdatutc, lastmodifiedatutc)
                VALUES
                (@Id, @DataCategory, @RetentionPeriodTicks, @AutoDelete, @Reason, @LegalBasis, @PolicyTypeValue, @CreatedAtUtc, @LastModifiedAtUtc)";

            await _connection.ExecuteAsync(sql, new
            {
                entity.Id,
                entity.DataCategory,
                entity.RetentionPeriodTicks,
                entity.AutoDelete,
                entity.Reason,
                entity.LegalBasis,
                entity.PolicyTypeValue,
                CreatedAtUtc = entity.CreatedAtUtc.UtcDateTime,
                LastModifiedAtUtc = entity.LastModifiedAtUtc?.UtcDateTime
            });

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
                SELECT id, datacategory, retentionperiodticks, autodelete, reason, legalbasis, policytypevalue, createdatutc, lastmodifiedatutc
                FROM {_tableName}
                WHERE id = @Id";

            var rows = await _connection.QueryAsync(sql, new { Id = policyId });
            var row = rows.FirstOrDefault();

            if (row is null)
            {
                return Right<EncinaError, Option<RetentionPolicy>>(None);
            }

            var entity = MapToEntity(row);
            var domain = RetentionPolicyMapper.ToDomain(entity);

            return domain is null
                ? Right<EncinaError, Option<RetentionPolicy>>(None)
                : Right<EncinaError, Option<RetentionPolicy>>(Some(domain));
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
                SELECT id, datacategory, retentionperiodticks, autodelete, reason, legalbasis, policytypevalue, createdatutc, lastmodifiedatutc
                FROM {_tableName}
                WHERE datacategory = @DataCategory";

            var rows = await _connection.QueryAsync(sql, new { DataCategory = dataCategory });
            var row = rows.FirstOrDefault();

            if (row is null)
            {
                return Right<EncinaError, Option<RetentionPolicy>>(None);
            }

            var entity = MapToEntity(row);
            var domain = RetentionPolicyMapper.ToDomain(entity);

            return domain is null
                ? Right<EncinaError, Option<RetentionPolicy>>(None)
                : Right<EncinaError, Option<RetentionPolicy>>(Some(domain));
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
                SELECT id, datacategory, retentionperiodticks, autodelete, reason, legalbasis, policytypevalue, createdatutc, lastmodifiedatutc
                FROM {_tableName}";

            var rows = await _connection.QueryAsync(sql);
            var policies = rows
                .Select(row => RetentionPolicyMapper.ToDomain(MapToEntity(row)))
                .Where(p => p is not null)
                .Cast<RetentionPolicy>()
                .ToList();

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
                SET datacategory = @DataCategory,
                    retentionperiodticks = @RetentionPeriodTicks,
                    autodelete = @AutoDelete,
                    reason = @Reason,
                    legalbasis = @LegalBasis,
                    policytypevalue = @PolicyTypeValue,
                    createdatutc = @CreatedAtUtc,
                    lastmodifiedatutc = @LastModifiedAtUtc
                WHERE id = @Id";

            await _connection.ExecuteAsync(sql, new
            {
                entity.Id,
                entity.DataCategory,
                entity.RetentionPeriodTicks,
                entity.AutoDelete,
                entity.Reason,
                entity.LegalBasis,
                entity.PolicyTypeValue,
                CreatedAtUtc = entity.CreatedAtUtc.UtcDateTime,
                LastModifiedAtUtc = entity.LastModifiedAtUtc?.UtcDateTime
            });

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
            var sql = $"DELETE FROM {_tableName} WHERE id = @Id";
            await _connection.ExecuteAsync(sql, new { Id = policyId });

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

    private static RetentionPolicyEntity MapToEntity(dynamic row)
    {
        return new RetentionPolicyEntity
        {
            Id = (string)row.id,
            DataCategory = (string)row.datacategory,
            RetentionPeriodTicks = (long)row.retentionperiodticks,
            AutoDelete = (bool)row.autodelete,
            Reason = row.reason is null or DBNull ? null : (string)row.reason,
            LegalBasis = row.legalbasis is null or DBNull ? null : (string)row.legalbasis,
            PolicyTypeValue = Convert.ToInt32(row.policytypevalue),
            CreatedAtUtc = new DateTimeOffset((DateTime)row.createdatutc, TimeSpan.Zero),
            LastModifiedAtUtc = row.lastmodifiedatutc is null or DBNull
                ? null
                : new DateTimeOffset((DateTime)row.lastmodifiedatutc, TimeSpan.Zero)
        };
    }
}
