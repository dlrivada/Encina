using Encina.Compliance.BreachNotification.Model;

namespace Encina.GuardTests.Compliance.BreachNotification;

/// <summary>
/// Guard tests verifying that model factory methods produce valid instances
/// and that required properties are correctly assigned. These tests exercise
/// the Create() factories in BreachRecord, PhasedReport, BreachAuditEntry,
/// SecurityEvent, and NotificationResult to cover their executable lines.
/// </summary>
public class BreachModelFactoryGuardTests
{
    private static readonly DateTimeOffset Now = new(2026, 4, 1, 10, 0, 0, TimeSpan.Zero);

    #region BreachRecord.Create

    [Fact]
    public void BreachRecord_Create_ShouldGenerateNonEmptyId()
    {
        var record = BreachRecord.Create(
            "unauthorized access", 500, ["email", "name"],
            "dpo@company.com", "Identity theft risk",
            "Access revoked", Now, BreachSeverity.High);

        record.Id.ShouldNotBeNullOrWhiteSpace();
        record.Id.Length.ShouldBe(32);
    }

    [Fact]
    public void BreachRecord_Create_ShouldSetStatusToDetected()
    {
        var record = BreachRecord.Create(
            "data exfiltration", 1000, ["health"],
            "dpo@company.com", "Health data exposure",
            "Encryption applied", Now, BreachSeverity.Critical);

        record.Status.ShouldBe(BreachStatus.Detected);
        record.NotificationDeadlineUtc.ShouldBe(Now.AddHours(72));
    }

    [Fact]
    public void BreachRecord_Create_ShouldPreserveAllParameters()
    {
        var record = BreachRecord.Create(
            "privilege escalation", 50, ["financial"],
            "dpo@test.com", "Financial data at risk",
            "Privileges revoked", Now, BreachSeverity.Medium);

        record.Nature.ShouldBe("privilege escalation");
        record.ApproximateSubjectsAffected.ShouldBe(50);
        record.CategoriesOfDataAffected.ShouldContain("financial");
        record.DPOContactDetails.ShouldBe("dpo@test.com");
        record.Severity.ShouldBe(BreachSeverity.Medium);
    }

    #endregion

    #region PhasedReport.Create

    [Fact]
    public void PhasedReport_Create_ShouldGenerateNonEmptyId()
    {
        var report = PhasedReport.Create("breach-1", 1, "Initial report", Now);

        report.Id.ShouldNotBeNullOrWhiteSpace();
        report.Id.Length.ShouldBe(32);
    }

    [Fact]
    public void PhasedReport_Create_ShouldPreserveParameters()
    {
        var report = PhasedReport.Create("breach-1", 2, "Follow-up findings", Now, "user-1");

        report.BreachId.ShouldBe("breach-1");
        report.ReportNumber.ShouldBe(2);
        report.Content.ShouldBe("Follow-up findings");
        report.SubmittedByUserId.ShouldBe("user-1");
    }

    [Fact]
    public void PhasedReport_Create_NullSubmittedBy_ShouldBeNull()
    {
        var report = PhasedReport.Create("breach-1", 1, "Auto report", Now);

        report.SubmittedByUserId.ShouldBeNull();
    }

    #endregion

    #region BreachAuditEntry.Create

    [Fact]
    public void BreachAuditEntry_Create_ShouldGenerateNonEmptyId()
    {
        var entry = BreachAuditEntry.Create("breach-1", "Assessed");

        entry.Id.ShouldNotBeNullOrWhiteSpace();
        entry.Id.Length.ShouldBe(32);
    }

    [Fact]
    public void BreachAuditEntry_Create_ShouldPreserveParameters()
    {
        var entry = BreachAuditEntry.Create("breach-1", "Authority notified", "Report sent to ICO", "dpo");

        entry.BreachId.ShouldBe("breach-1");
        entry.Action.ShouldBe("Authority notified");
        entry.PerformedByUserId.ShouldBe("dpo");
        entry.Detail.ShouldBe("Report sent to ICO");
    }

    #endregion

    #region SecurityEvent.Create

    [Fact]
    public void SecurityEvent_Create_ShouldGenerateNonEmptyId()
    {
        var evt = SecurityEvent.Create(
            SecurityEventType.UnauthorizedAccess, "AuthService", "Failed login", Now);

        evt.Id.ShouldNotBeNullOrWhiteSpace();
        evt.Id.Length.ShouldBe(32);
    }

    [Fact]
    public void SecurityEvent_Create_ShouldPreserveParameters()
    {
        var evt = SecurityEvent.Create(
            SecurityEventType.DataExfiltration, "DBMonitor", "Bulk export detected", Now);

        evt.EventType.ShouldBe(SecurityEventType.DataExfiltration);
        evt.Source.ShouldBe("DBMonitor");
        evt.Description.ShouldBe("Bulk export detected");
        evt.OccurredAtUtc.ShouldBe(Now);
    }

    #endregion

    #region NotificationResult

    [Fact]
    public void NotificationResult_SentOutcome_ShouldPreserveFields()
    {
        var result = new NotificationResult
        {
            Outcome = NotificationOutcome.Sent,
            SentAtUtc = Now,
            Recipient = "ICO",
            BreachId = "breach-1"
        };

        result.Outcome.ShouldBe(NotificationOutcome.Sent);
        result.SentAtUtc.ShouldBe(Now);
        result.Recipient.ShouldBe("ICO");
        result.BreachId.ShouldBe("breach-1");
        result.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public void NotificationResult_FailedOutcome_ShouldPreserveError()
    {
        var result = new NotificationResult
        {
            Outcome = NotificationOutcome.Failed,
            BreachId = "breach-2",
            ErrorMessage = "SMTP timeout"
        };

        result.Outcome.ShouldBe(NotificationOutcome.Failed);
        result.SentAtUtc.ShouldBeNull();
        result.ErrorMessage.ShouldBe("SMTP timeout");
    }

    #endregion

    #region PotentialBreach

    [Fact]
    public void PotentialBreach_ShouldPreserveRequiredFields()
    {
        var evt = SecurityEvent.Create(
            SecurityEventType.UnauthorizedAccess, "Auth", "Failed", Now);

        var breach = new PotentialBreach
        {
            DetectionRuleName = "ExcessiveFailedLogins",
            Severity = BreachSeverity.High,
            Description = "Multiple failed login attempts",
            SecurityEvent = evt,
            DetectedAtUtc = Now
        };

        breach.DetectionRuleName.ShouldBe("ExcessiveFailedLogins");
        breach.Severity.ShouldBe(BreachSeverity.High);
        breach.SecurityEvent.ShouldBe(evt);
        breach.RecommendedActions.ShouldBeNull();
    }

    [Fact]
    public void PotentialBreach_WithRecommendedActions_ShouldPreserve()
    {
        var evt = SecurityEvent.Create(
            SecurityEventType.DataExfiltration, "Net", "Export", Now);

        var breach = new PotentialBreach
        {
            DetectionRuleName = "BulkExport",
            Severity = BreachSeverity.Critical,
            Description = "Mass data export",
            SecurityEvent = evt,
            DetectedAtUtc = Now,
            RecommendedActions = ["Block IP", "Revoke access"]
        };

        breach.RecommendedActions.ShouldNotBeNull();
        breach.RecommendedActions.Count.ShouldBe(2);
    }

    #endregion
}
