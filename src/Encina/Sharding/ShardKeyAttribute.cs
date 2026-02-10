namespace Encina.Sharding;

/// <summary>
/// Marks a property as the shard key for an entity.
/// </summary>
/// <remarks>
/// <para>
/// Use this attribute as an alternative to implementing <see cref="IShardable"/>.
/// The <see cref="ShardKeyExtractor"/> utility can extract shard keys from entities
/// using either this attribute or the <see cref="IShardable"/> interface.
/// </para>
/// <para>
/// Only one property per entity should be marked with this attribute.
/// If multiple properties are marked, the first one found via reflection will be used.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class Order
/// {
///     public OrderId Id { get; init; }
///
///     [ShardKey]
///     public string CustomerId { get; init; }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public sealed class ShardKeyAttribute : Attribute;
