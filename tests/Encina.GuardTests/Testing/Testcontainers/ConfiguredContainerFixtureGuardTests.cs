using Encina.Testing.Testcontainers;

namespace Encina.GuardTests.Testing.Testcontainers;

public class ConfiguredContainerFixtureGuardTests
{
    [Fact]
    public void Constructor_NullContainer_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ConfiguredContainerFixture<DotNet.Testcontainers.Containers.IContainer>(null!));
    }
}
