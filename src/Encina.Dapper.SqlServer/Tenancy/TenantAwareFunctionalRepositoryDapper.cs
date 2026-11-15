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
            if (!TryUnwrap(GetTenantFilter("GetById"), out var tenantFilter, out var tenantError))
            {
                return Left<EncinaError, TEntity>(tenantError);
            }

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
            if (!TryUnwrap(_sqlBuilder.BuildSelectStatement(_mapping.TableName, "List"), out var built, out var tenantError))
            {
                return Left<EncinaError, IReadOnlyList<TEntity>>(tenantError);
            }

            var entities = await _connection.QueryAsync<TEntity>(built.Sql, built.Parameters);
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
            if (!TryUnwrap(_sqlBuilder.BuildSelectStatement(_mapping.TableName, specification, "List"), out var built, out var tenantError))
            {
                return Left<EncinaError, IReadOnlyList<TEntity>>(tenantError);
            }

            var entities = await _connection.QueryAsync<TEntity>(built.Sql, built.Parameters);
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
            if (!TryUnwrap(_sqlBuilder.BuildWhereClause(specification, "FirstOrDefault"), out var built, out var tenantError))
            {
                return Left<EncinaError, TEntity>(tenantError);
            }

            var columns = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"[{c}]"));
            var sql = $"SELECT TOP 1 {columns} FROM {_mapping.TableName} {built.WhereClause}";

            var entity = await _connection.QuerySingleOrDefaultAsync<TEntity>(sql, built.Parameters);

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
            if (!TryUnwrap(GetTenantFilter("Count"), out var tenantFilter, out var tenantError))
            {
                return Left<EncinaError, int>(tenantError);
            }

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
            if (!TryUnwrap(_sqlBuilder.BuildWhereClause(specification, "Count"), out var built, out var tenantError))
            {
                return Left<EncinaError, int>(tenantError);
            }

            var sql = $"SELECT COUNT(*) FROM {_mapping.TableName} {built.WhereClause}";

            var count = await _connection.ExecuteScalarAsync<int>(sql, built.Parameters);
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
            if (!TryUnwrap(_sqlBuilder.BuildWhereClause(specification, "Any"), out var built, out var tenantError))
            {
                return Left<EncinaError, bool>(tenantError);
            }

            var sql = $"SELECT CASE WHEN EXISTS (SELECT 1 FROM {_mapping.TableName} {built.WhereClause}) THEN 1 ELSE 0 END";

            var exists = await _connection.ExecuteScalarAsync<int>(sql, built.Parameters);
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
            if (!TryUnwrap(GetTenantFilter("Any"), out var tenantFilter, out var tenantError))
            {
                return Left<EncinaError, bool>(tenantError);
            }

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
            if (!TryUnwrap(GetTenantFilter("GetPaged"), out var tenantFilter, out var tenantError))
            {
                return Left<EncinaError, PagedResult<TEntity>>(tenantError);
            }

            var whereSuffix = string.IsNullOrEmpty(tenantFilter.filter) ? string.Empty : $" WHERE {tenantFilter.filter}";

            // Get total count
            var countSql = $"SELECT COUNT(*) FROM {_mapping.TableName}{whereSuffix}";
            var totalCount = await _connection.ExecuteScalarAsync<int>(countSql, BuildTenantParameters(tenantFilter));

            if (totalCount == 0)
            {
                return Right<EncinaError, PagedResult<TEntity>>(
                    PagedResult<TEntity>.Empty(pagination.PageNumber, pagination.PageSize));
            }

            // Get paged data - SQL Server requires ORDER BY for OFFSET/FETCH
            var columns = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"[{c}]"));
            var sql = $"SELECT {columns} FROM {_mapping.TableName}{whereSuffix} ORDER BY [{_mapping.IdColumnName}] OFFSET @Skip ROWS FETCH NEXT @PageSize ROWS ONLY";

            var parameters = BuildTenantParameters(tenantFilter);
            parameters.Add("Skip", pagination.Skip);
            parameters.Add("PageSize", pagination.PageSize);

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
            if (!TryUnwrap(_sqlBuilder.BuildWhereClause(specification, "GetPaged"), out var built, out var tenantError))
            {
                return Left<EncinaError, PagedResult<TEntity>>(tenantError);
            }

            var whereClause = built.WhereClause;
            var baseParameters = built.Parameters;

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
            if (!TryUnwrap(AssignTenantIdIfNeeded(entity, "Add"), out _, out var tenantError))
            {
                return Left<EncinaError, TEntity>(tenantError);
            }

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
            // The tenant is checked before anything is touched, even when ValidateTenantOnModify is off
            var prepared = ValidateTenantOwnership(entity, "Update")
                .Bind(_ => GetTenantFilter("Update"));
            if (!TryUnwrap(prepared, out var tenantFilter, out var tenantError))
            {
                return Left<EncinaError, TEntity>(tenantError);
            }

            // Populate audit fields for modification
            AuditFieldPopulator.PopulateForUpdate(entity, _requestContext?.UserId, _timeProvider);

            var id = _mapping.GetId(entity);

            // Handle versioned entities for optimistic concurrency
            return entity is IVersionedEntity versionedEntity
                ? await UpdateVersionedAsync(entity, versionedEntity, id, tenantFilter)
                : await UpdateUnversionedAsync(entity, id, tenantFilter);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, TEntity>(
                RepositoryErrors.PersistenceError<TEntity>("Update", ex));
        }
    }

    private async Task<Either<EncinaError, TEntity>> UpdateVersionedAsync(
        TEntity entity,
        IVersionedEntity versionedEntity,
        TId id,
        (string? filter, string? tenantId) tenantFilter)
    {
        var scoped = IsTenantScoped(tenantFilter);
        var originalVersion = versionedEntity.Version;
        versionedEntity.Version = originalVersion + 1;

        var versionedSql = BuildVersionedUpdateSql();

        // Build parameters combining entity properties with OriginalVersion and tenantId
        var parameters = new DynamicParameters(entity);
        parameters.Add("OriginalVersion", originalVersion);

        if (scoped)
        {
            versionedSql = $"{versionedSql} AND {tenantFilter.filter}";
            parameters.Add("tenantId", tenantFilter.tenantId);
        }

        var rowsAffected = await _connection.ExecuteAsync(versionedSql, parameters);

        return rowsAffected == 0
            ? await ResolveVersionedMissAsync(entity, id, tenantFilter)
            : Right<EncinaError, TEntity>(entity);
    }

    private async Task<Either<EncinaError, TEntity>> ResolveVersionedMissAsync(
        TEntity entity,
        TId id,
        (string? filter, string? tenantId) tenantFilter)
    {
        // Check if entity exists to distinguish NotFound from ConcurrencyConflict
        var scoped = IsTenantScoped(tenantFilter);
        var existsSql = scoped
            ? $"SELECT CASE WHEN EXISTS (SELECT 1 FROM {_mapping.TableName} WHERE [{_mapping.IdColumnName}] = @Id AND {tenantFilter.filter}) THEN 1 ELSE 0 END"
            : $"SELECT CASE WHEN EXISTS (SELECT 1 FROM {_mapping.TableName} WHERE [{_mapping.IdColumnName}] = @Id) THEN 1 ELSE 0 END";

        var existsParams = scoped
            ? new { Id = id, tenantId = tenantFilter.tenantId }
            : (object)new { Id = id };

        var exists = await _connection.ExecuteScalarAsync<int>(existsSql, existsParams);

        // Entity exists but version mismatch - concurrency conflict
        return exists == 1
            ? Left<EncinaError, TEntity>(
                RepositoryErrors.ConcurrencyConflict<TEntity>(
                    new ConcurrencyConflictInfo<TEntity>(entity, entity, default)))
            : Left<EncinaError, TEntity>(RepositoryErrors.NotFound<TEntity, TId>(id));
    }

    private async Task<Either<EncinaError, TEntity>> UpdateUnversionedAsync(
        TEntity entity,
        TId id,
        (string? filter, string? tenantId) tenantFilter)
    {
        var sql = _updateSql;
        object parameters = entity;

        if (IsTenantScoped(tenantFilter))
        {
            // Use DynamicParameters to combine entity properties with tenantId
            var scopedParameters = new DynamicParameters(entity);
            scopedParameters.Add("tenantId", tenantFilter.tenantId);
            sql = $"{_updateSql} AND {tenantFilter.filter}";
            parameters = scopedParameters;
        }

        var rowsAffected = await _connection.ExecuteAsync(sql, parameters);

        return rowsAffected == 0
            ? Left<EncinaError, TEntity>(RepositoryErrors.NotFound<TEntity, TId>(id))
            : Right<EncinaError, TEntity>(entity);
    }

    private bool IsTenantScoped((string? filter, string? tenantId) tenantFilter)
        => !string.IsNullOrEmpty(tenantFilter.filter) && _mapping.IsTenantEntity;

    private static DynamicParameters BuildTenantParameters((string? filter, string? tenantId) tenantFilter)
    {
        var parameters = new DynamicParameters();
        if (!string.IsNullOrEmpty(tenantFilter.filter))
        {
            parameters.Add("tenantId", tenantFilter.tenantId);
        }

        return parameters;
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, Unit>> DeleteAsync(
        TId id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!TryUnwrap(GetTenantFilter("Delete"), out var tenantFilter, out var tenantError))
            {
                return Left<EncinaError, Unit>(tenantError);
            }

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

        TId id;

        try
        {
            // Validate tenant ownership if enabled
            var validationResult = ValidateTenantOwnership(entity, "Delete");
            if (validationResult.IsLeft)
            {
                return validationResult;
            }

            id = _mapping.GetId(entity);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, Unit>(
                RepositoryErrors.PersistenceError<TEntity>("Delete", ex));
        }

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
                if (!TryUnwrap(AssignTenantIdIfNeeded(entity, "AddRange"), out _, out var tenantError))
                {
                    return Left<EncinaError, IReadOnlyList<TEntity>>(tenantError);
                }

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
            // The tenant is checked before anything is touched, even when ValidateTenantOnModify is off
            if (!TryUnwrap(GetTenantFilter("UpdateRange"), out var tenantFilter, out var tenantError))
            {
                return Left<EncinaError, Unit>(tenantError);
            }

            // Validate tenant ownership and populate audit fields for all entities
            var prepared = PrepareForUpdateRange(entityList);
            if (prepared.IsLeft)
            {
                return prepared;
            }

            return hasVersionedEntities
                ? await UpdateRangeVersionedAsync(entityList, tenantFilter)
                : await UpdateRangeOneByOneAsync(entityList, cancellationToken);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, Unit>(
                RepositoryErrors.PersistenceError<TEntity>("UpdateRange", ex));
        }
    }

    private Either<EncinaError, Unit> PrepareForUpdateRange(List<TEntity> entityList)
    {
        foreach (var entity in entityList)
        {
            var validationResult = ValidateTenantOwnership(entity, "UpdateRange");
            if (validationResult.IsLeft)
            {
                return validationResult;
            }

            AuditFieldPopulator.PopulateForUpdate(entity, _requestContext?.UserId, _timeProvider);
        }

        return Right<EncinaError, Unit>(Unit.Default);
    }

    // Handle versioned entities with optimistic concurrency
    private async Task<Either<EncinaError, Unit>> UpdateRangeVersionedAsync(
        List<TEntity> entityList,
        (string? filter, string? tenantId) tenantFilter)
    {
        var versionedSql = BuildVersionedUpdateSql();
        var scoped = IsTenantScoped(tenantFilter);
        var totalUpdated = 0;

        foreach (var versionedEntity in entityList.OfType<IVersionedEntity>())
        {
            var originalVersion = versionedEntity.Version;
            versionedEntity.Version = originalVersion + 1;

            var parameters = new DynamicParameters(versionedEntity);
            parameters.Add("OriginalVersion", originalVersion);

            var sql = versionedSql;
            if (scoped)
            {
                sql = $"{versionedSql} AND {tenantFilter.filter}";
                parameters.Add("tenantId", tenantFilter.tenantId);
            }

            totalUpdated += await _connection.ExecuteAsync(sql, parameters);
        }

        // Check for concurrency conflicts
        return totalUpdated < entityList.Count
            ? Left<EncinaError, Unit>(
                RepositoryErrors.ConcurrencyConflict<TEntity>(
                    new InvalidOperationException($"{entityList.Count - totalUpdated} entities had version conflicts")))
            : Right<EncinaError, Unit>(Unit.Default);
    }

    // Non-versioned entities - update one by one with tenant filter
    private async Task<Either<EncinaError, Unit>> UpdateRangeOneByOneAsync(
        List<TEntity> entityList,
        CancellationToken cancellationToken)
    {
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

    /// <inheritdoc/>
    public async Task<Either<EncinaError, int>> DeleteRangeAsync(
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        try
        {
            if (!TryUnwrap(_sqlBuilder.BuildWhereClause(specification, "DeleteRange"), out var built, out var tenantError))
            {
                return Left<EncinaError, int>(tenantError);
            }

            if (string.IsNullOrWhiteSpace(built.WhereClause))
            {
                return Left<EncinaError, int>(
                    RepositoryErrors.InvalidOperation<TEntity>("DeleteRange", "DELETE requires a WHERE clause to prevent accidental data loss."));
            }

            var sql = $"DELETE FROM {_mapping.TableName} {built.WhereClause}";
            var deletedCount = await _connection.ExecuteAsync(sql, built.Parameters);

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

    private Either<EncinaError, (string? filter, string? tenantId)> GetTenantFilter(string operation)
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
                return RepositoryErrors.TenantRequired<TEntity>(operation);
            }

            return (null, null);
        }

        return ($"[{_mapping.TenantColumnName}] = @tenantId", tenantId);
    }

    private Either<EncinaError, Unit> AssignTenantIdIfNeeded(TEntity entity, string operation)
    {
        if (!_mapping.IsTenantEntity || !_options.AutoAssignTenantId)
        {
            return Right<EncinaError, Unit>(Unit.Default);
        }

        var tenantId = _tenantProvider.GetCurrentTenantId();

        if (string.IsNullOrEmpty(tenantId))
        {
            return _options.ThrowOnMissingTenantContext
                ? Left<EncinaError, Unit>(RepositoryErrors.TenantRequired<TEntity>(operation))
                : Right<EncinaError, Unit>(Unit.Default);
        }

        _mapping.SetTenantId(entity, tenantId);
        return Right<EncinaError, Unit>(Unit.Default);
    }

    private static bool TryUnwrap<T>(Either<EncinaError, T> either, out T value, out EncinaError error)
    {
        var capturedValue = default(T)!;
        var capturedError = default(EncinaError);

        var isRight = either.Match(
            Right: right =>
            {
                capturedValue = right;
                return true;
            },
            Left: left =>
            {
                capturedError = left;
                return false;
            });

        value = capturedValue;
        error = capturedError;
        return isRight;
    }

    private Either<EncinaError, Unit> ValidateTenantOwnership(TEntity entity, string operation)
    {
        if (!_mapping.IsTenantEntity || !_options.ValidateTenantOnModify)
        {
            return Right<EncinaError, Unit>(Unit.Default);
        }

        var currentTenantId = _tenantProvider.GetCurrentTenantId();
        var entityTenantId = _mapping.GetTenantId(entity);

        if (string.IsNullOrEmpty(currentTenantId))
        {
            return _options.ThrowOnMissingTenantContext
                ? Left<EncinaError, Unit>(RepositoryErrors.TenantRequired<TEntity>(operation))
                : Right<EncinaError, Unit>(Unit.Default);
        }

        if (!string.Equals(currentTenantId, entityTenantId, StringComparison.Ordinal))
        {
            return Left<EncinaError, Unit>(
                RepositoryErrors.InvalidOperation<TEntity>(
                    operation,
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
