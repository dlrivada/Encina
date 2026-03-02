using Encina.Compliance.Retention.Model;

using FluentAssertions;

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
        result.TotalRecordsEvaluated.Should().Be(10);
        result.RecordsDeleted.Should().Be(7);
        result.RecordsRetained.Should().Be(1);
        result.RecordsFailed.Should().Be(1);
        result.RecordsUnderHold.Should().Be(1);
        result.Details.Should().HaveCount(1);
        result.ExecutedAtUtc.Should().Be(executedAt);
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
        result.Details.Should().BeEmpty();
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
        detail.EntityId.Should().Be("invoice-12345");
        detail.DataCategory.Should().Be("financial-records");
        detail.Outcome.Should().Be(DeletionOutcome.Deleted);
        detail.Reason.Should().BeNull();
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
        detail.Reason.Should().Be("Under active legal hold: Case #2024-456");
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
        detail.Reason.Should().BeNull();
    }

    #endregion

    #region DeletionOutcome Enum Tests

    [Fact]
    public void DeletionOutcome_Deleted_ShouldHaveValue0()
    {
        ((int)DeletionOutcome.Deleted).Should().Be(0);
    }

    [Fact]
    public void DeletionOutcome_Retained_ShouldHaveValue1()
    {
        ((int)DeletionOutcome.Retained).Should().Be(1);
    }

    [Fact]
    public void DeletionOutcome_Failed_ShouldHaveValue2()
    {
        ((int)DeletionOutcome.Failed).Should().Be(2);
    }

    [Fact]
    public void DeletionOutcome_HeldByLegalHold_ShouldHaveValue3()
    {
        ((int)DeletionOutcome.HeldByLegalHold).Should().Be(3);
    }

    [Fact]
    public void DeletionOutcome_Skipped_ShouldHaveValue4()
    {
        ((int)DeletionOutcome.Skipped).Should().Be(4);
    }

    [Fact]
    public void DeletionOutcome_ShouldDefineExactlyFiveValues()
    {
        Enum.GetValues<DeletionOutcome>().Should().HaveCount(5);
    }

    #endregion

    #region RetentionStatus Enum Tests

    [Fact]
    public void RetentionStatus_Active_ShouldHaveValue0()
    {
        ((int)RetentionStatus.Active).Should().Be(0);
    }

    [Fact]
    public void RetentionStatus_Expired_ShouldHaveValue1()
    {
        ((int)RetentionStatus.Expired).Should().Be(1);
    }

    [Fact]
    public void RetentionStatus_Deleted_ShouldHaveValue2()
    {
        ((int)RetentionStatus.Deleted).Should().Be(2);
    }

    [Fact]
    public void RetentionStatus_UnderLegalHold_ShouldHaveValue3()
    {
        ((int)RetentionStatus.UnderLegalHold).Should().Be(3);
    }

    [Fact]
    public void RetentionStatus_ShouldDefineExactlyFourValues()
    {
        Enum.GetValues<RetentionStatus>().Should().HaveCount(4);
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
        data.EntityId.Should().Be("order-12345");
        data.DataCategory.Should().Be("financial-records");
        data.ExpiresAtUtc.Should().Be(expiresAt);
        data.DaysUntilExpiration.Should().Be(7);
        data.PolicyId.Should().BeNull();
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
        data.PolicyId.Should().Be("policy-abc");
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
        data.DaysUntilExpiration.Should().Be(-5);
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
        data.DaysUntilExpiration.Should().Be(0);
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
        data.PolicyId.Should().BeNull();
    }

    #endregion
}
