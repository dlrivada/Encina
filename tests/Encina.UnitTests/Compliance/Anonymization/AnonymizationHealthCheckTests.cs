using Encina.Compliance.Anonymization;
using Encina.Compliance.Anonymization.Health;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace Encina.UnitTests.Compliance.Anonymization;

public class AnonymizationHealthCheckTests
{
    private readonly ILogger<AnonymizationHealthCheck> _logger = Substitute.For<ILogger<AnonymizationHealthCheck>>();

    [Fact]
    public async Task CheckHealthAsync_AllServicesRegistered_ReturnsHealthy()
    {
        var services = new ServiceCollection();
        services.Configure<AnonymizationOptions>(o => o.TrackAuditTrail = false);
        services.AddSingleton(Substitute.For<IKeyProvider>());
        services.AddSingleton(Substitute.For<IPseudonymizer>());
        services.AddSingleton(Substitute.For<ITokenizer>());
        services.AddSingleton(Substitute.For<IAnonymizationTechnique>());
        var sp = services.BuildServiceProvider();

        var sut = new AnonymizationHealthCheck(sp, _logger);

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_OptionsNotConfigured_ReturnsUnhealthy()
    {
        var services = new ServiceCollection();
        var sp = services.BuildServiceProvider();

        var sut = new AnonymizationHealthCheck(sp, _logger);

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_KeyProviderMissing_ReturnsUnhealthy()
    {
        var services = new ServiceCollection();
        services.Configure<AnonymizationOptions>(_ => { });
        var sp = services.BuildServiceProvider();

        var sut = new AnonymizationHealthCheck(sp, _logger);

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_PseudonymizerMissing_ReturnsUnhealthy()
    {
        var services = new ServiceCollection();
        services.Configure<AnonymizationOptions>(_ => { });
        services.AddSingleton(Substitute.For<IKeyProvider>());
        var sp = services.BuildServiceProvider();

        var sut = new AnonymizationHealthCheck(sp, _logger);

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_NoTechniques_ReturnsDegraded()
    {
        var services = new ServiceCollection();
        services.Configure<AnonymizationOptions>(o => o.TrackAuditTrail = false);
        services.AddSingleton(Substitute.For<IKeyProvider>());
        services.AddSingleton(Substitute.For<IPseudonymizer>());
        services.AddSingleton(Substitute.For<ITokenizer>());
        // No IAnonymizationTechnique registered
        var sp = services.BuildServiceProvider();

        var sut = new AnonymizationHealthCheck(sp, _logger);

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Degraded);
    }

    [Fact]
    public async Task CheckHealthAsync_TokenizerMissing_ReturnsDegraded()
    {
        var services = new ServiceCollection();
        services.Configure<AnonymizationOptions>(o => o.TrackAuditTrail = false);
        services.AddSingleton(Substitute.For<IKeyProvider>());
        services.AddSingleton(Substitute.For<IPseudonymizer>());
        services.AddSingleton(Substitute.For<IAnonymizationTechnique>());
        // No ITokenizer registered
        var sp = services.BuildServiceProvider();

        var sut = new AnonymizationHealthCheck(sp, _logger);

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Degraded);
    }

    [Fact]
    public async Task CheckHealthAsync_AuditStoreEnabledButMissing_ReturnsDegraded()
    {
        var services = new ServiceCollection();
        services.Configure<AnonymizationOptions>(o => o.TrackAuditTrail = true);
        services.AddSingleton(Substitute.For<IKeyProvider>());
        services.AddSingleton(Substitute.For<IPseudonymizer>());
        services.AddSingleton(Substitute.For<ITokenizer>());
        services.AddSingleton(Substitute.For<IAnonymizationTechnique>());
        // No IAnonymizationAuditStore registered
        var sp = services.BuildServiceProvider();

        var sut = new AnonymizationHealthCheck(sp, _logger);

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Degraded);
    }

    [Fact]
    public void DefaultName_ShouldBeExpected()
    {
        AnonymizationHealthCheck.DefaultName.Should().Be("encina-anonymization");
    }

    [Fact]
    public void Tags_ShouldContainExpectedValues()
    {
        AnonymizationHealthCheck.Tags.Should().Contain("encina");
        AnonymizationHealthCheck.Tags.Should().Contain("gdpr");
        AnonymizationHealthCheck.Tags.Should().Contain("anonymization");
    }
}
