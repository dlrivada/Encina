using System.Linq.Expressions;
using System.Reflection;

namespace Encina.MongoDB.SoftDelete;

/// <summary>
/// Fluent builder for configuring soft delete entity mappings for MongoDB.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TId">The entity identifier type.</typeparam>
/// <remarks>
/// <para>
/// This builder provides a fluent API for configuring how soft delete properties
/// map to MongoDB document fields. The resulting mapping is immutable and can be
/// registered as a singleton.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var mapping = new SoftDeleteEntityMappingBuilder&lt;Order, Guid&gt;()
///     .HasId(o =&gt; o.Id)
///     .HasSoftDelete(o =&gt; o.IsDeleted, "isDeleted")
///     .HasDeletedAt(o =&gt; o.DeletedAtUtc, "deletedAtUtc")
///     .HasDeletedBy(o =&gt; o.DeletedBy, "deletedBy")
///     .Build();
/// </code>
/// </example>
public sealed class SoftDeleteEntityMappingBuilder<TEntity, TId>
    where TEntity : class
    where TId : notnull
{
    private Expression<Func<TEntity, TId>>? _idSelector;
    private Expression<Func<TEntity, bool>>? _isDeletedSelector;
    private Expression<Func<TEntity, DateTime?>>? _deletedAtSelector;
    private Expression<Func<TEntity, string?>>? _deletedBySelector;
    private string? _isDeletedFieldName;
    private string? _deletedAtFieldName;
    private string? _deletedByFieldName;

    /// <summary>
    /// Configures the ID property selector for the entity.
    /// </summary>
    /// <typeparam name="TProperty">The property type (must be assignable to TId).</typeparam>
    /// <param name="propertySelector">Expression selecting the ID property.</param>
    /// <param name="fieldName">Optional MongoDB field name. Defaults to property name.</param>
    /// <returns>This builder for chaining.</returns>
    public SoftDeleteEntityMappingBuilder<TEntity, TId> HasId<TProperty>(
        Expression<Func<TEntity, TProperty>> propertySelector,
        string? fieldName = null)
    {
        ArgumentNullException.ThrowIfNull(propertySelector);

        // Convert the expression to the correct type
        var parameter = propertySelector.Parameters[0];
        var body = Expression.Convert(propertySelector.Body, typeof(TId));
        _idSelector = Expression.Lambda<Func<TEntity, TId>>(body, parameter);

        return this;
    }

    /// <summary>
    /// Configures the IsDeleted property mapping.
    /// </summary>
    /// <param name="propertySelector">Expression selecting the IsDeleted property.</param>
    /// <param name="fieldName">Optional MongoDB field name. Defaults to property name.</param>
    /// <returns>This builder for chaining.</returns>
    public SoftDeleteEntityMappingBuilder<TEntity, TId> HasSoftDelete(
        Expression<Func<TEntity, bool>> propertySelector,
        string? fieldName = null)
    {
        ArgumentNullException.ThrowIfNull(propertySelector);

        _isDeletedSelector = propertySelector;
        _isDeletedFieldName = fieldName ?? GetPropertyName(propertySelector);

        return this;
    }

    /// <summary>
    /// Configures the DeletedAtUtc property mapping.
    /// </summary>
    /// <param name="propertySelector">Expression selecting the DeletedAtUtc property.</param>
    /// <param name="fieldName">Optional MongoDB field name. Defaults to property name.</param>
    /// <returns>This builder for chaining.</returns>
    public SoftDeleteEntityMappingBuilder<TEntity, TId> HasDeletedAt(
        Expression<Func<TEntity, DateTime?>> propertySelector,
        string? fieldName = null)
    {
        ArgumentNullException.ThrowIfNull(propertySelector);

        _deletedAtSelector = propertySelector;
        _deletedAtFieldName = fieldName ?? GetPropertyName(propertySelector);

        return this;
    }

    /// <summary>
    /// Configures the DeletedBy property mapping.
    /// </summary>
    /// <param name="propertySelector">Expression selecting the DeletedBy property.</param>
    /// <param name="fieldName">Optional MongoDB field name. Defaults to property name.</param>
    /// <returns>This builder for chaining.</returns>
    public SoftDeleteEntityMappingBuilder<TEntity, TId> HasDeletedBy(
        Expression<Func<TEntity, string?>> propertySelector,
        string? fieldName = null)
    {
        ArgumentNullException.ThrowIfNull(propertySelector);

        _deletedBySelector = propertySelector;
        _deletedByFieldName = fieldName ?? GetPropertyName(propertySelector);

        return this;
    }

    /// <summary>
    /// Builds the immutable entity mapping.
    /// </summary>
    /// <returns>The configured entity mapping.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when required properties (Id, IsDeleted) are not configured.
    /// </exception>
    public ISoftDeleteEntityMapping<TEntity, TId> Build()
    {
        if (_idSelector is null)
        {
            throw new InvalidOperationException(
                $"ID property must be configured. Call HasId() before Build().");
        }

        if (_isDeletedSelector is null)
        {
            throw new InvalidOperationException(
                $"IsDeleted property must be configured. Call HasSoftDelete() before Build().");
        }

        return new SoftDeleteEntityMapping<TEntity, TId>(
            _idSelector,
            _isDeletedSelector,
            _deletedAtSelector,
            _deletedBySelector,
            _isDeletedFieldName!,
            _deletedAtFieldName,
            _deletedByFieldName);
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
            $"Expression must be a property access. Got: {propertySelector.Body.NodeType}",
            nameof(propertySelector));
    }
}

