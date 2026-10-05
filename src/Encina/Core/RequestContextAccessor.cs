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
/// The public setter is host infrastructure (middleware, circuit handlers, identity scopes,
/// the dispatcher); application code reads the context and never sets it.
/// </para>
/// <para>
/// <c>AddEncina</c> registers it as a singleton. Hosts that build <see cref="Encina"/> by hand get an
/// instance by default.
/// </para>
/// </remarks>
public sealed class RequestContextAccessor : IRequestContextAccessor
{
    private static readonly AsyncLocal<ContextHolder?> CurrentHolder = new();

    /// <inheritdoc />
    /// <remarks>
    /// Setting a value replaces the current one in this flow and stays bound to the innermost
    /// scope holder (see <see cref="Push"/>): when that scope ends, the value is no longer readable.
    /// Only scope holders are chained, so repeated sets in a long-lived flow never grow the chain.
    /// </remarks>
    public IRequestContext? RequestContext
    {
        get => CurrentHolder.Value?.ReadContext();
        set => CurrentHolder.Value = new ContextHolder(value, NearestScope(CurrentHolder.Value), isScope: false);
    }

    /// <summary>
    /// Makes <paramref name="context"/> ambient in a new scope holder and returns that holder, which
    /// the caller ends with <see cref="Pop"/>.
    /// </summary>
    internal static ContextHolder Push(IRequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var holder = new ContextHolder(context, CurrentHolder.Value, isScope: true);
        CurrentHolder.Value = holder;
        return holder;
    }

    // A non-scope holder's parent is always a scope holder (or null), so this is the innermost scope.
    private static ContextHolder? NearestScope(ContextHolder? holder) =>
        holder is null || holder.IsScope ? holder : holder.Parent;

    /// <summary>
    /// Ends <paramref name="holder"/>: invalidates it (and so every holder pushed over it) and
    /// restores the holder it replaced in the current flow.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when <paramref name="holder"/> was the current holder (LIFO);
    /// <see langword="false"/> when it was ended out of order or already ended. Either way the
    /// ended identity is never readable again.
    /// </returns>
    internal static bool Pop(ContextHolder holder)
    {
        ArgumentNullException.ThrowIfNull(holder);

        var inOrder = ReferenceEquals(CurrentHolder.Value, holder) && !holder.IsDisposed;
        holder.Invalidate();
        CurrentHolder.Value = holder.Parent;
        return inOrder;
    }

    /// <summary>
    /// One ambient value. Readable only while neither it nor any holder it was pushed over has ended.
    /// </summary>
    internal sealed class ContextHolder
    {
        private IRequestContext? _context;

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
        internal bool IsDisposed { get; private set; }

        /// <summary>Returns the context, or <see langword="null"/> when this holder or an ancestor has ended.</summary>
        internal IRequestContext? ReadContext() => IsValid() ? _context : null;

        /// <summary>Ends this holder.</summary>
        internal void Invalidate()
        {
            _context = null;
            IsDisposed = true;
        }

        private bool IsValid()
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
