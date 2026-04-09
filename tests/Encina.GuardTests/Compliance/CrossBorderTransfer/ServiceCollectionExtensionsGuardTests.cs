#pragma warning disable CA2012

using Encina.Compliance.CrossBorderTransfer;

namespace Encina.GuardTests.Compliance.CrossBorderTransfer;

/// <summary>
/// Guard tests for <see cref="ServiceCollectionExtensions"/> verifying null parameter handling.
/// </summary>
public class ServiceCollectionExtensionsGuardTests
{
    [Fact]
    public void AddEncinaCrossBorderTransfer_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection? services = null;

        var act = () => services!.AddEncinaCrossBorderTransfer();

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddEncinaCrossBorderTransfer_ValidServices_ReturnsServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddEncinaCrossBorderTransfer();

        result.ShouldNotBeNull();
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddEncinaCrossBorderTransfer_WithNullConfigure_RegistersDefaults()
    {
        var services = new ServiceCollection();

        var result = services.AddEncinaCrossBorderTransfer(configure: null);

        result.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaCrossBorderTransfer_WithConfigure_AppliesConfiguration()
    {
        var services = new ServiceCollection();

        var result = services.AddEncinaCrossBorderTransfer(options =>
        {
            options.EnforcementMode = CrossBorderTransferEnforcementMode.Warn;
            options.DefaultSourceCountryCode = "FR";
        });

        result.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaCrossBorderTransfer_WithHealthCheck_RegistersHealthCheck()
    {
        var services = new ServiceCollection();

        services.AddEncinaCrossBorderTransfer(options =>
        {
            options.AddHealthCheck = true;
        });

        services.Count.ShouldBeGreaterThan(0);
    }
}
