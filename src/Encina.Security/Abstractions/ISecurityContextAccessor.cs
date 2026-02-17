namespace Encina.Security;

/// <summary>
/// Provides access to the current <see cref="ISecurityContext"/> for the executing request.
/// </summary>
/// <remarks>
/// <para>
/// This accessor allows middleware and pipeline behaviors to set the security context
/// for the current request scope. The context is then available to all Encina handlers
/// and evaluators processing requests within that scope.
/// </para>
/// <para>
/// Implementation uses <see cref="AsyncLocal{T}"/> to ensure context flows
/// correctly across async operations within the same request scope, similar to
/// how <c>IRequestContextAccessor</c> works in <c>Encina.AspNetCore</c>.
/// </para>
/// </remarks>
public interface ISecurityContextAccessor
{
    /// <summary>
    /// Gets or sets the current security context for this request.
    /// </summary>
    /// <remarks>
    /// Set by middleware or pipeline infrastructure at the start of request processing.
    /// <c>null</c> when no security context has been established.
    /// </remarks>
    ISecurityContext? SecurityContext { get; set; }
}
