namespace Encina.Sharding.Resharding;

/// <summary>
/// Error codes emitted by the Encina online resharding infrastructure.
/// </summary>
/// <remarks>
/// <para>
/// All error codes follow the <c>encina.sharding.resharding.*</c> namespace convention and
/// are returned inside <c>Either&lt;EncinaError, T&gt;</c> results from
/// <see cref="IReshardingOrchestrator"/> and <see cref="IReshardingStateStore"/> methods.
/// These codes are also emitted as OpenTelemetry tags on resharding activity spans,
/// enabling correlation between ROP error paths and distributed traces.
/// </para>
/// <para>
/// Error codes are stable string constants suitable for alerting rules, log filters, and
/// dashboard queries. They never change between releases.
/// </para>
/// </remarks>
public static class ReshardingErrorCodes
{
    /// <summary>The old and new topologies are identical — no resharding is needed.</summary>
    public const string TopologiesIdentical = "encina.sharding.resharding.topologies_identical";

    /// <summary>The resharding plan contains no migration steps.</summary>
    public const string EmptyPlan = "encina.sharding.resharding.empty_plan";

    /// <summary>Plan generation failed due to unreachable shards or estimation errors.</summary>
    public const string PlanGenerationFailed = "encina.sharding.resharding.plan_generation_failed";

    /// <summary>The bulk copy phase failed for one or more migration steps.</summary>
    public const string CopyFailed = "encina.sharding.resharding.copy_failed";

    /// <summary>The CDC replication phase failed or could not catch up within the configured threshold.</summary>
    public const string ReplicationFailed = "encina.sharding.resharding.replication_failed";

    /// <summary>Data verification detected mismatches between source and target shards.</summary>
    public const string VerificationFailed = "encina.sharding.resharding.verification_failed";

    /// <summary>The cutover phase exceeded the configured timeout.</summary>
    public const string CutoverTimeout = "encina.sharding.resharding.cutover_timeout";

    /// <summary>The cutover was aborted by the <see cref="ReshardingOptions.OnCutoverStarting"/> predicate.</summary>
    public const string CutoverAborted = "encina.sharding.resharding.cutover_aborted";

    /// <summary>The cutover phase failed during topology switch.</summary>
    public const string CutoverFailed = "encina.sharding.resharding.cutover_failed";

    /// <summary>The cleanup phase failed while removing migrated rows from source shards.</summary>
    public const string CleanupFailed = "encina.sharding.resharding.cleanup_failed";

    /// <summary>A rollback operation failed (e.g., data already cleaned up, topology already switched).</summary>
    public const string RollbackFailed = "encina.sharding.resharding.rollback_failed";

    /// <summary>No rollback metadata is available for the specified resharding result.</summary>
    public const string RollbackNotAvailable = "encina.sharding.resharding.rollback_not_available";

    /// <summary>The specified resharding operation was not found.</summary>
    public const string ReshardingNotFound = "encina.sharding.resharding.resharding_not_found";

    /// <summary>An invalid phase transition was attempted.</summary>
    public const string InvalidPhaseTransition = "encina.sharding.resharding.invalid_phase_transition";

    /// <summary>The state store operation failed (save, load, or delete).</summary>
    public const string StateStoreFailed = "encina.sharding.resharding.state_store_failed";

    /// <summary>A resharding operation is already active and concurrent resharding is not supported.</summary>
    public const string ConcurrentReshardingNotAllowed = "encina.sharding.resharding.concurrent_resharding_not_allowed";
}
