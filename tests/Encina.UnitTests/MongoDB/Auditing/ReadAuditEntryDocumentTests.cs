using Encina.MongoDB.Auditing;
using Encina.Security.Audit;

namespace Encina.UnitTests.MongoDB.Auditing;

public sealed class ReadAuditEntryDocumentTests
{
    [Fact]
    public void FromEntry_MapsAllProperties()
    {
        var entry = new ReadAuditEntry
        {
            Id = Guid.NewGuid(),
            EntityType = "Customer",
            EntityId = "cust-1",
            UserId = "user-1",
            AccessedAtUtc = DateTimeOffset.UtcNow,
            AccessMethod = ReadAccessMethod.Api,
            Purpose = "Support inquiry",
            EntityCount = 1
        };

        var doc = ReadAuditEntryDocument.FromEntry(entry);

        doc.Id.ShouldBe(entry.Id);
        doc.EntityType.ShouldBe("Customer");
        doc.EntityId.ShouldBe("cust-1");
        doc.UserId.ShouldBe("user-1");
        doc.Purpose.ShouldBe("Support inquiry");
    }

    [Fact]
    public void ToEntry_MapsBackCorrectly()
    {
        var doc = new ReadAuditEntryDocument
        {
            Id = Guid.NewGuid(),
            EntityType = "Order",
            EntityId = "order-1",
            UserId = "admin",
            AccessedAtUtc = DateTime.UtcNow,
            AccessMethod = (int)ReadAccessMethod.Repository,
            Purpose = "Compliance review",
            EntityCount = 5
        };

        var entry = doc.ToEntry();

        entry.Id.ShouldBe(doc.Id);
        entry.EntityType.ShouldBe("Order");
    }

    [Fact]
    public void RoundTrip_PreservesData()
    {
        var original = new ReadAuditEntry
        {
            Id = Guid.NewGuid(),
            EntityType = "Account",
            EntityId = "acc-42",
            UserId = "auditor",
            AccessedAtUtc = DateTimeOffset.UtcNow,
            AccessMethod = ReadAccessMethod.Export,
            Purpose = "Annual audit",
            EntityCount = 100
        };

        var doc = ReadAuditEntryDocument.FromEntry(original);
        var restored = doc.ToEntry();

        restored.Id.ShouldBe(original.Id);
        restored.EntityType.ShouldBe(original.EntityType);
        restored.EntityId.ShouldBe(original.EntityId);
    }

    [Fact]
    public void Defaults_AreCorrect()
    {
        var doc = new ReadAuditEntryDocument();
        doc.Id.ShouldBe(Guid.Empty);
        doc.EntityType.ShouldBeEmpty();
        doc.UserId.ShouldBeNull();
        doc.Purpose.ShouldBeNull();
    }
}
