using Encina.Dapper.PostgreSQL.Tenancy;
using Encina.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.GuardTests.Dapper.PostgreSQL.Tenancy;

/// <summary>
/// Guard tests for <see cref="TenancyServiceCollectionExtensions"/> to verify null parameter handling.
/// </summary>
[Trait("Category", "Guard")]
public class TenancyServiceCollectionExtensionsGuardTests
{
    private sealed class GuardEntity
    {
        public Guid Id { get; set; }
    }

    [Fact]
    public void AddEncinaDapperWithTenancy_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddEncinaDapperWithTenancy(_ => { });

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddEncinaDapperWithTenancy_NullConfigure_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        Action<MessagingConfiguration> configure = null!;

        var act = () => services.AddEncinaDapperWithTenancy(configure);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("configure");
    }

    [Fact]
    public void AddEncinaDapperWithTenancy_NullConfigureTenancy_ReturnsServices()
    {
        var services = new ServiceCollection();

        var result = services.AddEncinaDapperWithTenancy(_ => { }, null);

        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddTenantAwareRepository_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddTenantAwareRepository<GuardEntity, Guid>(_ => { });

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddTenantAwareRepository_NullConfigure_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        Action<TenantEntityMappingBuilder<GuardEntity, Guid>> configure = null!;

        var act = () => services.AddTenantAwareRepository(configure);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("configure");
    }

    [Fact]
    public void AddTenantAwareReadRepository_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddTenantAwareReadRepository<GuardEntity, Guid>(_ => { });

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddTenantAwareReadRepository_NullConfigure_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        Action<TenantEntityMappingBuilder<GuardEntity, Guid>> configure = null!;

        var act = () => services.AddTenantAwareReadRepository(configure);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("configure");
    }
}
