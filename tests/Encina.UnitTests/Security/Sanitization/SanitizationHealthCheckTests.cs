using Encina.Security.Sanitization;
using Encina.Security.Sanitization.Health;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Encina.UnitTests.Security.Sanitization;

public sealed class SanitizationHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_AllServicesRegistered_ReturnsHealthy()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaSanitization(options => options.AddHealthCheck = true);
        var provider = services.BuildServiceProvider();

        var healthCheck = new SanitizationHealthCheck(provider);

        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext
            {
                Registration = new HealthCheckRegistration("test", healthCheck, null, null)
            });

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("healthy");
    }

    [Fact]
    public async Task CheckHealthAsync_NoSanitizer_ReturnsUnhealthy()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        var healthCheck = new SanitizationHealthCheck(provider);

        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext
            {
                Registration = new HealthCheckRegistration("test", healthCheck, null, null)
            });

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("ISanitizer");
    }

    [Fact]
    public async Task CheckHealthAsync_HealthyResult_ContainsServiceData()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaSanitization();
        var provider = services.BuildServiceProvider();

        var healthCheck = new SanitizationHealthCheck(provider);

        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext
            {
                Registration = new HealthCheckRegistration("test", healthCheck, null, null)
            });

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data.Should().ContainKey("sanitizer");
        result.Data.Should().ContainKey("encoder");
        result.Data["sanitizer"].Should().Be("DefaultSanitizer");
        result.Data["encoder"].Should().Be("DefaultOutputEncoder");
    }

    [Fact]
    public void DefaultName_IsCorrect()
    {
        SanitizationHealthCheck.DefaultName.Should().Be("encina-sanitization");
    }

    [Fact]
    public void Tags_ContainsExpectedValues()
    {
        SanitizationHealthCheck.Tags.Should().Contain("encina");
        SanitizationHealthCheck.Tags.Should().Contain("security");
        SanitizationHealthCheck.Tags.Should().Contain("sanitization");
        SanitizationHealthCheck.Tags.Should().Contain("ready");
    }
}
