namespace SimpleMediator;

/// <summary>
/// Configuration options for notification dispatch behavior.
/// </summary>
/// <remarks>
/// <para>Controls how notifications are dispatched to multiple handlers.</para>
/// <para>By default, notifications are dispatched sequentially with fail-fast semantics.
/// Parallel dispatch can be enabled for improved throughput when handlers are independent.</para>
/// </remarks>
/// <example>
/// <code>
/// services.AddSimpleMediator(config =>
/// {
///     config.NotificationDispatch.Strategy = NotificationDispatchStrategy.Parallel;
///     config.NotificationDispatch.MaxDegreeOfParallelism = 4;
/// });
/// </code>
/// </example>
public sealed class NotificationDispatchOptions
{
    /// <summary>
    /// Gets or sets the dispatch strategy for notifications.
    /// Default is <see cref="NotificationDispatchStrategy.Sequential"/>.
    /// </summary>
    public NotificationDispatchStrategy Strategy { get; set; } = NotificationDispatchStrategy.Sequential;

    /// <summary>
    /// Gets or sets the maximum degree of parallelism for parallel strategies.
    /// Default is -1 (uses <see cref="Environment.ProcessorCount"/>).
    /// </summary>
    /// <remarks>
    /// Only applies when <see cref="Strategy"/> is <see cref="NotificationDispatchStrategy.Parallel"/>
    /// or <see cref="NotificationDispatchStrategy.ParallelWhenAll"/>.
    /// </remarks>
    public int MaxDegreeOfParallelism { get; set; } = -1;
}

/// <summary>
/// Defines how notifications are dispatched to handlers.
/// </summary>
public enum NotificationDispatchStrategy
{
    /// <summary>
    /// Handlers execute one at a time, in registration order.
    /// First error stops execution (fail-fast). This is the default.
    /// </summary>
    Sequential = 0,

    /// <summary>
    /// Handlers execute concurrently. First error cancels remaining handlers (fail-fast).
    /// Uses <see cref="CancellationTokenSource"/> to cancel pending handlers on first failure.
    /// </summary>
    Parallel = 1,

    /// <summary>
    /// Handlers execute concurrently. All handlers complete before returning.
    /// Errors are aggregated into a single <see cref="MediatorError"/> with details of all failures.
    /// </summary>
    ParallelWhenAll = 2
}
