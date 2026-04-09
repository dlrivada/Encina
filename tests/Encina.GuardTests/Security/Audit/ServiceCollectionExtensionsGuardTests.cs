using Encina.Security.Audit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

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

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    [Fact]
    public void AddEncinaAudit_NullServicesWithConfigure_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddEncinaAudit(opts => opts.AuditAllCommands = false);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    [Fact]
    public void AddEncinaReadAuditing_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddEncinaReadAuditing();

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    [Fact]
    public void AddEncinaReadAuditing_NullServicesWithConfigure_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddEncinaReadAuditing(opts => opts.RequirePurpose = true);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    [Fact]
    public void AddEncinaAudit_ValidServices_DoesNotThrow()
    {
        var services = new ServiceCollection();

        var act = () => services.AddEncinaAudit();

        act.Should().NotThrow();
    }

    [Fact]
    public void AddEncinaReadAuditing_ValidServices_DoesNotThrow()
    {
        var services = new ServiceCollection();

        var act = () => services.AddEncinaReadAuditing();

        act.Should().NotThrow();
    }

    [Fact]
    public void AddEncinaAudit_WithNullConfigure_DoesNotThrow()
    {
        var services = new ServiceCollection();

        var act = () => services.AddEncinaAudit(configure: null);

        act.Should().NotThrow();
    }

    [Fact]
    public void AddEncinaReadAuditing_WithNullConfigure_DoesNotThrow()
    {
        var services = new ServiceCollection();

        var act = () => services.AddEncinaReadAuditing(configure: null);

        act.Should().NotThrow();
    }
}
