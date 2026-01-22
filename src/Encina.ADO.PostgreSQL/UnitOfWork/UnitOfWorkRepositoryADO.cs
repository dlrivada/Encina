using System.Data;
using System.Globalization;
using System.Reflection;
using Encina;
using Encina.ADO.PostgreSQL.Repository;
using Encina.DomainModeling;
using LanguageExt;
using Npgsql;
using static LanguageExt.Prelude;

namespace Encina.ADO.PostgreSQL.UnitOfWork;

/// <summary>
/// ADO.NET repository implementation for use within a Unit of Work for PostgreSQL.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TId">The entity identifier type.</typeparam>
/// <remarks>
/// <para>
/// ADO.NET executes all operations immediately against the database.
/// When used within a <see cref="UnitOfWorkADO"/>, operations participate in
/// the active transaction, providing atomicity across multiple repository operations.
/// </para>
/// <para>
/// This repository passes the current transaction to all ADO.NET commands,
/// ensuring changes are only committed when <see cref="IUnitOfWork.CommitAsync"/>
/// is called on the parent Unit of Work.
/// </para>
/// <para>
/// PostgreSQL-specific features:
/// <list type="bullet">
/// <item><description>Uses double-quotes for identifier quoting</description></item>
/// <item><description>Uses LIMIT for row limiting</description></item>
/// <item><description>Native UUID (GUID) support - no string conversion needed</description></item>
/// <item><description>Native boolean type support</description></item>
/// <item><description>Uses PostgreSQL error code 23505 for unique violations</description></item>
/// </list>
/// </para>
/// </remarks>
internal sealed class UnitOfWorkRepositoryADO<TEntity, TId> : IFunctionalRepository<TEntity, TId>
    where TEntity : class
    where TId : notnull
{
    private readonly IDbConnection _connection;
    private readonly IEntityMapping<TEntity, TId> _mapping;
    private readonly UnitOfWorkADO _unitOfWork;
    private readonly SpecificationSqlBuilder<TEntity> _sqlBuilder;

    // Cached SQL statements
    private readonly string _selectByIdSql;
    private readonly string _selectAllSql;
    private readonly string _insertSql;
    private readonly string _updateSql;
    private readonly string _deleteByIdSql;
    private readonly string _countSql;
    private readonly string _existsSql;

    // Cached property info for entity materialization
    private readonly Dictionary<string, PropertyInfo> _propertyCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitOfWorkRepositoryADO{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="mapping">The entity mapping configuration.</param>
    /// <param name="unitOfWork">The parent Unit of Work.</param>
    public UnitOfWorkRepositoryADO(
        IDbConnection connection,
        IEntityMapping<TEntity, TId> mapping,
        UnitOfWorkADO unitOfWork)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(mapping);
        ArgumentNullException.ThrowIfNull(unitOfWork);

        _connection = connection;
        _mapping = mapping;
        _unitOfWork = unitOfWork;
        _sqlBuilder = new SpecificationSqlBuilder<TEntity>(mapping.ColumnMappings);

        // Build property cache for entity materialization
        _propertyCache = typeof(TEntity).GetProperties()
            .Where(p => mapping.ColumnMappings.ContainsKey(p.Name))
            .ToDictionary(p => p.Name);

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
            using var command = CreateCommand(_selectByIdSql);
            AddParameter(command, "@Id", id);

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
            using var command = CreateCommand(_selectAllSql);
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
            var (sql, addParameters) = _sqlBuilder.BuildSelectStatement(_mapping.TableName, specification);

            using var command = CreateCommand(sql);
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
            var (whereClause, addParameters) = _sqlBuilder.BuildWhereClause(specification);
            var columns = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"\"{c}\""));
            var sql = $"SELECT {columns} FROM \"{_mapping.TableName}\" {whereClause} LIMIT 1";

            using var command = CreateCommand(sql);
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
            using var command = CreateCommand(_countSql);
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
            var (whereClause, addParameters) = _sqlBuilder.BuildWhereClause(specification);
            var sql = $"SELECT COUNT(*) FROM \"{_mapping.TableName}\" {whereClause}";

            using var command = CreateCommand(sql);
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
            var (whereClause, addParameters) = _sqlBuilder.BuildWhereClause(specification);
            var sql = $"SELECT EXISTS (SELECT 1 FROM \"{_mapping.TableName}\" {whereClause})";

            using var command = CreateCommand(sql);
            addParameters(command);

            var result = await ExecuteScalarAsync(command, cancellationToken).ConfigureAwait(false);
            return Right<EncinaError, bool>(Convert.ToBoolean(result, CultureInfo.InvariantCulture));
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
            using var command = CreateCommand(_existsSql);
            var result = await ExecuteScalarAsync(command, cancellationToken).ConfigureAwait(false);
            return Right<EncinaError, bool>(Convert.ToBoolean(result, CultureInfo.InvariantCulture));
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
            using var command = CreateCommand(_insertSql);
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
            using var command = CreateCommand(_updateSql);
            AddEntityParameters(command, entity, forInsert: false);

            // Add ID parameter for WHERE clause
            var id = _mapping.GetId(entity);
            AddParameter(command, "@Id", id);

            var rowsAffected = await ExecuteNonQueryAsync(command, cancellationToken).ConfigureAwait(false);

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
            using var command = CreateCommand(_deleteByIdSql);
            AddParameter(command, "@Id", id);

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
            foreach (var entity in entityList)
            {
                using var command = CreateCommand(_insertSql);
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
                using var command = CreateCommand(_updateSql);
                AddEntityParameters(command, entity, forInsert: false);

                var id = _mapping.GetId(entity);
                AddParameter(command, "@Id", id);

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
            var (whereClause, addParameters) = _sqlBuilder.BuildWhereClause(specification);

            if (string.IsNullOrWhiteSpace(whereClause))
            {
                return Left<EncinaError, int>(
                    RepositoryErrors.InvalidOperation<TEntity>("DeleteRange", "DELETE requires a WHERE clause to prevent accidental data loss."));
            }

            var sql = $"DELETE FROM \"{_mapping.TableName}\" {whereClause}";

            using var command = CreateCommand(sql);
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

    #endregion

    #region SQL Generation

    private string BuildSelectByIdSql()
    {
        var columns = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"\"{c}\""));
        return $"SELECT {columns} FROM \"{_mapping.TableName}\" WHERE \"{_mapping.IdColumnName}\" = @Id";
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
        var parameters = string.Join(", ", insertableProperties.Select(kvp => $"@{kvp.Key}"));

        return $"INSERT INTO \"{_mapping.TableName}\" ({columns}) VALUES ({parameters})";
    }

    private string BuildUpdateSql()
    {
        var updatableProperties = _mapping.ColumnMappings
            .Where(kvp => !_mapping.UpdateExcludedProperties.Contains(kvp.Key))
            .ToList();

        var setClauses = string.Join(", ", updatableProperties.Select(kvp => $"\"{kvp.Value}\" = @{kvp.Key}"));

        return $"UPDATE \"{_mapping.TableName}\" SET {setClauses} WHERE \"{_mapping.IdColumnName}\" = @Id";
    }

    private string BuildDeleteByIdSql()
    {
        return $"DELETE FROM \"{_mapping.TableName}\" WHERE \"{_mapping.IdColumnName}\" = @Id";
    }

    private string BuildCountSql()
    {
        return $"SELECT COUNT(*) FROM \"{_mapping.TableName}\"";
    }

    private string BuildExistsSql()
    {
        return $"SELECT EXISTS (SELECT 1 FROM \"{_mapping.TableName}\")";
    }

    #endregion

    #region Entity Materialization

    private TEntity MapReaderToEntity(IDataReader reader)
    {
        var entity = Activator.CreateInstance<TEntity>()
            ?? throw new InvalidOperationException(
                $"Cannot create instance of {typeof(TEntity).Name}. " +
                $"Ensure the type has a public parameterless constructor.");

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

        // PostgreSQL has native GUID support, no conversion needed
        if (underlyingType == typeof(Guid) && value is Guid)
        {
            return value;
        }

        if (underlyingType == value.GetType())
            return value;

        if (underlyingType.IsEnum)
            return Enum.ToObject(underlyingType, value);

        // PostgreSQL has native boolean type
        if (underlyingType == typeof(bool) && value is bool)
        {
            return value;
        }

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

    #region Command and Parameter Helpers

    private IDbCommand CreateCommand(string sql)
    {
        var command = _connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = _unitOfWork.CurrentTransaction;
        return command;
    }

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
                // PostgreSQL has native GUID and boolean support, no conversion needed
                AddParameter(command, $"@{propertyName}", value);
            }
        }
    }

    #endregion

    #region Async Helpers

    private static async Task<IDataReader> ExecuteReaderAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is NpgsqlCommand npgsqlCommand)
            return await npgsqlCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        return await Task.Run(command.ExecuteReader, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<int> ExecuteNonQueryAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is NpgsqlCommand npgsqlCommand)
            return await npgsqlCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        return await Task.Run(command.ExecuteNonQuery, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<object?> ExecuteScalarAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is NpgsqlCommand npgsqlCommand)
            return await npgsqlCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

        return await Task.Run(command.ExecuteScalar, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<bool> ReadAsync(IDataReader reader, CancellationToken cancellationToken)
    {
        if (reader is NpgsqlDataReader npgsqlReader)
            return await npgsqlReader.ReadAsync(cancellationToken).ConfigureAwait(false);

        return await Task.Run(reader.Read, cancellationToken).ConfigureAwait(false);
    }

    private static bool IsDuplicateKeyException(Exception ex)
    {
        // PostgreSQL unique violation error code is 23505
        if (ex is PostgresException pgEx)
        {
            return pgEx.SqlState == "23505";
        }

        var message = ex.Message;
        return message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
            || message.Contains("23505", StringComparison.OrdinalIgnoreCase)
            || message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
