using Encina.Audit.Marten.Events;
using Encina.Audit.Marten.Projections;
using Encina.Security.Audit;

namespace Encina.UnitTests.AuditMarten;

/// <summary>
/// Unit tests exercising audit Marten POCO records and read models (events + projected models).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Provider", "Marten")]
public sealed class AuditEntryModelsTests
{
    #region AuditEntryRecordedEvent

    [Fact]
    public void AuditEntryRecordedEvent_PreservesAllProperties()
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var nowOffset = DateTimeOffset.UtcNow;

        var evt = new AuditEntryRecordedEvent
        {
            Id = id,
            CorrelationId = "corr-1",
            Action = "Create",
            EntityType = "Order",
            EntityId = "order-42",
            Outcome = (int)AuditOutcome.Success,
            ErrorMessage = null,
            TimestampUtc = now,
            StartedAtUtc = nowOffset,
            CompletedAtUtc = nowOffset.AddSeconds(1),
            RequestPayloadHash = "hash123",
            TenantId = "tenant-1",
            TemporalKeyPeriod = "2026-03",
            EncryptedUserId = null,
            EncryptedIpAddress = null,
            EncryptedUserAgent = null,
            EncryptedRequestPayload = null,
            EncryptedResponsePayload = null,
            EncryptedMetadata = null
        };

