using Encina.Security.Audit;
using Shouldly;

namespace Encina.UnitTests.Security.Audit;

/// <summary>
/// Unit tests for <see cref="ReadAuditEntryEntity"/> persistence entity.
/// Verifies property defaults, required fields, and nullable semantics.
/// </summary>
public sealed class ReadAuditEntryEntityTests
{
    [Fact]
    public void RequiredProperties_CanBeSet()
    {
        var id = Guid.NewGuid();
        var accessedAt = DateTimeOffset.UtcNow;

        var entity = new ReadAuditEntryEntity
        {
            Id = id,
            EntityType = "Patient",
            AccessedAtUtc = accessedAt
        };

        entity.Id.ShouldBe(id);
        entity.EntityType.ShouldBe("Patient");
        entity.AccessedAtUtc.ShouldBe(accessedAt);
    }

    [Fact]
    public void OptionalProperties_DefaultToNullOrZero()
    {
        var entity = new ReadAuditEntryEntity
        {
            Id = Guid.NewGuid(),
            EntityType = "Test",
            AccessedAtUtc = DateTimeOffset.UtcNow
        };

        entity.EntityId.ShouldBeNull();
        entity.UserId.ShouldBeNull();
        entity.TenantId.ShouldBeNull();
        entity.CorrelationId.ShouldBeNull();
        entity.Purpose.ShouldBeNull();
        entity.Metadata.ShouldBeNull();
        entity.AccessMethod.ShouldBe(0);
        entity.EntityCount.ShouldBe(0);
    }

    [Fact]
    public void AllProperties_CanBeSetAndRetrieved()
    {
        var entity = new ReadAuditEntryEntity
        {
            Id = Guid.NewGuid(),
            EntityType = "FinancialRecord",
            EntityId = "FIN-42",
            UserId = "user-admin",
            TenantId = "acme-corp",
            AccessedAtUtc = DateTimeOffset.UtcNow,
            CorrelationId = "trace-xyz",
            Purpose = "Compliance audit",
            AccessMethod = (int)ReadAccessMethod.Export,
            EntityCount = 250,
            Metadata = "{\"source\":\"report\"}"
        };

        entity.EntityId.ShouldBe("FIN-42");
        entity.UserId.ShouldBe("user-admin");
        entity.TenantId.ShouldBe("acme-corp");
        entity.CorrelationId.ShouldBe("trace-xyz");
        entity.Purpose.ShouldBe("Compliance audit");
        entity.AccessMethod.ShouldBe(3);
        entity.EntityCount.ShouldBe(250);
        entity.Metadata.ShouldBe("{\"source\":\"report\"}");
    }
}
