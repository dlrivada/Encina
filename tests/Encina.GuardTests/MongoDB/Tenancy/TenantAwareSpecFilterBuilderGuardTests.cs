using Encina.DomainModeling;
using Encina.MongoDB.Tenancy;
using Encina.Tenancy;

namespace Encina.GuardTests.MongoDB.Tenancy;

public class TenantAwareSpecFilterBuilderGuardTests
{
    private static readonly ITenantEntityMapping<TestEntity, object> Mapping =
        Substitute.For<ITenantEntityMapping<TestEntity, object>>();
    private static readonly ITenantProvider TenantProvider = Substitute.For<ITenantProvider>();
    private static readonly MongoDbTenancyOptions Options = new();

    #region Constructor

    [Fact]
    public void Ctor_NullMapping_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new TenantAwareSpecificationFilterBuilder<TestEntity>(null!, TenantProvider, Options));

    [Fact]
    public void Ctor_NullTenantProvider_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new TenantAwareSpecificationFilterBuilder<TestEntity>(Mapping, null!, Options));

    [Fact]
    public void Ctor_NullOptions_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new TenantAwareSpecificationFilterBuilder<TestEntity>(Mapping, TenantProvider, null!));

    #endregion

    #region BuildFilter

    [Fact]
    public void BuildFilter_NullSpecification_Throws()
    {
        var builder = new TenantAwareSpecificationFilterBuilder<TestEntity>(Mapping, TenantProvider, Options);
        Should.Throw<ArgumentNullException>(() =>
            builder.BuildFilter(null!));
    }

    #endregion

    public class TestEntity
    {
        public Guid Id { get; set; }
        public string TenantId { get; set; } = "";
    }
}
