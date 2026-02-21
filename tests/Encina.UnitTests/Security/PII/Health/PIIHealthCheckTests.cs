using Encina.Security.PII;
using Encina.Security.PII.Abstractions;
using Encina.Security.PII.Health;
using Encina.Security.PII.Strategies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;

namespace Encina.UnitTests.Security.PII.Health;

public sealed class PIIHealthCheckTests
{
    private static PIIHealthCheck CreateSut(ServiceProvider sp) => new(sp);

    private static ServiceProvider CreateHealthyProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaPII();
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task CheckHealthAsync_AllServicesRegistered_ReturnsHealthy()
    {
        // Arrange
        var provider = CreateHealthyProvider();
        var sut = CreateSut(provider);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("test", sut, null, null)
        };

        // Act
        var result = await sut.CheckHealthAsync(context);

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldNotBeNull();
        result.Description.ShouldContain("healthy");
        result.Data.ShouldContainKey("masker");
        result.Data.ShouldContainKey("strategies_resolved");
        result.Data.ShouldContainKey("strategies_total");
    }

    [Fact]
    public async Task CheckHealthAsync_MissingIPIIMasker_ReturnsUnhealthy()
    {
        // Arrange - empty provider with no PII services
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var sut = CreateSut(provider);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("test", sut, null, null)
        };

        // Act
        var result = await sut.CheckHealthAsync(context);

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldNotBeNull();
        result.Description.ShouldContain("IPIIMasker");
    }

    [Fact]
    public async Task CheckHealthAsync_MissingAllStrategies_ReturnsUnhealthy()
    {
        // Arrange - register masker but no strategies
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IPIIMasker>(sp =>
        {
            // Register a real masker via full PII registration, then build a new
            // provider that only has IPIIMasker but no strategy types.
            // Simplest approach: use NSubstitute mock that returns the input (probe fails).
            var masker = Substitute.For<IPIIMasker>();
            masker.MaskObject(Arg.Any<object>()).Returns(x => x[0]);
            return masker;
        });
        var provider = services.BuildServiceProvider();
        var sut = CreateSut(provider);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("test", sut, null, null)
        };

        // Act
        var result = await sut.CheckHealthAsync(context);

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldNotBeNull();
        result.Description.ShouldContain("No masking strategies");
    }

    [Fact]
    public async Task CheckHealthAsync_MissingSomeStrategies_ReturnsDegraded()
    {
        // Arrange - register all PII services then remove one strategy to simulate partial resolution
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaPII();

        // Remove the EmailMaskingStrategy to simulate a missing strategy
        var descriptorToRemove = services.FirstOrDefault(
            d => d.ServiceType == typeof(EmailMaskingStrategy));
        if (descriptorToRemove is not null)
        {
            services.Remove(descriptorToRemove);
        }

        var provider = services.BuildServiceProvider();
        var sut = CreateSut(provider);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("test", sut, null, null)
        };

        // Act
        var result = await sut.CheckHealthAsync(context);

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description.ShouldNotBeNull();
        result.Description.ShouldContain("could not be resolved");
        result.Data.ShouldContainKey("missing_strategies");
    }

    [Fact]
    public async Task CheckHealthAsync_MaskingProbeFails_ReturnsUnhealthy()
    {
        // Arrange - register a masker that does NOT actually mask (returns input unchanged)
        var services = new ServiceCollection();
        services.AddLogging();

        // Register all strategies so that step passes
        services.AddEncinaPII();

        // Override IPIIMasker with a mock that returns the input unchanged (probe will detect this)
        var noOpMasker = Substitute.For<IPIIMasker>();
        noOpMasker.MaskObject(Arg.Any<object>()).Returns(x => x[0]);

        // Remove existing registration and add our mock
        var existingDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IPIIMasker));
        if (existingDescriptor is not null)
        {
            services.Remove(existingDescriptor);
        }

        services.AddSingleton(noOpMasker);
        var provider = services.BuildServiceProvider();
        var sut = CreateSut(provider);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("test", sut, null, null)
        };

        // Act
        var result = await sut.CheckHealthAsync(context);

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldNotBeNull();
        result.Description.ShouldContain("probe");
    }

    [Fact]
    public void DefaultName_ShouldBe_EncinaPii()
    {
        PIIHealthCheck.DefaultName.ShouldBe("encina_pii");
    }
}
