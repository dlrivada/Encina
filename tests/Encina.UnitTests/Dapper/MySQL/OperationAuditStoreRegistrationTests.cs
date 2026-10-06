using System.Data;
using Encina.Dapper.MySQL;
using Encina.Messaging;
using Encina.Security.Audit;
using DbOperationAuditStore = Encina.Dapper.MySQL.Auditing.OperationAuditStoreDapper;

namespace Encina.UnitTests.Dapper.MySQL;

/// <summary>
/// Verifies that the database-backed <see cref="IOperationAuditStore"/> wins over
/// <see cref="InMemoryOperationAuditStore"/> regardless of the order in which
/// <c>AddEncinaAudit</c> and <c>AddEncinaDapper</c> run, and never replaces a store the application
/// registered itself (#1633, #1269). Every provider is built with <c>ValidateOnBuild</c> and
/// <c>ValidateScopes</c>.
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
        services.AddSingleton(Substitute.For<IDbConnection>());
        return services;
    }

    [Fact]
    public void AddEncinaAudit_ThenAddEncinaDapper_ResolvesDatabaseStore()
    {
        // Arrange
        var services = NewServices();

        // Act
        services.AddEncinaAudit();
        services.AddEncinaDapper(config => config.UseOperationAuditStore = true);

        // Assert
        using var provider = Build(services);
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IOperationAuditStore>()
            .ShouldBeOfType<DbOperationAuditStore>();
    }

    [Fact]
    public void AddEncinaDapper_ThenAddEncinaAudit_ResolvesDatabaseStore()
    {
        // Arrange
        var services = NewServices();

        // Act
        services.AddEncinaDapper(config => config.UseOperationAuditStore = true);
        services.AddEncinaAudit();

        // Assert
        using var provider = Build(services);
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IOperationAuditStore>()
            .ShouldBeOfType<DbOperationAuditStore>();
    }

    [Fact]
    public void CustomOperationAuditStore_RegisteredBeforeProvider_StaysResolved()
    {
        // Arrange
        var services = NewServices();
        var customStore = Substitute.For<IOperationAuditStore>();
        services.AddSingleton(customStore);

        // Act
        services.AddEncinaDapper(config => config.UseOperationAuditStore = true);
        services.AddEncinaAudit();

        // Assert
        using var provider = Build(services);
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IOperationAuditStore>().ShouldBeSameAs(customStore);
    }

    [Fact]
    public void AddEncinaDapper_WithoutTheFlag_DoesNotRegisterTheDatabaseStore()
    {
        // Arrange
        var services = NewServices();

        // Act
        services.AddEncinaAudit();
        services.AddEncinaDapper(config => config.UseOperationAuditStore = false);

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
        services.AddEncinaDapper(config => config.UseOperationAuditStore = true);

        // Assert - ValidateScopes fails the build when the singleton hosted service captures the scoped store
        using var provider = Build(services);
        provider.GetServices<Microsoft.Extensions.Hosting.IHostedService>()
            .OfType<OperationAuditRetentionService>()
            .ShouldHaveSingleItem();
    }
}
