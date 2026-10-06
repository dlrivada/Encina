using System.Data;
using Encina.ADO.PostgreSQL;
using Encina.Messaging;
using Encina.Security.Audit;
using DbOperationAuditStore = Encina.ADO.PostgreSQL.Auditing.OperationAuditStoreADO;

namespace Encina.UnitTests.ADO.PostgreSQL;

/// <summary>
/// Verifies that the database-backed <see cref="IOperationAuditStore"/> wins over
/// <see cref="InMemoryOperationAuditStore"/> regardless of the order in which
/// <c>AddEncinaAudit</c> and <c>AddEncinaADO</c> run, and never replaces a store the application
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
    public void AddEncinaAudit_ThenAddEncinaADO_ResolvesDatabaseStore()
    {
        // Arrange
        var services = NewServices();

        // Act
        services.AddEncinaAudit();
        services.AddEncinaADO(config => config.UseOperationAuditStore = true);

        // Assert
        using var provider = Build(services);
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IOperationAuditStore>()
            .ShouldBeOfType<DbOperationAuditStore>();
    }

    [Fact]
    public void AddEncinaADO_ThenAddEncinaAudit_ResolvesDatabaseStore()
    {
        // Arrange
        var services = NewServices();

        // Act
        services.AddEncinaADO(config => config.UseOperationAuditStore = true);
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
        services.AddEncinaADO(config => config.UseOperationAuditStore = true);
        services.AddEncinaAudit();

        // Assert
        using var provider = Build(services);
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IOperationAuditStore>().ShouldBeSameAs(customStore);
    }

    [Fact]
    public void AddEncinaADO_WithoutTheFlag_DoesNotRegisterTheDatabaseStore()
    {
        // Arrange
        var services = NewServices();

        // Act
        services.AddEncinaAudit();
        services.AddEncinaADO(config => config.UseOperationAuditStore = false);

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
        services.AddEncinaADO(config => config.UseOperationAuditStore = true);

        // Assert - ValidateScopes fails the build when the singleton hosted service captures the scoped store
        using var provider = Build(services);
        provider.GetServices<Microsoft.Extensions.Hosting.IHostedService>()
            .OfType<OperationAuditRetentionService>()
            .ShouldHaveSingleItem();
    }
}
