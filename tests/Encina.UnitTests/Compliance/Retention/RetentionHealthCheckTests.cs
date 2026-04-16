using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Abstractions;
using Encina.Compliance.Retention.Health;

using Shouldly;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace Encina.UnitTests.Compliance.Retention;

/// <summary>
/// Unit tests for <see cref="RetentionHealthCheck"/>.
/// </summary>
public sealed class RetentionHealthCheckTests
{
    [Fact]
    public void DefaultName_ShouldBeEncinaRetention()
    {
        RetentionHealthCheck.DefaultName.ShouldBe("encina-retention");
    }

    [Fact]
    public void Tags_ShouldContainExpectedValues()
    {
        var tags = RetentionHealthCheck.Tags.ToList();

        tags.ShouldContain("encina");
        tags.ShouldContain("gdpr");
        tags.ShouldContain("retention");
        tags.ShouldContain("compliance");
        tags.ShouldContain("ready");
    }

    [Fact]
    public async Task CheckHealthAsync_NoOptions_ReturnsUnhealthy()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var sut = new RetentionHealthCheck(provider, NullLogger<RetentionHealthCheck>.Instance);

        var result = await sut.CheckHealthAsync(CreateContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("RetentionOptions");
    }

    [Fact]
    public async Task CheckHealthAsync_NoRecordService_ReturnsUnhealthy()
    {
        var services = new ServiceCollection();
        services.Configure<RetentionOptions>(_ => { });
        var provider = services.BuildServiceProvider();
        var sut = new RetentionHealthCheck(provider, NullLogger<RetentionHealthCheck>.Instance);

        var result = await sut.CheckHealthAsync(CreateContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("IRetentionRecordService");
    }

    [Fact]
    public async Task CheckHealthAsync_NoPolicyService_ReturnsUnhealthy()
    {
        var services = new ServiceCollection();
        services.Configure<RetentionOptions>(_ => { });
        services.AddScoped(_ => Substitute.For<IRetentionRecordService>());
        var provider = services.BuildServiceProvider();
        var sut = new RetentionHealthCheck(provider, NullLogger<RetentionHealthCheck>.Instance);

        var result = await sut.CheckHealthAsync(CreateContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("IRetentionPolicyService");
    }

    [Fact]
    public async Task CheckHealthAsync_NoLegalHoldService_ReturnsDegraded()
    {
        var services = new ServiceCollection();
        services.Configure<RetentionOptions>(_ => { });
        services.AddScoped(_ => Substitute.For<IRetentionRecordService>());
        services.AddScoped(_ => Substitute.For<IRetentionPolicyService>());
        var provider = services.BuildServiceProvider();
        var sut = new RetentionHealthCheck(provider, NullLogger<RetentionHealthCheck>.Instance);

        var result = await sut.CheckHealthAsync(CreateContext());

        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description!.ShouldContain("ILegalHoldService");
    }

    [Fact]
    public async Task CheckHealthAsync_AllServicesRegistered_ReturnsHealthy()
    {
        var services = new ServiceCollection();
        services.Configure<RetentionOptions>(_ => { });
        services.AddScoped(_ => Substitute.For<IRetentionRecordService>());
        services.AddScoped(_ => Substitute.For<IRetentionPolicyService>());
        services.AddScoped(_ => Substitute.For<ILegalHoldService>());
        var provider = services.BuildServiceProvider();
        var sut = new RetentionHealthCheck(provider, NullLogger<RetentionHealthCheck>.Instance);

        var result = await sut.CheckHealthAsync(CreateContext());

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description!.ShouldContain("fully configured");
    }

    [Fact]
    public async Task CheckHealthAsync_Healthy_IncludesEnforcementModeInData()
    {
        var services = new ServiceCollection();
        services.Configure<RetentionOptions>(o => o.EnforcementMode = RetentionEnforcementMode.Block);
        services.AddScoped(_ => Substitute.For<IRetentionRecordService>());
        services.AddScoped(_ => Substitute.For<IRetentionPolicyService>());
        services.AddScoped(_ => Substitute.For<ILegalHoldService>());
        var provider = services.BuildServiceProvider();
        var sut = new RetentionHealthCheck(provider, NullLogger<RetentionHealthCheck>.Instance);

        var result = await sut.CheckHealthAsync(CreateContext());

        result.Data.ShouldContainKey("enforcementMode");
        result.Data["enforcementMode"].ShouldBe("Block");
    }

    [Fact]
    public async Task CheckHealthAsync_Healthy_IncludesServiceTypeNames()
    {
        var services = new ServiceCollection();
        services.Configure<RetentionOptions>(_ => { });
        services.AddScoped(_ => Substitute.For<IRetentionRecordService>());
        services.AddScoped(_ => Substitute.For<IRetentionPolicyService>());
        services.AddScoped(_ => Substitute.For<ILegalHoldService>());
        var provider = services.BuildServiceProvider();
        var sut = new RetentionHealthCheck(provider, NullLogger<RetentionHealthCheck>.Instance);

        var result = await sut.CheckHealthAsync(CreateContext());

        result.Data.ShouldContainKey("recordServiceType");
        result.Data.ShouldContainKey("policyServiceType");
        result.Data.ShouldContainKey("legalHoldServiceType");
    }

    private static HealthCheckContext CreateContext() => new()
    {
        Registration = new HealthCheckRegistration(
            RetentionHealthCheck.DefaultName,
            Substitute.For<IHealthCheck>(),
            null,
            null)
    };
}
