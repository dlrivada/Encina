using System.Data;
using Dapper;
using Encina;
using Encina.Dapper.SqlServer.Repository;
using Encina.DomainModeling;
using Encina.DomainModeling.Concurrency;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dapper.SqlServer.UnitOfWork;

/// <summary>
/// Dapper repository implementation for use within a Unit of Work.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TId">The entity identifier type.</typeparam>
/// <remarks>
/// <para>
/// Unlike EF Core, Dapper executes all operations immediately against the database.
/// When used within a <see cref="UnitOfWorkDapper"/>, operations participate in
/// the active transaction, providing atomicity across multiple repository operations.
/// </para>
/// <para>
/// This repository passes the current transaction to all Dapper operations,
/// ensuring changes are only committed when <see cref="IUnitOfWork.CommitAsync"/>
/// is called on the parent Unit of Work.
/// </para>
/// </remarks>
internal sealed class UnitOfWorkRepositoryDapper<TEntity, TId> : IFunctionalRepository<TEntity, TId>
    where TEntity : class
    where TId : notnull
{
    private readonly IDbConnection _connection;
    private readonly IEntityMapping<TEntity, TId> _mapping;
    private readonly UnitOfWorkDapper _unitOfWork;
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
    /// Initializes a new instance of the <see cref="UnitOfWorkRepositoryDapper{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="mapping">The entity mapping configuration.</param>
    /// <param name="unitOfWork">The parent Unit of Work.</param>
    /// <param name="requestContext">Optional request context for audit field population.</param>
    /// <param name="timeProvider">Optional time provider for audit timestamps. Defaults to <see cref="TimeProvider.System"/>.</param>
    public UnitOfWorkRepositoryDapper(
        IDbConnection connection,
        IEntityMapping<TEntity, TId> mapping,
        UnitOfWorkDapper unitOfWork,
        IRequestContext? requestContext = null,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(mapping);
        ArgumentNullException.ThrowIfNull(unitOfWork);

        _connection = connection;
        _mapping = mapping;
        _unitOfWork = unitOfWork;
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
                new CommandDefinition(
                    _selectByIdSql,
                    new { Id = id },
                    _unitOfWork.CurrentTransaction,
                    cancellationToken: cancellationToken));

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
            var entities = await _connection.QueryAsync<TEntity>(
                new CommandDefinition(
                    _selectAllSql,
                    transaction: _unitOfWork.CurrentTransaction,
                    cancellationToken: cancellationToken));

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
            var entities = await _connection.QueryAsync<TEntity>(
                new CommandDefinition(
                    sql,
                    parameters,
                    _unitOfWork.CurrentTransaction,
                    cancellationToken: cancellationToken));

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

            var entity = await _connection.QuerySingleOrDefaultAsync<TEntity>(
                new CommandDefinition(
                    sql,
                    parameters,
                    _unitOfWork.CurrentTransaction,
                    cancellationToken: cancellationToken));

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
            var count = await _connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    _countSql,
                    transaction: _unitOfWork.CurrentTransaction,
                    cancellationToken: cancellationToken));

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

            var count = await _connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    sql,
                    parameters,
                    _unitOfWork.CurrentTransaction,
                    cancellationToken: cancellationToken));

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

            var exists = await _connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    sql,
                    parameters,
                    _unitOfWork.CurrentTransaction,
                    cancellationToken: cancellationToken));

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
            var exists = await _connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    _existsSql,
                    transaction: _unitOfWork.CurrentTransaction,
                    cancellationToken: cancellationToken));

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
    /// <remarks>
    /// This operation executes immediately within the Unit of Work transaction.
    /// The change is only committed when <see cref="IUnitOfWork.CommitAsync"/> is called.
    /// </remarks>
    public async Task<Either<EncinaError, TEntity>> AddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        try
        {
            // Populate audit fields for creation
            AuditFieldPopulator.PopulateForCreate(entity, _requestContext?.UserId, _timeProvider);

            await _connection.ExecuteAsync(
                new CommandDefinition(
                    _insertSql,
                    entity,
                    _unitOfWork.CurrentTransaction,
                    cancellationToken: cancellationToken));

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
    /// <remarks>
    /// This operation executes immediately within the Unit of Work transaction.
    /// The change is only committed when <see cref="IUnitOfWork.CommitAsync"/> is called.
    /// </remarks>
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
                rowsAffected = await _connection.ExecuteAsync(
                    new CommandDefinition(
                        versionedSql,
                        parameters,
                        _unitOfWork.CurrentTransaction,
                        cancellationToken: cancellationToken));

                if (rowsAffected == 0)
                {
                    // Check if entity exists to distinguish NotFound from ConcurrencyConflict
                    var exists = await _connection.ExecuteScalarAsync<int>(
                        new CommandDefinition(
                            $"SELECT CASE WHEN EXISTS (SELECT 1 FROM {_mapping.TableName} WHERE [{_mapping.IdColumnName}] = @Id) THEN 1 ELSE 0 END",
                            new { Id = id },
                            _unitOfWork.CurrentTransaction,
                            cancellationToken: cancellationToken));

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
            rowsAffected = await _connection.ExecuteAsync(
                new CommandDefinition(
                    _updateSql,
                    entity,
                    _unitOfWork.CurrentTransaction,
                    cancellationToken: cancellationToken));

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
    /// <remarks>
    /// This operation executes immediately within the Unit of Work transaction.
    /// The change is only committed when <see cref="IUnitOfWork.CommitAsync"/> is called.
    /// </remarks>
    public async Task<Either<EncinaError, Unit>> DeleteAsync(
        TId id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var rowsAffected = await _connection.ExecuteAsync(
                new CommandDefinition(
                    _deleteByIdSql,
                    new { Id = id },
                    _unitOfWork.CurrentTransaction,
                    cancellationToken: cancellationToken));

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
    /// <remarks>
    /// This operation executes immediately within the Unit of Work transaction.
    /// The change is only committed when <see cref="IUnitOfWork.CommitAsync"/> is called.
    /// </remarks>
    public async Task<Either<EncinaError, Unit>> DeleteAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var id = _mapping.GetId(entity);
        return await DeleteAsync(id, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// This operation executes immediately within the Unit of Work transaction.
    /// The changes are only committed when <see cref="IUnitOfWork.CommitAsync"/> is called.
    /// </remarks>
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

            await _connection.ExecuteAsync(
                new CommandDefinition(
                    _insertSql,
                    entityList,
                    _unitOfWork.CurrentTransaction,
                    cancellationToken: cancellationToken));

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
    /// <remarks>
    /// This operation executes immediately within the Unit of Work transaction.
    /// The changes are only committed when <see cref="IUnitOfWork.CommitAsync"/> is called.
    /// </remarks>
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

                        var rowsAffected = await _connection.ExecuteAsync(
                            new CommandDefinition(
                                versionedSql,
                                parameters,
                                _unitOfWork.CurrentTransaction,
                                cancellationToken: cancellationToken));
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
            await _connection.ExecuteAsync(
                new CommandDefinition(
                    _updateSql,
                    entityList,
                    _unitOfWork.CurrentTransaction,
                    cancellationToken: cancellationToken));

            return Right<EncinaError, Unit>(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, Unit>(
                RepositoryErrors.PersistenceError<TEntity>("UpdateRange", ex));
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// This operation executes immediately within the Unit of Work transaction.
    /// The changes are only committed when <see cref="IUnitOfWork.CommitAsync"/> is called.
    /// </remarks>
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
            var deletedCount = await _connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    parameters,
                    _unitOfWork.CurrentTransaction,
                    cancellationToken: cancellationToken));

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
