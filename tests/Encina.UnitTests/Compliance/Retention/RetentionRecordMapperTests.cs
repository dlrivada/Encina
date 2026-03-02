using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Model;

using FluentAssertions;

namespace Encina.UnitTests.Compliance.Retention;

/// <summary>
/// Unit tests for <see cref="RetentionRecordMapper"/> static mapping methods.
/// </summary>
public class RetentionRecordMapperTests
{
    #region ToEntity Tests

    [Fact]
    public void ToEntity_ValidRecord_ShouldMapAllProperties()
    {
        // Arrange
        var record = CreateRecord();

        // Act
        var entity = RetentionRecordMapper.ToEntity(record);

        // Assert
        entity.Id.Should().Be(record.Id);
        entity.EntityId.Should().Be(record.EntityId);
        entity.DataCategory.Should().Be(record.DataCategory);
        entity.PolicyId.Should().Be(record.PolicyId);
        entity.CreatedAtUtc.Should().Be(record.CreatedAtUtc);
        entity.ExpiresAtUtc.Should().Be(record.ExpiresAtUtc);
        entity.StatusValue.Should().Be((int)record.Status);
        entity.DeletedAtUtc.Should().Be(record.DeletedAtUtc);
        entity.LegalHoldId.Should().Be(record.LegalHoldId);
    }

    [Fact]
    public void ToEntity_ShouldConvertStatusToInt()
    {
        // Arrange
        var record = new RetentionRecord
        {
            Id = "r1",
            EntityId = "e1",
            DataCategory = "financial-records",
            CreatedAtUtc = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
            ExpiresAtUtc = new DateTimeOffset(2032, 1, 1, 0, 0, 0, TimeSpan.Zero),
            Status = RetentionStatus.Expired
        };

        // Act
        var entity = RetentionRecordMapper.ToEntity(record);

        // Assert
        entity.StatusValue.Should().Be(1);
    }

    [Fact]
    public void ToEntity_WithNullOptionalFields_ShouldMapNulls()
    {
        // Arrange
        var record = new RetentionRecord
        {
            Id = "r2",
            EntityId = "e2",
            DataCategory = "session-logs",
            CreatedAtUtc = new DateTimeOffset(2025, 3, 1, 0, 0, 0, TimeSpan.Zero),
            ExpiresAtUtc = new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero),
            Status = RetentionStatus.Active,
            PolicyId = null,
            DeletedAtUtc = null,
            LegalHoldId = null
        };

        // Act
        var entity = RetentionRecordMapper.ToEntity(record);

        // Assert
        entity.PolicyId.Should().BeNull();
        entity.DeletedAtUtc.Should().BeNull();
        entity.LegalHoldId.Should().BeNull();
    }

    [Fact]
    public void ToEntity_NullRecord_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => RetentionRecordMapper.ToEntity(null!);

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
        var domain = RetentionRecordMapper.ToDomain(entity);

        // Assert
        domain.Should().NotBeNull();
        domain!.Id.Should().Be(entity.Id);
        domain.EntityId.Should().Be(entity.EntityId);
        domain.DataCategory.Should().Be(entity.DataCategory);
        domain.PolicyId.Should().Be(entity.PolicyId);
        domain.CreatedAtUtc.Should().Be(entity.CreatedAtUtc);
        domain.ExpiresAtUtc.Should().Be(entity.ExpiresAtUtc);
        domain.Status.Should().Be((RetentionStatus)entity.StatusValue);
        domain.DeletedAtUtc.Should().Be(entity.DeletedAtUtc);
        domain.LegalHoldId.Should().Be(entity.LegalHoldId);
    }

    [Fact]
    public void ToDomain_InvalidStatusValue_ShouldReturnNull()
    {
        // Arrange
        var entity = CreateEntity();
        entity.StatusValue = 999;

        // Act
        var domain = RetentionRecordMapper.ToDomain(entity);

        // Assert
        domain.Should().BeNull();
    }

    [Fact]
    public void ToDomain_NullEntity_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => RetentionRecordMapper.ToDomain(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(0, RetentionStatus.Active)]
    [InlineData(1, RetentionStatus.Expired)]
    [InlineData(2, RetentionStatus.Deleted)]
    [InlineData(3, RetentionStatus.UnderLegalHold)]
    public void ToDomain_AllValidStatusValues_ShouldMapCorrectly(int statusValue, RetentionStatus expectedStatus)
    {
        // Arrange
        var entity = CreateEntity();
        entity.StatusValue = statusValue;

        // Act
        var domain = RetentionRecordMapper.ToDomain(entity);

        // Assert
        domain.Should().NotBeNull();
        domain!.Status.Should().Be(expectedStatus);
    }

    #endregion

    #region Roundtrip Tests

    [Fact]
    public void Roundtrip_ToEntityThenToDomain_ShouldPreserveAllFields()
    {
        // Arrange
        var original = CreateRecord();

        // Act
        var entity = RetentionRecordMapper.ToEntity(original);
        var roundtripped = RetentionRecordMapper.ToDomain(entity);

        // Assert
        roundtripped.Should().NotBeNull();
        roundtripped!.Id.Should().Be(original.Id);
        roundtripped.EntityId.Should().Be(original.EntityId);
        roundtripped.DataCategory.Should().Be(original.DataCategory);
        roundtripped.PolicyId.Should().Be(original.PolicyId);
        roundtripped.CreatedAtUtc.Should().Be(original.CreatedAtUtc);
        roundtripped.ExpiresAtUtc.Should().Be(original.ExpiresAtUtc);
        roundtripped.Status.Should().Be(original.Status);
        roundtripped.DeletedAtUtc.Should().Be(original.DeletedAtUtc);
        roundtripped.LegalHoldId.Should().Be(original.LegalHoldId);
    }

    #endregion

    private static RetentionRecord CreateRecord() => new()
    {
        Id = "record-001",
        EntityId = "order-12345",
        DataCategory = "financial-records",
        PolicyId = "policy-abc",
        CreatedAtUtc = new DateTimeOffset(2025, 1, 15, 8, 0, 0, TimeSpan.Zero),
        ExpiresAtUtc = new DateTimeOffset(2032, 1, 15, 8, 0, 0, TimeSpan.Zero),
        Status = RetentionStatus.Active,
        DeletedAtUtc = null,
        LegalHoldId = "hold-xyz"
    };

    private static RetentionRecordEntity CreateEntity() => new()
    {
        Id = "entity-001",
        EntityId = "invoice-99",
        DataCategory = "session-logs",
        PolicyId = "policy-999",
        CreatedAtUtc = new DateTimeOffset(2025, 3, 10, 12, 0, 0, TimeSpan.Zero),
        ExpiresAtUtc = new DateTimeOffset(2025, 6, 10, 12, 0, 0, TimeSpan.Zero),
        StatusValue = 0,
        DeletedAtUtc = null,
        LegalHoldId = null
    };
}
