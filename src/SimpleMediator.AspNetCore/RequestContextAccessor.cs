namespace SimpleMediator.AspNetCore;

/// <summary>
/// Default implementation of <see cref="IRequestContextAccessor"/> using <see cref="AsyncLocal{T}"/>.
/// </summary>
/// <remarks>
/// <para>
/// This implementation uses <see cref="AsyncLocal{T}"/> to store the request context, ensuring
/// it flows correctly across async operations within the same logical call context (HTTP request).
/// </para>
/// <para>
/// The context is scoped to the current async flow, meaning:
/// <list type="bullet">
/// <item><description>Each HTTP request gets its own isolated context</description></item>
/// <item><description>Context flows through await points</description></item>
/// <item><description>Context doesn't leak between requests</description></item>
/// </list>
/// </para>
/// </remarks>
internal sealed class RequestContextAccessor : IRequestContextAccessor
{
    private static readonly AsyncLocal<IRequestContext?> _currentContext = new();

    /// <inheritdoc />
    public IRequestContext? RequestContext
    {
        get => _currentContext.Value;
        set => _currentContext.Value = value;
    }
}
