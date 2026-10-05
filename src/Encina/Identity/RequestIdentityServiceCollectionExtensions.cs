using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Encina;

/// <summary>
/// Registers the request identity model.
/// </summary>
public static class RequestIdentityServiceCollectionExtensions
{
    /// <summary>
    /// Registers the services of the request identity model: the ambient
    /// <see cref="IRequestContextAccessor"/>, <see cref="TimeProvider"/>, the validated
    /// <see cref="RequestIdentityOptions"/> claim map and the internal claim mapper.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional changes to the claim map.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    /// <remarks>
    /// Idempotent: every service is added with <c>TryAdd</c>, so an application's own registration
    /// wins in any order and a second call only adds its <paramref name="configure"/> action. The
    /// options are validated at startup (<c>ValidateOnStart</c>). <c>AddEncina</c> calls this method.
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaRequestIdentity(options =>
    /// {
    ///     options.TenantIdClaimTypes.Clear();
    ///     options.TenantIdClaimTypes.Add("org_id");
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaRequestIdentity(
        this IServiceCollection services,
        Action<RequestIdentityOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IRequestContextAccessor, RequestContextAccessor>();
        services.TryAddSingleton(TimeProvider.System);

        var optionsBuilder = services.AddOptions<RequestIdentityOptions>();
        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }

        optionsBuilder.ValidateOnStart();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<RequestIdentityOptions>, RequestIdentityOptionsValidator>());
        services.TryAddSingleton<IRequestIdentityFactory, ClaimsRequestIdentityFactory>();

        return services;
    }
}
