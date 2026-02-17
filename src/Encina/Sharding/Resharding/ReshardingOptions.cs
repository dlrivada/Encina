namespace Encina.Sharding.Resharding;

/// <summary>
/// Configuration options for an online resharding operation.
/// </summary>
/// <remarks>
/// <para>
/// These options control the behavior of each phase in the resharding workflow.
/// Sensible defaults are provided for all settings.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var options = new ReshardingOptions
/// {
///     CopyBatchSize = 10_000,
///     CdcLagThreshold = TimeSpan.FromSeconds(5),
///     VerificationMode = VerificationMode.CountAndChecksum,
///     CutoverTimeout = TimeSpan.FromSeconds(30),
///     CleanupRetentionPeriod = TimeSpan.FromHours(24),
/// };
/// </code>
/// </example>
public sealed class ReshardingOptions
{
    /// <summary>
    /// Gets or sets the number of rows to copy per batch during the copy phase.
    /// </summary>
    /// <value>Default is 10,000.</value>
    public int CopyBatchSize { get; set; } = 10_000;

    /// <summary>
    /// Gets or sets the maximum acceptable CDC replication lag before the replication phase
    /// considers itself caught up and transitions to the verification phase.
    /// </summary>
    /// <value>Default is 5 seconds.</value>
    public TimeSpan CdcLagThreshold { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the verification strategy used to validate data consistency
    /// between source and target shards.
    /// </summary>
    /// <value>Default is <see cref="Resharding.VerificationMode.CountAndChecksum"/>.</value>
    public VerificationMode VerificationMode { get; set; } = VerificationMode.CountAndChecksum;

    /// <summary>
    /// Gets or sets the maximum duration for the cutover phase.
    /// If the topology switch exceeds this timeout, the operation is rolled back.
    /// </summary>
    /// <value>Default is 30 seconds.</value>
    public TimeSpan CutoverTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets how long to retain migrated rows on the source shard before cleanup.
    /// This provides a safety window for rollback after cutover.
    /// </summary>
    /// <value>Default is 24 hours.</value>
    public TimeSpan CleanupRetentionPeriod { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Gets or sets an optional callback invoked after each phase completes successfully.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this for monitoring, alerting, or custom logic between phases.
    /// The callback receives the completed phase and the current progress snapshot.
    /// </para>
    /// </remarks>
    public Func<ReshardingPhase, ReshardingProgress, Task>? OnPhaseCompleted { get; set; }

    /// <summary>
    /// Gets or sets an optional async predicate invoked just before the cutover phase begins.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Return <c>true</c> to proceed with the cutover, or <c>false</c> to abort.
    /// This allows external validation (e.g., checking system health, confirming with operators)
    /// before committing to the brief read-only window.
    /// </para>
    /// </remarks>
    public Func<ReshardingPlan, CancellationToken, Task<bool>>? OnCutoverStarting { get; set; }
}
