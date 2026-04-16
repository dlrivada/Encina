using Encina.Security.Sanitization;
using Encina.Security.Sanitization.Abstractions;
using Encina.Security.Sanitization.Encoders;
using Encina.Security.Sanitization.Health;
using Shouldly;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Encina.UnitTests.Security.Sanitization;

public sealed class SanitizationServiceCollectionExtensionsTests
{
    #region AddEncinaSanitization — Service Registration

    [Fact]
    public void AddEncinaSanitization_RegistersISanitizer()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaSanitization();

        var provider = services.BuildServiceProvider();
        provider.GetService<ISanitizer>().ShouldNotBeNull();
        provider.GetService<ISanitizer>().ShouldBeOfType<DefaultSanitizer>();
    }

    [Fact]
    public void AddEncinaSanitization_RegistersIOutputEncoder()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaSanitization();

        var provider = services.BuildServiceProvider();
        provider.GetService<IOutputEncoder>().ShouldNotBeNull();
        provider.GetService<IOutputEncoder>().ShouldBeOfType<DefaultOutputEncoder>();
    }

    [Fact]
    public void AddEncinaSanitization_RegistersISanitizer_AsSingleton()
    {
        var services = new ServiceCollection();

        services.AddEncinaSanitization();

        var descriptor = services.First(d => d.ServiceType == typeof(ISanitizer));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaSanitization_RegistersIOutputEncoder_AsSingleton()
    {
        var services = new ServiceCollection();

        services.AddEncinaSanitization();

        var descriptor = services.First(d => d.ServiceType == typeof(IOutputEncoder));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaSanitization_RegistersSanitizationOrchestrator_AsScoped()
    {
        var services = new ServiceCollection();

        services.AddEncinaSanitization();

        var descriptor = services.First(d => d.ServiceType == typeof(SanitizationOrchestrator));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddEncinaSanitization_RegistersPipelineBehaviors_AsTransient()
    {
        var services = new ServiceCollection();

        services.AddEncinaSanitization();

        var behaviors = services.Where(d => d.ServiceType == typeof(IPipelineBehavior<,>)).ToList();
        behaviors.Count.ShouldBeGreaterThanOrEqualTo(2);
        behaviors.ShouldAllBe(d => d.Lifetime == ServiceLifetime.Transient);
    }

    #endregion

    #region AddEncinaSanitization — Options Configuration

    [Fact]
    public void AddEncinaSanitization_ConfigureOptions_AppliesConfiguration()
    {
        var services = new ServiceCollection();

        services.AddEncinaSanitization(options =>
        {
            options.SanitizeAllStringInputs = true;
            options.EncodeAllOutputs = true;
            options.EnableTracing = true;
            options.EnableMetrics = true;
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<SanitizationOptions>>().Value;
        options.SanitizeAllStringInputs.ShouldBeTrue();
        options.EncodeAllOutputs.ShouldBeTrue();
        options.EnableTracing.ShouldBeTrue();
        options.EnableMetrics.ShouldBeTrue();
    }

    [Fact]
    public void AddEncinaSanitization_WithoutConfigure_UsesDefaults()
    {
        var services = new ServiceCollection();

        services.AddEncinaSanitization();

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<SanitizationOptions>>().Value;
        options.SanitizeAllStringInputs.ShouldBeFalse();
        options.EncodeAllOutputs.ShouldBeFalse();
        options.AddHealthCheck.ShouldBeFalse();
        options.EnableTracing.ShouldBeFalse();
        options.EnableMetrics.ShouldBeFalse();
    }

    #endregion

    #region AddEncinaSanitization — TryAdd Override

    [Fact]
    public void AddEncinaSanitization_TryAdd_AllowsCustomSanitizer()
    {
        var services = new ServiceCollection();
        var customSanitizer = Substitute.For<ISanitizer>();

        // Register custom BEFORE AddEncinaSanitization
        services.AddSingleton(customSanitizer);
        services.AddEncinaSanitization();

        var provider = services.BuildServiceProvider();
        var resolved = provider.GetRequiredService<ISanitizer>();
        resolved.ShouldBeSameAs(customSanitizer);
    }

    [Fact]
    public void AddEncinaSanitization_TryAdd_AllowsCustomEncoder()
    {
        var services = new ServiceCollection();
        var customEncoder = Substitute.For<IOutputEncoder>();

        services.AddSingleton(customEncoder);
        services.AddEncinaSanitization();

        var provider = services.BuildServiceProvider();
        var resolved = provider.GetRequiredService<IOutputEncoder>();
        resolved.ShouldBeSameAs(customEncoder);
    }

    #endregion

    #region AddEncinaSanitization — Chaining & Guard

    [Fact]
    public void AddEncinaSanitization_ReturnsServiceCollection_ForChaining()
    {
        var services = new ServiceCollection();

        var result = services.AddEncinaSanitization();

        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddEncinaSanitization_CalledMultipleTimes_DoesNotThrow()
    {
        var services = new ServiceCollection();

        var act = () =>
        {
            services.AddEncinaSanitization();
            services.AddEncinaSanitization();
        };

        Should.NotThrow(act);
    }

    [Fact]
    public void AddEncinaSanitization_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddEncinaSanitization();

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("services");
    }

    #endregion

    #region Health Check Registration

    [Fact]
    public void AddEncinaSanitization_HealthCheckEnabled_RegistersHealthCheck()
    {
        var services = new ServiceCollection();

        services.AddEncinaSanitization(options => options.AddHealthCheck = true);

        var provider = services.BuildServiceProvider();
        var healthCheckOptions = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        healthCheckOptions.Registrations.ShouldContain(r => r.Name == SanitizationHealthCheck.DefaultName);
    }

    [Fact]
    public void AddEncinaSanitization_HealthCheckDisabled_DoesNotRegisterHealthCheck()
    {
        var services = new ServiceCollection();

        services.AddEncinaSanitization(options => options.AddHealthCheck = false);

        var hasHealthChecks = services.Any(d => d.ServiceType == typeof(HealthCheckService));
        hasHealthChecks.ShouldBeFalse();
    }

    [Fact]
    public void AddEncinaSanitization_DefaultOptions_DoesNotRegisterHealthCheck()
    {
        var services = new ServiceCollection();

        services.AddEncinaSanitization();

        var hasHealthChecks = services.Any(d => d.ServiceType == typeof(HealthCheckService));
        hasHealthChecks.ShouldBeFalse();
    }

    #endregion
}
