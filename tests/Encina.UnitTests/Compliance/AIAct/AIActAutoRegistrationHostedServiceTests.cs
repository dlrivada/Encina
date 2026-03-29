using System.Reflection;
using Encina.Compliance.AIAct;
using Encina.Compliance.AIAct.Abstractions;
using Encina.Compliance.AIAct.Attributes;
using Encina.Compliance.AIAct.Model;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.AIAct;

/// <summary>
/// Unit tests for <see cref="AIActAutoRegistrationHostedService"/>.
/// </summary>
public sealed class AIActAutoRegistrationHostedServiceTests
{
    private readonly FakeTimeProvider _timeProvider;
    private readonly ILogger<AIActAutoRegistrationHostedService> _logger;

    public AIActAutoRegistrationHostedServiceTests()
    {
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero));
        _logger = NullLogger<AIActAutoRegistrationHostedService>.Instance;
    }

    [Fact]
    public async Task StartAsync_WithHighRiskTypes_RegistersThem()
    {
        // Arrange
        var registry = new InMemoryAISystemRegistry(_timeProvider);
        var descriptor = new AIActAutoRegistrationDescriptor(
            [typeof(SampleHighRiskForAutoReg).Assembly]);

        var service = CreateService(registry, descriptor);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        registry.IsRegistered(typeof(SampleHighRiskForAutoReg).FullName!).ShouldBeTrue();
    }

    [Fact]
    public async Task StartAsync_WithAlreadyRegisteredSystem_SkipsIt()
    {
        // Arrange
        var registry = new InMemoryAISystemRegistry(_timeProvider);
        var systemId = typeof(SampleHighRiskForAutoReg).FullName!;

        // Pre-register
        await registry.RegisterSystemAsync(new AISystemRegistration
        {
            SystemId = systemId,
            Name = "Pre-existing",
            Category = AISystemCategory.EmploymentWorkersManagement,
            RiskLevel = AIRiskLevel.HighRisk,
            RegisteredAtUtc = _timeProvider.GetUtcNow()
        });

        var descriptor = new AIActAutoRegistrationDescriptor(
            [typeof(SampleHighRiskForAutoReg).Assembly]);

        var service = CreateService(registry, descriptor);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert - should still have original
        var result = await registry.GetSystemAsync(systemId);
        result.Match(
            r => r.Name.ShouldBe("Pre-existing"),
            _ => Assert.Fail("Expected system to exist"));
    }

    [Fact]
    public async Task StartAsync_WithEmptyAssemblies_CompletesSuccessfully()
    {
        // Arrange
        var registry = new InMemoryAISystemRegistry(_timeProvider);
        var descriptor = new AIActAutoRegistrationDescriptor([]);

        var service = CreateService(registry, descriptor);

        // Act & Assert - should not throw
        await service.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StopAsync_DoesNothing()
    {
        // Arrange
        var registry = new InMemoryAISystemRegistry(_timeProvider);
        var descriptor = new AIActAutoRegistrationDescriptor([]);
        var service = CreateService(registry, descriptor);

        // Act & Assert - should complete immediately
        await service.StopAsync(CancellationToken.None);
    }

    private AIActAutoRegistrationHostedService CreateService(
        IAISystemRegistry registry,
        AIActAutoRegistrationDescriptor descriptor)
    {
        return new AIActAutoRegistrationHostedService(
            registry, descriptor, _timeProvider, _logger);
    }
}

[HighRiskAI(Category = AISystemCategory.EmploymentWorkersManagement)]
internal sealed class SampleHighRiskForAutoReg;
