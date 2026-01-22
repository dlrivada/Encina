using System.Linq.Expressions;
using Encina.Messaging;

namespace Encina.ADO.Sqlite.Repository;

/// <summary>
/// Fluent builder for configuring entity-to-table mappings for SQLite.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TId">The entity identifier type.</typeparam>
/// <remarks>
/// <para>
/// This builder provides a fluent API for defining how an entity maps to
/// a database table. It validates all SQL identifiers to prevent injection attacks.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var mapping = new EntityMappingBuilder&lt;Order, Guid&gt;()
///     .ToTable("Orders")
///     .HasId(o =&gt; o.Id)
///     .MapProperty(o =&gt; o.CustomerId, "CustomerId")
///     .MapProperty(o =&gt; o.Total, "Total")
///     .MapProperty(o =&gt; o.CreatedAtUtc, "CreatedAtUtc")
///     .ExcludeFromInsert(o =&gt; o.Id) // Auto-generated
///     .Build();
/// </code>
/// </example>
public sealed class EntityMappingBuilder<TEntity, TId>
    where TEntity : class
    where TId : notnull
{
    private string? _tableName;
    private string? _idColumnName;
    private string? _idPropertyName;
    private Func<TEntity, TId>? _idSelector;
    private readonly Dictionary<string, string> _columnMappings = new();
    private readonly HashSet<string> _insertExcluded = [];
    private readonly HashSet<string> _updateExcluded = [];

    /// <summary>
    /// Specifies the database table name for this entity.
    /// </summary>
    /// <param name="tableName">The table name.</param>
    /// <returns>This builder for chaining.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the table name is invalid or contains unsafe characters.
    /// </exception>
    public EntityMappingBuilder<TEntity, TId> ToTable(string tableName)
    {
        _tableName = SqlIdentifierValidator.ValidateTableName(tableName);
        return this;
    }

    /// <summary>
    /// Specifies the primary key property and its column name.
    /// </summary>
    /// <typeparam name="TProperty">The property type (must match TId).</typeparam>
    /// <param name="propertySelector">Expression selecting the ID property.</param>
    /// <param name="columnName">Optional column name (defaults to property name).</param>
    /// <returns>This builder for chaining.</returns>
    public EntityMappingBuilder<TEntity, TId> HasId<TProperty>(
        Expression<Func<TEntity, TProperty>> propertySelector,
        string? columnName = null)
    {
        ArgumentNullException.ThrowIfNull(propertySelector);

        var propertyName = GetPropertyName(propertySelector);
        _idPropertyName = propertyName;
        _idColumnName = SqlIdentifierValidator.ValidateTableName(columnName ?? propertyName, nameof(columnName));

        // Create the ID selector
        var compiled = propertySelector.Compile();
        _idSelector = entity => (TId)(object)compiled(entity)!;

        // Add to column mappings
        _columnMappings[propertyName] = _idColumnName;

        // ID is typically excluded from updates
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
    public EntityMappingBuilder<TEntity, TId> MapProperty<TProperty>(
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
    /// Excludes a property from INSERT operations (e.g., auto-generated columns).
    /// </summary>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="propertySelector">Expression selecting the property to exclude.</param>
    /// <returns>This builder for chaining.</returns>
    public EntityMappingBuilder<TEntity, TId> ExcludeFromInsert<TProperty>(
        Expression<Func<TEntity, TProperty>> propertySelector)
    {
        ArgumentNullException.ThrowIfNull(propertySelector);

        var propertyName = GetPropertyName(propertySelector);
        _insertExcluded.Add(propertyName);

        return this;
    }

    /// <summary>
    /// Excludes a property from UPDATE operations (e.g., created timestamp).
    /// </summary>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="propertySelector">Expression selecting the property to exclude.</param>
    /// <returns>This builder for chaining.</returns>
    public EntityMappingBuilder<TEntity, TId> ExcludeFromUpdate<TProperty>(
        Expression<Func<TEntity, TProperty>> propertySelector)
    {
        ArgumentNullException.ThrowIfNull(propertySelector);

        var propertyName = GetPropertyName(propertySelector);
        _updateExcluded.Add(propertyName);

        return this;
    }

    /// <summary>
    /// Builds the entity mapping configuration.
    /// </summary>
    /// <returns>The configured entity mapping.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when required configuration is missing.
    /// </exception>
    public IEntityMapping<TEntity, TId> Build()
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

        return new EntityMapping<TEntity, TId>(
            _tableName,
            _idColumnName,
            _idSelector,
            new Dictionary<string, string>(_columnMappings),
            new HashSet<string>(_insertExcluded),
            new HashSet<string>(_updateExcluded));
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
/// Internal implementation of <see cref="IEntityMapping{TEntity, TId}"/>.
/// </summary>
internal sealed class EntityMapping<TEntity, TId> : IEntityMapping<TEntity, TId>
    where TEntity : class
    where TId : notnull
{
    private readonly Func<TEntity, TId> _idSelector;

    public EntityMapping(
        string tableName,
        string idColumnName,
        Func<TEntity, TId> idSelector,
        Dictionary<string, string> columnMappings,
        HashSet<string> insertExcluded,
        HashSet<string> updateExcluded)
    {
        TableName = tableName;
        IdColumnName = idColumnName;
        _idSelector = idSelector;
        ColumnMappings = columnMappings;
        InsertExcludedProperties = insertExcluded;
        UpdateExcludedProperties = updateExcluded;
    }

    public string TableName { get; }
    public string IdColumnName { get; }
    public IReadOnlyDictionary<string, string> ColumnMappings { get; }
    public IReadOnlySet<string> InsertExcludedProperties { get; }
    public IReadOnlySet<string> UpdateExcludedProperties { get; }

    public TId GetId(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return _idSelector(entity);
    }
}
