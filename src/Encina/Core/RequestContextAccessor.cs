using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina;

/// <summary>
/// Default <see cref="IRequestContextAccessor"/> backed by an <see cref="AsyncLocal{T}"/> of
/// invalidatable holders.
/// </summary>
/// <remarks>
/// <para>
/// The value lives in a single static <see cref="AsyncLocal{T}"/>, so every instance observes the same
/// ambient context for the current logical call. The context:
/// <list type="bullet">
/// <item><description>flows through <c>await</c> points and into child tasks;</description></item>
/// <item><description>is isolated between concurrent logical calls (each HTTP request, each background job);</description></item>
/// <item><description>never flows back to a caller from an <c>async</c> method that changed it.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Holders.</b> The <see cref="AsyncLocal{T}"/> holds a holder, not the context itself (the
/// pattern of ASP.NET Core's <c>HttpContextAccessor</c>). Each holder links to the holder it
/// replaced. When an identity entry point ends its scope (the request middleware, an identity
/// scope), it invalidates its holder: every flow that captured it (a <c>Task.Run</c>, a
/// fire-and-forget call, a timer) then reads no context, which is the anonymous identity, which
/// every gate denies. Invalidating a holder also invalidates every holder pushed over it, so
/// disposing scopes out of order never resurrects an ended identity.
/// </para>
/// <para>
/// A flow whose current holder has ended (its scope ended, or an outer scope ended out of order)
/// reads no context, and the next <see cref="Push"/> or set starts a fresh readable chain. The new
/// holder still <b>inherits the facts</b> of the holder it replaced (whether a user identity or an
/// inbound request was ever in the chain, ended holders included), so a flow forked from an ended
/// request can never open a service scope by clearing or re-setting its context.
/// </para>
/// <para>
/// An identity issued by an identity scope reads as no context through the accessor once that scope
/// has ended; a copy of the context kept elsewhere reports <see cref="RequestIdentity.Anonymous"/>
/// as its identity (see <see cref="global::Encina.RequestContext.Identity"/>).
/// </para>
/// <para>
/// The public setter is host infrastructure; application code reads the context and never sets it.
/// It only preserves the identity it finds (see <see cref="RequestContext"/>); identities are bound
/// through <see cref="IRequestContextScopeFactory"/>.
/// </para>
/// <para>
/// <c>AddEncina</c> registers it as a singleton. Hosts that build <see cref="Encina"/> by hand get an
/// instance by default.
/// </para>
/// </remarks>
public sealed class RequestContextAccessor : IRequestContextAccessor
{
    private static readonly AsyncLocal<ContextHolder?> CurrentHolder = new();

    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestContextAccessor"/> class.
    /// </summary>
    /// <param name="logger">The logger for refused sets (Warning 165); none by default.</param>
    public RequestContextAccessor(ILogger<RequestContextAccessor>? logger = null)
    {
        _logger = logger ?? (ILogger)NullLogger.Instance;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// The setter is <b>identity- and origin-preserving</b> and never clears. A set is accepted only
    /// when <paramref name="value"/> carries the same identity (<c>IsSameAs</c>) and the same origin
    /// as the readable context; with no readable context (none set, or an ended chain) the reference
    /// is the anonymous identity with no origin, which is what
    /// <see cref="global::Encina.RequestContext.CreateAnonymousAt"/> builds. A tenant change is accepted only
    /// while no dispatch is in flight in this flow (entry-point middleware), so a handler cannot
    /// retarget its own dispatch. Any other set, and <see langword="null"/>, logs Warning 165 (kinds
    /// only) and throws <see cref="InvalidOperationException"/>.
    /// </para>
    /// <para>
    /// The value is checked and stored as an immutable snapshot (a foreign implementation is copied
    /// into a <see cref="global::Encina.RequestContext"/>), so the context readers see is the one
    /// that was checked. It stays bound to the innermost live scope holder: when that scope ends, the
    /// value is no longer readable. Only scope holders are chained, so repeated sets in a long-lived
    /// flow never grow the chain.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">The set would clear the context or change its identity, origin, or (during a dispatch) tenant.</exception>
    public IRequestContext? RequestContext
    {
        get => CurrentHolder.Value?.ReadContext();
        set => SetUnchecked(AmbientRequestContext.EnsureSettable(CurrentHolder.Value?.ReadContext(), value, _logger));
    }

    /// <summary>
    /// Sets the ambient value without the setter rule. Internal: used by the dispatcher, which has
    /// already applied the explicit-context rule (or is restoring the value it replaced).
    /// </summary>
    /// <returns>The holder now current in this flow.</returns>
    internal static ContextHolder SetUnchecked(IRequestContext? value)
    {
        // The facts are read from the holder current now, before LiveOrNull or NearestScope drop it.
        var current = CurrentHolder.Value;
        var holder = new ContextHolder(value, NearestScope(LiveOrNull(current)), isScope: false, FactsOf(current));
        CurrentHolder.Value = holder;
        return holder;
    }

    /// <summary>
    /// Gets the holder current in this flow, which the dispatcher captures to restore it exactly.
    /// </summary>
    internal static ContextHolder? Current => CurrentHolder.Value;

    /// <summary>
    /// Gets the facts of the current flow's chain: whether a user identity or an inbound request was
    /// ever in it, ended holders included.
    /// </summary>
    internal static ChainFacts CurrentFacts => FactsOf(CurrentHolder.Value);

    /// <summary>
    /// Makes <paramref name="holder"/> current in this flow as it is: an ended holder stays ended, so
    /// restoring a captured holder never revives an identity whose scope has ended.
    /// </summary>
    internal static void Install(ContextHolder? holder) => CurrentHolder.Value = holder;

    /// <summary>
    /// Makes <paramref name="context"/> ambient in a new scope holder and returns that holder, which
    /// the scope factory ends with <see cref="End"/>. Called only inside the factory's <c>async</c>
    /// body, so the write never reaches the caller's flow.
    /// </summary>
    internal static ContextHolder Push(IRequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var current = CurrentHolder.Value;
        var holder = new ContextHolder(context, LiveOrNull(current), isScope: true, FactsOf(current));
        CurrentHolder.Value = holder;
        return holder;
    }

    /// <summary>
    /// Ends <paramref name="holder"/>: invalidates it, and so every holder pushed over it. Nothing is
    /// restored; the caller's holder comes back through the <c>async</c> frame.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when the holder was still valid; <see langword="false"/> when an
    /// enclosing scope had already ended (the scope outlived its parent).
    /// </returns>
    internal static bool End(ContextHolder holder)
    {
        ArgumentNullException.ThrowIfNull(holder);

        var wasValid = holder.IsValid();
        holder.Invalidate();
        return wasValid;
    }

    private static ChainFacts FactsOf(ContextHolder? holder) => holder?.Facts ?? ChainFacts.None;

    // An ended holder (or one bound to an ended scope) starts no chain: the new value begins a fresh one.
    private static ContextHolder? LiveOrNull(ContextHolder? holder) =>
        holder is not null && holder.IsValid() ? holder : null;

    // A non-scope holder's parent is always a scope holder (or null), so this is the innermost scope.
    private static ContextHolder? NearestScope(ContextHolder? holder) =>
        holder is null || holder.IsScope ? holder : holder.Parent;

    /// <summary>
    /// One ambient value. Readable only while neither it nor any holder it was pushed over has ended,
    /// and while the scope that issued its identity (if any) is still active.
    /// </summary>
    internal sealed class ContextHolder
    {
        // Volatile: a holder is invalidated by one flow and read by every flow that captured it.
        private volatile IRequestContext? _context;
        private volatile bool _disposed;

        internal ContextHolder(IRequestContext? context, ContextHolder? parent, bool isScope, ChainFacts inheritedFacts = ChainFacts.None)
        {
            _context = context;
            Parent = parent;
            IsScope = isScope;
            // The issued identity: a scope pushes its context before binding the issuer, when the
            // checked Identity would still read as anonymous and the chain would lose its user fact.
            Kind = global::Encina.RequestContext.IssuedIdentityOf(context)?.Kind ?? IdentityKind.Anonymous;
            Origin = (context as RequestContext)?.Origin ?? RequestOrigin.Unspecified;
            Facts = inheritedFacts | OwnFacts(Kind, Origin);
        }

        /// <summary>
        /// Gets the holder this one is bound to: for a scope holder, the live holder it was pushed
        /// over; for a set value, the innermost scope holder.
        /// </summary>
        internal ContextHolder? Parent { get; }

        /// <summary>Gets a value indicating whether this holder was opened by <see cref="Push"/>.</summary>
        internal bool IsScope { get; }

        /// <summary>Gets the identity kind of the held context, kept after the holder ends.</summary>
        internal IdentityKind Kind { get; }

        /// <summary>Gets the origin of the held context, kept after the holder ends.</summary>
        internal RequestOrigin Origin { get; }

        /// <summary>
        /// Gets this holder's own facts plus every fact of the holder that was current when it was
        /// created, ended or not. Immutable: ending the holder never drops a fact.
        /// </summary>
        internal ChainFacts Facts { get; }

        /// <summary>Gets a value indicating whether this holder has ended.</summary>
        internal bool IsDisposed => _disposed;

        /// <summary>
        /// Returns the context, or <see langword="null"/> when this holder or an ancestor has ended, or
        /// when the scope that issued the held identity has ended.
        /// </summary>
        internal IRequestContext? ReadContext()
        {
            var context = IsValid() ? _context : null;
            var identity = global::Encina.RequestContext.IssuedIdentityOf(context);
            return identity is null || global::Encina.RequestContext.IsReadable(identity) ? context : null;
        }

        /// <summary>
        /// Gets the identity kind of the nearest ended ancestor (this holder's own kind when none).
        /// </summary>
        internal IdentityKind EndedAncestorKind()
        {
            for (var holder = Parent; holder is not null; holder = holder.Parent)
            {
                if (holder.IsDisposed)
                {
                    return holder.Kind;
                }
            }

            return Kind;
        }

        /// <summary>The facts a holder of <paramref name="kind"/> and <paramref name="origin"/> adds to its chain.</summary>
        internal static ChainFacts OwnFacts(IdentityKind kind, RequestOrigin origin) =>
            (kind == IdentityKind.User ? ChainFacts.User : ChainFacts.None)
            | (origin == RequestOrigin.Inbound ? ChainFacts.Inbound : ChainFacts.None)
            | (origin == RequestOrigin.Connection ? ChainFacts.Connection : ChainFacts.None);

        /// <summary>Ends this holder.</summary>
        internal void Invalidate()
        {
            _disposed = true;
            _context = null;
        }

        /// <summary>Determines whether neither this holder nor any holder it is bound to has ended.</summary>
        internal bool IsValid()
        {
            for (var holder = this; holder is not null; holder = holder.Parent)
            {
                if (holder.IsDisposed)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
