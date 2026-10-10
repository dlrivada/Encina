using Encina.EntityFrameworkCore;
using Encina.EntityFrameworkCore.DeadLetter;
using Encina.Messaging.DeadLetter;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Encina.UnitTests.EntityFrameworkCore;

/// <summary>
/// Verifies that <c>UseDeadLetterQueue</c> registers the dead letter queue on the EF Core registration
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
        services.AddDbContext<TestDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        return services;
    }

    [Fact]
    public void AddEncinaEntityFrameworkCore_WithDeadLetterQueue_ResolvesTheStoreManagerAndCleanup()
    {
        var services = NewServices();

        services.AddEncinaEntityFrameworkCore<TestDbContext>(config => config.UseDeadLetterQueue = true);

        using var provider = Build(services);
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IDeadLetterStore>().ShouldBeOfType<DeadLetterStoreEF>();
        scope.ServiceProvider.GetRequiredService<IDeadLetterMessageFactory>().ShouldBeOfType<DeadLetterMessageFactory>();
        scope.ServiceProvider.GetRequiredService<IDeadLetterManager>().ShouldBeOfType<DeadLetterManager>();
        provider.GetServices<IHostedService>().ShouldContain(h => h is DeadLetterCleanupProcessor);
    }

    [Fact]
    public void AddEncinaEntityFrameworkCore_WithoutDeadLetterQueue_RegistersNothing()
    {
        var services = NewServices();

        services.AddEncinaEntityFrameworkCore<TestDbContext>(config => config.UseOutbox = true);

        services.Any(d => d.ServiceType == typeof(IDeadLetterStore)).ShouldBeFalse();
    }

    [Fact]
    public void AddEncinaEntityFrameworkCore_ApplicationStoreRegisteredBeforeTheProvider_IsKept()
    {
        var services = NewServices();
        var own = Substitute.For<IDeadLetterStore>();
        services.AddScoped(_ => own);

        services.AddEncinaEntityFrameworkCore<TestDbContext>(config => config.UseDeadLetterQueue = true);

        using var provider = Build(services);
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IDeadLetterStore>().ShouldBeSameAs(own);
    }
}
