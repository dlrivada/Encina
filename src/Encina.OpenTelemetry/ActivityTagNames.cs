namespace Encina.OpenTelemetry;

/// <summary>
/// Defines constant tag names for OpenTelemetry messaging activities.
/// </summary>
/// <remarks>
/// These constants follow OpenTelemetry semantic conventions for messaging systems
/// with Encina-specific values and extensions.
/// </remarks>
public static class ActivityTagNames
{
    /// <summary>
    /// Messaging system tags following OpenTelemetry semantic conventions.
    /// </summary>
    public static class Messaging
    {
        /// <summary>Tag name for the messaging system identifier.</summary>
        public const string System = "messaging.system";

        /// <summary>Tag name for the operation name (publish, receive, schedule).</summary>
        public const string OperationName = "messaging.operation.name";

        /// <summary>Tag name for the message identifier.</summary>
        public const string MessageId = "messaging.message.id";

        /// <summary>Tag name for the message type.</summary>
        public const string MessageType = "messaging.message.type";

        /// <summary>Tag name indicating if the message was processed.</summary>
        public const string Processed = "messaging.message.processed";

        /// <summary>Tag name for the timestamp when the message was processed.</summary>
        public const string ProcessedAt = "messaging.message.processed_at";

        /// <summary>Tag name for the retry count.</summary>
        public const string RetryCount = "messaging.message.retry_count";

        /// <summary>Tag name for the error message.</summary>
        public const string Error = "messaging.message.error";

        /// <summary>Tag name for the scheduled execution time.</summary>
        public const string ScheduledAt = "messaging.message.scheduled_at";

        /// <summary>Tag name indicating if the message is recurring.</summary>
        public const string IsRecurring = "messaging.message.is_recurring";

        /// <summary>Tag name for the cron expression.</summary>
        public const string CronExpression = "messaging.message.cron_expression";
    }

    /// <summary>
    /// Messaging system identifier values.
    /// </summary>
    public static class Systems
    {
        /// <summary>Outbox messaging system identifier.</summary>
        public const string Outbox = "encina.outbox";

        /// <summary>Inbox messaging system identifier.</summary>
        public const string Inbox = "encina.inbox";

        /// <summary>Scheduling messaging system identifier.</summary>
        public const string Scheduling = "encina.scheduling";
    }

    /// <summary>
    /// Messaging operation name values.
    /// </summary>
    public static class Operations
    {
        /// <summary>Publish operation for outbox messages.</summary>
        public const string Publish = "publish";

        /// <summary>Receive operation for inbox messages.</summary>
        public const string Receive = "receive";

        /// <summary>Schedule operation for scheduled messages.</summary>
        public const string Schedule = "schedule";
    }

    /// <summary>
    /// Event metadata tags for correlation and causation tracking.
    /// </summary>
    /// <remarks>
    /// These tags are used to link event sourcing events with OpenTelemetry traces,
    /// enabling end-to-end tracing of event flows across distributed systems.
    /// </remarks>
    public static class EventMetadata
    {
        /// <summary>Tag name for the event identifier.</summary>
        public const string MessageId = "event.message_id";

        /// <summary>Tag name for the correlation identifier linking related events.</summary>
        public const string CorrelationId = "event.correlation_id";

        /// <summary>Tag name for the causation identifier linking cause-effect events.</summary>
        public const string CausationId = "event.causation_id";

        /// <summary>Tag name for the event stream identifier.</summary>
        public const string StreamId = "event.stream_id";

        /// <summary>Tag name for the event type name.</summary>
        public const string TypeName = "event.type_name";

        /// <summary>Tag name for the event version within the stream.</summary>
        public const string Version = "event.version";

        /// <summary>Tag name for the event sequence number (global position).</summary>
        public const string Sequence = "event.sequence";

        /// <summary>Tag name for the event timestamp.</summary>
        public const string Timestamp = "event.timestamp";
    }

    /// <summary>
    /// Saga-specific tags.
    /// </summary>
    public static class Saga
    {
        /// <summary>Tag name for the saga identifier.</summary>
        public const string Id = "saga.id";

        /// <summary>Tag name for the saga type.</summary>
        public const string Type = "saga.type";

        /// <summary>Tag name for the saga status.</summary>
        public const string Status = "saga.status";

        /// <summary>Tag name for the current saga step.</summary>
        public const string CurrentStep = "saga.current_step";

        /// <summary>Tag name for the saga completion timestamp.</summary>
        public const string CompletedAt = "saga.completed_at";

        /// <summary>Tag name for the saga error message.</summary>
        public const string Error = "saga.error";
    }

