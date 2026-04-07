using Encina.Compliance.BreachNotification.Model;

using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Compliance.BreachNotification;

/// <summary>
/// Property-based tests for <see cref="PotentialBreach"/> verifying domain invariants
/// using FsCheck random data generation.
/// </summary>
public class PotentialBreachPropertyTests
{
    [Property(MaxTest = 50)]
    public bool DetectionRuleName_IsPreserved(NonEmptyString ruleName, NonEmptyString desc)
    {
        var evt = SecurityEvent.Create(
            SecurityEventType.UnauthorizedAccess,
            "source",
            "desc",
            DateTimeOffset.UtcNow);

        var breach = new PotentialBreach
        {
            DetectionRuleName = ruleName.Get,
            Severity = BreachSeverity.Medium,
            Description = desc.Get,
            SecurityEvent = evt,
            DetectedAtUtc = DateTimeOffset.UtcNow
        };

        return breach.DetectionRuleName == ruleName.Get;
    }

    [Property(MaxTest = 50)]
    public bool Severity_IsPreserved(NonEmptyString desc)
    {
        var evt = SecurityEvent.Create(
            SecurityEventType.UnauthorizedAccess,
            "source",
            "desc",
            DateTimeOffset.UtcNow);

        var breach = new PotentialBreach
        {
            DetectionRuleName = "rule",
            Severity = BreachSeverity.Critical,
            Description = desc.Get,
            SecurityEvent = evt,
            DetectedAtUtc = DateTimeOffset.UtcNow
        };

        return breach.Severity == BreachSeverity.Critical;
    }

    [Property(MaxTest = 50)]
    public bool Description_IsPreserved(NonEmptyString desc)
    {
        var evt = SecurityEvent.Create(
            SecurityEventType.DataExfiltration,
            "source",
            "desc",
            DateTimeOffset.UtcNow);

        var breach = new PotentialBreach
        {
            DetectionRuleName = "rule",
            Severity = BreachSeverity.High,
            Description = desc.Get,
            SecurityEvent = evt,
            DetectedAtUtc = DateTimeOffset.UtcNow
        };

        return breach.Description == desc.Get;
    }

    [Property(MaxTest = 50)]
    public bool RecommendedActions_NullByDefault(NonEmptyString desc)
    {
        var evt = SecurityEvent.Create(
            SecurityEventType.UnauthorizedAccess,
            "source",
            "desc",
            DateTimeOffset.UtcNow);

        var breach = new PotentialBreach
        {
            DetectionRuleName = "rule",
            Severity = BreachSeverity.Low,
            Description = desc.Get,
            SecurityEvent = evt,
            DetectedAtUtc = DateTimeOffset.UtcNow
        };

        return breach.RecommendedActions is null;
    }
}
