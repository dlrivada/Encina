using System.Runtime.CompilerServices;

namespace Encina;

/// <summary>
/// Sets and restores the ambient <see cref="IRequestContext"/> around a dispatch, and decides which
/// context a dispatch runs with.
/// </summary>
/// <remarks>
/// <para>
/// Besides the accessor value, this class tracks whether a dispatch is in flight in the current
/// logical call. That flag is what separates an <em>entry point</em> (the first dispatch of a flow,
/// seeded from the context an entry point such as <c>EncinaContextMiddleware</c> put on the
/// accessor) from a <em>nested</em> dispatch (a <c>Send</c>, <c>Publish</c> or <c>Stream</c> issued
/// while another dispatch is running: from a handler, a behavior, a notification handler, a domain
/// event handler or an EF Core interceptor). The flag is private to the dispatcher, so it works with
/// any <see cref="IRequestContextAccessor"/> implementation.
/// </para>
/// </remarks>
internal static class AmbientRequestContext
{
    // True while a dispatch runs in this logical call. Kept apart from the accessor value because the
    // accessor also holds contexts set by entry points (middleware), which are not dispatches.
    private static readonly AsyncLocal<bool> DispatchInFlight = new();

    /// <summary>
    /// Gets a value indicating whether a dispatch is in flight in the current logical call.
    /// </summary>
    internal static bool IsDispatchInFlight => DispatchInFlight.Value;

    /// <summary>
    /// Resolves the context a dispatch runs with.
    /// </summary>
    /// <remarks>
    /// <list type="number">
    /// <item><description>An explicit context is used as-is.</description></item>
    /// <item><description>With no ambient context, a fresh one is created (correlation id from
    /// <see cref="System.Diagnostics.Activity.Current"/>).</description></item>
    /// <item><description>An ambient context seen while no dispatch is in flight belongs to an entry
    /// point and is used as-is, idempotency key included.</description></item>
    /// <item><description>An ambient context seen while another dispatch is in flight is inherited
    /// from that outer dispatch: the nested dispatch gets a derived context with the same
    /// correlation id, user, tenant and metadata, its own timestamp, and <b>no</b> idempotency
    /// key, so it never collides with the outer request in the idempotency stores.</description></item>
    /// </list>
    /// </remarks>
    public static IRequestContext Resolve(IRequestContextAccessor accessor, IRequestContext? explicitContext, TimeProvider timeProvider)
    {
        if (explicitContext is not null)
        {
            return explicitContext;
        }

        var ambient = accessor.RequestContext;
        if (ambient is null)
        {
            return RequestContext.CreateAt(timeProvider.GetUtcNow());
        }

        return DispatchInFlight.Value
            ? RequestContext.ForNestedDispatch(ambient, timeProvider.GetUtcNow())
            : ambient;
    }

    /// <summary>
    /// Makes <paramref name="context"/> the ambient context and marks a dispatch as in flight until
    /// the returned scope is disposed, which restores both previous values.
    /// </summary>
    public static Scope Enter(IRequestContextAccessor accessor, IRequestContext context)
    {
        var previous = accessor.RequestContext;

        // Already ambient (the usual case behind EncinaContextMiddleware): nothing to set or restore.
        var setContext = !ReferenceEquals(previous, context);
        if (setContext)
        {
            accessor.RequestContext = context;
        }

        // Nested dispatches find the flag already set; only the outermost one sets and clears it.
        var enterDispatch = !DispatchInFlight.Value;
        if (enterDispatch)
        {
            DispatchInFlight.Value = true;
        }

        return new Scope(setContext ? accessor : null, previous, enterDispatch);
    }

    /// <summary>
    /// Enumerates <paramref name="source"/> with <paramref name="context"/> as the ambient context
    /// during every step, including disposal.
    /// </summary>
    /// <remarks>
    /// An async iterator resumes on the consumer's execution context at every
    /// <c>MoveNextAsync</c>, so an ambient value set once inside the iterator would be lost after
    /// the first item. Entering the scope around each step keeps the context visible to the
    /// handler and its behaviors for the whole stream, while the consumer's code between items runs
    /// with the consumer's own ambient context.
    /// </remarks>
    public static async IAsyncEnumerable<T> Flow<T>(
        IAsyncEnumerable<T> source,
        IRequestContextAccessor accessor,
        IRequestContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IAsyncEnumerator<T> enumerator;
        using (Enter(accessor, context))
        {
            enumerator = source.GetAsyncEnumerator(cancellationToken);
        }

        try
        {
            while (true)
            {
                bool moved;
                using (Enter(accessor, context))
                {
                    moved = await enumerator.MoveNextAsync().ConfigureAwait(false);
                }

                if (!moved)
                {
                    yield break;
                }

                yield return enumerator.Current;
            }
        }
        finally
        {
            using (Enter(accessor, context))
            {
                await enumerator.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Restores the previous ambient context and dispatch flag on disposal.
    /// </summary>
    internal readonly struct Scope : IDisposable
    {
        private readonly IRequestContextAccessor? _accessor;
        private readonly IRequestContext? _previous;
        private readonly bool _leaveDispatch;

        internal Scope(IRequestContextAccessor? accessor, IRequestContext? previous, bool leaveDispatch)
        {
            _accessor = accessor;
            _previous = previous;
            _leaveDispatch = leaveDispatch;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            // A null accessor means the context was already ambient: there is nothing to restore.
            if (_accessor is not null)
            {
                _accessor.RequestContext = _previous;
            }

            if (_leaveDispatch)
            {
                DispatchInFlight.Value = false;
            }
        }
    }
}
