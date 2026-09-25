using System.Security.Claims;
using Encina.AspNetCore;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;

namespace Encina.AspNetCore.Blazor;

/// <summary>
/// <see cref="IPrincipalResolver"/> for Blazor Server applications. Uses the ambient
/// <see cref="HttpContext"/> when one is available, and falls back to
/// <see cref="AuthenticationStateProvider"/> otherwise.
/// </summary>
/// <remarks>
/// <para>
/// A Blazor Server interactive circuit has no <see cref="HttpContext"/> for the whole lifetime of the
/// circuit after the initial negotiate request — this is standard, documented ASP.NET Core Blazor Server
/// behavior. The caller's identity in that scenario is obtained from
/// <see cref="AuthenticationStateProvider"/> (typically surfaced to components through
/// <c>CascadingAuthenticationState</c>). This resolver prefers <see cref="HttpContext.User"/> when a
/// request is served over classic HTTP (e.g. a Blazor Server prerender, an API controller, or a
/// non-Blazor page in the same application) and only falls back to
/// <see cref="AuthenticationStateProvider"/> when there is no <see cref="HttpContext"/>, matching how
/// ASP.NET Core itself resolves the current user across those two transports.
/// </para>
/// <para>
/// Register this resolver with
/// <see cref="ServiceCollectionExtensions.AddEncinaBlazorAuthorization(Microsoft.Extensions.DependencyInjection.IServiceCollection)"/>,
/// which replaces the default <see cref="HttpContextPrincipalResolver"/> registered by
/// <c>Encina.AspNetCore</c>.
/// </para>
/// </remarks>
public sealed class AuthenticationStatePrincipalResolver : IPrincipalResolver
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationStatePrincipalResolver"/> class.
    /// </summary>
    /// <param name="authenticationStateProvider">Provides the authentication state of the current Blazor circuit.</param>
    /// <param name="httpContextAccessor">
    /// Optional accessor used to prefer the ambient <see cref="HttpContext"/> when one is available.
    /// </param>
    public AuthenticationStatePrincipalResolver(
        AuthenticationStateProvider authenticationStateProvider,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        ArgumentNullException.ThrowIfNull(authenticationStateProvider);

        _authenticationStateProvider = authenticationStateProvider;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public async ValueTask<ClaimsPrincipal?> ResolvePrincipalAsync(CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor?.HttpContext;
        if (httpContext is not null)
        {
            return httpContext.User;
        }

        var authenticationState = await _authenticationStateProvider
            .GetAuthenticationStateAsync()
            .ConfigureAwait(false);

        return authenticationState.User;
    }
}
