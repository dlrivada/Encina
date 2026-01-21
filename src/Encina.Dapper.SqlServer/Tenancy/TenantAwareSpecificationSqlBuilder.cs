using Encina.Dapper.SqlServer.Repository;
using Encina.DomainModeling;
using Encina.Messaging;
using Encina.Tenancy;

namespace Encina.Dapper.SqlServer.Tenancy;

/// <summary>
/// Tenant-aware SQL builder that automatically prepends tenant filters to queries.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <remarks>
/// <para>
/// This builder extends <see cref="SpecificationSqlBuilder{TEntity}"/> functionality
/// to automatically add <c>WHERE [TenantId] = @tenantId</c> to all queries when
/// operating on tenant-scoped entities.
/// </para>
/// <para>
/// <b>Automatic Tenant Filtering:</b>
/// <list type="bullet">
/// <item>All SELECT queries include tenant filter</item>
/// <item>DELETE with specification includes tenant filter</item>
/// <item>COUNT queries include tenant filter</item>
/// <item>EXISTS checks include tenant filter</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var spec = new ActiveOrdersSpec(); // o => o.Status == "Active"
/// var builder = new TenantAwareSpecificationSqlBuilder&lt;Order&gt;(mapping, tenantProvider, options);
/// var (whereClause, parameters) = builder.BuildWhereClause(spec);
/// // whereClause: "WHERE [TenantId] = @tenantId AND ([Status] = @p0)"
/// // parameters: { tenantId = "tenant-1", p0 = "Active" }
/// </code>
/// </example>
public sealed class TenantAwareSpecificationSqlBuilder<TEntity>
    where TEntity : class
{
    private readonly SpecificationSqlBuilder<TEntity> _innerBuilder;
    private readonly ITenantEntityMapping<TEntity, object> _mapping;
    private readonly ITenantProvider _tenantProvider;
    private readonly DapperTenancyOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantAwareSpecificationSqlBuilder{TEntity}"/> class.
    /// </summary>
    /// <param name="mapping">The tenant-aware entity mapping.</param>
    /// <param name="tenantProvider">The tenant provider for current tenant context.</param>
    /// <param name="options">The Dapper tenancy options.</param>
    public TenantAwareSpecificationSqlBuilder(
        ITenantEntityMapping<TEntity, object> mapping,
        ITenantProvider tenantProvider,
        DapperTenancyOptions options)
    {
        ArgumentNullException.ThrowIfNull(mapping);
        ArgumentNullException.ThrowIfNull(tenantProvider);
        ArgumentNullException.ThrowIfNull(options);

        _mapping = mapping;
        _tenantProvider = tenantProvider;
        _options = options;
        _innerBuilder = new SpecificationSqlBuilder<TEntity>(mapping.ColumnMappings);
    }

    /// <summary>
    /// Builds a WHERE clause from a specification with tenant filtering.
    /// </summary>
    /// <param name="specification">The specification to translate.</param>
    /// <returns>A tuple containing the WHERE clause and parameters dictionary.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no tenant context is available and <see cref="DapperTenancyOptions.ThrowOnMissingTenantContext"/> is <c>true</c>.
    /// </exception>
    public (string WhereClause, IDictionary<string, object?> Parameters) BuildWhereClause(
        Specification<TEntity> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        var (innerWhere, parameters) = _innerBuilder.BuildWhereClause(specification);
        return ApplyTenantFilter(innerWhere, parameters);
    }

    /// <summary>
    /// Builds a WHERE clause from a query specification with tenant filtering.
    /// </summary>
    /// <param name="specification">The query specification to translate.</param>
    /// <returns>A tuple containing the WHERE clause and parameters dictionary.</returns>
    public (string WhereClause, IDictionary<string, object?> Parameters) BuildWhereClause(
        QuerySpecification<TEntity> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        var (innerWhere, parameters) = _innerBuilder.BuildWhereClause(specification);
        return ApplyTenantFilter(innerWhere, parameters);
    }

    /// <summary>
    /// Builds an ORDER BY clause from a query specification.
    /// </summary>
    /// <param name="specification">The query specification to translate.</param>
    /// <returns>The ORDER BY clause.</returns>
    /// <remarks>
    /// ORDER BY does not require tenant filtering, so this delegates directly.
    /// </remarks>
    public string BuildOrderByClause(IQuerySpecification<TEntity> specification)
    {
        return _innerBuilder.BuildOrderByClause(specification);
    }

    /// <summary>
    /// Builds a pagination clause from a query specification.
    /// </summary>
    /// <param name="specification">The query specification to translate.</param>
    /// <returns>The pagination clause (OFFSET/FETCH).</returns>
    public string BuildPaginationClause(IQuerySpecification<TEntity> specification)
    {
        return _innerBuilder.BuildPaginationClause(specification);
    }

    /// <summary>
    /// Builds a complete SELECT statement with tenant filtering.
    /// </summary>
    /// <param name="tableName">The validated table name.</param>
    /// <returns>A tuple containing the SQL statement and parameters dictionary.</returns>
    public (string Sql, IDictionary<string, object?> Parameters) BuildSelectStatement(string tableName)
    {
        var validatedTableName = SqlIdentifierValidator.ValidateTableName(tableName);
        var columns = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"[{c}]"));

        var parameters = new Dictionary<string, object?>();
        var (whereClause, _) = ApplyTenantFilter(string.Empty, parameters);

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
        var columns = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"[{c}]"));

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
        var columns = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"[{c}]"));

        var (whereClause, parameters) = BuildWhereClause(specification);
        var orderByClause = BuildOrderByClause(specification);
        var paginationClause = BuildPaginationClause(specification);

        var needsOffsetZero = specification.KeysetPaginationEnabled &&
                              specification.Take.HasValue &&
                              !string.IsNullOrEmpty(orderByClause);

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

        if (needsOffsetZero)
        {
            sqlParts.Add("OFFSET 0 ROWS");
        }

        if (!string.IsNullOrWhiteSpace(paginationClause))
        {
            sqlParts.Add(paginationClause);
        }

        var sql = string.Join(" ", sqlParts);

        return (sql, parameters);
    }

    /// <summary>
    /// Applies tenant filter to the WHERE clause if the entity is tenant-scoped.
    /// </summary>
    private (string WhereClause, IDictionary<string, object?> Parameters) ApplyTenantFilter(
        string existingWhere,
        IDictionary<string, object?> parameters)
    {
        // If not a tenant entity or auto-filter is disabled, return as-is
        if (!_mapping.IsTenantEntity || !_options.AutoFilterTenantQueries)
        {
            return (existingWhere, parameters);
        }

        var tenantId = _tenantProvider.GetCurrentTenantId();

        // Handle missing tenant context
        if (string.IsNullOrEmpty(tenantId))
        {
            if (_options.ThrowOnMissingTenantContext)
            {
                throw new InvalidOperationException(
                    $"Cannot execute query on tenant entity {typeof(TEntity).Name} without tenant context. " +
                    $"Either set a current tenant or disable {nameof(DapperTenancyOptions.ThrowOnMissingTenantContext)}.");
            }

            // No tenant context and not throwing - return without filter
            return (existingWhere, parameters);
        }

        // Add tenant parameter
        parameters["tenantId"] = tenantId;

        var tenantFilter = $"[{_mapping.TenantColumnName}] = @tenantId";

        // Combine with existing WHERE clause
        if (string.IsNullOrWhiteSpace(existingWhere))
        {
            return ($"WHERE {tenantFilter}", parameters);
        }

        // existingWhere already contains "WHERE", so we need to extract the condition
        var existingCondition = existingWhere.StartsWith("WHERE ", StringComparison.OrdinalIgnoreCase)
            ? existingWhere[6..] // Remove "WHERE "
            : existingWhere;

        return ($"WHERE {tenantFilter} AND ({existingCondition})", parameters);
    }
}
