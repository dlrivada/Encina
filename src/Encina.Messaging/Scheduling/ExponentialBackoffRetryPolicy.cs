namespace Encina.Messaging.Scheduling;

/// <summary>
/// Default <see cref="IScheduledMessageRetryPolicy"/> implementation using exponential
/// backoff anchored on <see cref="SchedulingOptions.BaseRetryDelay"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Formula</b>: <c>delay = BaseRetryDelay * 2^retryCount</c>. With the default
/// <c>BaseRetryDelay = 5s</c>:
/// </para>
/// <list type="table">
/// <listheader>
///   <term>retryCount</term>
///   <description>delay until next attempt</description>
/// </listheader>
/// <item><term>0 (first failure)</term><description>5 s</description></item>
/// <item><term>1</term><description>10 s</description></item>
/// <item><term>2</term><description>20 s</description></item>
/// <item><term>3</term><description>40 s</description></item>
/// </list>
/// <para>
/// <b>Dead-letter rule</b>: when <c>retryCount + 1 &gt;= maxRetries</c> the policy
/// returns <see cref="RetryDecision.DeadLetter"/>, signalling the orchestrator to
/// stop retrying. The store interprets a <see langword="null"/>
/// <c>nextRetryAtUtc</c> as "no further retries".
/// </para>
/// <para>
/// <b>Determinism</b>: the policy is stateless and depends only on its constructor
/// arguments and the <c>nowUtc</c> value supplied by the orchestrator. It is
/// therefore safe to register as a singleton and trivially testable.
/// </para>
/// </remarks>
public sealed class ExponentialBackoffRetryPolicy : IScheduledMessageRetryPolicy
{
    private readonly SchedulingOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExponentialBackoffRetryPolicy"/> class.
    /// </summary>
    /// <param name="options">
    /// The scheduling options. Only <see cref="SchedulingOptions.BaseRetryDelay"/> is
    /// consumed; <see cref="SchedulingOptions.MaxRetries"/> is supplied per-call by the
    /// orchestrator so the same policy instance can serve multiple orchestrators.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> is <see langword="null"/>.
    /// </exception>
    public ExponentialBackoffRetryPolicy(SchedulingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
    }

    /// <inheritdoc />
    public RetryDecision Compute(int retryCount, int maxRetries, DateTime nowUtc)
    {
        if (retryCount + 1 >= maxRetries)
            return RetryDecision.DeadLetter();

        var delayMs = _options.BaseRetryDelay.TotalMilliseconds * Math.Pow(2, retryCount);
        return RetryDecision.RetryAt(nowUtc.AddMilliseconds(delayMs));
    }
}
