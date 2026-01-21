namespace Encina.UnitTests.Tenancy.AspNetCore;

/// <summary>
/// Unit tests for Encina.Tenancy.AspNetCore ServiceCollectionExtensions.
/// </summary>
public class TenancyAspNetCoreServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEncinaTenancyAspNetCore_NullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEncinaTenancyAspNetCore());
    }

    [Fact]
    public void AddEncinaTenancyAspNetCore_NullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaTenancyAspNetCore(null!));
    }

    [Fact]
    public void AddEncinaTenancyAspNetCore_RegistersBuiltInResolvers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncinaTenancy(); // Required for ITenantStore

        // Act
        services.AddEncinaTenancyAspNetCore();
        var provider = services.BuildServiceProvider();

        // Assert
        var resolvers = provider.GetServices<ITenantResolver>().ToList();
        resolvers.ShouldContain(r => r is HeaderTenantResolver);
        resolvers.ShouldContain(r => r is ClaimTenantResolver);
        resolvers.ShouldContain(r => r is RouteTenantResolver);
        resolvers.ShouldContain(r => r is SubdomainTenantResolver);
    }

    [Fact]
    public void AddEncinaTenancyAspNetCore_RegistersOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncinaTenancy();

        // Act
        services.AddEncinaTenancyAspNetCore();
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetService<IOptions<TenancyAspNetCoreOptions>>();
        options.ShouldNotBeNull();
        options.Value.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaTenancyAspNetCore_WithConfiguration_AppliesOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncinaTenancy();

        // Act
        services.AddEncinaTenancyAspNetCore(options =>
        {
            options.HeaderResolver.HeaderName = "X-Custom-Tenant";
            options.SubdomainResolver.Enabled = true;
            options.SubdomainResolver.BaseDomain = "myapp.com";
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<TenancyAspNetCoreOptions>>().Value;

        // Assert
        options.HeaderResolver.HeaderName.ShouldBe("X-Custom-Tenant");
        options.SubdomainResolver.Enabled.ShouldBeTrue();
        options.SubdomainResolver.BaseDomain.ShouldBe("myapp.com");
    }

    [Fact]
    public void AddEncinaTenancyAspNetCore_CalledMultipleTimes_DoesNotDuplicateResolvers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncinaTenancy();

        // Act
        services.AddEncinaTenancyAspNetCore();
        services.AddEncinaTenancyAspNetCore();

        var provider = services.BuildServiceProvider();
        var resolvers = provider.GetServices<ITenantResolver>().ToList();

        // Assert - should have exactly 4 built-in resolvers (not duplicated)
        resolvers.Count(r => r is HeaderTenantResolver).ShouldBe(1);
        resolvers.Count(r => r is ClaimTenantResolver).ShouldBe(1);
        resolvers.Count(r => r is RouteTenantResolver).ShouldBe(1);
        resolvers.Count(r => r is SubdomainTenantResolver).ShouldBe(1);
    }

    [Fact]
    public void AddTenantResolver_Generic_AddsCustomResolver()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncinaTenancy();
        services.AddEncinaTenancyAspNetCore();

        // Act
        services.AddTenantResolver<CustomTestResolver>();
        var provider = services.BuildServiceProvider();

        // Assert
        var resolvers = provider.GetServices<ITenantResolver>().ToList();
        resolvers.ShouldContain(r => r is CustomTestResolver);
    }

    [Fact]
    public void AddTenantResolver_Instance_AddsResolver()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncinaTenancy();
        services.AddEncinaTenancyAspNetCore();
        var customResolver = new CustomTestResolver();

        // Act
        services.AddTenantResolver(customResolver);
        var provider = services.BuildServiceProvider();

        // Assert
        var resolvers = provider.GetServices<ITenantResolver>().ToList();
        resolvers.ShouldContain(customResolver);
    }

    [Fact]
    public void AddTenantResolver_Factory_AddsResolver()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncinaTenancy();
        services.AddEncinaTenancyAspNetCore();

        // Act
        services.AddTenantResolver(_ => new CustomTestResolver());
        var provider = services.BuildServiceProvider();

        // Assert
        var resolvers = provider.GetServices<ITenantResolver>().ToList();
        resolvers.ShouldContain(r => r is CustomTestResolver);
    }

    [Fact]
    public void AddTenantResolver_NullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddTenantResolver<CustomTestResolver>());
    }

    [Fact]
    public void AddTenantResolver_NullInstance_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddTenantResolver((ITenantResolver)null!));
    }

    [Fact]
    public void AddTenantResolver_NullFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddTenantResolver((Func<IServiceProvider, ITenantResolver>)null!));
    }

    private sealed class CustomTestResolver : ITenantResolver
    {
        public int Priority => 0;

        public ValueTask<string?> ResolveAsync(
            Microsoft.AspNetCore.Http.HttpContext context,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult<string?>(null);
        }
    }
}
