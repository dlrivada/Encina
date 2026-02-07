using System.Data;
using System.Text;
using Dapper;
using Encina;
using Encina.DomainModeling;
using Encina.DomainModeling.Concurrency;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dapper.SqlServer.Repository;

/// <summary>
/// Dapper implementation of <see cref="IFunctionalRepository{TEntity, TId}"/>
/// with Railway Oriented Programming error handling for SQL Server.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TId">The entity identifier type.</typeparam>
/// <remarks>
/// <para>
/// This repository uses explicit entity-to-table mappings for generating SQL statements.
/// All SQL is parameterized to prevent injection attacks.
/// </para>
/// <para>
/// For specification-based queries, only basic predicates are supported. Complex queries
/// should use raw SQL or stored procedures.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Configure mapping
/// services.AddEncinaRepository&lt;Order, Guid&gt;(mapping =&gt;
/// {
///     mapping.ToTable("Orders")
///         .HasId(o =&gt; o.Id)
///         .MapProperty(o =&gt; o.CustomerId, "CustomerId")
///         .MapProperty(o =&gt; o.Total, "Total");
/// });
///
/// // Use repository
/// public class OrderService(IFunctionalRepository&lt;Order, Guid&gt; repository)
/// {
///     public Task&lt;Either&lt;EncinaError, Order&gt;&gt; GetOrderAsync(Guid id, CancellationToken ct)
///         =&gt; repository.GetByIdAsync(id, ct);
/// }
/// </code>
/// </example>
public sealed class FunctionalRepositoryDapper<TEntity, TId> : IFunctionalRepository<TEntity, TId>
    where TEntity : class
    where TId : notnull
{
    private readonly IDbConnection _connection;
    private readonly IEntityMapping<TEntity, TId> _mapping;
    private readonly SpecificationSqlBuilder<TEntity> _sqlBuilder;
    private readonly IRequestContext? _requestContext;
    private readonly TimeProvider _timeProvider;

    // Cached SQL statements
    private readonly string _selectByIdSql;
    private readonly string _selectAllSql;
    private readonly string _insertSql;
    private readonly string _updateSql;
    private readonly string _deleteByIdSql;
    private readonly string _countSql;
    private readonly string _existsSql;

    /// <summary>
    /// Initializes a new instance of the <see cref="FunctionalRepositoryDapper{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="mapping">The entity mapping configuration.</param>
    /// <param name="requestContext">Optional request context for audit field population.</param>
    /// <param name="timeProvider">Optional time provider for audit timestamps. Defaults to <see cref="TimeProvider.System"/>.</param>
    public FunctionalRepositoryDapper(
        IDbConnection connection,
        IEntityMapping<TEntity, TId> mapping,
        IRequestContext? requestContext = null,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(mapping);

        _connection = connection;
        _mapping = mapping;
        _requestContext = requestContext;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _sqlBuilder = new SpecificationSqlBuilder<TEntity>(mapping.ColumnMappings);

        // Pre-build SQL statements
        _selectByIdSql = BuildSelectByIdSql();
        _selectAllSql = BuildSelectAllSql();
        _insertSql = BuildInsertSql();
        _updateSql = BuildUpdateSql();
        _deleteByIdSql = BuildDeleteByIdSql();
        _countSql = BuildCountSql();
        _existsSql = BuildExistsSql();
    }

    #region Read Operations

    /// <inheritdoc/>
    public async Task<Either<EncinaError, TEntity>> GetByIdAsync(
        TId id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _connection.QuerySingleOrDefaultAsync<TEntity>(
                _selectByIdSql,
                new { Id = id });

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
            var entities = await _connection.QueryAsync<TEntity>(_selectAllSql);
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
            var columns = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"[{c}]"));
            var sql = $"SELECT TOP 1 {columns} FROM {_mapping.TableName} {whereClause}";

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
            var count = await _connection.ExecuteScalarAsync<int>(_countSql);
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
            var sql = $"SELECT COUNT(*) FROM {_mapping.TableName} {whereClause}";

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
            var sql = $"SELECT CASE WHEN EXISTS (SELECT 1 FROM {_mapping.TableName} {whereClause}) THEN 1 ELSE 0 END";

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
            var exists = await _connection.ExecuteScalarAsync<int>(_existsSql);
            return Right<EncinaError, bool>(exists == 1);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, bool>(
                RepositoryErrors.PersistenceError<TEntity>("Any", ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, PagedResult<TEntity>>> GetPagedAsync(
        PaginationOptions pagination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagination);

        try
        {
            var totalCount = await _connection.ExecuteScalarAsync<int>(_countSql);

            if (totalCount == 0)
            {
                return Right<EncinaError, PagedResult<TEntity>>(
                    PagedResult<TEntity>.Empty(pagination.PageNumber, pagination.PageSize));
            }

            // SQL Server uses OFFSET...FETCH NEXT for pagination (requires ORDER BY)
            var columns = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"[{c}]"));
            var sql = $"SELECT {columns} FROM {_mapping.TableName} ORDER BY [{_mapping.IdColumnName}] OFFSET @Skip ROWS FETCH NEXT @PageSize ROWS ONLY";

            var entities = await _connection.QueryAsync<TEntity>(
                sql,
                new { pagination.PageSize, pagination.Skip });

            return Right<EncinaError, PagedResult<TEntity>>(
                new PagedResult<TEntity>(entities.ToList(), pagination.PageNumber, pagination.PageSize, totalCount));
        }
        catch (Exception ex)
        {
            return Left<EncinaError, PagedResult<TEntity>>(
                RepositoryErrors.PersistenceError<TEntity>("GetPaged", ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, PagedResult<TEntity>>> GetPagedAsync(
        Specification<TEntity> specification,
        PaginationOptions pagination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);
        ArgumentNullException.ThrowIfNull(pagination);

        try
        {
            // Get count with specification filter
            var (whereClause, specParameters) = _sqlBuilder.BuildWhereClause(specification);
            var countSql = $"SELECT COUNT(*) FROM {_mapping.TableName} {whereClause}";
            var totalCount = await _connection.ExecuteScalarAsync<int>(countSql, specParameters);

            if (totalCount == 0)
            {
                return Right<EncinaError, PagedResult<TEntity>>(
                    PagedResult<TEntity>.Empty(pagination.PageNumber, pagination.PageSize));
            }

            // Build paginated query - SQL Server requires ORDER BY for OFFSET/FETCH
            var columns = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"[{c}]"));
            var sql = $"SELECT {columns} FROM {_mapping.TableName} {whereClause} ORDER BY [{_mapping.IdColumnName}] OFFSET @Skip ROWS FETCH NEXT @PageSize ROWS ONLY";

            // Merge pagination parameters with specification parameters
            var parameters = new DynamicParameters(specParameters);
            parameters.Add("PageSize", pagination.PageSize);
            parameters.Add("Skip", pagination.Skip);

            var entities = await _connection.QueryAsync<TEntity>(sql, parameters);

            return Right<EncinaError, PagedResult<TEntity>>(
                new PagedResult<TEntity>(entities.ToList(), pagination.PageNumber, pagination.PageSize, totalCount));
        }
        catch (NotSupportedException ex)
        {
            return Left<EncinaError, PagedResult<TEntity>>(
                RepositoryErrors.InvalidOperation<TEntity>("GetPaged", $"Specification not supported: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return Left<EncinaError, PagedResult<TEntity>>(
                RepositoryErrors.PersistenceError<TEntity>("GetPaged", ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, PagedResult<TEntity>>> GetPagedAsync(
        IPagedSpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        // IPagedSpecification implementations should inherit from Specification<T>
        // through PagedQuerySpecification<T>
        if (specification is not Specification<TEntity> spec)
        {
            return Left<EncinaError, PagedResult<TEntity>>(
                RepositoryErrors.InvalidOperation<TEntity>(
                    "GetPaged",
                    $"The specification {specification.GetType().Name} must inherit from Specification<{typeof(TEntity).Name}> or PagedQuerySpecification<{typeof(TEntity).Name}>."));
        }

        return await GetPagedAsync(spec, specification.Pagination, cancellationToken).ConfigureAwait(false);
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
            // Populate audit fields for creation
            AuditFieldPopulator.PopulateForCreate(entity, _requestContext?.UserId, _timeProvider);

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
            // Populate audit fields for modification
            AuditFieldPopulator.PopulateForUpdate(entity, _requestContext?.UserId, _timeProvider);

            var id = _mapping.GetId(entity);
            int rowsAffected;

            // Handle versioned entities for optimistic concurrency
            if (entity is IVersionedEntity versionedEntity)
            {
                var originalVersion = versionedEntity.Version;
                versionedEntity.Version = (int)(originalVersion + 1);

                // Build parameters combining entity properties with original version
                var parameters = new DynamicParameters(entity);
                parameters.Add("OriginalVersion", originalVersion);

                var versionedSql = BuildVersionedUpdateSql();
                rowsAffected = await _connection.ExecuteAsync(versionedSql, parameters);

                if (rowsAffected == 0)
                {
                    // Check if entity exists to distinguish NotFound from ConcurrencyConflict
                    var exists = await _connection.ExecuteScalarAsync<int>(
                        $"SELECT CASE WHEN EXISTS (SELECT 1 FROM {_mapping.TableName} WHERE [{_mapping.IdColumnName}] = @Id) THEN 1 ELSE 0 END",
                        new { Id = id });

                    if (exists == 1)
                    {
                        // Entity exists but version mismatch - concurrency conflict
                        return Left<EncinaError, TEntity>(
                            RepositoryErrors.ConcurrencyConflict<TEntity>(
                                new ConcurrencyConflictInfo<TEntity>(entity, entity, default)));
                    }

                    return Left<EncinaError, TEntity>(RepositoryErrors.NotFound<TEntity, TId>(id));
                }

                return Right<EncinaError, TEntity>(entity);
            }

            // Non-versioned entity - use standard update
            rowsAffected = await _connection.ExecuteAsync(_updateSql, entity);

            if (rowsAffected == 0)
            {
                return Left<EncinaError, TEntity>(RepositoryErrors.NotFound<TEntity, TId>(id));
            }

            return Right<EncinaError, TEntity>(entity);
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
            var rowsAffected = await _connection.ExecuteAsync(_deleteByIdSql, new { Id = id });

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
            // Populate audit fields for creation on all entities
            foreach (var entity in entityList)
            {
                AuditFieldPopulator.PopulateForCreate(entity, _requestContext?.UserId, _timeProvider);
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
        if (entityList.Count == 0)
            return Right<EncinaError, Unit>(Unit.Default);

        // Check if we're dealing with versioned entities
        var hasVersionedEntities = entityList[0] is IVersioned;

        try
        {
            // Populate audit fields for modification on all entities
            foreach (var entity in entityList)
            {
                AuditFieldPopulator.PopulateForUpdate(entity, _requestContext?.UserId, _timeProvider);
            }

            if (hasVersionedEntities)
            {
                // Handle versioned entities with optimistic concurrency
                var versionedSql = BuildVersionedUpdateSql();
                var totalUpdated = 0;

                foreach (var entity in entityList)
                {
                    if (entity is IVersionedEntity versionedEntity)
                    {
                        var originalVersion = versionedEntity.Version;
                        versionedEntity.Version = (int)(originalVersion + 1);

                        var parameters = new DynamicParameters(entity);
                        parameters.Add("OriginalVersion", originalVersion);

                        var rowsAffected = await _connection.ExecuteAsync(versionedSql, parameters);
                        totalUpdated += rowsAffected;
                    }
                }

                // Check for concurrency conflicts
                if (totalUpdated < entityList.Count)
                {
                    var conflictCount = entityList.Count - totalUpdated;
                    return Left<EncinaError, Unit>(
                        RepositoryErrors.ConcurrencyConflict<TEntity>(
                            new InvalidOperationException($"{conflictCount} entities had version conflicts")));
                }

                return Right<EncinaError, Unit>(Unit.Default);
            }

            // Non-versioned entities - use standard bulk update
            await _connection.ExecuteAsync(_updateSql, entityList);
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

            var sql = $"DELETE FROM {_mapping.TableName} {whereClause}";
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

    /// <inheritdoc/>
    /// <remarks>
    /// This operation is not supported for Dapper providers because they lack change tracking.
    /// Use <see cref="ImmutableAggregateHelper.PrepareForUpdate{TAggregate}"/> followed by the
    /// standard <c>UpdateAsync</c> method instead.
    /// </remarks>
    public Task<Either<EncinaError, Unit>> UpdateImmutableAsync(
        TEntity modified,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(modified);

        return Task.FromResult<Either<EncinaError, Unit>>(
            RepositoryErrors.OperationNotSupported<TEntity>("UpdateImmutableAsync"));
    }

    #endregion

    #region SQL Generation

    private string BuildSelectByIdSql()
    {
        var columns = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"[{c}]"));
        return $"SELECT {columns} FROM {_mapping.TableName} WHERE [{_mapping.IdColumnName}] = @Id";
    }

    private string BuildSelectAllSql()
    {
        var columns = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"[{c}]"));
        return $"SELECT {columns} FROM {_mapping.TableName}";
    }

    private string BuildInsertSql()
    {
        var insertableProperties = _mapping.ColumnMappings
            .Where(kvp => !_mapping.InsertExcludedProperties.Contains(kvp.Key))
            .ToList();

        var columns = string.Join(", ", insertableProperties.Select(kvp => $"[{kvp.Value}]"));
        var parameters = string.Join(", ", insertableProperties.Select(kvp => $"@{kvp.Key}"));

        return $"INSERT INTO {_mapping.TableName} ({columns}) VALUES ({parameters})";
    }

    private string BuildUpdateSql()
    {
        var updatableProperties = _mapping.ColumnMappings
            .Where(kvp => !_mapping.UpdateExcludedProperties.Contains(kvp.Key))
            .ToList();

        var setClauses = string.Join(", ", updatableProperties.Select(kvp => $"[{kvp.Value}] = @{kvp.Key}"));

        // Find the ID property name for the WHERE clause
        var idProperty = _mapping.ColumnMappings.First(kvp => kvp.Value == _mapping.IdColumnName);

        return $"UPDATE {_mapping.TableName} SET {setClauses} WHERE [{_mapping.IdColumnName}] = @{idProperty.Key}";
    }

    private string BuildVersionedUpdateSql()
    {
        var updatableProperties = _mapping.ColumnMappings
            .Where(kvp => !_mapping.UpdateExcludedProperties.Contains(kvp.Key))
            .ToList();

        var setClauses = string.Join(", ", updatableProperties.Select(kvp => $"[{kvp.Value}] = @{kvp.Key}"));

        // Find the ID property name for the WHERE clause
        var idProperty = _mapping.ColumnMappings.First(kvp => kvp.Value == _mapping.IdColumnName);

        // Add version check to WHERE clause for optimistic concurrency
        return $"UPDATE {_mapping.TableName} SET {setClauses} WHERE [{_mapping.IdColumnName}] = @{idProperty.Key} AND [Version] = @OriginalVersion";
    }

    private string BuildDeleteByIdSql()
    {
        return $"DELETE FROM {_mapping.TableName} WHERE [{_mapping.IdColumnName}] = @Id";
    }

    private string BuildCountSql()
    {
        return $"SELECT COUNT(*) FROM {_mapping.TableName}";
    }

    private string BuildExistsSql()
    {
        return $"SELECT CASE WHEN EXISTS (SELECT 1 FROM {_mapping.TableName}) THEN 1 ELSE 0 END";
    }

    private static bool IsDuplicateKeyException(Exception ex)
    {
        var message = ex.Message;
        return message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
            || message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase)
            || message.Contains("PRIMARY KEY constraint", StringComparison.OrdinalIgnoreCase)
            || message.Contains("violation of PRIMARY KEY", StringComparison.OrdinalIgnoreCase)
            || message.Contains("Cannot insert duplicate key", StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
