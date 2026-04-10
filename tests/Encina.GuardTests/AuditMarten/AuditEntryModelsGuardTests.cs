using Encina.Audit.Marten.Events;
using Encina.Audit.Marten.Projections;
using Encina.Security.Audit;

namespace Encina.GuardTests.AuditMarten;

/// <summary>
/// Guard tests exercising the audit Marten event records and read models (POCO validation).
/// These cover property assignment paths on the public event and read model types.
/// </summary>
public class AuditEntryModelsGuardTests
{
    [Fact]
    public void AuditEntryRecordedEvent_RequiredProperties_AreAssignable()
    {
        var evt = new AuditEntryRecordedEvent
        {
            Id = Guid.NewGuid(),
            CorrelationId = "c",
            Action = "A",
            EntityType = "E",
            Outcome = 0,
            TimestampUtc = DateTime.UtcNow,
            StartedAtUtc = DateTimeOffset.UtcNow,
            CompletedAtUtc = DateTimeOffset.UtcNow,
            TemporalKeyPeriod = "2026-03"
        };

        evt.ShouldNotBeNull();
        evt.CorrelationId.ShouldBe("c");
        evt.Action.ShouldBe("A");
        evt.EntityType.ShouldBe("E");
        evt.TemporalKeyPeriod.ShouldBe("2026-03");
    }

    [Fact]
    public void ReadAuditEntryRecordedEvent_RequiredProperties_AreAssignable()
    {
        var evt = new ReadAuditEntryRecordedEvent
        {
            Id = Guid.NewGuid(),
            EntityType = "E",
            EntityId = null,
            AccessedAtUtc = DateTimeOffset.UtcNow,
            AccessMethod = 0,
            EntityCount = 0,
            TemporalKeyPeriod = "2026-03"
        };

        evt.ShouldNotBeNull();
        evt.EntityType.ShouldBe("E");
        evt.TemporalKeyPeriod.ShouldBe("2026-03");
    }

    [Fact]
    public void AuditEntryReadModel_Defaults_AreInitialized()
    {
        var model = new AuditEntryReadModel();
        model.CorrelationId.ShouldBe(string.Empty);
        model.Action.ShouldBe(string.Empty);
        model.EntityType.ShouldBe(string.Empty);
        model.TemporalKeyPeriod.ShouldBe(string.Empty);
        model.IsShredded.ShouldBeFalse();
    }

    [Fact]
    public void AuditEntryReadModel_AllProperties_AreSettable()
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var offset = DateTimeOffset.UtcNow;

        var model = new AuditEntryReadModel
        {
            Id = id,
            CorrelationId = "c",
            Action = "A",
            EntityType = "E",
            EntityId = "1",
            Outcome = AuditOutcome.Success,
            ErrorMessage = "err",
            TimestampUtc = now,
            StartedAtUtc = offset,
            CompletedAtUtc = offset.AddSeconds(1),
            RequestPayloadHash = "h",
            TenantId = "t",
            UserId = "u",
            IpAddress = "ip",
            UserAgent = "ua",
            RequestPayload = "req",
            ResponsePayload = "resp",
            MetadataJson = "{}",
            IsShredded = true,
            TemporalKeyPeriod = "2026-03"
        };

        model.Id.ShouldBe(id);
        model.IsShredded.ShouldBeTrue();
    }

    [Fact]
    public void ReadAuditEntryReadModel_Defaults_AreInitialized()
    {
        var model = new ReadAuditEntryReadModel();
        model.EntityType.ShouldBe(string.Empty);
        model.TemporalKeyPeriod.ShouldBe(string.Empty);
        model.EntityCount.ShouldBe(0);
        model.IsShredded.ShouldBeFalse();
    }

    [Fact]
    public void ReadAuditEntryReadModel_AllProperties_AreSettable()
    {
        var id = Guid.NewGuid();
        var accessed = DateTimeOffset.UtcNow;

        var model = new ReadAuditEntryReadModel
        {
            Id = id,
            EntityType = "E",
            EntityId = "1",
            AccessedAtUtc = accessed,
            AccessMethod = ReadAccessMethod.Repository,
            EntityCount = 2,
            CorrelationId = "c",
            TenantId = "t",
            UserId = "u",
            Purpose = "p",
            MetadataJson = "{}",
            IsShredded = false,
            TemporalKeyPeriod = "2026-03"
        };

        model.Id.ShouldBe(id);
        model.AccessMethod.ShouldBe(ReadAccessMethod.Repository);
        model.EntityCount.ShouldBe(2);
    }
}
