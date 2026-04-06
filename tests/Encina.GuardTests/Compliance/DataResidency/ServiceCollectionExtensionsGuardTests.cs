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

    [Fact]
    public void AddEncinaDataResidency_ValidServices_ReturnsServiceCollection()
    {
        var services = new ServiceCollection();
        var result = services.AddEncinaDataResidency();
        result.ShouldNotBeNull();
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddEncinaDataResidency_WithConfigure_RegistersServices()
    {
        var services = new ServiceCollection();
        services.AddEncinaDataResidency(options =>
        {
            options.EnforcementMode = DataResidencyEnforcementMode.Block;
            options.AddHealthCheck = true;
        });
        services.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void AddEncinaDataResidency_WithAutoRegistration_RegistersHostedService()
    {
        var services = new ServiceCollection();
        services.AddEncinaDataResidency(options =>
        {
            options.AutoRegisterFromAttributes = true;
            options.AssembliesToScan.Add(typeof(ServiceCollectionExtensionsGuardTests).Assembly);
        });
        services.Count.ShouldBeGreaterThan(0);
    }
}
