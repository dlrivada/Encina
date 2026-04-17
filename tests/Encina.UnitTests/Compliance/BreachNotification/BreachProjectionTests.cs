using Encina.Compliance.BreachNotification.Events;
using Encina.Compliance.BreachNotification.Model;
using Encina.Compliance.BreachNotification.ReadModels;
using Encina.Marten.Projections;
using Shouldly;

namespace Encina.UnitTests.Compliance.BreachNotification;

/// <summary>
/// Unit tests for <see cref="BreachProjection"/>.
/// </summary>
public class BreachProjectionTests
{
    private readonly BreachProjection _sut = new();
    private readonly ProjectionContext _context = new();

    #region Helpers

    private static BreachDetected CreateBreachDetectedEvent(
        Guid? breachId = null,
        string nature = "unauthorized access",
        BreachSeverity severity = BreachSeverity.High,
        string detectedByRule = "UnauthorizedAccessRule",
        int estimatedAffectedSubjects = 500,
        string description = "Unauthorized access to customer database detected",
        string? detectedByUserId = "security-system",
        DateTimeOffset? detectedAtUtc = null,
        string? tenantId = "tenant-1",
        string? moduleId = "module-1")
    {
        return new BreachDetected(
            BreachId: breachId ?? Guid.NewGuid(),
            Nature: nature,
            Severity: severity,
            DetectedByRule: detectedByRule,
            EstimatedAffectedSubjects: estimatedAffectedSubjects,
            Description: description,
            DetectedByUserId: detectedByUserId,
            DetectedAtUtc: detectedAtUtc ?? new DateTimeOffset(2026, 3, 15, 10, 0, 0, TimeSpan.Zero),
            TenantId: tenantId,
            ModuleId: moduleId);
    }

    private BreachReadModel CreateDetectedReadModel(Guid? breachId = null, int version = 1)
    {
        var detected = CreateBreachDetectedEvent(breachId: breachId);
        var readModel = _sut.Create(detected, _context);
        readModel.Version = version;
        return readModel;
    }

    #endregion

    #region ProjectionName

    [Fact]
    public void ProjectionName_ShouldReturnBreachProjection()
    {
        // Act
        var name = _sut.ProjectionName;

        // Assert
        name.ShouldBe("BreachProjection");
    }

    #endregion

    #region Create (BreachDetected)

    [Fact]
    public void Create_BreachDetected_ShouldMapAllFieldsCorrectly()
    {
        // Arrange
        var breachId = Guid.NewGuid();
        var detectedAt = new DateTimeOffset(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);

        var detected = CreateBreachDetectedEvent(
            breachId: breachId,
            nature: "data exfiltration",
            severity: BreachSeverity.Critical,
            detectedByRule: "MassDataExfiltrationRule",
            estimatedAffectedSubjects: 10_000,
            description: "Large-scale data exfiltration via compromised API key",
            detectedByUserId: "siem-agent",
            detectedAtUtc: detectedAt,
            tenantId: "tenant-A",
            moduleId: "module-B");

        // Act
        var result = _sut.Create(detected, _context);

        // Assert
        result.Id.ShouldBe(breachId);
        result.Nature.ShouldBe("data exfiltration");
        result.Severity.ShouldBe(BreachSeverity.Critical);
        result.DetectedByRule.ShouldBe("MassDataExfiltrationRule");
        result.EstimatedAffectedSubjects.ShouldBe(10_000);
        result.Description.ShouldBe("Large-scale data exfiltration via compromised API key");
        result.DetectedByUserId.ShouldBe("siem-agent");
        result.DetectedAtUtc.ShouldBe(detectedAt);
        result.TenantId.ShouldBe("tenant-A");
        result.ModuleId.ShouldBe("module-B");
    }

    [Fact]
    public void Create_BreachDetected_ShouldSetStatusToDetected()
    {
        // Arrange
        var detected = CreateBreachDetectedEvent();

        // Act
        var result = _sut.Create(detected, _context);

        // Assert
        result.Status.ShouldBe(BreachStatus.Detected);
    }

