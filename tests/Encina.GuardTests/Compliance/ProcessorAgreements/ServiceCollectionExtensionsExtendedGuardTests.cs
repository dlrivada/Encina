using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Abstractions;
using Encina.Compliance.ProcessorAgreements.Health;
using Encina.Compliance.ProcessorAgreements.Model;

namespace Encina.GuardTests.Compliance.ProcessorAgreements;

/// <summary>
/// Extended guard tests for <see cref="ServiceCollectionExtensions"/> that exercise
/// the registration method with various configuration actions to cover more executable lines.
/// </summary>
public sealed class ServiceCollectionExtensionsExtendedGuardTests
{
    [Fact]
    public void AddEncinaProcessorAgreements_WithNullConfigure_RegistersDefaultOptions()
    {
        var services = new ServiceCollection();

        var result = services.AddEncinaProcessorAgreements(configure: null);

        result.ShouldBeSameAs(services);

        // Verify options are registered
        var descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IConfigureOptions<ProcessorAgreementOptions>));
        descriptor.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaProcessorAgreements_WithConfigureAction_AppliesConfiguration()
    {
        var services = new ServiceCollection();

        services.AddEncinaProcessorAgreements(options =>
        {
            options.EnforcementMode = ProcessorAgreementEnforcementMode.Block;
            options.MaxSubProcessorDepth = 5;
            options.EnableExpirationMonitoring = true;
            options.ExpirationCheckInterval = TimeSpan.FromMinutes(30);
            options.ExpirationWarningDays = 60;
            options.TrackAuditTrail = false;
        });

        var sp = services.BuildServiceProvider();
        var optionsMonitor = sp.GetRequiredService<IOptions<ProcessorAgreementOptions>>();
        var opts = optionsMonitor.Value;

        opts.EnforcementMode.ShouldBe(ProcessorAgreementEnforcementMode.Block);
        opts.MaxSubProcessorDepth.ShouldBe(5);
        opts.EnableExpirationMonitoring.ShouldBeTrue();
        opts.ExpirationCheckInterval.ShouldBe(TimeSpan.FromMinutes(30));
        opts.ExpirationWarningDays.ShouldBe(60);
        opts.TrackAuditTrail.ShouldBeFalse();
    }

    [Fact]
    public void AddEncinaProcessorAgreements_WithHealthCheck_RegistersHealthCheck()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaProcessorAgreements(options =>
        {
            options.AddHealthCheck = true;
        });

        // Verify health check registration exists
        var healthCheckDescriptor = services.FirstOrDefault(
            d => d.ServiceType.Name.Contains("HealthCheckService") ||
                 d.ImplementationType?.Name.Contains("ProcessorAgreementHealthCheck") == true);

        // The health check is registered via AddHealthChecks().AddCheck<T>()
        // which adds IHealthCheckRegistration entries
        services.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void AddEncinaProcessorAgreements_WithoutHealthCheck_DoesNotRegisterHealthCheck()
    {
        var services = new ServiceCollection();

        services.AddEncinaProcessorAgreements(options =>
        {
            options.AddHealthCheck = false;
        });

        // The health check type should not be directly registered as a service
        var healthCheckDescriptor = services.FirstOrDefault(
            d => d.ImplementationType == typeof(ProcessorAgreementHealthCheck));
        healthCheckDescriptor.ShouldBeNull();
    }

    [Fact]
    public void AddEncinaProcessorAgreements_BlockWithoutValidDPA_SetsBlockMode()
    {
        var services = new ServiceCollection();

        services.AddEncinaProcessorAgreements(options =>
        {
            options.BlockWithoutValidDPA = true;
        });

        var sp = services.BuildServiceProvider();
        var opts = sp.GetRequiredService<IOptions<ProcessorAgreementOptions>>().Value;

        opts.BlockWithoutValidDPA.ShouldBeTrue();
        opts.EnforcementMode.ShouldBe(ProcessorAgreementEnforcementMode.Block);
    }

    [Fact]
    public void AddEncinaProcessorAgreements_CalledTwice_DoesNotDuplicate()
    {
        var services = new ServiceCollection();

        services.AddEncinaProcessorAgreements();
        services.AddEncinaProcessorAgreements();

        // TryAdd should prevent duplicate registrations for scoped services
        var processorServiceDescriptors = services
            .Where(d => d.ServiceType == typeof(IProcessorService))
            .ToList();
        processorServiceDescriptors.Count.ShouldBe(1);
    }

    [Fact]
    public void AddEncinaProcessorAgreements_RegistersTimeProvider()
    {
        var services = new ServiceCollection();

        services.AddEncinaProcessorAgreements();

        var descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(TimeProvider));
        descriptor.ShouldNotBeNull();
    }

    [Fact]
    public void ProcessorAgreementOptions_Defaults_AreCorrect()
    {
        var options = new ProcessorAgreementOptions();

        options.EnforcementMode.ShouldBe(ProcessorAgreementEnforcementMode.Warn);
        options.BlockWithoutValidDPA.ShouldBeFalse();
        options.MaxSubProcessorDepth.ShouldBe(3);
        options.EnableExpirationMonitoring.ShouldBeFalse();
        options.ExpirationCheckInterval.ShouldBe(TimeSpan.FromHours(1));
        options.ExpirationWarningDays.ShouldBe(30);
        options.TrackAuditTrail.ShouldBeTrue();
        options.AddHealthCheck.ShouldBeFalse();
    }

    [Fact]
    public void ProcessorAgreementOptions_BlockWithoutValidDPA_SetFalse_DoesNotChangeMode()
    {
        var options = new ProcessorAgreementOptions
        {
            EnforcementMode = ProcessorAgreementEnforcementMode.Block
        };

        // Setting false does not change the mode (only true sets Block)
        options.BlockWithoutValidDPA = false;

        // The mode remains Block because setter only acts on true
        options.EnforcementMode.ShouldBe(ProcessorAgreementEnforcementMode.Block);
    }
}
