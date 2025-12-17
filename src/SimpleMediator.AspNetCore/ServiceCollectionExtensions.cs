using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SimpleMediator.AspNetCore;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> to register SimpleMediator ASP.NET Core integration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds SimpleMediator ASP.NET Core integration services to the service collection.
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
    /// After calling this method, use <c>app.UseSimpleMediatorContext()</c> in your middleware pipeline.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var builder = WebApplication.CreateBuilder(args);
    ///
    /// // Register SimpleMediator with ASP.NET Core integration
    /// builder.Services.AddSimpleMediator(cfg => { }, typeof(Program).Assembly);
    /// builder.Services.AddSimpleMediatorAspNetCore();
    ///
    /// var app = builder.Build();
    ///
    /// app.UseAuthentication();
    /// app.UseAuthorization();
    /// app.UseSimpleMediatorContext();
    ///
    /// app.MapControllers();
    /// app.Run();
    /// </code>
    /// </example>
    public static IServiceCollection AddSimpleMediatorAspNetCore(this IServiceCollection services)
    {
        return services.AddSimpleMediatorAspNetCore(_ => { });
    }

    /// <summary>
    /// Adds SimpleMediator ASP.NET Core integration services with custom configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddSimpleMediatorAspNetCore(options =>
    /// {
    ///     options.CorrelationIdHeader = "X-Request-ID";
    ///     options.TenantIdHeader = "X-Tenant";
    ///     options.UserIdClaimType = "sub";
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddSimpleMediatorAspNetCore(
        this IServiceCollection services,
        Action<SimpleMediatorAspNetCoreOptions> configureOptions)
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
    /// Adds authorization pipeline behavior to SimpleMediator.
    /// </summary>
    /// <param name="configuration">The SimpleMediator configuration.</param>
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
    /// builder.Services.AddSimpleMediator(cfg =>
    /// {
    ///     cfg.AddAuthorization(); // Enable [Authorize] attribute support
    /// }, typeof(Program).Assembly);
    /// </code>
    /// </example>
    public static SimpleMediatorConfiguration AddAuthorization(this SimpleMediatorConfiguration configuration)
    {
        // Register authorization behavior
        configuration.AddPipelineBehavior(typeof(AuthorizationPipelineBehavior<,>));

        return configuration;
    }
}