    [Fact]
    public void Create_BreachDetected_ShouldCalculateDeadlineAs72HoursFromDetection()
    {
        // Arrange
        var detectedAt = new DateTimeOffset(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);
        var detected = CreateBreachDetectedEvent(detectedAtUtc: detectedAt);

        // Act
        var result = _sut.Create(detected, _context);

        // Assert
        result.DeadlineUtc.ShouldBe(detectedAt.AddHours(72));
    }

    [Fact]
    public void Create_BreachDetected_ShouldSetVersionToOne()
    {
        // Arrange
        var detected = CreateBreachDetectedEvent();

        // Act
        var result = _sut.Create(detected, _context);

        // Assert
        result.Version.ShouldBe(1);
    }

    [Fact]
    public void Create_BreachDetected_ShouldSetLastModifiedAtUtcToDetectedTime()
    {
        // Arrange
        var detectedAt = new DateTimeOffset(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);
        var detected = CreateBreachDetectedEvent(detectedAtUtc: detectedAt);

        // Act
        var result = _sut.Create(detected, _context);

        // Assert
        result.LastModifiedAtUtc.ShouldBe(detectedAt);
    }

    [Fact]
    public void Create_BreachDetected_WithNullDetectedByUserId_ShouldSetNull()
    {
        // Arrange
        var detected = CreateBreachDetectedEvent(detectedByUserId: null);

        // Act
        var result = _sut.Create(detected, _context);

        // Assert
        result.DetectedByUserId.ShouldBeNull();
    }

    #endregion

    #region Apply (BreachAssessed)

    [Fact]
    public void Apply_BreachAssessed_ShouldUpdateSeverityAndSubjects()
    {
        // Arrange
        var readModel = CreateDetectedReadModel();
        var assessedAt = new DateTimeOffset(2026, 3, 15, 14, 0, 0, TimeSpan.Zero);
        var assessed = new BreachAssessed(
            BreachId: readModel.Id,
            UpdatedSeverity: BreachSeverity.Critical,
            UpdatedAffectedSubjects: 25_000,
            AssessmentSummary: "Investigation reveals broader scope than initially estimated",
            AssessedByUserId: "analyst-42",
            AssessedAtUtc: assessedAt,
            TenantId: "tenant-1",
            ModuleId: "module-1");

        // Act
        var result = _sut.Apply(assessed, readModel, _context);

        // Assert
        result.Severity.ShouldBe(BreachSeverity.Critical);
        result.EstimatedAffectedSubjects.ShouldBe(25_000);
        result.AssessmentSummary.ShouldBe("Investigation reveals broader scope than initially estimated");
        result.AssessedAtUtc.ShouldBe(assessedAt);
    }

    [Fact]
    public void Apply_BreachAssessed_ShouldSetStatusToInvestigating()
    {
        // Arrange
        var readModel = CreateDetectedReadModel();
        var assessed = new BreachAssessed(
            BreachId: readModel.Id,
            UpdatedSeverity: BreachSeverity.Medium,
            UpdatedAffectedSubjects: 100,
            AssessmentSummary: "Limited scope confirmed",
            AssessedByUserId: "analyst-1",
            AssessedAtUtc: DateTimeOffset.UtcNow,
            TenantId: null,
            ModuleId: null);

        // Act
        var result = _sut.Apply(assessed, readModel, _context);

        // Assert
        result.Status.ShouldBe(BreachStatus.Investigating);
    }

    [Fact]
    public void Apply_BreachAssessed_ShouldUpdateLastModifiedAtUtc()
    {
        // Arrange
        var readModel = CreateDetectedReadModel();
        var assessedAt = new DateTimeOffset(2026, 3, 15, 16, 30, 0, TimeSpan.Zero);
        var assessed = new BreachAssessed(
            BreachId: readModel.Id,
            UpdatedSeverity: BreachSeverity.High,
            UpdatedAffectedSubjects: 1_000,
            AssessmentSummary: "Assessment complete",
            AssessedByUserId: "analyst-7",
            AssessedAtUtc: assessedAt,
            TenantId: null,
            ModuleId: null);

        // Act
        var result = _sut.Apply(assessed, readModel, _context);

        // Assert
        result.LastModifiedAtUtc.ShouldBe(assessedAt);
    }

