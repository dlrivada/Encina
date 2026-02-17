using System.Collections.Concurrent;
using System.Reflection;
using LanguageExt;

namespace Encina.Sharding;

/// <summary>
/// Utility for extracting shard keys from entities using either the <see cref="IShardable"/>
/// interface or properties marked with <see cref="ShardKeyAttribute"/>.
/// </summary>
/// <remarks>
/// <para>
/// Resolution order:
/// <list type="number">
/// <item>If the entity implements <see cref="IShardable"/>, calls <see cref="IShardable.GetShardKey()"/>.</item>
/// <item>If a property is marked with <see cref="ShardKeyAttribute"/>, reads its value via reflection.</item>
/// </list>
/// </para>
/// <para>
/// Reflection metadata is cached per type to avoid repeated lookups.
/// </para>
/// </remarks>
public static class ShardKeyExtractor
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo?> ShardKeyPropertyCache = new();

    /// <summary>
    /// Extracts the shard key from an entity.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="entity">The entity to extract the shard key from.</param>
    /// <returns>
    /// Right with the shard key string if extraction succeeds;
    /// Left with an error if the entity has no shard key configured or the value is null.
    /// </returns>
    public static Either<EncinaError, string> Extract<TEntity>(TEntity entity)
        where TEntity : notnull
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity is IShardable shardable)
        {
            var key = shardable.GetShardKey();
            return string.IsNullOrEmpty(key)
                ? Either<EncinaError, string>.Left(
                    EncinaErrors.Create(
                        ShardingErrorCodes.ShardKeyEmpty,
                        $"IShardable.GetShardKey() returned null or empty for entity type '{typeof(TEntity).Name}'."))
                : Either<EncinaError, string>.Right(key);
        }

        var property = ShardKeyPropertyCache.GetOrAdd(typeof(TEntity), FindShardKeyProperty);

        if (property is null)
        {
            return Either<EncinaError, string>.Left(
                EncinaErrors.Create(
                    ShardingErrorCodes.ShardKeyNotConfigured,
                    $"Entity type '{typeof(TEntity).Name}' does not implement IShardable and has no property marked with [ShardKey]."));
        }

        var value = property.GetValue(entity);

        if (value is null)
        {
            return Either<EncinaError, string>.Left(
                EncinaErrors.Create(
                    ShardingErrorCodes.ShardKeyEmpty,
                    $"Shard key property '{property.Name}' on entity type '{typeof(TEntity).Name}' has a null value."));
        }

        var stringValue = value.ToString();

        return string.IsNullOrEmpty(stringValue)
            ? Either<EncinaError, string>.Left(
                EncinaErrors.Create(
                    ShardingErrorCodes.ShardKeyEmpty,
                    $"Shard key property '{property.Name}' on entity type '{typeof(TEntity).Name}' has an empty string value."))
            : Either<EncinaError, string>.Right(stringValue);
    }

    private static PropertyInfo? FindShardKeyProperty(Type type)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(p => p.GetCustomAttribute<ShardKeyAttribute>() is not null);
    }
}
