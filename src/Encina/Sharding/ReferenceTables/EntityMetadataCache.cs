using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace Encina.Sharding.ReferenceTables;

/// <summary>
/// Thread-safe cache for <see cref="EntityMetadata"/> instances, discovered via
/// reflection on first access for each entity type.
/// </summary>
/// <remarks>
/// <para>
/// The cache uses <see cref="ConcurrentDictionary{TKey,TValue}"/> for thread-safe
/// access. Reflection is performed at most once per entity type; subsequent calls
/// return the cached result.
/// </para>
/// <para>
/// Convention precedence:
/// <list type="number">
/// <item>Table name: <see cref="TableAttribute"/> → type name</item>
/// <item>Primary key: <see cref="KeyAttribute"/> → property named "Id" (case-insensitive)</item>
/// <item>Column name: <see cref="ColumnAttribute"/> → property name</item>
/// </list>
/// </para>
/// </remarks>
public static class EntityMetadataCache
{
    private static readonly ConcurrentDictionary<Type, EntityMetadata> Cache = new();

    /// <summary>
    /// Gets or creates cached metadata for the specified entity type.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to inspect.</typeparam>
    /// <returns>The cached entity metadata.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no primary key can be identified on the entity type.
    /// </exception>
    public static EntityMetadata GetOrCreate<TEntity>() where TEntity : class
        => GetOrCreate(typeof(TEntity));

    /// <summary>
    /// Gets or creates cached metadata for the specified entity type.
    /// </summary>
    /// <param name="entityType">The entity type to inspect.</param>
    /// <returns>The cached entity metadata.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no primary key can be identified on the entity type.
    /// </exception>
    public static EntityMetadata GetOrCreate(Type entityType)
        => Cache.GetOrAdd(entityType, static type => BuildMetadata(type));

    private static EntityMetadata BuildMetadata(Type entityType)
    {
        // Resolve table name
        var tableAttribute = entityType.GetCustomAttribute<TableAttribute>();
        var tableName = tableAttribute?.Name ?? entityType.Name;

        // Discover properties (public instance with both getter and setter)
        var properties = entityType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .ToList();

        // Build property metadata
        var allProperties = new List<PropertyMetadata>(properties.Count);
        PropertyMetadata? primaryKey = null;

        foreach (var property in properties)
        {
            var columnAttribute = property.GetCustomAttribute<ColumnAttribute>();
            var columnName = columnAttribute?.Name ?? property.Name;
            var isKey = property.GetCustomAttribute<KeyAttribute>() is not null;

            var meta = new PropertyMetadata(property, columnName, isKey);
            allProperties.Add(meta);

            if (isKey)
            {
                primaryKey = meta;
            }
        }

        // Fall back to convention: property named "Id" (case-insensitive)
        primaryKey ??= allProperties.Find(
            p => string.Equals(p.Property.Name, "Id", StringComparison.OrdinalIgnoreCase));

        if (primaryKey is null)
        {
            throw new InvalidOperationException(
                $"Reference table entity '{entityType.Name}' has no primary key. " +
                "Add a [Key] attribute or define a property named 'Id'.");
        }

        // Ensure the primary key's IsPrimaryKey flag is set
        if (!primaryKey.IsPrimaryKey)
        {
            primaryKey = primaryKey with { IsPrimaryKey = true };
            var index = allProperties.FindIndex(
                p => p.Property.Name == primaryKey.Property.Name);
            allProperties[index] = primaryKey;
        }

        var nonKeyProperties = allProperties
            .Where(p => !p.IsPrimaryKey)
            .ToList();

        return new EntityMetadata(
            tableName,
            primaryKey,
            allProperties.AsReadOnly(),
            nonKeyProperties.AsReadOnly());
    }
}
