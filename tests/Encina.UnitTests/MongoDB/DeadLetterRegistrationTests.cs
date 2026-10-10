using Encina.Messaging.DeadLetter;
using Encina.MongoDB;
using Encina.MongoDB.DeadLetter;
using Microsoft.Extensions.Hosting;

namespace Encina.UnitTests.MongoDB;

/// <summary>
/// Verifies that <c>UseDeadLetterQueue</c> registers the dead letter queue on the MongoDB registration
/// path, built with <c>ValidateOnBuild</c> and <c>ValidateScopes</c> (#583, AGENTS.md section 3).
/// </summary>
public sealed class DeadLetterRegistrationTests
{
    private static ServiceProvider Build(IServiceCollection services) =>
        services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

    private static ServiceCollection NewServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        return services;
    }

    private static void AddMongo(IServiceCollection services, bool useDeadLetterQueue) =>
        services.AddEncinaMongoDB(opts =>
        {
            opts.ConnectionString = "mongodb://localhost";
            opts.UseDeadLetterQueue = useDeadLetterQueue;
        });

    [Fact]
    public void AddEncinaMongoDB_WithDeadLetterQueue_ResolvesTheStoreManagerAndCleanup()
    {
        var services = NewServices();

        AddMongo(services, useDeadLetterQueue: true);

        using var provider = Build(services);
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IDeadLetterStore>().ShouldBeOfType<DeadLetterStoreMongoDB>();
        scope.ServiceProvider.GetRequiredService<IDeadLetterMessageFactory>().ShouldBeOfType<DeadLetterMessageFactory>();
        scope.ServiceProvider.GetRequiredService<IDeadLetterManager>().ShouldBeOfType<DeadLetterManager>();
        provider.GetServices<IHostedService>().ShouldContain(h => h is DeadLetterCleanupProcessor);
    }

    [Fact]
    public void AddEncinaMongoDB_WithoutDeadLetterQueue_RegistersNothing()
    {
        var services = NewServices();

        AddMongo(services, useDeadLetterQueue: false);

        services.Any(d => d.ServiceType == typeof(IDeadLetterStore)).ShouldBeFalse();
    }

    [Fact]
    public void AddEncinaMongoDB_ApplicationStoreRegisteredBeforeTheProvider_IsKept()
    {
        var services = NewServices();
        var own = Substitute.For<IDeadLetterStore>();
        services.AddScoped(_ => own);

        AddMongo(services, useDeadLetterQueue: true);

        using var provider = Build(services);
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IDeadLetterStore>().ShouldBeSameAs(own);
    }
}