    [Fact]
    public void Apply_BreachAssessed_ShouldIncrementVersion()
    {
        // Arrange
        var readModel = CreateDetectedReadModel(version: 1);
        var assessed = new BreachAssessed(
            BreachId: readModel.Id,
            UpdatedSeverity: BreachSeverity.High,
            UpdatedAffectedSubjects: 500,
            AssessmentSummary: "Confirmed",
            AssessedByUserId: "analyst-1",
            AssessedAtUtc: DateTimeOffset.UtcNow,
            TenantId: null,
            ModuleId: null);

        // Act
        var result = _sut.Apply(assessed, readModel, _context);

        // Assert
        result.Version.ShouldBe(2);
    }

    #endregion

    #region Apply (BreachReportedToDPA)

    [Fact]
    public void Apply_BreachReportedToDPA_ShouldUpdateAuthorityInfo()
    {
        // Arrange
        var readModel = CreateDetectedReadModel();
        var reportedAt = new DateTimeOffset(2026, 3, 16, 8, 0, 0, TimeSpan.Zero);
        var reported = new BreachReportedToDPA(
            BreachId: readModel.Id,
            AuthorityName: "AEPD",
            AuthorityContactInfo: "breach@aepd.es",
            ReportSummary: "Initial notification per Art. 33(1)",
            ReportedByUserId: "dpo-1",
            ReportedAtUtc: reportedAt,
            TenantId: "tenant-1",
            ModuleId: "module-1");

        // Act
        var result = _sut.Apply(reported, readModel, _context);

        // Assert
        result.AuthorityName.ShouldBe("AEPD");
        result.ReportedToDPAAtUtc.ShouldBe(reportedAt);
    }

    [Fact]
    public void Apply_BreachReportedToDPA_ShouldSetStatusToAuthorityNotified()
    {
        // Arrange
        var readModel = CreateDetectedReadModel();
        var reported = new BreachReportedToDPA(
            BreachId: readModel.Id,
            AuthorityName: "ICO",
            AuthorityContactInfo: "casework@ico.org.uk",
            ReportSummary: "Notification report",
            ReportedByUserId: "dpo-1",
            ReportedAtUtc: DateTimeOffset.UtcNow,
            TenantId: null,
            ModuleId: null);

        // Act
        var result = _sut.Apply(reported, readModel, _context);

        // Assert
        result.Status.ShouldBe(BreachStatus.AuthorityNotified);
    }

    [Fact]
    public void Apply_BreachReportedToDPA_ShouldUpdateLastModifiedAtUtcAndIncrementVersion()
    {
        // Arrange
        var readModel = CreateDetectedReadModel(version: 2);
        var reportedAt = new DateTimeOffset(2026, 3, 16, 9, 15, 0, TimeSpan.Zero);
        var reported = new BreachReportedToDPA(
            BreachId: readModel.Id,
            AuthorityName: "CNIL",
            AuthorityContactInfo: "notifications@cnil.fr",
            ReportSummary: "Report filed",
            ReportedByUserId: "dpo-2",
            ReportedAtUtc: reportedAt,
            TenantId: null,
            ModuleId: null);

        // Act
        var result = _sut.Apply(reported, readModel, _context);

        // Assert
        result.LastModifiedAtUtc.ShouldBe(reportedAt);
        result.Version.ShouldBe(3);
    }

    #endregion

    #region Apply (BreachNotifiedToSubjects)

