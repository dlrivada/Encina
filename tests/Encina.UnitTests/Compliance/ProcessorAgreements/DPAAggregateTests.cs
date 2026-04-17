#pragma warning disable CA2012

using Encina.Compliance.ProcessorAgreements.Aggregates;
using Encina.Compliance.ProcessorAgreements.Model;
using Shouldly;

namespace Encina.UnitTests.Compliance.ProcessorAgreements;

/// <summary>
/// Unit tests for <see cref="DPAAggregate"/>.
/// </summary>
public class DPAAggregateTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    #region Execute Tests

    [Fact]
    public void Execute_ValidParameters_CreatesActiveAgreement()
    {
        // Arrange
        var id = Guid.NewGuid();
        var processorId = Guid.NewGuid();
        var terms = CreateFullyCompliantTerms();
        var purposes = new List<string> { "Payment processing", "Fraud detection" };
        var signedAt = FixedNow.AddDays(-1);

        // Act
        var aggregate = DPAAggregate.Execute(
            id, processorId, terms, hasSCCs: true, purposes,
            signedAt, expiresAtUtc: FixedNow.AddYears(1), FixedNow);

        // Assert
        aggregate.Id.ShouldBe(id);
        aggregate.ProcessorId.ShouldBe(processorId);
        aggregate.Status.ShouldBe(DPAStatus.Active);
        aggregate.MandatoryTerms.ShouldBe(terms);
        aggregate.HasSCCs.ShouldBeTrue();
        aggregate.ProcessingPurposes.ShouldBe(purposes);
        aggregate.SignedAtUtc.ShouldBe(signedAt);
        aggregate.CreatedAtUtc.ShouldBe(FixedNow);
        aggregate.LastUpdatedAtUtc.ShouldBe(FixedNow);
    }

    [Fact]
    public void Execute_WithExpiration_SetsExpiresAtUtc()
    {
        // Arrange
        var expiresAt = FixedNow.AddYears(2);

        // Act
        var aggregate = CreateDefaultDPA(expiresAtUtc: expiresAt);

        // Assert
        aggregate.ExpiresAtUtc.ShouldBe(expiresAt);
    }

    [Fact]
    public void Execute_IndefiniteAgreement_HasNullExpiresAtUtc()
    {
        // Act
        var aggregate = DPAAggregate.Execute(
            Guid.NewGuid(), Guid.NewGuid(), CreateFullyCompliantTerms(),
            hasSCCs: true, new List<string> { "Data processing" },
            signedAtUtc: FixedNow.AddDays(-1),
            expiresAtUtc: null,
            occurredAtUtc: FixedNow);

        // Assert
        aggregate.ExpiresAtUtc.ShouldBeNull();
    }

    [Fact]
    public void Execute_NullMandatoryTerms_ThrowsArgumentNullException()
    {
        // Act
        var act = () => DPAAggregate.Execute(
            Guid.NewGuid(), Guid.NewGuid(), null!, hasSCCs: false,
            new List<string> { "Processing" }, FixedNow, null, FixedNow);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("mandatoryTerms");
    }

    [Fact]
    public void Execute_NullProcessingPurposes_ThrowsArgumentNullException()
    {
        // Act
        var act = () => DPAAggregate.Execute(
            Guid.NewGuid(), Guid.NewGuid(), CreateFullyCompliantTerms(),
            hasSCCs: false, null!, FixedNow, null, FixedNow);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("processingPurposes");
    }

    [Fact]
    public void Execute_EmptyProcessingPurposes_ThrowsArgumentException()
    {
        // Act
        var act = () => DPAAggregate.Execute(
            Guid.NewGuid(), Guid.NewGuid(), CreateFullyCompliantTerms(),
            hasSCCs: false, new List<string>(), FixedNow, null, FixedNow);

        // Assert
        Should.Throw<ArgumentException>(act)
            .ParamName.ShouldBe("processingPurposes");
    }

    #endregion

    #region Amend Tests

    [Fact]
    public void Amend_ActiveAgreement_UpdatesTerms()
    {
        // Arrange
        var aggregate = CreateDefaultDPA();
        var updatedTerms = CreateFullyCompliantTerms();
        var newPurposes = new List<string> { "Updated purpose" };
        var amendedAt = FixedNow.AddDays(10);

        // Act
        aggregate.Amend(updatedTerms, hasSCCs: false, newPurposes,
            "Regulatory requirement change", amendedAt);

        // Assert
        aggregate.MandatoryTerms.ShouldBe(updatedTerms);
        aggregate.HasSCCs.ShouldBeFalse();
        aggregate.ProcessingPurposes.ShouldBe(newPurposes);
        aggregate.LastUpdatedAtUtc.ShouldBe(amendedAt);
    }

    [Fact]
    public void Amend_PendingRenewalAgreement_Succeeds()
    {
        // Arrange
        var aggregate = CreateDefaultDPA();
        aggregate.MarkPendingRenewal(FixedNow.AddDays(5));

        // Act
        var act = () => aggregate.Amend(
            CreateFullyCompliantTerms(), hasSCCs: true,
            new List<string> { "Purpose" }, "Update before renewal",
            FixedNow.AddDays(6));

        // Assert
        Should.NotThrow(act);
    }

    [Fact]
    public void Amend_ExpiredAgreement_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = CreateDefaultDPA();
        aggregate.MarkExpired(FixedNow.AddDays(5));

        // Act
        var act = () => aggregate.Amend(
            CreateFullyCompliantTerms(), hasSCCs: true,
            new List<string> { "Purpose" }, "Attempt amendment",
            FixedNow.AddDays(6));

        // Assert
        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void Amend_TerminatedAgreement_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = CreateDefaultDPA();
        aggregate.Terminate("Contract breach", FixedNow.AddDays(5));

        // Act
        var act = () => aggregate.Amend(
            CreateFullyCompliantTerms(), hasSCCs: true,
            new List<string> { "Purpose" }, "Attempt amendment",
            FixedNow.AddDays(6));

        // Assert
        Should.Throw<InvalidOperationException>(act);
    }

    #endregion

    #region Audit Tests

    [Fact]
    public void Audit_ActiveAgreement_UpdatesTimestamp()
    {
        // Arrange
        var aggregate = CreateDefaultDPA();
        var auditedAt = FixedNow.AddDays(15);

        // Act
        aggregate.Audit("auditor-001", "No issues found", auditedAt);

        // Assert
        aggregate.LastUpdatedAtUtc.ShouldBe(auditedAt);
    }

    [Fact]
    public void Audit_TerminatedAgreement_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = CreateDefaultDPA();
        aggregate.Terminate("Breach of terms", FixedNow.AddDays(5));

        // Act
        var act = () => aggregate.Audit("auditor-001", "Post-termination audit",
            FixedNow.AddDays(6));

        // Assert
        Should.Throw<InvalidOperationException>(act);
    }

    #endregion

    #region Renew Tests

    [Fact]
    public void Renew_ActiveAgreement_UpdatesExpiration()
    {
        // Arrange
        var aggregate = CreateDefaultDPA();
        var newExpiry = FixedNow.AddYears(2);
        var renewedAt = FixedNow.AddDays(20);

        // Act
        aggregate.Renew(newExpiry, renewedAt);

        // Assert
        aggregate.ExpiresAtUtc.ShouldBe(newExpiry);
        aggregate.LastUpdatedAtUtc.ShouldBe(renewedAt);
    }

    [Fact]
    public void Renew_PendingRenewalAgreement_TransitionsToActive()
    {
        // Arrange
        var aggregate = CreateDefaultDPA();
        aggregate.MarkPendingRenewal(FixedNow.AddDays(5));
        aggregate.Status.ShouldBe(DPAStatus.PendingRenewal);

        // Act
        aggregate.Renew(FixedNow.AddYears(1), FixedNow.AddDays(6));

        // Assert
        aggregate.Status.ShouldBe(DPAStatus.Active);
    }

    #endregion

    #region Terminate Tests

    [Fact]
    public void Terminate_ActiveAgreement_SetsTerminatedStatus()
    {
        // Arrange
        var aggregate = CreateDefaultDPA();
        var terminatedAt = FixedNow.AddDays(30);

        // Act
        aggregate.Terminate("Contract breach", terminatedAt);

        // Assert
        aggregate.Status.ShouldBe(DPAStatus.Terminated);
        aggregate.LastUpdatedAtUtc.ShouldBe(terminatedAt);
    }

    [Fact]
    public void Terminate_AlreadyTerminated_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = CreateDefaultDPA();
        aggregate.Terminate("First termination", FixedNow.AddDays(5));

        // Act
        var act = () => aggregate.Terminate("Second termination", FixedNow.AddDays(6));

        // Assert
        Should.Throw<InvalidOperationException>(act);
    }

    #endregion

    #region MarkExpired Tests

    [Fact]
    public void MarkExpired_ActiveAgreement_SetsExpiredStatus()
    {
        // Arrange
        var aggregate = CreateDefaultDPA();
        var expiredAt = FixedNow.AddYears(1).AddDays(1);

        // Act
        aggregate.MarkExpired(expiredAt);

        // Assert
        aggregate.Status.ShouldBe(DPAStatus.Expired);
        aggregate.LastUpdatedAtUtc.ShouldBe(expiredAt);
    }

    #endregion

    #region MarkPendingRenewal Tests

    [Fact]
    public void MarkPendingRenewal_ActiveAgreement_SetsPendingRenewalStatus()
    {
        // Arrange
        var aggregate = CreateDefaultDPA();

        // Act
        aggregate.MarkPendingRenewal(FixedNow.AddMonths(11));

        // Assert
        aggregate.Status.ShouldBe(DPAStatus.PendingRenewal);
    }

    [Fact]
    public void MarkPendingRenewal_ExpiredAgreement_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = CreateDefaultDPA();
        aggregate.MarkExpired(FixedNow.AddDays(5));

        // Act
        var act = () => aggregate.MarkPendingRenewal(FixedNow.AddDays(6));

        // Assert
        Should.Throw<InvalidOperationException>(act);
    }

    #endregion

    #region IsActive Tests

    [Fact]
    public void IsActive_ActiveNotExpired_ReturnsTrue()
    {
        // Arrange
        var aggregate = CreateDefaultDPA(expiresAtUtc: FixedNow.AddYears(1));

        // Act
        var result = aggregate.IsActive(FixedNow.AddMonths(6));

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsActive_ActiveButExpired_ReturnsFalse()
    {
        // Arrange
        var aggregate = CreateDefaultDPA(expiresAtUtc: FixedNow.AddYears(1));

        // Act
        var result = aggregate.IsActive(FixedNow.AddYears(2));

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsActive_TerminatedStatus_ReturnsFalse()
    {
        // Arrange
        var aggregate = CreateDefaultDPA();
        aggregate.Terminate("No longer needed", FixedNow.AddDays(5));

        // Act
        var result = aggregate.IsActive(FixedNow.AddDays(6));

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region Full Lifecycle Tests

    [Fact]
    public void FullLifecycle_ActiveToRenewedToTerminated_AllStatesCorrect()
    {
        // Arrange - Create active agreement
        var aggregate = CreateDefaultDPA(expiresAtUtc: FixedNow.AddYears(1));
        aggregate.Status.ShouldBe(DPAStatus.Active);

        // Act/Assert - Mark pending renewal
        aggregate.MarkPendingRenewal(FixedNow.AddMonths(11));
        aggregate.Status.ShouldBe(DPAStatus.PendingRenewal);

        // Act/Assert - Renew (transitions back to Active)
        aggregate.Renew(FixedNow.AddYears(2), FixedNow.AddMonths(11).AddDays(5));
        aggregate.Status.ShouldBe(DPAStatus.Active);
        aggregate.ExpiresAtUtc.ShouldBe(FixedNow.AddYears(2));

        // Act/Assert - Terminate
        aggregate.Terminate("Business relationship ended", FixedNow.AddYears(1).AddMonths(6));
        aggregate.Status.ShouldBe(DPAStatus.Terminated);

        // Act/Assert - Cannot perform further operations
        var amendAct = () => aggregate.Amend(
            CreateFullyCompliantTerms(), hasSCCs: true,
            new List<string> { "Purpose" }, "Post-termination",
            FixedNow.AddYears(1).AddMonths(7));
        Should.Throw<InvalidOperationException>(amendAct);
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

    private static DPAAggregate CreateDefaultDPA(DateTimeOffset? expiresAtUtc = null)
    {
        return DPAAggregate.Execute(
            Guid.NewGuid(), Guid.NewGuid(), CreateFullyCompliantTerms(),
            hasSCCs: true, new List<string> { "Data processing", "Analytics" },
            signedAtUtc: FixedNow.AddDays(-1),
            expiresAtUtc: expiresAtUtc ?? FixedNow.AddYears(1),
            occurredAtUtc: FixedNow);
    }

    #endregion
}
