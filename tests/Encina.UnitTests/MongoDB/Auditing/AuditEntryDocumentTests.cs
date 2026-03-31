using Encina.MongoDB.Auditing;
using Encina.Security.Audit;

namespace Encina.UnitTests.MongoDB.Auditing;

public sealed class AuditEntryDocumentTests
{
    [Fact]
    public void FromEntry_MapsAllProperties()
    {
        var now = DateTimeOffset.UtcNow;
        var entry = new AuditEntry
        {
            Id = Guid.NewGuid(),
            CorrelationId = "corr-1",
            UserId = "user-1",
            Action = "Create",
            EntityType = "Order",
            EntityId = "order-123",
            Outcome = AuditOutcome.Success,
            TimestampUtc = now.UtcDateTime,
            StartedAtUtc = now,
            CompletedAtUtc = now
        };

        var doc = AuditEntryDocument.FromEntry(entry);

        doc.Id.ShouldBe(entry.Id);
        doc.CorrelationId.ShouldBe("corr-1");
        doc.UserId.ShouldBe("user-1");
        doc.Action.ShouldBe("Create");
        doc.EntityType.ShouldBe("Order");
        doc.EntityId.ShouldBe("order-123");
    }

    [Fact]
    public void ToEntry_MapsBackCorrectly()
    {
        var now = DateTime.UtcNow;
        var doc = new AuditEntryDocument
        {
            Id = Guid.NewGuid(),
            CorrelationId = "corr-2",
            UserId = "user-2",
            Action = "Update",
            EntityType = "Product",
            EntityId = "prod-456",
            Outcome = (int)AuditOutcome.Failure,
            TimestampUtc = now,
            StartedAtUtc = now,
            CompletedAtUtc = now
        };

        var entry = doc.ToEntry();

        entry.Id.ShouldBe(doc.Id);
        entry.CorrelationId.ShouldBe("corr-2");
        entry.Action.ShouldBe("Update");
        entry.EntityType.ShouldBe("Product");
    }

    [Fact]
    public void RoundTrip_PreservesData()
    {
        var now = DateTimeOffset.UtcNow;
        var original = new AuditEntry
        {
            Id = Guid.NewGuid(),
            CorrelationId = "round-trip",
            Action = "Delete",
            EntityType = "User",
            EntityId = "user-99",
            Outcome = AuditOutcome.Success,
            TimestampUtc = now.UtcDateTime,
            StartedAtUtc = now,
            CompletedAtUtc = now
        };

        var doc = AuditEntryDocument.FromEntry(original);
        var restored = doc.ToEntry();

        restored.Id.ShouldBe(original.Id);
        restored.CorrelationId.ShouldBe(original.CorrelationId);
        restored.Action.ShouldBe(original.Action);
        restored.EntityType.ShouldBe(original.EntityType);
        restored.EntityId.ShouldBe(original.EntityId);
    }

    [Fact]
    public void Defaults_AreCorrect()
    {
        var doc = new AuditEntryDocument();
        doc.Id.ShouldBe(Guid.Empty);
        doc.CorrelationId.ShouldBeEmpty();
        doc.Action.ShouldBeEmpty();
        doc.EntityType.ShouldBeEmpty();
        doc.UserId.ShouldBeNull();
        doc.ErrorMessage.ShouldBeNull();
    }
}
