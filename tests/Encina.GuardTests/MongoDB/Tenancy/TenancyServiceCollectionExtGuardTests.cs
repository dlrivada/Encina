using Encina.MongoDB.Tenancy;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.GuardTests.MongoDB.Tenancy;

public class TenancyServiceCollectionExtGuardTests
{
    #region AddEncinaMongoDBWithTenancy

    [Fact]
    public void AddEncinaMongoDBWithTenancy_NullServices_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            TenancyServiceCollectionExtensions.AddEncinaMongoDBWithTenancy(null!, _ => { }));

    [Fact]
    public void AddEncinaMongoDBWithTenancy_NullConfigure_Throws()
    {
        var services = new ServiceCollection();
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaMongoDBWithTenancy(null!));
    }

    #endregion

    #region AddTenantAwareRepository

    [Fact]
    public void AddTenantAwareRepository_NullServices_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            TenancyServiceCollectionExtensions.AddTenantAwareRepository<TestEntity, Guid>(null!, _ => { }));

    [Fact]
    public void AddTenantAwareRepository_NullConfigure_Throws()
    {
        var services = new ServiceCollection();
        Should.Throw<ArgumentNullException>(() =>
            services.AddTenantAwareRepository<TestEntity, Guid>(null!));
    }

    #endregion

    #region AddTenantAwareReadRepository

    [Fact]
    public void AddTenantAwareReadRepository_NullServices_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            TenancyServiceCollectionExtensions.AddTenantAwareReadRepository<TestEntity, Guid>(null!, _ => { }));

    [Fact]
    public void AddTenantAwareReadRepository_NullConfigure_Throws()
    {
        var services = new ServiceCollection();
        Should.Throw<ArgumentNullException>(() =>
            services.AddTenantAwareReadRepository<TestEntity, Guid>(null!));
    }

    #endregion

    public class TestEntity { public Guid Id { get; set; } public string TenantId { get; set; } = ""; }
}
