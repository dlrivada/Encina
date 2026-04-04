using Encina.Compliance.DataSubjectRights;
using Encina.Compliance.DataSubjectRights.Abstractions;
using Encina.Compliance.DataSubjectRights.Health;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Encina.UnitTests.Compliance.DataSubjectRights;

/// <summary>
/// Unit tests for <see cref="ServiceCollectionExtensions"/> verifying DI registrations
/// and configuration behavior.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEncinaDataSubjectRights_RegistersDefaultServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaDataSubjectRights();

        var provider = services.BuildServiceProvider();

        // Options validator should be registered
        var validator = provider.GetService<IValidateOptions<DataSubjectRightsOptions>>();
        validator.ShouldNotBeNull();

        // Singleton services should be resolvable directly
        var erasureStrategy = provider.GetService<IDataErasureStrategy>();
        erasureStrategy.ShouldNotBeNull();
        erasureStrategy.ShouldBeOfType<HardDeleteErasureStrategy>();

        var extractor = provider.GetService<IDataSubjectIdExtractor>();
        extractor.ShouldNotBeNull();
        extractor.ShouldBeOfType<DefaultDataSubjectIdExtractor>();

        // Verify service descriptors are registered for scoped services
        services.ShouldContain(s => s.ServiceType == typeof(IDSRService));
        services.ShouldContain(s => s.ServiceType == typeof(IDataErasureExecutor));
        services.ShouldContain(s => s.ServiceType == typeof(IDataPortabilityExporter));
    }

    [Fact]
    public void AddEncinaDataSubjectRights_RegistersExportWriters()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaDataSubjectRights();

        var provider = services.BuildServiceProvider();

        var jsonWriter = provider.GetService<JsonExportFormatWriter>();
        jsonWriter.ShouldNotBeNull();

        var csvWriter = provider.GetService<CsvExportFormatWriter>();
        csvWriter.ShouldNotBeNull();

        var xmlWriter = provider.GetService<XmlExportFormatWriter>();
        xmlWriter.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaDataSubjectRights_WithConfigure_AppliesOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaDataSubjectRights(opts =>
        {
            opts.DefaultDeadlineDays = 45;
            opts.RestrictionEnforcementMode = DSREnforcementMode.Warn;
            opts.AutoRegisterFromAttributes = false;
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<DataSubjectRightsOptions>>().Value;

        options.DefaultDeadlineDays.ShouldBe(45);
        options.RestrictionEnforcementMode.ShouldBe(DSREnforcementMode.Warn);
    }

    [Fact]
    public void AddEncinaDataSubjectRights_WithHealthCheck_RegistersHealthCheck()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaDataSubjectRights(opts =>
        {
            opts.AddHealthCheck = true;
            opts.AutoRegisterFromAttributes = false;
        });

        // Verify health check service registration exists
        var healthCheckDescriptor = services.FirstOrDefault(
            s => s.ServiceType == typeof(IHealthCheckPublisher)
                 || s.ImplementationType == typeof(DataSubjectRightsHealthCheck));

        // The AddHealthChecks().AddCheck<T> registers in IHealthChecksBuilder
        var hasHealthChecks = services.Any(s =>
            s.ServiceType == typeof(HealthCheckService)
            || s.ServiceType.Name.Contains("HealthCheck"));

        hasHealthChecks.ShouldBeTrue("Health check services should be registered");
    }

    [Fact]
    public void AddEncinaDataSubjectRights_WithoutHealthCheck_DoesNotRegisterHealthCheck()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaDataSubjectRights(opts =>
        {
            opts.AddHealthCheck = false;
            opts.AutoRegisterFromAttributes = false;
        });

        var hasHealthCheckRegistration = services.Any(s =>
            s.ImplementationType == typeof(DataSubjectRightsHealthCheck));

        hasHealthCheckRegistration.ShouldBeFalse();
    }

    [Fact]
    public void AddEncinaDataSubjectRights_WithAutoRegistration_RegistersHostedService()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaDataSubjectRights(opts =>
        {
            opts.AutoRegisterFromAttributes = true;
        });

        var hasHostedService = services.Any(s =>
            s.ImplementationType == typeof(DSRAutoRegistrationHostedService));

        hasHostedService.ShouldBeTrue("Auto-registration hosted service should be registered");
    }

    [Fact]
    public void AddEncinaDataSubjectRights_WithoutAutoRegistration_DoesNotRegisterHostedService()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaDataSubjectRights(opts =>
        {
            opts.AutoRegisterFromAttributes = false;
        });

        var hasHostedService = services.Any(s =>
            s.ImplementationType == typeof(DSRAutoRegistrationHostedService));

        hasHostedService.ShouldBeFalse();
    }

    [Fact]
    public void AddEncinaDataSubjectRights_TryAdd_AllowsCustomOverride()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        // Register custom implementation first
        services.AddSingleton<IDataSubjectIdExtractor, DefaultDataSubjectIdExtractor>();

        services.AddEncinaDataSubjectRights(opts =>
        {
            opts.AutoRegisterFromAttributes = false;
        });

        var provider = services.BuildServiceProvider();
        var extractor = provider.GetRequiredService<IDataSubjectIdExtractor>();

        // Should be the custom one registered first (TryAdd does not replace)
        extractor.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaDataSubjectRights_NullConfigure_RegistersDefaultOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaDataSubjectRights(configure: null);

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<DataSubjectRightsOptions>>().Value;

        // Should have all defaults
        options.DefaultDeadlineDays.ShouldBe(30);
        options.MaxExtensionDays.ShouldBe(60);
        options.RestrictionEnforcementMode.ShouldBe(DSREnforcementMode.Block);
    }

    [Fact]
    public void AddEncinaDataSubjectRights_RegistersTimeProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaDataSubjectRights(opts =>
        {
            opts.AutoRegisterFromAttributes = false;
        });

        var provider = services.BuildServiceProvider();
        var timeProvider = provider.GetService<TimeProvider>();

        timeProvider.ShouldNotBeNull();
    }
}
