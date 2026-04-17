#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.ProcessorAgreements.Events;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Compliance.ProcessorAgreements.ReadModels;
using Encina.Marten.Projections;

using Shouldly;

namespace Encina.UnitTests.Compliance.ProcessorAgreements;

/// <summary>
/// Unit tests for <see cref="DPAProjection"/>.
/// </summary>
public class DPAProjectionTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private readonly DPAProjection _sut = new();
    private readonly ProjectionContext _context = new();

    #region ProjectionName

    [Fact]
    public void ProjectionName_ReturnsExpectedValue()
    {
        // Arrange & Act
        var name = _sut.ProjectionName;

        // Assert
        name.ShouldBe("DPAProjection");
    }

    #endregion

    #region Create (DPAExecuted)

    [Fact]
    public void Create_FromDPAExecuted_SetsAllProperties()
    {
        // Arrange
        var dpaId = Guid.NewGuid();
        var processorId = Guid.NewGuid();
        var terms = CreateFullyCompliantTerms();
        var purposes = new List<string> { "Analytics", "Billing" };
        var signedAt = Now.AddDays(-1);
        var expiresAt = Now.AddYears(1);

        var executed = new DPAExecuted(
            DPAId: dpaId,
            ProcessorId: processorId,
            MandatoryTerms: terms,
            HasSCCs: true,
            ProcessingPurposes: purposes,
            SignedAtUtc: signedAt,
            ExpiresAtUtc: expiresAt,
            OccurredAtUtc: Now,
            TenantId: "tenant-1",
            ModuleId: "module-compliance");

        // Act
        var result = _sut.Create(executed, _context);

        // Assert
        result.Id.ShouldBe(dpaId);
        result.ProcessorId.ShouldBe(processorId);
        result.MandatoryTerms.ShouldBe(terms);
        result.HasSCCs.ShouldBeTrue();
        result.ProcessingPurposes.ShouldBe(purposes);
        result.SignedAtUtc.ShouldBe(signedAt);
        result.ExpiresAtUtc.ShouldBe(expiresAt);
        result.TenantId.ShouldBe("tenant-1");
        result.ModuleId.ShouldBe("module-compliance");
        result.CreatedAtUtc.ShouldBe(Now);
        result.LastModifiedAtUtc.ShouldBe(Now);
    }

    [Fact]
    public void Create_SetsActiveStatus()
    {
        // Arrange
        var executed = CreateDPAExecuted();

        // Act
        var result = _sut.Create(executed, _context);

        // Assert
        result.Status.ShouldBe(DPAStatus.Active);
    }

    [Fact]
    public void Create_SetsVersionToOne()
    {
        // Arrange
        var executed = CreateDPAExecuted();

        // Act
        var result = _sut.Create(executed, _context);

        // Assert
        result.Version.ShouldBe(1);
    }

    #endregion

    #region Apply (DPAAmended)

    [Fact]
    public void Apply_DPAAmended_UpdatesTermsAndPurposes()
    {
        // Arrange
        var current = CreateDPAReadModel();
        var newTerms = CreateFullyCompliantTerms() with { AuditRights = false };
        var newPurposes = new List<string> { "Marketing", "Research" };

        var amended = new DPAAmended(
            DPAId: current.Id,
            UpdatedTerms: newTerms,
            HasSCCs: false,
            ProcessingPurposes: newPurposes,
            AmendmentReason: "Updated scope",
            OccurredAtUtc: Now.AddHours(1));

        // Act
        var result = _sut.Apply(amended, current, _context);

        // Assert
        result.MandatoryTerms.ShouldBe(newTerms);
        result.HasSCCs.ShouldBeFalse();
        result.ProcessingPurposes.ShouldBe(newPurposes);
        result.LastModifiedAtUtc.ShouldBe(Now.AddHours(1));
    }

    [Fact]
    public void Apply_DPAAmended_IncrementsVersion()
    {
        // Arrange
        var current = CreateDPAReadModel();
        var initialVersion = current.Version;

        var amended = new DPAAmended(
            DPAId: current.Id,
            UpdatedTerms: CreateFullyCompliantTerms(),
            HasSCCs: true,
            ProcessingPurposes: ["Analytics"],
            AmendmentReason: "Minor update",
            OccurredAtUtc: Now.AddHours(1));

        // Act
        _sut.Apply(amended, current, _context);

        // Assert
        current.Version.ShouldBe(initialVersion + 1);
    }

    #endregion

    #region Apply (DPAAudited)

    [Fact]
    public void Apply_DPAAudited_AppendsToAuditHistory()
    {
        // Arrange
        var current = CreateDPAReadModel();
        var auditTime = Now.AddDays(30);

        var audited = new DPAAudited(
            DPAId: current.Id,
            AuditorId: "auditor-42",
            AuditFindings: "All terms compliant",
            OccurredAtUtc: auditTime);

        // Act
        _sut.Apply(audited, current, _context);

        // Assert
        current.AuditHistory.Count.ShouldBe(1);
        current.AuditHistory[0].AuditorId.ShouldBe("auditor-42");
        current.AuditHistory[0].AuditFindings.ShouldBe("All terms compliant");
        current.AuditHistory[0].AuditedAtUtc.ShouldBe(auditTime);
        current.LastModifiedAtUtc.ShouldBe(auditTime);
    }

    #endregion

    #region Apply (DPARenewed)

    [Fact]
    public void Apply_DPARenewed_UpdatesExpirationAndStatus()
    {
        // Arrange
        var current = CreateDPAReadModel();
        current.Status = DPAStatus.PendingRenewal;
        var newExpiry = Now.AddYears(2);

        var renewed = new DPARenewed(
            DPAId: current.Id,
            NewExpiresAtUtc: newExpiry,
            OccurredAtUtc: Now.AddHours(1));

        // Act
        var result = _sut.Apply(renewed, current, _context);

        // Assert
        result.Status.ShouldBe(DPAStatus.Active);
        result.ExpiresAtUtc.ShouldBe(newExpiry);
        result.LastModifiedAtUtc.ShouldBe(Now.AddHours(1));
    }

    #endregion

    #region Apply (DPATerminated)

    [Fact]
    public void Apply_DPATerminated_SetsTerminatedStatusAndReason()
    {
        // Arrange
        var current = CreateDPAReadModel();
        var terminationTime = Now.AddDays(60);

        var terminated = new DPATerminated(
            DPAId: current.Id,
            Reason: "Breach of contract",
            OccurredAtUtc: terminationTime);

        // Act
        var result = _sut.Apply(terminated, current, _context);

        // Assert
        result.Status.ShouldBe(DPAStatus.Terminated);
        result.TerminationReason.ShouldBe("Breach of contract");
        result.TerminatedAtUtc.ShouldBe(terminationTime);
        result.LastModifiedAtUtc.ShouldBe(terminationTime);
    }

    #endregion

    #region Apply (DPAExpired)

    [Fact]
    public void Apply_DPAExpired_SetsExpiredStatus()
    {
        // Arrange
        var current = CreateDPAReadModel();
        var expiredTime = Now.AddYears(1);

        var expired = new DPAExpired(
            DPAId: current.Id,
            OccurredAtUtc: expiredTime);

        // Act
        var result = _sut.Apply(expired, current, _context);

        // Assert
        result.Status.ShouldBe(DPAStatus.Expired);
        result.LastModifiedAtUtc.ShouldBe(expiredTime);
    }

    #endregion

    #region Apply (DPAMarkedPendingRenewal)

    [Fact]
    public void Apply_DPAMarkedPendingRenewal_SetsPendingRenewalStatus()
    {
        // Arrange
        var current = CreateDPAReadModel();
        var pendingTime = Now.AddMonths(11);

        var pending = new DPAMarkedPendingRenewal(
            DPAId: current.Id,
            OccurredAtUtc: pendingTime);

        // Act
        var result = _sut.Apply(pending, current, _context);

        // Assert
        result.Status.ShouldBe(DPAStatus.PendingRenewal);
        result.LastModifiedAtUtc.ShouldBe(pendingTime);
    }

    #endregion

    #region Full Lifecycle

    [Fact]
    public void FullLifecycle_AllEventsApplied_FinalStateCorrect()
    {
        // Arrange — Create
        var dpaId = Guid.NewGuid();
        var processorId = Guid.NewGuid();
        var terms = CreateFullyCompliantTerms();
        var executed = new DPAExecuted(
            DPAId: dpaId,
            ProcessorId: processorId,
            MandatoryTerms: terms,
            HasSCCs: true,
            ProcessingPurposes: ["Analytics"],
            SignedAtUtc: Now,
            ExpiresAtUtc: Now.AddYears(1),
            OccurredAtUtc: Now,
            TenantId: "tenant-1",
            ModuleId: null);

        var model = _sut.Create(executed, _context);

        // Act — Amend
        var amendedTerms = terms with { AuditRights = false };
        _sut.Apply(new DPAAmended(dpaId, amendedTerms, true, ["Analytics", "Billing"], "Added billing", Now.AddDays(10)), model, _context);

        // Act — Audit
        _sut.Apply(new DPAAudited(dpaId, "auditor-1", "Minor findings", Now.AddDays(30)), model, _context);
        _sut.Apply(new DPAAudited(dpaId, "auditor-2", "All clear", Now.AddDays(60)), model, _context);

        // Act — Mark pending renewal
        _sut.Apply(new DPAMarkedPendingRenewal(dpaId, Now.AddMonths(11)), model, _context);

        // Act — Renew
        var newExpiry = Now.AddYears(2);
        _sut.Apply(new DPARenewed(dpaId, newExpiry, Now.AddMonths(12)), model, _context);

        // Act — Terminate
        _sut.Apply(new DPATerminated(dpaId, "Service discontinued", Now.AddMonths(18)), model, _context);

        // Assert — Final state
        model.Id.ShouldBe(dpaId);
        model.ProcessorId.ShouldBe(processorId);
        model.Status.ShouldBe(DPAStatus.Terminated);
        model.MandatoryTerms.ShouldBe(amendedTerms);
        model.HasSCCs.ShouldBeTrue();
        model.ProcessingPurposes.ShouldBe(["Analytics", "Billing"]);
        model.ExpiresAtUtc.ShouldBe(newExpiry);
        model.TerminationReason.ShouldBe("Service discontinued");
        model.TerminatedAtUtc.ShouldBe(Now.AddMonths(18));
        model.AuditHistory.Count.ShouldBe(2);
        model.TenantId.ShouldBe("tenant-1");
        model.Version.ShouldBe(7); // 1 (create) + 6 (apply events)
        model.LastModifiedAtUtc.ShouldBe(Now.AddMonths(18));
    }

    #endregion

    #region Helpers

    private static DPAMandatoryTerms CreateFullyCompliantTerms() => new()
    {
        ProcessOnDocumentedInstructions = true,
        ConfidentialityObligations = true,
        SecurityMeasures = true,
        SubProcessorRequirements = true,
        DataSubjectRightsAssistance = true,
        ComplianceAssistance = true,
        DataDeletionOrReturn = true,
        AuditRights = true
    };

    private static DPAExecuted CreateDPAExecuted() => new(
        DPAId: Guid.NewGuid(),
        ProcessorId: Guid.NewGuid(),
        MandatoryTerms: CreateFullyCompliantTerms(),
        HasSCCs: true,
        ProcessingPurposes: ["Analytics"],
        SignedAtUtc: Now,
        ExpiresAtUtc: Now.AddYears(1),
        OccurredAtUtc: Now,
        TenantId: null,
        ModuleId: null);

    private static DPAReadModel CreateDPAReadModel() => new()
    {
        Id = Guid.NewGuid(),
        ProcessorId = Guid.NewGuid(),
        Status = DPAStatus.Active,
        MandatoryTerms = CreateFullyCompliantTerms(),
        HasSCCs = true,
        ProcessingPurposes = ["Analytics"],
        SignedAtUtc = Now,
        ExpiresAtUtc = Now.AddYears(1),
        TenantId = null,
        ModuleId = null,
        CreatedAtUtc = Now,
        LastModifiedAtUtc = Now,
        Version = 1
    };

    #endregion
}
