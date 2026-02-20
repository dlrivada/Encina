using System.Linq.Expressions;
using Encina.ADO.SqlServer.Repository;
using Encina.Messaging;
using LanguageExt;

namespace Encina.ADO.SqlServer.Tenancy;

/// <summary>
/// Fluent builder for configuring tenant-aware entity-to-table mappings for ADO.NET.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TId">The entity identifier type.</typeparam>
/// <remarks>
/// <para>
/// This builder extends EntityMappingBuilder with
/// tenant-specific configuration options. Use HasTenantId
/// to configure the tenant ID property.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var mapping = new TenantEntityMappingBuilder&lt;Order, Guid&gt;()
///     .ToTable("Orders")
///     .HasId(o =&gt; o.Id)
///     .HasTenantId(o =&gt; o.TenantId)
///     .MapProperty(o =&gt; o.CustomerId)
///     .MapProperty(o =&gt; o.Total)
///     .Build(); // Returns Either&lt;EncinaError, ITenantEntityMapping&lt;TEntity, TId&gt;&gt;
/// </code>
/// </example>
public sealed class TenantEntityMappingBuilder<TEntity, TId>
    where TEntity : class
    where TId : notnull
{
    private string? _tableName;
    private string? _idColumnName;
    private string? _idPropertyName;
    private Func<TEntity, TId>? _idSelector;
    private string? _tenantColumnName;
    private string? _tenantPropertyName;
    private Func<TEntity, string?>? _tenantIdGetter;
    private Action<TEntity, string>? _tenantIdSetter;
    private readonly Dictionary<string, string> _columnMappings = new();
    private readonly System.Collections.Generic.HashSet<string> _insertExcluded = [];
    private readonly System.Collections.Generic.HashSet<string> _updateExcluded = [];

    /// <summary>
    /// Specifies the database table name for this entity.
    /// </summary>
    /// <param name="tableName">The table name (can include schema prefix).</param>
    /// <returns>This builder for chaining.</returns>
    public TenantEntityMappingBuilder<TEntity, TId> ToTable(string tableName)
    {
        _tableName = SqlIdentifierValidator.ValidateTableName(tableName);
        return this;
    }

    /// <summary>
    /// Specifies the primary key property and its column name.
    /// </summary>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="propertySelector">Expression selecting the ID property.</param>
    /// <param name="columnName">Optional column name (defaults to property name).</param>
    /// <returns>This builder for chaining.</returns>
    public TenantEntityMappingBuilder<TEntity, TId> HasId<TProperty>(
        Expression<Func<TEntity, TProperty>> propertySelector,
        string? columnName = null)
    {
        ArgumentNullException.ThrowIfNull(propertySelector);

        var propertyName = GetPropertyName(propertySelector);
        _idPropertyName = propertyName;
        _idColumnName = SqlIdentifierValidator.ValidateTableName(columnName ?? propertyName, nameof(columnName));

        var compiled = propertySelector.Compile();
        _idSelector = entity => (TId)(object)compiled(entity)!;

        _columnMappings[propertyName] = _idColumnName;
        _updateExcluded.Add(propertyName);

        return this;
    }

    /// <summary>
    /// Specifies the tenant ID property and its column name.
    /// </summary>
    /// <param name="propertySelector">Expression selecting the tenant ID property.</param>
    /// <param name="columnName">Optional column name (defaults to property name).</param>
    /// <returns>This builder for chaining.</returns>
    /// <remarks>
    /// <para>
    /// The tenant ID property must be of type <see cref="string"/>.
    /// This method also adds the property to the column mappings and excludes
    /// it from UPDATE operations (tenant ID should not be modified after creation).
    /// </para>
    /// </remarks>
    public TenantEntityMappingBuilder<TEntity, TId> HasTenantId(
        Expression<Func<TEntity, string>> propertySelector,
        string? columnName = null)
    {
        ArgumentNullException.ThrowIfNull(propertySelector);

        var propertyName = GetPropertyName(propertySelector);
        _tenantPropertyName = propertyName;
        _tenantColumnName = SqlIdentifierValidator.ValidateTableName(columnName ?? propertyName, nameof(columnName));

        // Create getter
        var compiled = propertySelector.Compile();
        _tenantIdGetter = compiled;

        // Create setter using property info
        var memberExpression = propertySelector.Body as MemberExpression
            ?? throw new ArgumentException("Expression must be a property accessor", nameof(propertySelector));

        var propertyInfo = memberExpression.Member as System.Reflection.PropertyInfo
            ?? throw new ArgumentException("Expression must access a property", nameof(propertySelector));

        _tenantIdSetter = (entity, value) => propertyInfo.SetValue(entity, value);

        // Add to column mappings
        _columnMappings[propertyName] = _tenantColumnName;

        // Tenant ID should not be modified after creation
        _updateExcluded.Add(propertyName);

        return this;
    }

    /// <summary>
    /// Maps a property to a database column.
    /// </summary>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="propertySelector">Expression selecting the property.</param>
    /// <param name="columnName">Optional column name (defaults to property name).</param>
    /// <returns>This builder for chaining.</returns>
    public TenantEntityMappingBuilder<TEntity, TId> MapProperty<TProperty>(
        Expression<Func<TEntity, TProperty>> propertySelector,
        string? columnName = null)
    {
        ArgumentNullException.ThrowIfNull(propertySelector);

        var propertyName = GetPropertyName(propertySelector);
        var validatedColumnName = SqlIdentifierValidator.ValidateTableName(columnName ?? propertyName, nameof(columnName));

        _columnMappings[propertyName] = validatedColumnName;

        return this;
    }

    /// <summary>
    /// Excludes a property from INSERT operations.
    /// </summary>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="propertySelector">Expression selecting the property to exclude.</param>
    /// <returns>This builder for chaining.</returns>
    public TenantEntityMappingBuilder<TEntity, TId> ExcludeFromInsert<TProperty>(
        Expression<Func<TEntity, TProperty>> propertySelector)
    {
        ArgumentNullException.ThrowIfNull(propertySelector);

        var propertyName = GetPropertyName(propertySelector);
        _insertExcluded.Add(propertyName);

        return this;
    }

    /// <summary>
    /// Excludes a property from UPDATE operations.
    /// </summary>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="propertySelector">Expression selecting the property to exclude.</param>
    /// <returns>This builder for chaining.</returns>
    public TenantEntityMappingBuilder<TEntity, TId> ExcludeFromUpdate<TProperty>(
        Expression<Func<TEntity, TProperty>> propertySelector)
    {
        ArgumentNullException.ThrowIfNull(propertySelector);

        var propertyName = GetPropertyName(propertySelector);
        _updateExcluded.Add(propertyName);

        return this;
    }

    /// <summary>
    /// Builds the tenant-aware entity mapping configuration.
    /// </summary>
    /// <returns>Either the configured entity mapping or a validation error.</returns>
    public Either<EncinaError, ITenantEntityMapping<TEntity, TId>> Build()
    {
        if (string.IsNullOrWhiteSpace(_tableName))
        {
            return EncinaErrors.Create(
                EntityMappingErrorCodes.MissingTableName,
                $"Table name must be specified. Call {nameof(ToTable)}() before {nameof(Build)}().");
        }

        if (_idSelector is null || string.IsNullOrWhiteSpace(_idColumnName))
        {
            return EncinaErrors.Create(
                EntityMappingErrorCodes.MissingPrimaryKey,
                $"Primary key must be specified. Call {nameof(HasId)}() before {nameof(Build)}().");
        }

        if (_columnMappings.Count == 0)
        {
            return EncinaErrors.Create(
                EntityMappingErrorCodes.MissingColumnMappings,
                $"At least one column mapping is required. Call {nameof(MapProperty)}() or {nameof(HasId)}() before {nameof(Build)}().");
        }

        return Either<EncinaError, ITenantEntityMapping<TEntity, TId>>.Right(
            new TenantEntityMapping<TEntity, TId>(
                _tableName,
                _idColumnName,
                _idSelector,
                new Dictionary<string, string>(_columnMappings),
                new System.Collections.Generic.HashSet<string>(_insertExcluded),
                new System.Collections.Generic.HashSet<string>(_updateExcluded),
                _tenantColumnName,
                _tenantPropertyName,
                _tenantIdGetter,
                _tenantIdSetter));
    }

    private static string GetPropertyName<TProperty>(Expression<Func<TEntity, TProperty>> propertySelector)
    {
        if (propertySelector.Body is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }

        if (propertySelector.Body is UnaryExpression { Operand: MemberExpression unaryMember })
        {
            return unaryMember.Member.Name;
        }

        throw new ArgumentException(
            "Expression must be a property accessor (e.g., x => x.PropertyName)",
            nameof(propertySelector));
    }
}

