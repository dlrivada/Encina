using System.Collections.Concurrent;
using System.Reflection;
using LanguageExt;

namespace Encina.Sharding;

/// <summary>
/// Extracts compound shard keys from entities using a priority-based resolution strategy.
/// </summary>
/// <remarks>
/// <para>
/// Resolution order:
/// <list type="number">
/// <item>If the entity implements <see cref="ICompoundShardable"/>, calls
/// <see cref="ICompoundShardable.GetCompoundShardKey()"/>.</item>
/// <item>If multiple properties are marked with <see cref="ShardKeyAttribute"/>,
/// orders them by <see cref="ShardKeyAttribute.Order"/> and builds a compound key.</item>
/// <item>If the entity implements <see cref="IShardable"/>, wraps the result as a
/// single-component <see cref="CompoundShardKey"/>.</item>
/// <item>If a single property is marked with <see cref="ShardKeyAttribute"/>,
/// wraps its value as a single-component <see cref="CompoundShardKey"/>.</item>
/// </list>
/// </para>
/// <para>
/// Reflection metadata is cached per type via a <see cref="ConcurrentDictionary{TKey, TValue}"/>
/// to avoid repeated lookups.
/// </para>
/// </remarks>
public static class CompoundShardKeyExtractor
{
    private static readonly ConcurrentDictionary<Type, ShardKeyPropertyInfo[]?> ShardKeyPropertyCache = new();

    /// <summary>
    /// Extracts a <see cref="CompoundShardKey"/> from an entity.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="entity">The entity to extract the compound shard key from.</param>
    /// <returns>
    /// Right with the <see cref="CompoundShardKey"/> if extraction succeeds;
    /// Left with an <see cref="EncinaError"/> if the entity has no shard key configured,
    /// the key is empty, or duplicate <see cref="ShardKeyAttribute.Order"/> values are found.
    /// </returns>
    public static Either<EncinaError, CompoundShardKey> Extract<TEntity>(TEntity entity)
        where TEntity : notnull
    {
        ArgumentNullException.ThrowIfNull(entity);

        // Priority 1: ICompoundShardable interface
        if (entity is ICompoundShardable compoundShardable)
        {
            return ValidateCompoundKey(compoundShardable.GetCompoundShardKey(), typeof(TEntity).Name);
        }

        // Priority 2 & 4: [ShardKey] attributes (multiple → compound, single → wrapped)
        var properties = ShardKeyPropertyCache.GetOrAdd(typeof(TEntity), FindShardKeyProperties);

        if (properties is { Length: > 1 })
        {
            return ExtractFromMultipleAttributes(entity, properties);
        }

        // Priority 3: IShardable interface
        if (entity is IShardable shardable)
        {
            return ExtractFromShardable(shardable, typeof(TEntity).Name);
        }

        // Priority 4: Single [ShardKey] attribute
        if (properties is { Length: 1 })
        {
            return ExtractFromSingleAttribute(entity, properties[0]);
        }

        // No shard key configured
        return Either<EncinaError, CompoundShardKey>.Left(
            EncinaErrors.Create(
                ShardingErrorCodes.ShardKeyNotConfigured,
                $"Entity type '{typeof(TEntity).Name}' does not implement ICompoundShardable, IShardable, and has no property marked with [ShardKey]."));
    }

    private static Either<EncinaError, CompoundShardKey> ValidateCompoundKey(
        CompoundShardKey? key,
        string entityTypeName)
    {
        if (key is null || key.Components.Count == 0)
        {
            return Either<EncinaError, CompoundShardKey>.Left(
                EncinaErrors.Create(
                    ShardingErrorCodes.CompoundShardKeyEmpty,
                    $"ICompoundShardable.GetCompoundShardKey() returned null or an empty key for entity type '{entityTypeName}'."));
        }

        for (var i = 0; i < key.Components.Count; i++)
        {
            if (string.IsNullOrEmpty(key.Components[i]))
            {
                return Either<EncinaError, CompoundShardKey>.Left(
                    EncinaErrors.Create(
                        ShardingErrorCodes.CompoundShardKeyComponentEmpty,
                        $"Compound shard key component at index {i} is null or empty for entity type '{entityTypeName}'."));
            }
        }

        return Either<EncinaError, CompoundShardKey>.Right(key);
    }

