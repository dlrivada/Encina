#pragma warning disable CA2012

using Encina.Compliance.CrossBorderTransfer.Events;
using Encina.Compliance.CrossBorderTransfer.Model;

namespace Encina.GuardTests.Compliance.CrossBorderTransfer;

/// <summary>
/// Guard tests for cross-border transfer event records verifying correct instantiation.
/// </summary>
public class CrossBorderTransferEventGuardTests
{
    private static readonly Guid TestId = Guid.NewGuid();

    #region TIA Events

    [Fact]
    public void TIACreated_CanBeInstantiated()
    {
        var evt = new TIACreated(TestId, "DE", "US", "personal-data", "user1", "tenant1", "module1");

        evt.TIAId.ShouldBe(TestId);
        evt.SourceCountryCode.ShouldBe("DE");
        evt.DestinationCountryCode.ShouldBe("US");
        evt.DataCategory.ShouldBe("personal-data");
        evt.CreatedBy.ShouldBe("user1");
        evt.TenantId.ShouldBe("tenant1");
        evt.ModuleId.ShouldBe("module1");
    }

    [Fact]
    public void TIACreated_NullableFieldsCanBeNull()
    {
        var evt = new TIACreated(TestId, "DE", "US", "personal-data", "user1", null, null);

        evt.TenantId.ShouldBeNull();
        evt.ModuleId.ShouldBeNull();
    }

    [Fact]
    public void TIARiskAssessed_CanBeInstantiated()
    {
        var evt = new TIARiskAssessed(TestId, 0.75, "High risk", "assessor1");

        evt.TIAId.ShouldBe(TestId);
        evt.RiskScore.ShouldBe(0.75);
        evt.Findings.ShouldBe("High risk");
        evt.AssessorId.ShouldBe("assessor1");
    }

    [Fact]
    public void TIARiskAssessed_NullFindings_Allowed()
    {
        var evt = new TIARiskAssessed(TestId, 0.5, null, "assessor1");

        evt.Findings.ShouldBeNull();
    }

    [Fact]
    public void TIASupplementaryMeasureRequired_CanBeInstantiated()
    {
        var measureId = Guid.NewGuid();
        var evt = new TIASupplementaryMeasureRequired(TestId, measureId, SupplementaryMeasureType.Technical, "Encryption");

        evt.TIAId.ShouldBe(TestId);
        evt.MeasureId.ShouldBe(measureId);
        evt.MeasureType.ShouldBe(SupplementaryMeasureType.Technical);
        evt.Description.ShouldBe("Encryption");
    }

    [Fact]
    public void TIASubmittedForDPOReview_CanBeInstantiated()
    {
        var evt = new TIASubmittedForDPOReview(TestId, "user1");

        evt.TIAId.ShouldBe(TestId);
        evt.SubmittedBy.ShouldBe("user1");
    }

    [Fact]
    public void TIADPOApproved_CanBeInstantiated()
    {
        var evt = new TIADPOApproved(TestId, "dpo1");

        evt.TIAId.ShouldBe(TestId);
        evt.ReviewedBy.ShouldBe("dpo1");
    }

    [Fact]
    public void TIADPORejected_CanBeInstantiated()
    {
        var evt = new TIADPORejected(TestId, "dpo1", "Insufficient analysis");

        evt.TIAId.ShouldBe(TestId);
        evt.ReviewedBy.ShouldBe("dpo1");
        evt.Reason.ShouldBe("Insufficient analysis");
    }

    [Fact]
    public void TIACompleted_CanBeInstantiated()
    {
        var evt = new TIACompleted(TestId);

        evt.TIAId.ShouldBe(TestId);
    }

    [Fact]
    public void TIAExpired_CanBeInstantiated()
    {
        var evt = new TIAExpired(TestId);

        evt.TIAId.ShouldBe(TestId);
    }

    #endregion

    #region SCC Events

    [Fact]
    public void SCCAgreementRegistered_CanBeInstantiated()
    {
        var now = DateTimeOffset.UtcNow;
        var expires = now.AddDays(365);
        var evt = new SCCAgreementRegistered(
            TestId, "proc-1", SCCModule.ControllerToProcessor, "2021/914", now, expires, "tenant1", "module1");

        evt.AgreementId.ShouldBe(TestId);
        evt.ProcessorId.ShouldBe("proc-1");
        evt.Module.ShouldBe(SCCModule.ControllerToProcessor);
        evt.Version.ShouldBe("2021/914");
        evt.ExecutedAtUtc.ShouldBe(now);
        evt.ExpiresAtUtc.ShouldBe(expires);
        evt.TenantId.ShouldBe("tenant1");
        evt.ModuleId.ShouldBe("module1");
    }

