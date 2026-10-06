using Encina.MongoDB;
using Encina.MongoDB.Auditing;
using Encina.Security.Audit;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.UnitTests.MongoDB;

/// <summary>
/// Verifies that the MongoDB <see cref="IOperationAuditStore"/> wins over
/// <see cref="InMemoryOperationAuditStore"/> regardless of the order in which
/// <c>AddEncinaAudit</c> and <c>AddEncinaMongoDB</c> run, and never replaces a store the
/// application registered itself (#1633, #1269). Every provider is built with
/// <c>ValidateOnBuild</c> and <c>ValidateScopes</c>.
/// </summary>
public sealed class OperationAuditStoreRegistrationTests
{
    private static ServiceProvider Build(IServiceCollection services) =>
        services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

    private static ServiceCollection NewServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        return services;
    }

    private static void AddMongo(IServiceCollection services, bool useOperationAuditStore) =>
        services.AddEncinaMongoDB(opts =>
        {
            opts.ConnectionString = "mongodb://localhost";
            opts.UseOperationAuditStore = useOperationAuditStore;
        });

    [Fact]
    public void AddEncinaAudit_ThenAddEncinaMongoDB_ResolvesDatabaseStore()
    {
        // Arrange
        var services = NewServices();

        // Act
        services.AddEncinaAudit();
        AddMongo(services, useOperationAuditStore: true);

        // Assert
        using var provider = Build(services);
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IOperationAuditStore>()
            .ShouldBeOfType<OperationAuditStoreMongoDB>();
    }

    [Fact]
    public void AddEncinaMongoDB_ThenAddEncinaAudit_ResolvesDatabaseStore()
    {
        // Arrange
        var services = NewServices();

        // Act
        AddMongo(services, useOperationAuditStore: true);
        services.AddEncinaAudit();

        // Assert
        using var provider = Build(services);
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IOperationAuditStore>()
            .ShouldBeOfType<OperationAuditStoreMongoDB>();
    }

    [Fact]
    public void CustomOperationAuditStore_RegisteredBeforeProvider_StaysResolved()
    {
        // Arrange
        var services = NewServices();
        var customStore = Substitute.For<IOperationAuditStore>();
        services.AddSingleton(customStore);

        // Act
        AddMongo(services, useOperationAuditStore: true);
        services.AddEncinaAudit();

        // Assert
        using var provider = Build(services);
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IOperationAuditStore>().ShouldBeSameAs(customStore);
    }

    [Fact]
    public void AddEncinaMongoDB_WithoutTheFlag_DoesNotRegisterTheDatabaseStore()
    {
        // Arrange
        var services = NewServices();

        // Act
        services.AddEncinaAudit();
        AddMongo(services, useOperationAuditStore: false);

        // Assert
        using var provider = Build(services);
        provider.GetRequiredService<IOperationAuditStore>()
            .ShouldBeOfType<InMemoryOperationAuditStore>();
    }

    [Fact]
    public void AddEncinaAudit_WithAutoPurge_BuildsTheRetentionServiceOverTheScopedStore()
    {
        // Arrange
        var services = NewServices();

        // Act
        services.AddEncinaAudit(options => options.EnableAutoPurge = true);
        AddMongo(services, useOperationAuditStore: true);

        // Assert - ValidateScopes fails the build when the singleton hosted service captures the scoped store
        using var provider = Build(services);
        provider.GetServices<Microsoft.Extensions.Hosting.IHostedService>()
            .OfType<OperationAuditRetentionService>()
            .ShouldHaveSingleItem();
    }
}
