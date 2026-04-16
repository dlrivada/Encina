#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.BreachNotification;
using Encina.Compliance.BreachNotification.Detection.Rules;
using Encina.Compliance.BreachNotification.Model;

using Shouldly;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Encina.UnitTests.Compliance.BreachNotification;

/// <summary>
/// Unit tests for the built-in breach detection rules:
/// <see cref="UnauthorizedAccessRule"/>, <see cref="MassDataExfiltrationRule"/>,
/// <see cref="PrivilegeEscalationRule"/>, and <see cref="AnomalousQueryPatternRule"/>.
/// </summary>
public class DetectionRulesTests
{
    private static SecurityEvent CreateTestEvent(
        SecurityEventType eventType = SecurityEventType.UnauthorizedAccess,
        string source = "test-source",
        string description = "test description")
    {
        return SecurityEvent.Create(eventType, source, description, DateTimeOffset.UtcNow);
    }

    private static IOptions<BreachNotificationOptions> CreateDefaultOptions()
    {
        return Options.Create(new BreachNotificationOptions());
    }

    /// <summary>
    /// Extracts the <see cref="PotentialBreach"/> from a successful rule evaluation result,
    /// or returns <c>null</c> if the result is <c>None</c> or an error.
    /// </summary>
    private static PotentialBreach? ExtractBreach(Either<EncinaError, Option<PotentialBreach>> result)
    {
        PotentialBreach? extracted = null;
        result.Match(
            Right: optionalBreach => optionalBreach.IfSome(b => extracted = b),
            Left: _ => { });
        return extracted;
    }

    #region UnauthorizedAccessRule Tests

    [Fact]
    public async Task UnauthorizedAccessRule_MatchingEvent_ReturnsPotentialBreach()
    {
        // Arrange
        var rule = new UnauthorizedAccessRule(
            CreateDefaultOptions(),
            NullLogger<UnauthorizedAccessRule>.Instance);
        var securityEvent = CreateTestEvent(SecurityEventType.UnauthorizedAccess);

        // Act
        var result = await rule.EvaluateAsync(securityEvent);

        // Assert
        result.IsRight.ShouldBeTrue();
        var breach = ExtractBreach(result);
        breach.ShouldNotBeNull();
        breach!.DetectionRuleName.ShouldBe("UnauthorizedAccess");
        breach.Severity.ShouldBe(BreachSeverity.High);
    }

    [Fact]
    public async Task UnauthorizedAccessRule_NonMatchingEvent_ReturnsNone()
    {
        // Arrange
        var rule = new UnauthorizedAccessRule(
            CreateDefaultOptions(),
            NullLogger<UnauthorizedAccessRule>.Instance);
        var securityEvent = CreateTestEvent(SecurityEventType.DataExfiltration);

        // Act
        var result = await rule.EvaluateAsync(securityEvent);

        // Assert
        result.IsRight.ShouldBeTrue();
        var breach = ExtractBreach(result);
        breach.ShouldBeNull();
    }

    [Fact]
    public async Task UnauthorizedAccessRule_MatchingEvent_IncludesRecommendedActions()
    {
        // Arrange
        var rule = new UnauthorizedAccessRule(
            CreateDefaultOptions(),
            NullLogger<UnauthorizedAccessRule>.Instance);
        var securityEvent = CreateTestEvent(SecurityEventType.UnauthorizedAccess);

        // Act
        var result = await rule.EvaluateAsync(securityEvent);

        // Assert
        result.IsRight.ShouldBeTrue();
        var breach = ExtractBreach(result);
        breach.ShouldNotBeNull();
        breach!.RecommendedActions.ShouldNotBeEmpty();
    }

    #endregion

    #region MassDataExfiltrationRule Tests

    [Fact]
    public async Task MassDataExfiltrationRule_MatchingEvent_ReturnsCriticalBreach()
    {
        // Arrange
        var rule = new MassDataExfiltrationRule(
            CreateDefaultOptions(),
            NullLogger<MassDataExfiltrationRule>.Instance);
        var securityEvent = CreateTestEvent(SecurityEventType.DataExfiltration);

        // Act
        var result = await rule.EvaluateAsync(securityEvent);

        // Assert
        result.IsRight.ShouldBeTrue();
        var breach = ExtractBreach(result);
        breach.ShouldNotBeNull();
        breach!.DetectionRuleName.ShouldBe("MassDataExfiltration");
        breach.Severity.ShouldBe(BreachSeverity.Critical);
    }

    [Fact]
    public async Task MassDataExfiltrationRule_NonMatchingEvent_ReturnsNone()
    {
        // Arrange
        var rule = new MassDataExfiltrationRule(
            CreateDefaultOptions(),
            NullLogger<MassDataExfiltrationRule>.Instance);
        var securityEvent = CreateTestEvent(SecurityEventType.UnauthorizedAccess);

        // Act
        var result = await rule.EvaluateAsync(securityEvent);

        // Assert
        result.IsRight.ShouldBeTrue();
        var breach = ExtractBreach(result);
        breach.ShouldBeNull();
    }

