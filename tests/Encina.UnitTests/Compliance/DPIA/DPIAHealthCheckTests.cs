#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Abstractions;
using Encina.Compliance.DPIA.Health;
using Encina.Compliance.DPIA.Model;
using Encina.Compliance.DPIA.ReadModels;

using Shouldly;

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
        DPIAHealthCheck.DefaultName.ShouldBe("encina-dpia");
    }

    [Fact]
    public void Tags_ContainsExpectedValues()
    {
        DPIAHealthCheck.Tags.ShouldContain("encina");
        DPIAHealthCheck.Tags.ShouldContain("gdpr");
        DPIAHealthCheck.Tags.ShouldContain("dpia");
        DPIAHealthCheck.Tags.ShouldContain("compliance");
    }

    #endregion

    #region Healthy Scenarios

    [Fact]
    public async Task CheckHealthAsync_AllConfigured_ReturnsHealthy()
    {
        var service = Substitute.For<IDPIAService>();
        service.GetExpiredAssessmentsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Prelude.Right<EncinaError, IReadOnlyList<DPIAReadModel>>(
                    Array.Empty<DPIAReadModel>() as IReadOnlyList<DPIAReadModel>)));

        service.GetAllAssessmentsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Prelude.Right<EncinaError, IReadOnlyList<DPIAReadModel>>(
                    Array.Empty<DPIAReadModel>() as IReadOnlyList<DPIAReadModel>)));

        var engine = Substitute.For<IDPIAAssessmentEngine>();
        var options = new DPIAOptions();

        var sut = CreateHealthCheck(options, service, engine);

        var result = await sut.CheckHealthAsync(CreateContext());

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldContain("fully configured");
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

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldContain("DPIAOptions are not configured");
    }

    [Fact]
    public async Task CheckHealthAsync_NoService_ReturnsUnhealthy()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Options.Create(new DPIAOptions()));
        var provider = services.BuildServiceProvider();

        var sut = new DPIAHealthCheck(provider, new NullLogger<DPIAHealthCheck>());

        var result = await sut.CheckHealthAsync(CreateContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldContain("IDPIAService");
    }

    [Fact]
    public async Task CheckHealthAsync_NoEngine_ReturnsUnhealthy()
    {
        var service = Substitute.For<IDPIAService>();
        var services = new ServiceCollection();
        services.AddSingleton(Options.Create(new DPIAOptions()));
        services.AddSingleton(service);
        var provider = services.BuildServiceProvider();

        var sut = new DPIAHealthCheck(provider, new NullLogger<DPIAHealthCheck>());

        var result = await sut.CheckHealthAsync(CreateContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldContain("IDPIAAssessmentEngine");
    }

    #endregion

    #region Degraded Scenarios

    [Fact]
    public async Task CheckHealthAsync_ExpiredAssessments_ReturnsDegraded()
    {
        var expiredReadModel = new DPIAReadModel
        {
            Id = Guid.NewGuid(),
            RequestTypeName = "Ns.ExpiredCommand",
            Status = DPIAAssessmentStatus.Approved,
            NextReviewAtUtc = DateTimeOffset.UtcNow.AddDays(-30)
        };

        var service = Substitute.For<IDPIAService>();
        service.GetExpiredAssessmentsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Prelude.Right<EncinaError, IReadOnlyList<DPIAReadModel>>(
                    new[] { expiredReadModel } as IReadOnlyList<DPIAReadModel>)));

        service.GetAllAssessmentsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Prelude.Right<EncinaError, IReadOnlyList<DPIAReadModel>>(
                    new[] { expiredReadModel } as IReadOnlyList<DPIAReadModel>)));

        var options = new DPIAOptions();
        var sut = CreateHealthCheck(options, service);

        var result = await sut.CheckHealthAsync(CreateContext());

        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description.ShouldContain("expired");
    }

    [Fact]
    public async Task CheckHealthAsync_DraftInBlockMode_ReturnsDegraded()
    {
        var draftReadModel = new DPIAReadModel
        {
            Id = Guid.NewGuid(),
            RequestTypeName = "Ns.DraftCommand",
            Status = DPIAAssessmentStatus.Draft,
        };

        var service = Substitute.For<IDPIAService>();
        service.GetExpiredAssessmentsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Prelude.Right<EncinaError, IReadOnlyList<DPIAReadModel>>(
                    Array.Empty<DPIAReadModel>() as IReadOnlyList<DPIAReadModel>)));

        service.GetAllAssessmentsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Prelude.Right<EncinaError, IReadOnlyList<DPIAReadModel>>(
                    new[] { draftReadModel } as IReadOnlyList<DPIAReadModel>)));

        var options = new DPIAOptions
        {
            EnforcementMode = DPIAEnforcementMode.Block,
        };
        var sut = CreateHealthCheck(options, service);

        var result = await sut.CheckHealthAsync(CreateContext());

        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description.ShouldContain("Draft");
    }

    #endregion

    #region Data Properties

    [Fact]
    public async Task CheckHealthAsync_Healthy_IncludesDataProperties()
    {
        var service = Substitute.For<IDPIAService>();
        service.GetExpiredAssessmentsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Prelude.Right<EncinaError, IReadOnlyList<DPIAReadModel>>(
                    Array.Empty<DPIAReadModel>() as IReadOnlyList<DPIAReadModel>)));

        service.GetAllAssessmentsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Prelude.Right<EncinaError, IReadOnlyList<DPIAReadModel>>(
                    Array.Empty<DPIAReadModel>() as IReadOnlyList<DPIAReadModel>)));

        var options = new DPIAOptions
        {
            EnforcementMode = DPIAEnforcementMode.Block,
        };
        var sut = CreateHealthCheck(options, service);

        var result = await sut.CheckHealthAsync(CreateContext());

        result.Data.ShouldContainKey("enforcementMode");
        result.Data["enforcementMode"].ShouldBe("Block");
        result.Data.ShouldContainKey("serviceType");
        result.Data.ShouldContainKey("engineType");
    }

    #endregion

    #region Helpers

    private static DPIAHealthCheck CreateHealthCheck(
        DPIAOptions options,
        IDPIAService? service = null,
        IDPIAAssessmentEngine? engine = null)
    {
        service ??= Substitute.For<IDPIAService>();
        engine ??= Substitute.For<IDPIAAssessmentEngine>();

        var services = new ServiceCollection();
        services.AddSingleton(Options.Create(options));
        services.AddSingleton(service);
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
