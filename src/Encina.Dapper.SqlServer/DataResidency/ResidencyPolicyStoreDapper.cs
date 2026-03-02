using System.Data;

using Dapper;

using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;
using Encina.Messaging;

using LanguageExt;

using static LanguageExt.Prelude;

namespace Encina.Dapper.SqlServer.DataResidency;

/// <summary>
/// Dapper implementation of <see cref="IResidencyPolicyStore"/> for SQL Server.
/// Provides CRUD operations for residency policy descriptor lifecycle management.
/// </summary>
/// <remarks>
/// <para>
/// Per GDPR Article 44 (general principle for transfers), any transfer of personal data
/// to a third country shall take place only if the conditions of Chapter V (Articles 44-49)
/// are complied with. Residency policy descriptors encode these conditions as enforceable
/// rules that the <see cref="IDataResidencyPolicy"/> service evaluates at runtime.
/// </para>
/// <para>
/// SQL Server-specific considerations:
/// <list type="bullet">
/// <item><description>DateTimeOffset values are read using <c>new DateTimeOffset((DateTime)row.Column, TimeSpan.Zero)</c>.</description></item>
/// <item><description>Boolean values use native BIT support and are read as <c>(bool)row.Column</c>.</description></item>
/// <item><description>Column names use PascalCase with [bracket] identifiers.</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class ResidencyPolicyStoreDapper : IResidencyPolicyStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResidencyPolicyStoreDapper"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The residency policies table name (default: ResidencyPolicies).</param>
    /// <param name="timeProvider">The time provider for UTC time (default: <see cref="TimeProvider.System"/>).</param>
    public ResidencyPolicyStoreDapper(
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
                ([DataCategory], [AllowedRegionCodes], [RequireAdequacyDecision], [AllowedTransferBasesValue], [CreatedAtUtc], [LastModifiedAtUtc])
                VALUES
                (@DataCategory, @AllowedRegionCodes, @RequireAdequacyDecision, @AllowedTransferBasesValue, @CreatedAtUtc, @LastModifiedAtUtc)";

            await _connection.ExecuteAsync(sql, new
            {
                entity.DataCategory,
                entity.AllowedRegionCodes,
                entity.RequireAdequacyDecision,
                entity.AllowedTransferBasesValue,
                entity.CreatedAtUtc,
                entity.LastModifiedAtUtc
            });

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
    public async ValueTask<Either<EncinaError, Option<ResidencyPolicyDescriptor>>> GetByCategoryAsync(
        string dataCategory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataCategory);

        try
        {
            var sql = $@"
                SELECT [DataCategory], [AllowedRegionCodes], [RequireAdequacyDecision], [AllowedTransferBasesValue], [CreatedAtUtc], [LastModifiedAtUtc]
                FROM {_tableName}
                WHERE [DataCategory] = @DataCategory";

            var rows = await _connection.QueryAsync(sql, new { DataCategory = dataCategory });
            var row = rows.FirstOrDefault();

            if (row is null)
            {
                return Right<EncinaError, Option<ResidencyPolicyDescriptor>>(None);
            }

            var entity = MapToEntity(row);
            var domain = ResidencyPolicyMapper.ToDomain(entity);

            return domain is null
                ? Right<EncinaError, Option<ResidencyPolicyDescriptor>>(None)
                : Right<EncinaError, Option<ResidencyPolicyDescriptor>>(Some(domain));
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
    public async ValueTask<Either<EncinaError, IReadOnlyList<ResidencyPolicyDescriptor>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sql = $@"
                SELECT [DataCategory], [AllowedRegionCodes], [RequireAdequacyDecision], [AllowedTransferBasesValue], [CreatedAtUtc], [LastModifiedAtUtc]
                FROM {_tableName}";

            var rows = await _connection.QueryAsync(sql);
            var policies = rows
                .Select(row => ResidencyPolicyMapper.ToDomain(MapToEntity(row)))
                .Where(p => p is not null)
                .Cast<ResidencyPolicyDescriptor>()
                .ToList();

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
    public async ValueTask<Either<EncinaError, Unit>> UpdateAsync(
        ResidencyPolicyDescriptor policy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policy);

        try
        {
            var entity = ResidencyPolicyMapper.ToEntity(policy);
            var nowUtc = _timeProvider.GetUtcNow();

            var sql = $@"
                UPDATE {_tableName}
                SET [AllowedRegionCodes] = @AllowedRegionCodes,
                    [RequireAdequacyDecision] = @RequireAdequacyDecision,
                    [AllowedTransferBasesValue] = @AllowedTransferBasesValue,
                    [LastModifiedAtUtc] = @LastModifiedAtUtc
                WHERE [DataCategory] = @DataCategory";

            await _connection.ExecuteAsync(sql, new
            {
                entity.DataCategory,
                entity.AllowedRegionCodes,
                entity.RequireAdequacyDecision,
                entity.AllowedTransferBasesValue,
                LastModifiedAtUtc = nowUtc
            });

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
    public async ValueTask<Either<EncinaError, Unit>> DeleteAsync(
        string dataCategory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataCategory);

        try
        {
            var sql = $"DELETE FROM {_tableName} WHERE [DataCategory] = @DataCategory";
            await _connection.ExecuteAsync(sql, new { DataCategory = dataCategory });

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

    private static ResidencyPolicyEntity MapToEntity(dynamic row)
    {
        return new ResidencyPolicyEntity
        {
            DataCategory = (string)row.DataCategory,
            AllowedRegionCodes = (string)row.AllowedRegionCodes,
            RequireAdequacyDecision = (bool)row.RequireAdequacyDecision,
            AllowedTransferBasesValue = row.AllowedTransferBasesValue is null or DBNull ? null : (string)row.AllowedTransferBasesValue,
            CreatedAtUtc = new DateTimeOffset((DateTime)row.CreatedAtUtc, TimeSpan.Zero),
            LastModifiedAtUtc = row.LastModifiedAtUtc is null or DBNull ? null : new DateTimeOffset((DateTime)row.LastModifiedAtUtc, TimeSpan.Zero)
        };
    }
}
