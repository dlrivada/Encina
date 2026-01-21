using Encina.AspNetCore;
using Encina.Tenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Encina.UnitTests.Tenancy;

/// <summary>
/// Unit tests for tenancy ServiceCollectionExtensions.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    #region AddEncinaTenancy Tests

    [Fact]
    public void AddEncinaTenancy_NullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEncinaTenancy());
    }

    [Fact]
    public void AddEncinaTenancy_Default_RegistersRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IRequestContextAccessor>());

        // Act
        services.AddEncinaTenancy();
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<ITenantStore>().ShouldNotBeNull();
        provider.GetService<IOptions<TenancyOptions>>().ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaTenancy_WithConfiguration_AppliesOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IRequestContextAccessor>());

        // Act
        services.AddEncinaTenancy(options =>
        {
            options.DefaultStrategy = TenantIsolationStrategy.DatabasePerTenant;
            options.RequireTenant = true;
            options.TenantIdPropertyName = "CustomTenantId";
            options.DefaultConnectionString = "Server=test;";
            options.DefaultSchemaName = "custom_schema";
            options.ValidateTenantOnRequest = true;
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<TenancyOptions>>().Value;

        // Assert
        options.DefaultStrategy.ShouldBe(TenantIsolationStrategy.DatabasePerTenant);
        options.RequireTenant.ShouldBeTrue();
        options.TenantIdPropertyName.ShouldBe("CustomTenantId");
        options.DefaultConnectionString.ShouldBe("Server=test;");
        options.DefaultSchemaName.ShouldBe("custom_schema");
        options.ValidateTenantOnRequest.ShouldBeTrue();
    }

    [Fact]
    public void AddEncinaTenancy_WithTenants_RegistersTenantsInStore()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IRequestContextAccessor>());

        // Act
        services.AddEncinaTenancy(options =>
        {
            options.Tenants.Add(new TenantInfo("t1", "Tenant 1", TenantIsolationStrategy.SharedSchema));
            options.Tenants.Add(new TenantInfo("t2", "Tenant 2", TenantIsolationStrategy.DatabasePerTenant));
        });

        var provider = services.BuildServiceProvider();
        var store = provider.GetRequiredService<ITenantStore>() as InMemoryTenantStore;

        // Assert
        store.ShouldNotBeNull();
        store.Count.ShouldBe(2);
    }

    [Fact]
    public void AddEncinaTenancy_RegistersInMemoryTenantStoreByDefault()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IRequestContextAccessor>());

        // Act
        services.AddEncinaTenancy();
        var provider = services.BuildServiceProvider();
        var store = provider.GetService<ITenantStore>();

        // Assert
        store.ShouldBeOfType<InMemoryTenantStore>();
    }

    [Fact]
    public void AddEncinaTenancy_RegistersTenantProviderAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IRequestContextAccessor>());

        // Act
        services.AddEncinaTenancy();

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ITenantProvider));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddEncinaTenancy_CalledMultipleTimes_DoesNotDuplicateServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IRequestContextAccessor>());

        // Act
        services.AddEncinaTenancy();
        services.AddEncinaTenancy();

        // Assert
        services.Count(d => d.ServiceType == typeof(ITenantStore)).ShouldBe(1);
        services.Count(d => d.ServiceType == typeof(ITenantProvider)).ShouldBe(1);
    }

    #endregion

    #region AddTenantStore Tests

    [Fact]
    public void AddTenantStore_Generic_ReplacesDefaultStore()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IRequestContextAccessor>());
        services.AddEncinaTenancy();

        // Act
        services.AddTenantStore<CustomTenantStore>();
        var provider = services.BuildServiceProvider();
        var store = provider.GetService<ITenantStore>();

        // Assert
        store.ShouldBeOfType<CustomTenantStore>();
    }

    [Fact]
    public void AddTenantStore_WithFactory_ReplacesDefaultStore()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IRequestContextAccessor>());
        services.AddEncinaTenancy();
        var customStore = new CustomTenantStore();

        // Act
        services.AddTenantStore(_ => customStore);
        var provider = services.BuildServiceProvider();
        var store = provider.GetService<ITenantStore>();

        // Assert
        store.ShouldBeSameAs(customStore);
    }

    [Fact]
    public void AddTenantStore_NullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddTenantStore<CustomTenantStore>());
    }

    [Fact]
    public void AddTenantStore_NullFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddTenantStore(null!));
    }

    #endregion

    #region ConfigureTenantConnections Tests

    [Fact]
    public void ConfigureTenantConnections_AppliesOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.ConfigureTenantConnections(options =>
        {
            options.DefaultConnectionString = "Server=configured;";
            options.AutoOpenConnections = false;
            options.ConnectionTimeoutSeconds = 60;
            options.ThrowOnMissingConnectionString = false;
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<TenantConnectionOptions>>().Value;

        // Assert
        options.DefaultConnectionString.ShouldBe("Server=configured;");
        options.AutoOpenConnections.ShouldBeFalse();
        options.ConnectionTimeoutSeconds.ShouldBe(60);
        options.ThrowOnMissingConnectionString.ShouldBeFalse();
    }

    [Fact]
    public void ConfigureTenantConnections_NullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).ConfigureTenantConnections(_ => { }));
    }

    [Fact]
    public void ConfigureTenantConnections_NullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.ConfigureTenantConnections(null!));
    }

    #endregion

    #region Helper Classes

    private sealed class CustomTenantStore : ITenantStore
    {
        public ValueTask<TenantInfo?> GetTenantAsync(string tenantId, CancellationToken cancellationToken = default)
            => new((TenantInfo?)null);

        public ValueTask<IReadOnlyList<TenantInfo>> GetAllTenantsAsync(CancellationToken cancellationToken = default)
            => new(Array.Empty<TenantInfo>());

        public ValueTask<bool> ExistsAsync(string tenantId, CancellationToken cancellationToken = default)
            => new(false);
    }

    #endregion
}
