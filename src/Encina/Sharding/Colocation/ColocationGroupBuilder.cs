namespace Encina.Sharding.Colocation;

/// <summary>
/// Fluent builder for constructing <see cref="IColocationGroup"/> instances programmatically.
/// </summary>
/// <remarks>
/// <para>
/// Use this builder when you prefer programmatic co-location group definition over the
/// declarative <see cref="ColocatedWithAttribute"/> approach.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var group = new ColocationGroupBuilder()
///     .WithRootEntity&lt;Order&gt;()
///     .AddColocatedEntity&lt;OrderItem&gt;()
///     .AddColocatedEntity&lt;OrderPayment&gt;()
///     .WithSharedShardKeyProperty("CustomerId")
///     .Build();
/// </code>
/// </example>
public sealed class ColocationGroupBuilder
{
    private Type? _rootEntity;
    private readonly List<Type> _colocatedEntities = [];
    private string _sharedShardKeyProperty = string.Empty;

    /// <summary>
    /// Sets the root entity type for the co-location group.
    /// </summary>
    /// <typeparam name="T">The root entity type.</typeparam>
    /// <returns>This builder for fluent chaining.</returns>
    public ColocationGroupBuilder WithRootEntity<T>()
        where T : notnull
    {
        _rootEntity = typeof(T);
        return this;
    }

    /// <summary>
    /// Adds a co-located entity type to the group.
    /// </summary>
    /// <typeparam name="T">The entity type to co-locate with the root entity.</typeparam>
    /// <returns>This builder for fluent chaining.</returns>
    public ColocationGroupBuilder AddColocatedEntity<T>()
        where T : notnull
    {
        var entityType = typeof(T);

        if (!_colocatedEntities.Contains(entityType))
        {
            _colocatedEntities.Add(entityType);
        }

        return this;
    }

    /// <summary>
    /// Sets the shared shard key property name for documentation and diagnostics.
    /// </summary>
    /// <param name="propertyName">
    /// The property name shared across entities in the group (e.g., "CustomerId").
    /// </param>
    /// <returns>This builder for fluent chaining.</returns>
    public ColocationGroupBuilder WithSharedShardKeyProperty(string propertyName)
    {
        ArgumentNullException.ThrowIfNull(propertyName);
        _sharedShardKeyProperty = propertyName;
        return this;
    }

    /// <summary>
    /// Builds the <see cref="IColocationGroup"/> from the configured state.
    /// </summary>
    /// <returns>An immutable <see cref="ColocationGroup"/> instance.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="WithRootEntity{T}"/> has not been called.
    /// </exception>
    public IColocationGroup Build()
    {
        if (_rootEntity is null)
        {
            throw new InvalidOperationException(
                "Root entity type must be set before building a co-location group. " +
                "Call WithRootEntity<T>() before Build().");
        }

        return new ColocationGroup(
            _rootEntity,
            _colocatedEntities.AsReadOnly(),
            _sharedShardKeyProperty);
    }
}
