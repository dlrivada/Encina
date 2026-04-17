using Encina.Security.Audit;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Encina.GuardTests.Security.Audit;

/// <summary>
/// Guard clause tests for <see cref="ServiceCollectionExtensions"/>.
/// Verifies that null arguments to DI registration methods are properly rejected.
/// </summary>
public class ServiceCollectionExtensionsGuardTests
{
    [Fact]
    public void AddEncinaAudit_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddEncinaAudit();

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddEncinaAudit_NullServicesWithConfigure_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddEncinaAudit(opts => opts.AuditAllCommands = false);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddEncinaReadAuditing_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddEncinaReadAuditing();

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddEncinaReadAuditing_NullServicesWithConfigure_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddEncinaReadAuditing(opts => opts.RequirePurpose = true);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddEncinaAudit_ValidServices_DoesNotThrow()
    {
        var services = new ServiceCollection();

        var act = () => services.AddEncinaAudit();

        Should.NotThrow(act);
    }

    [Fact]
    public void AddEncinaReadAuditing_ValidServices_DoesNotThrow()
    {
        var services = new ServiceCollection();

        var act = () => services.AddEncinaReadAuditing();

        Should.NotThrow(act);
    }

    [Fact]
    public void AddEncinaAudit_WithNullConfigure_DoesNotThrow()
    {
        var services = new ServiceCollection();

        var act = () => services.AddEncinaAudit(configure: null);

        Should.NotThrow(act);
    }

    [Fact]
    public void AddEncinaReadAuditing_WithNullConfigure_DoesNotThrow()
    {
        var services = new ServiceCollection();

        var act = () => services.AddEncinaReadAuditing(configure: null);

        Should.NotThrow(act);
    }
}
