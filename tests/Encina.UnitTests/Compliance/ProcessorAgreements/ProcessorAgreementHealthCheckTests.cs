#pragma warning disable CA2012

using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Health;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.UnitTests.Compliance.ProcessorAgreements;

/// <summary>
/// Unit tests for <see cref="ProcessorAgreementHealthCheck"/>.
/// </summary>
public class ProcessorAgreementHealthCheckTests
{
    #region Helpers

    private static ServiceProvider BuildServiceProvider(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddEncinaProcessorAgreements();
        services.AddLogging();
        configure?.Invoke(services);
        return services.BuildServiceProvider();
    }

    private static HealthCheckContext CreateContext() =>
        new()
        {
            Registration = new HealthCheckRegistration(
                ProcessorAgreementHealthCheck.DefaultName,
                _ => new ProcessorAgreementHealthCheck(
                    new ServiceCollection().BuildServiceProvider(),
                    NullLogger<ProcessorAgreementHealthCheck>.Instance),
                failureStatus: null,
                tags: null)
        };

    #endregion

    [Fact]
    public async Task CheckHealthAsync_AllServicesRegistered_ReturnsHealthy()
    {
        // Arrange
        var provider = BuildServiceProvider();
        var sut = new ProcessorAgreementHealthCheck(
            provider,
            NullLogger<ProcessorAgreementHealthCheck>.Instance);

        // Act
        var result = await sut.CheckHealthAsync(CreateContext(), CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_OptionsNotConfigured_ReturnsUnhealthy()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        // Intentionally NOT calling AddEncinaProcessorAgreements()
        var provider = services.BuildServiceProvider();

        var sut = new ProcessorAgreementHealthCheck(
            provider,
            NullLogger<ProcessorAgreementHealthCheck>.Instance);

        // Act
        var result = await sut.CheckHealthAsync(CreateContext(), CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_DPAStoreNotRegistered_ReturnsUnhealthy()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<ProcessorAgreementOptions>(_ => { });
        // Register everything except IDPAStore
        services.AddSingleton<IDPAValidator, DefaultDPAValidator>();
        services.AddSingleton<IProcessorRegistry, InMemoryProcessorRegistry>();
        var provider = services.BuildServiceProvider();

        var sut = new ProcessorAgreementHealthCheck(
            provider,
            NullLogger<ProcessorAgreementHealthCheck>.Instance);

        // Act
        var result = await sut.CheckHealthAsync(CreateContext(), CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("IDPAStore");
    }

    [Fact]
    public async Task CheckHealthAsync_ValidatorNotRegistered_ReturnsUnhealthy()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<ProcessorAgreementOptions>(_ => { });
        services.AddSingleton<IDPAStore, InMemoryDPAStore>();
        services.AddSingleton<IProcessorRegistry, InMemoryProcessorRegistry>();
        // No IDPAValidator registered
        var provider = services.BuildServiceProvider();

        var sut = new ProcessorAgreementHealthCheck(
            provider,
            NullLogger<ProcessorAgreementHealthCheck>.Instance);

        // Act
        var result = await sut.CheckHealthAsync(CreateContext(), CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("IDPAValidator");
    }

    [Fact]
    public async Task CheckHealthAsync_AuditStoreNotRegistered_ReturnsDegraded()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<ProcessorAgreementOptions>(_ => { });
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IDPAStore, InMemoryDPAStore>();
        services.AddSingleton<IProcessorRegistry, InMemoryProcessorRegistry>();
        services.AddScoped<IDPAValidator, DefaultDPAValidator>();
        // No IProcessorAuditStore registered
        var provider = services.BuildServiceProvider();

        var sut = new ProcessorAgreementHealthCheck(
            provider,
            NullLogger<ProcessorAgreementHealthCheck>.Instance);

        // Act
        var result = await sut.CheckHealthAsync(CreateContext(), CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("IProcessorAuditStore");
    }

    [Fact]
    public void DefaultName_HasExpectedValue()
    {
        // Assert
        ProcessorAgreementHealthCheck.DefaultName.Should().Be("encina-processor-agreements");
    }
}
