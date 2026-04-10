using Encina.Compliance.LawfulBasis;

namespace Encina.GuardTests.Compliance.LawfulBasis;

/// <summary>
/// Guard tests for <see cref="ServiceCollectionExtensions"/> and <see cref="LawfulBasisMartenExtensions"/>.
/// </summary>
public class LawfulBasisServiceCollectionExtensionsGuardTests
{
    [Fact]
    public void AddEncinaLawfulBasis_NullServices_Throws()
    {
        IServiceCollection? services = null;
        Should.Throw<ArgumentNullException>(() =>
            services!.AddEncinaLawfulBasis());
    }

    [Fact]
    public void AddLawfulBasisAggregates_NullServices_Throws()
    {
        IServiceCollection? services = null;
        Should.Throw<ArgumentNullException>(() =>
            services!.AddLawfulBasisAggregates());
    }
}
