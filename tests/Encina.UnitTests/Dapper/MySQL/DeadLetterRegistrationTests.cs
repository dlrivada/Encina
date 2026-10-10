using System.Data.Common;
using Encina.Dapper.MySQL;
using Encina.Dapper.MySQL.DeadLetter;
using Encina.Messaging.DeadLetter;
using Microsoft.Extensions.Hosting;

namespace Encina.UnitTests.Dapper.MySQL;

/// <summary>
/// Verifies that <c>UseDeadLetterQueue</c> registers the dead letter queue on the Dapper MySQL
/// registration path, built with <c>ValidateOnBuild</c> and <c>ValidateScopes</c> (#583, AGENTS.md section 3).
/// </summary>
public sealed class DeadLetterRegistrationTests
{
    private static ServiceProvider Build(IServiceCollection services) =>
        services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

    private static ServiceCollection NewServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<System.Data.IDbConnection>(Substitute.For<DbConnection>());
        return services;
    }

    [Fact]
    public void AddEncinaDapper_WithDeadLetterQueue_ResolvesTheStoreManagerAndCleanup()
    {
        var services = NewServices();

        services.AddEncinaDapper(config => config.UseDeadLetterQueue = true);

        using var provider = Build(services);
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IDeadLetterStore>().ShouldBeOfType<DeadLetterStoreDapper>();
        scope.ServiceProvider.GetRequiredService<IDeadLetterMessageFactory>().ShouldBeOfType<DeadLetterMessageFactory>();
        scope.ServiceProvider.GetRequiredService<IDeadLetterManager>().ShouldBeOfType<DeadLetterManager>();
        provider.GetServices<IHostedService>().ShouldContain(h => h is DeadLetterCleanupProcessor);
    }

    [Fact]
    public void AddEncinaDapper_WithoutDeadLetterQueue_RegistersNothing()
    {
        var services = NewServices();

        services.AddEncinaDapper(config => config.UseOutbox = true);

        services.Any(d => d.ServiceType == typeof(IDeadLetterStore)).ShouldBeFalse();
    }

    [Fact]
    public void AddEncinaDapper_ApplicationStoreRegisteredBeforeTheProvider_IsKept()
    {
        var services = NewServices();
        var own = Substitute.For<IDeadLetterStore>();
        services.AddScoped(_ => own);

        services.AddEncinaDapper(config => config.UseDeadLetterQueue = true);

        using var provider = Build(services);
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IDeadLetterStore>().ShouldBeSameAs(own);
    }
}
