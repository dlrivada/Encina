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
    /// <item><description><see cref="IRequestContextAccessor"/> for ambient context access</description></item>
    /// <item><description>Default configuration options</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// After calling this method, use <c>app.UseEncinaContext()</c> in your middleware pipeline.
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
    /// app.UseAuthentication();
    /// app.UseAuthorization();
    /// app.UseEncinaContext();
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
    ///     options.UserIdClaimType = "sub";
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaAspNetCore(
        this IServiceCollection services,
        Action<EncinaAspNetCoreOptions> configureOptions)
    {
        // Register options
        services.Configure(configureOptions);

        // Register request context accessor as singleton (AsyncLocal-based)
        services.TryAddSingleton<IRequestContextAccessor, RequestContextAccessor>();

        // Register HttpContextAccessor (required by authorization behavior)
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
    /// </list>
    /// </para>
    /// <para>
    /// This method complements — not replaces — <see cref="AddAuthorization(EncinaConfiguration)"/>.
    /// You can call both, or use this method alone which also registers the behavior.
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
        // Register AuthorizationConfiguration via IOptions<T>
        services.Configure<AuthorizationConfiguration>(config =>
        {
            configureAuthorization?.Invoke(config);
        });

        // Ensure HttpContextAccessor is available
        services.AddHttpContextAccessor();

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
