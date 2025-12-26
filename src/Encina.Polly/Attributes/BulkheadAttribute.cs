namespace Encina.Polly;

/// <summary>
/// Configures bulkhead isolation for a request to limit concurrent executions.
/// Prevents cascade failures by isolating resource usage per handler type.
/// </summary>
/// <remarks>
/// <para>
/// The bulkhead pattern limits the number of concurrent executions for a handler type:
/// </para>
/// <list type="bullet">
/// <item><description><b>MaxConcurrency</b>: Maximum parallel executions allowed.</description></item>
/// <item><description><b>MaxQueuedActions</b>: Additional requests that can wait in queue.</description></item>
/// <item><description>When both limits are reached, requests are rejected immediately.</description></item>
/// </list>
/// <para>
/// Use this pattern to:
/// </para>
/// <list type="bullet">
/// <item><description>Prevent a slow handler from consuming all available threads.</description></item>
/// <item><description>Isolate resource-intensive operations from affecting other handlers.</description></item>
/// <item><description>Limit concurrent database connections or external API calls.</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Limit payment processing to 10 concurrent executions
/// [Bulkhead(MaxConcurrency = 10, MaxQueuedActions = 20)]
/// public record ProcessPaymentCommand(PaymentData Data) : ICommand&lt;PaymentResult&gt;;
///
/// // Limit external API calls with custom timeout
/// [Bulkhead(
///     MaxConcurrency = 5,
///     MaxQueuedActions = 10,
///     QueueTimeoutMs = 5000)]
/// public record CallExternalApiQuery(string Endpoint) : IRequest&lt;ApiResponse&gt;;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class BulkheadAttribute : Attribute
{
    /// <summary>
    /// Maximum number of concurrent executions allowed.
    /// </summary>
    /// <remarks>
    /// Default: 10 concurrent executions.
    /// When this limit is reached, new requests are queued up to <see cref="MaxQueuedActions"/>.
    /// </remarks>
    public int MaxConcurrency { get; init; } = 10;

    /// <summary>
    /// Maximum number of actions that can be queued when concurrency limit is reached.
    /// </summary>
    /// <remarks>
    /// Default: 20 queued actions.
    /// When both <see cref="MaxConcurrency"/> and this limit are reached, requests are rejected.
    /// Set to 0 to disable queueing (immediate rejection when concurrency limit reached).
    /// </remarks>
    public int MaxQueuedActions { get; init; } = 20;

    /// <summary>
    /// Maximum time in milliseconds to wait in the queue.
    /// </summary>
    /// <remarks>
    /// Default: 30000ms (30 seconds).
    /// If a queued request exceeds this timeout, it is rejected.
    /// </remarks>
    public int QueueTimeoutMs { get; init; } = 30000;
}
