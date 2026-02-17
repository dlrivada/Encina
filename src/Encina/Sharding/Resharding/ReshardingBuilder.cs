namespace Encina.Sharding.Resharding;

/// <summary>
/// Fluent builder for configuring resharding options within the sharding setup pipeline.
/// </summary>
/// <remarks>
/// <para>
/// Use this builder via <see cref="Configuration.ShardingOptions{TEntity}.WithResharding"/>
/// to configure resharding behavior:
/// </para>
/// <code>
/// services.AddEncinaSharding&lt;Order&gt;(options =>
/// {
///     options.UseHashRouting()
///         .AddShard("shard-0", "conn0")
///         .AddShard("shard-1", "conn1")
///         .WithResharding(resharding =>
///         {
///             resharding.CopyBatchSize = 10_000;
///             resharding.CdcLagThreshold = TimeSpan.FromSeconds(5);
///             resharding.VerificationMode = VerificationMode.CountAndChecksum;
///             resharding.CutoverTimeout = TimeSpan.FromSeconds(30);
///             resharding.CleanupRetentionPeriod = TimeSpan.FromHours(24);
///             resharding.OnPhaseCompleted(async (phase, progress) =>
///             {
///                 logger.LogInformation("Phase {Phase} completed: {Percent}%",
///                     phase, progress.OverallPercentComplete);
///             });
///             resharding.OnCutoverStarting(async (plan, ct) =>
///             {
///                 // External validation before cutover
///                 return await healthChecker.IsSystemHealthyAsync(ct);
///             });
///         });
/// });
/// </code>
/// </remarks>
public sealed class ReshardingBuilder
{
    private readonly ReshardingOptions _options = new();

    /// <summary>
    /// Gets or sets the number of rows to copy per batch during the copy phase.
    /// </summary>
    /// <value>Default is 10,000.</value>
    public int CopyBatchSize
    {
        get => _options.CopyBatchSize;
        set => _options.CopyBatchSize = value;
    }

    /// <summary>
    /// Gets or sets the maximum acceptable CDC replication lag before transitioning to verification.
    /// </summary>
    /// <value>Default is 5 seconds.</value>
    public TimeSpan CdcLagThreshold
    {
        get => _options.CdcLagThreshold;
        set => _options.CdcLagThreshold = value;
    }

    /// <summary>
    /// Gets or sets the verification mode used to validate data consistency.
    /// </summary>
    /// <value>Default is <see cref="Resharding.VerificationMode.CountAndChecksum"/>.</value>
    public VerificationMode VerificationMode
    {
        get => _options.VerificationMode;
        set => _options.VerificationMode = value;
    }

    /// <summary>
    /// Gets or sets the maximum duration for the cutover phase.
    /// </summary>
    /// <value>Default is 30 seconds.</value>
    public TimeSpan CutoverTimeout
    {
        get => _options.CutoverTimeout;
        set => _options.CutoverTimeout = value;
    }

    /// <summary>
    /// Gets or sets how long to retain migrated rows on the source shard before cleanup.
    /// </summary>
    /// <value>Default is 24 hours.</value>
    public TimeSpan CleanupRetentionPeriod
    {
        get => _options.CleanupRetentionPeriod;
        set => _options.CleanupRetentionPeriod = value;
    }

    /// <summary>
    /// Sets a callback invoked after each phase completes successfully.
    /// </summary>
    /// <param name="callback">
    /// The async callback receiving the completed phase and current progress.
    /// </param>
    /// <returns>This builder for fluent chaining.</returns>
    public ReshardingBuilder OnPhaseCompleted(Func<ReshardingPhase, ReshardingProgress, Task> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        _options.OnPhaseCompleted = callback;
        return this;
    }

    /// <summary>
    /// Sets an async predicate invoked just before the cutover phase begins.
    /// </summary>
    /// <param name="predicate">
    /// The async predicate. Return <c>true</c> to proceed with cutover, <c>false</c> to abort.
    /// </param>
    /// <returns>This builder for fluent chaining.</returns>
    public ReshardingBuilder OnCutoverStarting(Func<ReshardingPlan, CancellationToken, Task<bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        _options.OnCutoverStarting = predicate;
        return this;
    }

    /// <summary>
    /// Builds the <see cref="ReshardingOptions"/> from the current builder state.
    /// </summary>
    internal ReshardingOptions Build() => _options;
}
