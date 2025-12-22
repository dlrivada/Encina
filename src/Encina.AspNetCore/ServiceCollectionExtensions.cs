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
    /// This adds a pipeline behavior that enforces <see cref="Microsoft.AspNetCore.Authorization.AuthorizeAttribute"/>
    /// on request types using ASP.NET Core's authorization system.
    /// </para>
    /// <para>
    /// Supports role-based and policy-based authorization.
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
}
