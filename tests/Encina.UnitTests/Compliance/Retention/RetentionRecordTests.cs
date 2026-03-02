using Encina.Compliance.Retention.Model;
using FluentAssertions;

namespace Encina.UnitTests.Compliance.Retention;

/// <summary>
/// Unit tests for <see cref="RetentionRecord"/> factory method and record behavior.
/// </summary>
public class RetentionRecordTests
{
    #region Create Factory Method Tests

    [Fact]
    public void Create_ShouldSetEntityId()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow;
        var expiresAt = createdAt.AddDays(365);

        // Act
        var record = RetentionRecord.Create(
            entityId: "order-12345",
            dataCategory: "financial-records",
            createdAtUtc: createdAt,
            expiresAtUtc: expiresAt);

        // Assert
        record.EntityId.Should().Be("order-12345");
    }

    [Fact]
    public void Create_ShouldSetDataCategory()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow;
        var expiresAt = createdAt.AddDays(365);

        // Act
        var record = RetentionRecord.Create(
            entityId: "order-12345",
            dataCategory: "financial-records",
            createdAtUtc: createdAt,
            expiresAtUtc: expiresAt);

        // Assert
        record.DataCategory.Should().Be("financial-records");
    }

    [Fact]
    public void Create_ShouldSetStatusToActive()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow;
        var expiresAt = createdAt.AddDays(365);

        // Act
        var record = RetentionRecord.Create(
            entityId: "order-12345",
            dataCategory: "financial-records",
            createdAtUtc: createdAt,
            expiresAtUtc: expiresAt);

        // Assert
        record.Status.Should().Be(RetentionStatus.Active);
    }

    [Fact]
    public void Create_ShouldGenerateId_WithNoHyphens()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow;
        var expiresAt = createdAt.AddDays(365);

        // Act
        var record = RetentionRecord.Create(
            entityId: "order-12345",
            dataCategory: "financial-records",
            createdAtUtc: createdAt,
            expiresAtUtc: expiresAt);

        // Assert
        record.Id.Should().NotBeNullOrEmpty();
        record.Id.Should().HaveLength(32);
        record.Id.Should().NotContain("-");
    }

    [Fact]
    public void Create_ShouldSetCreatedAtUtcCorrectly()
    {
        // Arrange
        var createdAt = new DateTimeOffset(2025, 6, 15, 10, 30, 0, TimeSpan.Zero);
        var expiresAt = createdAt.AddDays(365);

        // Act
        var record = RetentionRecord.Create(
            entityId: "order-12345",
            dataCategory: "financial-records",
            createdAtUtc: createdAt,
            expiresAtUtc: expiresAt);

        // Assert
        record.CreatedAtUtc.Should().Be(createdAt);
    }

    [Fact]
    public void Create_ShouldSetExpiresAtUtcCorrectly()
    {
        // Arrange
        var createdAt = new DateTimeOffset(2025, 6, 15, 10, 30, 0, TimeSpan.Zero);
        var expiresAt = createdAt.AddDays(365);

        // Act
        var record = RetentionRecord.Create(
            entityId: "order-12345",
            dataCategory: "financial-records",
            createdAtUtc: createdAt,
            expiresAtUtc: expiresAt);

        // Assert
        record.ExpiresAtUtc.Should().Be(expiresAt);
    }

    [Fact]
    public void Create_WithPolicyId_ShouldStorePolicyId()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow;
        var expiresAt = createdAt.AddDays(365);

        // Act
        var record = RetentionRecord.Create(
            entityId: "order-12345",
            dataCategory: "financial-records",
            createdAtUtc: createdAt,
            expiresAtUtc: expiresAt,
            policyId: "policy-abc123");

        // Assert
        record.PolicyId.Should().Be("policy-abc123");
    }

    [Fact]
    public void Create_WithoutPolicyId_ShouldLeaveItNull()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow;
        var expiresAt = createdAt.AddDays(365);

        // Act
        var record = RetentionRecord.Create(
            entityId: "order-12345",
            dataCategory: "financial-records",
            createdAtUtc: createdAt,
            expiresAtUtc: expiresAt);

        // Assert
        record.PolicyId.Should().BeNull();
    }

    [Fact]
    public void Create_TwoCalls_ShouldGenerateDifferentIds()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow;
        var expiresAt = createdAt.AddDays(365);

        // Act
        var record1 = RetentionRecord.Create("entity-1", "financial-records", createdAt, expiresAt);
        var record2 = RetentionRecord.Create("entity-2", "financial-records", createdAt, expiresAt);

        // Assert
        record1.Id.Should().NotBe(record2.Id);
    }

    #endregion

    #region Init Property Tests

    [Fact]
    public void Properties_AreSettableViaInit()
    {
        // Arrange
        var createdAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var expiresAt = createdAt.AddYears(1);

        // Act
        var record = new RetentionRecord
        {
            Id = "abc123",
            EntityId = "order-99",
            DataCategory = "session-logs",
            CreatedAtUtc = createdAt,
            ExpiresAtUtc = expiresAt,
            Status = RetentionStatus.Expired
        };

        // Assert
        record.Id.Should().Be("abc123");
        record.EntityId.Should().Be("order-99");
        record.DataCategory.Should().Be("session-logs");
        record.CreatedAtUtc.Should().Be(createdAt);
        record.ExpiresAtUtc.Should().Be(expiresAt);
        record.Status.Should().Be(RetentionStatus.Expired);
    }

    #endregion
}
