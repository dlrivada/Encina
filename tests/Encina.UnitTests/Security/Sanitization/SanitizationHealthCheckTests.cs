using Encina.Security.Sanitization;
using Encina.Security.Sanitization.Abstractions;
using Encina.Security.Sanitization.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;
using Shouldly;

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

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description!.ShouldContain("healthy");
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

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("ISanitizer");
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

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Data.ShouldContainKey("sanitizer");
        result.Data.ShouldContainKey("encoder");
        result.Data["sanitizer"].ShouldBe("DefaultSanitizer");
        result.Data["encoder"].ShouldBe("DefaultOutputEncoder");
    }

    [Fact]
    public async Task CheckHealthAsync_SanitizerThrows_DoesNotLeakExceptionMessage()
    {
        const string sentinel = "sensitive-connection-string-sentinel";
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaSanitization();

        var throwingSanitizer = Substitute.For<ISanitizer>();
        throwingSanitizer.SanitizeHtml(Arg.Any<string>())
            .Returns(_ => throw new InvalidOperationException(sentinel));

        var existingDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ISanitizer));
        if (existingDescriptor is not null)
        {
            services.Remove(existingDescriptor);
        }

        services.AddSingleton(throwingSanitizer);
        var provider = services.BuildServiceProvider();

        var healthCheck = new SanitizationHealthCheck(provider);

        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext
            {
                Registration = new HealthCheckRegistration("test", healthCheck, null, null)
            });

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldNotContain(sentinel);
        result.Description!.ShouldContain(nameof(InvalidOperationException));
    }

    [Fact]
    public void DefaultName_IsCorrect()
    {
        SanitizationHealthCheck.DefaultName.ShouldBe("encina-sanitization");
    }

    [Fact]
    public void Tags_ContainsExpectedValues()
    {
        SanitizationHealthCheck.Tags.ShouldContain("encina");
        SanitizationHealthCheck.Tags.ShouldContain("security");
        SanitizationHealthCheck.Tags.ShouldContain("sanitization");
        SanitizationHealthCheck.Tags.ShouldContain("ready");
    }
}