    [Fact]
    public void SCCAgreementRegistered_NullableFieldsCanBeNull()
    {
        var evt = new SCCAgreementRegistered(
            TestId, "proc-1", SCCModule.ControllerToProcessor, "2021/914", DateTimeOffset.UtcNow, null, null, null);

        evt.ExpiresAtUtc.ShouldBeNull();
        evt.TenantId.ShouldBeNull();
        evt.ModuleId.ShouldBeNull();
    }

    [Fact]
    public void SCCSupplementaryMeasureAdded_CanBeInstantiated()
    {
        var measureId = Guid.NewGuid();
        var evt = new SCCSupplementaryMeasureAdded(TestId, measureId, SupplementaryMeasureType.Contractual, "Audit clause");

        evt.AgreementId.ShouldBe(TestId);
        evt.MeasureId.ShouldBe(measureId);
        evt.MeasureType.ShouldBe(SupplementaryMeasureType.Contractual);
        evt.Description.ShouldBe("Audit clause");
    }

    [Fact]
    public void SCCAgreementRevoked_CanBeInstantiated()
    {
        var evt = new SCCAgreementRevoked(TestId, "Non-compliance", "admin1");

        evt.AgreementId.ShouldBe(TestId);
        evt.Reason.ShouldBe("Non-compliance");
        evt.RevokedBy.ShouldBe("admin1");
    }

    [Fact]
    public void SCCAgreementExpired_CanBeInstantiated()
    {
        var evt = new SCCAgreementExpired(TestId);

        evt.AgreementId.ShouldBe(TestId);
    }

    #endregion

    #region Approved Transfer Events

    [Fact]
    public void TransferApproved_CanBeInstantiated()
    {
        var sccId = Guid.NewGuid();
        var tiaId = Guid.NewGuid();
        var expires = DateTimeOffset.UtcNow.AddDays(365);

        var evt = new TransferApproved(
            TestId, "DE", "US", "personal-data", TransferBasis.SCCs,
            sccId, tiaId, "admin1", expires, "tenant1", "module1");

        evt.TransferId.ShouldBe(TestId);
        evt.SourceCountryCode.ShouldBe("DE");
        evt.DestinationCountryCode.ShouldBe("US");
        evt.DataCategory.ShouldBe("personal-data");
        evt.Basis.ShouldBe(TransferBasis.SCCs);
        evt.SCCAgreementId.ShouldBe(sccId);
        evt.TIAId.ShouldBe(tiaId);
        evt.ApprovedBy.ShouldBe("admin1");
        evt.ExpiresAtUtc.ShouldBe(expires);
        evt.TenantId.ShouldBe("tenant1");
        evt.ModuleId.ShouldBe("module1");
    }

    [Fact]
    public void TransferApproved_NullableFieldsCanBeNull()
    {
        var evt = new TransferApproved(
            TestId, "DE", "US", "personal-data", TransferBasis.AdequacyDecision,
            null, null, "admin1", null, null, null);

        evt.SCCAgreementId.ShouldBeNull();
        evt.TIAId.ShouldBeNull();
        evt.ExpiresAtUtc.ShouldBeNull();
        evt.TenantId.ShouldBeNull();
        evt.ModuleId.ShouldBeNull();
    }

    [Fact]
    public void TransferRevoked_CanBeInstantiated()
    {
        var evt = new TransferRevoked(TestId, "Legal change", "admin1");

        evt.TransferId.ShouldBe(TestId);
        evt.Reason.ShouldBe("Legal change");
        evt.RevokedBy.ShouldBe("admin1");
    }

    [Fact]
    public void TransferExpired_CanBeInstantiated()
    {
        var evt = new TransferExpired(TestId);

        evt.TransferId.ShouldBe(TestId);
    }

    [Fact]
    public void TransferRenewed_CanBeInstantiated()
    {
        var newExpiry = DateTimeOffset.UtcNow.AddDays(365);
        var evt = new TransferRenewed(TestId, newExpiry, "admin1");

        evt.TransferId.ShouldBe(TestId);
        evt.NewExpiresAtUtc.ShouldBe(newExpiry);
        evt.RenewedBy.ShouldBe("admin1");
    }

    #endregion
}