    /// <summary>
    /// CDC (Change Data Capture) tags for sharded CDC observability.
    /// </summary>
    /// <remarks>
    /// These tags track CDC connector operations, shard-level event processing,
    /// and position tracking across sharded database topologies.
    /// </remarks>
    public static class Cdc
    {
        /// <summary>Tag name for the CDC connector identifier.</summary>
        public const string ConnectorId = "cdc.connector_id";

        /// <summary>Tag name for the shard identifier in sharded CDC.</summary>
        public const string ShardId = "cdc.shard_id";

        /// <summary>Tag name for the CDC change operation type (insert, update, delete, snapshot).</summary>
        public const string Operation = "cdc.operation";

        /// <summary>Tag name for the database table name.</summary>
        public const string TableName = "cdc.table_name";

        /// <summary>Tag name for the CDC position string representation.</summary>
        public const string Position = "cdc.position";

        /// <summary>Tag name for the number of events in a capture batch.</summary>
        public const string EventsCount = "cdc.events_count";
    }

    /// <summary>
    /// Co-location group tags for sharding observability.
    /// </summary>
    /// <remarks>
    /// These tags track co-location group membership and routing decisions,
    /// enabling visibility into whether related entities are being correctly
    /// placed on the same shard.
    /// </remarks>
    public static class Colocation
    {
        /// <summary>Tag name for the co-location group name (root entity type name).</summary>
        public const string Group = "encina.sharding.colocation.group";

        /// <summary>Tag name indicating whether the routed entity belongs to a co-location group.</summary>
        public const string IsColocated = "encina.sharding.colocation.is_colocated";

        /// <summary>Tag name for the root entity type of the co-location group.</summary>
        public const string RootEntity = "encina.sharding.colocation.root_entity";
    }

    /// <summary>
    /// Time-based sharding tier tags for observability.
    /// </summary>
    /// <remarks>
    /// These tags track tier lifecycle operations including tier transitions,
    /// shard period context, and routing decisions by storage tier.
    /// </remarks>
    public static class Tiering
    {
        /// <summary>Tag name for the current storage tier of a shard.</summary>
        public const string ShardTier = "shard.tier";

        /// <summary>Tag name for the shard period label (e.g., "2026-02").</summary>
        public const string ShardPeriod = "shard.period";

        /// <summary>Tag name for the source tier in a tier transition.</summary>
        public const string TierFrom = "tier.from";

        /// <summary>Tag name for the target tier in a tier transition.</summary>
        public const string TierTo = "tier.to";
    }

    /// <summary>
    /// Shadow sharding tags for testing new shard topologies under production traffic.
    /// </summary>
    /// <remarks>
    /// These tags track shadow routing comparisons, dual-write outcomes, and
    /// shadow read result matching during topology migration validation.
    /// </remarks>
    public static class Shadow
    {
        /// <summary>Tag name for the production shard identifier.</summary>
        public const string ProductionShard = "encina.sharding.shadow.production_shard";

        /// <summary>Tag name for the shadow shard identifier.</summary>
        public const string ShadowShard = "encina.sharding.shadow.shadow_shard";

        /// <summary>Tag name indicating whether routing decisions match between production and shadow.</summary>
        public const string RoutingMatch = "encina.sharding.shadow.routing_match";

        /// <summary>Tag name for the outcome of a shadow write operation (success/failure).</summary>
        public const string WriteOutcome = "encina.sharding.shadow.write_outcome";

        /// <summary>Tag name indicating whether read results match between production and shadow.</summary>
        public const string ReadResultsMatch = "encina.sharding.shadow.read_results_match";
    }

    /// <summary>
    /// Reference table replication tags for sharding observability.
    /// </summary>
    /// <remarks>
    /// These tags track reference table replication operations including
    /// entity type, rows synced, shard count, duration, and change detection.
    /// </remarks>
    public static class ReferenceTable
    {
        /// <summary>Tag name for the reference table entity type.</summary>
        public const string EntityType = "reference_table.entity_type";

        /// <summary>Tag name for the number of rows synced during replication.</summary>
        public const string RowsSynced = "reference_table.rows_synced";

        /// <summary>Tag name for the number of shards targeted during replication.</summary>
        public const string ShardCount = "reference_table.shard_count";

        /// <summary>Tag name for the replication duration in milliseconds.</summary>
        public const string DurationMs = "reference_table.duration_ms";

        /// <summary>Tag name indicating whether a change was detected during polling.</summary>
        public const string ChangeDetected = "reference_table.change_detected";

        /// <summary>Tag name for the content hash value.</summary>
        public const string HashValue = "reference_table.hash";
    }
}
