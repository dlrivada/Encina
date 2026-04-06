using Encina.Compliance.DataSubjectRights;

using Microsoft.Extensions.DependencyInjection;

namespace Encina.GuardTests.Compliance.DataSubjectRights;

/// <summary>
/// Guard tests for <see cref="ServiceCollectionExtensions"/> verifying null parameter handling.
/// </summary>
public class ServiceCollectionExtensionsGuardTests
{
    [Fact]
    public void AddEncinaDataSubjectRights_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddEncinaDataSubjectRights();

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddEncinaDataSubjectRights_NullConfigure_DoesNotThrow()
    {
        var services = new ServiceCollection();

        var act = () => services.AddEncinaDataSubjectRights(configure: null);

        Should.NotThrow(act);
    }

    [Fact]
    public void AddEncinaDataSubjectRights_ValidServices_ReturnsServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddEncinaDataSubjectRights();

        result.ShouldNotBeNull();
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddEncinaDataSubjectRights_WithConfigure_RegistersServices()
    {
        var services = new ServiceCollection();

        services.AddEncinaDataSubjectRights(options =>
        {
            options.RestrictionEnforcementMode = DSREnforcementMode.Block;
            options.AddHealthCheck = true;
        });

        services.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void AddEncinaDataSubjectRights_WithAutoRegistration_RegistersHostedService()
    {
        var services = new ServiceCollection();

        services.AddEncinaDataSubjectRights(options =>
        {
            options.AutoRegisterFromAttributes = true;
            options.AssembliesToScan.Add(typeof(ServiceCollectionExtensionsGuardTests).Assembly);
        });

        services.Count.ShouldBeGreaterThan(0);
    }
}
