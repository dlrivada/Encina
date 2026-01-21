using System.Linq.Expressions;

namespace Encina.MongoDB.Tenancy;

/// <summary>
/// Fluent builder for configuring tenant-aware entity-to-collection mappings for MongoDB.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TId">The entity identifier type.</typeparam>
/// <remarks>
/// <para>
/// This builder provides a fluent API for configuring MongoDB
/// entity mappings with tenant-specific configuration options.
/// Use HasTenantId to configure the tenant ID property.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var mapping = new TenantEntityMappingBuilder&lt;Order, Guid&gt;()
///     .ToCollection("orders")
///     .HasId(o =&gt; o.Id)
///     .HasTenantId(o =&gt; o.TenantId)
///     .MapField(o =&gt; o.CustomerId)
///     .MapField(o =&gt; o.Total)
///     .Build();
/// </code>
/// </example>
public sealed class TenantEntityMappingBuilder<TEntity, TId>
    where TEntity : class
    where TId : notnull
{
    private string? _collectionName;
    private string? _idFieldName;
    private string? _idPropertyName;
    private Func<TEntity, TId>? _idSelector;
    private string? _tenantFieldName;
    private string? _tenantPropertyName;
    private Func<TEntity, string?>? _tenantIdGetter;
    private Action<TEntity, string>? _tenantIdSetter;
    private readonly Dictionary<string, string> _fieldMappings = new();

    /// <summary>
    /// Specifies the MongoDB collection name for this entity.
    /// </summary>
    /// <param name="collectionName">The collection name.</param>
    /// <returns>This builder for chaining.</returns>
    public TenantEntityMappingBuilder<TEntity, TId> ToCollection(string collectionName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(collectionName);
        _collectionName = collectionName;
        return this;
    }

    /// <summary>
    /// Specifies the primary key property and its field name.
    /// </summary>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="propertySelector">Expression selecting the ID property.</param>
    /// <param name="fieldName">Optional field name (defaults to "_id" for MongoDB convention).</param>
    /// <returns>This builder for chaining.</returns>
    public TenantEntityMappingBuilder<TEntity, TId> HasId<TProperty>(
        Expression<Func<TEntity, TProperty>> propertySelector,
        string? fieldName = null)
    {
        ArgumentNullException.ThrowIfNull(propertySelector);

        var propertyName = GetPropertyName(propertySelector);
        _idPropertyName = propertyName;
        _idFieldName = fieldName ?? "_id";

        var compiled = propertySelector.Compile();
        _idSelector = entity => (TId)(object)compiled(entity)!;

        _fieldMappings[propertyName] = _idFieldName;

        return this;
    }

    /// <summary>
    /// Specifies the tenant ID property and its field name.
    /// </summary>
    /// <param name="propertySelector">Expression selecting the tenant ID property.</param>
    /// <param name="fieldName">Optional field name (defaults to property name).</param>
    /// <returns>This builder for chaining.</returns>
    /// <remarks>
    /// <para>
    /// The tenant ID property must be of type <see cref="string"/>.
    /// This method also adds the property to the field mappings.
    /// </para>
    /// </remarks>
    public TenantEntityMappingBuilder<TEntity, TId> HasTenantId(
        Expression<Func<TEntity, string>> propertySelector,
        string? fieldName = null)
    {
        ArgumentNullException.ThrowIfNull(propertySelector);

        var propertyName = GetPropertyName(propertySelector);
        _tenantPropertyName = propertyName;
        _tenantFieldName = fieldName ?? propertyName;

        // Create getter
        var compiled = propertySelector.Compile();
        _tenantIdGetter = compiled;

        // Create setter using property info
        var memberExpression = propertySelector.Body as MemberExpression
            ?? throw new ArgumentException("Expression must be a property accessor", nameof(propertySelector));

        var propertyInfo = memberExpression.Member as System.Reflection.PropertyInfo
            ?? throw new ArgumentException("Expression must access a property", nameof(propertySelector));

        _tenantIdSetter = (entity, value) => propertyInfo.SetValue(entity, value);

        // Add to field mappings
        _fieldMappings[propertyName] = _tenantFieldName;

        return this;
    }

    /// <summary>
    /// Maps a property to a MongoDB field.
    /// </summary>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="propertySelector">Expression selecting the property.</param>
    /// <param name="fieldName">Optional field name (defaults to property name).</param>
    /// <returns>This builder for chaining.</returns>
    public TenantEntityMappingBuilder<TEntity, TId> MapField<TProperty>(
        Expression<Func<TEntity, TProperty>> propertySelector,
        string? fieldName = null)
    {
        ArgumentNullException.ThrowIfNull(propertySelector);

        var propertyName = GetPropertyName(propertySelector);
        var validatedFieldName = fieldName ?? propertyName;

        _fieldMappings[propertyName] = validatedFieldName;

        return this;
    }

    /// <summary>
    /// Builds the tenant-aware entity mapping configuration.
    /// </summary>
    /// <returns>The configured entity mapping.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when required configuration is missing.
    /// </exception>
    public ITenantEntityMapping<TEntity, TId> Build()
    {
        if (string.IsNullOrWhiteSpace(_collectionName))
        {
            throw new InvalidOperationException(
                $"Collection name must be specified. Call {nameof(ToCollection)}() before {nameof(Build)}().");
        }

        if (_idSelector is null || string.IsNullOrWhiteSpace(_idFieldName))
        {
            throw new InvalidOperationException(
                $"Primary key must be specified. Call {nameof(HasId)}() before {nameof(Build)}().");
        }

        if (_fieldMappings.Count == 0)
        {
            throw new InvalidOperationException(
                $"At least one field mapping is required. Call {nameof(MapField)}() or {nameof(HasId)}() before {nameof(Build)}().");
        }

        return new TenantEntityMapping<TEntity, TId>(
            _collectionName,
            _idFieldName,
            _idSelector,
            new Dictionary<string, string>(_fieldMappings),
            _tenantFieldName,
            _tenantPropertyName,
            _tenantIdGetter,
            _tenantIdSetter);
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
/// Internal implementation of <see cref="ITenantEntityMapping{TEntity, TId}"/> for MongoDB.
/// </summary>
internal sealed class TenantEntityMapping<TEntity, TId> : ITenantEntityMapping<TEntity, TId>
    where TEntity : class
    where TId : notnull
{
    private readonly Func<TEntity, TId> _idSelector;
    private readonly Func<TEntity, string?>? _tenantIdGetter;
    private readonly Action<TEntity, string>? _tenantIdSetter;

    public TenantEntityMapping(
        string collectionName,
        string idFieldName,
        Func<TEntity, TId> idSelector,
        Dictionary<string, string> fieldMappings,
        string? tenantFieldName,
        string? tenantPropertyName,
        Func<TEntity, string?>? tenantIdGetter,
        Action<TEntity, string>? tenantIdSetter)
    {
        CollectionName = collectionName;
        IdFieldName = idFieldName;
        _idSelector = idSelector;
        FieldMappings = fieldMappings;
        TenantFieldName = tenantFieldName;
        TenantPropertyName = tenantPropertyName;
        _tenantIdGetter = tenantIdGetter;
        _tenantIdSetter = tenantIdSetter;
    }

    public string CollectionName { get; }
    public string IdFieldName { get; }
    public IReadOnlyDictionary<string, string> FieldMappings { get; }
    public bool IsTenantEntity => TenantFieldName is not null;
    public string? TenantFieldName { get; }
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
