#pragma warning disable CA2012 // Use ValueTasks correctly - required for NSubstitute mock setup

using Encina.Compliance.AIAct;
using Encina.Compliance.AIAct.Abstractions;
using Encina.Compliance.AIAct.Health;
using Encina.Compliance.AIAct.Model;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.AIAct;

/// <summary>
/// Unit tests for <see cref="AIActHealthCheck"/>.
/// </summary>
public class AIActHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_AllServicesRegistered_ShouldReturnHealthy()
    {
        // Arrange
        var services = BuildFullyConfiguredServices();
        var provider = services.BuildServiceProvider();

        // Register at least one AI system — health check returns Degraded for empty registries
        var registry = provider.GetRequiredService<IAISystemRegistry>();
        await registry.RegisterSystemAsync(new AISystemRegistration
        {
            SystemId = "health-check-test",
            Name = "Health Check Test System",
            Category = AISystemCategory.EmploymentWorkersManagement,
            RiskLevel = AIRiskLevel.MinimalRisk,
            RegisteredAtUtc = DateTimeOffset.UtcNow
        });

        var sut = new AIActHealthCheck(provider, CreateLogger());

        // Act
        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_MissingOptions_ShouldReturnUnhealthy()
    {
        // Arrange — no AddEncinaAIAct
        var services = new ServiceCollection();
        services.AddLogging();
        var provider = services.BuildServiceProvider();
        var sut = new AIActHealthCheck(provider, CreateLogger());

        // Act
        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_MissingRegistry_ShouldReturnUnhealthy()
    {
        // Arrange — options present but no registry
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<AIActOptions>(_ => { });
        var provider = services.BuildServiceProvider();
        var sut = new AIActHealthCheck(provider, CreateLogger());

        // Act
        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_DisabledEnforcementMode_ShouldReturnDegraded()
    {
        // Arrange
        var services = BuildFullyConfiguredServices(opts =>
            opts.EnforcementMode = AIActEnforcementMode.Disabled);
        var provider = services.BuildServiceProvider();
        var sut = new AIActHealthCheck(provider, CreateLogger());

        // Act
        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
    }

    [Fact]
    public async Task CheckHealthAsync_MissingClassifier_ShouldReturnDegraded()
    {
        // Arrange — registry present but no classifier
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<AIActOptions>(_ => { });
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IAISystemRegistry>(new InMemoryAISystemRegistry(TimeProvider.System));
        services.AddSingleton(Substitute.For<IHumanOversightEnforcer>());
        services.AddScoped(_ => Substitute.For<IAIActComplianceValidator>());
        var provider = services.BuildServiceProvider();
        var sut = new AIActHealthCheck(provider, CreateLogger());

        // Act
        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
    }

    [Fact]
    public async Task CheckHealthAsync_RegistryQueryFails_DoesNotLeakErrorMessage()
    {
        // Arrange
        const string sentinel = "sensitive-connection-string-sentinel";
        var registry = Substitute.For<IAISystemRegistry>();
        registry.GetAllSystemsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Left<EncinaError, IReadOnlyList<AISystemRegistration>>(
                    EncinaErrors.Create("registry.error", sentinel))));

        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<AIActOptions>(_ => { });
        services.AddSingleton(registry);
        services.AddSingleton(Substitute.For<IAIActClassifier>());
        services.AddSingleton(Substitute.For<IHumanOversightEnforcer>());
        services.AddScoped(_ => Substitute.For<IAIActComplianceValidator>());
        var provider = services.BuildServiceProvider();
        var sut = new AIActHealthCheck(provider, CreateLogger());

        // Act
        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Description.ShouldNotBeNull();
        result.Description!.ShouldNotContain(sentinel);
        result.Description!.ShouldContain("registry.error");
    }

    [Fact]
    public void DefaultName_ShouldBeEncinaAIAct()
    {
        AIActHealthCheck.DefaultName.ShouldBe("encina-aiact");
    }

    [Fact]
    public void Tags_ShouldContainExpectedTags()
    {
        AIActHealthCheck.Tags.ShouldContain("encina");
        AIActHealthCheck.Tags.ShouldContain("aiact");
        AIActHealthCheck.Tags.ShouldContain("compliance");
        AIActHealthCheck.Tags.ShouldContain("ready");
    }

    // -- Helpers --

    private static ServiceCollection BuildFullyConfiguredServices(
        Action<AIActOptions>? configureOptions = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaAIAct(options =>
        {
            options.AutoRegisterFromAttributes = false;
            configureOptions?.Invoke(options);
        });
        return services;
    }

    private static Microsoft.Extensions.Logging.Abstractions.NullLogger<AIActHealthCheck> CreateLogger() =>
        new();
}
