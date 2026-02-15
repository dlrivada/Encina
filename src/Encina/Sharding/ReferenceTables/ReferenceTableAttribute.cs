namespace Encina.Sharding.ReferenceTables;

/// <summary>
/// Marks an entity class as a reference table (broadcast table) that is replicated
/// to all shards for local JOINs without cross-shard traffic.
/// </summary>
/// <remarks>
/// <para>
/// Reference tables are small, read-heavy lookup tables (e.g., countries, currencies,
/// categories) that are automatically replicated from a primary shard to all other
/// shards in the topology. This enables efficient local JOINs with sharded entities
/// without scatter-gather overhead.
/// </para>
/// <para>
/// Entities marked with this attribute can be registered via the fluent API:
/// </para>
/// <code>
/// services.AddEncinaSharding&lt;Order&gt;(options =>
/// {
///     options.UseHashRouting()
///         .AddShard("shard-0", "Server=shard0;...")
///         .AddShard("shard-1", "Server=shard1;...")
///         .AddReferenceTable&lt;Country&gt;(rt =>
///         {
///             rt.RefreshStrategy = RefreshStrategy.Polling;
///             rt.PrimaryShardId = "shard-0";
///         });
/// });
/// </code>
/// <para>
/// Alternatively, entities can be registered without the attribute by using explicit
/// registration via <c>AddReferenceTable&lt;T&gt;()</c>.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ReferenceTableAttribute : Attribute;
