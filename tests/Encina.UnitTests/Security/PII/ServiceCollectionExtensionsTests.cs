using Encina.Security.Audit;
using Encina.Security.PII;
using Encina.Security.PII.Abstractions;
using Encina.Security.PII.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Encina.UnitTests.Security.PII;

public sealed class ServiceCollectionExtensionsTests
{
    #region Service Registration

    [Fact]
    public void AddEncinaPII_RegistersIPIIMasker()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaPII();

        var provider = services.BuildServiceProvider();
        provider.GetService<IPIIMasker>().ShouldNotBeNull();
        provider.GetService<IPIIMasker>().ShouldBeOfType<PIIMasker>();
    }

    [Fact]
    public void AddEncinaPII_RegistersAuditPiiMasker()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaPII();

        var provider = services.BuildServiceProvider();
        var piiMasker = provider.GetService<IPiiMasker>();
        piiMasker.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaPII_IPiiMasker_IsSameInstanceAsIPIIMasker()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaPII();

        var provider = services.BuildServiceProvider();
        var masker = provider.GetRequiredService<IPIIMasker>();
        var auditMasker = provider.GetRequiredService<IPiiMasker>();

        auditMasker.ShouldBeSameAs(masker);
    }

    [Fact]
    public void AddEncinaPII_RegistersPipelineBehavior()
    {
        var services = new ServiceCollection();

        services.AddEncinaPII();

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IPipelineBehavior<,>));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Transient);
    }

    [Fact]
    public void AddEncinaPII_RegistersIPIIMasker_AsSingleton()
    {
        var services = new ServiceCollection();

        services.AddEncinaPII();

        var descriptor = services.First(d => d.ServiceType == typeof(IPIIMasker));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    #endregion

    #region Health Check

    [Fact]
    public void AddEncinaPII_WithHealthCheck_RegistersHealthCheck()
    {
        var services = new ServiceCollection();

        services.AddEncinaPII(o => o.AddHealthCheck = true);

        var provider = services.BuildServiceProvider();
        var healthCheckOptions = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        var registration = healthCheckOptions.Registrations
            .FirstOrDefault(r => r.Name == PIIHealthCheck.DefaultName);
        registration.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaPII_WithoutHealthCheck_DoesNotRegisterHealthCheck()
    {
        var services = new ServiceCollection();

        services.AddEncinaPII(o => o.AddHealthCheck = false);

        var hasHealthChecks = services.Any(d => d.ServiceType == typeof(HealthCheckService));
        hasHealthChecks.ShouldBeFalse();
    }

    [Fact]
    public void AddEncinaPII_DefaultOptions_DoesNotRegisterHealthCheck()
    {
        var services = new ServiceCollection();

        services.AddEncinaPII();

        var hasHealthChecks = services.Any(d => d.ServiceType == typeof(HealthCheckService));
        hasHealthChecks.ShouldBeFalse();
    }

    #endregion

    #region Options Configuration

    [Fact]
    public void AddEncinaPII_WithConfigure_AppliesOptions()
    {
        var services = new ServiceCollection();

        services.AddEncinaPII(o =>
        {
            o.MaskInResponses = false;
            o.MaskInLogs = false;
            o.MaskInAuditTrails = false;
            o.DefaultMode = MaskingMode.Full;
            o.EnableTracing = true;
            o.EnableMetrics = true;
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<PIIOptions>>().Value;
        options.MaskInResponses.ShouldBeFalse();
        options.MaskInLogs.ShouldBeFalse();
        options.MaskInAuditTrails.ShouldBeFalse();
        options.DefaultMode.ShouldBe(MaskingMode.Full);
        options.EnableTracing.ShouldBeTrue();
        options.EnableMetrics.ShouldBeTrue();
    }

    [Fact]
    public void AddEncinaPII_WithoutConfigure_UsesDefaults()
    {
        var services = new ServiceCollection();

        services.AddEncinaPII();

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<PIIOptions>>().Value;
        options.MaskInResponses.ShouldBeTrue();
        options.MaskInLogs.ShouldBeTrue();
        options.MaskInAuditTrails.ShouldBeTrue();
        options.DefaultMode.ShouldBe(MaskingMode.Partial);
        options.EnableTracing.ShouldBeFalse();
        options.EnableMetrics.ShouldBeFalse();
        options.AddHealthCheck.ShouldBeFalse();
    }

    #endregion

    #region Guard Clauses & Chaining

    [Fact]
    public void AddEncinaPII_NullServices_ThrowsArgumentNull()
    {
        IServiceCollection services = null!;

        Should.Throw<ArgumentNullException>(() => services.AddEncinaPII());
    }

    [Fact]
    public void AddEncinaPII_ReturnsServiceCollection_ForChaining()
    {
        var services = new ServiceCollection();

        var result = services.AddEncinaPII();

        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddEncinaPII_CalledMultipleTimes_DoesNotThrow()
    {
        var services = new ServiceCollection();

        Should.NotThrow(() =>
        {
            services.AddEncinaPII();
            services.AddEncinaPII();
        });
    }

    #endregion
}
