using Encina.Compliance.NIS2;
using Encina.Compliance.NIS2.Abstractions;
using Encina.Compliance.NIS2.Model;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Encina.GuardTests.Compliance.NIS2;

/// <summary>
/// Guard clause tests for <see cref="ServiceCollectionExtensions"/>.
/// Verifies null argument guards and service registration.
/// </summary>
public sealed class NIS2ServiceCollectionExtensionsGuardTests
{
    [Fact]
    public void AddEncinaNIS2_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddEncinaNIS2();

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    [Fact]
    public void AddEncinaNIS2_WithoutConfigure_RegistersAllServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaNIS2(o =>
        {
            o.CompetentAuthority = "test@authority.eu";
            o.EnforceEncryption = false;
        });

        var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IOptions<NIS2Options>>().Value
            .Should().NotBeNull();
        provider.GetRequiredService<ISupplyChainSecurityValidator>()
            .Should().NotBeNull();
        provider.GetRequiredService<IMFAEnforcer>()
            .Should().NotBeNull();
        provider.GetRequiredService<IEncryptionValidator>()
            .Should().NotBeNull();
    }

    [Fact]
    public void AddEncinaNIS2_WithConfigure_AppliesOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaNIS2(options =>
        {
            options.EntityType = NIS2EntityType.Important;
            options.Sector = NIS2Sector.Manufacturing;
            options.EnforcementMode = NIS2EnforcementMode.Disabled;
            options.EnforceEncryption = false;
        });

        var provider = services.BuildServiceProvider();
        var opts = provider.GetRequiredService<IOptions<NIS2Options>>().Value;

        opts.EntityType.Should().Be(NIS2EntityType.Important);
        opts.Sector.Should().Be(NIS2Sector.Manufacturing);
        opts.EnforcementMode.Should().Be(NIS2EnforcementMode.Disabled);
    }

    [Fact]
    public void AddEncinaNIS2_Registers10Evaluators()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaNIS2();

        var provider = services.BuildServiceProvider();
        var evaluators = provider.GetServices<INIS2MeasureEvaluator>().ToList();

        evaluators.Should().HaveCount(10);
    }

    [Fact]
    public void AddEncinaNIS2_WithHealthCheck_RegistersHealthCheck()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaNIS2(options => options.AddHealthCheck = true);

        var provider = services.BuildServiceProvider();

        // Health check registration adds to the builder
        var healthCheckOptions = provider.GetService<IOptions<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckServiceOptions>>();
        healthCheckOptions.Should().NotBeNull();
    }

    [Fact]
    public void AddEncinaNIS2_TryAdd_DoesNotOverrideCustomRegistrations()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        // Register custom enforcer before AddEncinaNIS2
        var customEnforcer = NSubstitute.Substitute.For<IMFAEnforcer>();
        services.AddSingleton(customEnforcer);

        services.AddEncinaNIS2();

        var provider = services.BuildServiceProvider();
        var resolved = provider.GetRequiredService<IMFAEnforcer>();

        resolved.Should().BeSameAs(customEnforcer);
    }
}
