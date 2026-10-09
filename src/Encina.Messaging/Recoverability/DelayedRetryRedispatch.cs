namespace Encina.Messaging.Recoverability;

/// <summary>
/// Marks the dispatch of a delayed retry, so <see cref="RecoverabilityPipelineBehavior{TRequest, TResponse}"/>
/// runs the handler with its immediate retries but starts no new retry chain: the
/// <see cref="DelayedRetryProcessor"/> alone decides attempt N+1 or permanent failure.
/// </summary>
/// <remarks>
/// <para>
/// The marker is ambient (an <see cref="AsyncLocal{T}"/>) and bound to the request instance being
/// re-dispatched, so a handler that sends other requests through the same <see cref="IEncina"/>
/// does not inherit it. It is internal: application code cannot set it, so it cannot skip
/// recoverability by accident, and a request with no marker (a first-time failure) always schedules
/// delayed retry attempt 0.
/// </para>
/// <para>
/// The behavior reports the classification of the last error back through
/// <see cref="Classification"/>, so a permanent error goes straight to the permanent-failure path.
/// </para>
/// </remarks>
internal sealed class DelayedRetryRedispatch
{
    private static readonly AsyncLocal<DelayedRetryRedispatch?> Current = new();

    private DelayedRetryRedispatch(object request)
    {
        Request = request;
    }

    /// <summary>Gets the request instance being re-dispatched.</summary>
    public object Request { get; }

    /// <summary>
    /// Gets the classification of the last error the behavior saw, <see cref="ErrorClassification.Unknown"/>
    /// when the dispatch did not pass through the behavior.
    /// </summary>
    public ErrorClassification Classification { get; private set; } = ErrorClassification.Unknown;

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
    /// Gets the marker when <paramref name="request"/> is the instance being re-dispatched;
    /// <see langword="null"/> for a first-time failure or any other request.
    /// </summary>
    public static DelayedRetryRedispatch? For(object request) =>
        Current.Value is { } marker && ReferenceEquals(marker.Request, request) ? marker : null;

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
