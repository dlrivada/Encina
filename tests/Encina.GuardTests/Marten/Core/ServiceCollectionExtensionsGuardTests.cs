using Encina.Marten;

namespace Encina.GuardTests.Marten.Core;

public class ServiceCollectionExtensionsGuardTests
{
    [Fact]
    public void AddEncinaMarten_NullServices_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEncinaMarten(_ => { }));

    [Fact]
    public void AddEncinaMarten_NullConfigure_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new ServiceCollection().AddEncinaMarten(null!));

    [Fact]
    public void AddAggregateRepository_NullServices_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddAggregateRepository<TestAgg>());

    public class TestAgg : global::Encina.DomainModeling.AggregateBase { protected override void Apply(object domainEvent) { } }
}
