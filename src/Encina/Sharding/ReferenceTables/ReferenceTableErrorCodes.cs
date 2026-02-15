namespace Encina.Sharding.ReferenceTables;

/// <summary>
/// Error codes emitted by the Encina reference table replication infrastructure.
/// </summary>
/// <remarks>
/// <para>
/// All error codes follow the <c>encina.reference_table.*</c> namespace convention and are
/// returned inside <c>Either&lt;EncinaError, T&gt;</c> results throughout the reference table
/// API. These codes are also emitted as OpenTelemetry tags on replication activity spans,
/// enabling correlation between ROP error paths and distributed traces.
/// </para>
/// <para>
/// Error codes are stable string constants suitable for alerting rules, log filters, and
/// dashboard queries. They never change between releases.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// Either&lt;EncinaError, ReplicationResult&gt; result = await replicator.ReplicateAsync&lt;Country&gt;(ct);
///
/// result.Match(
///     Right: rep => logger.LogInformation("Synced {Rows} rows to {Shards} shards", rep.RowsSynced, rep.ShardResults.Count),
///     Left: error =>
///     {
///         if (error.GetCode().IfNone("") == ReferenceTableErrorCodes.PrimaryShardNotFound)
///             logger.LogError("Primary shard is missing from topology");
///     });
/// </code>
/// </example>
public static class ReferenceTableErrorCodes
{
    /// <summary>The entity type is not registered as a reference table.</summary>
    public const string EntityNotRegistered = "encina.reference_table.entity_not_registered";

    /// <summary>The primary shard ID does not exist in the shard topology.</summary>
    public const string PrimaryShardNotFound = "encina.reference_table.primary_shard_not_found";

    /// <summary>No active target shards are available for replication.</summary>
    public const string NoTargetShards = "encina.reference_table.no_target_shards";

    /// <summary>Reading data from the primary shard failed.</summary>
    public const string PrimaryReadFailed = "encina.reference_table.primary_read_failed";

    /// <summary>Replication to one or more target shards failed.</summary>
    public const string ReplicationPartialFailure = "encina.reference_table.replication_partial_failure";

    /// <summary>Replication to all target shards failed.</summary>
    public const string ReplicationFailed = "encina.reference_table.replication_failed";

    /// <summary>Computing the content hash for change detection failed.</summary>
    public const string HashComputationFailed = "encina.reference_table.hash_computation_failed";

    /// <summary>The reference table store (upsert provider) is not registered.</summary>
    public const string StoreNotRegistered = "encina.reference_table.store_not_registered";

    /// <summary>The configured batch size is invalid (must be greater than zero).</summary>
    public const string InvalidBatchSize = "encina.reference_table.invalid_batch_size";

    /// <summary>The configured polling interval is invalid (must be positive).</summary>
    public const string InvalidPollingInterval = "encina.reference_table.invalid_polling_interval";

    /// <summary>The entity type does not have the <see cref="ReferenceTableAttribute"/> and was not explicitly registered.</summary>
    public const string MissingAttribute = "encina.reference_table.missing_attribute";

    /// <summary>A replication operation timed out.</summary>
    public const string ReplicationTimeout = "encina.reference_table.replication_timeout";

    /// <summary>Upserting entities to a target shard failed.</summary>
    public const string UpsertFailed = "encina.reference_table.upsert_failed";

    /// <summary>Reading all entities from a shard failed.</summary>
    public const string GetAllFailed = "encina.reference_table.get_all_failed";

    /// <summary>The entity type does not have a discoverable primary key.</summary>
    public const string NoPrimaryKeyFound = "encina.reference_table.no_primary_key_found";
}
