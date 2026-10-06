using Encina.EntityFrameworkCore;
using Encina.Security.Audit;
using Microsoft.EntityFrameworkCore;
using DbOperationAuditStore = Encina.EntityFrameworkCore.Auditing.OperationAuditStoreEF;

namespace Encina.UnitTests.EntityFrameworkCore;

/// <summary>
/// Verifies that the EF Core <see cref="IOperationAuditStore"/> follows the same rule as the ADO, Dapper
/// and MongoDB providers: it wins over <see cref="InMemoryOperationAuditStore"/> regardless of the order
/// in which <c>AddEncinaAudit</c> and <c>AddEncinaEntityFrameworkCore</c> run, and never replaces a store
/// the application registered itself (#1633, #1269). Every provider is built with
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
        services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        return services;
    }

    [Fact]
    public void AddEncinaAudit_ThenAddEncinaEntityFrameworkCore_ResolvesDatabaseStore()
    {
        // Arrange
        var services = NewServices();

        // Act
        services.AddEncinaAudit();
        services.AddEncinaEntityFrameworkCore<TestDbContext>(config => config.UseOperationAuditStore = true);

        // Assert
        using var provider = Build(services);
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IOperationAuditStore>()
            .ShouldBeOfType<DbOperationAuditStore>();
    }

    [Fact]
    public void AddEncinaEntityFrameworkCore_ThenAddEncinaAudit_ResolvesDatabaseStore()
    {
        // Arrange
        var services = NewServices();

        // Act
        services.AddEncinaEntityFrameworkCore<TestDbContext>(config => config.UseOperationAuditStore = true);
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
        services.AddEncinaEntityFrameworkCore<TestDbContext>(config => config.UseOperationAuditStore = true);
        services.AddEncinaAudit();

        // Assert
        using var provider = Build(services);
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IOperationAuditStore>().ShouldBeSameAs(customStore);
    }

    [Fact]
    public void CustomOperationAuditStore_RegisteredAfterProvider_StaysResolved()
    {
        // Arrange
        var services = NewServices();
        var customStore = Substitute.For<IOperationAuditStore>();

        // Act
        services.AddEncinaEntityFrameworkCore<TestDbContext>(config => config.UseOperationAuditStore = true);
        services.AddSingleton(customStore);
        services.AddEncinaAudit();

        // Assert
        using var provider = Build(services);
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IOperationAuditStore>().ShouldBeSameAs(customStore);
    }

    [Fact]
    public void AddEncinaEntityFrameworkCore_WithoutTheFlag_DoesNotRegisterTheDatabaseStore()
    {
        // Arrange
        var services = NewServices();

        // Act
        services.AddEncinaAudit();
        services.AddEncinaEntityFrameworkCore<TestDbContext>(config => config.UseOperationAuditStore = false);

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
        services.AddEncinaEntityFrameworkCore<TestDbContext>(config => config.UseOperationAuditStore = true);

        // Assert - ValidateScopes fails the build when the singleton hosted service captures the scoped store
        using var provider = Build(services);
        provider.GetServices<Microsoft.Extensions.Hosting.IHostedService>()
            .OfType<OperationAuditRetentionService>()
            .ShouldHaveSingleItem();
    }
}
