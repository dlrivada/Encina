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
    /// <b>Explicit-context rule</b>, applied to the snapshot. "The chain has a user" means the facts
    /// of the current holder chain (a user identity anywhere in it, ended holders included), not
    /// only the readable ambient identity. The first matching identity rule decides:
    /// </para>
    /// <list type="number">
    /// <item><description>The explicit identity is not authenticated: accepted.</description></item>
    /// <item><description>Its issuing scope has ended: refused (stale replay), even when it is the
    /// ambient identity.</description></item>
    /// <item><description>The chain has a user and the explicit identity is not the readable ambient
    /// identity: refused.</description></item>
    /// <item><description>It has no issuer (built outside a scope): accepted only inside an active
    /// scope of the same identity, otherwise refused.</description></item>
    /// <item><description>It differs from the ambient identity (and the chain has no user):
    /// accepted, Warning 165.</description></item>
    /// <item><description>Otherwise (the same identity): accepted silently.</description></item>
    /// </list>
    /// <para>
    /// Refusals return <see cref="RequestIdentityErrorCodes.ScopeConflict"/> and log Warning 165
    /// with both kinds (never ids). Then, on every accepted context, when the chain has a user and
    /// the explicit tenant differs from the ambient tenant, the dispatch is refused with
    /// <see cref="RequestIdentityErrorCodes.TenantConflict"/> (Warning 165).
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
            return CheckExplicitContext(explicitContext, ambient, ChainHasUser(accessor, ambient), logger);
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
        bool chainHasUser,
        ILogger logger)
    {
        // Check and dispatch the same immutable snapshot: a foreign implementation could return one
        // identity to the check and another to the handlers.
        var snapshot = RequestContext.CopyOf(explicitContext);
        var requested = snapshot.Identity;
        var current = IdentityOf(ambient);

        var verdict = IdentityVerdict(requested, current, chainHasUser);
        if (verdict == ExplicitIdentityVerdict.Refused)
        {
            RequestIdentityLog.ExplicitContextIdentityConflict(logger, requested.Kind, current.Kind, "refused");
            return Left<EncinaError, IRequestContext>(RequestIdentityErrors.ScopeConflict(current.Kind, requested.Kind));
        }

        if (chainHasUser && !string.Equals(snapshot.TenantId, ambient?.TenantId, StringComparison.Ordinal))
        {
            RequestIdentityLog.ExplicitContextIdentityConflict(logger, requested.Kind, current.Kind, "refused (tenant)");
            return Left<EncinaError, IRequestContext>(RequestIdentityErrors.TenantConflict(current.Kind, requested.Kind));
        }

        if (verdict == ExplicitIdentityVerdict.AcceptedChange)
        {
            RequestIdentityLog.ExplicitContextIdentityConflict(logger, requested.Kind, current.Kind, "accepted");
        }

        return Right<EncinaError, IRequestContext>(snapshot);
    }

    // The identity rules of Resolve, first match wins (see the remarks of Resolve).
    private static ExplicitIdentityVerdict IdentityVerdict(RequestIdentity requested, RequestIdentity current, bool chainHasUser)
    {
        if (!requested.IsAuthenticated)
        {
            return ExplicitIdentityVerdict.Accepted;
        }

        var same = requested.IsSameAs(current);
        if (requested.Issuer is { IsLive: false } || (chainHasUser && !same) || (requested.Issuer is null && !IsActiveScopeOf(current, same)))
        {
            return ExplicitIdentityVerdict.Refused;
        }

        return same ? ExplicitIdentityVerdict.Accepted : ExplicitIdentityVerdict.AcceptedChange;
    }

    // An issuer-less identity is accepted only inside an active scope of the same identity.
    private static bool IsActiveScopeOf(RequestIdentity current, bool same) =>
        same && current.IsAuthenticated && current.Issuer is { IsLive: true };

    // With the default accessor, the facts of the holder chain (ended holders included); with any
    // other accessor (unit tests that substitute it), the readable ambient identity.
    private static bool ChainHasUser(IRequestContextAccessor accessor, IRequestContext? ambient) =>
        accessor is RequestContextAccessor
            ? RequestContextAccessor.CurrentFacts.HasFlag(ChainFacts.User)
            : IdentityOf(ambient).Kind == IdentityKind.User;

    /// <summary>
    /// Applies the setter rule of <see cref="RequestContextAccessor.RequestContext"/> and returns the
    /// snapshot to store.
    /// </summary>
    /// <param name="current">The readable ambient context, or <see langword="null"/> when none is readable.</param>
    /// <param name="value">The value being set.</param>
    /// <param name="logger">The logger for Warning 165.</param>
    /// <returns>The immutable snapshot of <paramref name="value"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="value"/> is <see langword="null"/>, or its identity or origin differs from the
    /// readable context (anonymous with no origin when none is readable), or it changes the tenant
    /// while a dispatch is in flight.
    /// </exception>
    internal static RequestContext EnsureSettable(IRequestContext? current, IRequestContext? value, ILogger logger)
    {
        var reference = IdentityOf(current);
        var snapshot = value is null ? null : RequestContext.CopyOf(value);
        if (snapshot is not null
            && snapshot.Identity.IsSameAs(reference)
            && snapshot.Origin == OriginOf(current)
            && !IsTenantChangeInDispatch(current, snapshot))
        {
            return snapshot;
        }

        var requestedKind = snapshot?.Identity.Kind ?? IdentityKind.Anonymous;
        RequestIdentityLog.ExplicitContextIdentityConflict(logger, requestedKind, reference.Kind, "refused");
        throw new InvalidOperationException(
            "The ambient request context can only be replaced by a context with the same identity and origin (and, during a dispatch, the same tenant); it is never cleared. Bind identities through IRequestContextScopeFactory.");
    }

    private static RequestOrigin OriginOf(IRequestContext? context) =>
        (context as RequestContext)?.Origin ?? RequestOrigin.Unspecified;

    private static bool IsTenantChangeInDispatch(IRequestContext? current, RequestContext snapshot) =>
        DispatchInFlight.Value && !string.Equals(current?.TenantId, snapshot.TenantId, StringComparison.Ordinal);

    // A missing context, or a non-conforming one whose Identity is null, reads as anonymous.
    private static RequestIdentity IdentityOf(IRequestContext? context) =>
        context?.Identity ?? RequestIdentity.Anonymous;

    private enum ExplicitIdentityVerdict
    {
        Accepted,
        AcceptedChange,
        Refused
    }

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
