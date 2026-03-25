using Encina.Security.Audit;

using Shouldly;

namespace Encina.UnitTests.Security.Audit;

/// <summary>
/// Unit tests for <see cref="ReadAuditEntryMapper"/>.
/// </summary>
public sealed class ReadAuditEntryMapperTests
{
    #region MapToEntity

    [Fact]
    public void MapToEntity_NullEntry_ShouldThrowArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ReadAuditEntryMapper.MapToEntity(null!));
    }

    [Fact]
    public void MapToEntity_ValidEntry_ShouldMapAllFields()
    {
        // Arrange
        var id = Guid.NewGuid();
        var accessedAt = DateTimeOffset.UtcNow;
        var entry = new ReadAuditEntry
        {
            Id = id,
            EntityType = "Patient",
            EntityId = "PAT-123",
            UserId = "user-1",
            TenantId = "tenant-a",
            AccessedAtUtc = accessedAt,
            CorrelationId = "corr-xyz",
            Purpose = "Medical review",
            AccessMethod = ReadAccessMethod.Repository,
            EntityCount = 1,
            Metadata = new Dictionary<string, object?> { ["source"] = "api" }
        };

        // Act
        var entity = ReadAuditEntryMapper.MapToEntity(entry);

        // Assert
        entity.Id.ShouldBe(id);
        entity.EntityType.ShouldBe("Patient");
        entity.EntityId.ShouldBe("PAT-123");
        entity.UserId.ShouldBe("user-1");
        entity.TenantId.ShouldBe("tenant-a");
        entity.AccessedAtUtc.ShouldBe(accessedAt);
        entity.CorrelationId.ShouldBe("corr-xyz");
        entity.Purpose.ShouldBe("Medical review");
        entity.AccessMethod.ShouldBe((int)ReadAccessMethod.Repository);
        entity.EntityCount.ShouldBe(1);
        entity.Metadata.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void MapToEntity_EmptyMetadata_ShouldProduceNullMetadataJson()
    {
        // Arrange
        var entry = new ReadAuditEntry
        {
            Id = Guid.NewGuid(),
            EntityType = "Order",
            EntityId = "ORD-1",
            AccessedAtUtc = DateTimeOffset.UtcNow,
            AccessMethod = ReadAccessMethod.Api,
            EntityCount = 5,
            Metadata = new Dictionary<string, object?>()
        };

        // Act
        var entity = ReadAuditEntryMapper.MapToEntity(entry);

        // Assert
        entity.Metadata.ShouldBeNull();
    }

    [Theory]
    [InlineData(ReadAccessMethod.Repository, 0)]
    [InlineData(ReadAccessMethod.DirectQuery, 1)]
    [InlineData(ReadAccessMethod.Api, 2)]
    [InlineData(ReadAccessMethod.Export, 3)]
    [InlineData(ReadAccessMethod.Custom, 4)]
    public void MapToEntity_AccessMethod_ShouldMapToIntOrdinal(ReadAccessMethod method, int expectedInt)
    {
        var entry = new ReadAuditEntry
        {
            Id = Guid.NewGuid(),
            EntityType = "Test",
            EntityId = null,
            AccessedAtUtc = DateTimeOffset.UtcNow,
            AccessMethod = method,
            EntityCount = 0
        };

        var entity = ReadAuditEntryMapper.MapToEntity(entry);

        entity.AccessMethod.ShouldBe(expectedInt);
    }

    #endregion

    #region MapToRecord

    [Fact]
    public void MapToRecord_NullEntity_ShouldThrowArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ReadAuditEntryMapper.MapToRecord(null!));
    }

    [Fact]
    public void MapToRecord_ValidEntity_ShouldMapAllFields()
    {
        // Arrange
        var id = Guid.NewGuid();
        var accessedAt = DateTimeOffset.UtcNow;
        var entity = new ReadAuditEntryEntity
        {
            Id = id,
            EntityType = "FinancialRecord",
            EntityId = "FIN-999",
            UserId = "user-2",
            TenantId = "tenant-b",
            AccessedAtUtc = accessedAt,
            CorrelationId = "corr-abc",
            Purpose = "Audit report",
            AccessMethod = (int)ReadAccessMethod.Export,
            EntityCount = 100,
            Metadata = """{"department":"finance"}"""
        };

        // Act
        var record = ReadAuditEntryMapper.MapToRecord(entity);

        // Assert
        record.Id.ShouldBe(id);
        record.EntityType.ShouldBe("FinancialRecord");
        record.EntityId.ShouldBe("FIN-999");
        record.UserId.ShouldBe("user-2");
        record.TenantId.ShouldBe("tenant-b");
        record.AccessedAtUtc.ShouldBe(accessedAt);
        record.CorrelationId.ShouldBe("corr-abc");
        record.Purpose.ShouldBe("Audit report");
        record.AccessMethod.ShouldBe(ReadAccessMethod.Export);
        record.EntityCount.ShouldBe(100);
        record.Metadata.ShouldNotBeEmpty();
    }

    [Fact]
    public void MapToRecord_NullMetadata_ShouldReturnEmptyDictionary()
    {
        var entity = new ReadAuditEntryEntity
        {
            Id = Guid.NewGuid(),
            EntityType = "Test",
            AccessedAtUtc = DateTimeOffset.UtcNow,
            AccessMethod = 0,
            EntityCount = 1,
            Metadata = null
        };

        var record = ReadAuditEntryMapper.MapToRecord(entity);

        record.Metadata.ShouldBeEmpty();
    }

    #endregion

    #region SerializeMetadata

    [Fact]
    public void SerializeMetadata_EmptyDictionary_ShouldReturnNull()
    {
        var result = ReadAuditEntryMapper.SerializeMetadata(new Dictionary<string, object?>());
        result.ShouldBeNull();
    }

    [Fact]
    public void SerializeMetadata_WithEntries_ShouldReturnJson()
    {
        var metadata = new Dictionary<string, object?>
        {
            ["key1"] = "value1",
            ["key2"] = 42
        };

        var result = ReadAuditEntryMapper.SerializeMetadata(metadata);

        result.ShouldNotBeNull();
        result.ShouldContain("key1");
        result.ShouldContain("value1");
    }

    #endregion

    #region DeserializeMetadata

    [Fact]
    public void DeserializeMetadata_NullJson_ShouldReturnEmptyDictionary()
    {
        var result = ReadAuditEntryMapper.DeserializeMetadata(null);
        result.ShouldBeEmpty();
    }

    [Fact]
    public void DeserializeMetadata_EmptyString_ShouldReturnEmptyDictionary()
    {
        var result = ReadAuditEntryMapper.DeserializeMetadata("");
        result.ShouldBeEmpty();
    }

    [Fact]
    public void DeserializeMetadata_InvalidJson_ShouldReturnEmptyDictionary()
    {
        var result = ReadAuditEntryMapper.DeserializeMetadata("not-json");
        result.ShouldBeEmpty();
    }

    [Fact]
    public void DeserializeMetadata_ValidJson_ShouldReturnDictionary()
    {
        var result = ReadAuditEntryMapper.DeserializeMetadata("""{"key":"value"}""");
        result.ShouldNotBeEmpty();
        result.ShouldContainKey("key");
    }

    #endregion

    #region Roundtrip

    [Fact]
    public void Roundtrip_MapToEntity_ThenMapToRecord_ShouldPreserveData()
    {
        // Arrange
        var original = new ReadAuditEntry
        {
            Id = Guid.NewGuid(),
            EntityType = "Customer",
            EntityId = "CUST-42",
            UserId = "admin",
            TenantId = "acme",
            AccessedAtUtc = DateTimeOffset.UtcNow,
            CorrelationId = "trace-1",
            Purpose = "Support ticket",
            AccessMethod = ReadAccessMethod.DirectQuery,
            EntityCount = 3,
            Metadata = new Dictionary<string, object?> { ["reason"] = "support" }
        };

        // Act
        var entity = ReadAuditEntryMapper.MapToEntity(original);
        var roundtripped = ReadAuditEntryMapper.MapToRecord(entity);

        // Assert
        roundtripped.Id.ShouldBe(original.Id);
        roundtripped.EntityType.ShouldBe(original.EntityType);
        roundtripped.EntityId.ShouldBe(original.EntityId);
        roundtripped.UserId.ShouldBe(original.UserId);
        roundtripped.TenantId.ShouldBe(original.TenantId);
        roundtripped.AccessedAtUtc.ShouldBe(original.AccessedAtUtc);
        roundtripped.CorrelationId.ShouldBe(original.CorrelationId);
        roundtripped.Purpose.ShouldBe(original.Purpose);
        roundtripped.AccessMethod.ShouldBe(original.AccessMethod);
        roundtripped.EntityCount.ShouldBe(original.EntityCount);
    }

    #endregion
}
