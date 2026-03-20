using Encina.Compliance.AIAct;
using Encina.Compliance.AIAct.Model;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Microsoft.Extensions.Time.Testing;

namespace Encina.PropertyTests.Compliance.AIAct;

/// <summary>
/// Property-based tests for Encina.Compliance.AIAct invariants.
/// </summary>
public class AIActPropertyTests
{
    // -- InMemoryAISystemRegistry invariants --

    [Property(MaxTest = 50)]
    public bool Registry_RegisterThenGet_AlwaysReturnsRegistered(NonEmptyString name)
    {
        var registry = new InMemoryAISystemRegistry(new FakeTimeProvider());
        var systemId = $"sys-{Guid.NewGuid():N}";
        var registration = new AISystemRegistration
        {
            SystemId = systemId,
            Name = name.Get,
            Category = AISystemCategory.EmploymentWorkersManagement,
            RiskLevel = AIRiskLevel.HighRisk,
            RegisteredAtUtc = DateTimeOffset.UtcNow
        };

        var registerResult = registry.RegisterSystemAsync(registration).AsTask().Result;
        if (!registerResult.IsRight) return false;

        var getResult = registry.GetSystemAsync(systemId).AsTask().Result;
        if (!getResult.IsRight) return false;

        var found = (AISystemRegistration)getResult;
        return found.Name == name.Get && found.SystemId == systemId;
    }

    [Property(MaxTest = 20)]
    public Property Registry_GetAll_CountMatchesRegistrations()
    {
        return Prop.ForAll(
            Gen.Choose(1, 10).ToArbitrary(),
            count =>
            {
                var registry = new InMemoryAISystemRegistry(new FakeTimeProvider());
                var registered = 0;

                for (var i = 0; i < count; i++)
                {
                    var reg = new AISystemRegistration
                    {
                        SystemId = $"sys-{i}",
                        Name = $"System {i}",
                        Category = AISystemCategory.EmploymentWorkersManagement,
                        RiskLevel = AIRiskLevel.HighRisk,
                        RegisteredAtUtc = DateTimeOffset.UtcNow
                    };

                    var result = registry.RegisterSystemAsync(reg).AsTask().Result;
                    if (result.IsRight) registered++;
                }

                var allResult = registry.GetAllSystemsAsync().AsTask().Result;
                allResult.IsRight.ShouldBeTrue();
                var all = allResult.Match(
                    Right: systems => systems,
                    Left: _ => (IReadOnlyList<AISystemRegistration>)[]);
                all.Count.ShouldBe(registered);
            });
    }

    [Property(MaxTest = 30)]
    public bool Registry_DuplicateRegistration_AlwaysFails(NonEmptyString name)
    {
        var registry = new InMemoryAISystemRegistry(new FakeTimeProvider());
        var reg = new AISystemRegistration
        {
            SystemId = "fixed-id",
            Name = name.Get,
            Category = AISystemCategory.EmploymentWorkersManagement,
            RiskLevel = AIRiskLevel.HighRisk,
            RegisteredAtUtc = DateTimeOffset.UtcNow
        };

        var first = registry.RegisterSystemAsync(reg).AsTask().Result;
        var second = registry.RegisterSystemAsync(reg).AsTask().Result;

        return first.IsRight && second.IsLeft;
    }

    // -- AIActComplianceResult invariants --

    [Property(MaxTest = 50)]
    public bool ComplianceResult_RecordEquality_HoldsForSameValues()
    {
        var time = DateTimeOffset.UtcNow;
        var a = new AIActComplianceResult
        {
            SystemId = "sys-1",
            RiskLevel = AIRiskLevel.HighRisk,
            IsProhibited = false,
            RequiresHumanOversight = true,
            RequiresTransparency = true,
            EvaluatedAtUtc = time
        };
        var b = new AIActComplianceResult
        {
            SystemId = "sys-1",
            RiskLevel = AIRiskLevel.HighRisk,
            IsProhibited = false,
            RequiresHumanOversight = true,
            RequiresTransparency = true,
            EvaluatedAtUtc = time
        };

        return a == b;
    }

