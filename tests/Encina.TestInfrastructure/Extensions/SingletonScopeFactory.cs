using Microsoft.Extensions.DependencyInjection;

namespace Encina.TestInfrastructure.Extensions;

/// <summary>
/// Builds an <see cref="IServiceScopeFactory"/> whose every scope resolves the given service instance,
/// so unit tests can hand a substitute to code that resolves its store from a child scope.
/// </summary>
public static class SingletonScopeFactory
{
    /// <summary>Creates a scope factory that resolves <typeparamref name="TService"/> to <paramref name="instance"/>.</summary>
    /// <typeparam name="TService">The service type to expose.</typeparam>
    /// <param name="instance">The instance every scope returns.</param>
    /// <returns>The scope factory of a provider that holds only <paramref name="instance"/>.</returns>
    public static IServiceScopeFactory For<TService>(TService instance)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(instance);

        var services = new ServiceCollection();
        services.AddSingleton(instance);
        return services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }
}
