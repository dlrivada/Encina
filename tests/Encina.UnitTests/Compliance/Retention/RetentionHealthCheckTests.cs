using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Abstractions;
using Encina.Compliance.Retention.Health;

using FluentAssertions;

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
        RetentionHealthCheck.DefaultName.Should().Be("encina-retention");
    }

    [Fact]
    public void Tags_ShouldContainExpectedValues()
    {
        var tags = RetentionHealthCheck.Tags.ToList();

        tags.Should().Contain("encina");
        tags.Should().Contain("gdpr");
        tags.Should().Contain("retention");
        tags.Should().Contain("compliance");
        tags.Should().Contain("ready");
    }

    [Fact]
    public async Task CheckHealthAsync_NoOptions_ReturnsUnhealthy()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var sut = new RetentionHealthCheck(provider, NullLogger<RetentionHealthCheck>.Instance);

        var result = await sut.CheckHealthAsync(CreateContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("RetentionOptions");
    }

    [Fact]
    public async Task CheckHealthAsync_NoRecordService_ReturnsUnhealthy()
    {
        var services = new ServiceCollection();
        services.Configure<RetentionOptions>(_ => { });
        var provider = services.BuildServiceProvider();
        var sut = new RetentionHealthCheck(provider, NullLogger<RetentionHealthCheck>.Instance);

        var result = await sut.CheckHealthAsync(CreateContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("IRetentionRecordService");
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

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("IRetentionPolicyService");
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

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("ILegalHoldService");
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

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("fully configured");
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

        result.Data.Should().ContainKey("enforcementMode");
        result.Data["enforcementMode"].Should().Be("Block");
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

        result.Data.Should().ContainKey("recordServiceType");
        result.Data.Should().ContainKey("policyServiceType");
        result.Data.Should().ContainKey("legalHoldServiceType");
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
