using System.Runtime.CompilerServices;

namespace Encina;

/// <summary>
/// Sets and restores the ambient <see cref="IRequestContext"/> around a dispatch.
/// </summary>
internal static class AmbientRequestContext
{
    /// <summary>
    /// Resolves the context a dispatch runs with: the explicit one, else the ambient one,
    /// else a fresh context (correlation id from <see cref="System.Diagnostics.Activity.Current"/>).
    /// </summary>
    public static IRequestContext Resolve(IRequestContextAccessor accessor, IRequestContext? explicitContext)
        => explicitContext ?? accessor.RequestContext ?? RequestContext.Create();

    /// <summary>
    /// Makes <paramref name="context"/> the ambient context until the returned scope is disposed,
    /// which restores the previous value.
    /// </summary>
    public static Scope Enter(IRequestContextAccessor accessor, IRequestContext context)
    {
        var previous = accessor.RequestContext;
        if (ReferenceEquals(previous, context))
        {
            // Already ambient (the usual case behind EncinaContextMiddleware): nothing to set or
            // restore, and no execution-context copy is allocated on the hot path.
            return default;
        }

        accessor.RequestContext = context;
        return new Scope(accessor, previous);
    }

    /// <summary>
    /// Enumerates <paramref name="source"/> with <paramref name="context"/> as the ambient context
    /// during every step, including disposal.
    /// </summary>
    /// <remarks>
    /// An async iterator resumes on the consumer's execution context at every
    /// <c>MoveNextAsync</c>, so an ambient value set once inside the iterator would be lost after
    /// the first item. Entering the scope around each step keeps the context visible to the
    /// handler and its behaviors for the whole stream.
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
    /// Restores the previous ambient context on disposal.
    /// </summary>
    internal readonly struct Scope : IDisposable
    {
        private readonly IRequestContextAccessor? _accessor;
        private readonly IRequestContext? _previous;

        internal Scope(IRequestContextAccessor accessor, IRequestContext? previous)
        {
            _accessor = accessor;
            _previous = previous;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            // A default scope (context was already ambient) restores nothing.
            if (_accessor is not null)
            {
                _accessor.RequestContext = _previous;
            }
        }
    }
}
