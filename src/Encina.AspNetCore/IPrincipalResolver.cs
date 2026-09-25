using System.Security.Claims;

namespace Encina.AspNetCore;

/// <summary>
/// Resolves the current caller's <see cref="ClaimsPrincipal"/> in a transport-agnostic way.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="AuthorizationPipelineBehavior{TRequest, TResponse}"/> asks this abstraction for the caller's
/// principal instead of reading <see cref="Microsoft.AspNetCore.Http.IHttpContextAccessor"/> directly.
/// This lets transports without an ambient <c>HttpContext</c> — most notably a Blazor Server circuit,
/// where <c>HttpContext</c> is <see langword="null"/> for the whole lifetime of the circuit after the
/// initial negotiate request — supply a principal through their own resolver.
/// </para>
/// <para>
/// The default implementation, <see cref="HttpContextPrincipalResolver"/>, is registered by
/// <see cref="ServiceCollectionExtensions.AddEncinaAspNetCore(Microsoft.Extensions.DependencyInjection.IServiceCollection)"/>
/// and preserves today's HTTP-only behavior. The <c>Encina.AspNetCore.Blazor</c> package provides
/// <c>AuthenticationStatePrincipalResolver</c>, which falls back to
/// <c>Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider</c> when there is no
/// <c>HttpContext</c>.
/// </para>
/// </remarks>
public interface IPrincipalResolver
{
    /// <summary>
    /// Asynchronously resolves the current caller's principal.
    /// </summary>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>
    /// The current caller's <see cref="ClaimsPrincipal"/>, or <see langword="null"/> when no principal
    /// can be resolved for the current call.
    /// </returns>
    ValueTask<ClaimsPrincipal?> ResolvePrincipalAsync(CancellationToken cancellationToken);
}