/// <summary>
/// Internal implementation of <see cref="ITenantEntityMapping{TEntity, TId}"/> for ADO.NET.
/// </summary>
internal sealed class TenantEntityMapping<TEntity, TId> : ITenantEntityMapping<TEntity, TId>
    where TEntity : class
    where TId : notnull
{
    private readonly Func<TEntity, TId> _idSelector;
    private readonly Func<TEntity, string?>? _tenantIdGetter;
    private readonly Action<TEntity, string>? _tenantIdSetter;

    public TenantEntityMapping(
        string tableName,
        string idColumnName,
        Func<TEntity, TId> idSelector,
        Dictionary<string, string> columnMappings,
        System.Collections.Generic.HashSet<string> insertExcluded,
        System.Collections.Generic.HashSet<string> updateExcluded,
        string? tenantColumnName,
        string? tenantPropertyName,
        Func<TEntity, string?>? tenantIdGetter,
        Action<TEntity, string>? tenantIdSetter)
    {
        TableName = tableName;
        IdColumnName = idColumnName;
        _idSelector = idSelector;
        ColumnMappings = columnMappings;
        InsertExcludedProperties = insertExcluded;
        UpdateExcludedProperties = updateExcluded;
        TenantColumnName = tenantColumnName;
        TenantPropertyName = tenantPropertyName;
        _tenantIdGetter = tenantIdGetter;
        _tenantIdSetter = tenantIdSetter;
    }

    public string TableName { get; }
    public string IdColumnName { get; }
    public IReadOnlyDictionary<string, string> ColumnMappings { get; }
    public IReadOnlySet<string> InsertExcludedProperties { get; }
    public IReadOnlySet<string> UpdateExcludedProperties { get; }
    public bool IsTenantEntity => TenantColumnName is not null;
    public string? TenantColumnName { get; }
    public string? TenantPropertyName { get; }

    public TId GetId(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return _idSelector(entity);
    }

    public string? GetTenantId(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return _tenantIdGetter?.Invoke(entity);
    }

    public void SetTenantId(TEntity entity, string tenantId)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(tenantId);

        if (_tenantIdSetter is null)
        {
            throw new InvalidOperationException(
                $"Cannot set tenant ID on entity {typeof(TEntity).Name} because it is not configured as a tenant entity.");
        }

        _tenantIdSetter(entity, tenantId);
    }
}
