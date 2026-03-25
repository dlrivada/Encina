using Encina.Tenancy;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Encina.UnitTests.Tenancy;

/// <summary>
/// Extended unit tests for Tenancy <see cref="ServiceCollectionExtensions"/>.
/// Covers AddTenantStore overloads and ConfigureTenantConnections.
/// </summary>
public sealed class ServiceCollectionExtensionsExtendedTests
{
    #region AddTenantStore<TStore> Tests

    [Fact]
    public void AddTenantStore_WithNullServices_ThrowsArgumentNullException()
    {
        IServiceCollection? services = null;
        Should.Throw<ArgumentNullException>(() =>
            services!.AddTenantStore<InMemoryTenantStore>());
    }

    [Fact]
    public void AddTenantStore_ReplacesDefaultStore()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncinaTenancy();

        // Act
        services.AddTenantStore<InMemoryTenantStore>();

        // Assert
        var sp = services.BuildServiceProvider();
        var store = sp.GetRequiredService<ITenantStore>();
        store.ShouldBeOfType<InMemoryTenantStore>();
    }

    #endregion

    #region AddTenantStore(factory) Tests

    [Fact]
    public void AddTenantStoreFactory_WithNullServices_ThrowsArgumentNullException()
    {
        IServiceCollection? services = null;
        Should.Throw<ArgumentNullException>(() =>
            services!.AddTenantStore(_ => new InMemoryTenantStore()));
    }

    [Fact]
    public void AddTenantStoreFactory_WithNullFactory_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        Should.Throw<ArgumentNullException>(() =>
            services.AddTenantStore(null!));
    }

    [Fact]
    public void AddTenantStoreFactory_UsesFactory()
    {
        // Arrange
        var customStore = new InMemoryTenantStore([
            new TenantInfo("t1", "Factory Tenant", TenantIsolationStrategy.SharedSchema)
        ]);
        var services = new ServiceCollection();
        services.AddEncinaTenancy();

        // Act
        services.AddTenantStore(_ => customStore);

        // Assert
        var sp = services.BuildServiceProvider();
        var store = sp.GetRequiredService<ITenantStore>();
        store.ShouldBeSameAs(customStore);
    }

    #endregion

    #region ConfigureTenantConnections Tests

    [Fact]
    public void ConfigureTenantConnections_WithNullServices_ThrowsArgumentNullException()
    {
        IServiceCollection? services = null;
        Should.Throw<ArgumentNullException>(() =>
            services!.ConfigureTenantConnections(_ => { }));
    }

    [Fact]
    public void ConfigureTenantConnections_WithNullConfigure_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        Should.Throw<ArgumentNullException>(() =>
            services.ConfigureTenantConnections(null!));
    }

    [Fact]
    public void ConfigureTenantConnections_AppliesConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.ConfigureTenantConnections(opt =>
        {
            opt.AutoOpenConnections = false;
            opt.ConnectionTimeoutSeconds = 60;
            opt.ThrowOnMissingConnectionString = false;
        });

        // Assert
        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<TenantConnectionOptions>>();
        options.Value.AutoOpenConnections.ShouldBeFalse();
        options.Value.ConnectionTimeoutSeconds.ShouldBe(60);
        options.Value.ThrowOnMissingConnectionString.ShouldBeFalse();
    }

    #endregion

    #region AddEncinaTenancy with Tenants

    [Fact]
    public void AddEncinaTenancy_WithTenantsInOptions_PopulatesStore()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaTenancy(opt =>
        {
            opt.Tenants.Add(new TenantInfo("t1", "Tenant 1", TenantIsolationStrategy.SharedSchema));
            opt.Tenants.Add(new TenantInfo("t2", "Tenant 2", TenantIsolationStrategy.DatabasePerTenant, "Server=t2;"));
        });

        // Assert
        var sp = services.BuildServiceProvider();
        var store = sp.GetRequiredService<ITenantStore>();
        var t1 = store.GetTenantAsync("t1").AsTask().Result;
        var t2 = store.GetTenantAsync("t2").AsTask().Result;
        t1.ShouldNotBeNull();
        t1.Name.ShouldBe("Tenant 1");
        t2.ShouldNotBeNull();
        t2.HasDedicatedDatabase.ShouldBeTrue();
    }

    [Fact]
    public void AddEncinaTenancy_WithNullConfigure_UsesDefaults()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaTenancy();

        // Assert
        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<TenancyOptions>>();
        options.Value.DefaultStrategy.ShouldBe(TenantIsolationStrategy.SharedSchema);
        options.Value.RequireTenant.ShouldBeFalse();
    }

    [Fact]
    public void AddEncinaTenancy_RegistersInMemoryTenantStoreDirectly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaTenancy();

        // Assert
        var sp = services.BuildServiceProvider();
        var store = sp.GetRequiredService<InMemoryTenantStore>();
        store.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaTenancy_WithDefaultConnectionString_SetsConnectionOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaTenancy(opt =>
        {
            opt.DefaultConnectionString = "Server=shared;Database=App;";
        });

        // Assert
        var sp = services.BuildServiceProvider();
        var connOptions = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<TenantConnectionOptions>>();
        connOptions.Value.DefaultConnectionString.ShouldBe("Server=shared;Database=App;");
    }

    #endregion
}
