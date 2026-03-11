#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Health;
using Encina.Compliance.DPIA.Model;

using FluentAssertions;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace Encina.UnitTests.Compliance.DPIA;

/// <summary>
/// Unit tests for <see cref="DPIAHealthCheck"/>.
/// </summary>
public class DPIAHealthCheckTests
{
    #region Constants

    [Fact]
    public void DefaultName_HasExpectedValue()
    {
        DPIAHealthCheck.DefaultName.Should().Be("encina-dpia");
    }

    [Fact]
    public void Tags_ContainsExpectedValues()
    {
        DPIAHealthCheck.Tags.Should().Contain("encina");
        DPIAHealthCheck.Tags.Should().Contain("gdpr");
        DPIAHealthCheck.Tags.Should().Contain("dpia");
        DPIAHealthCheck.Tags.Should().Contain("compliance");
    }

    #endregion

    #region Healthy Scenarios

    [Fact]
    public async Task CheckHealthAsync_AllConfigured_ReturnsHealthy()
    {
        var store = Substitute.For<IDPIAStore>();
        store.GetExpiredAssessmentsAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Prelude.Right<EncinaError, IReadOnlyList<DPIAAssessment>>([])));

        store.GetAllAssessmentsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Prelude.Right<EncinaError, IReadOnlyList<DPIAAssessment>>([])));

        var engine = Substitute.For<IDPIAAssessmentEngine>();
        var options = new DPIAOptions { TrackAuditTrail = false };

        var sut = CreateHealthCheck(options, store, engine);

        var result = await sut.CheckHealthAsync(CreateContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("fully configured");
    }

    #endregion

    #region Unhealthy Scenarios

    [Fact]
    public async Task CheckHealthAsync_NoOptions_ReturnsUnhealthy()
    {
        // Build a provider WITHOUT DPIAOptions registered
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        var sut = new DPIAHealthCheck(provider, new NullLogger<DPIAHealthCheck>());

        var result = await sut.CheckHealthAsync(CreateContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("DPIAOptions are not configured");
    }

    [Fact]
    public async Task CheckHealthAsync_NoStore_ReturnsUnhealthy()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Options.Create(new DPIAOptions()));
        var provider = services.BuildServiceProvider();

        var sut = new DPIAHealthCheck(provider, new NullLogger<DPIAHealthCheck>());

        var result = await sut.CheckHealthAsync(CreateContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("IDPIAStore");
    }

    [Fact]
    public async Task CheckHealthAsync_NoEngine_ReturnsUnhealthy()
    {
        var store = Substitute.For<IDPIAStore>();
        var services = new ServiceCollection();
        services.AddSingleton(Options.Create(new DPIAOptions()));
        services.AddSingleton(store);
        var provider = services.BuildServiceProvider();

        var sut = new DPIAHealthCheck(provider, new NullLogger<DPIAHealthCheck>());

        var result = await sut.CheckHealthAsync(CreateContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("IDPIAAssessmentEngine");
    }

    #endregion

    #region Degraded Scenarios

    [Fact]
    public async Task CheckHealthAsync_ExpiredAssessments_ReturnsDegraded()
    {
        var expiredAssessment = new DPIAAssessment
        {
            Id = Guid.NewGuid(),
            RequestTypeName = "Ns.ExpiredCommand",
            Status = DPIAAssessmentStatus.Approved,
            CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-400),
            NextReviewAtUtc = DateTimeOffset.UtcNow.AddDays(-30)
        };

        var store = Substitute.For<IDPIAStore>();
        store.GetExpiredAssessmentsAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Prelude.Right<EncinaError, IReadOnlyList<DPIAAssessment>>([expiredAssessment])));

        store.GetAllAssessmentsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Prelude.Right<EncinaError, IReadOnlyList<DPIAAssessment>>([expiredAssessment])));

        var options = new DPIAOptions { TrackAuditTrail = false };
        var sut = CreateHealthCheck(options, store);

        var result = await sut.CheckHealthAsync(CreateContext());

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("expired");
    }

    [Fact]
    public async Task CheckHealthAsync_DraftInBlockMode_ReturnsDegraded()
    {
        var draftAssessment = new DPIAAssessment
        {
            Id = Guid.NewGuid(),
            RequestTypeName = "Ns.DraftCommand",
            Status = DPIAAssessmentStatus.Draft,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        var store = Substitute.For<IDPIAStore>();
        store.GetExpiredAssessmentsAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Prelude.Right<EncinaError, IReadOnlyList<DPIAAssessment>>([])));

        store.GetAllAssessmentsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Prelude.Right<EncinaError, IReadOnlyList<DPIAAssessment>>([draftAssessment])));

        var options = new DPIAOptions
        {
            EnforcementMode = DPIAEnforcementMode.Block,
            TrackAuditTrail = false
        };
        var sut = CreateHealthCheck(options, store);

        var result = await sut.CheckHealthAsync(CreateContext());

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("Draft");
    }

    [Fact]
    public async Task CheckHealthAsync_AuditTrailEnabled_NoAuditStore_ReturnsDegraded()
    {
        var store = Substitute.For<IDPIAStore>();
        store.GetExpiredAssessmentsAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Prelude.Right<EncinaError, IReadOnlyList<DPIAAssessment>>([])));

        store.GetAllAssessmentsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Prelude.Right<EncinaError, IReadOnlyList<DPIAAssessment>>([])));

        // TrackAuditTrail = true but no IDPIAAuditStore registered
        var options = new DPIAOptions { TrackAuditTrail = true };

        var services = new ServiceCollection();
        services.AddSingleton(Options.Create(options));
        services.AddSingleton(store);
        services.AddSingleton(Substitute.For<IDPIAAssessmentEngine>());
        services.AddSingleton(TimeProvider.System);
        var provider = services.BuildServiceProvider();

        var sut = new DPIAHealthCheck(provider, new NullLogger<DPIAHealthCheck>());

        var result = await sut.CheckHealthAsync(CreateContext());

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("IDPIAAuditStore");
    }

    #endregion

    #region Data Properties

    [Fact]
    public async Task CheckHealthAsync_Healthy_IncludesDataProperties()
    {
        var store = Substitute.For<IDPIAStore>();
        store.GetExpiredAssessmentsAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Prelude.Right<EncinaError, IReadOnlyList<DPIAAssessment>>([])));

        store.GetAllAssessmentsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Prelude.Right<EncinaError, IReadOnlyList<DPIAAssessment>>([])));

        var options = new DPIAOptions
        {
            EnforcementMode = DPIAEnforcementMode.Block,
            TrackAuditTrail = false
        };
        var sut = CreateHealthCheck(options, store);

        var result = await sut.CheckHealthAsync(CreateContext());

        result.Data.Should().ContainKey("enforcementMode");
        result.Data["enforcementMode"].Should().Be("Block");
        result.Data.Should().ContainKey("storeType");
        result.Data.Should().ContainKey("engineType");
    }

    #endregion

    #region Helpers

    private static DPIAHealthCheck CreateHealthCheck(
        DPIAOptions options,
        IDPIAStore? store = null,
        IDPIAAssessmentEngine? engine = null)
    {
        store ??= Substitute.For<IDPIAStore>();
        engine ??= Substitute.For<IDPIAAssessmentEngine>();

        var services = new ServiceCollection();
        services.AddSingleton(Options.Create(options));
        services.AddSingleton(store);
        services.AddSingleton(engine);
        services.AddSingleton(TimeProvider.System);

        var provider = services.BuildServiceProvider();

        return new DPIAHealthCheck(provider, new NullLogger<DPIAHealthCheck>());
    }

    private static HealthCheckContext CreateContext() => new()
    {
        Registration = new HealthCheckRegistration(
            DPIAHealthCheck.DefaultName,
            Substitute.For<IHealthCheck>(),
            null,
            null)
    };

    #endregion
}
