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
/// reads no context, and the next <see cref="Push"/> or set starts a fresh chain, so the flow can
/// establish a new readable context.
/// </para>
/// <para>
/// The public setter is host infrastructure (middleware, circuit handlers, identity scopes,
/// the dispatcher); application code reads the context and never sets it. It follows the
/// explicit-context rule: replacing an ambient authenticated user with a context of a different
/// authenticated identity is refused (Warning 165 and <see cref="InvalidOperationException"/>).
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
    /// <param name="logger">The logger for refused or changed identities (Warning 165); none by default.</param>
    public RequestContextAccessor(ILogger<RequestContextAccessor>? logger = null)
    {
        _logger = logger ?? (ILogger)NullLogger.Instance;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Setting a value replaces the current one in this flow and stays bound to the innermost live
    /// scope holder (see <see cref="Push"/>): when that scope ends, the value is no longer readable.
    /// Only scope holders are chained, so repeated sets in a long-lived flow never grow the chain.
    /// </para>
    /// <para>
    /// When the ambient identity is an authenticated <see cref="IdentityKind.User"/> and
    /// <paramref name="value"/> carries a different authenticated identity, the set is refused:
    /// Warning 165 is logged (kinds only) and <see cref="InvalidOperationException"/> is thrown.
    /// Any other change of authenticated identity is allowed and logs Warning 165.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">The set would replace an ambient user with a different authenticated identity.</exception>
    public IRequestContext? RequestContext
    {
        get => CurrentHolder.Value?.ReadContext();
        set
        {
            AmbientRequestContext.EnsureReplaceable(CurrentHolder.Value?.ReadContext(), value, _logger);
            SetUnchecked(value);
        }
    }

    /// <summary>
    /// Sets the ambient value without the explicit-context rule. Internal: used by the dispatcher,
    /// which has already applied the rule (or is restoring the value it replaced).
    /// </summary>
    internal static void SetUnchecked(IRequestContext? value) =>
        CurrentHolder.Value = new ContextHolder(value, NearestScope(LiveOrNull(CurrentHolder.Value)), isScope: false);

    /// <summary>
    /// Makes <paramref name="context"/> ambient in a new scope holder and returns that holder, which
    /// the caller ends with <see cref="Pop"/>.
    /// </summary>
    internal static ContextHolder Push(IRequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var holder = new ContextHolder(context, LiveOrNull(CurrentHolder.Value), isScope: true);
        CurrentHolder.Value = holder;
        return holder;
    }

    // An ended holder (or one bound to an ended scope) starts no chain: the new value begins a fresh one.
    private static ContextHolder? LiveOrNull(ContextHolder? holder) =>
        holder is not null && holder.IsValid() ? holder : null;

    // A non-scope holder's parent is always a scope holder (or null), so this is the innermost scope.
    private static ContextHolder? NearestScope(ContextHolder? holder) =>
        holder is null || holder.IsScope ? holder : holder.Parent;

    /// <summary>
    /// Ends <paramref name="holder"/>: invalidates it (and so every holder pushed over it) and, when it
    /// is the current scope of this flow, restores the holder it replaced.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when <paramref name="holder"/> was the current scope of this flow (LIFO),
    /// including when a value was set inside it; <see langword="false"/> when it was ended out of order
    /// or already ended. Either way the ended identity is never readable again.
    /// </returns>
    internal static bool Pop(ContextHolder holder)
    {
        ArgumentNullException.ThrowIfNull(holder);

        var inOrder = !holder.IsDisposed && ReferenceEquals(NearestScope(CurrentHolder.Value), holder);
        holder.Invalidate();

        // Restore only when the holder is this flow's current scope: ending it out of order must never
        // install its parent identity. The ended holder already reads null.
        if (inOrder)
        {
            CurrentHolder.Value = holder.Parent;
        }

        return inOrder;
    }

    /// <summary>
    /// One ambient value. Readable only while neither it nor any holder it was pushed over has ended.
    /// </summary>
    internal sealed class ContextHolder
    {
        // Volatile: a holder is invalidated by one flow and read by every flow that captured it.
        private volatile IRequestContext? _context;
        private volatile bool _disposed;

        internal ContextHolder(IRequestContext? context, ContextHolder? parent, bool isScope)
        {
            _context = context;
            Parent = parent;
            IsScope = isScope;
        }

        /// <summary>
        /// Gets the holder this one is bound to: for a scope holder, the holder it replaced (restored
        /// when it ends); for a set value, the innermost scope holder.
        /// </summary>
        internal ContextHolder? Parent { get; }

        /// <summary>Gets a value indicating whether this holder was opened by <see cref="Push"/>.</summary>
        internal bool IsScope { get; }

        /// <summary>Gets a value indicating whether this holder has ended.</summary>
        internal bool IsDisposed => _disposed;

        /// <summary>Returns the context, or <see langword="null"/> when this holder or an ancestor has ended.</summary>
        internal IRequestContext? ReadContext() => IsValid() ? _context : null;

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