    [Fact]
    public void Apply_BreachNotifiedToSubjects_ShouldUpdateSubjectNotificationFields()
    {
        // Arrange
        var readModel = CreateDetectedReadModel();
        var notifiedAt = new DateTimeOffset(2026, 3, 17, 12, 0, 0, TimeSpan.Zero);
        var notified = new BreachNotifiedToSubjects(
            BreachId: readModel.Id,
            SubjectCount: 3_500,
            CommunicationMethod: "email",
            Exemption: SubjectNotificationExemption.None,
            NotifiedByUserId: "comms-team",
            NotifiedAtUtc: notifiedAt,
            TenantId: "tenant-1",
            ModuleId: "module-1");

        // Act
        var result = _sut.Apply(notified, readModel, _context);

        // Assert
        result.SubjectCount.ShouldBe(3_500);
        result.CommunicationMethod.ShouldBe("email");
        result.Exemption.ShouldBe(SubjectNotificationExemption.None);
        result.NotifiedSubjectsAtUtc.ShouldBe(notifiedAt);
    }

    [Fact]
    public void Apply_BreachNotifiedToSubjects_ShouldSetStatusToSubjectsNotified()
    {
        // Arrange
        var readModel = CreateDetectedReadModel();
        var notified = new BreachNotifiedToSubjects(
            BreachId: readModel.Id,
            SubjectCount: 100,
            CommunicationMethod: "letter",
            Exemption: SubjectNotificationExemption.None,
            NotifiedByUserId: "comms-1",
            NotifiedAtUtc: DateTimeOffset.UtcNow,
            TenantId: null,
            ModuleId: null);

        // Act
        var result = _sut.Apply(notified, readModel, _context);

        // Assert
        result.Status.ShouldBe(BreachStatus.SubjectsNotified);
    }

    [Fact]
    public void Apply_BreachNotifiedToSubjects_WithExemption_ShouldRecordExemption()
    {
        // Arrange
        var readModel = CreateDetectedReadModel();
        var notified = new BreachNotifiedToSubjects(
            BreachId: readModel.Id,
            SubjectCount: 50_000,
            CommunicationMethod: "public-notice",
            Exemption: SubjectNotificationExemption.DisproportionateEffort,
            NotifiedByUserId: "comms-lead",
            NotifiedAtUtc: DateTimeOffset.UtcNow,
            TenantId: null,
            ModuleId: null);

        // Act
        var result = _sut.Apply(notified, readModel, _context);

        // Assert
        result.Exemption.ShouldBe(SubjectNotificationExemption.DisproportionateEffort);
    }

    [Fact]
    public void Apply_BreachNotifiedToSubjects_ShouldUpdateLastModifiedAtUtcAndIncrementVersion()
    {
        // Arrange
        var readModel = CreateDetectedReadModel(version: 3);
        var notifiedAt = new DateTimeOffset(2026, 3, 17, 14, 30, 0, TimeSpan.Zero);
        var notified = new BreachNotifiedToSubjects(
            BreachId: readModel.Id,
            SubjectCount: 200,
            CommunicationMethod: "email",
            Exemption: SubjectNotificationExemption.EncryptionProtected,
            NotifiedByUserId: "comms-1",
            NotifiedAtUtc: notifiedAt,
            TenantId: null,
            ModuleId: null);

        // Act
        var result = _sut.Apply(notified, readModel, _context);

        // Assert
        result.LastModifiedAtUtc.ShouldBe(notifiedAt);
        result.Version.ShouldBe(4);
    }

    #endregion

    #region Apply (BreachPhasedReportAdded)

    [Fact]
    public void Apply_BreachPhasedReportAdded_ShouldAddEntryToPhasedReportsList()
    {
        // Arrange
        var readModel = CreateDetectedReadModel();
        var submittedAt = new DateTimeOffset(2026, 3, 16, 18, 0, 0, TimeSpan.Zero);
        var phasedReport = new BreachPhasedReportAdded(
            BreachId: readModel.Id,
            PhaseNumber: 1,
            ReportContent: "Initial findings: compromised credentials used to access customer records",
            SubmittedByUserId: "analyst-42",
            SubmittedAtUtc: submittedAt,
            TenantId: "tenant-1",
            ModuleId: "module-1");

        // Act
        var result = _sut.Apply(phasedReport, readModel, _context);

        // Assert
        result.PhasedReports.Count.ShouldBe(1);
        result.PhasedReports[0].PhaseNumber.ShouldBe(1);
        result.PhasedReports[0].ReportContent.ShouldBe("Initial findings: compromised credentials used to access customer records");
        result.PhasedReports[0].SubmittedByUserId.ShouldBe("analyst-42");
        result.PhasedReports[0].SubmittedAtUtc.ShouldBe(submittedAt);
    }

