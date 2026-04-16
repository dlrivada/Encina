using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Model;
using Encina.Compliance.Retention.ReadModels;

using Shouldly;

namespace Encina.UnitTests.Compliance.Retention;

/// <summary>
/// Unit tests for retention model types: DeletionDetail, DeletionOutcome, ExpiringData,
/// RetentionPolicyType, RetentionStatus, RetentionRecordReadModel.
/// </summary>
public sealed class ModelTests
{
    #region DeletionDetail

    [Fact]
    public void DeletionDetail_SetsAllRequiredProperties()
    {
        var detail = new DeletionDetail
        {
            EntityId = "entity-1",
            DataCategory = "user-data",
            Outcome = DeletionOutcome.Deleted,
            Reason = null
        };

        detail.EntityId.ShouldBe("entity-1");
        detail.DataCategory.ShouldBe("user-data");
        detail.Outcome.ShouldBe(DeletionOutcome.Deleted);
        detail.Reason.ShouldBeNull();
    }

    [Fact]
    public void DeletionDetail_WithReason_SetsReason()
    {
        var detail = new DeletionDetail
        {
            EntityId = "entity-2",
            DataCategory = "logs",
            Outcome = DeletionOutcome.HeldByLegalHold,
            Reason = "Active hold: Case #12345"
        };

        detail.Reason.ShouldBe("Active hold: Case #12345");
    }

    #endregion

    #region DeletionOutcome

    [Theory]
    [InlineData(DeletionOutcome.Deleted, 0)]
    [InlineData(DeletionOutcome.Retained, 1)]
    [InlineData(DeletionOutcome.Failed, 2)]
    [InlineData(DeletionOutcome.HeldByLegalHold, 3)]
    [InlineData(DeletionOutcome.Skipped, 4)]
    public void DeletionOutcome_HasExpectedValues(DeletionOutcome outcome, int expected)
    {
        ((int)outcome).ShouldBe(expected);
    }

    [Fact]
    public void DeletionOutcome_AllValuesDefined()
    {
        Enum.GetValues<DeletionOutcome>().Count.ShouldBe(5);
    }

    #endregion

    #region ExpiringData

    [Fact]
    public void ExpiringData_SetsAllRequiredProperties()
    {
        var expiresAt = DateTimeOffset.UtcNow.AddDays(5);
        var data = new ExpiringData
        {
            EntityId = "entity-3",
            DataCategory = "marketing",
            ExpiresAtUtc = expiresAt,
            DaysUntilExpiration = 5
        };

        data.EntityId.ShouldBe("entity-3");
        data.DataCategory.ShouldBe("marketing");
        data.ExpiresAtUtc.ShouldBe(expiresAt);
        data.DaysUntilExpiration.ShouldBe(5);
        data.PolicyId.ShouldBeNull();
    }

    [Fact]
    public void ExpiringData_WithPolicyId_SetsPolicyId()
    {
        var data = new ExpiringData
        {
            EntityId = "e",
            DataCategory = "c",
            ExpiresAtUtc = DateTimeOffset.UtcNow,
            DaysUntilExpiration = 0,
            PolicyId = "policy-123"
        };

        data.PolicyId.ShouldBe("policy-123");
    }

    [Fact]
    public void ExpiringData_NegativeDaysUntilExpiration_AlreadyExpired()
    {
        var data = new ExpiringData
        {
            EntityId = "e",
            DataCategory = "c",
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(-2),
            DaysUntilExpiration = -2
        };

        data.DaysUntilExpiration.ShouldBeLessThan(0);
    }

    #endregion

    #region RetentionPolicyType

    [Theory]
    [InlineData(RetentionPolicyType.TimeBased, 0)]
    [InlineData(RetentionPolicyType.EventBased, 1)]
    [InlineData(RetentionPolicyType.ConsentBased, 2)]
    public void RetentionPolicyType_HasExpectedValues(RetentionPolicyType type, int expected)
    {
        ((int)type).ShouldBe(expected);
    }

    [Fact]
    public void RetentionPolicyType_AllValuesDefined()
    {
        Enum.GetValues<RetentionPolicyType>().Count.ShouldBe(3);
    }

    #endregion

    #region RetentionStatus

