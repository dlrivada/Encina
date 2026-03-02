using System.Data;
using Dapper;
using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Model;
using Encina.Messaging;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dapper.MySQL.Retention;

/// <summary>
/// Dapper implementation of <see cref="IRetentionPolicyStore"/> for MySQL.
/// Provides CRUD operations for retention policy lifecycle management.
/// </summary>
/// <remarks>
/// <para>
/// Per GDPR Article 5(1)(e) (storage limitation), retention policies formalize
/// explicit, auditable retention periods per data category.
/// </para>
/// <para>
/// MySQL-specific considerations:
/// <list type="bullet">
/// <item><description>DateTimeOffset values are written via <c>.UtcDateTime</c> (DATETIME type).</description></item>
/// <item><description>Boolean values are stored as integers (0/1) using ternary conversion.</description></item>
/// <item><description>Integer values require <c>Convert.ToInt32()</c> for safe casting.</description></item>
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
                (Id, DataCategory, RetentionPeriodTicks, AutoDelete, Reason, LegalBasis, PolicyTypeValue, CreatedAtUtc, LastModifiedAtUtc)
                VALUES
                (@Id, @DataCategory, @RetentionPeriodTicks, @AutoDelete, @Reason, @LegalBasis, @PolicyTypeValue, @CreatedAtUtc, @LastModifiedAtUtc)";

            await _connection.ExecuteAsync(sql, new
            {
                entity.Id,
                entity.DataCategory,
                entity.RetentionPeriodTicks,
                AutoDelete = entity.AutoDelete ? 1 : 0,
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
                SELECT Id, DataCategory, RetentionPeriodTicks, AutoDelete, Reason, LegalBasis, PolicyTypeValue, CreatedAtUtc, LastModifiedAtUtc
                FROM {_tableName}
                WHERE Id = @Id";

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
                SELECT Id, DataCategory, RetentionPeriodTicks, AutoDelete, Reason, LegalBasis, PolicyTypeValue, CreatedAtUtc, LastModifiedAtUtc
                FROM {_tableName}
                WHERE DataCategory = @DataCategory";

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
                SELECT Id, DataCategory, RetentionPeriodTicks, AutoDelete, Reason, LegalBasis, PolicyTypeValue, CreatedAtUtc, LastModifiedAtUtc
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
                SET DataCategory = @DataCategory,
                    RetentionPeriodTicks = @RetentionPeriodTicks,
                    AutoDelete = @AutoDelete,
                    Reason = @Reason,
                    LegalBasis = @LegalBasis,
                    PolicyTypeValue = @PolicyTypeValue,
                    CreatedAtUtc = @CreatedAtUtc,
                    LastModifiedAtUtc = @LastModifiedAtUtc
                WHERE Id = @Id";

            await _connection.ExecuteAsync(sql, new
            {
                entity.Id,
                entity.DataCategory,
                entity.RetentionPeriodTicks,
                AutoDelete = entity.AutoDelete ? 1 : 0,
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
            var sql = $"DELETE FROM {_tableName} WHERE Id = @Id";
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
            Id = (string)row.Id,
            DataCategory = (string)row.DataCategory,
            RetentionPeriodTicks = (long)row.RetentionPeriodTicks,
            AutoDelete = Convert.ToInt32(row.AutoDelete) != 0,
            Reason = row.Reason is null or DBNull ? null : (string)row.Reason,
            LegalBasis = row.LegalBasis is null or DBNull ? null : (string)row.LegalBasis,
            PolicyTypeValue = Convert.ToInt32(row.PolicyTypeValue),
            CreatedAtUtc = new DateTimeOffset((DateTime)row.CreatedAtUtc, TimeSpan.Zero),
            LastModifiedAtUtc = row.LastModifiedAtUtc is null or DBNull
                ? null
                : new DateTimeOffset((DateTime)row.LastModifiedAtUtc, TimeSpan.Zero)
        };
    }
}
