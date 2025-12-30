using Encina.Messaging.DeadLetter;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;
using Encina.Testing.Fakes.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.Testing.Fakes;

/// <summary>
/// Extension methods for registering Encina fake implementations in the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all Encina fake implementations as singletons.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers:
    /// <list type="bullet">
    /// <item><description><see cref="FakeEncina"/> as <see cref="IEncina"/></description></item>
    /// <item><description><see cref="FakeOutboxStore"/> as <see cref="IOutboxStore"/></description></item>
    /// <item><description><see cref="FakeInboxStore"/> as <see cref="IInboxStore"/></description></item>
    /// <item><description><see cref="FakeSagaStore"/> as <see cref="ISagaStore"/></description></item>
    /// <item><description><see cref="FakeScheduledMessageStore"/> as <see cref="IScheduledMessageStore"/></description></item>
    /// <item><description><see cref="FakeDeadLetterStore"/> as <see cref="IDeadLetterStore"/></description></item>
    /// </list>
    /// </para>
    /// <para>
    /// All fakes are registered as singletons so you can access them after the test
    /// to verify interactions.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var services = new ServiceCollection();
    /// services.AddEncinaFakes();
    ///
    /// var provider = services.BuildServiceProvider();
    /// var fakeEncina = provider.GetRequiredService&lt;FakeEncina&gt;();
    /// var encina = provider.GetRequiredService&lt;IEncina&gt;(); // Same instance
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaFakes(this IServiceCollection services)
    {
        services.AddFakeEncina();
        services.AddFakeOutboxStore();
        services.AddFakeInboxStore();
        services.AddFakeSagaStore();
        services.AddFakeScheduledMessageStore();
        services.AddFakeDeadLetterStore();

        return services;
    }

    /// <summary>
    /// Registers <see cref="FakeEncina"/> as both <see cref="IEncina"/> and <see cref="FakeEncina"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddFakeEncina(this IServiceCollection services)
    {
        var fakeEncina = new FakeEncina();
        services.TryAddSingleton(fakeEncina);
        services.TryAddSingleton<IEncina>(fakeEncina);
        return services;
    }

    /// <summary>
    /// Registers <see cref="FakeOutboxStore"/> as both <see cref="IOutboxStore"/> and <see cref="FakeOutboxStore"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddFakeOutboxStore(this IServiceCollection services)
    {
        var fakeStore = new FakeOutboxStore();
        services.TryAddSingleton(fakeStore);
        services.TryAddSingleton<IOutboxStore>(fakeStore);
        return services;
    }

    /// <summary>
    /// Registers <see cref="FakeInboxStore"/> as both <see cref="IInboxStore"/> and <see cref="FakeInboxStore"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddFakeInboxStore(this IServiceCollection services)
    {
        var fakeStore = new FakeInboxStore();
        services.TryAddSingleton(fakeStore);
        services.TryAddSingleton<IInboxStore>(fakeStore);
        return services;
    }

    /// <summary>
    /// Registers <see cref="FakeSagaStore"/> as both <see cref="ISagaStore"/> and <see cref="FakeSagaStore"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddFakeSagaStore(this IServiceCollection services)
    {
        var fakeStore = new FakeSagaStore();
        services.TryAddSingleton(fakeStore);
        services.TryAddSingleton<ISagaStore>(fakeStore);
        return services;
    }

    /// <summary>
    /// Registers <see cref="FakeScheduledMessageStore"/> as both <see cref="IScheduledMessageStore"/> and <see cref="FakeScheduledMessageStore"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddFakeScheduledMessageStore(this IServiceCollection services)
    {
        var fakeStore = new FakeScheduledMessageStore();
        services.TryAddSingleton(fakeStore);
        services.TryAddSingleton<IScheduledMessageStore>(fakeStore);
        return services;
    }

    /// <summary>
    /// Registers <see cref="FakeDeadLetterStore"/> as both <see cref="IDeadLetterStore"/> and <see cref="FakeDeadLetterStore"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddFakeDeadLetterStore(this IServiceCollection services)
    {
        var fakeStore = new FakeDeadLetterStore();
        services.TryAddSingleton(fakeStore);
        services.TryAddSingleton<IDeadLetterStore>(fakeStore);
        return services;
    }

    /// <summary>
    /// Replaces existing Encina registrations with fake implementations.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// Use this method when you have already registered real Encina services
    /// and want to replace them with fakes for testing.
    /// </remarks>
    public static IServiceCollection ReplaceWithFakes(this IServiceCollection services)
    {
        // Remove existing registrations (interface or concrete fakes)
        Type[] fakeTypes =
        [
            typeof(FakeEncina),
            typeof(FakeOutboxStore),
            typeof(FakeInboxStore),
            typeof(FakeSagaStore),
            typeof(FakeScheduledMessageStore),
            typeof(FakeDeadLetterStore)
        ];

        var descriptorsToRemove = services
            .Where(d =>
                d.ServiceType == typeof(IEncina) ||
                d.ServiceType == typeof(IOutboxStore) ||
                d.ServiceType == typeof(IInboxStore) ||
                d.ServiceType == typeof(ISagaStore) ||
                d.ServiceType == typeof(IScheduledMessageStore) ||
                d.ServiceType == typeof(IDeadLetterStore) ||
                (d.ImplementationType != null && fakeTypes.Contains(d.ImplementationType)) ||
                (d.ImplementationInstance != null && fakeTypes.Contains(d.ImplementationInstance.GetType())))
            .ToList();

        foreach (var descriptor in descriptorsToRemove)
        {
            services.Remove(descriptor);
        }

        // Add fakes
        return services.AddEncinaFakes();
    }
}
