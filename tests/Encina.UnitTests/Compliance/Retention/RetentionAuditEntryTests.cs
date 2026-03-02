using Encina.Compliance.Retention.Model;
using FluentAssertions;

namespace Encina.UnitTests.Compliance.Retention;

/// <summary>
/// Unit tests for <see cref="RetentionAuditEntry"/> factory method and record behavior.
/// </summary>
public class RetentionAuditEntryTests
{
    #region Create Factory Method Tests

    [Fact]
    public void Create_ShouldGenerateNonEmptyId()
    {
        // Act
        var entry = RetentionAuditEntry.Create(action: "PolicyCreated");

        // Assert
        entry.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Create_ShouldGenerateId_WithNoHyphens()
    {
        // Act
        var entry = RetentionAuditEntry.Create(action: "PolicyCreated");

        // Assert
        entry.Id.Should().HaveLength(32);
        entry.Id.Should().NotContain("-");
    }

    [Fact]
    public void Create_ShouldSetAction()
    {
        // Act
        var entry = RetentionAuditEntry.Create(action: "PolicyCreated");

        // Assert
        entry.Action.Should().Be("PolicyCreated");
    }

    [Fact]
    public void Create_ShouldSetOccurredAtUtcToNow()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var entry = RetentionAuditEntry.Create(action: "EnforcementExecuted");

        var after = DateTimeOffset.UtcNow;

        // Assert
        entry.OccurredAtUtc.Should().BeOnOrAfter(before);
        entry.OccurredAtUtc.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void Create_WithEntityId_ShouldSetEntityId()
    {
        // Act
        var entry = RetentionAuditEntry.Create(
            action: "RecordDeleted",
            entityId: "order-12345");

        // Assert
        entry.EntityId.Should().Be("order-12345");
    }

    [Fact]
    public void Create_WithoutEntityId_ShouldLeaveItNull()
    {
        // Act
        var entry = RetentionAuditEntry.Create(action: "EnforcementExecuted");

        // Assert
        entry.EntityId.Should().BeNull();
    }

    [Fact]
    public void Create_WithDataCategory_ShouldSetDataCategory()
    {
        // Act
        var entry = RetentionAuditEntry.Create(
            action: "RecordTracked",
            dataCategory: "financial-records");

        // Assert
        entry.DataCategory.Should().Be("financial-records");
    }

    [Fact]
    public void Create_WithoutDataCategory_ShouldLeaveItNull()
    {
        // Act
        var entry = RetentionAuditEntry.Create(action: "EnforcementExecuted");

        // Assert
        entry.DataCategory.Should().BeNull();
    }

    [Fact]
    public void Create_WithDetail_ShouldSetDetail()
    {
        // Act
        var entry = RetentionAuditEntry.Create(
            action: "EnforcementExecuted",
            detail: "Deleted 42 records, retained 3 under legal hold");

        // Assert
        entry.Detail.Should().Be("Deleted 42 records, retained 3 under legal hold");
    }

    [Fact]
    public void Create_WithoutDetail_ShouldLeaveItNull()
    {
        // Act
        var entry = RetentionAuditEntry.Create(action: "PolicyCreated");

        // Assert
        entry.Detail.Should().BeNull();
    }

    [Fact]
    public void Create_WithPerformedByUserId_ShouldSetPerformedByUserId()
    {
        // Act
        var entry = RetentionAuditEntry.Create(
            action: "LegalHoldApplied",
            performedByUserId: "legal-counsel@company.com");

        // Assert
        entry.PerformedByUserId.Should().Be("legal-counsel@company.com");
    }

    [Fact]
    public void Create_WithoutPerformedByUserId_ShouldLeaveItNull()
    {
        // Act
        var entry = RetentionAuditEntry.Create(action: "EnforcementExecuted");

        // Assert
        entry.PerformedByUserId.Should().BeNull();
    }

    [Fact]
    public void Create_WithAllParameters_ShouldSetAllProperties()
    {
        // Act
        var entry = RetentionAuditEntry.Create(
            action: "RecordDeleted",
            entityId: "order-12345",
            dataCategory: "financial-records",
            detail: "Deleted after 7 year retention period",
            performedByUserId: "enforcement-service");

        // Assert
        entry.Id.Should().NotBeNullOrEmpty();
        entry.Action.Should().Be("RecordDeleted");
        entry.EntityId.Should().Be("order-12345");
        entry.DataCategory.Should().Be("financial-records");
        entry.Detail.Should().Be("Deleted after 7 year retention period");
        entry.PerformedByUserId.Should().Be("enforcement-service");
    }

    [Fact]
    public void Create_TwoCalls_ShouldGenerateDifferentIds()
    {
        // Act
        var entry1 = RetentionAuditEntry.Create(action: "PolicyCreated");
        var entry2 = RetentionAuditEntry.Create(action: "RecordTracked");

        // Assert
        entry1.Id.Should().NotBe(entry2.Id);
    }

    #endregion

    #region Action Coverage Tests

    [Theory]
    [InlineData("PolicyCreated")]
    [InlineData("RecordTracked")]
    [InlineData("EnforcementExecuted")]
    [InlineData("RecordDeleted")]
    [InlineData("LegalHoldApplied")]
    [InlineData("LegalHoldReleased")]
    [InlineData("ExpirationAlertSent")]
    public void Create_WithEachAction_ShouldSetActionCorrectly(string action)
    {
        // Act
        var entry = RetentionAuditEntry.Create(action: action);

        // Assert
        entry.Action.Should().Be(action);
    }

    #endregion
}
