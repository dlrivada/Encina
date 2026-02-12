using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Encina.Sharding.Colocation;

/// <summary>
/// Central singleton registry for co-location groups, maintaining bidirectional mappings
/// between entity types and their co-location groups.
/// </summary>
/// <remarks>
/// <para>
/// The registry is populated during startup by:
/// <list type="bullet">
/// <item>Fluent API calls via <c>AddColocatedEntity&lt;T&gt;()</c> on sharding options</item>
/// <item>Scanning for <see cref="ColocatedWithAttribute"/> declarations on entity types</item>
/// </list>
/// </para>
/// <para>
/// Once populated, the registry provides O(1) lookups from any entity type (root or co-located)
/// to its group. This enables the routing layer and query planner to determine co-location
/// relationships efficiently.
/// </para>
/// </remarks>
public sealed class ColocationGroupRegistry
{
    private readonly ConcurrentDictionary<Type, IColocationGroup> _entityToGroup = new();
    private readonly ConcurrentDictionary<Type, IColocationGroup> _rootToGroup = new();

    /// <summary>
    /// Registers a complete co-location group.
    /// </summary>
    /// <param name="group">The co-location group to register.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the root entity or any co-located entity is already registered in a different group.
    /// </exception>
    public void RegisterGroup(IColocationGroup group)
    {
        ArgumentNullException.ThrowIfNull(group);

        // Register root entity
        if (!_entityToGroup.TryAdd(group.RootEntity, group))
        {
            var existing = _entityToGroup[group.RootEntity];
            if (!ReferenceEquals(existing, group))
            {
                throw new InvalidOperationException(
                    $"Entity type '{group.RootEntity.FullName}' is already registered in a co-location group " +
                    $"with root entity '{existing.RootEntity.FullName}'.");
            }
        }

        _rootToGroup[group.RootEntity] = group;

        // Register each co-located entity
        foreach (var colocatedEntity in group.ColocatedEntities)
        {
            if (!_entityToGroup.TryAdd(colocatedEntity, group))
            {
                var existing = _entityToGroup[colocatedEntity];
                if (!ReferenceEquals(existing, group))
                {
                    throw new InvalidOperationException(
                        $"Entity type '{colocatedEntity.FullName}' is already registered in a co-location group " +
                        $"with root entity '{existing.RootEntity.FullName}'. " +
                        $"Cannot register it in a new group with root entity '{group.RootEntity.FullName}'.");
                }
            }
        }
    }

    /// <summary>
    /// Registers a single co-located entity with a root entity.
    /// </summary>
    /// <param name="rootEntity">The root entity type.</param>
    /// <param name="colocatedEntity">The entity type to co-locate.</param>
    /// <remarks>
    /// If a group for the root entity does not yet exist, a new group is created.
    /// If a group already exists, the co-located entity is added to it.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the co-located entity is already registered in a different group.
    /// </exception>
    public void RegisterColocatedEntity(Type rootEntity, Type colocatedEntity)
    {
        ArgumentNullException.ThrowIfNull(rootEntity);
        ArgumentNullException.ThrowIfNull(colocatedEntity);

        var group = _rootToGroup.GetOrAdd(rootEntity, _ => new ColocationGroup(
            rootEntity,
            new List<Type>(),
            string.Empty));

        // Add to the colocated entities list if it's a mutable list
        if (group.ColocatedEntities is List<Type> mutableList)
        {
            if (!mutableList.Contains(colocatedEntity))
            {
                mutableList.Add(colocatedEntity);
            }
        }

        // Register the root entity mapping
        _entityToGroup.TryAdd(rootEntity, group);

        // Register the co-located entity mapping
        if (!_entityToGroup.TryAdd(colocatedEntity, group))
        {
            var existing = _entityToGroup[colocatedEntity];
            if (!ReferenceEquals(existing, group))
            {
                throw new InvalidOperationException(
                    $"Entity type '{colocatedEntity.FullName}' is already registered in a co-location group " +
                    $"with root entity '{existing.RootEntity.FullName}'. " +
                    $"Cannot register it in a new group with root entity '{rootEntity.FullName}'.");
            }
        }
    }

    /// <summary>
    /// Tries to get the co-location group for a given entity type.
    /// </summary>
    /// <param name="entityType">The entity type to look up (can be root or co-located).</param>
    /// <param name="group">
    /// When this method returns, contains the co-location group if found; otherwise, <c>null</c>.
    /// </param>
    /// <returns><c>true</c> if the entity type belongs to a co-location group; otherwise, <c>false</c>.</returns>
    public bool TryGetGroup(Type entityType, [NotNullWhen(true)] out IColocationGroup? group)
    {
        ArgumentNullException.ThrowIfNull(entityType);
        return _entityToGroup.TryGetValue(entityType, out group);
    }

    /// <summary>
    /// Gets all registered co-location groups.
    /// </summary>
    /// <returns>A read-only collection of all registered co-location groups.</returns>
    public IReadOnlyCollection<IColocationGroup> GetAllGroups()
    {
        return _rootToGroup.Values.Distinct().ToList().AsReadOnly();
    }
}
