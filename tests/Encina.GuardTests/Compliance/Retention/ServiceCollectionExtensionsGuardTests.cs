using Encina.Compliance.Retention;

using Microsoft.Extensions.DependencyInjection;

namespace Encina.GuardTests.Compliance.Retention;

/// <summary>
/// Guard tests for <see cref="ServiceCollectionExtensions"/> null parameter handling.
/// </summary>
public sealed class ServiceCollectionExtensionsGuardTests
{
    [Fact]
    public void AddEncinaRetention_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddEncinaRetention();

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddEncinaRetention_NullServicesWithConfigure_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddEncinaRetention(options =>
        {
            options.EnforcementMode = RetentionEnforcementMode.Block;
        });

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddEncinaRetention_ValidServices_DoesNotThrow()
    {
        var services = new ServiceCollection();

        var act = () => services.AddEncinaRetention();

        Should.NotThrow(act);
    }

    [Fact]
    public void AddEncinaRetention_ValidServicesWithConfigure_DoesNotThrow()
    {
        var services = new ServiceCollection();

        var act = () => services.AddEncinaRetention(options =>
        {
            options.EnforcementMode = RetentionEnforcementMode.Block;
        });

        Should.NotThrow(act);
    }
}
