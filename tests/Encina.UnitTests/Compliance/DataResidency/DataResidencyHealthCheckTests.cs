using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Abstractions;
using Encina.Compliance.DataResidency.Health;
using Encina.Compliance.DataResidency.Model;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Encina.UnitTests.Compliance.DataResidency;

public class DataResidencyHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_WithAllServicesRegistered_ShouldReturnHealthy()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Register mock services that the health check resolves via scoped provider
        services.AddScoped(_ => Substitute.For<IResidencyPolicyService>());
        services.AddScoped(_ => Substitute.For<IDataLocationService>());
        services.AddSingleton(_ => Substitute.For<IRegionContextProvider>());
        services.AddSingleton(_ => Substitute.For<ICrossBorderTransferValidator>());
        services.Configure<DataResidencyOptions>(options =>
        {
            options.DefaultRegion = RegionRegistry.DE;
            options.TrackDataLocations = true;
        });

        var provider = services.BuildServiceProvider();

        var healthCheck = new DataResidencyHealthCheck(
            provider,
            provider.GetRequiredService<ILogger<DataResidencyHealthCheck>>());

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WithoutPolicyService_ShouldReturnUnhealthy()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<DataResidencyOptions>(_ => { });

        var provider = services.BuildServiceProvider();

        var healthCheck = new DataResidencyHealthCheck(
            provider,
            provider.GetRequiredService<ILogger<DataResidencyHealthCheck>>());

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("IResidencyPolicyService");
    }

    [Fact]
    public async Task CheckHealthAsync_WithoutTransferValidator_ShouldReturnDegraded()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped(_ => Substitute.For<IResidencyPolicyService>());
        services.AddScoped(_ => Substitute.For<IDataLocationService>());
        services.AddSingleton(_ => Substitute.For<IRegionContextProvider>());
        // Deliberately NOT registering ICrossBorderTransferValidator
        services.Configure<DataResidencyOptions>(_ => { });

        var provider = services.BuildServiceProvider();

        var healthCheck = new DataResidencyHealthCheck(
            provider,
            provider.GetRequiredService<ILogger<DataResidencyHealthCheck>>());

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
    }

    [Fact]
    public void DefaultName_ShouldBeDataResidency()
    {
        DataResidencyHealthCheck.DefaultName.ShouldBe("encina-data-residency");
    }

    [Fact]
    public void Tags_ShouldContainExpectedValues()
    {
        DataResidencyHealthCheck.Tags.ShouldNotBeEmpty();
    }
}
