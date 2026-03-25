using Encina.Compliance.BreachNotification;
using Encina.Compliance.BreachNotification.Abstractions;
using Encina.Compliance.BreachNotification.Health;
using Encina.Compliance.BreachNotification.Model;
using Encina.Compliance.BreachNotification.ReadModels;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.BreachNotification;

/// <summary>
/// Unit tests for <see cref="BreachNotificationHealthCheck"/>.
/// </summary>
public sealed class BreachNotificationHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_WithAllServicesRegistered_NoBreaches_ShouldReturnHealthy()
    {
        // Arrange
        var breachService = Substitute.For<IBreachNotificationService>();
        breachService.GetApproachingDeadlineBreachesAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<BreachReadModel>>(
                new List<BreachReadModel>() as IReadOnlyList<BreachReadModel>));

        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<BreachNotificationOptions>(_ => { });
        services.AddScoped(_ => breachService);
        services.AddScoped(_ => Substitute.For<IBreachDetector>());
        services.AddScoped(_ => Substitute.For<IBreachNotifier>());

        var provider = services.BuildServiceProvider();
        var healthCheck = new BreachNotificationHealthCheck(
            provider,
            provider.GetRequiredService<ILogger<BreachNotificationHealthCheck>>());

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description!.ShouldContain("fully configured");
    }

    [Fact]
    public async Task CheckHealthAsync_WithoutBreachNotificationService_ShouldReturnUnhealthy()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<BreachNotificationOptions>(_ => { });
        // Do NOT register IBreachNotificationService

        var provider = services.BuildServiceProvider();
        var healthCheck = new BreachNotificationHealthCheck(
            provider,
            provider.GetRequiredService<ILogger<BreachNotificationHealthCheck>>());

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("IBreachNotificationService");
    }

    [Fact]
    public async Task CheckHealthAsync_WithoutBreachService_ShouldReturnUnhealthy()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<BreachNotificationOptions>(_ => { });
        // Do NOT register IBreachNotificationService

        var provider = services.BuildServiceProvider();
        var healthCheck = new BreachNotificationHealthCheck(
            provider,
            provider.GetRequiredService<ILogger<BreachNotificationHealthCheck>>());

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("IBreachNotificationService");
    }

    [Fact]
    public async Task CheckHealthAsync_WithoutDetector_ShouldReturnUnhealthy()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<BreachNotificationOptions>(_ => { });
        services.AddScoped(_ => Substitute.For<IBreachNotificationService>());
        // Do NOT register IBreachDetector

        var provider = services.BuildServiceProvider();
        var healthCheck = new BreachNotificationHealthCheck(
            provider,
            provider.GetRequiredService<ILogger<BreachNotificationHealthCheck>>());

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("IBreachDetector");
    }

    [Fact]
    public async Task CheckHealthAsync_WithoutNotifier_ShouldReturnUnhealthy()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<BreachNotificationOptions>(_ => { });
        services.AddScoped(_ => Substitute.For<IBreachNotificationService>());
        services.AddScoped(_ => Substitute.For<IBreachDetector>());
        // Do NOT register IBreachNotifier

        var provider = services.BuildServiceProvider();
        var healthCheck = new BreachNotificationHealthCheck(
            provider,
            provider.GetRequiredService<ILogger<BreachNotificationHealthCheck>>());

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("IBreachNotifier");
    }

    [Fact]
    public async Task CheckHealthAsync_WithApproachingDeadlineBreaches_ShouldReturnDegraded()
    {
        // Arrange
        var breachService = Substitute.For<IBreachNotificationService>();
        var now = TimeProvider.System.GetUtcNow();
        var approachingBreaches = new List<BreachReadModel>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Nature = "Test breach",
                Severity = BreachSeverity.High,
                Status = BreachStatus.Detected,
                DeadlineUtc = now.AddHours(12), // Within 24 hours
                DetectedAtUtc = now.AddHours(-60),
                LastModifiedAtUtc = now
            }
        } as IReadOnlyList<BreachReadModel>;

        breachService.GetApproachingDeadlineBreachesAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<BreachReadModel>>(approachingBreaches));

        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<BreachNotificationOptions>(_ => { });
        services.AddScoped(_ => breachService);
        services.AddScoped(_ => Substitute.For<IBreachDetector>());
        services.AddScoped(_ => Substitute.For<IBreachNotifier>());

        var provider = services.BuildServiceProvider();
        var healthCheck = new BreachNotificationHealthCheck(
            provider,
            provider.GetRequiredService<ILogger<BreachNotificationHealthCheck>>());

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description!.ShouldContain("warnings");
    }

    [Fact]
    public async Task CheckHealthAsync_WithOverdueBreaches_ShouldReturnDegradedWithOverdueInfo()
    {
        // Arrange
        var breachService = Substitute.For<IBreachNotificationService>();
        var now = TimeProvider.System.GetUtcNow();
        var overdueBreaches = new List<BreachReadModel>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Nature = "Overdue breach",
                Severity = BreachSeverity.Critical,
                Status = BreachStatus.Detected,
                DeadlineUtc = now.AddHours(-2), // Past deadline
                DetectedAtUtc = now.AddHours(-80),
                LastModifiedAtUtc = now
            }
        } as IReadOnlyList<BreachReadModel>;

        breachService.GetApproachingDeadlineBreachesAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<BreachReadModel>>(overdueBreaches));

        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<BreachNotificationOptions>(_ => { });
        services.AddScoped(_ => breachService);
        services.AddScoped(_ => Substitute.For<IBreachDetector>());
        services.AddScoped(_ => Substitute.For<IBreachNotifier>());

        var provider = services.BuildServiceProvider();
        var healthCheck = new BreachNotificationHealthCheck(
            provider,
            provider.GetRequiredService<ILogger<BreachNotificationHealthCheck>>());

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description!.ShouldContain("exceeded");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenBreachQueryFails_ShouldReturnDegraded()
    {
        // Arrange
        var breachService = Substitute.For<IBreachNotificationService>();
        var error = EncinaErrors.Create("query.failed", "DB unavailable");
        breachService.GetApproachingDeadlineBreachesAsync(Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, IReadOnlyList<BreachReadModel>>(error));

        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<BreachNotificationOptions>(_ => { });
        services.AddScoped(_ => breachService);
        services.AddScoped(_ => Substitute.For<IBreachDetector>());
        services.AddScoped(_ => Substitute.For<IBreachNotifier>());

        var provider = services.BuildServiceProvider();
        var healthCheck = new BreachNotificationHealthCheck(
            provider,
            provider.GetRequiredService<ILogger<BreachNotificationHealthCheck>>());

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
    }

    [Fact]
    public void DefaultName_ShouldBeExpectedValue()
    {
        BreachNotificationHealthCheck.DefaultName.ShouldBe("encina-breach-notification");
    }

    [Fact]
    public void Tags_ShouldContainExpectedValues()
    {
        var tags = BreachNotificationHealthCheck.Tags.ToList();
        tags.ShouldContain("encina");
        tags.ShouldContain("gdpr");
        tags.ShouldContain("breach");
        tags.ShouldContain("compliance");
    }
}
