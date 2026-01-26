using System.Data;
using Dapper;
using Encina;
using Encina.Dapper.Oracle.Repository;
using Encina.DomainModeling;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dapper.Oracle.UnitOfWork;

/// <summary>
/// Dapper repository implementation for use within a Unit of Work for Oracle.
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
/// <para>
/// Oracle-specific features:
/// <list type="bullet">
/// <item><description>Uses double-quotes for identifiers (case-sensitive)</description></item>
/// <item><description>Uses OFFSET/FETCH for pagination (Oracle 12c+)</description></item>
/// <item><description>Uses colon prefix for parameters (:paramName)</description></item>
/// <item><description>GUIDs are stored as RAW(16) and converted to/from byte arrays</description></item>
/// <item><description>Booleans are stored as NUMBER(1) (0/1)</description></item>
/// </list>
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
    public UnitOfWorkRepositoryDapper(
        IDbConnection connection,
        IEntityMapping<TEntity, TId> mapping,
        UnitOfWorkDapper unitOfWork)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(mapping);
        ArgumentNullException.ThrowIfNull(unitOfWork);

        _connection = connection;
        _mapping = mapping;
        _unitOfWork = unitOfWork;
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
            var parameters = new DynamicParameters();
            AddIdParameter(parameters, "Id", id);

            var entity = await _connection.QuerySingleOrDefaultAsync<TEntity>(
                new CommandDefinition(
                    _selectByIdSql,
                    parameters,
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
            var columns = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"\"{c}\""));
            var sql = $"SELECT {columns} FROM \"{_mapping.TableName}\" {whereClause} FETCH FIRST 1 ROWS ONLY";

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
            var sql = $"SELECT COUNT(*) FROM \"{_mapping.TableName}\" {whereClause}";

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
            // Oracle requires FROM DUAL for queries without a table in SELECT
            var sql = $"SELECT CASE WHEN EXISTS (SELECT 1 FROM \"{_mapping.TableName}\" {whereClause}) THEN 1 ELSE 0 END FROM DUAL";

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
            var parameters = BuildInsertParameters(entity);
            await _connection.ExecuteAsync(
                new CommandDefinition(
                    _insertSql,
                    parameters,
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
            var parameters = BuildUpdateParameters(entity);
            var rowsAffected = await _connection.ExecuteAsync(
                new CommandDefinition(
                    _updateSql,
                    parameters,
                    _unitOfWork.CurrentTransaction,
                    cancellationToken: cancellationToken));

            if (rowsAffected == 0)
            {
                var id = _mapping.GetId(entity);
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
            var parameters = new DynamicParameters();
            AddIdParameter(parameters, "Id", id);

            var rowsAffected = await _connection.ExecuteAsync(
                new CommandDefinition(
                    _deleteByIdSql,
                    parameters,
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
            // Oracle/Dapper works better with individual inserts for batches
            foreach (var entity in entityList)
            {
                var parameters = BuildInsertParameters(entity);
                await _connection.ExecuteAsync(
                    new CommandDefinition(
                        _insertSql,
                        parameters,
                        _unitOfWork.CurrentTransaction,
                        cancellationToken: cancellationToken));
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

        try
        {
            foreach (var entity in entityList)
            {
                var parameters = BuildUpdateParameters(entity);
                await _connection.ExecuteAsync(
                    new CommandDefinition(
                        _updateSql,
                        parameters,
                        _unitOfWork.CurrentTransaction,
                        cancellationToken: cancellationToken));
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

            var sql = $"DELETE FROM \"{_mapping.TableName}\" {whereClause}";
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

    #endregion

    #region SQL Generation

    private string BuildSelectByIdSql()
    {
        var columns = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"\"{c}\""));
        return $"SELECT {columns} FROM \"{_mapping.TableName}\" WHERE \"{_mapping.IdColumnName}\" = :Id";
    }

    private string BuildSelectAllSql()
    {
        var columns = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"\"{c}\""));
        return $"SELECT {columns} FROM \"{_mapping.TableName}\"";
    }

    private string BuildInsertSql()
    {
        var insertableProperties = _mapping.ColumnMappings
            .Where(kvp => !_mapping.InsertExcludedProperties.Contains(kvp.Key))
            .ToList();

        var columns = string.Join(", ", insertableProperties.Select(kvp => $"\"{kvp.Value}\""));
        var parameters = string.Join(", ", insertableProperties.Select(kvp => $":{kvp.Key}"));

        return $"INSERT INTO \"{_mapping.TableName}\" ({columns}) VALUES ({parameters})";
    }

    private string BuildUpdateSql()
    {
        var updatableProperties = _mapping.ColumnMappings
            .Where(kvp => !_mapping.UpdateExcludedProperties.Contains(kvp.Key))
            .ToList();

        var setClauses = string.Join(", ", updatableProperties.Select(kvp => $"\"{kvp.Value}\" = :{kvp.Key}"));

        // Find the ID property name for the WHERE clause
        var idProperty = _mapping.ColumnMappings.First(kvp => kvp.Value == _mapping.IdColumnName);

        return $"UPDATE \"{_mapping.TableName}\" SET {setClauses} WHERE \"{_mapping.IdColumnName}\" = :{idProperty.Key}";
    }

    private string BuildDeleteByIdSql()
    {
        return $"DELETE FROM \"{_mapping.TableName}\" WHERE \"{_mapping.IdColumnName}\" = :Id";
    }

    private string BuildCountSql()
    {
        return $"SELECT COUNT(*) FROM \"{_mapping.TableName}\"";
    }

    private string BuildExistsSql()
    {
        // Oracle requires FROM DUAL for queries without a table
        return $"SELECT CASE WHEN EXISTS (SELECT 1 FROM \"{_mapping.TableName}\") THEN 1 ELSE 0 END FROM DUAL";
    }

    #endregion

    #region Parameter Helpers

    private DynamicParameters BuildInsertParameters(TEntity entity)
    {
        var parameters = new DynamicParameters();
        var entityType = typeof(TEntity);

        foreach (var kvp in _mapping.ColumnMappings)
        {
            if (_mapping.InsertExcludedProperties.Contains(kvp.Key))
                continue;

            var property = entityType.GetProperty(kvp.Key);
            if (property is null)
                continue;

            var value = property.GetValue(entity);
            AddParameterWithTypeHandling(parameters, kvp.Key, value, property.PropertyType);
        }

        return parameters;
    }

    private DynamicParameters BuildUpdateParameters(TEntity entity)
    {
        var parameters = new DynamicParameters();
        var entityType = typeof(TEntity);

        // Add all column parameters
        foreach (var kvp in _mapping.ColumnMappings)
        {
            var property = entityType.GetProperty(kvp.Key);
            if (property is null)
                continue;

            var value = property.GetValue(entity);
            AddParameterWithTypeHandling(parameters, kvp.Key, value, property.PropertyType);
        }

        return parameters;
    }

    private static void AddIdParameter(DynamicParameters parameters, string name, TId id)
    {
        // Oracle stores GUIDs as RAW(16) - convert to byte array
        if (id is Guid guidId)
        {
            parameters.Add(name, guidId.ToByteArray(), DbType.Binary, size: 16);
        }
        else
        {
            parameters.Add(name, id);
        }
    }

    private static void AddParameterWithTypeHandling(DynamicParameters parameters, string name, object? value, Type propertyType)
    {
        // Handle GUID -> RAW(16) conversion for Oracle (byte array)
        if (propertyType == typeof(Guid))
        {
            parameters.Add(name, value is Guid g ? g.ToByteArray() : null, DbType.Binary, size: 16);
        }
        else if (propertyType == typeof(Guid?))
        {
            parameters.Add(name, value is Guid g ? g.ToByteArray() : null, DbType.Binary, size: 16);
        }
        // Handle boolean -> NUMBER(1) conversion for Oracle
        else if (propertyType == typeof(bool))
        {
            parameters.Add(name, value is true ? 1 : 0, DbType.Int32);
        }
        else if (propertyType == typeof(bool?))
        {
            parameters.Add(name, value is bool b ? (b ? 1 : 0) : null, DbType.Int32);
        }
        else
        {
            parameters.Add(name, value);
        }
    }

    private static bool IsDuplicateKeyException(Exception ex)
    {
        var message = ex.Message;
        // Oracle unique constraint error: ORA-00001
        return message.Contains("ORA-00001", StringComparison.OrdinalIgnoreCase)
            || message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
