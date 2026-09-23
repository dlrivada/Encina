namespace Encina;

/// <summary>
/// Default <see cref="IRequestContextAccessor"/> backed by an <see cref="AsyncLocal{T}"/>.
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
/// <c>AddEncina</c> registers it as a singleton. Hosts that build <see cref="Encina"/> by hand get an
/// instance by default.
/// </para>
/// </remarks>
public sealed class RequestContextAccessor : IRequestContextAccessor
{
    private static readonly AsyncLocal<IRequestContext?> CurrentContext = new();

    /// <inheritdoc />
    public IRequestContext? RequestContext
    {
        get => CurrentContext.Value;
        set => CurrentContext.Value = value;
    }
}
