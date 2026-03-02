using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Model;

using FluentAssertions;

namespace Encina.UnitTests.Compliance.Retention;

/// <summary>
/// Unit tests for <see cref="RetentionAuditEntryMapper"/> static mapping methods.
/// </summary>
public class RetentionAuditEntryMapperTests
{
    #region ToEntity Tests

    [Fact]
    public void ToEntity_ValidEntry_ShouldMapAllProperties()
    {
        // Arrange
        var entry = CreateEntry();

        // Act
        var entity = RetentionAuditEntryMapper.ToEntity(entry);

        // Assert
        entity.Id.Should().Be(entry.Id);
        entity.Action.Should().Be(entry.Action);
        entity.EntityId.Should().Be(entry.EntityId);
        entity.DataCategory.Should().Be(entry.DataCategory);
        entity.Detail.Should().Be(entry.Detail);
        entity.PerformedByUserId.Should().Be(entry.PerformedByUserId);
        entity.OccurredAtUtc.Should().Be(entry.OccurredAtUtc);
    }

    [Fact]
    public void ToEntity_WithAllNullOptionalFields_ShouldMapNulls()
    {
        // Arrange
        var entry = new RetentionAuditEntry
        {
            Id = "audit-null",
            Action = "EnforcementExecuted",
            EntityId = null,
            DataCategory = null,
            Detail = null,
            PerformedByUserId = null,
            OccurredAtUtc = new DateTimeOffset(2025, 5, 1, 0, 0, 0, TimeSpan.Zero)
        };

        // Act
        var entity = RetentionAuditEntryMapper.ToEntity(entry);

        // Assert
        entity.EntityId.Should().BeNull();
        entity.DataCategory.Should().BeNull();
        entity.Detail.Should().BeNull();
        entity.PerformedByUserId.Should().BeNull();
    }

    [Fact]
    public void ToEntity_NullEntry_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => RetentionAuditEntryMapper.ToEntity(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region ToDomain Tests

    [Fact]
    public void ToDomain_ValidEntity_ShouldMapAllProperties()
    {
        // Arrange
        var entity = CreateEntity();

        // Act
        var domain = RetentionAuditEntryMapper.ToDomain(entity);

        // Assert
        domain.Id.Should().Be(entity.Id);
        domain.Action.Should().Be(entity.Action);
        domain.EntityId.Should().Be(entity.EntityId);
        domain.DataCategory.Should().Be(entity.DataCategory);
        domain.Detail.Should().Be(entity.Detail);
        domain.PerformedByUserId.Should().Be(entity.PerformedByUserId);
        domain.OccurredAtUtc.Should().Be(entity.OccurredAtUtc);
    }

    [Fact]
    public void ToDomain_WithNullOptionalFields_ShouldMapNulls()
    {
        // Arrange
        var entity = new RetentionAuditEntryEntity
        {
            Id = "audit-null",
            Action = "PolicyCreated",
            EntityId = null,
            DataCategory = null,
            Detail = null,
            PerformedByUserId = null,
            OccurredAtUtc = new DateTimeOffset(2025, 5, 1, 0, 0, 0, TimeSpan.Zero)
        };

        // Act
        var domain = RetentionAuditEntryMapper.ToDomain(entity);

        // Assert
        domain.EntityId.Should().BeNull();
        domain.DataCategory.Should().BeNull();
        domain.Detail.Should().BeNull();
        domain.PerformedByUserId.Should().BeNull();
    }

    [Fact]
    public void ToDomain_NullEntity_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => RetentionAuditEntryMapper.ToDomain(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Roundtrip Tests

    [Fact]
    public void Roundtrip_ToEntityThenToDomain_ShouldPreserveAllFields()
    {
        // Arrange
        var original = CreateEntry();

        // Act
        var entity = RetentionAuditEntryMapper.ToEntity(original);
        var roundtripped = RetentionAuditEntryMapper.ToDomain(entity);

        // Assert
        roundtripped.Id.Should().Be(original.Id);
        roundtripped.Action.Should().Be(original.Action);
        roundtripped.EntityId.Should().Be(original.EntityId);
        roundtripped.DataCategory.Should().Be(original.DataCategory);
        roundtripped.Detail.Should().Be(original.Detail);
        roundtripped.PerformedByUserId.Should().Be(original.PerformedByUserId);
        roundtripped.OccurredAtUtc.Should().Be(original.OccurredAtUtc);
    }

    [Fact]
    public void Roundtrip_WithNullOptionalFields_ShouldPreserveNulls()
    {
        // Arrange
        var original = new RetentionAuditEntry
        {
            Id = "audit-roundtrip-null",
            Action = "RecordDeleted",
            EntityId = null,
            DataCategory = null,
            Detail = null,
            PerformedByUserId = null,
            OccurredAtUtc = new DateTimeOffset(2025, 7, 15, 14, 30, 0, TimeSpan.Zero)
        };

        // Act
        var entity = RetentionAuditEntryMapper.ToEntity(original);
        var roundtripped = RetentionAuditEntryMapper.ToDomain(entity);

        // Assert
        roundtripped.EntityId.Should().BeNull();
        roundtripped.DataCategory.Should().BeNull();
        roundtripped.Detail.Should().BeNull();
        roundtripped.PerformedByUserId.Should().BeNull();
    }

    #endregion

    private static RetentionAuditEntry CreateEntry() => new()
    {
        Id = "audit-001",
        Action = "LegalHoldApplied",
        EntityId = "invoice-12345",
        DataCategory = "financial-records",
        Detail = "Pending tax audit for fiscal year 2024",
        PerformedByUserId = "legal-counsel@company.com",
        OccurredAtUtc = new DateTimeOffset(2025, 3, 5, 10, 0, 0, TimeSpan.Zero)
    };

    private static RetentionAuditEntryEntity CreateEntity() => new()
    {
        Id = "entity-001",
        Action = "RecordTracked",
        EntityId = "order-99",
        DataCategory = "session-logs",
        Detail = "Tracked via pipeline behavior",
        PerformedByUserId = null,
        OccurredAtUtc = new DateTimeOffset(2025, 4, 1, 8, 0, 0, TimeSpan.Zero)
    };
}
