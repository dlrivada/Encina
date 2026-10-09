namespace Encina.Messaging.Recoverability;

/// <summary>
/// Marks the dispatch of a delayed retry, so <see cref="RecoverabilityPipelineBehavior{TRequest, TResponse}"/>
/// runs the handler with its immediate retries but starts no new retry chain: the
/// <see cref="DelayedRetryProcessor"/> alone decides attempt N+1 or permanent failure.
/// </summary>
/// <remarks>
/// <para>
/// The marker is ambient (an <see cref="AsyncLocal{T}"/>) for the duration of the processor's dispatch
/// and is bound to the runtime request type, not to the request instance, so it also works for value
/// type requests (a boxed value has no stable reference identity). The first
/// <see cref="RecoverabilityPipelineBehavior{TRequest, TResponse}"/> invocation for that request type
/// consumes it; a handler that sends the same type again, or another type, does not inherit it.
/// </para>
/// <para>
/// It is internal: application code cannot set it, so it cannot skip recoverability by accident, and
/// a request with no marker (a first-time failure) always schedules delayed retry attempt 0.
/// </para>
/// <para>
/// The behavior reports the classification of the last error back through
/// <see cref="Classification"/>, so a permanent error goes straight to the permanent-failure path.
/// </para>
/// </remarks>
internal sealed class DelayedRetryRedispatch
{
    private static readonly AsyncLocal<DelayedRetryRedispatch?> Current = new();

    private int _consumed;

    private DelayedRetryRedispatch(Type requestType)
    {
        RequestType = requestType;
    }

    /// <summary>Gets the runtime type of the request being re-dispatched.</summary>
    public Type RequestType { get; }

    /// <summary>
    /// Gets the classification of the last error the behavior saw, <see cref="ErrorClassification.Unknown"/>
    /// when the dispatch did not pass through the behavior.
    /// </summary>
    public ErrorClassification Classification { get; private set; } = ErrorClassification.Unknown;

    /// <summary>
    /// Marks the dispatch of a request of type <paramref name="requestType"/> as a delayed retry until
    /// the returned scope is disposed.
    /// </summary>
    public static Scope Begin(Type requestType)
    {
        ArgumentNullException.ThrowIfNull(requestType);

        var marker = new DelayedRetryRedispatch(requestType);
        Current.Value = marker;
        return new Scope(marker);
    }

    /// <summary>
    /// Takes the marker when the dispatch in flight is a re-dispatch of <paramref name="requestType"/>
    /// and nobody has taken it yet; <see langword="null"/> for a first-time failure, any other request
    /// type, or a nested send after the re-dispatched request's behavior has run.
    /// </summary>
    public static DelayedRetryRedispatch? Consume(Type requestType) =>
        Current.Value is { } marker
        && marker.RequestType == requestType
        && Interlocked.Exchange(ref marker._consumed, 1) == 0
            ? marker
            : null;

    /// <summary>Records the classification of the last error of the dispatch.</summary>
    public void Report(ErrorClassification classification) => Classification = classification;

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
