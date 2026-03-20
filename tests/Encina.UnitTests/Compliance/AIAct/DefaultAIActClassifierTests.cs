using Encina.Compliance.AIAct;
using Encina.Compliance.AIAct.Abstractions;
using Encina.Compliance.AIAct.Model;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Time.Testing;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.AIAct;

/// <summary>
/// Unit tests for <see cref="DefaultAIActClassifier"/>.
/// </summary>
public class DefaultAIActClassifierTests
{
    private readonly FakeTimeProvider _timeProvider = new();
    private readonly InMemoryAISystemRegistry _registry;
    private readonly DefaultAIActClassifier _sut;

    public DefaultAIActClassifierTests()
    {
        _registry = new InMemoryAISystemRegistry(_timeProvider);
        _sut = new DefaultAIActClassifier(_registry, _timeProvider);
    }

    // -- ClassifySystemAsync --

    [Fact]
    public async Task ClassifySystemAsync_HighRiskSystem_ShouldReturnHighRisk()
    {
        // Arrange
        await RegisterSystem("sys-1", AIRiskLevel.HighRisk);

        // Act
        var result = await _sut.ClassifySystemAsync("sys-1");

        // Assert
        result.IsRight.Should().BeTrue();
        var level = (AIRiskLevel)result;
        level.Should().Be(AIRiskLevel.HighRisk);
    }

    [Fact]
    public async Task ClassifySystemAsync_SystemWithProhibitedPractices_ShouldReturnProhibited()
    {
        // Arrange
        await RegisterSystem("sys-1", AIRiskLevel.HighRisk,
            [ProhibitedPractice.SocialScoring]);

        // Act
        var result = await _sut.ClassifySystemAsync("sys-1");

        // Assert
        result.IsRight.Should().BeTrue();
        var level = (AIRiskLevel)result;
        level.Should().Be(AIRiskLevel.Prohibited);
    }

    [Fact]
    public async Task ClassifySystemAsync_UnregisteredSystem_ShouldReturnError()
    {
        // Act
        var result = await _sut.ClassifySystemAsync("nonexistent");

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    // -- IsProhibitedAsync --

    [Fact]
    public async Task IsProhibitedAsync_ProhibitedSystem_ShouldReturnTrue()
    {
        // Arrange
        await RegisterSystem("sys-1", AIRiskLevel.Prohibited);

        // Act
        var result = await _sut.IsProhibitedAsync("sys-1");

        // Assert
        result.IsRight.Should().BeTrue();
        var prohibited = (bool)result;
        prohibited.Should().BeTrue();
    }

    [Fact]
    public async Task IsProhibitedAsync_NonProhibitedSystem_ShouldReturnFalse()
    {
        // Arrange
        await RegisterSystem("sys-1", AIRiskLevel.MinimalRisk);

        // Act
        var result = await _sut.IsProhibitedAsync("sys-1");

        // Assert
        result.IsRight.Should().BeTrue();
        var prohibited = (bool)result;
        prohibited.Should().BeFalse();
    }

    // -- EvaluateComplianceAsync --

    [Fact]
    public async Task EvaluateComplianceAsync_HighRiskSystem_ShouldRequireOversight()
    {
        // Arrange
        await RegisterSystem("sys-1", AIRiskLevel.HighRisk);

        // Act
        var result = await _sut.EvaluateComplianceAsync("sys-1");

        // Assert
        result.IsRight.Should().BeTrue();
        var compliance = (AIActComplianceResult)result;
        compliance.RequiresHumanOversight.Should().BeTrue();
        compliance.RequiresTransparency.Should().BeTrue();
        compliance.IsProhibited.Should().BeFalse();
        compliance.Violations.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateComplianceAsync_ProhibitedSystem_ShouldHaveViolations()
    {
        // Arrange
        await RegisterSystem("sys-1", AIRiskLevel.HighRisk,
            [ProhibitedPractice.SocialScoring, ProhibitedPractice.SubliminalManipulation]);

        // Act
        var result = await _sut.EvaluateComplianceAsync("sys-1");

        // Assert
        result.IsRight.Should().BeTrue();
        var compliance = (AIActComplianceResult)result;
        compliance.IsProhibited.Should().BeTrue();
        compliance.RiskLevel.Should().Be(AIRiskLevel.Prohibited);
        compliance.Violations.Should().HaveCount(2);
        compliance.Violations.Should().AllSatisfy(v => v.Should().Contain("Art. 5"));
    }

    [Fact]
    public async Task EvaluateComplianceAsync_MinimalRiskSystem_ShouldNotRequireOversight()
    {
        // Arrange
        await RegisterSystem("sys-1", AIRiskLevel.MinimalRisk);

        // Act
        var result = await _sut.EvaluateComplianceAsync("sys-1");

        // Assert
        result.IsRight.Should().BeTrue();
        var compliance = (AIActComplianceResult)result;
        compliance.RequiresHumanOversight.Should().BeFalse();
        compliance.RequiresTransparency.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateComplianceAsync_LimitedRiskSystem_ShouldRequireTransparencyOnly()
    {
        // Arrange
        await RegisterSystem("sys-1", AIRiskLevel.LimitedRisk);

        // Act
        var result = await _sut.EvaluateComplianceAsync("sys-1");

        // Assert
        result.IsRight.Should().BeTrue();
        var compliance = (AIActComplianceResult)result;
        compliance.RequiresHumanOversight.Should().BeFalse();
        compliance.RequiresTransparency.Should().BeTrue();
    }

    // -- Helper --

    private async Task RegisterSystem(
        string systemId,
        AIRiskLevel riskLevel,
        IReadOnlyList<ProhibitedPractice>? prohibitedPractices = null)
    {
        var reg = new AISystemRegistration
        {
            SystemId = systemId,
            Name = $"System {systemId}",
            Category = AISystemCategory.EmploymentWorkersManagement,
            RiskLevel = riskLevel,
            RegisteredAtUtc = DateTimeOffset.UtcNow,
            ProhibitedPractices = prohibitedPractices ?? []
        };
        await _registry.RegisterSystemAsync(reg);
    }
}
