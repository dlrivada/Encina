using Encina.Compliance.DataResidency;

using Shouldly;

namespace Encina.GuardTests.Compliance.DataResidency;

/// <summary>
/// Guard clause tests for <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public class ServiceCollectionExtensionsGuardTests
{
    [Fact]
    public void AddEncinaDataResidency_NullServices_ShouldThrowArgumentNullException()
    {
        IServiceCollection? services = null;
        var act = () => services!.AddEncinaDataResidency();
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddEncinaDataResidency_NullServicesWithConfigure_ShouldThrowArgumentNullException()
    {
        IServiceCollection? services = null;
        var act = () => services!.AddEncinaDataResidency(_ => { });
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("services");
    }
}