    [Fact]
    public void Apply_BreachPhasedReportAdded_MultipleTimes_ShouldAccumulateReports()
    {
        // Arrange
        var readModel = CreateDetectedReadModel();
        var t1 = new DateTimeOffset(2026, 3, 16, 18, 0, 0, TimeSpan.Zero);
        var t2 = t1.AddHours(12);

        var phase1 = new BreachPhasedReportAdded(
            BreachId: readModel.Id,
            PhaseNumber: 1,
            ReportContent: "Phase 1 findings",
            SubmittedByUserId: "analyst-1",
            SubmittedAtUtc: t1,
            TenantId: null,
            ModuleId: null);

        var phase2 = new BreachPhasedReportAdded(
            BreachId: readModel.Id,
            PhaseNumber: 2,
            ReportContent: "Phase 2 findings",
            SubmittedByUserId: "analyst-2",
            SubmittedAtUtc: t2,
            TenantId: null,
            ModuleId: null);

        // Act
        _sut.Apply(phase1, readModel, _context);
        var result = _sut.Apply(phase2, readModel, _context);

        // Assert
        result.PhasedReports.Count.ShouldBe(2);
        result.PhasedReports[0].PhaseNumber.ShouldBe(1);
        result.PhasedReports[1].PhaseNumber.ShouldBe(2);
    }

    [Fact]
    public void Apply_BreachPhasedReportAdded_ShouldUpdateLastModifiedAtUtcAndIncrementVersion()
    {
        // Arrange
        var readModel = CreateDetectedReadModel(version: 2);
        var submittedAt = new DateTimeOffset(2026, 3, 16, 20, 0, 0, TimeSpan.Zero);
        var phasedReport = new BreachPhasedReportAdded(
            BreachId: readModel.Id,
            PhaseNumber: 1,
            ReportContent: "First update",
            SubmittedByUserId: "analyst-1",
            SubmittedAtUtc: submittedAt,
            TenantId: null,
            ModuleId: null);

        // Act
        var result = _sut.Apply(phasedReport, readModel, _context);

        // Assert
        result.LastModifiedAtUtc.ShouldBe(submittedAt);
        result.Version.ShouldBe(3);
    }

    #endregion

    #region Apply (BreachContained)

    [Fact]
    public void Apply_BreachContained_ShouldUpdateContainmentFields()
    {
        // Arrange
        var readModel = CreateDetectedReadModel();
        var containedAt = new DateTimeOffset(2026, 3, 18, 6, 0, 0, TimeSpan.Zero);
        var contained = new BreachContained(
            BreachId: readModel.Id,
            ContainmentMeasures: "Revoked compromised API keys, rotated secrets, blocked suspicious IPs",
            ContainedByUserId: "incident-lead",
            ContainedAtUtc: containedAt,
            TenantId: "tenant-1",
            ModuleId: "module-1");

        // Act
        var result = _sut.Apply(contained, readModel, _context);

        // Assert
        result.ContainmentMeasures.ShouldBe("Revoked compromised API keys, rotated secrets, blocked suspicious IPs");
        result.ContainedAtUtc.ShouldBe(containedAt);
    }

    [Fact]
    public void Apply_BreachContained_ShouldSetStatusToResolved()
    {
        // Arrange
        var readModel = CreateDetectedReadModel();
        var contained = new BreachContained(
            BreachId: readModel.Id,
            ContainmentMeasures: "Patch applied",
            ContainedByUserId: "ops-1",
            ContainedAtUtc: DateTimeOffset.UtcNow,
            TenantId: null,
            ModuleId: null);

        // Act
        var result = _sut.Apply(contained, readModel, _context);

        // Assert
        result.Status.ShouldBe(BreachStatus.Resolved);
    }

