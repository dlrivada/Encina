using System.Data;
using Dapper;
using Encina.DomainModeling;
using Encina.DomainModeling.Concurrency;
using Encina.Tenancy;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dapper.SqlServer.Tenancy;

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
/// <item><b>Query Filtering:</b> All queries automatically include <c>WHERE TenantId = @tenantId</c></item>
/// <item><b>Insert Assignment:</b> New entities automatically get <c>TenantId</c> set from current context</item>
/// <item><b>Modify Validation:</b> Updates/deletes validate that the entity belongs to the current tenant</item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Register tenant-aware repository
/// services.AddTenantAwareRepository&lt;Order, Guid&gt;(mapping =&gt;
///     mapping.ToTable("Orders")
///            .HasId(o =&gt; o.Id)
///            .HasTenantId(o =&gt; o.TenantId)
///            .MapProperty(o =&gt; o.Total));
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
    private readonly IRequestContext? _requestContext;
    private readonly TimeProvider _timeProvider;

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
    /// <param name="requestContext">Optional request context for audit field population.</param>
    /// <param name="timeProvider">Optional time provider for audit timestamps. Defaults to <see cref="TimeProvider.System"/>.</param>
    public TenantAwareFunctionalRepositoryDapper(
        IDbConnection connection,
        ITenantEntityMapping<TEntity, TId> mapping,
        ITenantProvider tenantProvider,
        DapperTenancyOptions options,
        IRequestContext? requestContext = null,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(mapping);
        ArgumentNullException.ThrowIfNull(tenantProvider);
        ArgumentNullException.ThrowIfNull(options);

        _connection = connection;
        _mapping = mapping;
        _tenantProvider = tenantProvider;
        _options = options;
        _requestContext = requestContext;
        _timeProvider = timeProvider ?? TimeProvider.System;

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
            var columns = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"[{c}]"));

            string sql;
            object parameters;

            if (!string.IsNullOrEmpty(tenantFilter.filter))
            {
                sql = $"SELECT {columns} FROM {_mapping.TableName} WHERE [{_mapping.IdColumnName}] = @Id AND {tenantFilter.filter}";
                parameters = new { Id = id, tenantId = tenantFilter.tenantId };
            }
            else
            {
                sql = $"SELECT {columns} FROM {_mapping.TableName} WHERE [{_mapping.IdColumnName}] = @Id";
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
            var tenantFilter = GetTenantFilter();

            string sql;
            object? parameters;

            if (!string.IsNullOrEmpty(tenantFilter.filter))
            {
                sql = $"SELECT COUNT(*) FROM {_mapping.TableName} WHERE {tenantFilter.filter}";
                parameters = new { tenantId = tenantFilter.tenantId };
            }
            else
            {
                sql = $"SELECT COUNT(*) FROM {_mapping.TableName}";
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
            var tenantFilter = GetTenantFilter();

            string sql;
            object? parameters;

            if (!string.IsNullOrEmpty(tenantFilter.filter))
            {
                sql = $"SELECT CASE WHEN EXISTS (SELECT 1 FROM {_mapping.TableName} WHERE {tenantFilter.filter}) THEN 1 ELSE 0 END";
                parameters = new { tenantId = tenantFilter.tenantId };
            }
            else
            {
                sql = $"SELECT CASE WHEN EXISTS (SELECT 1 FROM {_mapping.TableName}) THEN 1 ELSE 0 END";
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

    /// <inheritdoc/>
    public async Task<Either<EncinaError, PagedResult<TEntity>>> GetPagedAsync(
        PaginationOptions pagination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagination);

        try
        {
            var tenantFilter = GetTenantFilter();

            // Get total count
            string countSql;
            object? countParameters;

            if (!string.IsNullOrEmpty(tenantFilter.filter))
            {
                countSql = $"SELECT COUNT(*) FROM {_mapping.TableName} WHERE {tenantFilter.filter}";
                countParameters = new { tenantId = tenantFilter.tenantId };
            }
            else
            {
                countSql = $"SELECT COUNT(*) FROM {_mapping.TableName}";
                countParameters = null;
            }

            var totalCount = await _connection.ExecuteScalarAsync<int>(countSql, countParameters);

            if (totalCount == 0)
            {
                return Right<EncinaError, PagedResult<TEntity>>(
                    PagedResult<TEntity>.Empty(pagination.PageNumber, pagination.PageSize));
            }

            // Get paged data - SQL Server requires ORDER BY for OFFSET/FETCH
            var columns = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"[{c}]"));

            string sql;
            object parameters;

            if (!string.IsNullOrEmpty(tenantFilter.filter))
            {
                sql = $"SELECT {columns} FROM {_mapping.TableName} WHERE {tenantFilter.filter} ORDER BY [{_mapping.IdColumnName}] OFFSET @Skip ROWS FETCH NEXT @PageSize ROWS ONLY";
                parameters = new { tenantId = tenantFilter.tenantId, pagination.Skip, pagination.PageSize };
            }
            else
            {
                sql = $"SELECT {columns} FROM {_mapping.TableName} ORDER BY [{_mapping.IdColumnName}] OFFSET @Skip ROWS FETCH NEXT @PageSize ROWS ONLY";
                parameters = new { pagination.Skip, pagination.PageSize };
            }

            var entities = await _connection.QueryAsync<TEntity>(sql, parameters);

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
            var (whereClause, baseParameters) = _sqlBuilder.BuildWhereClause(specification);

            // Get total count with specification filter
            var countSql = $"SELECT COUNT(*) FROM {_mapping.TableName} {whereClause}";
            var totalCount = await _connection.ExecuteScalarAsync<int>(countSql, baseParameters);

            if (totalCount == 0)
            {
                return Right<EncinaError, PagedResult<TEntity>>(
                    PagedResult<TEntity>.Empty(pagination.PageNumber, pagination.PageSize));
            }

            // Get paged data with specification filter - SQL Server requires ORDER BY for OFFSET/FETCH
            var columns = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"[{c}]"));
            var sql = $"SELECT {columns} FROM {_mapping.TableName} {whereClause} ORDER BY [{_mapping.IdColumnName}] OFFSET @Skip ROWS FETCH NEXT @PageSize ROWS ONLY";

            // Combine specification parameters with pagination parameters
            var parameters = new DynamicParameters(baseParameters);
            parameters.Add("Skip", pagination.Skip);
            parameters.Add("PageSize", pagination.PageSize);

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
    public Task<Either<EncinaError, PagedResult<TEntity>>> GetPagedAsync(
        IPagedSpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        // Convert IPagedSpecification to Specification and PaginationOptions
        var baseSpecification = specification switch
        {
            Specification<TEntity> spec => spec,
            _ => throw new NotSupportedException(
                $"IPagedSpecification must also inherit from Specification<{typeof(TEntity).Name}>")
        };

        var pagination = new PaginationOptions(
            specification.Pagination.PageNumber,
            specification.Pagination.PageSize);

        return GetPagedAsync(baseSpecification, pagination, cancellationToken);
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
            // Validate tenant ownership if enabled
            var validationResult = ValidateTenantOwnership(entity);
            if (validationResult.IsLeft)
            {
                return validationResult.Map(_ => entity);
            }

            // Populate audit fields for modification
            AuditFieldPopulator.PopulateForUpdate(entity, _requestContext?.UserId, _timeProvider);

            var tenantFilter = GetTenantFilter();
            var id = _mapping.GetId(entity);

            // Handle versioned entities for optimistic concurrency
            if (entity is IVersionedEntity versionedEntity)
            {
                var originalVersion = versionedEntity.Version;
                versionedEntity.Version = (int)(originalVersion + 1);

                var versionedSql = BuildVersionedUpdateSql();

                // Build parameters combining entity properties with OriginalVersion and tenantId
                var parameters = new DynamicParameters(entity);
                parameters.Add("OriginalVersion", originalVersion);

                if (!string.IsNullOrEmpty(tenantFilter.filter) && _mapping.IsTenantEntity)
                {
                    versionedSql = $"{versionedSql} AND {tenantFilter.filter}";
                    parameters.Add("tenantId", tenantFilter.tenantId);
                }

                var rowsAffected = await _connection.ExecuteAsync(versionedSql, parameters);

                if (rowsAffected == 0)
                {
                    // Check if entity exists to distinguish NotFound from ConcurrencyConflict
                    var existsSql = !string.IsNullOrEmpty(tenantFilter.filter) && _mapping.IsTenantEntity
                        ? $"SELECT CASE WHEN EXISTS (SELECT 1 FROM {_mapping.TableName} WHERE [{_mapping.IdColumnName}] = @Id AND {tenantFilter.filter}) THEN 1 ELSE 0 END"
                        : $"SELECT CASE WHEN EXISTS (SELECT 1 FROM {_mapping.TableName} WHERE [{_mapping.IdColumnName}] = @Id) THEN 1 ELSE 0 END";

                    var existsParams = !string.IsNullOrEmpty(tenantFilter.filter) && _mapping.IsTenantEntity
                        ? new { Id = id, tenantId = tenantFilter.tenantId }
                        : (object)new { Id = id };

                    var exists = await _connection.ExecuteScalarAsync<int>(existsSql, existsParams);

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
                    return Left<EncinaError, TEntity>(RepositoryErrors.NotFound<TEntity, TId>(id));
                }

                return Right<EncinaError, TEntity>(entity);
            }
            else
            {
                var rowsAffected = await _connection.ExecuteAsync(_updateSql, entity);

                if (rowsAffected == 0)
                {
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
                sql = $"DELETE FROM {_mapping.TableName} WHERE [{_mapping.IdColumnName}] = @Id AND {tenantFilter.filter}";
                parameters = new { Id = id, tenantId = tenantFilter.tenantId };
            }
            else
            {
                sql = $"DELETE FROM {_mapping.TableName} WHERE [{_mapping.IdColumnName}] = @Id";
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
            // Auto-assign tenant ID and populate audit fields for all entities
            foreach (var entity in entityList)
            {
                AssignTenantIdIfNeeded(entity);
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
            // Validate tenant ownership and populate audit fields for all entities
            foreach (var entity in entityList)
            {
                var validationResult = ValidateTenantOwnership(entity);
                if (validationResult.IsLeft)
                {
                    return validationResult;
                }

                AuditFieldPopulator.PopulateForUpdate(entity, _requestContext?.UserId, _timeProvider);
            }

            if (hasVersionedEntities)
            {
                // Handle versioned entities with optimistic concurrency
                var versionedSql = BuildVersionedUpdateSql();
                var tenantFilter = GetTenantFilter();
                var totalUpdated = 0;

                foreach (var entity in entityList)
                {
                    if (entity is IVersionedEntity versionedEntity)
                    {
                        var originalVersion = versionedEntity.Version;
                        versionedEntity.Version = (int)(originalVersion + 1);

                        var parameters = new DynamicParameters(entity);
                        parameters.Add("OriginalVersion", originalVersion);

                        var sql = versionedSql;
                        if (!string.IsNullOrEmpty(tenantFilter.filter) && _mapping.IsTenantEntity)
                        {
                            sql = $"{versionedSql} AND {tenantFilter.filter}";
                            parameters.Add("tenantId", tenantFilter.tenantId);
                        }

                        var rowsAffected = await _connection.ExecuteAsync(sql, parameters);
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

            // Non-versioned entities - update one by one with tenant filter
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
    /// This operation is not supported in Dapper providers because they don't have change tracking.
    /// Use EF Core providers for immutable record support.
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

        return ($"[{_mapping.TenantColumnName}] = @tenantId", tenantId);
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
        var idProperty = _mapping.ColumnMappings.First(kvp => kvp.Value == _mapping.IdColumnName);

        return $"UPDATE {_mapping.TableName} SET {setClauses} WHERE [{_mapping.IdColumnName}] = @{idProperty.Key}";
    }

    private string BuildVersionedUpdateSql()
    {
        var updatableProperties = _mapping.ColumnMappings
            .Where(kvp => !_mapping.UpdateExcludedProperties.Contains(kvp.Key))
            .ToList();

        var setClauses = string.Join(", ", updatableProperties.Select(kvp => $"[{kvp.Value}] = @{kvp.Key}"));
        var idProperty = _mapping.ColumnMappings.First(kvp => kvp.Value == _mapping.IdColumnName);

        // Add version check to WHERE clause for optimistic concurrency
        return $"UPDATE {_mapping.TableName} SET {setClauses} WHERE [{_mapping.IdColumnName}] = @{idProperty.Key} AND [Version] = @OriginalVersion";
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
