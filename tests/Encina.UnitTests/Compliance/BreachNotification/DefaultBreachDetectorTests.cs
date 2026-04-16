#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.BreachNotification;
using Encina.Compliance.BreachNotification.Detection;
using Encina.Compliance.BreachNotification.Model;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.BreachNotification;

/// <summary>
/// Unit tests for <see cref="DefaultBreachDetector"/>.
/// </summary>
public class DefaultBreachDetectorTests
{
    private static SecurityEvent CreateTestEvent(
        SecurityEventType eventType = SecurityEventType.UnauthorizedAccess,
        string source = "test-source",
        string description = "test description")
    {
        return SecurityEvent.Create(eventType, source, description, DateTimeOffset.UtcNow);
    }

    private static DefaultBreachDetector CreateSut(
        IEnumerable<IBreachDetectionRule>? rules = null)
    {
        return new DefaultBreachDetector(
            rules ?? [],
            NullLogger<DefaultBreachDetector>.Instance);
    }

    private static IBreachDetectionRule CreateMatchingRule(
        string name = "TestRule",
        BreachSeverity severity = BreachSeverity.High,
        SecurityEventType matchType = SecurityEventType.UnauthorizedAccess)
    {
        var rule = Substitute.For<IBreachDetectionRule>();
        rule.Name.Returns(name);
        rule.EvaluateAsync(Arg.Any<SecurityEvent>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var evt = callInfo.Arg<SecurityEvent>();
                if (evt.EventType == matchType)
                {
                    var potentialBreach = new PotentialBreach
                    {
                        DetectionRuleName = name,
                        Severity = severity,
                        Description = $"Breach detected by {name}",
                        SecurityEvent = evt,
                        DetectedAtUtc = DateTimeOffset.UtcNow
                    };
                    return ValueTask.FromResult(
                        Right<EncinaError, Option<PotentialBreach>>(Some(potentialBreach)));
                }

                return ValueTask.FromResult(
                    Right<EncinaError, Option<PotentialBreach>>(None));
            });
        return rule;
    }

    private static IBreachDetectionRule CreateNonMatchingRule(string name = "NonMatchingRule")
    {
        var rule = Substitute.For<IBreachDetectionRule>();
        rule.Name.Returns(name);
        rule.EvaluateAsync(Arg.Any<SecurityEvent>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, Option<PotentialBreach>>(None)));
        return rule;
    }

    #region DetectAsync Tests

    [Fact]
    public async Task DetectAsync_NoRules_ReturnsEmptyList()
    {
        // Arrange
        var sut = CreateSut(rules: []);
        var securityEvent = CreateTestEvent();

        // Act
        var result = await sut.DetectAsync(securityEvent);

        // Assert
        result.IsRight.ShouldBeTrue();
        var breaches = result.Match(r => r, _ => []);
        breaches.ShouldBeEmpty();
    }

    [Fact]
    public async Task DetectAsync_MatchingRule_ReturnsPotentialBreach()
    {
        // Arrange
        var rule = CreateMatchingRule(
            name: "UnauthorizedAccessRule",
            matchType: SecurityEventType.UnauthorizedAccess);
        var sut = CreateSut(rules: [rule]);
        var securityEvent = CreateTestEvent(SecurityEventType.UnauthorizedAccess);

        // Act
        var result = await sut.DetectAsync(securityEvent);

        // Assert
        result.IsRight.ShouldBeTrue();
        var breaches = result.Match(r => r, _ => []);
        breaches.Count.ShouldBe(1);
        breaches[0].DetectionRuleName.ShouldBe("UnauthorizedAccessRule");
    }

    [Fact]
    public async Task DetectAsync_NonMatchingRule_ReturnsEmptyList()
    {
        // Arrange
        var rule = CreateNonMatchingRule();
        var sut = CreateSut(rules: [rule]);
        var securityEvent = CreateTestEvent();

        // Act
        var result = await sut.DetectAsync(securityEvent);

        // Assert
        result.IsRight.ShouldBeTrue();
        var breaches = result.Match(r => r, _ => []);
        breaches.ShouldBeEmpty();
    }

    [Fact]
    public async Task DetectAsync_RuleThrowsException_ContinuesWithOtherRules()
    {
        // Arrange
        var throwingRule = Substitute.For<IBreachDetectionRule>();
        throwingRule.Name.Returns("ThrowingRule");
        throwingRule.EvaluateAsync(Arg.Any<SecurityEvent>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Rule crashed"));

        var workingRule = CreateMatchingRule(
            name: "WorkingRule",
            matchType: SecurityEventType.UnauthorizedAccess);

        var sut = CreateSut(rules: [throwingRule, workingRule]);
        var securityEvent = CreateTestEvent(SecurityEventType.UnauthorizedAccess);

        // Act
        var result = await sut.DetectAsync(securityEvent);

        // Assert
        result.IsRight.ShouldBeTrue();
        var breaches = result.Match(r => r, _ => []);
        breaches.Count.ShouldBe(1);
        breaches[0].DetectionRuleName.ShouldBe("WorkingRule");
    }

    [Fact]
    public async Task DetectAsync_MultipleMatchingRules_ReturnsAllBreaches()
    {
        // Arrange
        var rule1 = CreateMatchingRule(
            name: "Rule1",
            matchType: SecurityEventType.UnauthorizedAccess);
        var rule2 = CreateMatchingRule(
            name: "Rule2",
            matchType: SecurityEventType.UnauthorizedAccess);
        var sut = CreateSut(rules: [rule1, rule2]);
        var securityEvent = CreateTestEvent(SecurityEventType.UnauthorizedAccess);

        // Act
        var result = await sut.DetectAsync(securityEvent);

        // Assert
        result.IsRight.ShouldBeTrue();
        var breaches = result.Match(r => r, _ => []);
        breaches.Count.ShouldBe(2);
        var ruleNames = breaches.Select(b => b.DetectionRuleName).ToList();
        ruleNames.ShouldContain("Rule1");
        ruleNames.ShouldContain("Rule2");
    }

    [Fact]
    public async Task DetectAsync_RuleReturnsLeft_HandlesGracefully()
    {
        // Arrange
        var errorRule = Substitute.For<IBreachDetectionRule>();
        errorRule.Name.Returns("ErrorRule");
        errorRule.EvaluateAsync(Arg.Any<SecurityEvent>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Option<PotentialBreach>>>(
                Left<EncinaError, Option<PotentialBreach>>(
                    EncinaErrors.Create(code: "rule.error", message: "Rule evaluation failed"))));

        var workingRule = CreateMatchingRule(
            name: "WorkingRule",
            matchType: SecurityEventType.UnauthorizedAccess);

        var sut = CreateSut(rules: [errorRule, workingRule]);
        var securityEvent = CreateTestEvent(SecurityEventType.UnauthorizedAccess);

        // Act
        var result = await sut.DetectAsync(securityEvent);

        // Assert
        result.IsRight.ShouldBeTrue();
        var breaches = result.Match(r => r, _ => []);
        breaches.Count.ShouldBe(1);
        breaches[0].DetectionRuleName.ShouldBe("WorkingRule");
    }

    #endregion

    #region RegisterDetectionRule Tests

    [Fact]
    public async Task RegisterDetectionRule_NewRule_AddsSuccessfully()
    {
        // Arrange
        var sut = CreateSut(rules: []);
        var rule = CreateMatchingRule(
            name: "DynamicRule",
            matchType: SecurityEventType.DataExfiltration);

        // Act
        sut.RegisterDetectionRule(rule);

        // Assert — verify by detecting with it
        var securityEvent = CreateTestEvent(SecurityEventType.DataExfiltration);
        var result = await sut.DetectAsync(securityEvent);
        result.IsRight.ShouldBeTrue();
        var breaches = result.Match(r => r, _ => []);
        breaches.Count.ShouldBe(1);
        breaches[0].DetectionRuleName.ShouldBe("DynamicRule");
    }

    [Fact]
    public async Task RegisterDetectionRule_DuplicateName_ReplacesExisting()
    {
        // Arrange
        var originalRule = CreateMatchingRule(
            name: "SameNameRule",
            severity: BreachSeverity.Low,
            matchType: SecurityEventType.UnauthorizedAccess);
        var sut = CreateSut(rules: [originalRule]);

        var replacementRule = CreateMatchingRule(
            name: "SameNameRule",
            severity: BreachSeverity.Critical,
            matchType: SecurityEventType.UnauthorizedAccess);

        // Act
        sut.RegisterDetectionRule(replacementRule);

        // Assert
        var securityEvent = CreateTestEvent(SecurityEventType.UnauthorizedAccess);
        var result = await sut.DetectAsync(securityEvent);
        result.IsRight.ShouldBeTrue();
        var breaches = result.Match(r => r, _ => []);
        breaches.Count.ShouldBe(1);

        // Only one rule with that name should exist (replacement, not duplicate)
        var rulesResult = await sut.GetRegisteredRulesAsync();
        var ruleNames = rulesResult.Match(r => r, _ => []);
        ruleNames.Count(n => n == "SameNameRule").ShouldBe(1);
    }

    #endregion

    #region GetRegisteredRulesAsync Tests

    [Fact]
    public async Task GetRegisteredRulesAsync_ReturnsAllRuleNames()
    {
        // Arrange
        var rule1 = CreateMatchingRule(name: "RuleAlpha");
        var rule2 = CreateMatchingRule(name: "RuleBeta");
        var rule3 = CreateMatchingRule(name: "RuleGamma");
        var sut = CreateSut(rules: [rule1, rule2, rule3]);

        // Act
        var result = await sut.GetRegisteredRulesAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        var ruleNames = result.Match(r => r, _ => []);
        ruleNames.Count.ShouldBe(3);
        ruleNames.ShouldContain("RuleAlpha");
        ruleNames.ShouldContain("RuleBeta");
        ruleNames.ShouldContain("RuleGamma");
    }

    #endregion
}
