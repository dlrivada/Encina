using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Encina;

/// <summary>
/// Registers the request identity model and declares service identities.
/// </summary>
public static class RequestIdentityServiceCollectionExtensions
{
    /// <summary>
    /// Registers the services of the request identity model: the ambient
    /// <see cref="IRequestContextAccessor"/>, <see cref="TimeProvider"/>, the validated
    /// <see cref="RequestIdentityOptions"/> claim map, the internal claim mapper, the
    /// <see cref="IServiceIdentityCatalog"/> and the <see cref="IRequestContextScopeFactory"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional changes to the claim map.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Idempotent: every service is added with <c>TryAdd</c>, so an application's own registration
    /// wins in any order and a second call only adds its <paramref name="configure"/> action.
    /// <c>AddEncina</c> calls this method.
    /// </para>
    /// <para>
    /// Validated at startup (<c>ValidateOnStart</c>): the claim map, the declared service identities,
    /// and that the registered <see cref="IRequestContextAccessor"/> is Encina's
    /// <see cref="RequestContextAccessor"/> (identity scopes and the dispatcher share it; a custom
    /// accessor fails the host).
    /// </para>
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

        services.AddOptions<ServiceIdentityCatalogOptions>().ValidateOnStart();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<ServiceIdentityCatalogOptions>, ServiceIdentityCatalogOptionsValidator>());
        services.TryAddSingleton<IServiceIdentityCatalog, ServiceIdentityCatalog>();

        services.AddOptions<RequestContextAccessorRequirement>().ValidateOnStart();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<RequestContextAccessorRequirement>, RequestContextAccessorRequirementValidator>());

        // One singleton behind both interfaces: Encina packages inject the internal one.
        services.TryAddSingleton<RequestContextScopeFactory>();
        services.TryAddSingleton<IRequestContextScopeFactory>(static provider => provider.GetRequiredService<RequestContextScopeFactory>());
        services.TryAddSingleton<IInternalRequestContextScopeFactory>(static provider => provider.GetRequiredService<RequestContextScopeFactory>());

        return services;
    }

    /// <summary>
    /// Declares a service identity that background work opens with
    /// <see cref="IRequestContextScopeFactory.RunAsServiceAsync{T}"/>, and registers the request
    /// identity model (<see cref="AddEncinaRequestIdentity"/>).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The identity name: pattern <c>^[a-z0-9][a-z0-9.-]{0,62}$</c>, not starting with <c>encina.</c>.</param>
    /// <param name="configure">Declares the roles, permissions and claims; none by default.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    /// <remarks>
    /// The subject of the identity is <c>service:&lt;name&gt;</c>. A repeated identical declaration is
    /// ignored; a different declaration under the same name fails startup, and so does a wildcard
    /// role or permission or a claim with a user-id claim type. Declare one identity per job and the
    /// least authority it needs; the tenant is chosen per scope.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> breaks the name pattern or uses the reserved <c>encina.</c> prefix.</exception>
    /// <example>
    /// <code>
    /// services.AddEncinaServiceIdentity("billing-reconciliation", id => id
    ///     .WithRoles("billing-job")
    ///     .WithPermissions("invoices:reconcile"));
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaServiceIdentity(
        this IServiceCollection services,
        string name,
        Action<ServiceIdentityBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        if (!ServiceIdentityCatalogOptionsValidator.IsValidName(name))
        {
            throw new ArgumentException("A service identity name must match ^[a-z0-9][a-z0-9.-]{0,62}$.", nameof(name));
        }

        if (ServiceIdentityCatalogOptionsValidator.IsReservedName(name))
        {
            throw new ArgumentException("The 'encina.' prefix is reserved for built-in identities of Encina packages.", nameof(name));
        }

        return services.Declare(name, configure, isBuiltIn: false);
    }

    /// <summary>
    /// Declares a built-in service identity of an Encina package (name prefix <c>encina.</c>),
    /// opened only through <see cref="IInternalRequestContextScopeFactory.RunAsBuiltInAsync{T}"/>.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="name"/> breaks the name pattern or lacks the <c>encina.</c> prefix.</exception>
    internal static IServiceCollection AddBuiltInServiceIdentity(
        this IServiceCollection services,
        string name,
        Action<ServiceIdentityBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        if (!ServiceIdentityCatalogOptionsValidator.IsValidName(name) || !ServiceIdentityCatalogOptionsValidator.IsReservedName(name))
        {
            throw new ArgumentException("A built-in service identity name must match ^[a-z0-9][a-z0-9.-]{0,62}$ and start with 'encina.'.", nameof(name));
        }

        return services.Declare(name, configure, isBuiltIn: true);
    }

    private static IServiceCollection Declare(
        this IServiceCollection services,
        string name,
        Action<ServiceIdentityBuilder>? configure,
        bool isBuiltIn)
    {
        var builder = new ServiceIdentityBuilder();
        configure?.Invoke(builder);
        var definition = builder.Build(name, isBuiltIn);

        services.AddEncinaRequestIdentity();
        services.Configure<ServiceIdentityCatalogOptions>(options => options.Declare(definition));
        return services;
    }
}
