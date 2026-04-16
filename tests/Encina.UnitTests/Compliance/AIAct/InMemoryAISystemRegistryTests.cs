using Encina.Compliance.AIAct;
using Encina.Compliance.AIAct.Model;
using LanguageExt;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.AIAct;

/// <summary>
/// Unit tests for <see cref="InMemoryAISystemRegistry"/>.
/// </summary>
public class InMemoryAISystemRegistryTests
{
    private readonly FakeTimeProvider _timeProvider = new();
    private readonly InMemoryAISystemRegistry _sut;

    public InMemoryAISystemRegistryTests()
    {
        _sut = new InMemoryAISystemRegistry(_timeProvider);
    }

    // -- RegisterSystemAsync --

    [Fact]
    public async Task RegisterSystemAsync_ValidRegistration_ShouldSucceed()
    {
        // Arrange
        var registration = CreateRegistration("sys-1");

        // Act
        var result = await _sut.RegisterSystemAsync(registration);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task RegisterSystemAsync_DuplicateSystemId_ShouldReturnError()
    {
        // Arrange
        var registration = CreateRegistration("sys-1");
        await _sut.RegisterSystemAsync(registration);

        // Act
        var result = await _sut.RegisterSystemAsync(CreateRegistration("sys-1"));

        // Assert
        result.IsLeft.ShouldBeTrue();
        var error = (EncinaError)result;
        error.Message.ShouldContain("already registered");
    }

    // -- GetSystemAsync --

    [Fact]
    public async Task GetSystemAsync_RegisteredSystem_ShouldReturnRegistration()
    {
        // Arrange
        var registration = CreateRegistration("sys-1");
        await _sut.RegisterSystemAsync(registration);

        // Act
        var result = await _sut.GetSystemAsync("sys-1");

        // Assert
        result.IsRight.ShouldBeTrue();
        var found = (AISystemRegistration)result;
        found.SystemId.ShouldBe("sys-1");
    }

    [Fact]
    public async Task GetSystemAsync_UnregisteredSystem_ShouldReturnError()
    {
        // Act
        var result = await _sut.GetSystemAsync("nonexistent");

        // Assert
        result.IsLeft.ShouldBeTrue();
        var error = (EncinaError)result;
        error.Message.ShouldContain("not registered");
    }

    // -- GetAllSystemsAsync --

    [Fact]
    public async Task GetAllSystemsAsync_EmptyRegistry_ShouldReturnEmptyList()
    {
        // Act
        var result = await _sut.GetAllSystemsAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        var systems = result.Match(Right: s => s, Left: _ => []);
        systems.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAllSystemsAsync_WithRegistrations_ShouldReturnAll()
    {
        // Arrange
        await _sut.RegisterSystemAsync(CreateRegistration("sys-1"));
        await _sut.RegisterSystemAsync(CreateRegistration("sys-2"));

        // Act
        var result = await _sut.GetAllSystemsAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        var systems = result.Match(Right: s => s, Left: _ => []);
        systems.Count.ShouldBe(2);
    }

    // -- GetSystemsByRiskLevelAsync --

    [Fact]
    public async Task GetSystemsByRiskLevelAsync_ShouldFilterByLevel()
    {
        // Arrange
        await _sut.RegisterSystemAsync(CreateRegistration("sys-high", AIRiskLevel.HighRisk));
        await _sut.RegisterSystemAsync(CreateRegistration("sys-min", AIRiskLevel.MinimalRisk));

        // Act
        var result = await _sut.GetSystemsByRiskLevelAsync(AIRiskLevel.HighRisk);

        // Assert
        result.IsRight.ShouldBeTrue();
        var systems = result.Match(Right: s => s, Left: _ => []);
        systems.Count.ShouldBe(1);
        systems[0].SystemId.ShouldBe("sys-high");
    }

    // -- IsRegistered --

    [Fact]
    public async Task IsRegistered_RegisteredSystem_ShouldReturnTrue()
    {
        // Arrange
        await _sut.RegisterSystemAsync(CreateRegistration("sys-1"));

        // Act & Assert
        _sut.IsRegistered("sys-1").ShouldBeTrue();
    }

    [Fact]
    public void IsRegistered_UnregisteredSystem_ShouldReturnFalse()
    {
        _sut.IsRegistered("nonexistent").ShouldBeFalse();
    }

    // -- ReclassifyAsync --

    [Fact]
    public async Task ReclassifyAsync_RegisteredSystem_ShouldUpdateLevel()
    {
        // Arrange
        await _sut.RegisterSystemAsync(CreateRegistration("sys-1", AIRiskLevel.HighRisk));

        // Act
        var result = await _sut.ReclassifyAsync("sys-1", AIRiskLevel.MinimalRisk, "Risk re-assessment");

        // Assert
        result.IsRight.ShouldBeTrue();
        var updated = await _sut.GetSystemAsync("sys-1");
        var reg = (AISystemRegistration)updated;
        reg.RiskLevel.ShouldBe(AIRiskLevel.MinimalRisk);
    }

    [Fact]
    public async Task ReclassifyAsync_UnregisteredSystem_ShouldReturnError()
    {
        // Act
        var result = await _sut.ReclassifyAsync("nonexistent", AIRiskLevel.HighRisk, "test");

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task ReclassifyAsync_WithEncina_ShouldPublishNotification()
    {
        // Arrange
        var encina = Substitute.For<IEncina>();
        var registry = new InMemoryAISystemRegistry(_timeProvider, encina);
        await registry.RegisterSystemAsync(CreateRegistration("sys-1", AIRiskLevel.HighRisk));

        // Act
        await registry.ReclassifyAsync("sys-1", AIRiskLevel.MinimalRisk, "Updated assessment");

        // Assert
        await encina.Received(1).Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>());
    }

    // -- Helper --

    private static AISystemRegistration CreateRegistration(
        string systemId,
        AIRiskLevel riskLevel = AIRiskLevel.HighRisk) => new()
        {
            SystemId = systemId,
            Name = $"System {systemId}",
            Category = AISystemCategory.EmploymentWorkersManagement,
            RiskLevel = riskLevel,
            RegisteredAtUtc = DateTimeOffset.UtcNow
        };
}
