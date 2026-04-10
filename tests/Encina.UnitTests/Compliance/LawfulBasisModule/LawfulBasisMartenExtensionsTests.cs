using Encina.Compliance.LawfulBasis;
using Encina.Compliance.LawfulBasis.Aggregates;
using Encina.Marten;

namespace Encina.UnitTests.Compliance.LawfulBasisModule;

/// <summary>
/// Unit tests for <see cref="LawfulBasisMartenExtensions"/>.
/// </summary>
public class LawfulBasisMartenExtensionsTests
{
    [Fact]
    public void AddLawfulBasisAggregates_NullServices_Throws()
    {
        IServiceCollection? services = null;
        Should.Throw<ArgumentNullException>(() => services!.AddLawfulBasisAggregates());
    }

    [Fact]
    public void AddLawfulBasisAggregates_RegistersBothAggregateRepositories()
    {
        var services = new ServiceCollection();

        var result = services.AddLawfulBasisAggregates();

        result.ShouldBeSameAs(services);
        services.ShouldContain(sd => sd.ServiceType == typeof(IAggregateRepository<LawfulBasisAggregate>));
        services.ShouldContain(sd => sd.ServiceType == typeof(IAggregateRepository<LIAAggregate>));
    }

    [Fact]
    public void AddLawfulBasisAggregates_ReturnsSameServiceCollectionForChaining()
    {
        var services = new ServiceCollection();
        var returned = services.AddLawfulBasisAggregates();
        returned.ShouldBeSameAs(services);
    }
}
