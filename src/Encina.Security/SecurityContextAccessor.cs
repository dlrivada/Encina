namespace Encina.Security;

/// <summary>
/// Default implementation of <see cref="ISecurityContextAccessor"/> using <see cref="AsyncLocal{T}"/>.
/// </summary>
/// <remarks>
/// <para>
/// This implementation uses <see cref="AsyncLocal{T}"/> to store the security context, ensuring
/// it flows correctly across async operations within the same logical call context.
/// </para>
/// <para>
/// The context is scoped to the current async flow, meaning:
/// <list type="bullet">
/// <item><description>Each request gets its own isolated security context</description></item>
/// <item><description>Context flows through await points</description></item>
/// <item><description>Context doesn't leak between requests</description></item>
/// </list>
/// </para>
/// </remarks>
internal sealed class SecurityContextAccessor : ISecurityContextAccessor
{
    private static readonly AsyncLocal<ISecurityContext?> _currentContext = new();

    /// <inheritdoc />
    public ISecurityContext? SecurityContext
    {
        get => _currentContext.Value;
        set => _currentContext.Value = value;
    }
}