    #endregion

    #region PrivilegeEscalationRule Tests

    [Fact]
    public async Task PrivilegeEscalationRule_MatchingEvent_ReturnsHighSeverityBreach()
    {
        // Arrange
        var rule = new PrivilegeEscalationRule(
            NullLogger<PrivilegeEscalationRule>.Instance);
        var securityEvent = CreateTestEvent(SecurityEventType.PrivilegeEscalation);

        // Act
        var result = await rule.EvaluateAsync(securityEvent);

        // Assert
        result.IsRight.ShouldBeTrue();
        var breach = ExtractBreach(result);
        breach.ShouldNotBeNull();
        breach!.DetectionRuleName.ShouldBe("PrivilegeEscalation");
        breach.Severity.ShouldBe(BreachSeverity.High);
    }

    [Fact]
    public async Task PrivilegeEscalationRule_NonMatchingEvent_ReturnsNone()
    {
        // Arrange
        var rule = new PrivilegeEscalationRule(
            NullLogger<PrivilegeEscalationRule>.Instance);
        var securityEvent = CreateTestEvent(SecurityEventType.AnomalousQuery);

        // Act
        var result = await rule.EvaluateAsync(securityEvent);

        // Assert
        result.IsRight.ShouldBeTrue();
        var breach = ExtractBreach(result);
        breach.ShouldBeNull();
    }

    #endregion

    #region AnomalousQueryPatternRule Tests

    [Fact]
    public async Task AnomalousQueryPatternRule_MatchingEvent_ReturnsMediumSeverityBreach()
    {
        // Arrange
        var rule = new AnomalousQueryPatternRule(
            CreateDefaultOptions(),
            NullLogger<AnomalousQueryPatternRule>.Instance);
        var securityEvent = CreateTestEvent(SecurityEventType.AnomalousQuery);

        // Act
        var result = await rule.EvaluateAsync(securityEvent);

        // Assert
        result.IsRight.ShouldBeTrue();
        var breach = ExtractBreach(result);
        breach.ShouldNotBeNull();
        breach!.DetectionRuleName.ShouldBe("AnomalousQueryPattern");
        breach.Severity.ShouldBe(BreachSeverity.Medium);
    }

    [Fact]
    public async Task AnomalousQueryPatternRule_NonMatchingEvent_ReturnsNone()
    {
        // Arrange
        var rule = new AnomalousQueryPatternRule(
            CreateDefaultOptions(),
            NullLogger<AnomalousQueryPatternRule>.Instance);
        var securityEvent = CreateTestEvent(SecurityEventType.PrivilegeEscalation);

        // Act
        var result = await rule.EvaluateAsync(securityEvent);

        // Assert
        result.IsRight.ShouldBeTrue();
        var breach = ExtractBreach(result);
        breach.ShouldBeNull();
    }

    #endregion

    #region Cross-Rule Verification

    [Fact]
    public async Task AllRules_HaveUniqueNames()
    {
        // Arrange
        var rules = new IBreachDetectionRule[]
        {
            new UnauthorizedAccessRule(CreateDefaultOptions(), NullLogger<UnauthorizedAccessRule>.Instance),
            new MassDataExfiltrationRule(CreateDefaultOptions(), NullLogger<MassDataExfiltrationRule>.Instance),
            new PrivilegeEscalationRule(NullLogger<PrivilegeEscalationRule>.Instance),
            new AnomalousQueryPatternRule(CreateDefaultOptions(), NullLogger<AnomalousQueryPatternRule>.Instance)
        };

        // Act
        var names = rules.Select(r => r.Name).ToList();

        // Assert
        names.ShouldBeUnique();
        names.Count.ShouldBe(4);
    }

    [Fact]
    public async Task AllRules_DescriptionIncludesSourceInfo()
    {
        // Arrange
        var rules = new (IBreachDetectionRule Rule, SecurityEventType MatchType)[]
        {
            (new UnauthorizedAccessRule(CreateDefaultOptions(), NullLogger<UnauthorizedAccessRule>.Instance), SecurityEventType.UnauthorizedAccess),
            (new MassDataExfiltrationRule(CreateDefaultOptions(), NullLogger<MassDataExfiltrationRule>.Instance), SecurityEventType.DataExfiltration),
            (new PrivilegeEscalationRule(NullLogger<PrivilegeEscalationRule>.Instance), SecurityEventType.PrivilegeEscalation),
            (new AnomalousQueryPatternRule(CreateDefaultOptions(), NullLogger<AnomalousQueryPatternRule>.Instance), SecurityEventType.AnomalousQuery)
        };

        foreach (var (rule, matchType) in rules)
        {
            var securityEvent = CreateTestEvent(matchType, source: "my-test-source");

            // Act
            var result = await rule.EvaluateAsync(securityEvent);

            // Assert
            result.IsRight.ShouldBeTrue();
            var breach = ExtractBreach(result);
            breach.ShouldNotBeNull();
            breach!.Description.ShouldContain("my-test-source",
                $"Rule '{rule.Name}' should include source in description");
        }
    }

    #endregion
}
