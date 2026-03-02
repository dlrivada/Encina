using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Health;
using Encina.Compliance.DataResidency.InMemory;
using Encina.Compliance.DataResidency.Model;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace Encina.UnitTests.Compliance.DataResidency;

public class DataResidencyHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_WithAllServicesRegistered_ShouldReturnHealthy()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaDataResidency(options =>
        {
            options.DefaultRegion = RegionRegistry.DE;
            options.AddHealthCheck = true;
        });
        var provider = services.BuildServiceProvider();

        // Verify health check was registered in HealthCheckServiceOptions
        var healthCheckOptions = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>();
        healthCheckOptions.Value.Registrations
            .Should().Contain(r => r.Name == DataResidencyHealthCheck.DefaultName);

        // Construct health check directly (AddHealthChecks registers via options, not as IHealthCheck)
        var healthCheck = new DataResidencyHealthCheck(
            provider,
            provider.GetRequiredService<ILogger<DataResidencyHealthCheck>>());

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public void DefaultName_ShouldBeDataResidency()
    {
        DataResidencyHealthCheck.DefaultName.Should().Be("encina-data-residency");
    }

    [Fact]
    public void Tags_ShouldContainExpectedValues()
    {
        DataResidencyHealthCheck.Tags.Should().NotBeEmpty();
    }
}
