using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Model;
using Encina.Compliance.Retention.ReadModels;

using FluentAssertions;

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

        detail.EntityId.Should().Be("entity-1");
        detail.DataCategory.Should().Be("user-data");
        detail.Outcome.Should().Be(DeletionOutcome.Deleted);
        detail.Reason.Should().BeNull();
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

        detail.Reason.Should().Be("Active hold: Case #12345");
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
        ((int)outcome).Should().Be(expected);
    }

    [Fact]
    public void DeletionOutcome_AllValuesDefined()
    {
        Enum.GetValues<DeletionOutcome>().Should().HaveCount(5);
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

        data.EntityId.Should().Be("entity-3");
        data.DataCategory.Should().Be("marketing");
        data.ExpiresAtUtc.Should().Be(expiresAt);
        data.DaysUntilExpiration.Should().Be(5);
        data.PolicyId.Should().BeNull();
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

        data.PolicyId.Should().Be("policy-123");
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

        data.DaysUntilExpiration.Should().BeNegative();
    }

    #endregion

    #region RetentionPolicyType

    [Theory]
    [InlineData(RetentionPolicyType.TimeBased, 0)]
    [InlineData(RetentionPolicyType.EventBased, 1)]
    [InlineData(RetentionPolicyType.ConsentBased, 2)]
    public void RetentionPolicyType_HasExpectedValues(RetentionPolicyType type, int expected)
    {
        ((int)type).Should().Be(expected);
    }

    [Fact]
    public void RetentionPolicyType_AllValuesDefined()
    {
        Enum.GetValues<RetentionPolicyType>().Should().HaveCount(3);
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
        ((int)status).Should().Be(expected);
    }

    [Fact]
    public void RetentionStatus_AllValuesDefined()
    {
        Enum.GetValues<RetentionStatus>().Should().HaveCount(4);
    }

    #endregion

    #region RetentionEnforcementMode

    [Theory]
    [InlineData(RetentionEnforcementMode.Block, 0)]
    [InlineData(RetentionEnforcementMode.Warn, 1)]
    [InlineData(RetentionEnforcementMode.Disabled, 2)]
    public void RetentionEnforcementMode_HasExpectedValues(RetentionEnforcementMode mode, int expected)
    {
        ((int)mode).Should().Be(expected);
    }

    [Fact]
    public void RetentionEnforcementMode_AllValuesDefined()
    {
        Enum.GetValues<RetentionEnforcementMode>().Should().HaveCount(3);
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

        model.IsExpired(DateTimeOffset.UtcNow).Should().BeTrue();
    }

    [Fact]
    public void RetentionRecordReadModel_IsExpired_WhenBeforeExpiresAtUtc_ReturnsFalse()
    {
        var model = new RetentionRecordReadModel
        {
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(30),
            Status = RetentionStatus.Active
        };

        model.IsExpired(DateTimeOffset.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void RetentionRecordReadModel_IsExpired_WhenDeleted_ReturnsFalse()
    {
        var model = new RetentionRecordReadModel
        {
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
            Status = RetentionStatus.Deleted
        };

        model.IsExpired(DateTimeOffset.UtcNow).Should().BeFalse();
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

        model.IsExpired(now).Should().BeTrue();
    }

    [Fact]
    public void RetentionRecordReadModel_DefaultValues()
    {
        var model = new RetentionRecordReadModel();

        model.EntityId.Should().Be(string.Empty);
        model.DataCategory.Should().Be(string.Empty);
        model.LegalHoldId.Should().BeNull();
        model.DeletedAtUtc.Should().BeNull();
        model.AnonymizedAtUtc.Should().BeNull();
        model.TenantId.Should().BeNull();
        model.ModuleId.Should().BeNull();
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

        result.TotalRecordsEvaluated.Should().Be(5);
        result.RecordsDeleted.Should().Be(2);
        result.RecordsRetained.Should().Be(1);
        result.RecordsFailed.Should().Be(1);
        result.RecordsUnderHold.Should().Be(1);
        result.Details.Should().HaveCount(2);
        result.ExecutedAtUtc.Should().Be(executedAt);
    }

    #endregion
}
