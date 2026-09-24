using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Encina.AspNetCore;

/// <summary>
/// Default <see cref="IPrincipalResolver"/> that reads the current caller's principal from
/// <see cref="IHttpContextAccessor.HttpContext"/>.
/// </summary>
/// <remarks>
/// Registered with <c>TryAdd</c> semantics by
/// <see cref="ServiceCollectionExtensions.AddEncinaAspNetCore(Microsoft.Extensions.DependencyInjection.IServiceCollection)"/>.
/// Returns <see langword="null"/> when there is no ambient <see cref="HttpContext"/>, such as inside a
/// Blazor Server circuit after the initial negotiate request. Applications hosting Blazor Server should
/// reference <c>Encina.AspNetCore.Blazor</c> and call its <c>AddEncinaBlazorAuthorization()</c> to replace
/// this resolver with one that falls back to <c>AuthenticationStateProvider</c>.
/// </remarks>
public sealed class HttpContextPrincipalResolver : IPrincipalResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpContextPrincipalResolver"/> class.
    /// </summary>
    /// <param name="httpContextAccessor">Accessor used to obtain the current HTTP context.</param>
    public HttpContextPrincipalResolver(IHttpContextAccessor httpContextAccessor)
    {
        ArgumentNullException.ThrowIfNull(httpContextAccessor);

        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public ValueTask<ClaimsPrincipal?> ResolvePrincipalAsync(CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(_httpContextAccessor.HttpContext?.User);
    }
}