    [Fact]
    public void Apply_BreachContained_ShouldUpdateLastModifiedAtUtcAndIncrementVersion()
    {
        // Arrange
        var readModel = CreateDetectedReadModel(version: 4);
        var containedAt = new DateTimeOffset(2026, 3, 18, 8, 30, 0, TimeSpan.Zero);
        var contained = new BreachContained(
            BreachId: readModel.Id,
            ContainmentMeasures: "Systems isolated",
            ContainedByUserId: "ops-2",
            ContainedAtUtc: containedAt,
            TenantId: null,
            ModuleId: null);

        // Act
        var result = _sut.Apply(contained, readModel, _context);

        // Assert
        result.LastModifiedAtUtc.ShouldBe(containedAt);
        result.Version.ShouldBe(5);
    }

    #endregion

    #region Apply (BreachClosed)

    [Fact]
    public void Apply_BreachClosed_ShouldUpdateResolutionFields()
    {
        // Arrange
        var readModel = CreateDetectedReadModel();
        var closedAt = new DateTimeOffset(2026, 3, 25, 16, 0, 0, TimeSpan.Zero);
        var closed = new BreachClosed(
            BreachId: readModel.Id,
            ResolutionSummary: "Root cause: compromised third-party dependency. Remediation: updated dependency, rotated all credentials, implemented WAF rules.",
            ClosedByUserId: "ciso",
            ClosedAtUtc: closedAt,
            TenantId: "tenant-1",
            ModuleId: "module-1");

        // Act
        var result = _sut.Apply(closed, readModel, _context);

        // Assert
        result.ResolutionSummary.ShouldBe("Root cause: compromised third-party dependency. Remediation: updated dependency, rotated all credentials, implemented WAF rules.");
        result.ClosedAtUtc.ShouldBe(closedAt);
    }

    [Fact]
    public void Apply_BreachClosed_ShouldSetStatusToClosed()
    {
        // Arrange
        var readModel = CreateDetectedReadModel();
        var closed = new BreachClosed(
            BreachId: readModel.Id,
            ResolutionSummary: "Case resolved",
            ClosedByUserId: "manager-1",
            ClosedAtUtc: DateTimeOffset.UtcNow,
            TenantId: null,
            ModuleId: null);

        // Act
        var result = _sut.Apply(closed, readModel, _context);

        // Assert
        result.Status.ShouldBe(BreachStatus.Closed);
    }

    [Fact]
    public void Apply_BreachClosed_ShouldUpdateLastModifiedAtUtcAndIncrementVersion()
    {
        // Arrange
        var readModel = CreateDetectedReadModel(version: 6);
        var closedAt = new DateTimeOffset(2026, 3, 25, 17, 0, 0, TimeSpan.Zero);
        var closed = new BreachClosed(
            BreachId: readModel.Id,
            ResolutionSummary: "Fully remediated",
            ClosedByUserId: "ciso",
            ClosedAtUtc: closedAt,
            TenantId: null,
            ModuleId: null);

        // Act
        var result = _sut.Apply(closed, readModel, _context);

        // Assert
        result.LastModifiedAtUtc.ShouldBe(closedAt);
        result.Version.ShouldBe(7);
    }

    #endregion

    #region Full Lifecycle