    // -- HumanDecisionRecord invariants --

    [Property(MaxTest = 50)]
    public bool DecisionRecord_RoundTrip_AlwaysReturnsRecorded(NonEmptyString reviewerId, NonEmptyString decision)
    {
        var enforcer = new DefaultHumanOversightEnforcer();
        var decisionId = Guid.NewGuid();
        var record = new HumanDecisionRecord
        {
            DecisionId = decisionId,
            SystemId = "sys-1",
            ReviewerId = reviewerId.Get,
            ReviewedAtUtc = DateTimeOffset.UtcNow,
            Decision = decision.Get,
            Rationale = "Test"
        };

        var recordResult = enforcer.RecordHumanDecisionAsync(record).AsTask().Result;
        if (!recordResult.IsRight) return false;

        var hasResult = enforcer.HasHumanApprovalAsync(decisionId).AsTask().Result;
        if (!hasResult.IsRight) return false;

        return (bool)hasResult;
    }

    // -- DefaultAIActClassifier invariants --

    [Property(MaxTest = 20)]
    public Property Classifier_ProhibitedPractices_AlwaysClassifyAsProhibited()
    {
        var practicesGen = Gen.Elements(
                ProhibitedPractice.SocialScoring,
                ProhibitedPractice.SubliminalManipulation,
                ProhibitedPractice.PredictivePolicing,
                ProhibitedPractice.UntargetedFacialScraping)
            .ArrayOf()
            .Where(arr => arr.Length > 0);

        return Prop.ForAll(
            Arb.From(practicesGen),
            practices =>
            {
                var timeProvider = new FakeTimeProvider();
                var registry = new InMemoryAISystemRegistry(timeProvider);
                var registration = new AISystemRegistration
                {
                    SystemId = "test-sys",
                    Name = "Test",
                    Category = AISystemCategory.LawEnforcement,
                    RiskLevel = AIRiskLevel.HighRisk,
                    ProhibitedPractices = practices.Distinct().ToList(),
                    RegisteredAtUtc = DateTimeOffset.UtcNow
                };

                registry.RegisterSystemAsync(registration).AsTask().Wait();

                var classifier = new DefaultAIActClassifier(registry, timeProvider);
                var result = classifier.ClassifySystemAsync("test-sys").AsTask().Result;

                result.IsRight.ShouldBeTrue();
                var level = (AIRiskLevel)result;
                level.ShouldBe(AIRiskLevel.Prohibited);
            });
    }

    // -- Enum invariants --

    [Fact]
    public void AIRiskLevel_ShouldHaveExactlyFourValues()
    {
        Enum.GetValues<AIRiskLevel>().Length.ShouldBe(4,
            "EU AI Act risk pyramid has exactly four tiers");
    }

    [Fact]
    public void AIActEnforcementMode_ShouldHaveExactlyThreeValues()
    {
        Enum.GetValues<AIActEnforcementMode>().Length.ShouldBe(3,
            "Three enforcement modes: Block, Warn, Disabled");
    }

    [Fact]
    public void AISystemCategory_ShouldHaveExpectedCount()
    {
        Enum.GetValues<AISystemCategory>().Length.ShouldBeGreaterThanOrEqualTo(8,
            "Annex III defines multiple high-risk categories");
    }

    [Fact]
    public void ProhibitedPractice_ShouldHaveExpectedCount()
    {
        Enum.GetValues<ProhibitedPractice>().Length.ShouldBeGreaterThanOrEqualTo(4,
            "Art. 5 defines multiple prohibited practices");
    }

    [Fact]
    public void TransparencyObligationType_ShouldHaveExpectedCount()
    {
        Enum.GetValues<TransparencyObligationType>().Length.ShouldBeGreaterThanOrEqualTo(3,
            "Art. 50 defines multiple transparency obligation types");
    }
}
