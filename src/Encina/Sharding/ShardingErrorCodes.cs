namespace Encina.Sharding;

/// <summary>
/// Error codes emitted by the Encina sharding infrastructure.
/// </summary>
/// <remarks>
/// <para>
/// All error codes follow the <c>encina.sharding.*</c> namespace convention and are returned
/// inside <c>Either&lt;EncinaError, T&gt;</c> results throughout the sharding API. These codes
/// are also emitted as OpenTelemetry tags (<c>encina.sharding.error.code</c>) on routing and
/// scatter-gather activity spans, enabling correlation between ROP error paths and distributed
/// traces.
/// </para>
/// <para>
/// Error codes are stable string constants suitable for alerting rules, log filters, and
/// dashboard queries. They never change between releases.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Error codes appear in Either results
/// Either&lt;EncinaError, string&gt; result = router.GetShardId("my-key");
///
/// result.Match(
///     Right: shardId => logger.LogInformation("Routed to {ShardId}", shardId),
///     Left: error =>
///     {
///         // error.Code is one of ShardingErrorCodes constants
///         if (error.Code == ShardingErrorCodes.ShardNotFound)
///             logger.LogWarning("Shard topology may be stale");
///         else if (error.Code == ShardingErrorCodes.ShardKeyEmpty)
///             logger.LogError("Entity is missing a shard key");
///     });
/// </code>
/// </example>
public static class ShardingErrorCodes
{
    /// <summary>Shard key is not configured on the entity.</summary>
    public const string ShardKeyNotConfigured = "encina.sharding.shard_key_not_configured";

    /// <summary>Shard key value is null or empty.</summary>
    public const string ShardKeyEmpty = "encina.sharding.shard_key_empty";

    /// <summary>The requested shard was not found in the topology.</summary>
    public const string ShardNotFound = "encina.sharding.shard_not_found";

    /// <summary>The shard key does not match any configured range.</summary>
    public const string KeyOutsideRange = "encina.sharding.key_outside_range";

    /// <summary>Overlapping ranges were detected during configuration.</summary>
    public const string OverlappingRanges = "encina.sharding.overlapping_ranges";

    /// <summary>The shard topology has no active shards.</summary>
    public const string NoActiveShards = "encina.sharding.no_active_shards";

    /// <summary>The shard router is not configured.</summary>
    public const string RouterNotConfigured = "encina.sharding.router_not_configured";

    /// <summary>A shard ID is missing a connection string.</summary>
    public const string MissingConnectionString = "encina.sharding.missing_connection_string";

    /// <summary>A geo region was not found and no fallback is configured.</summary>
    public const string RegionNotFound = "encina.sharding.region_not_found";

    /// <summary>Scatter-gather operation timed out.</summary>
    public const string ScatterGatherTimeout = "encina.sharding.scatter_gather_timeout";

    /// <summary>Partial failures occurred during scatter-gather.</summary>
    public const string ScatterGatherPartialFailure = "encina.sharding.scatter_gather_partial_failure";

    /// <summary>A cache operation failed during sharding.</summary>
    public const string CacheOperationFailed = "encina.sharding.cache_operation_failed";

    /// <summary>A topology refresh operation failed.</summary>
    public const string TopologyRefreshFailed = "encina.sharding.topology_refresh_failed";

    /// <summary>A compound shard key has no components.</summary>
    public const string CompoundShardKeyEmpty = "encina.sharding.compound_shard_key_empty";

    /// <summary>A component within a compound shard key is null or empty.</summary>
    public const string CompoundShardKeyComponentEmpty = "encina.sharding.compound_shard_key_component_empty";

    /// <summary>Multiple properties share the same <see cref="ShardKeyAttribute.Order"/> value.</summary>
    public const string DuplicateShardKeyOrder = "encina.sharding.duplicate_shard_key_order";

    /// <summary>A partial key routing operation failed because the router does not support prefix routing.</summary>
    public const string PartialKeyRoutingFailed = "encina.sharding.partial_key_routing_failed";
}