    [Fact]
    public void FullLifecycle_DetectThroughClose_ShouldTrackStateCorrectly()
    {
        // Arrange
        var breachId = Guid.NewGuid();
        var t0 = new DateTimeOffset(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);

        // Step 1: Breach detected
        var detected = CreateBreachDetectedEvent(
            breachId: breachId,
            nature: "data exfiltration",
            severity: BreachSeverity.High,
            detectedByRule: "MassDataExfiltrationRule",
            estimatedAffectedSubjects: 5_000,
            description: "Large data transfer to unknown external endpoint",
            detectedAtUtc: t0);

        var readModel = _sut.Create(detected, _context);
        readModel.Status.ShouldBe(BreachStatus.Detected);
        readModel.Version.ShouldBe(1);
        readModel.DeadlineUtc.ShouldBe(t0.AddHours(72));

        // Step 2: Breach assessed
        var t1 = t0.AddHours(4);
        var assessed = new BreachAssessed(
            BreachId: breachId,
            UpdatedSeverity: BreachSeverity.Critical,
            UpdatedAffectedSubjects: 15_000,
            AssessmentSummary: "Scope larger than initially detected",
            AssessedByUserId: "analyst-lead",
            AssessedAtUtc: t1,
            TenantId: "tenant-1",
            ModuleId: "module-1");

        readModel = _sut.Apply(assessed, readModel, _context);
        readModel.Status.ShouldBe(BreachStatus.Investigating);
        readModel.Severity.ShouldBe(BreachSeverity.Critical);
        readModel.Version.ShouldBe(2);

        // Step 3: Reported to DPA
        var t2 = t1.AddHours(8);
        var reported = new BreachReportedToDPA(
            BreachId: breachId,
            AuthorityName: "AEPD",
            AuthorityContactInfo: "breach@aepd.es",
            ReportSummary: "Art. 33 notification within 72h deadline",
            ReportedByUserId: "dpo",
            ReportedAtUtc: t2,
            TenantId: "tenant-1",
            ModuleId: "module-1");

        readModel = _sut.Apply(reported, readModel, _context);
        readModel.Status.ShouldBe(BreachStatus.AuthorityNotified);
        readModel.AuthorityName.ShouldBe("AEPD");
        readModel.Version.ShouldBe(3);

        // Step 4: Phased report added
        var t3 = t2.AddDays(1);
        var phasedReport = new BreachPhasedReportAdded(
            BreachId: breachId,
            PhaseNumber: 1,
            ReportContent: "Forensic analysis confirms SQL injection vector",
            SubmittedByUserId: "forensic-team",
            SubmittedAtUtc: t3,
            TenantId: "tenant-1",
            ModuleId: "module-1");

        readModel = _sut.Apply(phasedReport, readModel, _context);
        readModel.PhasedReports.Count.ShouldBe(1);
        readModel.Version.ShouldBe(4);

        // Step 5: Subjects notified
        var t4 = t3.AddDays(1);
        var notified = new BreachNotifiedToSubjects(
            BreachId: breachId,
            SubjectCount: 15_000,
            CommunicationMethod: "email",
            Exemption: SubjectNotificationExemption.None,
            NotifiedByUserId: "comms-lead",
            NotifiedAtUtc: t4,
            TenantId: "tenant-1",
            ModuleId: "module-1");

        readModel = _sut.Apply(notified, readModel, _context);
        readModel.Status.ShouldBe(BreachStatus.SubjectsNotified);
        readModel.SubjectCount.ShouldBe(15_000);
        readModel.Version.ShouldBe(5);

        // Step 6: Breach contained
        var t5 = t4.AddDays(2);
        var contained = new BreachContained(
            BreachId: breachId,
            ContainmentMeasures: "Patched SQL injection, rotated credentials, deployed WAF",
            ContainedByUserId: "incident-lead",
            ContainedAtUtc: t5,
            TenantId: "tenant-1",
            ModuleId: "module-1");

        readModel = _sut.Apply(contained, readModel, _context);
        readModel.Status.ShouldBe(BreachStatus.Resolved);
        readModel.Version.ShouldBe(6);

        // Step 7: Breach closed
        var t6 = t5.AddDays(5);
        var closed = new BreachClosed(
            BreachId: breachId,
            ResolutionSummary: "Root cause remediated. Post-incident review completed. Security policies updated.",
            ClosedByUserId: "ciso",
            ClosedAtUtc: t6,
            TenantId: "tenant-1",
            ModuleId: "module-1");

        readModel = _sut.Apply(closed, readModel, _context);
        readModel.Status.ShouldBe(BreachStatus.Closed);
        readModel.ClosedAtUtc.ShouldBe(t6);
        readModel.LastModifiedAtUtc.ShouldBe(t6);
        readModel.Version.ShouldBe(7);
    }

    #endregion
}
