using Encina.Compliance.AIAct;
using Encina.Compliance.AIAct.Model;

using FsCheck;
using FsCheck.Xunit;

using Microsoft.Extensions.Time.Testing;

namespace Encina.PropertyTests.Compliance.AIAct;

/// <summary>
/// Property-based tests for the three AI Act validators: <see cref="AIActOptionsValidator"/>,
/// <see cref="DefaultDataQualityValidator"/> and <see cref="DefaultAIActComplianceValidator"/>.
/// </summary>
[Trait("Category", "Property")]
[Trait("Feature", "AIAct")]
public sealed class AIActValidatorPropertyTests
{
    private static readonly AIActEnforcementMode[] Modes = Enum.GetValues<AIActEnforcementMode>();

    private static readonly System.Reflection.Assembly[] Assemblies =
        [typeof(object).Assembly, typeof(AIActOptions).Assembly, typeof(AIActValidatorPropertyTests).Assembly];

    private static AIActOptions BuildOptions(byte mode, bool autoRegister, byte assemblyCount)
    {
        var options = new AIActOptions
        {
            EnforcementMode = Modes[mode % Modes.Length],
            AutoRegisterFromAttributes = autoRegister
        };

        for (var i = 0; i < assemblyCount % (Assemblies.Length + 1); i++)
        {
            options.AssembliesToScan.Add(Assemblies[i]);
        }

        return options;
    }

    // -- AIActOptionsValidator --

    [Property(MaxTest = 100)]
    public bool OptionsValidator_Fails_ExactlyWhenAssembliesAreListedButAutoRegistrationIsOffAndEnforcementIsOn(
        byte mode, bool autoRegister, byte assemblyCount)
    {
        var options = BuildOptions(mode, autoRegister, assemblyCount);

        var result = new AIActOptionsValidator().Validate(null, options);

        var expectedFailure = options.EnforcementMode != AIActEnforcementMode.Disabled
            && !options.AutoRegisterFromAttributes
            && options.AssembliesToScan.Count > 0;

        return result.Failed == expectedFailure && result.Succeeded == !expectedFailure;
    }

    [Property(MaxTest = 50)]
    public bool OptionsValidator_DisabledEnforcement_AlwaysSucceeds(bool autoRegister, byte assemblyCount)
    {
        var options = BuildOptions((byte)Array.IndexOf(Modes, AIActEnforcementMode.Disabled), autoRegister, assemblyCount);

        return new AIActOptionsValidator().Validate("any-name", options).Succeeded;
    }

    [Property(MaxTest = 50)]
    public bool OptionsValidator_AutoRegistrationOn_AlwaysSucceeds(byte mode, byte assemblyCount)
    {
        var options = BuildOptions(mode, autoRegister: true, assemblyCount);

        return new AIActOptionsValidator().Validate(null, options).Succeeded;
    }

    [Property(MaxTest = 50)]
    public bool OptionsValidator_Failure_NamesTheOffendingProperties(byte mode, byte assemblyCount)
    {
        var options = BuildOptions(mode, autoRegister: false, assemblyCount);
        var result = new AIActOptionsValidator().Validate(null, options);

        return result.Failed
            ? result.FailureMessage!.Contains(nameof(AIActOptions.AssembliesToScan), StringComparison.Ordinal)
              && result.FailureMessage.Contains(nameof(AIActOptions.AutoRegisterFromAttributes), StringComparison.Ordinal)
            : result.Succeeded;
    }

    // -- DefaultDataQualityValidator --

    [Property(MaxTest = 100)]
    public bool DataQuality_ValidateTrainingData_EchoesDatasetIdAndMeetsRequirements(NonEmptyString datasetId)
    {
        var time = new FakeTimeProvider();
        var sut = new DefaultDataQualityValidator(time);

        var result = sut.ValidateTrainingDataAsync(datasetId.Get).AsTask().Result;

        return result.Match(
            Right: report => report.DatasetId == datasetId.Get
                && report.MeetsAIActRequirements
                && report.CompletenessScore is >= 0.9 and <= 1.0
                && report.AccuracyScore is >= 0.85 and <= 1.0
                && report.ConsistencyScore is >= 0.9 and <= 1.0
                && report.IdentifiedGaps.Count == 0
                && report.BiasIndicators.Count == 0
                && report.EvaluatedAtUtc == time.GetUtcNow(),
            Left: _ => false);
    }