        evt.Id.ShouldBe(id);
        evt.CorrelationId.ShouldBe("corr-1");
        evt.Action.ShouldBe("Create");
        evt.EntityType.ShouldBe("Order");
        evt.EntityId.ShouldBe("order-42");
        evt.Outcome.ShouldBe((int)AuditOutcome.Success);
        evt.ErrorMessage.ShouldBeNull();
        evt.TimestampUtc.ShouldBe(now);
        evt.StartedAtUtc.ShouldBe(nowOffset);
        evt.RequestPayloadHash.ShouldBe("hash123");
        evt.TenantId.ShouldBe("tenant-1");
        evt.TemporalKeyPeriod.ShouldBe("2026-03");
    }

    [Fact]
    public void AuditEntryRecordedEvent_RecordEquality_WorksByValue()
    {
        var fixedTime = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc);
        var fixedOffset = new DateTimeOffset(fixedTime, TimeSpan.Zero);
        var id = Guid.NewGuid();

        var a = new AuditEntryRecordedEvent
        {
            Id = id,
            CorrelationId = "c",
            Action = "A",
            EntityType = "E",
            Outcome = 0,
            TimestampUtc = fixedTime,
            StartedAtUtc = fixedOffset,
            CompletedAtUtc = fixedOffset,
            TemporalKeyPeriod = "2026-03"
        };

        var b = new AuditEntryRecordedEvent
        {
            Id = id,
            CorrelationId = "c",
            Action = "A",
            EntityType = "E",
            Outcome = 0,
            TimestampUtc = fixedTime,
            StartedAtUtc = fixedOffset,
            CompletedAtUtc = fixedOffset,
            TemporalKeyPeriod = "2026-03"
        };

        a.ShouldBe(b);
        a.GetHashCode().ShouldBe(b.GetHashCode());
    }

    #endregion

    #region ReadAuditEntryRecordedEvent

    [Fact]
    public void ReadAuditEntryRecordedEvent_PreservesAllProperties()
    {
        var id = Guid.NewGuid();
        var accessed = DateTimeOffset.UtcNow;

        var evt = new ReadAuditEntryRecordedEvent
        {
            Id = id,
            EntityType = "Patient",
            EntityId = "p-1",
            AccessedAtUtc = accessed,
            AccessMethod = (int)ReadAccessMethod.Repository,
            EntityCount = 3,
            CorrelationId = "cr",
            TenantId = "t",
            TemporalKeyPeriod = "2026-03",
            EncryptedUserId = null,
            EncryptedPurpose = null,
            EncryptedMetadata = null
        };

        evt.Id.ShouldBe(id);
        evt.EntityType.ShouldBe("Patient");
        evt.EntityId.ShouldBe("p-1");
        evt.AccessedAtUtc.ShouldBe(accessed);
        evt.AccessMethod.ShouldBe((int)ReadAccessMethod.Repository);
        evt.EntityCount.ShouldBe(3);
        evt.CorrelationId.ShouldBe("cr");
        evt.TenantId.ShouldBe("t");
        evt.TemporalKeyPeriod.ShouldBe("2026-03");
    }

    #endregion

    #region AuditEntryReadModel

    [Fact]
    public void AuditEntryReadModel_DefaultValues_AreSensible()
    {
        var model = new AuditEntryReadModel();

        model.Id.ShouldBe(Guid.Empty);
        model.CorrelationId.ShouldBe(string.Empty);
        model.Action.ShouldBe(string.Empty);
        model.EntityType.ShouldBe(string.Empty);
        model.EntityId.ShouldBeNull();
        model.ErrorMessage.ShouldBeNull();
        model.RequestPayloadHash.ShouldBeNull();
        model.TenantId.ShouldBeNull();
        model.UserId.ShouldBeNull();
        model.IpAddress.ShouldBeNull();
        model.UserAgent.ShouldBeNull();
        model.RequestPayload.ShouldBeNull();
        model.ResponsePayload.ShouldBeNull();
        model.MetadataJson.ShouldBeNull();
        model.IsShredded.ShouldBeFalse();
        model.TemporalKeyPeriod.ShouldBe(string.Empty);
    }

    [Fact]
    public void AuditEntryReadModel_CanSetAllProperties()
    {
        var id = Guid.NewGuid();
        var ts = DateTime.UtcNow;
        var startedAt = DateTimeOffset.UtcNow;
        var completedAt = startedAt.AddMilliseconds(50);

        var model = new AuditEntryReadModel
        {
            Id = id,
            CorrelationId = "c",
            Action = "Update",
            EntityType = "Customer",
            EntityId = "cust-1",
            Outcome = AuditOutcome.Success,
            ErrorMessage = null,
            TimestampUtc = ts,
            StartedAtUtc = startedAt,
            CompletedAtUtc = completedAt,
            RequestPayloadHash = "h",
            TenantId = "t",
            UserId = "u",
            IpAddress = "ip",
            UserAgent = "ua",
            RequestPayload = "req",
            ResponsePayload = "resp",
            MetadataJson = "{}",
            IsShredded = false,
            TemporalKeyPeriod = "2026-03"
        };

        model.Id.ShouldBe(id);
        model.CorrelationId.ShouldBe("c");
        model.Action.ShouldBe("Update");
        model.EntityType.ShouldBe("Customer");
        model.EntityId.ShouldBe("cust-1");
        model.Outcome.ShouldBe(AuditOutcome.Success);
        model.TimestampUtc.ShouldBe(ts);
        model.StartedAtUtc.ShouldBe(startedAt);
        model.CompletedAtUtc.ShouldBe(completedAt);
        model.RequestPayloadHash.ShouldBe("h");
        model.TenantId.ShouldBe("t");
        model.UserId.ShouldBe("u");
        model.IpAddress.ShouldBe("ip");
        model.UserAgent.ShouldBe("ua");
        model.RequestPayload.ShouldBe("req");
        model.ResponsePayload.ShouldBe("resp");
        model.MetadataJson.ShouldBe("{}");
        model.IsShredded.ShouldBeFalse();
        model.TemporalKeyPeriod.ShouldBe("2026-03");
    }

    #endregion

    #region ReadAuditEntryReadModel

    [Fact]
    public void ReadAuditEntryReadModel_DefaultValues_AreSensible()
    {
        var model = new ReadAuditEntryReadModel();

        model.Id.ShouldBe(Guid.Empty);
        model.EntityType.ShouldBe(string.Empty);
        model.EntityId.ShouldBeNull();
        model.EntityCount.ShouldBe(0);
        model.CorrelationId.ShouldBeNull();
        model.TenantId.ShouldBeNull();
        model.UserId.ShouldBeNull();
        model.Purpose.ShouldBeNull();
        model.MetadataJson.ShouldBeNull();
        model.IsShredded.ShouldBeFalse();
        model.TemporalKeyPeriod.ShouldBe(string.Empty);
    }

    [Fact]
    public void ReadAuditEntryReadModel_CanSetAllProperties()
    {
        var id = Guid.NewGuid();
        var accessed = DateTimeOffset.UtcNow;

        var model = new ReadAuditEntryReadModel
        {
            Id = id,
            EntityType = "Patient",
            EntityId = "p-1",
            AccessedAtUtc = accessed,
            AccessMethod = ReadAccessMethod.Repository,
            EntityCount = 7,
            CorrelationId = "c",
            TenantId = "t",
            UserId = "u",
            Purpose = "treatment",
            MetadataJson = "{}",
            IsShredded = true,
            TemporalKeyPeriod = "2026-03"
        };

        model.Id.ShouldBe(id);
        model.EntityType.ShouldBe("Patient");
        model.EntityId.ShouldBe("p-1");
        model.AccessedAtUtc.ShouldBe(accessed);
        model.AccessMethod.ShouldBe(ReadAccessMethod.Repository);
        model.EntityCount.ShouldBe(7);
        model.CorrelationId.ShouldBe("c");
        model.TenantId.ShouldBe("t");
        model.UserId.ShouldBe("u");
        model.Purpose.ShouldBe("treatment");
        model.MetadataJson.ShouldBe("{}");
        model.IsShredded.ShouldBeTrue();
        model.TemporalKeyPeriod.ShouldBe("2026-03");
    }

    #endregion
}
