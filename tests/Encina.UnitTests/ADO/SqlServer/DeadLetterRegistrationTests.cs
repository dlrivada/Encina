using System.Data.Common;
using Encina.ADO.SqlServer;
using Encina.ADO.SqlServer.DeadLetter;
using Encina.ADO.SqlServer.Tenancy;
using Encina.Messaging.DeadLetter;
using Microsoft.Extensions.Hosting;

namespace Encina.UnitTests.ADO.SqlServer;

/// <summary>
/// Verifies that <c>UseDeadLetterQueue</c> registers the dead letter queue on every ADO.NET SQL Server
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

    private static ServiceCollection NewTenancyServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<ITenantProvider>());
        services.AddSingleton(Substitute.For<ITenantStore>());
        services.AddSingleton(Options.Create(new TenancyOptions { DefaultConnectionString = "Server=test;Database=test;" }));

        // The real factory opens a connection; the registration test only needs the connection to resolve.
        services.AddSingleton<ITenantConnectionFactory>(new StubTenantConnectionFactory(Substitute.For<DbConnection>()));
        return services;
    }

    private sealed class StubTenantConnectionFactory(System.Data.IDbConnection connection) : ITenantConnectionFactory
    {
        public ValueTask<Either<EncinaError, System.Data.IDbConnection>> CreateConnectionAsync(CancellationToken cancellationToken = default)
            => new(Prelude.Right<EncinaError, System.Data.IDbConnection>(connection));

        public ValueTask<Either<EncinaError, System.Data.IDbConnection>> CreateConnectionForTenantAsync(
            string tenantId,
            CancellationToken cancellationToken = default)
            => CreateConnectionAsync(cancellationToken);

        public ValueTask<Either<EncinaError, string>> GetConnectionStringAsync(CancellationToken cancellationToken = default)
            => new(Prelude.Right<EncinaError, string>("Server=test;Database=test;"));
    }

    [Fact]
    public void AddEncinaADO_WithDeadLetterQueue_ResolvesTheStoreManagerAndCleanup()
    {
        var services = NewServices();

        services.AddEncinaADO(config => config.UseDeadLetterQueue = true);

        using var provider = Build(services);
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IDeadLetterStore>().ShouldBeOfType<DeadLetterStoreADO>();
        scope.ServiceProvider.GetRequiredService<IDeadLetterMessageFactory>().ShouldBeOfType<DeadLetterMessageFactory>();
        scope.ServiceProvider.GetRequiredService<IDeadLetterManager>().ShouldBeOfType<DeadLetterManager>();
        provider.GetServices<IHostedService>().ShouldContain(h => h is DeadLetterCleanupProcessor);
    }

    [Fact]
    public void AddEncinaADO_WithoutDeadLetterQueue_RegistersNothing()
    {
        var services = NewServices();

        services.AddEncinaADO(config => config.UseOutbox = true);

        services.Any(d => d.ServiceType == typeof(IDeadLetterStore)).ShouldBeFalse();
    }

    [Fact]
    public void AddEncinaADO_ApplicationStoreRegisteredBeforeTheProvider_IsKept()
    {
        var services = NewServices();
        var own = Substitute.For<IDeadLetterStore>();
        services.AddScoped(_ => own);

        services.AddEncinaADO(config => config.UseDeadLetterQueue = true);

        using var provider = Build(services);
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IDeadLetterStore>().ShouldBeSameAs(own);
    }

    [Fact]
    public void AddEncinaADOWithTenancy_WithDeadLetterQueue_ResolvesTheStoreManagerAndCleanup()
    {
        var services = NewTenancyServices();

        services.AddEncinaADOWithTenancy(config => config.UseDeadLetterQueue = true);

        using var provider = Build(services);
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IDeadLetterStore>().ShouldBeOfType<DeadLetterStoreADO>();
        scope.ServiceProvider.GetRequiredService<IDeadLetterManager>().ShouldBeOfType<DeadLetterManager>();
        provider.GetServices<IHostedService>().ShouldContain(h => h is DeadLetterCleanupProcessor);
    }
}