    private static Either<EncinaError, CompoundShardKey> ExtractFromMultipleAttributes<TEntity>(
        TEntity entity,
        ShardKeyPropertyInfo[] properties)
        where TEntity : notnull
    {
        var entityTypeName = typeof(TEntity).Name;

        // Validate no duplicate Order values
        for (var i = 1; i < properties.Length; i++)
        {
            if (properties[i].Order == properties[i - 1].Order)
            {
                return Either<EncinaError, CompoundShardKey>.Left(
                    EncinaErrors.Create(
                        ShardingErrorCodes.DuplicateShardKeyOrder,
                        $"Properties '{properties[i - 1].Property.Name}' and '{properties[i].Property.Name}' on entity type '{entityTypeName}' share the same [ShardKey(Order = {properties[i].Order})]."));
            }
        }

        var components = new string[properties.Length];

        for (var i = 0; i < properties.Length; i++)
        {
            var value = properties[i].Property.GetValue(entity);

            if (value is null)
            {
                return Either<EncinaError, CompoundShardKey>.Left(
                    EncinaErrors.Create(
                        ShardingErrorCodes.CompoundShardKeyComponentEmpty,
                        $"Shard key property '{properties[i].Property.Name}' (Order={properties[i].Order}) on entity type '{entityTypeName}' has a null value."));
            }

            var stringValue = value.ToString();

            if (string.IsNullOrEmpty(stringValue))
            {
                return Either<EncinaError, CompoundShardKey>.Left(
                    EncinaErrors.Create(
                        ShardingErrorCodes.CompoundShardKeyComponentEmpty,
                        $"Shard key property '{properties[i].Property.Name}' (Order={properties[i].Order}) on entity type '{entityTypeName}' has an empty string value."));
            }

            components[i] = stringValue;
        }

        return Either<EncinaError, CompoundShardKey>.Right(new CompoundShardKey(components));
    }

    private static Either<EncinaError, CompoundShardKey> ExtractFromShardable(
        IShardable shardable,
        string entityTypeName)
    {
        var key = shardable.GetShardKey();

        return string.IsNullOrEmpty(key)
            ? Either<EncinaError, CompoundShardKey>.Left(
                EncinaErrors.Create(
                    ShardingErrorCodes.ShardKeyEmpty,
                    $"IShardable.GetShardKey() returned null or empty for entity type '{entityTypeName}'."))
            : Either<EncinaError, CompoundShardKey>.Right(new CompoundShardKey(key));
    }

    private static Either<EncinaError, CompoundShardKey> ExtractFromSingleAttribute<TEntity>(
        TEntity entity,
        ShardKeyPropertyInfo propertyInfo)
        where TEntity : notnull
    {
        var entityTypeName = typeof(TEntity).Name;
        var value = propertyInfo.Property.GetValue(entity);

        if (value is null)
        {
            return Either<EncinaError, CompoundShardKey>.Left(
                EncinaErrors.Create(
                    ShardingErrorCodes.ShardKeyEmpty,
                    $"Shard key property '{propertyInfo.Property.Name}' on entity type '{entityTypeName}' has a null value."));
        }

        var stringValue = value.ToString();

        return string.IsNullOrEmpty(stringValue)
            ? Either<EncinaError, CompoundShardKey>.Left(
                EncinaErrors.Create(
                    ShardingErrorCodes.ShardKeyEmpty,
                    $"Shard key property '{propertyInfo.Property.Name}' on entity type '{entityTypeName}' has an empty string value."))
            : Either<EncinaError, CompoundShardKey>.Right(new CompoundShardKey(stringValue));
    }

    private static ShardKeyPropertyInfo[]? FindShardKeyProperties(Type type)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => new { Property = p, Attribute = p.GetCustomAttribute<ShardKeyAttribute>() })
            .Where(x => x.Attribute is not null)
            .Select(x => new ShardKeyPropertyInfo(x.Property, x.Attribute!.Order))
            .ToArray();

        if (properties.Length == 0)
        {
            return null;
        }

        // Sort by Order value for compound key component ordering
        return [.. properties.OrderBy(p => p.Order)];
    }

    private sealed record ShardKeyPropertyInfo(PropertyInfo Property, int Order);
}
