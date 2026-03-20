#pragma warning disable CA1859 // Contract tests intentionally use interface types to verify contracts

using Encina.Compliance.AIAct;
using Encina.Compliance.AIAct.Abstractions;
using Encina.Compliance.AIAct.Model;
using LanguageExt;
using Microsoft.Extensions.Time.Testing;

namespace Encina.ContractTests.Compliance.AIAct;

/// <summary>
/// Contract tests for Encina.Compliance.AIAct public interfaces.
/// Verifies that implementations conform to interface contracts.
/// </summary>
public class AIActContractTests
{
    private static readonly DateTimeOffset FixedTime =
        new(2026, 3, 20, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _timeProvider = new();

    // -- IAISystemRegistry contract --

    [Fact]
    public async Task IAISystemRegistry_InMemory_RegisterAsync_ReturnsRight()
    {
        IAISystemRegistry registry = new InMemoryAISystemRegistry(_timeProvider);
        var registration = CreateRegistration("sys-1");

        var result = await registry.RegisterSystemAsync(registration);

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task IAISystemRegistry_InMemory_GetAllAsync_ReturnsRight()
    {
        IAISystemRegistry registry = new InMemoryAISystemRegistry(_timeProvider);

        var result = await registry.GetAllSystemsAsync();

        result.IsRight.ShouldBeTrue();
        var systems = result.Match(
            Right: s => s,
            Left: _ => (IReadOnlyList<AISystemRegistration>)[]);
        systems.ShouldNotBeNull();
    }

    [Fact]
    public async Task IAISystemRegistry_InMemory_RegisterThenGet_RoundTrips()
    {
        IAISystemRegistry registry = new InMemoryAISystemRegistry(_timeProvider);
        var registration = CreateRegistration("sys-1");

        await registry.RegisterSystemAsync(registration);
        var result = await registry.GetSystemAsync("sys-1");

        result.IsRight.ShouldBeTrue();
        var found = (AISystemRegistration)result;
        found.SystemId.ShouldBe("sys-1");
        found.Name.ShouldBe(registration.Name);
    }

    [Fact]
    public async Task IAISystemRegistry_InMemory_GetUnregistered_ReturnsLeft()
    {
        IAISystemRegistry registry = new InMemoryAISystemRegistry(_timeProvider);

        var result = await registry.GetSystemAsync("nonexistent");

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void IAISystemRegistry_InMemory_IsRegistered_ReturnsFalseForUnknown()
    {
        IAISystemRegistry registry = new InMemoryAISystemRegistry(_timeProvider);

        registry.IsRegistered("nonexistent").ShouldBeFalse();
    }

    [Fact]
    public async Task IAISystemRegistry_InMemory_IsRegistered_ReturnsTrueAfterRegister()
    {
        IAISystemRegistry registry = new InMemoryAISystemRegistry(_timeProvider);
        await registry.RegisterSystemAsync(CreateRegistration("sys-1"));

        registry.IsRegistered("sys-1").ShouldBeTrue();
    }

    [Fact]
    public async Task IAISystemRegistry_InMemory_GetByRiskLevel_ReturnsFiltered()
    {
        IAISystemRegistry registry = new InMemoryAISystemRegistry(_timeProvider);
        await registry.RegisterSystemAsync(CreateRegistration("high-1", AIRiskLevel.HighRisk));
        await registry.RegisterSystemAsync(CreateRegistration("min-1", AIRiskLevel.MinimalRisk));

        var result = await registry.GetSystemsByRiskLevelAsync(AIRiskLevel.HighRisk);

        result.IsRight.ShouldBeTrue();
        var systems = result.Match(Right: s => s, Left: _ => []);
        systems.Count.ShouldBe(1);
        systems[0].SystemId.ShouldBe("high-1");
    }

    // -- IAIActClassifier contract --

    [Fact]
    public async Task IAIActClassifier_Default_ClassifyRegisteredSystem_ReturnsRight()
    {
        var registry = new InMemoryAISystemRegistry(_timeProvider);
        await registry.RegisterSystemAsync(CreateRegistration("sys-1", AIRiskLevel.HighRisk));
        IAIActClassifier classifier = new DefaultAIActClassifier(registry, _timeProvider);

        var result = await classifier.ClassifySystemAsync("sys-1");

        result.IsRight.ShouldBeTrue();
        var level = (AIRiskLevel)result;
        level.ShouldBe(AIRiskLevel.HighRisk);
    }

    [Fact]
    public async Task IAIActClassifier_Default_EvaluateComplianceAsync_ReturnsRight()
    {
        var registry = new InMemoryAISystemRegistry(_timeProvider);
        await registry.RegisterSystemAsync(CreateRegistration("sys-1", AIRiskLevel.HighRisk));
        IAIActClassifier classifier = new DefaultAIActClassifier(registry, _timeProvider);

        var result = await classifier.EvaluateComplianceAsync("sys-1");

        result.IsRight.ShouldBeTrue();
        var compliance = (AIActComplianceResult)result;
        compliance.SystemId.ShouldBe("sys-1");
    }

    // -- IHumanOversightEnforcer contract --

    [Fact]
    public async Task IHumanOversightEnforcer_Default_RecordAndRetrieve_RoundTrips()
    {
        IHumanOversightEnforcer enforcer = new DefaultHumanOversightEnforcer();
        var decision = CreateDecision();

        await enforcer.RecordHumanDecisionAsync(decision);
        var result = await enforcer.HasHumanApprovalAsync(decision.DecisionId);

        result.IsRight.ShouldBeTrue();
        var exists = (bool)result;
        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task IHumanOversightEnforcer_Default_HasApproval_FalseForUnknown()
    {
        IHumanOversightEnforcer enforcer = new DefaultHumanOversightEnforcer();

        var result = await enforcer.HasHumanApprovalAsync(Guid.NewGuid());

        result.IsRight.ShouldBeTrue();
        var exists = (bool)result;
        exists.ShouldBeFalse();
    }

    // -- AIActComplianceResult factory contract --

    [Fact]
    public void AIActComplianceResult_DefaultViolations_AreEmpty()
    {
        var result = new AIActComplianceResult
        {
            SystemId = "sys-1",
            RiskLevel = AIRiskLevel.MinimalRisk,
            IsProhibited = false,
            RequiresHumanOversight = false,
            RequiresTransparency = false,
            EvaluatedAtUtc = FixedTime
        };

        result.Violations.Count.ShouldBe(0);
    }

    // -- AIRiskLevel enum contract --

    [Fact]
    public void AIRiskLevel_ShouldHaveExactlyFourValues()
    {
        Enum.GetValues<AIRiskLevel>().Length.ShouldBe(4,
            "EU AI Act defines four risk tiers: Prohibited, HighRisk, LimitedRisk, MinimalRisk");
    }

    // -- Helpers --

    private static AISystemRegistration CreateRegistration(
        string systemId,
        AIRiskLevel riskLevel = AIRiskLevel.HighRisk) => new()
    {
        SystemId = systemId,
        Name = $"System {systemId}",
        Category = AISystemCategory.EmploymentWorkersManagement,
        RiskLevel = riskLevel,
        RegisteredAtUtc = FixedTime
    };

    private static HumanDecisionRecord CreateDecision() => new()
    {
        DecisionId = Guid.NewGuid(),
        SystemId = "sys-1",
        ReviewerId = "reviewer-1",
        ReviewedAtUtc = FixedTime,
        Decision = "approved",
        Rationale = "All checks passed"
    };
}