    [Property(MaxTest = 100)]
    public bool DataQuality_DetectBias_PreservesProtectedAttributesAndReportsFairness(
        NonEmptyString datasetId, NonEmptyString[] attributes)
    {
        var time = new FakeTimeProvider();
        var sut = new DefaultDataQualityValidator(time);
        IReadOnlyList<string> protectedAttributes = [.. attributes.Select(a => a.Get)];

        var result = sut.DetectBiasAsync(datasetId.Get, protectedAttributes).AsTask().Result;

        return result.Match(
            Right: report => report.DatasetId == datasetId.Get
                && report.ProtectedAttributes.SequenceEqual(protectedAttributes)
                && report.OverallFairness
                && report.Indicators.Count == 0
                && report.EvaluatedAtUtc == time.GetUtcNow(),
            Left: _ => false);
    }

    [Property(MaxTest = 50)]
    public bool DataQuality_Timestamp_FollowsTheInjectedClock(PositiveInt seconds)
    {
        var time = new FakeTimeProvider();
        var sut = new DefaultDataQualityValidator(time);

        time.Advance(TimeSpan.FromSeconds(seconds.Get));
        var result = sut.ValidateTrainingDataAsync("ds").AsTask().Result;

        return result.Match(Right: r => r.EvaluatedAtUtc == time.GetUtcNow(), Left: _ => false);
    }

    // -- DefaultAIActComplianceValidator (explicit system id only; see #2512 for the identity-less case) --

    private static DefaultAIActComplianceValidator CreateComplianceValidator(
        InMemoryAISystemRegistry registry, FakeTimeProvider time)
        => new(new DefaultAIActClassifier(registry, time), registry, new DefaultHumanOversightEnforcer(), time);

    [Property(MaxTest = 50)]
    public bool ComplianceValidator_UnregisteredSystem_IsAlwaysHighRiskWithOneViolationNamingIt(PositiveInt n)
    {
        var time = new FakeTimeProvider();
        var registry = new InMemoryAISystemRegistry(time);
        var systemId = $"unregistered-{n.Get}";

        var result = CreateComplianceValidator(registry, time)
            .ValidateAsync(new object(), systemId).AsTask().Result;

        return result.Match(
            Right: r => r.SystemId == systemId
                && r.RiskLevel == AIRiskLevel.HighRisk
                && !r.IsProhibited
                && r.RequiresHumanOversight
                && r.RequiresTransparency
                && r.Violations.Count == 1
                && r.Violations[0].Contains(systemId, StringComparison.Ordinal),
            Left: _ => false);
    }

    [Property(MaxTest = 50)]
    public bool ComplianceValidator_RegisteredNonProhibitedSystem_KeepsItsRiskLevelAndHasNoViolations(byte level)
    {
        var levels = new[] { AIRiskLevel.MinimalRisk, AIRiskLevel.LimitedRisk, AIRiskLevel.HighRisk };
        var riskLevel = levels[level % levels.Length];
        var time = new FakeTimeProvider();
        var registry = new InMemoryAISystemRegistry(time);
        registry.RegisterSystemAsync(new AISystemRegistration
        {
            SystemId = "registered",
            Name = "Registered",
            Category = AISystemCategory.EmploymentWorkersManagement,
            RiskLevel = riskLevel,
            RegisteredAtUtc = time.GetUtcNow()
        }).AsTask().Result.IsRight.ShouldBeTrue();

        var result = CreateComplianceValidator(registry, time)
            .ValidateAsync(new object(), "registered").AsTask().Result;

        return result.Match(
            Right: r => r.RiskLevel == riskLevel
                && !r.IsProhibited
                && r.Violations.Count == 0
                && r.RequiresHumanOversight == (riskLevel == AIRiskLevel.HighRisk)
                && r.RequiresTransparency == (riskLevel is AIRiskLevel.HighRisk or AIRiskLevel.LimitedRisk),
            Left: _ => false);
    }

    [Property(MaxTest = 30)]
    public bool ComplianceValidator_AnyProhibitedPractice_MakesTheSystemProhibitedWithOneViolationEach(byte practiceCount)
    {
        var practices = Enum.GetValues<ProhibitedPractice>();
        var count = 1 + (practiceCount % practices.Length);
        var time = new FakeTimeProvider();
        var registry = new InMemoryAISystemRegistry(time);
        registry.RegisterSystemAsync(new AISystemRegistration
        {
            SystemId = "banned",
            Name = "Banned",
            Category = AISystemCategory.EmploymentWorkersManagement,
            RiskLevel = AIRiskLevel.HighRisk,
            RegisteredAtUtc = time.GetUtcNow(),
            ProhibitedPractices = [.. practices.Take(count)]
        }).AsTask().Result.IsRight.ShouldBeTrue();

        var result = CreateComplianceValidator(registry, time)
            .ValidateAsync(new object(), "banned").AsTask().Result;

        return result.Match(
            Right: r => r.IsProhibited
                && r.RiskLevel == AIRiskLevel.Prohibited
                && r.Violations.Count == count,
            Left: _ => false);
    }
}
