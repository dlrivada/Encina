#pragma warning disable CA2012

using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Abstractions;
using Encina.Compliance.ProcessorAgreements.Health;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Compliance.ProcessorAgreements.Scheduling;
using Encina.Compliance.ProcessorAgreements.Services;

using Shouldly;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace Encina.UnitTests.Compliance.ProcessorAgreements;

/// <summary>
/// Unit tests for <see cref="ServiceCollectionExtensions.AddEncinaProcessorAgreements"/>.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEncinaProcessorAgreements_RegistersProcessorService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaProcessorAgreements();

        // Assert — check descriptor because DefaultProcessorService has Marten dependencies
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IProcessorService));
        descriptor.ShouldNotBeNull();
        descriptor!.ImplementationType.ShouldBe(typeof(DefaultProcessorService));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddEncinaProcessorAgreements_RegistersDPAService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaProcessorAgreements();

        // Assert — check descriptor because DefaultDPAService has Marten dependencies
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IDPAService));
        descriptor.ShouldNotBeNull();
        descriptor!.ImplementationType.ShouldBe(typeof(DefaultDPAService));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddEncinaProcessorAgreements_RegistersPipelineBehavior()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaProcessorAgreements();
        var provider = services.BuildServiceProvider();

        // Assert
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IPipelineBehavior<,>) &&
            d.ImplementationType == typeof(ProcessorValidationPipelineBehavior<,>));
        descriptor.ShouldNotBeNull();
        descriptor!.Lifetime.ShouldBe(ServiceLifetime.Transient);
    }

    [Fact]
    public void AddEncinaProcessorAgreements_RegistersExpirationHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaProcessorAgreements();

        // Assert
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(ICommandHandler<CheckDPAExpirationCommand, Unit>) &&
            d.ImplementationType == typeof(CheckDPAExpirationHandler));
        descriptor.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaProcessorAgreements_RegistersTimeProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaProcessorAgreements();
        var provider = services.BuildServiceProvider();

        // Assert
        var timeProvider = provider.GetService<TimeProvider>();
        timeProvider.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaProcessorAgreements_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act
        var act = () => services.AddEncinaProcessorAgreements();

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .And.ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddEncinaProcessorAgreements_WithConfigure_AppliesOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaProcessorAgreements(options =>
        {
            options.EnforcementMode = ProcessorAgreementEnforcementMode.Block;
            options.MaxSubProcessorDepth = 5;
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<IOptions<ProcessorAgreementOptions>>().Value;
        options.EnforcementMode.ShouldBe(ProcessorAgreementEnforcementMode.Block);
        options.MaxSubProcessorDepth.ShouldBe(5);
    }

    [Fact]
    public void AddEncinaProcessorAgreements_WithHealthCheck_RegistersHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaProcessorAgreements(options =>
        {
            options.AddHealthCheck = true;
        });

        // Assert
        // The health check is registered via AddHealthChecks().AddCheck<T>,
        // which uses IConfigureOptions<HealthCheckServiceOptions>.
        var healthCheckOptionDescriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IConfigureOptions<HealthCheckServiceOptions>));
        healthCheckOptionDescriptor.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaProcessorAgreements_WithoutHealthCheck_DoesNotRegisterHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaProcessorAgreements(options =>
        {
            options.AddHealthCheck = false;
        });

        // Assert
        var healthCheckOptionDescriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IConfigureOptions<HealthCheckServiceOptions>));
        healthCheckOptionDescriptor.ShouldBeNull();
    }

    [Fact]
    public void AddEncinaProcessorAgreements_CustomServiceBeforeCall_PreservesCustomService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IDPAService>(_ => Substitute.For<IDPAService>());

        // Act
        services.AddEncinaProcessorAgreements();

        // Assert — TryAdd should NOT overwrite the custom registration
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IDPAService));
        descriptor.ShouldNotBeNull();
        descriptor!.ImplementationType.ShouldBeNull();
        descriptor.ImplementationFactory.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }
}
