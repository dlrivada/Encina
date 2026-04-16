using Encina.Compliance.Retention.Model;

using Shouldly;

namespace Encina.UnitTests.Compliance.Retention;

/// <summary>
/// Unit tests for <see cref="DeletionResult"/>, <see cref="DeletionDetail"/>,
/// <see cref="ExpiringData"/>, <see cref="DeletionOutcome"/>, and <see cref="RetentionStatus"/> models.
/// </summary>
public class DeletionResultTests
{
    #region DeletionResult Tests

    [Fact]
    public void DeletionResult_AllRequiredProperties_ShouldBeSetViaInit()
    {
        // Arrange
        var executedAt = new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var details = new List<DeletionDetail>
        {
            new() { EntityId = "e1", DataCategory = "financial-records", Outcome = DeletionOutcome.Deleted }
        };

        // Act
        var result = new DeletionResult
        {
            TotalRecordsEvaluated = 10,
            RecordsDeleted = 7,
            RecordsRetained = 1,
            RecordsFailed = 1,
            RecordsUnderHold = 1,
            Details = details,
            ExecutedAtUtc = executedAt
        };

        // Assert
        result.TotalRecordsEvaluated.ShouldBe(10);
        result.RecordsDeleted.ShouldBe(7);
        result.RecordsRetained.ShouldBe(1);
        result.RecordsFailed.ShouldBe(1);
        result.RecordsUnderHold.ShouldBe(1);
        result.Details.Count.ShouldBe(1);
        result.ExecutedAtUtc.ShouldBe(executedAt);
    }

    [Fact]
    public void DeletionResult_WithEmptyDetails_ShouldHaveEmptyList()
    {
        // Act
        var result = new DeletionResult
        {
            TotalRecordsEvaluated = 0,
            RecordsDeleted = 0,
            RecordsRetained = 0,
            RecordsFailed = 0,
            RecordsUnderHold = 0,
            Details = [],
            ExecutedAtUtc = DateTimeOffset.UtcNow
        };

        // Assert
        result.Details.ShouldBeEmpty();
    }

    #endregion

    #region DeletionDetail Tests

    [Fact]
    public void DeletionDetail_AllRequiredProperties_ShouldBeSetViaInit()
    {
        // Act
        var detail = new DeletionDetail
        {
            EntityId = "invoice-12345",
            DataCategory = "financial-records",
            Outcome = DeletionOutcome.Deleted
        };

        // Assert
        detail.EntityId.ShouldBe("invoice-12345");
        detail.DataCategory.ShouldBe("financial-records");
        detail.Outcome.ShouldBe(DeletionOutcome.Deleted);
        detail.Reason.ShouldBeNull();
    }

    [Fact]
    public void DeletionDetail_WithOptionalReason_ShouldStoreReason()
    {
        // Act
        var detail = new DeletionDetail
        {
            EntityId = "invoice-99",
            DataCategory = "financial-records",
            Outcome = DeletionOutcome.HeldByLegalHold,
            Reason = "Under active legal hold: Case #2024-456"
        };

        // Assert
        detail.Reason.ShouldBe("Under active legal hold: Case #2024-456");
    }

    [Fact]
    public void DeletionDetail_WithoutReason_ShouldHaveNullReason()
    {
        // Act
        var detail = new DeletionDetail
        {
            EntityId = "order-1",
            DataCategory = "session-logs",
            Outcome = DeletionOutcome.Deleted
        };

        // Assert
        detail.Reason.ShouldBeNull();
    }

    #endregion

    #region DeletionOutcome Enum Tests

    [Fact]
    public void DeletionOutcome_Deleted_ShouldHaveValue0()
    {
        ((int)DeletionOutcome.Deleted).ShouldBe(0);
    }

    [Fact]
    public void DeletionOutcome_Retained_ShouldHaveValue1()
    {
        ((int)DeletionOutcome.Retained).ShouldBe(1);
    }

    [Fact]
    public void DeletionOutcome_Failed_ShouldHaveValue2()
    {
        ((int)DeletionOutcome.Failed).ShouldBe(2);
    }

