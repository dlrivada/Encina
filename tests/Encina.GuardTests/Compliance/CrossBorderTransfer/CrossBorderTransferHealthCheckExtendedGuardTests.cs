#pragma warning disable CA2012

using Encina.Compliance.CrossBorderTransfer;
using Encina.Compliance.CrossBorderTransfer.Abstractions;
using Encina.Compliance.CrossBorderTransfer.Health;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

namespace Encina.GuardTests.Compliance.CrossBorderTransfer;

/// <summary>
/// Extended guard tests for <see cref="CrossBorderTransferHealthCheck"/> exercising
/// CheckHealthAsync with various service provider configurations.
/// </summary>
public class CrossBorderTransferHealthCheckExtendedGuardTests
{
    private readonly ILogger<CrossBorderTransferHealthCheck> _logger =
        NullLoggerFactory.Instance.CreateLogger<CrossBorderTransferHealthCheck>();

    #region CheckHealthAsync — Healthy

    [Fact]
    public async Task CheckHealthAsync_AllServicesRegistered_ReturnsHealthy()
    {
        var services = new ServiceCollection();
        services.AddEncinaCrossBorderTransfer(options =>
        {
            options.EnforcementMode = CrossBorderTransferEnforcementMode.Block;
            options.DefaultSourceCountryCode = "DE";
        });

        // Register mocked scoped services
        services.AddScoped<ITransferValidator>(_ => Substitute.For<ITransferValidator>());
        services.AddScoped<ITIAService>(_ => Substitute.For<ITIAService>());
        services.AddScoped<ISCCService>(_ => Substitute.For<ISCCService>());
        services.AddScoped<IApprovedTransferService>(_ => Substitute.For<IApprovedTransferService>());

        var sp = services.BuildServiceProvider();
        var sut = new CrossBorderTransferHealthCheck(sp, _logger);

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    #endregion

    #region CheckHealthAsync — Unhealthy (No Options)

    [Fact]
    public async Task CheckHealthAsync_NoOptionsConfigured_ReturnsUnhealthy()
    {
        var services = new ServiceCollection();
        var sp = services.BuildServiceProvider();
        var sut = new CrossBorderTransferHealthCheck(sp, _logger);

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    #endregion

    #region CheckHealthAsync — Unhealthy (No Validator)

    [Fact]
    public async Task CheckHealthAsync_NoTransferValidator_ReturnsUnhealthy()
    {
        var services = new ServiceCollection();
        services.Configure<CrossBorderTransferOptions>(o =>
        {
            o.DefaultSourceCountryCode = "DE";
        });
        var sp = services.BuildServiceProvider();
        var sut = new CrossBorderTransferHealthCheck(sp, _logger);

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    #endregion

    #region CheckHealthAsync — Degraded (Missing Optional Services)

    [Fact]
    public async Task CheckHealthAsync_MissingTIAService_ReturnsDegraded()
    {
        var services = new ServiceCollection();
        services.Configure<CrossBorderTransferOptions>(o =>
        {
            o.DefaultSourceCountryCode = "DE";
        });
        services.AddScoped<ITransferValidator>(_ => Substitute.For<ITransferValidator>());
        // ITIAService, ISCCService, IApprovedTransferService intentionally not registered

        var sp = services.BuildServiceProvider();
        var sut = new CrossBorderTransferHealthCheck(sp, _logger);

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Degraded);
    }

    [Fact]
    public async Task CheckHealthAsync_MissingSCCService_ReturnsDegraded()
    {
        var services = new ServiceCollection();
        services.Configure<CrossBorderTransferOptions>(o =>
        {
            o.DefaultSourceCountryCode = "DE";
        });
        services.AddScoped<ITransferValidator>(_ => Substitute.For<ITransferValidator>());
        services.AddScoped<ITIAService>(_ => Substitute.For<ITIAService>());
        services.AddScoped<IApprovedTransferService>(_ => Substitute.For<IApprovedTransferService>());
        // ISCCService intentionally not registered

        var sp = services.BuildServiceProvider();
        var sut = new CrossBorderTransferHealthCheck(sp, _logger);

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Degraded);
    }

    [Fact]
    public async Task CheckHealthAsync_MissingApprovedTransferService_ReturnsDegraded()
    {
        var services = new ServiceCollection();
        services.Configure<CrossBorderTransferOptions>(o =>
        {
            o.DefaultSourceCountryCode = "DE";
        });
        services.AddScoped<ITransferValidator>(_ => Substitute.For<ITransferValidator>());
        services.AddScoped<ITIAService>(_ => Substitute.For<ITIAService>());
        services.AddScoped<ISCCService>(_ => Substitute.For<ISCCService>());
        // IApprovedTransferService intentionally not registered

        var sp = services.BuildServiceProvider();
        var sut = new CrossBorderTransferHealthCheck(sp, _logger);

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Degraded);
    }

    #endregion

    #region Tags

    [Fact]
    public void Tags_ContainsExpectedValues()
    {
        var tags = CrossBorderTransferHealthCheck.Tags.ToList();

        tags.ShouldContain("encina");
        tags.ShouldContain("cross-border-transfer");
        tags.ShouldContain("compliance");
        tags.ShouldContain("gdpr");
        tags.ShouldContain("ready");
    }

    #endregion

    #region DefaultName

    [Fact]
    public void DefaultName_HasExpectedValue()
    {
        CrossBorderTransferHealthCheck.DefaultName.ShouldBe("encina-cross-border-transfer");
    }

    #endregion
}
