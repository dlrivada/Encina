using LanguageExt;

namespace Encina.Sharding;

/// <summary>
/// Default implementation of <see cref="IShardRouter{TEntity}"/> that extracts shard keys
/// from entities using <see cref="ShardKeyExtractor"/> and delegates routing to an <see cref="IShardRouter"/>.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <remarks>
/// <para>
/// Shard key extraction follows a two-step priority:
/// <list type="number">
///   <item><description>If <typeparamref name="TEntity"/> implements <see cref="IShardable"/>,
///   the <see cref="IShardable.GetShardKey"/> method is used directly.</description></item>
///   <item><description>Otherwise, <see cref="ShardKeyExtractor"/> scans for a property decorated
///   with <see cref="ShardKeyAttribute"/> and extracts its value via reflection (cached).</description></item>
/// </list>
/// If neither mechanism produces a shard key, the result is <c>Left</c> with error code
/// <see cref="ShardingErrorCodes.ShardKeyNotConfigured"/>.
/// </para>
/// <para>
/// This class is registered internally by the sharding DI extensions and is not intended
/// for direct construction. Use <c>IShardRouter&lt;TEntity&gt;</c> through dependency injection.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // IShardable approach (preferred — no reflection)
/// public class Order : IShardable
/// {
///     public string Id { get; set; }
///     public string CustomerId { get; set; }
///     public string GetShardKey() => CustomerId;
/// }
///
/// // Attribute approach (simpler, uses cached reflection)
/// public class Invoice
/// {
///     public string Id { get; set; }
///     [ShardKey]
///     public string TenantId { get; set; }
/// }
///
/// // Both work identically with the router:
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

        return ShardKeyExtractor.Extract(entity)
            .Bind(key => _inner.GetShardId(key));
    }

    /// <inheritdoc />
    public Either<EncinaError, string> GetShardId(string shardKey) => _inner.GetShardId(shardKey);

    /// <inheritdoc />
    public IReadOnlyList<string> GetAllShardIds() => _inner.GetAllShardIds();

    /// <inheritdoc />
    public Either<EncinaError, string> GetShardConnectionString(string shardId) =>
        _inner.GetShardConnectionString(shardId);
}
