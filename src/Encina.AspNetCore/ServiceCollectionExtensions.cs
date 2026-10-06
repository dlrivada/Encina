using Encina.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.AspNetCore;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> to register Encina ASP.NET Core integration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina ASP.NET Core integration services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers:
    /// <list type="bullet">
    /// <item><description>the request identity model (<c>AddEncinaRequestIdentity()</c>): the
    /// <see cref="IRequestContextAccessor"/>, the <see cref="RequestIdentityOptions"/> claim map and the
    /// <see cref="IRequestContextScopeFactory"/> that <c>UseEncinaContext()</c> uses;</description></item>
    /// <item><description>the <see cref="EncinaAspNetCoreOptions"/> header names;</description></item>
    /// <item><description><see cref="Microsoft.AspNetCore.Http.IHttpContextAccessor"/>, for
    /// <see cref="HttpRegionContextProvider"/>.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// After calling this method, use <c>app.UseEncinaContext()</c> in your middleware pipeline, after
    /// <c>UseRouting()</c> and <c>UseAuthentication()</c>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var builder = WebApplication.CreateBuilder(args);
    ///
    /// // Register Encina with ASP.NET Core integration
    /// builder.Services.AddEncina(cfg => { }, typeof(Program).Assembly);
    /// builder.Services.AddEncinaAspNetCore();
    ///
    /// var app = builder.Build();
    ///
    /// app.UseRouting();
    /// app.UseAuthentication();
    /// app.UseEncinaContext();
    /// app.UseAuthorization();
    ///
    /// app.MapControllers();
    /// app.Run();
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaAspNetCore(this IServiceCollection services)
    {
        return services.AddEncinaAspNetCore(_ => { });
    }

    /// <summary>
    /// Adds Encina ASP.NET Core integration services with custom configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddEncinaAspNetCore(options =>
    /// {
    ///     options.CorrelationIdHeader = "X-Request-ID";
    ///     options.TenantIdHeader = "X-Tenant";
    /// });
    ///
    /// // Claim types are configured once for every entry point:
    /// builder.Services.AddEncinaRequestIdentity(options =>
    /// {
    ///     options.UserIdClaimTypes.Clear();
    ///     options.UserIdClaimTypes.Add("sub");
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaAspNetCore(
        this IServiceCollection services,
        Action<EncinaAspNetCoreOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.Configure(configureOptions);

        // The accessor, the claim map and the scope factory that UseEncinaContext() opens scopes with.
        services.AddEncinaRequestIdentity();

        // HttpRegionContextProvider reads the request through IHttpContextAccessor.
        services.AddHttpContextAccessor();

        return services;
    }

    /// <summary>
    /// Adds authorization pipeline behavior to Encina.
    /// </summary>
    /// <param name="configuration">The Encina configuration.</param>
    /// <returns>The configuration for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This adds a pipeline behavior that enforces <see cref="AuthorizeAttribute"/>
    /// on request types using ASP.NET Core's authorization system.
    /// </para>
    /// <para>
    /// For CQRS-aware authorization with default policies, use
    /// <see cref="AddEncinaAuthorization(IServiceCollection, Action{AuthorizationConfiguration}?, Action{AuthorizationOptions}?)"/> instead.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.Services.AddEncina(cfg =>
    /// {
    ///     cfg.AddAuthorization(); // Enable [Authorize] attribute support
    /// }, typeof(Program).Assembly);
    /// </code>
    /// </example>
    public static EncinaConfiguration AddAuthorization(this EncinaConfiguration configuration)
    {
        // Register authorization behavior
        configuration.AddPipelineBehavior(typeof(AuthorizationPipelineBehavior<,>));

        return configuration;
    }

    /// <summary>
    /// Adds Encina's CQRS-aware authorization services and pipeline behavior.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureAuthorization">
    /// Optional action to configure <see cref="AuthorizationConfiguration"/>.
    /// When <c>null</c>, secure defaults are used.
    /// </param>
    /// <param name="configurePolicies">
    /// Optional action to register ASP.NET Core authorization policies.
    /// This delegates directly to <see cref="AuthorizationOptions"/> —
    /// no parallel infrastructure is created.
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers:
    /// <list type="bullet">
    /// <item><description><see cref="AuthorizationConfiguration"/> via <c>IOptions&lt;T&gt;</c></description></item>
    /// <item><description>A <c>"RequireAuthenticated"</c> policy if not already registered</description></item>
    /// <item><description><see cref="IResourceAuthorizer"/> as a scoped service (thin facade over <see cref="Microsoft.AspNetCore.Authorization.IAuthorizationService"/>)</description></item>
    /// <item><description><see cref="AuthorizationPipelineBehavior{TRequest, TResponse}"/> as a pipeline behavior</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// This method complements — not replaces — <see cref="AddAuthorization(EncinaConfiguration)"/>.
    /// You can call both (the behavior is registered once), or use this method alone, which also
    /// registers the behavior. The behavior evaluates the request identity that
    /// <c>UseEncinaContext()</c> binds (<see cref="RequestIdentity.Principal"/>).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.Services.AddEncinaAuthorization(
    ///     auth =>
    ///     {
    ///         auth.AutoApplyPolicies = true;
    ///     },
    ///     policies =>
    ///     {
    ///         policies.AddPolicy("CanEditOrders", p => p
    ///             .RequireAuthenticatedUser()
    ///             .RequireRole("Admin", "OrderManager"));
    ///     });
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaAuthorization(
        this IServiceCollection services,
        Action<AuthorizationConfiguration>? configureAuthorization = null,
        Action<AuthorizationOptions>? configurePolicies = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register AuthorizationConfiguration via IOptions<T>
        services.Configure<AuthorizationConfiguration>(config =>
        {
            configureAuthorization?.Invoke(config);
        });

        // The behavior logs; ResourceAuthorizer reads the request through IHttpContextAccessor.
        services.AddLogging();
        services.AddHttpContextAccessor();

        // The [Authorize] gate itself. TryAddEnumerable: it is added even when other behaviors are
        // registered, and once when AddEncina's cfg.AddAuthorization() registered it too.
        services.TryAddEnumerable(ServiceDescriptor.Scoped(typeof(IPipelineBehavior<,>), typeof(AuthorizationPipelineBehavior<,>)));

        // Register the "RequireAuthenticated" policy if not already configured
        services.AddAuthorizationBuilder()
            .AddPolicy(AuthorizationConfiguration.RequireAuthenticatedPolicyName, policy =>
                policy.RequireAuthenticatedUser());

        // Register IResourceAuthorizer facade (thin wrapper over IAuthorizationService)
        services.TryAddScoped<IResourceAuthorizer, ResourceAuthorizer>();

        // Apply user-provided policies
        if (configurePolicies is not null)
        {
            services.Configure(configurePolicies);
        }

        return services;
    }
}
