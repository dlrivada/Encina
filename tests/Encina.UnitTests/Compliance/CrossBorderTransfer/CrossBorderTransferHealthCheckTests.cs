using Encina.Compliance.CrossBorderTransfer;
using Encina.Compliance.CrossBorderTransfer.Abstractions;
using Encina.Compliance.CrossBorderTransfer.Health;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace Encina.UnitTests.Compliance.CrossBorderTransfer;

public class CrossBorderTransferHealthCheckTests
{
    private readonly ILogger<CrossBorderTransferHealthCheck> _logger = Substitute.For<ILogger<CrossBorderTransferHealthCheck>>();

    [Fact]
    public async Task CheckHealthAsync_AllServicesRegistered_ReturnsHealthy()
    {
        var services = new ServiceCollection();
        services.Configure<CrossBorderTransferOptions>(_ => { });
        services.AddSingleton(Substitute.For<ITransferValidator>());
        services.AddSingleton(Substitute.For<ITIAService>());
        services.AddSingleton(Substitute.For<ISCCService>());
        services.AddSingleton(Substitute.For<IApprovedTransferService>());
        var sp = services.BuildServiceProvider();

        var sut = new CrossBorderTransferHealthCheck(sp, _logger);

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_OptionsNotConfigured_ReturnsUnhealthy()
    {
        var services = new ServiceCollection();
        var sp = services.BuildServiceProvider();

        var sut = new CrossBorderTransferHealthCheck(sp, _logger);

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_TransferValidatorMissing_ReturnsUnhealthy()
    {
        var services = new ServiceCollection();
        services.Configure<CrossBorderTransferOptions>(_ => { });
        var sp = services.BuildServiceProvider();

        var sut = new CrossBorderTransferHealthCheck(sp, _logger);

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_OptionalServicesMissing_ReturnsDegraded()
    {
        var services = new ServiceCollection();
        services.Configure<CrossBorderTransferOptions>(_ => { });
        services.AddSingleton(Substitute.For<ITransferValidator>());
        // ITIAService, ISCCService, IApprovedTransferService all missing
        var sp = services.BuildServiceProvider();

        var sut = new CrossBorderTransferHealthCheck(sp, _logger);

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Degraded);
    }

    [Fact]
    public void DefaultName_ShouldBeExpected()
    {
        CrossBorderTransferHealthCheck.DefaultName.Should().Be("encina-cross-border-transfer");
    }

    [Fact]
    public void Tags_ShouldContainExpectedValues()
    {
        CrossBorderTransferHealthCheck.Tags.Should().Contain("encina");
        CrossBorderTransferHealthCheck.Tags.Should().Contain("gdpr");
        CrossBorderTransferHealthCheck.Tags.Should().Contain("compliance");
    }
}
