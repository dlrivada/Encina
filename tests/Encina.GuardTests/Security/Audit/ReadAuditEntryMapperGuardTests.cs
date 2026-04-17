using Encina.Security.Audit;
using Shouldly;

namespace Encina.GuardTests.Security.Audit;

/// <summary>
/// Guard clause tests for <see cref="ReadAuditEntryMapper"/>.
/// Verifies that null arguments to mapping methods are properly rejected.
/// </summary>
public class ReadAuditEntryMapperGuardTests
{
    [Fact]
    public void MapToEntity_NullEntry_ThrowsArgumentNullException()
    {
        var act = () => ReadAuditEntryMapper.MapToEntity(null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("entry");
    }

    [Fact]
    public void MapToRecord_NullEntity_ThrowsArgumentNullException()
    {
        var act = () => ReadAuditEntryMapper.MapToRecord(null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("entity");
    }

    [Fact]
    public void MapToEntity_ValidEntry_DoesNotThrow()
    {
        var entry = new ReadAuditEntry
        {
            Id = Guid.NewGuid(),
            EntityType = "Patient",
            EntityId = "PAT-001",
            AccessedAtUtc = DateTimeOffset.UtcNow,
            AccessMethod = ReadAccessMethod.Repository,
            EntityCount = 1
        };

        var act = () => ReadAuditEntryMapper.MapToEntity(entry);

        Should.NotThrow(act);
    }

    [Fact]
    public void MapToRecord_ValidEntity_DoesNotThrow()
    {
        var entity = new ReadAuditEntryEntity
        {
            Id = Guid.NewGuid(),
            EntityType = "Patient",
            AccessedAtUtc = DateTimeOffset.UtcNow,
            AccessMethod = 0,
            EntityCount = 1
        };

        var act = () => ReadAuditEntryMapper.MapToRecord(entity);

        Should.NotThrow(act);
    }
}
