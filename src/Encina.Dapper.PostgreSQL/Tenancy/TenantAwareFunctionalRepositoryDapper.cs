using System.Data;
using Dapper;
using Encina.DomainModeling;
using Encina.Tenancy;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dapper.PostgreSQL.Tenancy;

/// <summary>
/// Tenant-aware Dapper implementation of <see cref="IFunctionalRepository{TEntity, TId}"/>
/// with automatic tenant filtering, assignment, and validation.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TId">The entity identifier type.</typeparam>
/// <remarks>
/// <para>
/// This repository extends the standard Dapper repository with multi-tenancy support:
/// </para>
/// <list type="bullet">
/// <item><b>Query Filtering:</b> All queries automatically include <c>WHERE "TenantId" = @tenantId</c></item>
/// <item><b>Insert Assignment:</b> New entities automatically get <c>TenantId</c> set from current context</item>
/// <item><b>Modify Validation:</b> Updates/deletes validate that the entity belongs to the current tenant</item>
/// </list>
/// <para>
/// <b>PostgreSQL-specific syntax:</b>
/// Uses double-quoted identifiers, LIMIT/OFFSET for pagination, and PostgreSQL error codes for duplicate key detection.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register tenant-aware repository
/// services.AddTenantAwareRepository&lt;Order, Guid&gt;(mapping =&gt;
///     mapping.ToTable("orders")
///            .HasId(o =&gt; o.Id, "id")
///            .HasTenantId(o =&gt; o.TenantId, "tenant_id")
///            .MapProperty(o =&gt; o.Total, "total"));
///
/// // Use in service
/// public class OrderService(IFunctionalRepository&lt;Order, Guid&gt; repository)
/// {
///     // All operations are automatically tenant-scoped
///     public Task&lt;Either&lt;EncinaError, IReadOnlyList&lt;Order&gt;&gt;&gt; GetOrdersAsync(CancellationToken ct)
///         =&gt; repository.ListAsync(ct);
/// }
/// </code>
/// </example>
public sealed class TenantAwareFunctionalRepositoryDapper<TEntity, TId> : IFunctionalRepository<TEntity, TId>
    where TEntity : class
    where TId : notnull
{
    private readonly IDbConnection _connection;
    private readonly ITenantEntityMapping<TEntity, TId> _mapping;
    private readonly ITenantProvider _tenantProvider;
    private readonly DapperTenancyOptions _options;
    private readonly TenantAwareSpecificationSqlBuilder<TEntity> _sqlBuilder;

    // Cached SQL statements
    private readonly string _insertSql;
    private readonly string _updateSql;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantAwareFunctionalRepositoryDapper{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="mapping">The tenant-aware entity mapping.</param>
    /// <param name="tenantProvider">The tenant provider for current tenant context.</param>
    /// <param name="options">The Dapper tenancy options.</param>
    public TenantAwareFunctionalRepositoryDapper(
        IDbConnection connection,
        ITenantEntityMapping<TEntity, TId> mapping,
        ITenantProvider tenantProvider,
        DapperTenancyOptions options)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(mapping);
        ArgumentNullException.ThrowIfNull(tenantProvider);
        ArgumentNullException.ThrowIfNull(options);

        _connection = connection;
        _mapping = mapping;
        _tenantProvider = tenantProvider;
        _options = options;

        // Create a generic mapping interface for the SQL builder
        var genericMapping = new GenericTenantMappingAdapter<TEntity, TId>(mapping);
        _sqlBuilder = new TenantAwareSpecificationSqlBuilder<TEntity>(genericMapping, tenantProvider, options);

        // Pre-build SQL statements
        _insertSql = BuildInsertSql();
        _updateSql = BuildUpdateSql();
    }

    #region Read Operations

    /// <inheritdoc/>
    public async Task<Either<EncinaError, TEntity>> GetByIdAsync(
        TId id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tenantFilter = GetTenantFilter();
            var columns = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"\"{c}\""));

            string sql;
            object parameters;

            if (!string.IsNullOrEmpty(tenantFilter.filter))
            {
                sql = $"SELECT {columns} FROM \"{_mapping.TableName}\" WHERE \"{_mapping.IdColumnName}\" = @Id AND {tenantFilter.filter}";
                parameters = new { Id = id, tenantId = tenantFilter.tenantId };
            }
            else
            {
                sql = $"SELECT {columns} FROM \"{_mapping.TableName}\" WHERE \"{_mapping.IdColumnName}\" = @Id";
                parameters = new { Id = id };
            }

            var entity = await _connection.QuerySingleOrDefaultAsync<TEntity>(sql, parameters);

            return entity is not null
                ? Right<EncinaError, TEntity>(entity)
                : Left<EncinaError, TEntity>(RepositoryErrors.NotFound<TEntity, TId>(id));
        }
        catch (Exception ex)
        {
            return Left<EncinaError, TEntity>(
                RepositoryErrors.PersistenceError<TEntity, TId>(id, "GetById", ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, IReadOnlyList<TEntity>>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (sql, parameters) = _sqlBuilder.BuildSelectStatement(_mapping.TableName);
            var entities = await _connection.QueryAsync<TEntity>(sql, parameters);
            return Right<EncinaError, IReadOnlyList<TEntity>>(entities.ToList());
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<TEntity>>(
                RepositoryErrors.PersistenceError<TEntity>("List", ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, IReadOnlyList<TEntity>>> ListAsync(
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        try
        {
            var (sql, parameters) = _sqlBuilder.BuildSelectStatement(_mapping.TableName, specification);
            var entities = await _connection.QueryAsync<TEntity>(sql, parameters);
            return Right<EncinaError, IReadOnlyList<TEntity>>(entities.ToList());
        }
        catch (NotSupportedException ex)
        {
            return Left<EncinaError, IReadOnlyList<TEntity>>(
                RepositoryErrors.InvalidOperation<TEntity>("List", $"Specification not supported: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<TEntity>>(
                RepositoryErrors.PersistenceError<TEntity>("List", ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, TEntity>> FirstOrDefaultAsync(
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        try
        {
            var (whereClause, parameters) = _sqlBuilder.BuildWhereClause(specification);
            var columns = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"\"{c}\""));
            // PostgreSQL uses LIMIT 1 instead of TOP 1
            var sql = $"SELECT {columns} FROM \"{_mapping.TableName}\" {whereClause} LIMIT 1";

            var entity = await _connection.QuerySingleOrDefaultAsync<TEntity>(sql, parameters);

            return entity is not null
                ? Right<EncinaError, TEntity>(entity)
                : Left<EncinaError, TEntity>(
                    RepositoryErrors.NotFound<TEntity>($"specification: {specification.GetType().Name}"));
        }
        catch (NotSupportedException ex)
        {
            return Left<EncinaError, TEntity>(
                RepositoryErrors.InvalidOperation<TEntity>("FirstOrDefault", $"Specification not supported: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return Left<EncinaError, TEntity>(
                RepositoryErrors.PersistenceError<TEntity>("FirstOrDefault", ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, int>> CountAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tenantFilter = GetTenantFilter();

            string sql;
            object? parameters;

            if (!string.IsNullOrEmpty(tenantFilter.filter))
            {
                sql = $"SELECT COUNT(*) FROM \"{_mapping.TableName}\" WHERE {tenantFilter.filter}";
                parameters = new { tenantId = tenantFilter.tenantId };
            }
            else
            {
                sql = $"SELECT COUNT(*) FROM \"{_mapping.TableName}\"";
                parameters = null;
            }

            var count = await _connection.ExecuteScalarAsync<int>(sql, parameters);
            return Right<EncinaError, int>(count);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, int>(
                RepositoryErrors.PersistenceError<TEntity>("Count", ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, int>> CountAsync(
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        try
        {
            var (whereClause, parameters) = _sqlBuilder.BuildWhereClause(specification);
            var sql = $"SELECT COUNT(*) FROM \"{_mapping.TableName}\" {whereClause}";

            var count = await _connection.ExecuteScalarAsync<int>(sql, parameters);
            return Right<EncinaError, int>(count);
        }
        catch (NotSupportedException ex)
        {
            return Left<EncinaError, int>(
                RepositoryErrors.InvalidOperation<TEntity>("Count", $"Specification not supported: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return Left<EncinaError, int>(
                RepositoryErrors.PersistenceError<TEntity>("Count", ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, bool>> AnyAsync(
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        try
        {
            var (whereClause, parameters) = _sqlBuilder.BuildWhereClause(specification);
            var sql = $"SELECT CASE WHEN EXISTS (SELECT 1 FROM \"{_mapping.TableName}\" {whereClause}) THEN 1 ELSE 0 END";

            var exists = await _connection.ExecuteScalarAsync<int>(sql, parameters);
            return Right<EncinaError, bool>(exists == 1);
        }
        catch (NotSupportedException ex)
        {
            return Left<EncinaError, bool>(
                RepositoryErrors.InvalidOperation<TEntity>("Any", $"Specification not supported: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return Left<EncinaError, bool>(
                RepositoryErrors.PersistenceError<TEntity>("Any", ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, bool>> AnyAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tenantFilter = GetTenantFilter();

            string sql;
            object? parameters;

            if (!string.IsNullOrEmpty(tenantFilter.filter))
            {
                sql = $"SELECT CASE WHEN EXISTS (SELECT 1 FROM \"{_mapping.TableName}\" WHERE {tenantFilter.filter}) THEN 1 ELSE 0 END";
                parameters = new { tenantId = tenantFilter.tenantId };
            }
            else
            {
                sql = $"SELECT CASE WHEN EXISTS (SELECT 1 FROM \"{_mapping.TableName}\") THEN 1 ELSE 0 END";
                parameters = null;
            }

            var exists = await _connection.ExecuteScalarAsync<int>(sql, parameters);
            return Right<EncinaError, bool>(exists == 1);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, bool>(
                RepositoryErrors.PersistenceError<TEntity>("Any", ex));
        }
    }

    #endregion

    #region Write Operations

    /// <inheritdoc/>
    public async Task<Either<EncinaError, TEntity>> AddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        try
        {
            // Auto-assign tenant ID if enabled
            AssignTenantIdIfNeeded(entity);

            await _connection.ExecuteAsync(_insertSql, entity);
            return Right<EncinaError, TEntity>(entity);
        }
        catch (Exception ex) when (IsDuplicateKeyException(ex))
        {
            var id = _mapping.GetId(entity);
            return Left<EncinaError, TEntity>(RepositoryErrors.AlreadyExists<TEntity, TId>(id));
        }
        catch (Exception ex)
        {
            return Left<EncinaError, TEntity>(
                RepositoryErrors.PersistenceError<TEntity>("Add", ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, TEntity>> UpdateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        try
        {
            // Validate tenant ownership if enabled
            var validationResult = ValidateTenantOwnership(entity);
            if (validationResult.IsLeft)
            {
                return validationResult.Map(_ => entity);
            }

            var tenantFilter = GetTenantFilter();

            // Build UPDATE with tenant filter
            if (!string.IsNullOrEmpty(tenantFilter.filter) && _mapping.IsTenantEntity)
            {
                var sql = $"{_updateSql} AND {tenantFilter.filter}";

                // Use DynamicParameters to combine entity properties with tenantId
                var parameters = new DynamicParameters(entity);
                parameters.Add("tenantId", tenantFilter.tenantId);

                var rowsAffected = await _connection.ExecuteAsync(sql, parameters);

                if (rowsAffected == 0)
                {
                    var id = _mapping.GetId(entity);
                    return Left<EncinaError, TEntity>(RepositoryErrors.NotFound<TEntity, TId>(id));
                }

                return Right<EncinaError, TEntity>(entity);
            }
            else
            {
                var rowsAffected = await _connection.ExecuteAsync(_updateSql, entity);

                if (rowsAffected == 0)
                {
                    var id = _mapping.GetId(entity);
                    return Left<EncinaError, TEntity>(RepositoryErrors.NotFound<TEntity, TId>(id));
                }

                return Right<EncinaError, TEntity>(entity);
            }
        }
        catch (Exception ex)
        {
            return Left<EncinaError, TEntity>(
                RepositoryErrors.PersistenceError<TEntity>("Update", ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, Unit>> DeleteAsync(
        TId id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tenantFilter = GetTenantFilter();

            string sql;
            object parameters;

            if (!string.IsNullOrEmpty(tenantFilter.filter))
            {
                sql = $"DELETE FROM \"{_mapping.TableName}\" WHERE \"{_mapping.IdColumnName}\" = @Id AND {tenantFilter.filter}";
                parameters = new { Id = id, tenantId = tenantFilter.tenantId };
            }
            else
            {
                sql = $"DELETE FROM \"{_mapping.TableName}\" WHERE \"{_mapping.IdColumnName}\" = @Id";
                parameters = new { Id = id };
            }

            var rowsAffected = await _connection.ExecuteAsync(sql, parameters);

            if (rowsAffected == 0)
            {
                return Left<EncinaError, Unit>(RepositoryErrors.NotFound<TEntity, TId>(id));
            }

            return Right<EncinaError, Unit>(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, Unit>(
                RepositoryErrors.PersistenceError<TEntity, TId>(id, "Delete", ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, Unit>> DeleteAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        // Validate tenant ownership if enabled
        var validationResult = ValidateTenantOwnership(entity);
        if (validationResult.IsLeft)
        {
            return validationResult;
        }

        var id = _mapping.GetId(entity);
        return await DeleteAsync(id, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, IReadOnlyList<TEntity>>> AddRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);

        var entityList = entities.ToList();

        try
        {
            // Auto-assign tenant ID to all entities if enabled
            foreach (var entity in entityList)
            {
                AssignTenantIdIfNeeded(entity);
            }

            await _connection.ExecuteAsync(_insertSql, entityList);
            return Right<EncinaError, IReadOnlyList<TEntity>>(entityList);
        }
        catch (Exception ex) when (IsDuplicateKeyException(ex))
        {
            return Left<EncinaError, IReadOnlyList<TEntity>>(
                RepositoryErrors.AlreadyExists<TEntity>("One or more entities already exist"));
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<TEntity>>(
                RepositoryErrors.PersistenceError<TEntity>("AddRange", ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, Unit>> UpdateRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);

        var entityList = entities.ToList();

        try
        {
            // Validate tenant ownership for all entities if enabled
            foreach (var entity in entityList)
            {
                var validationResult = ValidateTenantOwnership(entity);
                if (validationResult.IsLeft)
                {
                    return validationResult;
                }
            }

            // For simplicity, we update one by one with tenant filter
            // In a production scenario, you might want to batch this
            foreach (var entity in entityList)
            {
                var updateResult = await UpdateAsync(entity, cancellationToken);
                if (updateResult.IsLeft)
                {
                    return updateResult.Map(_ => Unit.Default);
                }
            }

            return Right<EncinaError, Unit>(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, Unit>(
                RepositoryErrors.PersistenceError<TEntity>("UpdateRange", ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, int>> DeleteRangeAsync(
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        try
        {
            var (whereClause, parameters) = _sqlBuilder.BuildWhereClause(specification);

            if (string.IsNullOrWhiteSpace(whereClause))
            {
                return Left<EncinaError, int>(
                    RepositoryErrors.InvalidOperation<TEntity>("DeleteRange", "DELETE requires a WHERE clause to prevent accidental data loss."));
            }

            var sql = $"DELETE FROM \"{_mapping.TableName}\" {whereClause}";
            var deletedCount = await _connection.ExecuteAsync(sql, parameters);

            return Right<EncinaError, int>(deletedCount);
        }
        catch (NotSupportedException ex)
        {
            return Left<EncinaError, int>(
                RepositoryErrors.InvalidOperation<TEntity>("DeleteRange", $"Specification not supported: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return Left<EncinaError, int>(
                RepositoryErrors.PersistenceError<TEntity>("DeleteRange", ex));
        }
    }

    #endregion

    #region Helper Methods

    private (string? filter, string? tenantId) GetTenantFilter()
    {
        if (!_mapping.IsTenantEntity || !_options.AutoFilterTenantQueries)
        {
            return (null, null);
        }

        var tenantId = _tenantProvider.GetCurrentTenantId();

        if (string.IsNullOrEmpty(tenantId))
        {
            if (_options.ThrowOnMissingTenantContext)
            {
                throw new InvalidOperationException(
                    $"Cannot execute query on tenant entity {typeof(TEntity).Name} without tenant context.");
            }

            return (null, null);
        }

        // PostgreSQL uses double-quoted identifiers
        return ($"\"{_mapping.TenantColumnName}\" = @tenantId", tenantId);
    }

    private void AssignTenantIdIfNeeded(TEntity entity)
    {
        if (!_mapping.IsTenantEntity || !_options.AutoAssignTenantId)
        {
            return;
        }

        var tenantId = _tenantProvider.GetCurrentTenantId();

        if (string.IsNullOrEmpty(tenantId))
        {
            if (_options.ThrowOnMissingTenantContext)
            {
                throw new InvalidOperationException(
                    $"Cannot add tenant entity {typeof(TEntity).Name} without tenant context.");
            }

            return;
        }

        _mapping.SetTenantId(entity, tenantId);
    }

    private Either<EncinaError, Unit> ValidateTenantOwnership(TEntity entity)
    {
        if (!_mapping.IsTenantEntity || !_options.ValidateTenantOnModify)
        {
            return Right<EncinaError, Unit>(Unit.Default);
        }

        var currentTenantId = _tenantProvider.GetCurrentTenantId();
        var entityTenantId = _mapping.GetTenantId(entity);

        if (string.IsNullOrEmpty(currentTenantId))
        {
            if (_options.ThrowOnMissingTenantContext)
            {
                return Left<EncinaError, Unit>(
                    RepositoryErrors.InvalidOperation<TEntity>(
                        "Modify",
                        $"Cannot modify tenant entity {typeof(TEntity).Name} without tenant context."));
            }

            return Right<EncinaError, Unit>(Unit.Default);
        }

        if (!string.Equals(currentTenantId, entityTenantId, StringComparison.Ordinal))
        {
            return Left<EncinaError, Unit>(
                RepositoryErrors.InvalidOperation<TEntity>(
                    "Modify",
                    $"Tenant mismatch: entity belongs to tenant '{entityTenantId}' but current tenant is '{currentTenantId}'."));
        }

        return Right<EncinaError, Unit>(Unit.Default);
    }

    private string BuildInsertSql()
    {
        var insertableProperties = _mapping.ColumnMappings
            .Where(kvp => !_mapping.InsertExcludedProperties.Contains(kvp.Key))
            .ToList();

        // PostgreSQL uses double-quoted identifiers
        var columns = string.Join(", ", insertableProperties.Select(kvp => $"\"{kvp.Value}\""));
        var parameters = string.Join(", ", insertableProperties.Select(kvp => $"@{kvp.Key}"));

        return $"INSERT INTO \"{_mapping.TableName}\" ({columns}) VALUES ({parameters})";
    }

    private string BuildUpdateSql()
    {
        var updatableProperties = _mapping.ColumnMappings
            .Where(kvp => !_mapping.UpdateExcludedProperties.Contains(kvp.Key))
            .ToList();

        // PostgreSQL uses double-quoted identifiers
        var setClauses = string.Join(", ", updatableProperties.Select(kvp => $"\"{kvp.Value}\" = @{kvp.Key}"));
        var idProperty = _mapping.ColumnMappings.First(kvp => kvp.Value == _mapping.IdColumnName);

        return $"UPDATE \"{_mapping.TableName}\" SET {setClauses} WHERE \"{_mapping.IdColumnName}\" = @{idProperty.Key}";
    }

    private static bool IsDuplicateKeyException(Exception ex)
    {
        var message = ex.Message;
        // PostgreSQL specific error messages and error code 23505
        return message.Contains("duplicate key value violates unique constraint", StringComparison.OrdinalIgnoreCase)
            || message.Contains("23505", StringComparison.OrdinalIgnoreCase)
            || message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}

/// <summary>
/// Adapter to convert generic TId mapping to object-based mapping for SQL builder.
/// </summary>
internal sealed class GenericTenantMappingAdapter<TEntity, TId> : ITenantEntityMapping<TEntity, object>
    where TEntity : class
    where TId : notnull
{
    private readonly ITenantEntityMapping<TEntity, TId> _innerMapping;

    public GenericTenantMappingAdapter(ITenantEntityMapping<TEntity, TId> innerMapping)
    {
        _innerMapping = innerMapping;
    }

    public string TableName => _innerMapping.TableName;
    public string IdColumnName => _innerMapping.IdColumnName;
    public IReadOnlyDictionary<string, string> ColumnMappings => _innerMapping.ColumnMappings;
    public IReadOnlySet<string> InsertExcludedProperties => _innerMapping.InsertExcludedProperties;
    public IReadOnlySet<string> UpdateExcludedProperties => _innerMapping.UpdateExcludedProperties;
    public bool IsTenantEntity => _innerMapping.IsTenantEntity;
    public string? TenantColumnName => _innerMapping.TenantColumnName;
    public string? TenantPropertyName => _innerMapping.TenantPropertyName;

    public object GetId(TEntity entity) => _innerMapping.GetId(entity)!;
    public string? GetTenantId(TEntity entity) => _innerMapping.GetTenantId(entity);
    public void SetTenantId(TEntity entity, string tenantId) => _innerMapping.SetTenantId(entity, tenantId);
}