/// <summary>
/// Immutable implementation of <see cref="ISoftDeleteEntityMapping{TEntity, TId}"/>.
/// </summary>
internal sealed class SoftDeleteEntityMapping<TEntity, TId> : ISoftDeleteEntityMapping<TEntity, TId>
    where TEntity : class
    where TId : notnull
{
    private readonly Func<TEntity, TId> _compiledIdSelector;
    private readonly Func<TEntity, bool> _compiledIsDeletedSelector;
    private readonly Func<TEntity, DateTime?>? _compiledDeletedAtSelector;
    private readonly Func<TEntity, string?>? _compiledDeletedBySelector;
    private readonly Action<TEntity, bool> _isDeletedSetter;
    private readonly Action<TEntity, DateTime?>? _deletedAtSetter;
    private readonly Action<TEntity, string?>? _deletedBySetter;

    public SoftDeleteEntityMapping(
        Expression<Func<TEntity, TId>> idSelector,
        Expression<Func<TEntity, bool>> isDeletedSelector,
        Expression<Func<TEntity, DateTime?>>? deletedAtSelector,
        Expression<Func<TEntity, string?>>? deletedBySelector,
        string isDeletedFieldName,
        string? deletedAtFieldName,
        string? deletedByFieldName)
    {
        IdSelector = idSelector;
        IsDeletedFieldName = isDeletedFieldName;
        DeletedAtFieldName = deletedAtFieldName;
        DeletedByFieldName = deletedByFieldName;

        _compiledIdSelector = idSelector.Compile();
        _compiledIsDeletedSelector = isDeletedSelector.Compile();
        _compiledDeletedAtSelector = deletedAtSelector?.Compile();
        _compiledDeletedBySelector = deletedBySelector?.Compile();

        // Build setters
        _isDeletedSetter = BuildSetter(isDeletedSelector);
        _deletedAtSetter = deletedAtSelector is not null ? BuildSetter(deletedAtSelector) : null;
        _deletedBySetter = deletedBySelector is not null ? BuildSetter(deletedBySelector) : null;
    }

    public bool IsSoftDeletable => true;

    public string IsDeletedFieldName { get; }

    public string? DeletedAtFieldName { get; }

    public string? DeletedByFieldName { get; }

    public Expression<Func<TEntity, TId>> IdSelector { get; }

    public TId GetId(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return _compiledIdSelector(entity);
    }

    public bool GetIsDeleted(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return _compiledIsDeletedSelector(entity);
    }

    public void SetIsDeleted(TEntity entity, bool value)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _isDeletedSetter(entity, value);
    }

    public DateTime? GetDeletedAt(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return _compiledDeletedAtSelector?.Invoke(entity);
    }

    public void SetDeletedAt(TEntity entity, DateTime? value)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _deletedAtSetter?.Invoke(entity, value);
    }

    public string? GetDeletedBy(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return _compiledDeletedBySelector?.Invoke(entity);
    }

    public void SetDeletedBy(TEntity entity, string? value)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _deletedBySetter?.Invoke(entity, value);
    }

    private static Action<TEntity, TValue> BuildSetter<TValue>(Expression<Func<TEntity, TValue>> getter)
    {
        var memberExpression = getter.Body as MemberExpression
            ?? (getter.Body as UnaryExpression)?.Operand as MemberExpression;

        if (memberExpression?.Member is not PropertyInfo property)
        {
            throw new ArgumentException(
                $"Expression must be a property access. Got: {getter.Body.NodeType}",
                nameof(getter));
        }

        var entityParam = Expression.Parameter(typeof(TEntity), "entity");
        var valueParam = Expression.Parameter(typeof(TValue), "value");
        var propertyAccess = Expression.Property(entityParam, property);
        var assign = Expression.Assign(propertyAccess, valueParam);

        return Expression.Lambda<Action<TEntity, TValue>>(assign, entityParam, valueParam).Compile();
    }
}
