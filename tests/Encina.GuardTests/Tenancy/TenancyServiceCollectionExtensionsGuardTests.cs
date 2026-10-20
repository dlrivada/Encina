using Encina.Tenancy;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Encina.GuardTests.Tenancy;

/// <summary>
/// Guard tests of <c>AddEncinaTenancy</c>: a <c>null</c> collection is rejected with its parameter's
/// name, and a valid call registers the tenancy services, the multi-tenancy marker included (#751).
/// </summary>
public sealed class TenancyServiceCollectionExtensionsGuardTests
{
    [Fact]
    public void AddEncinaTenancy_NullServices_Throws() =>
        Should.Throw<ArgumentNullException>(() => ((IServiceCollection)null!).AddEncinaTenancy())
            .ParamName.ShouldBe("services");

    [Fact]
    public void AddEncinaTenancy_ValidServices_RegistersTheTenancyServicesAndTheMarker()
    {
        var services = new ServiceCollection();

        services.AddEncinaTenancy(options => options.Tenants.Add(new TenantInfo("t1", "Tenant 1", TenantIsolationStrategy.SharedSchema)));
        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<ITenantStore>().ShouldNotBeNull();
        provider.GetRequiredService<InMemoryTenantStore>().ShouldNotBeNull();
        provider.GetRequiredService<MultiTenancyMarker>().ShouldNotBeNull();
        provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<TenantConnectionOptions>>().Value.ShouldNotBeNull();
        provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<TenancyOptions>>().Value.Tenants.ShouldHaveSingleItem();
    }
}
