using System.Data;
using System.Globalization;
using System.Reflection;
using Encina.DomainModeling;
using Encina.DomainModeling.Auditing;
using Encina.DomainModeling.Concurrency;
using Encina.Tenancy;
using LanguageExt;
using Microsoft.Data.Sqlite;
using static LanguageExt.Prelude;

namespace Encina.ADO.Sqlite.Tenancy;

/// <summary>
/// Tenant-aware ADO.NET SQLite implementation of <see cref="IFunctionalRepository{TEntity, TId}"/>
/// with automatic tenant filtering, assignment, and validation.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TId">The entity identifier type.</typeparam>
/// <remarks>
/// <para>
/// This repository extends the standard ADO.NET repository with multi-tenancy capabilities:
/// </para>
/// <list type="bullet">
/// <item><description>Automatic WHERE TenantId filtering on all queries</description></item>
/// <item><description>Automatic tenant ID assignment on inserts</description></item>
/// <item><description>Cross-tenant validation on updates and deletes</description></item>
/// </list>
/// <para>
/// For non-tenant entities (without HasTenantId configuration), the repository
/// behaves identically to the standard FunctionalRepositoryADO.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Registration
/// services.AddTenantAwareRepository&lt;Order, Guid&gt;(mapping =&gt;
///     mapping.ToTable("Orders")
///            .HasId(o =&gt; o.Id)
///            .HasTenantId(o =&gt; o.TenantId)
///            .MapProperty(o =&gt; o.CustomerId));
///
/// // Usage - automatically filtered by current tenant
/// var orders = await repository.ListAsync();
/// </code>
/// </example>
public sealed class TenantAwareFunctionalRepositoryADO<TEntity, TId> : IFunctionalRepository<TEntity, TId>
    where TEntity : class, new()
    where TId : notnull
{
    private readonly IDbConnection _connection;
    private readonly ITenantEntityMapping<TEntity, TId> _mapping;
    private readonly ITenantProvider _tenantProvider;
    private readonly ADOTenancyOptions _options;
    private readonly TenantAwareSpecificationSqlBuilder<TEntity> _tenantSqlBuilder;
    private readonly IRequestContext? _requestContext;
    private readonly TimeProvider _timeProvider;

    // Cached SQL statements
    private readonly string _insertSql;
    private readonly string _updateSql;
    private readonly string _deleteByIdSql;

    // Cached property info for entity materialization
    private readonly Dictionary<string, PropertyInfo> _propertyCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantAwareFunctionalRepositoryADO{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="mapping">The tenant-aware entity mapping configuration.</param>
    /// <param name="tenantProvider">The tenant provider for current tenant context.</param>
    /// <param name="options">The tenancy options.</param>
    /// <param name="requestContext">Optional request context for audit fields.</param>
    /// <param name="timeProvider">Optional time provider for audit timestamps.</param>
    public TenantAwareFunctionalRepositoryADO(
        IDbConnection connection,
        ITenantEntityMapping<TEntity, TId> mapping,
        ITenantProvider tenantProvider,
        ADOTenancyOptions options,
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

        // Create tenant-aware SQL builder with object ID adapter
        var objectMapping = new GenericTenantMappingAdapter<TEntity, TId>(mapping);
        _tenantSqlBuilder = new TenantAwareSpecificationSqlBuilder<TEntity>(objectMapping, tenantProvider, options);

        // Build property cache for entity materialization
        _propertyCache = typeof(TEntity).GetProperties()
            .Where(p => mapping.ColumnMappings.ContainsKey(p.Name))
            .ToDictionary(p => p.Name);

        // Pre-build SQL statements
        _insertSql = BuildInsertSql();
        _updateSql = BuildUpdateSql();
        _deleteByIdSql = BuildDeleteByIdSql();
    }

    #region Read Operations

    /// <inheritdoc/>
    public async Task<Either<EncinaError, TEntity>> GetByIdAsync(
        TId id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureConnectionOpenAsync(cancellationToken).ConfigureAwait(false);

            // Build SELECT with tenant filter
            var (sql, addParameters) = BuildSelectByIdWithTenantFilter(id);

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            addParameters(command);

            using var reader = await ExecuteReaderAsync(command, cancellationToken).ConfigureAwait(false);

            if (await ReadAsync(reader, cancellationToken).ConfigureAwait(false))
            {
                var entity = MapReaderToEntity(reader);
                return Right<EncinaError, TEntity>(entity);
            }

            return Left<EncinaError, TEntity>(RepositoryErrors.NotFound<TEntity, TId>(id));
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
            await EnsureConnectionOpenAsync(cancellationToken).ConfigureAwait(false);

            var (sql, addParameters) = _tenantSqlBuilder.BuildSelectStatement(_mapping.TableName);

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            addParameters(command);

            var entities = await ReadEntitiesAsync(command, cancellationToken).ConfigureAwait(false);
            return Right<EncinaError, IReadOnlyList<TEntity>>(entities);
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
            await EnsureConnectionOpenAsync(cancellationToken).ConfigureAwait(false);

            var (sql, addParameters) = _tenantSqlBuilder.BuildSelectStatement(_mapping.TableName, specification);

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            addParameters(command);

            var entities = await ReadEntitiesAsync(command, cancellationToken).ConfigureAwait(false);
            return Right<EncinaError, IReadOnlyList<TEntity>>(entities);
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
            await EnsureConnectionOpenAsync(cancellationToken).ConfigureAwait(false);

            var (whereClause, addParameters) = _tenantSqlBuilder.BuildWhereClause(specification);
            var columns = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"\"{c}\""));
            // SQLite uses LIMIT 1 instead of TOP 1
            var sql = $"SELECT {columns} FROM {_mapping.TableName} {whereClause} LIMIT 1";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            addParameters(command);

            using var reader = await ExecuteReaderAsync(command, cancellationToken).ConfigureAwait(false);

            if (await ReadAsync(reader, cancellationToken).ConfigureAwait(false))
            {
                var entity = MapReaderToEntity(reader);
                return Right<EncinaError, TEntity>(entity);
            }

            return Left<EncinaError, TEntity>(
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
            await EnsureConnectionOpenAsync(cancellationToken).ConfigureAwait(false);

            var (whereClause, addParameters) = GetTenantFilterClause();
            var sql = string.IsNullOrWhiteSpace(whereClause)
                ? $"SELECT COUNT(*) FROM {_mapping.TableName}"
                : $"SELECT COUNT(*) FROM {_mapping.TableName} {whereClause}";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            addParameters(command);

            var result = await ExecuteScalarAsync(command, cancellationToken).ConfigureAwait(false);
            return Right<EncinaError, int>(Convert.ToInt32(result, CultureInfo.InvariantCulture));
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
            await EnsureConnectionOpenAsync(cancellationToken).ConfigureAwait(false);

            var (whereClause, addParameters) = _tenantSqlBuilder.BuildWhereClause(specification);
            var sql = $"SELECT COUNT(*) FROM {_mapping.TableName} {whereClause}";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            addParameters(command);

            var result = await ExecuteScalarAsync(command, cancellationToken).ConfigureAwait(false);
            return Right<EncinaError, int>(Convert.ToInt32(result, CultureInfo.InvariantCulture));
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
            await EnsureConnectionOpenAsync(cancellationToken).ConfigureAwait(false);

            var (whereClause, addParameters) = _tenantSqlBuilder.BuildWhereClause(specification);
            var sql = $"SELECT CASE WHEN EXISTS (SELECT 1 FROM {_mapping.TableName} {whereClause}) THEN 1 ELSE 0 END";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            addParameters(command);

            var result = await ExecuteScalarAsync(command, cancellationToken).ConfigureAwait(false);
            return Right<EncinaError, bool>(Convert.ToInt32(result, CultureInfo.InvariantCulture) == 1);
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
            await EnsureConnectionOpenAsync(cancellationToken).ConfigureAwait(false);

            var (whereClause, addParameters) = GetTenantFilterClause();
            var sql = string.IsNullOrWhiteSpace(whereClause)
                ? $"SELECT CASE WHEN EXISTS (SELECT 1 FROM {_mapping.TableName}) THEN 1 ELSE 0 END"
                : $"SELECT CASE WHEN EXISTS (SELECT 1 FROM {_mapping.TableName} {whereClause}) THEN 1 ELSE 0 END";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            addParameters(command);

            var result = await ExecuteScalarAsync(command, cancellationToken).ConfigureAwait(false);
            return Right<EncinaError, bool>(Convert.ToInt32(result, CultureInfo.InvariantCulture) == 1);
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

            // Populate audit fields before persistence
            AuditFieldPopulator.PopulateForCreate(entity, _requestContext?.UserId, _timeProvider);

            await EnsureConnectionOpenAsync(cancellationToken).ConfigureAwait(false);

            using var command = _connection.CreateCommand();
            command.CommandText = _insertSql;
            AddEntityParameters(command, entity, forInsert: true);

            await ExecuteNonQueryAsync(command, cancellationToken).ConfigureAwait(false);
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

            // Populate audit fields before persistence
            AuditFieldPopulator.PopulateForUpdate(entity, _requestContext?.UserId, _timeProvider);

            await EnsureConnectionOpenAsync(cancellationToken).ConfigureAwait(false);

            var (tenantFilter, tenantId) = GetTenantFilter();
            var id = _mapping.GetId(entity);
            int rowsAffected;

            // Handle versioned entities for optimistic concurrency
            if (entity is IVersionedEntity versionedEntity)
            {
                var originalVersion = versionedEntity.Version;
                versionedEntity.Version = (int)(originalVersion + 1);

                var versionedSql = BuildVersionedUpdateSql();

                // Build UPDATE with tenant filter and version check
                if (!string.IsNullOrEmpty(tenantFilter) && _mapping.IsTenantEntity)
                {
                    versionedSql = $"{versionedSql} AND {tenantFilter}";
                }

                using var command = _connection.CreateCommand();
                command.CommandText = versionedSql;
                AddEntityParameters(command, entity, forInsert: false);
                AddParameter(command, "@Id", ConvertIdForStorage(id));
                AddParameter(command, "@OriginalVersion", originalVersion);

                if (!string.IsNullOrEmpty(tenantId))
                {
                    AddParameter(command, "@TenantId", tenantId);
                }

                rowsAffected = await ExecuteNonQueryAsync(command, cancellationToken).ConfigureAwait(false);

                if (rowsAffected == 0)
                {
                    // Check if entity exists to distinguish NotFound from ConcurrencyConflict
                    var existsSql = !string.IsNullOrEmpty(tenantFilter) && _mapping.IsTenantEntity
                        ? $"SELECT CASE WHEN EXISTS (SELECT 1 FROM \"{_mapping.TableName}\" WHERE \"{_mapping.IdColumnName}\" = @Id AND {tenantFilter}) THEN 1 ELSE 0 END"
                        : $"SELECT CASE WHEN EXISTS (SELECT 1 FROM \"{_mapping.TableName}\" WHERE \"{_mapping.IdColumnName}\" = @Id) THEN 1 ELSE 0 END";

                    using var existsCommand = _connection.CreateCommand();
                    existsCommand.CommandText = existsSql;
                    AddParameter(existsCommand, "@Id", ConvertIdForStorage(id));

                    if (!string.IsNullOrEmpty(tenantId))
                    {
                        AddParameter(existsCommand, "@TenantId", tenantId);
                    }

                    var exists = Convert.ToInt32(await ExecuteScalarAsync(existsCommand, cancellationToken).ConfigureAwait(false), CultureInfo.InvariantCulture);

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
            if (!string.IsNullOrEmpty(tenantFilter) && _mapping.IsTenantEntity)
            {
                var sql = $"{_updateSql} AND {tenantFilter}";

                using var command = _connection.CreateCommand();
                command.CommandText = sql;
                AddEntityParameters(command, entity, forInsert: false);
                AddParameter(command, "@Id", ConvertIdForStorage(id));
                AddParameter(command, "@TenantId", tenantId!);

                rowsAffected = await ExecuteNonQueryAsync(command, cancellationToken).ConfigureAwait(false);

                if (rowsAffected == 0)
                {
                    return Left<EncinaError, TEntity>(RepositoryErrors.NotFound<TEntity, TId>(id));
                }

                return Right<EncinaError, TEntity>(entity);
            }
            else
            {
                using var command = _connection.CreateCommand();
                command.CommandText = _updateSql;
                AddEntityParameters(command, entity, forInsert: false);
                AddParameter(command, "@Id", ConvertIdForStorage(id));

                rowsAffected = await ExecuteNonQueryAsync(command, cancellationToken).ConfigureAwait(false);

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
            await EnsureConnectionOpenAsync(cancellationToken).ConfigureAwait(false);

            var (tenantFilter, tenantId) = GetTenantFilter();

            string sql;
            if (!string.IsNullOrEmpty(tenantFilter) && _mapping.IsTenantEntity)
            {
                sql = $"{_deleteByIdSql} AND {tenantFilter}";
            }
            else
            {
                sql = _deleteByIdSql;
            }

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", id);

            if (!string.IsNullOrEmpty(tenantId))
            {
                AddParameter(command, "@TenantId", tenantId);
            }

            var rowsAffected = await ExecuteNonQueryAsync(command, cancellationToken).ConfigureAwait(false);

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

        // Validate tenant ownership before delete
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
            // Auto-assign tenant IDs and populate audit fields
            foreach (var entity in entityList)
            {
                AssignTenantIdIfNeeded(entity);
                AuditFieldPopulator.PopulateForCreate(entity, _requestContext?.UserId, _timeProvider);
            }

            await EnsureConnectionOpenAsync(cancellationToken).ConfigureAwait(false);

            foreach (var entity in entityList)
            {
                using var command = _connection.CreateCommand();
                command.CommandText = _insertSql;
                AddEntityParameters(command, entity, forInsert: true);

                await ExecuteNonQueryAsync(command, cancellationToken).ConfigureAwait(false);
            }

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

        // Validate all entities first
        foreach (var entity in entityList)
        {
            var validationResult = ValidateTenantOwnership(entity);
            if (validationResult.IsLeft)
            {
                return validationResult;
            }
        }

        // Check if we're dealing with versioned entities
        var hasVersionedEntities = entityList[0] is IVersioned;

        // Populate audit fields before persistence
        foreach (var entity in entityList)
        {
            AuditFieldPopulator.PopulateForUpdate(entity, _requestContext?.UserId, _timeProvider);
        }

        try
        {
            await EnsureConnectionOpenAsync(cancellationToken).ConfigureAwait(false);

            var (tenantFilter, tenantId) = GetTenantFilter();

            if (hasVersionedEntities)
            {
                // Handle versioned entities with optimistic concurrency
                var versionedSql = BuildVersionedUpdateSql();
                if (!string.IsNullOrEmpty(tenantFilter) && _mapping.IsTenantEntity)
                {
                    versionedSql = $"{versionedSql} AND {tenantFilter}";
                }

                var totalUpdated = 0;

                foreach (var entity in entityList)
                {
                    if (entity is IVersionedEntity versionedEntity)
                    {
                        var originalVersion = versionedEntity.Version;
                        versionedEntity.Version = (int)(originalVersion + 1);

                        using var command = _connection.CreateCommand();
                        command.CommandText = versionedSql;
                        AddEntityParameters(command, entity, forInsert: false);
                        AddParameter(command, "@Id", ConvertIdForStorage(_mapping.GetId(entity)));
                        AddParameter(command, "@OriginalVersion", originalVersion);

                        if (!string.IsNullOrEmpty(tenantId))
                        {
                            AddParameter(command, "@TenantId", tenantId);
                        }

                        var rowsAffected = await ExecuteNonQueryAsync(command, cancellationToken).ConfigureAwait(false);
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

            // Non-versioned entities - use standard update
            foreach (var entity in entityList)
            {
                using var command = _connection.CreateCommand();

                if (!string.IsNullOrEmpty(tenantFilter) && _mapping.IsTenantEntity)
                {
                    command.CommandText = $"{_updateSql} AND {tenantFilter}";
                    AddEntityParameters(command, entity, forInsert: false);
                    AddParameter(command, "@Id", ConvertIdForStorage(_mapping.GetId(entity)));
                    AddParameter(command, "@TenantId", tenantId!);
                }
                else
                {
                    command.CommandText = _updateSql;
                    AddEntityParameters(command, entity, forInsert: false);
                    AddParameter(command, "@Id", ConvertIdForStorage(_mapping.GetId(entity)));
                }

                await ExecuteNonQueryAsync(command, cancellationToken).ConfigureAwait(false);
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
            await EnsureConnectionOpenAsync(cancellationToken).ConfigureAwait(false);

            var (whereClause, addParameters) = _tenantSqlBuilder.BuildWhereClause(specification);

            if (string.IsNullOrWhiteSpace(whereClause))
            {
                return Left<EncinaError, int>(
                    RepositoryErrors.InvalidOperation<TEntity>("DeleteRange", "DELETE requires a WHERE clause to prevent accidental data loss."));
            }

            var sql = $"DELETE FROM {_mapping.TableName} {whereClause}";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            addParameters(command);

            var deletedCount = await ExecuteNonQueryAsync(command, cancellationToken).ConfigureAwait(false);
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
    /// This operation is not supported in ADO.NET providers because they don't have change tracking.
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

    #region Tenant Helpers

    private void AssignTenantIdIfNeeded(TEntity entity)
    {
        if (!_mapping.IsTenantEntity || !_options.AutoAssignTenantId)
        {
            return;
        }

        var currentTenantId = _tenantProvider.GetCurrentTenantId();
        if (string.IsNullOrEmpty(currentTenantId))
        {
            if (_options.ThrowOnMissingTenantContext)
            {
                throw new InvalidOperationException(
                    $"Cannot add entity {typeof(TEntity).Name} without tenant context. " +
                    $"Either set a current tenant or disable {nameof(ADOTenancyOptions.AutoAssignTenantId)}.");
            }

            return;
        }

        _mapping.SetTenantId(entity, currentTenantId);
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
                        "TenantValidation",
                        $"Cannot modify entity {typeof(TEntity).Name} without tenant context."));
            }

            return Right<EncinaError, Unit>(Unit.Default);
        }

        if (!string.Equals(currentTenantId, entityTenantId, StringComparison.Ordinal))
        {
            return Left<EncinaError, Unit>(
                RepositoryErrors.InvalidOperation<TEntity>(
                    "TenantValidation",
                    $"Cannot modify entity belonging to tenant '{entityTenantId}' from tenant context '{currentTenantId}'."));
        }

        return Right<EncinaError, Unit>(Unit.Default);
    }

    private (string filter, string? tenantId) GetTenantFilter()
    {
        if (!_mapping.IsTenantEntity || !_options.AutoFilterTenantQueries)
        {
            return (string.Empty, null);
        }

        var tenantId = _tenantProvider.GetCurrentTenantId();
        if (string.IsNullOrEmpty(tenantId))
        {
            if (_options.ThrowOnMissingTenantContext)
            {
                throw new InvalidOperationException(
                    $"Cannot execute operation on tenant entity {typeof(TEntity).Name} without tenant context.");
            }

            return (string.Empty, null);
        }

        // SQLite uses double quotes for identifier quoting
        return ($"\"{_mapping.TenantColumnName}\" = @TenantId", tenantId);
    }

    private (string WhereClause, Action<IDbCommand> AddParameters) GetTenantFilterClause()
    {
        var (filter, tenantId) = GetTenantFilter();

        if (string.IsNullOrEmpty(filter))
        {
            return (string.Empty, _ => { });
        }

        return ($"WHERE {filter}", command =>
        {
            AddParameter(command, "@TenantId", tenantId!);
        }
        );
    }

    private (string Sql, Action<IDbCommand> AddParameters) BuildSelectByIdWithTenantFilter(TId id)
    {
        var columns = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"\"{c}\""));
        var (tenantFilter, tenantId) = GetTenantFilter();

        string sql;
        if (!string.IsNullOrEmpty(tenantFilter) && _mapping.IsTenantEntity)
        {
            sql = $"SELECT {columns} FROM {_mapping.TableName} WHERE \"{_mapping.IdColumnName}\" = @Id AND {tenantFilter}";
            return (sql, command =>
            {
                AddParameter(command, "@Id", id);
                AddParameter(command, "@TenantId", tenantId!);
            }
            );
        }
        else
        {
            sql = $"SELECT {columns} FROM {_mapping.TableName} WHERE \"{_mapping.IdColumnName}\" = @Id";
            return (sql, command => AddParameter(command, "@Id", id));
        }
    }

    #endregion

    #region SQL Generation

    private string BuildInsertSql()
    {
        var insertableProperties = _mapping.ColumnMappings
            .Where(kvp => !_mapping.InsertExcludedProperties.Contains(kvp.Key))
            .ToList();

        // SQLite uses double quotes for identifier quoting
        var columns = string.Join(", ", insertableProperties.Select(kvp => $"\"{kvp.Value}\""));
        var parameters = string.Join(", ", insertableProperties.Select(kvp => $"@{kvp.Key}"));

        return $"INSERT INTO {_mapping.TableName} ({columns}) VALUES ({parameters})";
    }

    private string BuildUpdateSql()
    {
        var updatableProperties = _mapping.ColumnMappings
            .Where(kvp => !_mapping.UpdateExcludedProperties.Contains(kvp.Key))
            .ToList();

        // SQLite uses double quotes for identifier quoting
        var setClauses = string.Join(", ", updatableProperties.Select(kvp => $"\"{kvp.Value}\" = @{kvp.Key}"));

        return $"UPDATE \"{_mapping.TableName}\" SET {setClauses} WHERE \"{_mapping.IdColumnName}\" = @Id";
    }

    private string BuildVersionedUpdateSql()
    {
        var updatableProperties = _mapping.ColumnMappings
            .Where(kvp => !_mapping.UpdateExcludedProperties.Contains(kvp.Key))
            .ToList();

        var setClauses = string.Join(", ", updatableProperties.Select(kvp => $"\"{kvp.Value}\" = @{kvp.Key}"));

        // Add version check to WHERE clause for optimistic concurrency
        return $"UPDATE \"{_mapping.TableName}\" SET {setClauses} WHERE \"{_mapping.IdColumnName}\" = @Id AND \"Version\" = @OriginalVersion";
    }

    private string BuildDeleteByIdSql()
    {
        // SQLite uses double quotes for identifier quoting
        return $"DELETE FROM {_mapping.TableName} WHERE \"{_mapping.IdColumnName}\" = @Id";
    }

    #endregion

    #region Entity Materialization

    private TEntity MapReaderToEntity(IDataReader reader)
    {
        var entity = new TEntity();

        foreach (var (propertyName, columnName) in _mapping.ColumnMappings)
        {
            if (!_propertyCache.TryGetValue(propertyName, out var property))
                continue;

            var ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal))
                continue;

            var value = reader.GetValue(ordinal);
            var convertedValue = ConvertValue(value, property.PropertyType);
            property.SetValue(entity, convertedValue);
        }

        return entity;
    }

    private static object? ConvertValue(object value, Type targetType)
    {
        if (value is DBNull)
            return null;

        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlyingType == value.GetType())
            return value;

        if (underlyingType.IsEnum)
            return Enum.ToObject(underlyingType, value);

        // Handle Guid conversion for SQLite (stored as text or blob)
        if (underlyingType == typeof(Guid) && value is string stringValue)
            return Guid.Parse(stringValue);

        if (underlyingType == typeof(Guid) && value is byte[] byteValue)
            return new Guid(byteValue);

        return Convert.ChangeType(value, underlyingType, CultureInfo.InvariantCulture);
    }

    private async Task<List<TEntity>> ReadEntitiesAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        var entities = new List<TEntity>();

        using var reader = await ExecuteReaderAsync(command, cancellationToken).ConfigureAwait(false);
        while (await ReadAsync(reader, cancellationToken).ConfigureAwait(false))
        {
            entities.Add(MapReaderToEntity(reader));
        }

        return entities;
    }

    #endregion

    #region Parameter Helpers

    private static void AddParameter(IDbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private void AddEntityParameters(IDbCommand command, TEntity entity, bool forInsert)
    {
        var excludedProperties = forInsert
            ? _mapping.InsertExcludedProperties
            : _mapping.UpdateExcludedProperties;

        foreach (var (propertyName, _) in _mapping.ColumnMappings)
        {
            if (excludedProperties.Contains(propertyName))
                continue;

            if (_propertyCache.TryGetValue(propertyName, out var property))
            {
                var value = property.GetValue(entity);
                AddParameter(command, $"@{propertyName}", value);
            }
        }
    }

    /// <summary>
    /// Converts an ID value for SQLite storage (GUIDs to strings).
    /// </summary>
    private static object ConvertIdForStorage(TId id)
    {
        if (id is Guid guidId)
        {
            return guidId.ToString();
        }
        return id;
    }

    #endregion

    #region Async Helpers

    private async Task EnsureConnectionOpenAsync(CancellationToken cancellationToken)
    {
        if (_connection.State == ConnectionState.Open)
            return;

        if (_connection is SqliteConnection sqliteConnection)
        {
            await sqliteConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await Task.Run(_connection.Open, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task<IDataReader> ExecuteReaderAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is SqliteCommand sqliteCommand)
            return await sqliteCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        return await Task.Run(command.ExecuteReader, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<int> ExecuteNonQueryAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is SqliteCommand sqliteCommand)
            return await sqliteCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        return await Task.Run(command.ExecuteNonQuery, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<object?> ExecuteScalarAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is SqliteCommand sqliteCommand)
            return await sqliteCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

        return await Task.Run(command.ExecuteScalar, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<bool> ReadAsync(IDataReader reader, CancellationToken cancellationToken)
    {
        if (reader is SqliteDataReader sqliteReader)
            return await sqliteReader.ReadAsync(cancellationToken).ConfigureAwait(false);

        return await Task.Run(reader.Read, cancellationToken).ConfigureAwait(false);
    }

    private static bool IsDuplicateKeyException(Exception ex)
    {
        var message = ex.Message;
        // SQLite specific duplicate key error messages
        return message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase)
            || message.Contains("PRIMARY KEY constraint failed", StringComparison.OrdinalIgnoreCase)
            || message.Contains("constraint failed", StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}

/// <summary>
/// Adapter to convert typed ITenantEntityMapping to object-based version.
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
