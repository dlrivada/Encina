#pragma warning disable CA2012

using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Abstractions;
using Encina.Compliance.ProcessorAgreements.Health;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Compliance.ProcessorAgreements.ReadModels;

using Shouldly;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.ProcessorAgreements;

/// <summary>
/// Unit tests for <see cref="ProcessorAgreementHealthCheck"/>.
/// </summary>
public class ProcessorAgreementHealthCheckTests
{
    #region Helpers

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
        var mockProcessorService = Substitute.For<IProcessorService>();
        var mockDpaService = Substitute.For<IDPAService>();
        mockDpaService.GetDPAsByStatusAsync(Arg.Any<DPAStatus>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, IReadOnlyList<DPAReadModel>>(
                    System.Array.Empty<DPAReadModel>().AsReadOnly())));

        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<ProcessorAgreementOptions>(_ => { });
        services.AddScoped<IProcessorService>(_ => mockProcessorService);
        services.AddScoped<IDPAService>(_ => mockDpaService);
        var provider = services.BuildServiceProvider();

        var sut = new ProcessorAgreementHealthCheck(
            provider,
            NullLogger<ProcessorAgreementHealthCheck>.Instance);

        // Act
        var result = await sut.CheckHealthAsync(CreateContext(), CancellationToken.None);

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_OptionsNotConfigured_ReturnsUnhealthy()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        // Intentionally NOT calling AddEncinaProcessorAgreements() or Configure<ProcessorAgreementOptions>
        var provider = services.BuildServiceProvider();

        var sut = new ProcessorAgreementHealthCheck(
            provider,
            NullLogger<ProcessorAgreementHealthCheck>.Instance);

        // Act
        var result = await sut.CheckHealthAsync(CreateContext(), CancellationToken.None);

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_ProcessorServiceNotRegistered_ReturnsUnhealthy()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<ProcessorAgreementOptions>(_ => { });
        // Register IDPAService but NOT IProcessorService
        services.AddScoped<IDPAService>(_ => Substitute.For<IDPAService>());
        var provider = services.BuildServiceProvider();

        var sut = new ProcessorAgreementHealthCheck(
            provider,
            NullLogger<ProcessorAgreementHealthCheck>.Instance);

        // Act
        var result = await sut.CheckHealthAsync(CreateContext(), CancellationToken.None);

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldContain("IProcessorService");
    }

    [Fact]
    public async Task CheckHealthAsync_DPAServiceNotRegistered_ReturnsUnhealthy()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<ProcessorAgreementOptions>(_ => { });
        services.AddScoped<IProcessorService>(_ => Substitute.For<IProcessorService>());
        // No IDPAService registered
        var provider = services.BuildServiceProvider();

        var sut = new ProcessorAgreementHealthCheck(
            provider,
            NullLogger<ProcessorAgreementHealthCheck>.Instance);

        // Act
        var result = await sut.CheckHealthAsync(CreateContext(), CancellationToken.None);

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldContain("IDPAService");
    }

    [Fact]
    public void DefaultName_HasExpectedValue()
    {
        // Assert
        ProcessorAgreementHealthCheck.DefaultName.ShouldBe("encina-processor-agreements");
    }
}
