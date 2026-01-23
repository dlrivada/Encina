using System.Data;
using Encina.ADO.PostgreSQL.Repository;
using Encina.DomainModeling;
using Encina.Messaging;
using Encina.Tenancy;

namespace Encina.ADO.PostgreSQL.Tenancy;

/// <summary>
/// Tenant-aware SQL builder that automatically prepends tenant filters to queries for ADO.NET PostgreSQL.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <remarks>
/// <para>
/// This builder extends <see cref="SpecificationSqlBuilder{TEntity}"/> functionality
/// to automatically add <c>WHERE "TenantId" = @TenantId</c> to all queries when
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
/// var (whereClause, addParameters) = builder.BuildWhereClause(spec);
/// // whereClause: "WHERE \"TenantId\" = @TenantId AND (\"Status\" = @p0)"
/// // addParameters adds @TenantId and @p0 to the command
/// </code>
/// </example>
public sealed class TenantAwareSpecificationSqlBuilder<TEntity>
    where TEntity : class
{
    private readonly SpecificationSqlBuilder<TEntity> _innerBuilder;
    private readonly ITenantEntityMapping<TEntity, object> _mapping;
    private readonly ITenantProvider _tenantProvider;
    private readonly ADOTenancyOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantAwareSpecificationSqlBuilder{TEntity}"/> class.
    /// </summary>
    /// <param name="mapping">The tenant-aware entity mapping.</param>
    /// <param name="tenantProvider">The tenant provider for current tenant context.</param>
    /// <param name="options">The ADO tenancy options.</param>
    public TenantAwareSpecificationSqlBuilder(
        ITenantEntityMapping<TEntity, object> mapping,
        ITenantProvider tenantProvider,
        ADOTenancyOptions options)
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
    /// <returns>A tuple containing the WHERE clause and an action to add parameters to a command.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no tenant context is available and <see cref="ADOTenancyOptions.ThrowOnMissingTenantContext"/> is <c>true</c>.
    /// </exception>
    public (string WhereClause, Action<IDbCommand> AddParameters) BuildWhereClause(
        Specification<TEntity> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        var (innerWhere, innerAddParameters) = _innerBuilder.BuildWhereClause(specification);
        return ApplyTenantFilter(innerWhere, innerAddParameters);
    }

    /// <summary>
    /// Builds a WHERE clause from a query specification with tenant filtering.
    /// </summary>
    /// <param name="specification">The query specification to translate.</param>
    /// <returns>A tuple containing the WHERE clause and an action to add parameters to a command.</returns>
    public (string WhereClause, Action<IDbCommand> AddParameters) BuildWhereClause(
        QuerySpecification<TEntity> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        var (innerWhere, innerAddParameters) = _innerBuilder.BuildWhereClause(specification);
        return ApplyTenantFilter(innerWhere, innerAddParameters);
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
    /// <returns>The pagination clause (LIMIT/OFFSET).</returns>
    public string BuildPaginationClause(IQuerySpecification<TEntity> specification)
    {
        return _innerBuilder.BuildPaginationClause(specification);
    }

    /// <summary>
    /// Builds a complete SELECT statement with tenant filtering.
    /// </summary>
    /// <param name="tableName">The validated table name.</param>
    /// <returns>A tuple containing the SQL statement and an action to add parameters to a command.</returns>
    public (string Sql, Action<IDbCommand> AddParameters) BuildSelectStatement(string tableName)
    {
        var validatedTableName = SqlIdentifierValidator.ValidateTableName(tableName);
        var columns = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"\"{c}\""));

        var (whereClause, addParameters) = ApplyTenantFilter(string.Empty, _ => { });

        var sql = string.IsNullOrWhiteSpace(whereClause)
            ? $"SELECT {columns} FROM \"{validatedTableName}\""
            : $"SELECT {columns} FROM \"{validatedTableName}\" {whereClause}";

        return (sql, addParameters);
    }

    /// <summary>
    /// Builds a complete SELECT statement with a WHERE clause from a specification.
    /// </summary>
    /// <param name="tableName">The validated table name.</param>
    /// <param name="specification">The specification for filtering.</param>
    /// <returns>A tuple containing the SQL statement and an action to add parameters to a command.</returns>
    public (string Sql, Action<IDbCommand> AddParameters) BuildSelectStatement(
        string tableName,
        Specification<TEntity> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        var validatedTableName = SqlIdentifierValidator.ValidateTableName(tableName);
        var columns = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"\"{c}\""));

        var (whereClause, addParameters) = BuildWhereClause(specification);
        var sql = $"SELECT {columns} FROM \"{validatedTableName}\" {whereClause}".Trim();

        return (sql, addParameters);
    }

    /// <summary>
    /// Builds a complete SELECT statement from a query specification with all features.
    /// </summary>
    /// <param name="tableName">The validated table name.</param>
    /// <param name="specification">The query specification for filtering, ordering, and pagination.</param>
    /// <returns>A tuple containing the SQL statement and an action to add parameters to a command.</returns>
    public (string Sql, Action<IDbCommand> AddParameters) BuildSelectStatement(
        string tableName,
        QuerySpecification<TEntity> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        var validatedTableName = SqlIdentifierValidator.ValidateTableName(tableName);
        var columns = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"\"{c}\""));

        var (whereClause, addParameters) = BuildWhereClause(specification);
        var orderByClause = BuildOrderByClause(specification);
        var paginationClause = BuildPaginationClause(specification);

        var sqlParts = new List<string>
        {
            $"SELECT {columns} FROM \"{validatedTableName}\""
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

        return (sql, addParameters);
    }

    /// <summary>
    /// Applies tenant filter to the WHERE clause if the entity is tenant-scoped.
    /// </summary>
    private (string WhereClause, Action<IDbCommand> AddParameters) ApplyTenantFilter(
        string existingWhere,
        Action<IDbCommand> existingAddParameters)
    {
        // If not a tenant entity or auto-filter is disabled, return as-is
        if (!_mapping.IsTenantEntity || !_options.AutoFilterTenantQueries)
        {
            return (existingWhere, existingAddParameters);
        }

        var tenantId = _tenantProvider.GetCurrentTenantId();

        // Handle missing tenant context
        if (string.IsNullOrEmpty(tenantId))
        {
            if (_options.ThrowOnMissingTenantContext)
            {
                throw new InvalidOperationException(
                    $"Cannot execute query on tenant entity {typeof(TEntity).Name} without tenant context. " +
                    $"Either set a current tenant or disable {nameof(ADOTenancyOptions.ThrowOnMissingTenantContext)}.");
            }

            // No tenant context and not throwing - return without filter
            return (existingWhere, existingAddParameters);
        }

        // PostgreSQL uses double quotes for identifier quoting
        var tenantFilter = $"\"{_mapping.TenantColumnName}\" = @TenantId";

        // Combine with existing WHERE clause
        string whereClause;
        if (string.IsNullOrWhiteSpace(existingWhere))
        {
            whereClause = $"WHERE {tenantFilter}";
        }
        else
        {
            // existingWhere already contains "WHERE", so we need to extract the condition
            var existingCondition = existingWhere.StartsWith("WHERE ", StringComparison.OrdinalIgnoreCase)
                ? existingWhere[6..] // Remove "WHERE "
                : existingWhere;

            whereClause = $"WHERE {tenantFilter} AND ({existingCondition})";
        }

        // Create combined parameter action
        Action<IDbCommand> combinedAddParameters = command =>
        {
            // Add tenant parameter
            var tenantParam = command.CreateParameter();
            tenantParam.ParameterName = "@TenantId";
            tenantParam.Value = tenantId;
            command.Parameters.Add(tenantParam);

            // Add existing parameters
            existingAddParameters(command);
        };

        return (whereClause, combinedAddParameters);
    }
}
