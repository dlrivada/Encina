using Encina.Marten;
using Encina.Marten.Versioning;
using Marten;

namespace Encina.GuardTests.Marten.Core;

public class ConfigureMartenGuardTests
{
    [Fact]
    public void ConfigureMartenEventVersioning_Configure_NullOptions_Throws()
    {
        var registry = new EventUpcasterRegistry();
        var svc = new ConfigureMartenEventVersioning(
            registry, Options.Create(new EncinaMartenOptions()), [], NullLogger<ConfigureMartenEventVersioning>.Instance);
        Should.Throw<ArgumentNullException>(() => svc.Configure(null!));
    }

    [Fact]
    public void ConfigureMartenEventMetadata_Configure_NullOptions_Throws()
    {
        var svc = new ConfigureMartenEventMetadata(
            Options.Create(new EncinaMartenOptions()), NullLogger<ConfigureMartenEventMetadata>.Instance);
        Should.Throw<ArgumentNullException>(() => svc.Configure(null!));
    }
}
