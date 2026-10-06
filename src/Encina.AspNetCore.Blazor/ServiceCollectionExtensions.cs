using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.AspNetCore.Blazor;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> to register Encina's Blazor Server
/// request identity integration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Binds the request identity of Blazor Server circuits: every circuit activity (a UI event, a
    /// JavaScript interop call) runs inside one inbound identity scope built from the circuit's current
    /// <see cref="AuthenticationStateProvider"/> state, so <c>[Authorize]</c>, ABAC and audit see the
    /// signed-in user inside interactive components.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Call this after registering Blazor Server (<c>AddServerSideBlazor</c> /
    /// <c>AddRazorComponents().AddInteractiveServerComponents()</c>), which registers
    /// <see cref="AuthenticationStateProvider"/>. It also registers the request identity model
    /// (<c>AddEncinaRequestIdentity()</c>).
    /// </para>
    /// <para>
    /// The circuit's connection (<c>/_blazor</c>) carries no request identity: <c>UseEncinaContext()</c>
    /// runs it under the anonymous connection marker. A changed authentication state applies at the
    /// next activity; code that runs outside an activity (a continuation that outlives it, a timer)
    /// reads the anonymous identity.
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

        services.AddEncinaRequestIdentity();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<CircuitHandler, RequestIdentityCircuitHandler>());

        return services;
    }
}
