using Encina.Compliance.GDPR;
using Encina.Compliance.LawfulBasis;
using Encina.Compliance.LawfulBasis.Abstractions;
using Encina.Compliance.LawfulBasis.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

using GDPRLawfulBasis = global::Encina.Compliance.GDPR.LawfulBasis;

namespace Encina.UnitTests.Compliance.LawfulBasisModule;

/// <summary>
/// Unit tests for <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEncinaLawfulBasis_NullServices_Throws()
    {
        IServiceCollection? services = null;
        Should.Throw<ArgumentNullException>(() => services!.AddEncinaLawfulBasis());
    }

    [Fact]
    public void AddEncinaLawfulBasis_NoConfigure_RegistersCoreServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaLawfulBasis();

        // Core services registered
        services.ShouldContain(sd => sd.ServiceType == typeof(ILawfulBasisService));
        services.ShouldContain(sd => sd.ServiceType == typeof(ILawfulBasisProvider));
        services.ShouldContain(sd => sd.ServiceType == typeof(ILawfulBasisSubjectIdExtractor));
        services.ShouldContain(sd => sd.ServiceType == typeof(IPipelineBehavior<,>));
    }

    [Fact]
    public void AddEncinaLawfulBasis_WithConfigure_AppliesConfiguration()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaLawfulBasis(options =>
        {
            options.EnforcementMode = LawfulBasisEnforcementMode.Warn;
            options.AutoRegisterFromAttributes = false;
        });

        var provider = services.BuildServiceProvider();
        var opts = provider.GetRequiredService<IOptions<LawfulBasisOptions>>();
        opts.Value.EnforcementMode.ShouldBe(LawfulBasisEnforcementMode.Warn);
    }

    [Fact]
    public void AddEncinaLawfulBasis_RegistersOptionsValidator()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaLawfulBasis();

        services.ShouldContain(sd => sd.ServiceType == typeof(IValidateOptions<LawfulBasisOptions>));
    }

    [Fact]
    public void AddEncinaLawfulBasis_RegistersTimeProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaLawfulBasis();

        services.ShouldContain(sd => sd.ServiceType == typeof(TimeProvider));
    }

    [Fact]
    public void AddEncinaLawfulBasis_WithHealthCheckEnabled_RegistersHealthCheck()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaLawfulBasis(options =>
        {
            options.AddHealthCheck = true;
            options.AutoRegisterFromAttributes = false;
        });

        var provider = services.BuildServiceProvider();
        var hcService = provider.GetService<HealthCheckService>();
        // HealthCheckService should be registered when AddHealthCheck = true
        hcService.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaLawfulBasis_WithDefaultBases_RegistersHostedService()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaLawfulBasis(options =>
        {
            options.AutoRegisterFromAttributes = false;
            options.DefaultBasis<object>(GDPRLawfulBasis.Contract);
        });

        services.ShouldContain(sd => sd.ServiceType == typeof(IHostedService));
    }

    [Fact]
    public void AddEncinaLawfulBasis_WithAutoRegister_RegistersHostedService()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaLawfulBasis(options =>
        {
            options.AutoRegisterFromAttributes = true;
            options.ScanAssemblyContaining<ServiceCollectionExtensionsTests>();
        });

        services.ShouldContain(sd => sd.ServiceType == typeof(IHostedService));
    }

    [Fact]
    public void AddEncinaLawfulBasis_WithoutAutoRegistration_DoesNotRegisterHostedService()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaLawfulBasis(options =>
        {
            options.AutoRegisterFromAttributes = false;
            options.DefaultBases.Clear();
        });

        services.Where(sd => sd.ImplementationType != null
                && sd.ImplementationType.Name.Contains("AutoRegistration", StringComparison.Ordinal))
            .ShouldBeEmpty();
    }

    [Fact]
    public void AddEncinaLawfulBasis_PipelineBehaviorRegisteredAsTransient()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaLawfulBasis();

        var descriptor = services.First(sd =>
            sd.ServiceType == typeof(IPipelineBehavior<,>));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Transient);
    }
}
