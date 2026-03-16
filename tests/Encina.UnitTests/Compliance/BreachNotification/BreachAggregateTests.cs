using Encina.Compliance.BreachNotification.Aggregates;
using Encina.Compliance.BreachNotification.Events;
using Encina.Compliance.BreachNotification.Model;
using FluentAssertions;

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
        aggregate.Status.Should().Be(BreachStatus.Detected);
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
        aggregate.Id.Should().Be(DefaultId);
        aggregate.Nature.Should().Be("Data exfiltration via API");
        aggregate.Severity.Should().Be(BreachSeverity.Critical);
        aggregate.EstimatedAffectedSubjects.Should().Be(2000);
        aggregate.Description.Should().Be("Sensitive data was exfiltrated through an unprotected API endpoint.");
        aggregate.DetectedAtUtc.Should().Be(Now);
        aggregate.TenantId.Should().Be("tenant-1");
        aggregate.ModuleId.Should().Be("module-1");
    }

    [Fact]
    public void Detect_ValidParameters_CalculatesDeadlineAs72Hours()
    {
        // Act
        var aggregate = CreateDetectedAggregate();

        // Assert
        aggregate.DeadlineUtc.Should().Be(Now.AddHours(72));
    }

    [Fact]
    public void Detect_ValidParameters_RaisesBreachDetectedEvent()
    {
        // Act
        var aggregate = CreateDetectedAggregate();

        // Assert
        aggregate.UncommittedEvents.Should().ContainSingle()
            .Which.Should().BeOfType<BreachDetected>();
        aggregate.Version.Should().Be(1);
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
        var evt = aggregate.UncommittedEvents.Should().ContainSingle()
            .Which.Should().BeOfType<BreachDetected>().Subject;

        evt.BreachId.Should().Be(DefaultId);
        evt.Nature.Should().Be("Unauthorized access");
        evt.Severity.Should().Be(BreachSeverity.High);
        evt.DetectedByRule.Should().Be("rule-access");
        evt.EstimatedAffectedSubjects.Should().Be(500);
        evt.Description.Should().Be("Breach description.");
        evt.DetectedByUserId.Should().Be("user-1");
        evt.DetectedAtUtc.Should().Be(Now);
        evt.TenantId.Should().Be("tenant-1");
        evt.ModuleId.Should().Be("module-1");
    }

    [Fact]
    public void Detect_WithTenantAndModule_SetsTenantIdAndModuleId()
    {
        // Act
        var aggregate = CreateDetectedAggregate(tenantId: "tenant-abc", moduleId: "module-xyz");

        // Assert
        aggregate.TenantId.Should().Be("tenant-abc");
        aggregate.ModuleId.Should().Be("module-xyz");
    }

    [Fact]
    public void Detect_WithoutTenantAndModule_DefaultsToNull()
    {
        // Act
        var aggregate = CreateDetectedAggregate();

        // Assert
        aggregate.TenantId.Should().BeNull();
        aggregate.ModuleId.Should().BeNull();
    }

    [Fact]
    public void Detect_InitialState_TimestampsAreNull()
    {
        // Act
        var aggregate = CreateDetectedAggregate();

        // Assert
        aggregate.AssessedAtUtc.Should().BeNull();
        aggregate.ReportedToDPAAtUtc.Should().BeNull();
        aggregate.NotifiedSubjectsAtUtc.Should().BeNull();
        aggregate.ContainedAtUtc.Should().BeNull();
        aggregate.ClosedAtUtc.Should().BeNull();
        aggregate.AuthorityName.Should().BeNull();
        aggregate.SubjectCount.Should().Be(0);
        aggregate.PhasedReportCount.Should().Be(0);
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
        aggregate.Status.Should().Be(BreachStatus.Detected);
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
        act.Should().Throw<ArgumentException>();
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
        act.Should().Throw<ArgumentException>();
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
        act.Should().Throw<ArgumentException>();
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
        aggregate.Status.Should().Be(BreachStatus.Investigating);
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
        aggregate.Severity.Should().Be(BreachSeverity.Critical);
        aggregate.EstimatedAffectedSubjects.Should().Be(1500);
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
        aggregate.AssessedAtUtc.Should().Be(assessedAt);
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
        aggregate.UncommittedEvents.Should().HaveCount(2);
        aggregate.UncommittedEvents[^1].Should().BeOfType<BreachAssessed>();
        aggregate.Version.Should().Be(2);
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
        var evt = aggregate.UncommittedEvents[^1].Should().BeOfType<BreachAssessed>().Subject;
        evt.BreachId.Should().Be(aggregate.Id);
        evt.UpdatedSeverity.Should().Be(BreachSeverity.Critical);
        evt.UpdatedAffectedSubjects.Should().Be(1200);
        evt.AssessmentSummary.Should().Be("Full assessment summary.");
        evt.AssessedByUserId.Should().Be("user-assessor-1");
        evt.AssessedAtUtc.Should().Be(assessedAt);
        evt.TenantId.Should().Be("tenant-1");
        evt.ModuleId.Should().Be("module-1");
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
        act.Should().Throw<ArgumentException>();
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
        act.Should().Throw<ArgumentException>();
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
        aggregate.Status.Should().Be(BreachStatus.AuthorityNotified);
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
        aggregate.Status.Should().Be(BreachStatus.AuthorityNotified);
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
        aggregate.AuthorityName.Should().Be("ICO");
        aggregate.ReportedToDPAAtUtc.Should().Be(reportedAt);
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
        aggregate.UncommittedEvents.Should().HaveCount(2);
        aggregate.UncommittedEvents[^1].Should().BeOfType<BreachReportedToDPA>();
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
        var evt = aggregate.UncommittedEvents[^1].Should().BeOfType<BreachReportedToDPA>().Subject;
        evt.BreachId.Should().Be(aggregate.Id);
        evt.AuthorityName.Should().Be("AEPD");
        evt.AuthorityContactInfo.Should().Be("dpo@aepd.es");
        evt.ReportSummary.Should().Be("Full report.");
        evt.ReportedByUserId.Should().Be("user-reporter-1");
        evt.ReportedAtUtc.Should().Be(reportedAt);
        evt.TenantId.Should().Be("tenant-1");
        evt.ModuleId.Should().Be("module-1");
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
        act.Should().Throw<ArgumentException>();
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
        act.Should().Throw<ArgumentException>();
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
        act.Should().Throw<ArgumentException>();
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
        act.Should().Throw<ArgumentException>();
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
        aggregate.Status.Should().Be(BreachStatus.SubjectsNotified);
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
        aggregate.SubjectCount.Should().Be(950);
        aggregate.NotifiedSubjectsAtUtc.Should().Be(notifiedAt);
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
        aggregate.UncommittedEvents[^1].Should().BeOfType<BreachNotifiedToSubjects>();
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
        var evt = aggregate.UncommittedEvents[^1].Should().BeOfType<BreachNotifiedToSubjects>().Subject;
        evt.BreachId.Should().Be(aggregate.Id);
        evt.SubjectCount.Should().Be(800);
        evt.CommunicationMethod.Should().Be("public-notice");
        evt.Exemption.Should().Be(SubjectNotificationExemption.DisproportionateEffort);
        evt.NotifiedByUserId.Should().Be("user-notifier-1");
        evt.NotifiedAtUtc.Should().Be(notifiedAt);
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
        act.Should().Throw<ArgumentException>();
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
        act.Should().Throw<ArgumentException>();
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
        aggregate.PhasedReportCount.Should().Be(1);
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
        aggregate.PhasedReportCount.Should().Be(3);
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
        aggregate.UncommittedEvents.Should().HaveCount(2);
        aggregate.UncommittedEvents[^1].Should().BeOfType<BreachPhasedReportAdded>();
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

        events.Should().HaveCount(2);
        events[0].PhaseNumber.Should().Be(1);
        events[1].PhaseNumber.Should().Be(2);
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
        var evt = aggregate.UncommittedEvents[^1].Should().BeOfType<BreachPhasedReportAdded>().Subject;
        evt.BreachId.Should().Be(aggregate.Id);
        evt.PhaseNumber.Should().Be(1);
        evt.ReportContent.Should().Be("Detailed phase 1 findings.");
        evt.SubmittedByUserId.Should().Be("user-reporter-1");
        evt.SubmittedAtUtc.Should().Be(submittedAt);
        evt.TenantId.Should().Be("tenant-1");
        evt.ModuleId.Should().Be("module-1");
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
        aggregate.Status.Should().Be(statusBefore);
    }

    [Fact]
    public void AddPhasedReport_FromInvestigating_Succeeds()
    {
        // Arrange
        var aggregate = CreateAssessedAggregate();

        // Act
        aggregate.AddPhasedReport("Phase report during investigation.", "user-1", Now.AddHours(6));

        // Assert
        aggregate.PhasedReportCount.Should().Be(1);
    }

    [Fact]
    public void AddPhasedReport_FromAuthorityNotified_Succeeds()
    {
        // Arrange
        var aggregate = CreateAuthorityNotifiedAggregate();

        // Act
        aggregate.AddPhasedReport("Phase report after DPA notification.", "user-1", Now.AddHours(24));

        // Assert
        aggregate.PhasedReportCount.Should().Be(1);
    }

    [Fact]
    public void AddPhasedReport_FromSubjectsNotified_Succeeds()
    {
        // Arrange
        var aggregate = CreateSubjectsNotifiedAggregate();

        // Act
        aggregate.AddPhasedReport("Phase report after subject notification.", "user-1", Now.AddHours(48));

        // Assert
        aggregate.PhasedReportCount.Should().Be(1);
    }

    [Fact]
    public void AddPhasedReport_FromResolved_Succeeds()
    {
        // Arrange
        var aggregate = CreateResolvedAggregate();

        // Act
        aggregate.AddPhasedReport("Final phase report after containment.", "user-1", Now.AddDays(7));

        // Assert
        aggregate.PhasedReportCount.Should().Be(1);
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
        act.Should().Throw<ArgumentException>();
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
        act.Should().Throw<ArgumentException>();
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
        aggregate.Status.Should().Be(BreachStatus.Resolved);
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
        aggregate.Status.Should().Be(BreachStatus.Resolved);
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
        aggregate.Status.Should().Be(BreachStatus.Resolved);
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
        aggregate.Status.Should().Be(BreachStatus.Resolved);
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
        aggregate.ContainedAtUtc.Should().Be(containedAt);
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
        aggregate.UncommittedEvents[^1].Should().BeOfType<BreachContained>();
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
        var evt = aggregate.UncommittedEvents[^1].Should().BeOfType<BreachContained>().Subject;
        evt.BreachId.Should().Be(aggregate.Id);
        evt.ContainmentMeasures.Should().Be("Credentials rotated.");
        evt.ContainedByUserId.Should().Be("user-containment-1");
        evt.ContainedAtUtc.Should().Be(containedAt);
        evt.TenantId.Should().Be("tenant-1");
        evt.ModuleId.Should().Be("module-1");
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
        act.Should().Throw<ArgumentException>();
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
        act.Should().Throw<ArgumentException>();
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
        aggregate.Status.Should().Be(BreachStatus.Closed);
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
        aggregate.Status.Should().Be(BreachStatus.Closed);
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
        aggregate.ClosedAtUtc.Should().Be(closedAt);
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
        aggregate.UncommittedEvents[^1].Should().BeOfType<BreachClosed>();
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
        var evt = aggregate.UncommittedEvents[^1].Should().BeOfType<BreachClosed>().Subject;
        evt.BreachId.Should().Be(aggregate.Id);
        evt.ResolutionSummary.Should().Be("Root cause identified. All measures applied.");
        evt.ClosedByUserId.Should().Be("user-closer-1");
        evt.ClosedAtUtc.Should().Be(closedAt);
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
        act.Should().Throw<ArgumentException>();
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
        act.Should().Throw<ArgumentException>();
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
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Investigating*");
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
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*AuthorityNotified*");
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
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*SubjectsNotified*");
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
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Resolved*");
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
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Closed*");
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
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*AuthorityNotified*");
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
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*SubjectsNotified*");
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
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Resolved*");
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
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Closed*");
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
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Detected*");
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
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Investigating*");
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
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*SubjectsNotified*");
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
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Resolved*");
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
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Closed*");
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
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*closed*");
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
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*closed*");
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
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Detected*");
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
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Investigating*");
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
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*AuthorityNotified*");
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
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Closed*");
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

        aggregate.Status.Should().Be(BreachStatus.Detected);
        aggregate.DeadlineUtc.Should().Be(Now.AddHours(72));

        // Act: Assess
        var assessedAt = Now.AddHours(2);
        aggregate.Assess(
            BreachSeverity.Critical,
            1200,
            "Scope larger than initial estimate.",
            "user-assessor-1",
            assessedAt);

        aggregate.Status.Should().Be(BreachStatus.Investigating);
        aggregate.Severity.Should().Be(BreachSeverity.Critical);
        aggregate.EstimatedAffectedSubjects.Should().Be(1200);
        aggregate.AssessedAtUtc.Should().Be(assessedAt);

        // Act: ReportToDPA
        var reportedAt = Now.AddHours(12);
        aggregate.ReportToDPA(
            "AEPD",
            "dpo@aepd.es",
            "Comprehensive breach report per Art. 33(3).",
            "user-reporter-1",
            reportedAt);

        aggregate.Status.Should().Be(BreachStatus.AuthorityNotified);
        aggregate.AuthorityName.Should().Be("AEPD");
        aggregate.ReportedToDPAAtUtc.Should().Be(reportedAt);

        // Act: NotifySubjects
        var notifiedAt = Now.AddHours(24);
        aggregate.NotifySubjects(
            1200,
            "email",
            SubjectNotificationExemption.None,
            "user-notifier-1",
            notifiedAt);

        aggregate.Status.Should().Be(BreachStatus.SubjectsNotified);
        aggregate.SubjectCount.Should().Be(1200);
        aggregate.NotifiedSubjectsAtUtc.Should().Be(notifiedAt);

        // Act: Contain
        var containedAt = Now.AddHours(48);
        aggregate.Contain(
            "All credentials rotated, endpoint patched, monitoring enhanced.",
            "user-containment-1",
            containedAt);

        aggregate.Status.Should().Be(BreachStatus.Resolved);
        aggregate.ContainedAtUtc.Should().Be(containedAt);

        // Act: Close
        var closedAt = Now.AddDays(30);
        aggregate.Close(
            "Root cause: SQL injection. Remediation: parameterized queries, WAF rules, security audit.",
            "user-closer-1",
            closedAt);

        aggregate.Status.Should().Be(BreachStatus.Closed);
        aggregate.ClosedAtUtc.Should().Be(closedAt);

        // Assert: All events raised in order
        aggregate.UncommittedEvents.Should().HaveCount(6);
        aggregate.UncommittedEvents[0].Should().BeOfType<BreachDetected>();
        aggregate.UncommittedEvents[1].Should().BeOfType<BreachAssessed>();
        aggregate.UncommittedEvents[2].Should().BeOfType<BreachReportedToDPA>();
        aggregate.UncommittedEvents[3].Should().BeOfType<BreachNotifiedToSubjects>();
        aggregate.UncommittedEvents[4].Should().BeOfType<BreachContained>();
        aggregate.UncommittedEvents[5].Should().BeOfType<BreachClosed>();
        aggregate.Version.Should().Be(6);

        // Assert: TenantId and ModuleId preserved throughout
        aggregate.TenantId.Should().Be("tenant-1");
        aggregate.ModuleId.Should().Be("module-1");
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
        aggregate.PhasedReportCount.Should().Be(3);

        var phasedEvents = aggregate.UncommittedEvents
            .OfType<BreachPhasedReportAdded>()
            .ToList();

        phasedEvents.Should().HaveCount(3);
        phasedEvents[0].PhaseNumber.Should().Be(1);
        phasedEvents[1].PhaseNumber.Should().Be(2);
        phasedEvents[2].PhaseNumber.Should().Be(3);
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
        aggregate.Status.Should().Be(BreachStatus.AuthorityNotified);
        aggregate.AssessedAtUtc.Should().BeNull();
    }

    #endregion
}
