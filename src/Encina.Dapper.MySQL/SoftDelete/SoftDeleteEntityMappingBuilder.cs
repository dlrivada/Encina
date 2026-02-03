using System.Linq.Expressions;
using Encina.Dapper.MySQL.Repository;
using Encina.Messaging;

namespace Encina.Dapper.MySQL.SoftDelete;

/// <summary>
/// Fluent builder for configuring soft-delete-aware entity-to-table mappings for MySQL.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TId">The entity identifier type.</typeparam>
/// <remarks>
/// <para>
/// This builder extends EntityMappingBuilder with
/// soft delete-specific configuration options. Use <see cref="HasSoftDelete"/>
/// to configure the <c>IsDeleted</c> property.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var mapping = new SoftDeleteEntityMappingBuilder&lt;Order, Guid&gt;()
///     .ToTable("Orders")
///     .HasId(o =&gt; o.Id)
///     .HasSoftDelete(o =&gt; o.IsDeleted)
///     .HasDeletedAt(o =&gt; o.DeletedAtUtc)
///     .HasDeletedBy(o =&gt; o.DeletedBy)
///     .MapProperty(o =&gt; o.CustomerId)
///     .MapProperty(o =&gt; o.Total)
///     .Build();
/// </code>
/// </example>
public sealed class SoftDeleteEntityMappingBuilder<TEntity, TId>
    where TEntity : class
    where TId : notnull
{
    private string? _tableName;
    private string? _idColumnName;
    private Func<TEntity, TId>? _idSelector;
    private string? _isDeletedColumnName;
    private string? _isDeletedPropertyName;
    private Func<TEntity, bool>? _isDeletedGetter;
    private Action<TEntity, bool>? _isDeletedSetter;
    private string? _deletedAtColumnName;
    private string? _deletedAtPropertyName;
    private Func<TEntity, DateTime?>? _deletedAtGetter;
    private Action<TEntity, DateTime?>? _deletedAtSetter;
    private string? _deletedByColumnName;
    private string? _deletedByPropertyName;
    private Func<TEntity, string?>? _deletedByGetter;
    private Action<TEntity, string?>? _deletedBySetter;
    private readonly Dictionary<string, string> _columnMappings = new();
    private readonly HashSet<string> _insertExcluded = [];
    private readonly HashSet<string> _updateExcluded = [];

    /// <summary>
    /// Specifies the database table name for this entity.
    /// </summary>
    /// <param name="tableName">The table name (can include schema prefix).</param>
    /// <returns>This builder for chaining.</returns>
    public SoftDeleteEntityMappingBuilder<TEntity, TId> ToTable(string tableName)
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
    public SoftDeleteEntityMappingBuilder<TEntity, TId> HasId<TProperty>(
        Expression<Func<TEntity, TProperty>> propertySelector,
        string? columnName = null)
    {
        ArgumentNullException.ThrowIfNull(propertySelector);

        var propertyName = GetPropertyName(propertySelector);
        _idColumnName = SqlIdentifierValidator.ValidateTableName(columnName ?? propertyName, nameof(columnName));

        var compiled = propertySelector.Compile();
        _idSelector = entity => (TId)(object)compiled(entity)!;

        _columnMappings[propertyName] = _idColumnName;
        _updateExcluded.Add(propertyName);

        return this;
    }

    /// <summary>
    /// Specifies the soft delete flag property and its column name.
    /// </summary>
    /// <param name="propertySelector">Expression selecting the <c>IsDeleted</c> property.</param>
    /// <param name="columnName">Optional column name (defaults to property name).</param>
    /// <returns>This builder for chaining.</returns>
    public SoftDeleteEntityMappingBuilder<TEntity, TId> HasSoftDelete(
        Expression<Func<TEntity, bool>> propertySelector,
        string? columnName = null)
    {
        ArgumentNullException.ThrowIfNull(propertySelector);

        var propertyName = GetPropertyName(propertySelector);
        _isDeletedPropertyName = propertyName;
        _isDeletedColumnName = SqlIdentifierValidator.ValidateTableName(columnName ?? propertyName, nameof(columnName));

        var compiled = propertySelector.Compile();
        _isDeletedGetter = compiled;

        var memberExpression = propertySelector.Body as MemberExpression
            ?? throw new ArgumentException("Expression must be a property accessor", nameof(propertySelector));

        var propertyInfo = memberExpression.Member as System.Reflection.PropertyInfo
            ?? throw new ArgumentException("Expression must access a property", nameof(propertySelector));

        _isDeletedSetter = (entity, value) => propertyInfo.SetValue(entity, value);

        _columnMappings[propertyName] = _isDeletedColumnName;

        return this;
    }

    /// <summary>
    /// Specifies the deletion timestamp property and its column name.
    /// </summary>
    /// <param name="propertySelector">Expression selecting the <c>DeletedAtUtc</c> property.</param>
    /// <param name="columnName">Optional column name (defaults to property name).</param>
    /// <returns>This builder for chaining.</returns>
    public SoftDeleteEntityMappingBuilder<TEntity, TId> HasDeletedAt(
        Expression<Func<TEntity, DateTime?>> propertySelector,
        string? columnName = null)
    {
        ArgumentNullException.ThrowIfNull(propertySelector);

        var propertyName = GetPropertyName(propertySelector);
        _deletedAtPropertyName = propertyName;
        _deletedAtColumnName = SqlIdentifierValidator.ValidateTableName(columnName ?? propertyName, nameof(columnName));

        var compiled = propertySelector.Compile();
        _deletedAtGetter = compiled;

        var memberExpression = propertySelector.Body as MemberExpression
            ?? throw new ArgumentException("Expression must be a property accessor", nameof(propertySelector));

        var propertyInfo = memberExpression.Member as System.Reflection.PropertyInfo
            ?? throw new ArgumentException("Expression must access a property", nameof(propertySelector));

        _deletedAtSetter = (entity, value) => propertyInfo.SetValue(entity, value);

        _columnMappings[propertyName] = _deletedAtColumnName;

        return this;
    }

    /// <summary>
    /// Specifies the "deleted by" user property and its column name.
    /// </summary>
    /// <param name="propertySelector">Expression selecting the <c>DeletedBy</c> property.</param>
    /// <param name="columnName">Optional column name (defaults to property name).</param>
    /// <returns>This builder for chaining.</returns>
    public SoftDeleteEntityMappingBuilder<TEntity, TId> HasDeletedBy(
        Expression<Func<TEntity, string?>> propertySelector,
        string? columnName = null)
    {
        ArgumentNullException.ThrowIfNull(propertySelector);

        var propertyName = GetPropertyName(propertySelector);
        _deletedByPropertyName = propertyName;
        _deletedByColumnName = SqlIdentifierValidator.ValidateTableName(columnName ?? propertyName, nameof(columnName));

        var compiled = propertySelector.Compile();
        _deletedByGetter = compiled;

        var memberExpression = propertySelector.Body as MemberExpression
            ?? throw new ArgumentException("Expression must be a property accessor", nameof(propertySelector));

        var propertyInfo = memberExpression.Member as System.Reflection.PropertyInfo
            ?? throw new ArgumentException("Expression must access a property", nameof(propertySelector));

        _deletedBySetter = (entity, value) => propertyInfo.SetValue(entity, value);

        _columnMappings[propertyName] = _deletedByColumnName;

        return this;
    }

    /// <summary>
    /// Maps a property to a database column.
    /// </summary>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="propertySelector">Expression selecting the property.</param>
    /// <param name="columnName">Optional column name (defaults to property name).</param>
    /// <returns>This builder for chaining.</returns>
    public SoftDeleteEntityMappingBuilder<TEntity, TId> MapProperty<TProperty>(
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
    public SoftDeleteEntityMappingBuilder<TEntity, TId> ExcludeFromInsert<TProperty>(
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
    public SoftDeleteEntityMappingBuilder<TEntity, TId> ExcludeFromUpdate<TProperty>(
        Expression<Func<TEntity, TProperty>> propertySelector)
    {
        ArgumentNullException.ThrowIfNull(propertySelector);

        var propertyName = GetPropertyName(propertySelector);
        _updateExcluded.Add(propertyName);

        return this;
    }

    /// <summary>
    /// Builds the soft-delete-aware entity mapping configuration.
    /// </summary>
    /// <returns>The configured entity mapping.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when required configuration is missing.
    /// </exception>
    public ISoftDeleteEntityMapping<TEntity, TId> Build()
    {
        if (string.IsNullOrWhiteSpace(_tableName))
        {
            throw new InvalidOperationException(
                $"Table name must be specified. Call {nameof(ToTable)}() before {nameof(Build)}().");
        }

        if (_idSelector is null || string.IsNullOrWhiteSpace(_idColumnName))
        {
            throw new InvalidOperationException(
                $"Primary key must be specified. Call {nameof(HasId)}() before {nameof(Build)}().");
        }

        if (_columnMappings.Count == 0)
        {
            throw new InvalidOperationException(
                $"At least one column mapping is required. Call {nameof(MapProperty)}() or {nameof(HasId)}() before {nameof(Build)}().");
        }

        return new SoftDeleteEntityMapping<TEntity, TId>(
            _tableName,
            _idColumnName,
            _idSelector,
            new Dictionary<string, string>(_columnMappings),
            new HashSet<string>(_insertExcluded),
            new HashSet<string>(_updateExcluded),
            _isDeletedColumnName,
            _isDeletedPropertyName,
            _isDeletedGetter,
            _isDeletedSetter,
            _deletedAtColumnName,
            _deletedAtPropertyName,
            _deletedAtGetter,
            _deletedAtSetter,
            _deletedByColumnName,
            _deletedByPropertyName,
            _deletedByGetter,
            _deletedBySetter);
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
/// Internal implementation of <see cref="ISoftDeleteEntityMapping{TEntity, TId}"/> for MySQL.
/// </summary>
internal sealed class SoftDeleteEntityMapping<TEntity, TId> : ISoftDeleteEntityMapping<TEntity, TId>
    where TEntity : class
    where TId : notnull
{
    private readonly Func<TEntity, TId> _idSelector;
    private readonly Func<TEntity, bool>? _isDeletedGetter;
    private readonly Action<TEntity, bool>? _isDeletedSetter;
    private readonly Func<TEntity, DateTime?>? _deletedAtGetter;
    private readonly Action<TEntity, DateTime?>? _deletedAtSetter;
    private readonly Func<TEntity, string?>? _deletedByGetter;
    private readonly Action<TEntity, string?>? _deletedBySetter;

    public SoftDeleteEntityMapping(
        string tableName,
        string idColumnName,
        Func<TEntity, TId> idSelector,
        Dictionary<string, string> columnMappings,
        HashSet<string> insertExcluded,
        HashSet<string> updateExcluded,
        string? isDeletedColumnName,
        string? isDeletedPropertyName,
        Func<TEntity, bool>? isDeletedGetter,
        Action<TEntity, bool>? isDeletedSetter,
        string? deletedAtColumnName,
        string? deletedAtPropertyName,
        Func<TEntity, DateTime?>? deletedAtGetter,
        Action<TEntity, DateTime?>? deletedAtSetter,
        string? deletedByColumnName,
        string? deletedByPropertyName,
        Func<TEntity, string?>? deletedByGetter,
        Action<TEntity, string?>? deletedBySetter)
    {
        TableName = tableName;
        IdColumnName = idColumnName;
        _idSelector = idSelector;
        ColumnMappings = columnMappings;
        InsertExcludedProperties = insertExcluded;
        UpdateExcludedProperties = updateExcluded;
        IsDeletedColumnName = isDeletedColumnName;
        IsDeletedPropertyName = isDeletedPropertyName;
        _isDeletedGetter = isDeletedGetter;
        _isDeletedSetter = isDeletedSetter;
        DeletedAtColumnName = deletedAtColumnName;
        DeletedAtPropertyName = deletedAtPropertyName;
        _deletedAtGetter = deletedAtGetter;
        _deletedAtSetter = deletedAtSetter;
        DeletedByColumnName = deletedByColumnName;
        DeletedByPropertyName = deletedByPropertyName;
        _deletedByGetter = deletedByGetter;
        _deletedBySetter = deletedBySetter;
    }

    public string TableName { get; }
    public string IdColumnName { get; }
    public IReadOnlyDictionary<string, string> ColumnMappings { get; }
    public IReadOnlySet<string> InsertExcludedProperties { get; }
    public IReadOnlySet<string> UpdateExcludedProperties { get; }
    public bool IsSoftDeletable => IsDeletedColumnName is not null;
    public string? IsDeletedColumnName { get; }
    public string? IsDeletedPropertyName { get; }
    public string? DeletedAtColumnName { get; }
    public string? DeletedAtPropertyName { get; }
    public string? DeletedByColumnName { get; }
    public string? DeletedByPropertyName { get; }

    public TId GetId(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return _idSelector(entity);
    }

    public bool? GetIsDeleted(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return _isDeletedGetter?.Invoke(entity);
    }

    public void SetIsDeleted(TEntity entity, bool isDeleted)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (_isDeletedSetter is null)
        {
            throw new InvalidOperationException(
                $"Cannot set IsDeleted on entity {typeof(TEntity).Name} because soft delete is not configured.");
        }

        _isDeletedSetter(entity, isDeleted);
    }

    public DateTime? GetDeletedAtUtc(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return _deletedAtGetter?.Invoke(entity);
    }

    public void SetDeletedAtUtc(TEntity entity, DateTime? deletedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (_deletedAtSetter is null)
        {
            throw new InvalidOperationException(
                $"Cannot set DeletedAtUtc on entity {typeof(TEntity).Name} because DeletedAt tracking is not configured.");
        }

        _deletedAtSetter(entity, deletedAtUtc);
    }

    public string? GetDeletedBy(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return _deletedByGetter?.Invoke(entity);
    }

    public void SetDeletedBy(TEntity entity, string? deletedBy)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (_deletedBySetter is null)
        {
            throw new InvalidOperationException(
                $"Cannot set DeletedBy on entity {typeof(TEntity).Name} because DeletedBy tracking is not configured.");
        }

        _deletedBySetter(entity, deletedBy);
    }
}
