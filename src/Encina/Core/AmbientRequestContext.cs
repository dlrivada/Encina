using System.Runtime.CompilerServices;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

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

    // The identity kind of the dispatch running in this logical call, read by the dispatch activity
    // (encina.identity.kind). Kept here so the activity tag does not depend on the accessor implementation.
    private static readonly AsyncLocal<IdentityKind> DispatchIdentity = new();

    /// <summary>
    /// Gets the identity kind of the dispatch in flight in the current logical call
    /// (<see cref="IdentityKind.Anonymous"/> when none).
    /// </summary>
    internal static IdentityKind DispatchIdentityKind => DispatchIdentity.Value;

    /// <summary>
    /// Resolves the context a dispatch runs with.
    /// </summary>
    /// <remarks>
    /// <list type="number">
    /// <item><description>An explicit context is snapshotted (a non-<see cref="RequestContext"/>
    /// implementation is copied into an immutable <see cref="RequestContext"/>), the snapshot is
    /// checked against the ambient identity (see below) and, when accepted, the snapshot is what the
    /// dispatch runs with.</description></item>
    /// <item><description>With no ambient context, a fresh anonymous one is created (correlation id
    /// from <see cref="System.Diagnostics.Activity.Current"/>).</description></item>
    /// <item><description>An ambient context seen while no dispatch is in flight belongs to an entry
    /// point and is used as-is, idempotency key included.</description></item>
    /// <item><description>An ambient context seen while another dispatch is in flight is inherited
    /// from that outer dispatch: the nested dispatch gets a derived context with the same
    /// correlation id, identity, tenant and metadata, its own timestamp, and <b>no</b> idempotency
    /// key, so it never collides with the outer request in the idempotency stores.</description></item>
    /// </list>
    /// <para>
    /// <b>Explicit-context rule.</b> An explicit context whose identity is authenticated and differs
    /// (kind, user id, roles or permissions) from the ambient identity logs Warning 165 with both
    /// kinds (never ids). When
    /// the ambient identity is a <see cref="IdentityKind.User"/>, the dispatch is refused with
    /// <see cref="RequestIdentityErrorCodes.ScopeConflict"/>: a dispatch cannot run a user's request
    /// under someone else's identity. An explicit anonymous context, or one with the ambient
    /// identity, is accepted silently.
    /// </para>
    /// </remarks>
    public static Either<EncinaError, IRequestContext> Resolve(
        IRequestContextAccessor accessor,
        IRequestContext? explicitContext,
        TimeProvider timeProvider,
        ILogger logger)
    {
        var ambient = accessor.RequestContext;
        if (explicitContext is not null)
        {
            return CheckExplicitContext(explicitContext, ambient, logger);
        }

        if (ambient is null)
        {
            return Right<EncinaError, IRequestContext>(
                RequestContext.CreateAnonymousAt(timeProvider.GetUtcNow(), RequestContext.NewCorrelationId()));
        }

        return Right<EncinaError, IRequestContext>(DispatchInFlight.Value
            ? RequestContext.ForNestedDispatch(ambient, timeProvider.GetUtcNow())
            : ambient);
    }

    private static Either<EncinaError, IRequestContext> CheckExplicitContext(
        IRequestContext explicitContext,
        IRequestContext? ambient,
        ILogger logger)
    {
        // Check and dispatch the same immutable snapshot: a foreign implementation could return one
        // identity to the check and another to the handlers.
        var snapshot = RequestContext.CopyOf(explicitContext);
        var requested = snapshot.Identity;
        var current = IdentityOf(ambient);
        if (!IsIdentityChange(requested, current))
        {
            return Right<EncinaError, IRequestContext>(snapshot);
        }

        return current.Kind == IdentityKind.User
            ? Refuse(requested, current, logger)
            : Accept(snapshot, requested, current, logger);
    }

    /// <summary>
    /// Applies the explicit-context rule to a direct set of the ambient context: replacing an ambient
    /// <see cref="IdentityKind.User"/> with a different authenticated identity logs Warning 165 and
    /// throws; any other change of authenticated identity logs Warning 165 and is allowed.
    /// </summary>
    /// <exception cref="InvalidOperationException">The set would replace an ambient user with a different authenticated identity.</exception>
    internal static void EnsureReplaceable(IRequestContext? ambient, IRequestContext? replacement, ILogger logger)
    {
        var requested = IdentityOf(replacement);
        var current = IdentityOf(ambient);
        if (!IsIdentityChange(requested, current))
        {
            return;
        }

        if (current.Kind == IdentityKind.User)
        {
            RequestIdentityLog.ExplicitContextIdentityConflict(logger, requested.Kind, current.Kind, "refused");
            throw new InvalidOperationException(
                $"The ambient request context carries an authenticated user; it cannot be replaced by a context with a different {requested.Kind} identity. End the user's scope before running as another identity.");
        }

        RequestIdentityLog.ExplicitContextIdentityConflict(logger, requested.Kind, current.Kind, "accepted");
    }

    // An anonymous context never changes the identity; an authenticated one does unless it is the same caller.
    private static bool IsIdentityChange(RequestIdentity requested, RequestIdentity current) =>
        requested.IsAuthenticated && !requested.IsSameAs(current);

    private static Either<EncinaError, IRequestContext> Refuse(RequestIdentity requested, RequestIdentity current, ILogger logger)
    {
        RequestIdentityLog.ExplicitContextIdentityConflict(logger, requested.Kind, current.Kind, "refused");
        return Left<EncinaError, IRequestContext>(RequestIdentityErrors.ScopeConflict(current.Kind, requested.Kind));
    }

    private static Either<EncinaError, IRequestContext> Accept(
        IRequestContext explicitContext,
        RequestIdentity requested,
        RequestIdentity current,
        ILogger logger)
    {
        RequestIdentityLog.ExplicitContextIdentityConflict(logger, requested.Kind, current.Kind, "accepted");
        return Right<EncinaError, IRequestContext>(explicitContext);
    }

    // A missing context, or a non-conforming one whose Identity is null, reads as anonymous.
    private static RequestIdentity IdentityOf(IRequestContext? context) =>
        context?.Identity ?? RequestIdentity.Anonymous;

    /// <summary>
    /// Makes <paramref name="context"/> the ambient context and marks a dispatch as in flight until
    /// the returned scope is disposed, which restores both previous values.
    /// </summary>
    public static Scope Enter(IRequestContextAccessor accessor, IRequestContext context) =>
        Enter(accessor, context, holder: null);

    /// <summary>
    /// <see cref="Enter(IRequestContextAccessor, IRequestContext)"/>, reinstalling
    /// <paramref name="holder"/> (a holder an earlier step of the same dispatch installed) instead of
    /// creating a new one, so a stream step never revives a context whose scope ended between steps.
    /// </summary>
    internal static Scope Enter(IRequestContextAccessor accessor, IRequestContext context, RequestContextAccessor.ContextHolder? holder)
    {
        var swap = AmbientSwap.Apply(accessor, context, holder);

        // Nested dispatches find the flag already set; only the outermost one sets and clears it.
        var enterDispatch = !DispatchInFlight.Value;
        if (enterDispatch)
        {
            DispatchInFlight.Value = true;
        }

        var previousKind = DispatchIdentity.Value;
        DispatchIdentity.Value = IdentityOf(context).Kind;

        return new Scope(swap, enterDispatch, previousKind);
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
        RequestContextAccessor.ContextHolder? holder;
        using (var first = Enter(accessor, context))
        {
            // Every later step reinstalls this holder: if its scope ends, the stream reads no context.
            holder = first.Holder;
            enumerator = source.GetAsyncEnumerator(cancellationToken);
        }

        try
        {
            while (true)
            {
                bool moved;
                using (Enter(accessor, context, holder))
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
            using (Enter(accessor, context, holder))
            {
                await enumerator.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Restores the previous ambient context, dispatch flag and dispatch identity kind on disposal.
    /// </summary>
    internal readonly struct Scope : IDisposable
    {
        private readonly AmbientSwap _swap;
        private readonly bool _leaveDispatch;
        private readonly IdentityKind _previousKind;

        internal Scope(AmbientSwap swap, bool leaveDispatch, IdentityKind previousKind)
        {
            _swap = swap;
            _leaveDispatch = leaveDispatch;
            _previousKind = previousKind;
        }

        /// <summary>
        /// Gets the holder current during this scope (default accessor only), or <see langword="null"/>.
        /// </summary>
        internal RequestContextAccessor.ContextHolder? Holder => _swap.Holder;

        /// <inheritdoc />
        public void Dispose()
        {
            _swap.Restore();

            if (_leaveDispatch)
            {
                DispatchInFlight.Value = false;
            }

            DispatchIdentity.Value = _previousKind;
        }
    }

    /// <summary>
    /// Sets the ambient context for a dispatch step and puts back exactly what was there before.
    /// </summary>
    /// <remarks>
    /// With the default <see cref="RequestContextAccessor"/> the set skips the explicit-context rule
    /// (<see cref="Resolve"/> has applied it), and the restore reinstalls the captured holder itself
    /// rather than setting its value again: a holder whose scope ended meanwhile stays ended, so the
    /// end of a dispatch never revives an identity. Any other accessor gets plain sets.
    /// </remarks>
    internal readonly struct AmbientSwap
    {
        private readonly IRequestContextAccessor? _accessor;
        private readonly IRequestContext? _previousContext;
        private readonly RequestContextAccessor.ContextHolder? _previousHolder;

        private AmbientSwap(
            IRequestContextAccessor? accessor,
            IRequestContext? previousContext,
            RequestContextAccessor.ContextHolder? previousHolder,
            RequestContextAccessor.ContextHolder? holder)
        {
            _accessor = accessor;
            _previousContext = previousContext;
            _previousHolder = previousHolder;
            Holder = holder;
        }

        /// <summary>
        /// Gets the holder current after the swap (default accessor only), or <see langword="null"/>.
        /// </summary>
        internal RequestContextAccessor.ContextHolder? Holder { get; }

        /// <summary>
        /// Makes <paramref name="context"/> ambient, reinstalling <paramref name="reuse"/> when given.
        /// </summary>
        internal static AmbientSwap Apply(IRequestContextAccessor accessor, IRequestContext context, RequestContextAccessor.ContextHolder? reuse)
        {
            var previous = accessor.RequestContext;
            if (accessor is not RequestContextAccessor)
            {
                return ApplyPlain(accessor, context, previous);
            }

            var previousHolder = RequestContextAccessor.Current;

            // Already ambient (the usual case behind EncinaContextMiddleware): nothing to set.
            if (ReferenceEquals(previous, context))
            {
                return new AmbientSwap(null, null, null, previousHolder);
            }

            if (reuse is null)
            {
                return new AmbientSwap(accessor, previous, previousHolder, RequestContextAccessor.SetUnchecked(context));
            }

            RequestContextAccessor.Install(reuse);
            return new AmbientSwap(accessor, previous, previousHolder, reuse);
        }

        /// <summary>Puts back what <see cref="Apply"/> replaced.</summary>
        internal void Restore()
        {
            if (_accessor is RequestContextAccessor)
            {
                RequestContextAccessor.Install(_previousHolder);
            }
            else if (_accessor is not null)
            {
                _accessor.RequestContext = _previousContext;
            }
        }

        private static AmbientSwap ApplyPlain(IRequestContextAccessor accessor, IRequestContext context, IRequestContext? previous)
        {
            if (ReferenceEquals(previous, context))
            {
                return default;
            }

            accessor.RequestContext = context;
            return new AmbientSwap(accessor, previous, null, null);
        }
    }
}