    [Fact]
    public void DeletionOutcome_HeldByLegalHold_ShouldHaveValue3()
    {
        ((int)DeletionOutcome.HeldByLegalHold).ShouldBe(3);
    }

    [Fact]
    public void DeletionOutcome_Skipped_ShouldHaveValue4()
    {
        ((int)DeletionOutcome.Skipped).ShouldBe(4);
    }

    [Fact]
    public void DeletionOutcome_ShouldDefineExactlyFiveValues()
    {
        Enum.GetValues<DeletionOutcome>().Count.ShouldBe(5);
    }

    #endregion

    #region RetentionStatus Enum Tests

    [Fact]
    public void RetentionStatus_Active_ShouldHaveValue0()
    {
        ((int)RetentionStatus.Active).ShouldBe(0);
    }

    [Fact]
    public void RetentionStatus_Expired_ShouldHaveValue1()
    {
        ((int)RetentionStatus.Expired).ShouldBe(1);
    }

    [Fact]
    public void RetentionStatus_Deleted_ShouldHaveValue2()
    {
        ((int)RetentionStatus.Deleted).ShouldBe(2);
    }

    [Fact]
    public void RetentionStatus_UnderLegalHold_ShouldHaveValue3()
    {
        ((int)RetentionStatus.UnderLegalHold).ShouldBe(3);
    }

    [Fact]
    public void RetentionStatus_ShouldDefineExactlyFourValues()
    {
        Enum.GetValues<RetentionStatus>().Count.ShouldBe(4);
    }

    #endregion

    #region ExpiringData Tests

    [Fact]
    public void ExpiringData_AllRequiredProperties_ShouldBeSetViaInit()
    {
        // Arrange
        var expiresAt = new DateTimeOffset(2025, 6, 15, 0, 0, 0, TimeSpan.Zero);

        // Act
        var data = new ExpiringData
        {
            EntityId = "order-12345",
            DataCategory = "financial-records",
            ExpiresAtUtc = expiresAt,
            DaysUntilExpiration = 7
        };

        // Assert
        data.EntityId.ShouldBe("order-12345");
        data.DataCategory.ShouldBe("financial-records");
        data.ExpiresAtUtc.ShouldBe(expiresAt);
        data.DaysUntilExpiration.ShouldBe(7);
        data.PolicyId.ShouldBeNull();
    }

    [Fact]
    public void ExpiringData_WithPolicyId_ShouldStorePolicyId()
    {
        // Act
        var data = new ExpiringData
        {
            EntityId = "order-99",
            DataCategory = "session-logs",
            ExpiresAtUtc = DateTimeOffset.UtcNow,
            DaysUntilExpiration = 3,
            PolicyId = "policy-abc"
        };

        // Assert
        data.PolicyId.ShouldBe("policy-abc");
    }

    [Fact]
    public void ExpiringData_DaysUntilExpiration_CanBeNegative()
    {
        // Act
        var data = new ExpiringData
        {
            EntityId = "order-past",
            DataCategory = "financial-records",
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(-5),
            DaysUntilExpiration = -5
        };

        // Assert
        data.DaysUntilExpiration.ShouldBe(-5);
    }

    [Fact]
    public void ExpiringData_DaysUntilExpiration_CanBeZero()
    {
        // Act
        var data = new ExpiringData
        {
            EntityId = "order-today",
            DataCategory = "financial-records",
            ExpiresAtUtc = DateTimeOffset.UtcNow,
            DaysUntilExpiration = 0
        };

        // Assert
        data.DaysUntilExpiration.ShouldBe(0);
    }

    [Fact]
    public void ExpiringData_WithoutPolicyId_ShouldHaveNullPolicyId()
    {
        // Act
        var data = new ExpiringData
        {
            EntityId = "invoice-001",
            DataCategory = "marketing-consent",
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(2),
            DaysUntilExpiration = 2
        };

        // Assert
        data.PolicyId.ShouldBeNull();
    }

    #endregion
}