    [Theory]
    [InlineData(RetentionStatus.Active, 0)]
    [InlineData(RetentionStatus.Expired, 1)]
    [InlineData(RetentionStatus.Deleted, 2)]
    [InlineData(RetentionStatus.UnderLegalHold, 3)]
    public void RetentionStatus_HasExpectedValues(RetentionStatus status, int expected)
    {
        ((int)status).ShouldBe(expected);
    }

    [Fact]
    public void RetentionStatus_AllValuesDefined()
    {
        Enum.GetValues<RetentionStatus>().Count.ShouldBe(4);
    }

    #endregion

    #region RetentionEnforcementMode

    [Theory]
    [InlineData(RetentionEnforcementMode.Block, 0)]
    [InlineData(RetentionEnforcementMode.Warn, 1)]
    [InlineData(RetentionEnforcementMode.Disabled, 2)]
    public void RetentionEnforcementMode_HasExpectedValues(RetentionEnforcementMode mode, int expected)
    {
        ((int)mode).ShouldBe(expected);
    }

    [Fact]
    public void RetentionEnforcementMode_AllValuesDefined()
    {
        Enum.GetValues<RetentionEnforcementMode>().Count.ShouldBe(3);
    }

    #endregion

    #region RetentionRecordReadModel

    [Fact]
    public void RetentionRecordReadModel_IsExpired_WhenPastExpiresAtUtc_ReturnsTrue()
    {
        var model = new RetentionRecordReadModel
        {
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
            Status = RetentionStatus.Active
        };

        model.IsExpired(DateTimeOffset.UtcNow).ShouldBeTrue();
    }

    [Fact]
    public void RetentionRecordReadModel_IsExpired_WhenBeforeExpiresAtUtc_ReturnsFalse()
    {
        var model = new RetentionRecordReadModel
        {
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(30),
            Status = RetentionStatus.Active
        };

        model.IsExpired(DateTimeOffset.UtcNow).ShouldBeFalse();
    }

    [Fact]
    public void RetentionRecordReadModel_IsExpired_WhenDeleted_ReturnsFalse()
    {
        var model = new RetentionRecordReadModel
        {
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
            Status = RetentionStatus.Deleted
        };

        model.IsExpired(DateTimeOffset.UtcNow).ShouldBeFalse();
    }

    [Fact]
    public void RetentionRecordReadModel_IsExpired_ExactExpiration_ReturnsTrue()
    {
        var now = DateTimeOffset.UtcNow;
        var model = new RetentionRecordReadModel
        {
            ExpiresAtUtc = now,
            Status = RetentionStatus.Active
        };

        model.IsExpired(now).ShouldBeTrue();
    }

    [Fact]
    public void RetentionRecordReadModel_DefaultValues()
    {
        var model = new RetentionRecordReadModel();

        model.EntityId.ShouldBe(string.Empty);
        model.DataCategory.ShouldBe(string.Empty);
        model.LegalHoldId.ShouldBeNull();
        model.DeletedAtUtc.ShouldBeNull();
        model.AnonymizedAtUtc.ShouldBeNull();
        model.TenantId.ShouldBeNull();
        model.ModuleId.ShouldBeNull();
    }

    #endregion

    #region DeletionResult

    [Fact]
    public void DeletionResult_SetsAllProperties()
    {
        var details = new List<DeletionDetail>
        {
            new() { EntityId = "e1", DataCategory = "c", Outcome = DeletionOutcome.Deleted },
            new() { EntityId = "e2", DataCategory = "c", Outcome = DeletionOutcome.Failed, Reason = "Error" }
        };

        var executedAt = DateTimeOffset.UtcNow;
        var result = new DeletionResult
        {
            TotalRecordsEvaluated = 5,
            RecordsDeleted = 2,
            RecordsRetained = 1,
            RecordsFailed = 1,
            RecordsUnderHold = 1,
            Details = details,
            ExecutedAtUtc = executedAt
        };

        result.TotalRecordsEvaluated.ShouldBe(5);
        result.RecordsDeleted.ShouldBe(2);
        result.RecordsRetained.ShouldBe(1);
        result.RecordsFailed.ShouldBe(1);
        result.RecordsUnderHold.ShouldBe(1);
        result.Details.Count.ShouldBe(2);
        result.ExecutedAtUtc.ShouldBe(executedAt);
    }

    #endregion
}
