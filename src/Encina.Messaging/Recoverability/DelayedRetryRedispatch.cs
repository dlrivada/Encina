namespace Encina.Messaging.Recoverability;

/// <summary>
/// Marks the dispatch of a delayed retry, so <see cref="RecoverabilityPipelineBehavior{TRequest, TResponse}"/>
/// runs the handler with its immediate retries but starts no new retry chain: the
/// <see cref="DelayedRetryProcessor"/> alone decides attempt N+1 or permanent failure.
/// </summary>
/// <remarks>
/// <para>
/// The marker is ambient (an <see cref="AsyncLocal{T}"/>) for the whole dispatch of the re-dispatched
/// request and is matched to that request: by reference for a reference type, by value equality for a
/// value type (a boxed value has no stable reference identity). Every
/// <see cref="RecoverabilityPipelineBehavior{TRequest, TResponse}"/> invocation for that request sees
/// it, so an outer behavior that re-enters the pipeline (a retry or resilience behavior calling the next
/// step twice) keeps one chain. A handler that sends another request, or another instance of the same
/// type, is not matched and starts its own chain; a handler that sends an equal value-type request is
/// treated as the same request.
/// </para>
/// <para>
/// It is internal: application code cannot set it, so it cannot skip recoverability by accident, and
/// a request with no marker (a first-time failure) always schedules delayed retry attempt 0.
/// </para>
/// <para>
/// The behavior reports its own <see cref="RecoverabilityContext"/> back through <see cref="Report"/>,
/// so the processor sees the real classification, error, exception and history of the failure.
/// </para>
/// </remarks>
internal sealed class DelayedRetryRedispatch
{
    private static readonly AsyncLocal<DelayedRetryRedispatch?> Current = new();

    private DelayedRetryRedispatch(object request)
    {
        Request = request;
    }

    /// <summary>Gets the request being re-dispatched.</summary>
    public object Request { get; }

    /// <summary>
    /// Gets the recoverability context of the last invocation that reported, or <see langword="null"/>
    /// when the dispatch did not pass through the behavior.
    /// </summary>
    public RecoverabilityContext? Attempt { get; private set; }

    /// <summary>
    /// Gets the classification of the last error the behavior saw, <see cref="ErrorClassification.Unknown"/>
    /// when the dispatch did not pass through the behavior.
    /// </summary>
    public ErrorClassification Classification => Attempt?.LastClassification ?? ErrorClassification.Unknown;

    /// <summary>
    /// Marks the dispatch of <paramref name="request"/> as a delayed retry until the returned scope is disposed.
    /// </summary>
    public static Scope Begin(object request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var marker = new DelayedRetryRedispatch(request);
        Current.Value = marker;
        return new Scope(marker);
    }

    /// <summary>
    /// Gets the marker when <paramref name="request"/> is the request being re-dispatched;
    /// <see langword="null"/> for a first-time failure or any other request.
    /// </summary>
    public static DelayedRetryRedispatch? For(object request) =>
        Current.Value is { } marker && marker.Matches(request) ? marker : null;

    /// <summary>Records the recoverability context of the dispatch's last invocation.</summary>
    public void Report(RecoverabilityContext attempt) => Attempt = attempt;

    private bool Matches(object request) =>
        ReferenceEquals(Request, request) || (request.GetType().IsValueType && Request.Equals(request));

    /// <summary>Ends the marker on disposal.</summary>
    public readonly struct Scope : IDisposable
    {
        private readonly DelayedRetryRedispatch _marker;

        internal Scope(DelayedRetryRedispatch marker)
        {
            _marker = marker;
        }

        /// <summary>Gets the marker the dispatch reports into.</summary>
        public DelayedRetryRedispatch Marker => _marker;

        /// <inheritdoc />
        public void Dispose()
        {
            if (ReferenceEquals(Current.Value, _marker))
            {
                Current.Value = null;
            }
        }
    }
}
