using LanguageExt;

namespace Encina.Sharding;

/// <summary>
/// Default implementation of <see cref="IShardRouter{TEntity}"/> that extracts shard keys
/// from entities using <see cref="CompoundShardKeyExtractor"/> and delegates routing to an <see cref="IShardRouter"/>.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <remarks>
/// <para>
/// Shard key extraction follows a four-step priority:
/// <list type="number">
///   <item><description>If <typeparamref name="TEntity"/> implements <see cref="ICompoundShardable"/>,
///   the <see cref="ICompoundShardable.GetCompoundShardKey"/> method is used directly.</description></item>
///   <item><description>If <typeparamref name="TEntity"/> has multiple properties decorated with
///   <see cref="ShardKeyAttribute"/>, they are combined by <see cref="ShardKeyAttribute.Order"/>
///   into a <see cref="CompoundShardKey"/>.</description></item>
///   <item><description>If <typeparamref name="TEntity"/> implements <see cref="IShardable"/>,
///   the <see cref="IShardable.GetShardKey"/> method is used (wrapped in a single-component key).</description></item>
///   <item><description>Otherwise, a single <see cref="ShardKeyAttribute"/> property is extracted
///   via reflection (cached) and wrapped in a single-component key.</description></item>
/// </list>
/// If none of these mechanisms produces a shard key, the result is <c>Left</c> with error code
/// <see cref="ShardingErrorCodes.ShardKeyNotConfigured"/>.
/// </para>
/// <para>
/// Single-component compound keys route identically to simple string keys, ensuring
/// backward compatibility with existing entities that use <see cref="IShardable"/> or
/// a single <see cref="ShardKeyAttribute"/>.
/// </para>
/// <para>
/// This class is registered internally by the sharding DI extensions and is not intended
/// for direct construction. Use <c>IShardRouter&lt;TEntity&gt;</c> through dependency injection.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // ICompoundShardable approach (multi-field keys)
/// public class Order : ICompoundShardable
/// {
///     public string Region { get; set; }
///     public string CustomerId { get; set; }
///     public CompoundShardKey GetCompoundShardKey() => new(Region, CustomerId);
/// }
///
/// // Multiple [ShardKey] attributes (simpler compound keys)
/// public class Invoice
/// {
///     [ShardKey(Order = 0)]
///     public string TenantId { get; set; }
///     [ShardKey(Order = 1)]
///     public string Region { get; set; }
/// }
///
/// // IShardable approach (single key — backward compatible)
/// public class Payment : IShardable
/// {
///     public string CustomerId { get; set; }
///     public string GetShardKey() => CustomerId;
/// }
///
/// // All work identically with the router:
/// Either&lt;EncinaError, string&gt; shardId = router.GetShardId(order);
/// </code>
/// </example>
internal sealed class EntityShardRouter<TEntity> : IShardRouter<TEntity>
    where TEntity : notnull
{
    private readonly IShardRouter _inner;

    public EntityShardRouter(IShardRouter inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
    }

    /// <inheritdoc />
    public Either<EncinaError, string> GetShardId(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return CompoundShardKeyExtractor.Extract(entity)
            .Bind(key => _inner.GetShardId(key));
    }

    /// <inheritdoc />
    public Either<EncinaError, IReadOnlyList<string>> GetShardIds(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return CompoundShardKeyExtractor.Extract(entity)
            .Bind(key => _inner.GetShardIds(key));
    }

    /// <inheritdoc />
    public Either<EncinaError, string> GetShardId(string shardKey) => _inner.GetShardId(shardKey);

    /// <inheritdoc />
    public IReadOnlyList<string> GetAllShardIds() => _inner.GetAllShardIds();

    /// <inheritdoc />
    public Either<EncinaError, string> GetShardConnectionString(string shardId) =>
        _inner.GetShardConnectionString(shardId);
}
