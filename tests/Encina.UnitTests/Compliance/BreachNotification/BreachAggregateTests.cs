using Encina.Compliance.BreachNotification.Aggregates;
using Encina.Compliance.BreachNotification.Events;
using Encina.Compliance.BreachNotification.Model;
using Shouldly;

namespace Encina.UnitTests.Compliance.BreachNotification;

/// <summary>
/// Unit tests for <see cref="BreachAggregate"/>.
/// </summary>
public class BreachAggregateTests
{
    private static readonly Guid DefaultId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);

    #region Helpers

    private static BreachAggregate CreateDetectedAggregate(
        Guid? id = null,
        string? tenantId = null,
        string? moduleId = null)
    {
        return BreachAggregate.Detect(
            id ?? DefaultId,
            "Unauthorized access to personal data",
            BreachSeverity.High,
            "rule-unauthorized-access",
            500,
            "An attacker gained unauthorized access to a database containing personal data.",
            "user-detector-1",
            Now,
            tenantId,
            moduleId);
    }

    private static BreachAggregate CreateAssessedAggregate()
    {
        var aggregate = CreateDetectedAggregate();
        aggregate.Assess(
            BreachSeverity.Critical,
            1200,
            "Assessment reveals broader scope than initially detected.",
            "user-assessor-1",
            Now.AddHours(2));
        return aggregate;
    }

    private static BreachAggregate CreateAuthorityNotifiedAggregate()
    {
        var aggregate = CreateAssessedAggregate();
        aggregate.ReportToDPA(
            "AEPD",
            "dpo@aepd.es",
            "Full breach report submitted per Art. 33(3).",
            "user-reporter-1",
            Now.AddHours(12));
        return aggregate;
    }

    private static BreachAggregate CreateSubjectsNotifiedAggregate()
    {
        var aggregate = CreateAuthorityNotifiedAggregate();
        aggregate.NotifySubjects(
            1200,
            "email",
            SubjectNotificationExemption.None,
            "user-notifier-1",
            Now.AddHours(24));
        return aggregate;
    }

    private static BreachAggregate CreateResolvedAggregate()
    {
        var aggregate = CreateSubjectsNotifiedAggregate();
        aggregate.Contain(
            "Database credentials rotated, firewall rules tightened, compromised accounts disabled.",
            "user-containment-1",
            Now.AddHours(48));
        return aggregate;
    }

    private static BreachAggregate CreateClosedAggregate()
    {
        var aggregate = CreateResolvedAggregate();
        aggregate.Close(
            "Root cause: weak credentials. Remediation: enforced MFA, rotated all secrets.",
            "user-closer-1",
            Now.AddDays(30));
        return aggregate;
    }

    #endregion

    #region Detect (Static Factory)

    [Fact]
    public void Detect_ValidParameters_SetsStatusToDetected()
    {
        // Act
        var aggregate = CreateDetectedAggregate();

        // Assert
        aggregate.Status.ShouldBe(BreachStatus.Detected);
    }

    [Fact]
    public void Detect_ValidParameters_SetsAllProperties()
    {
        // Act
        var aggregate = BreachAggregate.Detect(
            DefaultId,
            "Data exfiltration via API",
            BreachSeverity.Critical,
            "rule-exfiltration",
            2000,
            "Sensitive data was exfiltrated through an unprotected API endpoint.",
            "user-detector-1",
            Now,
            "tenant-1",
            "module-1");

        // Assert
        aggregate.Id.ShouldBe(DefaultId);
        aggregate.Nature.ShouldBe("Data exfiltration via API");
        aggregate.Severity.ShouldBe(BreachSeverity.Critical);
        aggregate.EstimatedAffectedSubjects.ShouldBe(2000);
        aggregate.Description.ShouldBe("Sensitive data was exfiltrated through an unprotected API endpoint.");
        aggregate.DetectedAtUtc.ShouldBe(Now);
        aggregate.TenantId.ShouldBe("tenant-1");
        aggregate.ModuleId.ShouldBe("module-1");
    }

    [Fact]
    public void Detect_ValidParameters_CalculatesDeadlineAs72Hours()
    {
        // Act
        var aggregate = CreateDetectedAggregate();

        // Assert
        aggregate.DeadlineUtc.ShouldBe(Now.AddHours(72));
    }

    [Fact]
    public void Detect_ValidParameters_RaisesBreachDetectedEvent()
    {
        // Act
        var aggregate = CreateDetectedAggregate();

        // Assert
        aggregate.UncommittedEvents.ShouldHaveSingleItem()
            .Which.ShouldBeOfType<BreachDetected>();
        aggregate.Version.ShouldBe(1);
    }

    [Fact]
    public void Detect_ValidParameters_EventContainsCorrectData()
    {
        // Act
        var aggregate = BreachAggregate.Detect(
            DefaultId,
            "Unauthorized access",
            BreachSeverity.High,
            "rule-access",
            500,
            "Breach description.",
            "user-1",
            Now,
            "tenant-1",
            "module-1");

        // Assert
        var evt = aggregate.UncommittedEvents.ShouldHaveSingleItem()
            .Which.ShouldBeOfType<BreachDetected>().Subject;

        evt.BreachId.ShouldBe(DefaultId);
        evt.Nature.ShouldBe("Unauthorized access");
        evt.Severity.ShouldBe(BreachSeverity.High);
        evt.DetectedByRule.ShouldBe("rule-access");
        evt.EstimatedAffectedSubjects.ShouldBe(500);
        evt.Description.ShouldBe("Breach description.");
        evt.DetectedByUserId.ShouldBe("user-1");
        evt.DetectedAtUtc.ShouldBe(Now);
        evt.TenantId.ShouldBe("tenant-1");
        evt.ModuleId.ShouldBe("module-1");
    }

    [Fact]
    public void Detect_WithTenantAndModule_SetsTenantIdAndModuleId()
    {
        // Act
        var aggregate = CreateDetectedAggregate(tenantId: "tenant-abc", moduleId: "module-xyz");

        // Assert
        aggregate.TenantId.ShouldBe("tenant-abc");
        aggregate.ModuleId.ShouldBe("module-xyz");
    }

    [Fact]
    public void Detect_WithoutTenantAndModule_DefaultsToNull()
    {
        // Act
        var aggregate = CreateDetectedAggregate();

        // Assert
        aggregate.TenantId.ShouldBeNull();
        aggregate.ModuleId.ShouldBeNull();
    }

    [Fact]
    public void Detect_InitialState_TimestampsAreNull()
    {
        // Act
        var aggregate = CreateDetectedAggregate();

        // Assert
        aggregate.AssessedAtUtc.ShouldBeNull();
        aggregate.ReportedToDPAAtUtc.ShouldBeNull();
        aggregate.NotifiedSubjectsAtUtc.ShouldBeNull();
        aggregate.ContainedAtUtc.ShouldBeNull();
        aggregate.ClosedAtUtc.ShouldBeNull();
        aggregate.AuthorityName.ShouldBeNull();
        aggregate.SubjectCount.ShouldBe(0);
        aggregate.PhasedReportCount.ShouldBe(0);
    }

    [Fact]
    public void Detect_NullDetectedByUserId_CreatesAggregate()
    {
        // Act
        var aggregate = BreachAggregate.Detect(
            DefaultId,
            "Automated detection",
            BreachSeverity.Medium,
            "rule-automated",
            100,
            "Automated system detected anomalous activity.",
            null,
            Now);

        // Assert
        aggregate.Status.ShouldBe(BreachStatus.Detected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Detect_NullOrWhiteSpaceNature_ThrowsArgumentException(string? nature)
    {
        // Act
        var act = () => BreachAggregate.Detect(
            DefaultId, nature!, BreachSeverity.High, "rule-1", 100, "desc", null, Now);

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Detect_NullOrWhiteSpaceDetectedByRule_ThrowsArgumentException(string? rule)
    {
        // Act
        var act = () => BreachAggregate.Detect(
            DefaultId, "nature", BreachSeverity.High, rule!, 100, "desc", null, Now);

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Detect_NullOrWhiteSpaceDescription_ThrowsArgumentException(string? description)
    {
        // Act
        var act = () => BreachAggregate.Detect(
            DefaultId, "nature", BreachSeverity.High, "rule-1", 100, description!, null, Now);

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    #endregion

    #region Assess

    [Fact]
    public void Assess_FromDetected_TransitionsToInvestigating()
    {
        // Arrange
        var aggregate = CreateDetectedAggregate();

        // Act
        aggregate.Assess(
            BreachSeverity.Critical,
            1200,
            "Broader scope identified.",
            "user-assessor-1",
            Now.AddHours(2));

        // Assert
        aggregate.Status.ShouldBe(BreachStatus.Investigating);
    }

    [Fact]
    public void Assess_FromDetected_UpdatesSeverityAndSubjects()
    {
        // Arrange
        var aggregate = CreateDetectedAggregate();

        // Act
        aggregate.Assess(
            BreachSeverity.Critical,
            1500,
            "Scope expanded after investigation.",
            "user-assessor-1",
            Now.AddHours(3));

        // Assert
        aggregate.Severity.ShouldBe(BreachSeverity.Critical);
        aggregate.EstimatedAffectedSubjects.ShouldBe(1500);
    }

    [Fact]
    public void Assess_FromDetected_SetsAssessedAtUtc()
    {
        // Arrange
        var aggregate = CreateDetectedAggregate();
        var assessedAt = Now.AddHours(4);

        // Act
        aggregate.Assess(
            BreachSeverity.High,
            600,
            "Assessment completed.",
            "user-assessor-1",
            assessedAt);

        // Assert
        aggregate.AssessedAtUtc.ShouldBe(assessedAt);
    }

    [Fact]
    public void Assess_FromDetected_RaisesBreachAssessedEvent()
    {
        // Arrange
        var aggregate = CreateDetectedAggregate();

        // Act
        aggregate.Assess(
            BreachSeverity.Critical,
            1200,
            "Assessment summary.",
            "user-assessor-1",
            Now.AddHours(2));

        // Assert
        aggregate.UncommittedEvents.Count.ShouldBe(2);
        aggregate.UncommittedEvents[^1].ShouldBeOfType<BreachAssessed>();
        aggregate.Version.ShouldBe(2);
    }

    [Fact]
    public void Assess_FromDetected_EventContainsCorrectData()
    {
        // Arrange
        var aggregate = CreateDetectedAggregate(tenantId: "tenant-1", moduleId: "module-1");
        var assessedAt = Now.AddHours(2);

        // Act
        aggregate.Assess(
            BreachSeverity.Critical,
            1200,
            "Full assessment summary.",
            "user-assessor-1",
            assessedAt);

        // Assert
        var evt = aggregate.UncommittedEvents[^1].ShouldBeOfType<BreachAssessed>().Subject;
        evt.BreachId.ShouldBe(aggregate.Id);
        evt.UpdatedSeverity.ShouldBe(BreachSeverity.Critical);
        evt.UpdatedAffectedSubjects.ShouldBe(1200);
        evt.AssessmentSummary.ShouldBe("Full assessment summary.");
        evt.AssessedByUserId.ShouldBe("user-assessor-1");
        evt.AssessedAtUtc.ShouldBe(assessedAt);
        evt.TenantId.ShouldBe("tenant-1");
        evt.ModuleId.ShouldBe("module-1");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Assess_NullOrWhiteSpaceAssessmentSummary_ThrowsArgumentException(string? summary)
    {
        // Arrange
        var aggregate = CreateDetectedAggregate();

        // Act
        var act = () => aggregate.Assess(
            BreachSeverity.High, 500, summary!, "user-1", Now.AddHours(1));

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Assess_NullOrWhiteSpaceAssessedByUserId_ThrowsArgumentException(string? userId)
    {
        // Arrange
        var aggregate = CreateDetectedAggregate();

        // Act
        var act = () => aggregate.Assess(
            BreachSeverity.High, 500, "Summary.", userId!, Now.AddHours(1));

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    #endregion

    #region ReportToDPA

    [Fact]
    public void ReportToDPA_FromDetected_TransitionsToAuthorityNotified()
    {
        // Arrange
        var aggregate = CreateDetectedAggregate();

        // Act
        aggregate.ReportToDPA(
            "AEPD",
            "dpo@aepd.es",
            "Initial breach report.",
            "user-reporter-1",
            Now.AddHours(10));

        // Assert
        aggregate.Status.ShouldBe(BreachStatus.AuthorityNotified);
    }

    [Fact]
    public void ReportToDPA_FromInvestigating_TransitionsToAuthorityNotified()
    {
        // Arrange
        var aggregate = CreateAssessedAggregate();

        // Act
        aggregate.ReportToDPA(
            "AEPD",
            "dpo@aepd.es",
            "Breach report with assessment findings.",
            "user-reporter-1",
            Now.AddHours(12));

        // Assert
        aggregate.Status.ShouldBe(BreachStatus.AuthorityNotified);
    }

    [Fact]
    public void ReportToDPA_SetsAuthorityNameAndTimestamp()
    {
        // Arrange
        var aggregate = CreateDetectedAggregate();
        var reportedAt = Now.AddHours(8);

        // Act
        aggregate.ReportToDPA(
            "ICO",
            "ico@gov.uk",
            "Report to UK authority.",
            "user-reporter-1",
            reportedAt);

        // Assert
        aggregate.AuthorityName.ShouldBe("ICO");
        aggregate.ReportedToDPAAtUtc.ShouldBe(reportedAt);
    }

    [Fact]
    public void ReportToDPA_RaisesBreachReportedToDPAEvent()
    {
        // Arrange
        var aggregate = CreateDetectedAggregate();

        // Act
        aggregate.ReportToDPA(
            "AEPD",
            "dpo@aepd.es",
            "Report summary.",
            "user-reporter-1",
            Now.AddHours(10));

        // Assert
        aggregate.UncommittedEvents.Count.ShouldBe(2);
        aggregate.UncommittedEvents[^1].ShouldBeOfType<BreachReportedToDPA>();
    }

    [Fact]
    public void ReportToDPA_EventContainsCorrectData()
    {
        // Arrange
        var aggregate = CreateDetectedAggregate(tenantId: "tenant-1", moduleId: "module-1");
        var reportedAt = Now.AddHours(10);

        // Act
        aggregate.ReportToDPA(
            "AEPD",
            "dpo@aepd.es",
            "Full report.",
            "user-reporter-1",
            reportedAt);

        // Assert
        var evt = aggregate.UncommittedEvents[^1].ShouldBeOfType<BreachReportedToDPA>().Subject;
        evt.BreachId.ShouldBe(aggregate.Id);
        evt.AuthorityName.ShouldBe("AEPD");
        evt.AuthorityContactInfo.ShouldBe("dpo@aepd.es");
        evt.ReportSummary.ShouldBe("Full report.");
        evt.ReportedByUserId.ShouldBe("user-reporter-1");
        evt.ReportedAtUtc.ShouldBe(reportedAt);
        evt.TenantId.ShouldBe("tenant-1");
        evt.ModuleId.ShouldBe("module-1");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ReportToDPA_NullOrWhiteSpaceAuthorityName_ThrowsArgumentException(string? name)
    {
        // Arrange
        var aggregate = CreateDetectedAggregate();

        // Act
        var act = () => aggregate.ReportToDPA(
            name!, "contact", "summary", "user-1", Now.AddHours(1));

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ReportToDPA_NullOrWhiteSpaceContactInfo_ThrowsArgumentException(string? contact)
    {
        // Arrange
        var aggregate = CreateDetectedAggregate();

        // Act
        var act = () => aggregate.ReportToDPA(
            "AEPD", contact!, "summary", "user-1", Now.AddHours(1));

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ReportToDPA_NullOrWhiteSpaceReportSummary_ThrowsArgumentException(string? summary)
    {
        // Arrange
        var aggregate = CreateDetectedAggregate();

        // Act
        var act = () => aggregate.ReportToDPA(
            "AEPD", "contact", summary!, "user-1", Now.AddHours(1));

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ReportToDPA_NullOrWhiteSpaceReportedByUserId_ThrowsArgumentException(string? userId)
    {
        // Arrange
        var aggregate = CreateDetectedAggregate();

        // Act
        var act = () => aggregate.ReportToDPA(
            "AEPD", "contact", "summary", userId!, Now.AddHours(1));

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    #endregion

    #region NotifySubjects

    [Fact]
    public void NotifySubjects_FromAuthorityNotified_TransitionsToSubjectsNotified()
    {
        // Arrange
        var aggregate = CreateAuthorityNotifiedAggregate();

        // Act
        aggregate.NotifySubjects(
            1200,
            "email",
            SubjectNotificationExemption.None,
            "user-notifier-1",
            Now.AddHours(24));

        // Assert
        aggregate.Status.ShouldBe(BreachStatus.SubjectsNotified);
    }

    [Fact]
    public void NotifySubjects_SetsSubjectCountAndTimestamp()
    {
        // Arrange
        var aggregate = CreateAuthorityNotifiedAggregate();
        var notifiedAt = Now.AddHours(20);

        // Act
        aggregate.NotifySubjects(
            950,
            "letter",
            SubjectNotificationExemption.None,
            "user-notifier-1",
            notifiedAt);

        // Assert
        aggregate.SubjectCount.ShouldBe(950);
        aggregate.NotifiedSubjectsAtUtc.ShouldBe(notifiedAt);
    }

    [Fact]
    public void NotifySubjects_RaisesBreachNotifiedToSubjectsEvent()
    {
        // Arrange
        var aggregate = CreateAuthorityNotifiedAggregate();

        // Act
        aggregate.NotifySubjects(
            1200,
            "email",
            SubjectNotificationExemption.None,
            "user-notifier-1",
            Now.AddHours(24));

        // Assert
        aggregate.UncommittedEvents[^1].ShouldBeOfType<BreachNotifiedToSubjects>();
    }

    [Fact]
    public void NotifySubjects_EventContainsCorrectData()
    {
        // Arrange
        var aggregate = CreateAuthorityNotifiedAggregate();
        var notifiedAt = Now.AddHours(24);

        // Act
        aggregate.NotifySubjects(
            800,
            "public-notice",
            SubjectNotificationExemption.DisproportionateEffort,
            "user-notifier-1",
            notifiedAt);

        // Assert
        var evt = aggregate.UncommittedEvents[^1].ShouldBeOfType<BreachNotifiedToSubjects>().Subject;
        evt.BreachId.ShouldBe(aggregate.Id);
        evt.SubjectCount.ShouldBe(800);
        evt.CommunicationMethod.ShouldBe("public-notice");
        evt.Exemption.ShouldBe(SubjectNotificationExemption.DisproportionateEffort);
        evt.NotifiedByUserId.ShouldBe("user-notifier-1");
        evt.NotifiedAtUtc.ShouldBe(notifiedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NotifySubjects_NullOrWhiteSpaceCommunicationMethod_ThrowsArgumentException(string? method)
    {
        // Arrange
        var aggregate = CreateAuthorityNotifiedAggregate();

        // Act
        var act = () => aggregate.NotifySubjects(
            100, method!, SubjectNotificationExemption.None, "user-1", Now.AddHours(1));

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NotifySubjects_NullOrWhiteSpaceNotifiedByUserId_ThrowsArgumentException(string? userId)
    {
        // Arrange
        var aggregate = CreateAuthorityNotifiedAggregate();

        // Act
        var act = () => aggregate.NotifySubjects(
            100, "email", SubjectNotificationExemption.None, userId!, Now.AddHours(1));

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    #endregion

    #region AddPhasedReport

    [Fact]
    public void AddPhasedReport_FromDetected_IncrementsPhasedReportCount()
    {
        // Arrange
        var aggregate = CreateDetectedAggregate();

        // Act
        aggregate.AddPhasedReport(
            "Phase 1: Initial findings.",
            "user-reporter-1",
            Now.AddHours(6));

        // Assert
        aggregate.PhasedReportCount.ShouldBe(1);
    }

    [Fact]
    public void AddPhasedReport_MultipleTimes_IncrementsSequentially()
    {
        // Arrange
        var aggregate = CreateDetectedAggregate();

        // Act
        aggregate.AddPhasedReport("Phase 1.", "user-1", Now.AddHours(6));
        aggregate.AddPhasedReport("Phase 2.", "user-1", Now.AddHours(12));
        aggregate.AddPhasedReport("Phase 3.", "user-1", Now.AddHours(18));

        // Assert
        aggregate.PhasedReportCount.ShouldBe(3);
    }

    [Fact]
    public void AddPhasedReport_RaisesBreachPhasedReportAddedEvent()
    {
        // Arrange
        var aggregate = CreateDetectedAggregate();

        // Act
        aggregate.AddPhasedReport(
            "Phase 1 report content.",
            "user-reporter-1",
            Now.AddHours(6));

        // Assert
        aggregate.UncommittedEvents.Count.ShouldBe(2);
        aggregate.UncommittedEvents[^1].ShouldBeOfType<BreachPhasedReportAdded>();
    }

    [Fact]
    public void AddPhasedReport_EventContainsSequentialPhaseNumber()
    {
        // Arrange
        var aggregate = CreateDetectedAggregate();

        // Act
        aggregate.AddPhasedReport("Phase 1.", "user-1", Now.AddHours(6));
        aggregate.AddPhasedReport("Phase 2.", "user-1", Now.AddHours(12));

        // Assert
        var events = aggregate.UncommittedEvents
            .OfType<BreachPhasedReportAdded>()
            .ToList();

        events.Count.ShouldBe(2);
        events[0].PhaseNumber.ShouldBe(1);
        events[1].PhaseNumber.ShouldBe(2);
    }

    [Fact]
    public void AddPhasedReport_EventContainsCorrectData()
    {
        // Arrange
        var aggregate = CreateDetectedAggregate(tenantId: "tenant-1", moduleId: "module-1");
        var submittedAt = Now.AddHours(6);

        // Act
        aggregate.AddPhasedReport(
            "Detailed phase 1 findings.",
            "user-reporter-1",
            submittedAt);

        // Assert
        var evt = aggregate.UncommittedEvents[^1].ShouldBeOfType<BreachPhasedReportAdded>().Subject;
        evt.BreachId.ShouldBe(aggregate.Id);
        evt.PhaseNumber.ShouldBe(1);
        evt.ReportContent.ShouldBe("Detailed phase 1 findings.");
        evt.SubmittedByUserId.ShouldBe("user-reporter-1");
        evt.SubmittedAtUtc.ShouldBe(submittedAt);
        evt.TenantId.ShouldBe("tenant-1");
        evt.ModuleId.ShouldBe("module-1");
    }

    [Fact]
    public void AddPhasedReport_DoesNotChangeStatus()
    {
        // Arrange
        var aggregate = CreateAssessedAggregate();
        var statusBefore = aggregate.Status;

        // Act
        aggregate.AddPhasedReport("Report.", "user-1", Now.AddHours(6));

        // Assert
        aggregate.Status.ShouldBe(statusBefore);
    }

    [Fact]
    public void AddPhasedReport_FromInvestigating_Succeeds()
    {
        // Arrange
        var aggregate = CreateAssessedAggregate();

        // Act
        aggregate.AddPhasedReport("Phase report during investigation.", "user-1", Now.AddHours(6));

        // Assert
        aggregate.PhasedReportCount.ShouldBe(1);
    }

    [Fact]
    public void AddPhasedReport_FromAuthorityNotified_Succeeds()
    {
        // Arrange
        var aggregate = CreateAuthorityNotifiedAggregate();

        // Act
        aggregate.AddPhasedReport("Phase report after DPA notification.", "user-1", Now.AddHours(24));

        // Assert
        aggregate.PhasedReportCount.ShouldBe(1);
    }

    [Fact]
    public void AddPhasedReport_FromSubjectsNotified_Succeeds()
    {
        // Arrange
        var aggregate = CreateSubjectsNotifiedAggregate();

        // Act
        aggregate.AddPhasedReport("Phase report after subject notification.", "user-1", Now.AddHours(48));

        // Assert
        aggregate.PhasedReportCount.ShouldBe(1);
    }

    [Fact]
    public void AddPhasedReport_FromResolved_Succeeds()
    {
        // Arrange
        var aggregate = CreateResolvedAggregate();

        // Act
        aggregate.AddPhasedReport("Final phase report after containment.", "user-1", Now.AddDays(7));

        // Assert
        aggregate.PhasedReportCount.ShouldBe(1);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddPhasedReport_NullOrWhiteSpaceReportContent_ThrowsArgumentException(string? content)
    {
        // Arrange
        var aggregate = CreateDetectedAggregate();

        // Act
        var act = () => aggregate.AddPhasedReport(content!, "user-1", Now.AddHours(1));

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddPhasedReport_NullOrWhiteSpaceSubmittedByUserId_ThrowsArgumentException(string? userId)
    {
        // Arrange
        var aggregate = CreateDetectedAggregate();

        // Act
        var act = () => aggregate.AddPhasedReport("Content.", userId!, Now.AddHours(1));

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    #endregion

    #region Contain

    [Fact]
    public void Contain_FromDetected_TransitionsToResolved()
    {
        // Arrange
        var aggregate = CreateDetectedAggregate();

        // Act
        aggregate.Contain(
            "Immediate containment measures applied.",
            "user-containment-1",
            Now.AddHours(1));

        // Assert
        aggregate.Status.ShouldBe(BreachStatus.Resolved);
    }

    [Fact]
    public void Contain_FromInvestigating_TransitionsToResolved()
    {
        // Arrange
        var aggregate = CreateAssessedAggregate();

        // Act
        aggregate.Contain(
            "Containment after assessment.",
            "user-containment-1",
            Now.AddHours(6));

        // Assert
        aggregate.Status.ShouldBe(BreachStatus.Resolved);
    }

    [Fact]
    public void Contain_FromAuthorityNotified_TransitionsToResolved()
    {
        // Arrange
        var aggregate = CreateAuthorityNotifiedAggregate();

        // Act
        aggregate.Contain(
            "Containment after DPA notification.",
            "user-containment-1",
            Now.AddHours(24));

        // Assert
        aggregate.Status.ShouldBe(BreachStatus.Resolved);
    }

    [Fact]
    public void Contain_FromSubjectsNotified_TransitionsToResolved()
    {
        // Arrange
        var aggregate = CreateSubjectsNotifiedAggregate();

        // Act
        aggregate.Contain(
            "Containment after subject notification.",
            "user-containment-1",
            Now.AddHours(48));

        // Assert
        aggregate.Status.ShouldBe(BreachStatus.Resolved);
    }

    [Fact]
    public void Contain_SetsContainedAtUtc()
    {
        // Arrange
        var aggregate = CreateDetectedAggregate();
        var containedAt = Now.AddHours(5);

        // Act
        aggregate.Contain(
            "Firewall rules updated.",
            "user-containment-1",
            containedAt);

        // Assert
        aggregate.ContainedAtUtc.ShouldBe(containedAt);
    }

    [Fact]
    public void Contain_RaisesBreachContainedEvent()
    {
        // Arrange
        var aggregate = CreateDetectedAggregate();

        // Act
        aggregate.Contain(
            "Containment measures.",
            "user-containment-1",
            Now.AddHours(5));

        // Assert
        aggregate.UncommittedEvents[^1].ShouldBeOfType<BreachContained>();
    }

    [Fact]
    public void Contain_EventContainsCorrectData()
    {
        // Arrange
        var aggregate = CreateDetectedAggregate(tenantId: "tenant-1", moduleId: "module-1");
        var containedAt = Now.AddHours(5);

        // Act
        aggregate.Contain(
            "Credentials rotated.",
            "user-containment-1",
            containedAt);

        // Assert
        var evt = aggregate.UncommittedEvents[^1].ShouldBeOfType<BreachContained>().Subject;
        evt.BreachId.ShouldBe(aggregate.Id);
        evt.ContainmentMeasures.ShouldBe("Credentials rotated.");
        evt.ContainedByUserId.ShouldBe("user-containment-1");
        evt.ContainedAtUtc.ShouldBe(containedAt);
        evt.TenantId.ShouldBe("tenant-1");
        evt.ModuleId.ShouldBe("module-1");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Contain_NullOrWhiteSpaceContainmentMeasures_ThrowsArgumentException(string? measures)
    {
        // Arrange
        var aggregate = CreateDetectedAggregate();

        // Act
        var act = () => aggregate.Contain(measures!, "user-1", Now.AddHours(1));

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Contain_NullOrWhiteSpaceContainedByUserId_ThrowsArgumentException(string? userId)
    {
        // Arrange
        var aggregate = CreateDetectedAggregate();

        // Act
        var act = () => aggregate.Contain("Measures.", userId!, Now.AddHours(1));

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    #endregion

    #region Close

    [Fact]
    public void Close_FromSubjectsNotified_TransitionsToClosed()
    {
        // Arrange
        var aggregate = CreateSubjectsNotifiedAggregate();

        // Act
        aggregate.Close(
            "All obligations fulfilled.",
            "user-closer-1",
            Now.AddDays(30));

        // Assert
        aggregate.Status.ShouldBe(BreachStatus.Closed);
    }

    [Fact]
    public void Close_FromResolved_TransitionsToClosed()
    {
        // Arrange
        var aggregate = CreateResolvedAggregate();

        // Act
        aggregate.Close(
            "Case closed after containment and notification.",
            "user-closer-1",
            Now.AddDays(30));

        // Assert
        aggregate.Status.ShouldBe(BreachStatus.Closed);
    }

    [Fact]
    public void Close_SetsClosedAtUtc()
    {
        // Arrange
        var aggregate = CreateSubjectsNotifiedAggregate();
        var closedAt = Now.AddDays(14);

        // Act
        aggregate.Close(
            "Resolution summary.",
            "user-closer-1",
            closedAt);

        // Assert
        aggregate.ClosedAtUtc.ShouldBe(closedAt);
    }

    [Fact]
    public void Close_RaisesBreachClosedEvent()
    {
        // Arrange
        var aggregate = CreateSubjectsNotifiedAggregate();

        // Act
        aggregate.Close(
            "Resolution summary.",
            "user-closer-1",
            Now.AddDays(30));

        // Assert
        aggregate.UncommittedEvents[^1].ShouldBeOfType<BreachClosed>();
    }

    [Fact]
    public void Close_EventContainsCorrectData()
    {
        // Arrange
        var aggregate = CreateSubjectsNotifiedAggregate();
        var closedAt = Now.AddDays(30);

        // Act
        aggregate.Close(
            "Root cause identified. All measures applied.",
            "user-closer-1",
            closedAt);

        // Assert
        var evt = aggregate.UncommittedEvents[^1].ShouldBeOfType<BreachClosed>().Subject;
        evt.BreachId.ShouldBe(aggregate.Id);
        evt.ResolutionSummary.ShouldBe("Root cause identified. All measures applied.");
        evt.ClosedByUserId.ShouldBe("user-closer-1");
        evt.ClosedAtUtc.ShouldBe(closedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Close_NullOrWhiteSpaceResolutionSummary_ThrowsArgumentException(string? summary)
    {
        // Arrange
        var aggregate = CreateSubjectsNotifiedAggregate();

        // Act
        var act = () => aggregate.Close(summary!, "user-1", Now.AddDays(1));

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Close_NullOrWhiteSpaceClosedByUserId_ThrowsArgumentException(string? userId)
    {
        // Arrange
        var aggregate = CreateSubjectsNotifiedAggregate();

        // Act
        var act = () => aggregate.Close("Summary.", userId!, Now.AddDays(1));

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    #endregion

    #region Invalid State Transitions

    [Fact]
    public void Assess_FromInvestigating_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = CreateAssessedAggregate();

        // Act
        var act = () => aggregate.Assess(
            BreachSeverity.Critical, 2000, "Re-assessment.", "user-1", Now.AddHours(5));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Investigating");
    }

    [Fact]
    public void Assess_FromAuthorityNotified_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = CreateAuthorityNotifiedAggregate();

        // Act
        var act = () => aggregate.Assess(
            BreachSeverity.Critical, 2000, "Late assessment.", "user-1", Now.AddHours(24));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("AuthorityNotified");
    }

    [Fact]
    public void Assess_FromSubjectsNotified_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = CreateSubjectsNotifiedAggregate();

        // Act
        var act = () => aggregate.Assess(
            BreachSeverity.Critical, 2000, "Assessment.", "user-1", Now.AddHours(48));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("SubjectsNotified");
    }

    [Fact]
    public void Assess_FromResolved_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = CreateResolvedAggregate();

        // Act
        var act = () => aggregate.Assess(
            BreachSeverity.Critical, 2000, "Assessment.", "user-1", Now.AddDays(7));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Resolved");
    }

    [Fact]
    public void Assess_FromClosed_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = CreateClosedAggregate();

        // Act
        var act = () => aggregate.Assess(
            BreachSeverity.Critical, 2000, "Assessment.", "user-1", Now.AddDays(60));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Closed");
    }

    [Fact]
    public void ReportToDPA_FromAuthorityNotified_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = CreateAuthorityNotifiedAggregate();

        // Act
        var act = () => aggregate.ReportToDPA(
            "Another DPA", "contact", "summary", "user-1", Now.AddHours(24));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("AuthorityNotified");
    }

    [Fact]
    public void ReportToDPA_FromSubjectsNotified_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = CreateSubjectsNotifiedAggregate();

        // Act
        var act = () => aggregate.ReportToDPA(
            "DPA", "contact", "summary", "user-1", Now.AddHours(48));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("SubjectsNotified");
    }

    [Fact]
    public void ReportToDPA_FromResolved_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = CreateResolvedAggregate();

        // Act
        var act = () => aggregate.ReportToDPA(
            "DPA", "contact", "summary", "user-1", Now.AddDays(7));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Resolved");
    }

    [Fact]
    public void ReportToDPA_FromClosed_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = CreateClosedAggregate();

        // Act
        var act = () => aggregate.ReportToDPA(
            "DPA", "contact", "summary", "user-1", Now.AddDays(60));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Closed");
    }

    [Fact]
    public void NotifySubjects_FromDetected_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = CreateDetectedAggregate();

        // Act
        var act = () => aggregate.NotifySubjects(
            100, "email", SubjectNotificationExemption.None, "user-1", Now.AddHours(1));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Detected");
    }

    [Fact]
    public void NotifySubjects_FromInvestigating_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = CreateAssessedAggregate();

        // Act
        var act = () => aggregate.NotifySubjects(
            100, "email", SubjectNotificationExemption.None, "user-1", Now.AddHours(6));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Investigating");
    }

    [Fact]
    public void NotifySubjects_FromSubjectsNotified_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = CreateSubjectsNotifiedAggregate();

        // Act
        var act = () => aggregate.NotifySubjects(
            100, "email", SubjectNotificationExemption.None, "user-1", Now.AddHours(48));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("SubjectsNotified");
    }

    [Fact]
    public void NotifySubjects_FromResolved_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = CreateResolvedAggregate();

        // Act
        var act = () => aggregate.NotifySubjects(
            100, "email", SubjectNotificationExemption.None, "user-1", Now.AddDays(7));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Resolved");
    }

    [Fact]
    public void NotifySubjects_FromClosed_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = CreateClosedAggregate();

        // Act
        var act = () => aggregate.NotifySubjects(
            100, "email", SubjectNotificationExemption.None, "user-1", Now.AddDays(60));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Closed");
    }

    [Fact]
    public void AddPhasedReport_FromClosed_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = CreateClosedAggregate();

        // Act
        var act = () => aggregate.AddPhasedReport(
            "Late report.", "user-1", Now.AddDays(60));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("closed");
    }

    [Fact]
    public void Contain_FromClosed_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = CreateClosedAggregate();

        // Act
        var act = () => aggregate.Contain(
            "Containment measures.", "user-1", Now.AddDays(60));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("closed");
    }

    [Fact]
    public void Close_FromDetected_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = CreateDetectedAggregate();

        // Act
        var act = () => aggregate.Close(
            "Premature closure.", "user-1", Now.AddHours(1));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Detected");
    }

    [Fact]
    public void Close_FromInvestigating_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = CreateAssessedAggregate();

        // Act
        var act = () => aggregate.Close(
            "Premature closure.", "user-1", Now.AddHours(6));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Investigating");
    }

    [Fact]
    public void Close_FromAuthorityNotified_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = CreateAuthorityNotifiedAggregate();

        // Act
        var act = () => aggregate.Close(
            "Premature closure.", "user-1", Now.AddHours(24));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("AuthorityNotified");
    }

    [Fact]
    public void Close_FromClosed_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = CreateClosedAggregate();

        // Act
        var act = () => aggregate.Close(
            "Double closure.", "user-1", Now.AddDays(60));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Closed");
    }

    #endregion

    #region Full Lifecycle

    [Fact]
    public void FullLifecycle_DetectThroughClose_AllTransitionsSucceed()
    {
        // Arrange & Act: Detect
        var aggregate = BreachAggregate.Detect(
            DefaultId,
            "Data exfiltration",
            BreachSeverity.High,
            "rule-exfil",
            500,
            "Database records exfiltrated through compromised endpoint.",
            "user-detector-1",
            Now,
            "tenant-1",
            "module-1");

        aggregate.Status.ShouldBe(BreachStatus.Detected);
        aggregate.DeadlineUtc.ShouldBe(Now.AddHours(72));

        // Act: Assess
        var assessedAt = Now.AddHours(2);
        aggregate.Assess(
            BreachSeverity.Critical,
            1200,
            "Scope larger than initial estimate.",
            "user-assessor-1",
            assessedAt);

        aggregate.Status.ShouldBe(BreachStatus.Investigating);
        aggregate.Severity.ShouldBe(BreachSeverity.Critical);
        aggregate.EstimatedAffectedSubjects.ShouldBe(1200);
        aggregate.AssessedAtUtc.ShouldBe(assessedAt);

        // Act: ReportToDPA
        var reportedAt = Now.AddHours(12);
        aggregate.ReportToDPA(
            "AEPD",
            "dpo@aepd.es",
            "Comprehensive breach report per Art. 33(3).",
            "user-reporter-1",
            reportedAt);

        aggregate.Status.ShouldBe(BreachStatus.AuthorityNotified);
        aggregate.AuthorityName.ShouldBe("AEPD");
        aggregate.ReportedToDPAAtUtc.ShouldBe(reportedAt);

        // Act: NotifySubjects
        var notifiedAt = Now.AddHours(24);
        aggregate.NotifySubjects(
            1200,
            "email",
            SubjectNotificationExemption.None,
            "user-notifier-1",
            notifiedAt);

        aggregate.Status.ShouldBe(BreachStatus.SubjectsNotified);
        aggregate.SubjectCount.ShouldBe(1200);
        aggregate.NotifiedSubjectsAtUtc.ShouldBe(notifiedAt);

        // Act: Contain
        var containedAt = Now.AddHours(48);
        aggregate.Contain(
            "All credentials rotated, endpoint patched, monitoring enhanced.",
            "user-containment-1",
            containedAt);

        aggregate.Status.ShouldBe(BreachStatus.Resolved);
        aggregate.ContainedAtUtc.ShouldBe(containedAt);

        // Act: Close
        var closedAt = Now.AddDays(30);
        aggregate.Close(
            "Root cause: SQL injection. Remediation: parameterized queries, WAF rules, security audit.",
            "user-closer-1",
            closedAt);

        aggregate.Status.ShouldBe(BreachStatus.Closed);
        aggregate.ClosedAtUtc.ShouldBe(closedAt);

        // Assert: All events raised in order
        aggregate.UncommittedEvents.Count.ShouldBe(6);
        aggregate.UncommittedEvents[0].ShouldBeOfType<BreachDetected>();
        aggregate.UncommittedEvents[1].ShouldBeOfType<BreachAssessed>();
        aggregate.UncommittedEvents[2].ShouldBeOfType<BreachReportedToDPA>();
        aggregate.UncommittedEvents[3].ShouldBeOfType<BreachNotifiedToSubjects>();
        aggregate.UncommittedEvents[4].ShouldBeOfType<BreachContained>();
        aggregate.UncommittedEvents[5].ShouldBeOfType<BreachClosed>();
        aggregate.Version.ShouldBe(6);

        // Assert: TenantId and ModuleId preserved throughout
        aggregate.TenantId.ShouldBe("tenant-1");
        aggregate.ModuleId.ShouldBe("module-1");
    }

    [Fact]
    public void FullLifecycle_WithPhasedReports_AllEventsTracked()
    {
        // Arrange & Act
        var aggregate = CreateDetectedAggregate();

        aggregate.AddPhasedReport("Phase 1: Initial scope assessment.", "user-1", Now.AddHours(4));
        aggregate.Assess(
            BreachSeverity.Critical, 1500, "Full assessment.", "user-1", Now.AddHours(6));
        aggregate.AddPhasedReport("Phase 2: Updated scope after assessment.", "user-1", Now.AddHours(8));
        aggregate.ReportToDPA(
            "AEPD", "dpo@aepd.es", "Report.", "user-1", Now.AddHours(12));
        aggregate.AddPhasedReport("Phase 3: Post-notification update.", "user-1", Now.AddHours(18));

        // Assert
        aggregate.PhasedReportCount.ShouldBe(3);

        var phasedEvents = aggregate.UncommittedEvents
            .OfType<BreachPhasedReportAdded>()
            .ToList();

        phasedEvents.Count.ShouldBe(3);
        phasedEvents[0].PhaseNumber.ShouldBe(1);
        phasedEvents[1].PhaseNumber.ShouldBe(2);
        phasedEvents[2].PhaseNumber.ShouldBe(3);
    }

    [Fact]
    public void FullLifecycle_SkipAssess_DetectedDirectlyToAuthorityNotified()
    {
        // Arrange: Per Art. 33(1), authority can be notified before assessment
        var aggregate = CreateDetectedAggregate();

        // Act
        aggregate.ReportToDPA(
            "AEPD",
            "dpo@aepd.es",
            "Emergency notification within 72h deadline.",
            "user-reporter-1",
            Now.AddHours(6));

        // Assert
        aggregate.Status.ShouldBe(BreachStatus.AuthorityNotified);
        aggregate.AssessedAtUtc.ShouldBeNull();
    }

    #endregion
}
