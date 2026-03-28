using Encina.Dapper.Sqlite.Repository;
using Encina.DomainModeling;
using Encina.Messaging;
using Encina.Messaging.SoftDelete;

namespace Encina.Dapper.Sqlite.SoftDelete;

/// <summary>
/// Soft-delete-aware SQL builder for SQLite that automatically prepends soft delete filters to queries.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <remarks>
/// <para>
/// This builder extends <see cref="SpecificationSqlBuilder{TEntity}"/> functionality
/// to automatically add <c>WHERE "IsDeleted" = 0</c> to all queries when
/// operating on soft-deletable entities.
/// </para>
/// <para>
/// <b>SQLite-specific syntax:</b>
/// <list type="bullet">
/// <item>Uses double-quotes for identifiers: "ColumnName"</item>
/// <item>Boolean values as 0/1 (SQLite has no native boolean type)</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var spec = new ActiveOrdersSpec(); // o => o.Status == "Active"
/// var builder = new SoftDeleteSpecificationSqlBuilder&lt;Order&gt;(mapping, options);
/// var (whereClause, parameters) = builder.BuildWhereClause(spec);
/// // whereClause: "WHERE \"IsDeleted\" = 0 AND (\"Status\" = @p0)"
/// </code>
/// </example>
public sealed class SoftDeleteSpecificationSqlBuilder<TEntity>
    where TEntity : class
{
    private readonly SpecificationSqlBuilder<TEntity> _innerBuilder;
    private readonly ISoftDeleteEntityMapping<TEntity, object> _mapping;
    private readonly SoftDeleteOptions _options;
    private readonly bool _includeSoftDeleted;

    /// <summary>
    /// Initializes a new instance of the <see cref="SoftDeleteSpecificationSqlBuilder{TEntity}"/> class.
    /// </summary>
    /// <param name="mapping">The soft-delete-aware entity mapping.</param>
    /// <param name="options">The soft delete options.</param>
    public SoftDeleteSpecificationSqlBuilder(
        ISoftDeleteEntityMapping<TEntity, object> mapping,
        SoftDeleteOptions options)
        : this(mapping, options, includeSoftDeleted: false)
    {
    }

    private SoftDeleteSpecificationSqlBuilder(
        ISoftDeleteEntityMapping<TEntity, object> mapping,
        SoftDeleteOptions options,
        bool includeSoftDeleted)
    {
        ArgumentNullException.ThrowIfNull(mapping);
        ArgumentNullException.ThrowIfNull(options);

        _mapping = mapping;
        _options = options;
        _includeSoftDeleted = includeSoftDeleted;
        _innerBuilder = new SpecificationSqlBuilder<TEntity>(mapping.ColumnMappings);
    }

    /// <summary>
    /// Creates a new builder that includes soft-deleted entities in queries.
    /// </summary>
    /// <returns>A new builder with soft delete filtering disabled.</returns>
    public SoftDeleteSpecificationSqlBuilder<TEntity> IncludeDeleted()
    {
        return new SoftDeleteSpecificationSqlBuilder<TEntity>(_mapping, _options, includeSoftDeleted: true);
    }

    /// <summary>
    /// Builds a WHERE clause from a specification with soft delete filtering.
    /// </summary>
    /// <param name="specification">The specification to translate.</param>
    /// <returns>A tuple containing the WHERE clause and parameters dictionary.</returns>
    public (string WhereClause, IDictionary<string, object?> Parameters) BuildWhereClause(
        Specification<TEntity> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        var (innerWhere, parameters) = _innerBuilder.BuildWhereClause(specification);
        return ApplySoftDeleteFilter(innerWhere, parameters);
    }

    /// <summary>
    /// Builds a WHERE clause from a query specification with soft delete filtering.
    /// </summary>
    /// <param name="specification">The query specification to translate.</param>
    /// <returns>A tuple containing the WHERE clause and parameters dictionary.</returns>
    public (string WhereClause, IDictionary<string, object?> Parameters) BuildWhereClause(
        QuerySpecification<TEntity> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        var (innerWhere, parameters) = _innerBuilder.BuildWhereClause(specification);
        return ApplySoftDeleteFilter(innerWhere, parameters);
    }

    /// <summary>
    /// Builds an ORDER BY clause from a query specification.
    /// </summary>
    /// <param name="specification">The query specification to translate.</param>
    /// <returns>The ORDER BY clause.</returns>
    public string BuildOrderByClause(IQuerySpecification<TEntity> specification)
    {
        return _innerBuilder.BuildOrderByClause(specification);
    }

    /// <summary>
    /// Builds a pagination clause from a query specification.
    /// </summary>
    /// <param name="specification">The query specification to translate.</param>
    /// <returns>The pagination clause (LIMIT/OFFSET for SQLite).</returns>
    public string BuildPaginationClause(IQuerySpecification<TEntity> specification)
    {
        return _innerBuilder.BuildPaginationClause(specification);
    }

    /// <summary>
    /// Builds a complete SELECT statement with soft delete filtering.
    /// </summary>
    /// <param name="tableName">The validated table name.</param>
    /// <returns>A tuple containing the SQL statement and parameters dictionary.</returns>
    public (string Sql, IDictionary<string, object?> Parameters) BuildSelectStatement(string tableName)
    {
        var validatedTableName = SqlIdentifierValidator.ValidateTableName(tableName);
        var columns = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"\"{c}\""));

        var parameters = new Dictionary<string, object?>();
        var (whereClause, _) = ApplySoftDeleteFilter(string.Empty, parameters);

        var sql = string.IsNullOrWhiteSpace(whereClause)
            ? $"SELECT {columns} FROM {validatedTableName}"
            : $"SELECT {columns} FROM {validatedTableName} {whereClause}";

        return (sql, parameters);
    }

    /// <summary>
    /// Builds a complete SELECT statement with a WHERE clause from a specification.
    /// </summary>
    /// <param name="tableName">The validated table name.</param>
    /// <param name="specification">The specification for filtering.</param>
    /// <returns>A tuple containing the SQL statement and parameters dictionary.</returns>
    public (string Sql, IDictionary<string, object?> Parameters) BuildSelectStatement(
        string tableName,
        Specification<TEntity> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        var validatedTableName = SqlIdentifierValidator.ValidateTableName(tableName);
        var columns = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"\"{c}\""));

        var (whereClause, parameters) = BuildWhereClause(specification);
        var sql = $"SELECT {columns} FROM {validatedTableName} {whereClause}".Trim();

        return (sql, parameters);
    }

    /// <summary>
    /// Builds a complete SELECT statement from a query specification with all features.
    /// </summary>
    /// <param name="tableName">The validated table name.</param>
    /// <param name="specification">The query specification for filtering, ordering, and pagination.</param>
    /// <returns>A tuple containing the SQL statement and parameters dictionary.</returns>
    public (string Sql, IDictionary<string, object?> Parameters) BuildSelectStatement(
        string tableName,
        QuerySpecification<TEntity> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        var validatedTableName = SqlIdentifierValidator.ValidateTableName(tableName);
        var columns = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"\"{c}\""));

        var (whereClause, parameters) = BuildWhereClause(specification);
        var orderByClause = BuildOrderByClause(specification);
        var paginationClause = BuildPaginationClause(specification);

        var sqlParts = new List<string>
        {
            $"SELECT {columns} FROM {validatedTableName}"
        };

        if (!string.IsNullOrWhiteSpace(whereClause))
        {
            sqlParts.Add(whereClause);
        }

        if (!string.IsNullOrWhiteSpace(orderByClause))
        {
            sqlParts.Add(orderByClause);
        }

        if (!string.IsNullOrWhiteSpace(paginationClause))
        {
            sqlParts.Add(paginationClause);
        }

        var sql = string.Join(" ", sqlParts);

        return (sql, parameters);
    }

    /// <summary>
    /// Applies soft delete filter to the WHERE clause if the entity is soft-deletable.
    /// </summary>
    private (string WhereClause, IDictionary<string, object?> Parameters) ApplySoftDeleteFilter(
        string existingWhere,
        IDictionary<string, object?> parameters)
    {
        if (!_mapping.IsSoftDeletable ||
            !_options.AutoFilterSoftDeletedQueries ||
            _includeSoftDeleted)
        {
            return (existingWhere, parameters);
        }

        // SQLite uses 0/1 for boolean values (no native boolean type)
        var softDeleteFilter = $"\"{_mapping.IsDeletedColumnName}\" = 0";

        if (string.IsNullOrWhiteSpace(existingWhere))
        {
            return ($"WHERE {softDeleteFilter}", parameters);
        }

        var existingCondition = existingWhere.StartsWith("WHERE ", StringComparison.OrdinalIgnoreCase)
            ? existingWhere[6..]
            : existingWhere;

        return ($"WHERE {softDeleteFilter} AND ({existingCondition})", parameters);
    }
}
