using Encina.Compliance.Anonymization;

namespace Encina.GuardTests.Compliance.Anonymization;

/// <summary>
/// Guard tests for <see cref="ServiceCollectionExtensions"/> to verify null parameter handling.
/// </summary>
public class ServiceCollectionExtensionsGuardTests
{
    #region AddEncinaAnonymization Guards

    [Fact]
    public void AddEncinaAnonymization_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddEncinaAnonymization();

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddEncinaAnonymization_NullServicesWithConfigure_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddEncinaAnonymization(opts => opts.TrackAuditTrail = false);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddEncinaAnonymization_ValidServices_DoesNotThrow()
    {
        var services = new ServiceCollection();

        var result = services.AddEncinaAnonymization();

        result.ShouldNotBeNull();
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddEncinaAnonymization_WithConfigureAction_DoesNotThrow()
    {
        var services = new ServiceCollection();

        var result = services.AddEncinaAnonymization(opts =>
        {
            opts.EnforcementMode = AnonymizationEnforcementMode.Warn;
            opts.TrackAuditTrail = false;
        });

        result.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaAnonymization_NullConfigureAction_DoesNotThrow()
    {
        var services = new ServiceCollection();

        var result = services.AddEncinaAnonymization(null);

        result.ShouldNotBeNull();
    }

    #endregion
}
