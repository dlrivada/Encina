using Encina.AspNetCore;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.AspNetCore.Blazor;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> to register Encina's Blazor Server
/// authorization integration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Replaces the default, HTTP-only <see cref="IPrincipalResolver"/> with one that also resolves the
    /// caller's principal from <see cref="AuthenticationStateProvider"/> when there is no ambient
    /// <see cref="HttpContext"/>, as happens inside a Blazor Server interactive circuit.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Call this after registering Blazor Server (<c>AddServerSideBlazor</c> /
    /// <c>AddRazorComponents().AddInteractiveServerComponents()</c>), which registers
    /// <see cref="AuthenticationStateProvider"/>. This method also ensures
    /// <see cref="IHttpContextAccessor"/> is registered, so requests served over classic HTTP still
    /// resolve the principal from <see cref="HttpContext.User"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.Services.AddEncina(cfg => cfg.AddAuthorization(), typeof(Program).Assembly);
    /// builder.Services.AddEncinaAspNetCore();
    /// builder.Services.AddServerSideBlazor();
    /// builder.Services.AddEncinaBlazorAuthorization();
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaBlazorAuthorization(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpContextAccessor();
        services.Replace(ServiceDescriptor.Scoped<IPrincipalResolver, AuthenticationStatePrincipalResolver>());

        return services;
    }
}
