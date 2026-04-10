using Encina.Audit.Marten;
using Encina.Audit.Marten.Crypto;
using Encina.Audit.Marten.Events;
using Encina.Audit.Marten.Projections;
using Encina.Security.Audit;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace Encina.UnitTests.AuditMarten;

/// <summary>
/// Unit tests for <see cref="AuditEntryProjection"/> event-to-read-model projection logic.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Provider", "Marten")]
public sealed class AuditEntryProjectionTests
{
    private static ServiceProvider BuildServices(InMemoryTemporalKeyProvider keyProvider, MartenAuditOptions? options = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ITemporalKeyProvider>(keyProvider);
        services.Configure<MartenAuditOptions>(o =>
        {
            var cfg = options ?? new MartenAuditOptions();
            o.TemporalGranularity = cfg.TemporalGranularity;
            o.EncryptionScope = cfg.EncryptionScope;
            o.RetentionPeriod = cfg.RetentionPeriod;
            o.ShreddedPlaceholder = cfg.ShreddedPlaceholder;
            o.EnableAutoPurge = cfg.EnableAutoPurge;
            o.PurgeIntervalHours = cfg.PurgeIntervalHours;
        });
        services.AddLogging();
        return services.BuildServiceProvider();
    }

    private static InMemoryTemporalKeyProvider CreateKeyProvider(FakeTimeProvider? time = null) =>
        new(time ?? new FakeTimeProvider(new DateTimeOffset(2026, 3, 15, 12, 0, 0, TimeSpan.Zero)),
            NullLogger<InMemoryTemporalKeyProvider>.Instance);

    [Fact]
    public void Constructor_SetsName()
    {
        var projection = new AuditEntryProjection();
        projection.Name.ShouldBe("AuditEntryProjection");
    }

    [Fact]
    public async Task Create_WithValidKey_DecryptsPiiFields()
    {
        // Arrange
        var keyProvider = CreateKeyProvider();
        var keyResult = await keyProvider.GetOrCreateKeyAsync("2026-03");
        byte[] keyMaterial = null!;
        string keyId = null!;
        keyResult.IfRight(k => { keyMaterial = k.KeyMaterial; keyId = k.KeyId; });

        var services = BuildServices(keyProvider);
        var projection = new AuditEntryProjection();

        var evt = new AuditEntryRecordedEvent
        {
            Id = Guid.NewGuid(),
            CorrelationId = "corr-1",
            Action = "Update",
            EntityType = "Order",
            EntityId = "order-42",
            Outcome = (int)AuditOutcome.Success,
            ErrorMessage = null,
            TimestampUtc = DateTime.UtcNow,
            StartedAtUtc = DateTimeOffset.UtcNow,
            CompletedAtUtc = DateTimeOffset.UtcNow.AddMilliseconds(10),
            RequestPayloadHash = "abc123",
            TenantId = "tenant-1",
            EncryptedUserId = EncryptedField.Encrypt("user@example.com", keyMaterial, keyId),
            EncryptedIpAddress = EncryptedField.Encrypt("192.168.1.1", keyMaterial, keyId),
            EncryptedUserAgent = EncryptedField.Encrypt("Mozilla/5.0", keyMaterial, keyId),
            EncryptedRequestPayload = EncryptedField.Encrypt("{}", keyMaterial, keyId),
            EncryptedResponsePayload = EncryptedField.Encrypt("{\"ok\":true}", keyMaterial, keyId),
            EncryptedMetadata = EncryptedField.Encrypt("{\"k\":\"v\"}", keyMaterial, keyId),
            TemporalKeyPeriod = "2026-03"
        };

        // Act
        var readModel = await projection.Create(evt, services);

        // Assert
        readModel.ShouldNotBeNull();
        readModel.Id.ShouldBe(evt.Id);
        readModel.CorrelationId.ShouldBe("corr-1");
        readModel.Action.ShouldBe("Update");
        readModel.EntityType.ShouldBe("Order");
        readModel.EntityId.ShouldBe("order-42");
        readModel.Outcome.ShouldBe(AuditOutcome.Success);
        readModel.TenantId.ShouldBe("tenant-1");
        readModel.UserId.ShouldBe("user@example.com");
        readModel.IpAddress.ShouldBe("192.168.1.1");
        readModel.UserAgent.ShouldBe("Mozilla/5.0");
        readModel.RequestPayload.ShouldBe("{}");
        readModel.ResponsePayload.ShouldBe("{\"ok\":true}");
        readModel.MetadataJson.ShouldBe("{\"k\":\"v\"}");
        readModel.IsShredded.ShouldBeFalse();
        readModel.TemporalKeyPeriod.ShouldBe("2026-03");
    }

    [Fact]
    public async Task Create_WithMissingKey_ReturnsShreddedModel()
    {
        // Arrange
        var keyProvider = CreateKeyProvider();
        // DO NOT create key for "2099-12" - it will be shredded
        var services = BuildServices(keyProvider);
        var projection = new AuditEntryProjection();

        // Encrypt using a different key that won't be found
        var dummyKey = new byte[32];
        System.Security.Cryptography.RandomNumberGenerator.Fill(dummyKey);
        var encrypted = EncryptedField.Encrypt("secret", dummyKey, "temporal:2099-12:v1");

        var evt = new AuditEntryRecordedEvent
        {
            Id = Guid.NewGuid(),
            CorrelationId = "corr-x",
            Action = "Delete",
            EntityType = "User",
            EntityId = null,
            Outcome = (int)AuditOutcome.Failure,
            ErrorMessage = "denied",
            TimestampUtc = DateTime.UtcNow,
            StartedAtUtc = DateTimeOffset.UtcNow,
            CompletedAtUtc = DateTimeOffset.UtcNow,
            RequestPayloadHash = null,
            TenantId = null,
            EncryptedUserId = encrypted,
            EncryptedIpAddress = null,
            EncryptedUserAgent = null,
            EncryptedRequestPayload = null,
            EncryptedResponsePayload = null,
            EncryptedMetadata = null,
            TemporalKeyPeriod = "2099-12"
        };

        // Act
        var readModel = await projection.Create(evt, services);

        // Assert
        readModel.IsShredded.ShouldBeTrue();
        readModel.UserId.ShouldBe("[SHREDDED]");
        readModel.Outcome.ShouldBe(AuditOutcome.Failure);
        readModel.ErrorMessage.ShouldBe("denied");
    }

    [Fact]
    public async Task Create_WithNullEncryptedFields_ReturnsModelWithNulls()
    {
        // Arrange
        var keyProvider = CreateKeyProvider();
        await keyProvider.GetOrCreateKeyAsync("2026-03");
        var services = BuildServices(keyProvider);
        var projection = new AuditEntryProjection();

        var evt = new AuditEntryRecordedEvent
        {
            Id = Guid.NewGuid(),
            CorrelationId = "corr-nulls",
            Action = "Read",
            EntityType = "Report",
            EntityId = null,
            Outcome = (int)AuditOutcome.Success,
            ErrorMessage = null,
            TimestampUtc = DateTime.UtcNow,
            StartedAtUtc = DateTimeOffset.UtcNow,
            CompletedAtUtc = DateTimeOffset.UtcNow,
            RequestPayloadHash = null,
            TenantId = null,
            EncryptedUserId = null,
            EncryptedIpAddress = null,
            EncryptedUserAgent = null,
            EncryptedRequestPayload = null,
            EncryptedResponsePayload = null,
            EncryptedMetadata = null,
            TemporalKeyPeriod = "2026-03"
        };

        // Act
        var readModel = await projection.Create(evt, services);

        // Assert
        readModel.UserId.ShouldBeNull();
        readModel.IpAddress.ShouldBeNull();
        readModel.UserAgent.ShouldBeNull();
        readModel.RequestPayload.ShouldBeNull();
        readModel.ResponsePayload.ShouldBeNull();
        readModel.MetadataJson.ShouldBeNull();
        readModel.IsShredded.ShouldBeFalse();
    }

    [Fact]
    public async Task Create_UsesCustomShreddedPlaceholder()
    {
        // Arrange
        var keyProvider = CreateKeyProvider();
        var options = new MartenAuditOptions { ShreddedPlaceholder = "<REDACTED>" };
        var services = BuildServices(keyProvider, options);
        var projection = new AuditEntryProjection();

        var dummyKey = new byte[32];
        System.Security.Cryptography.RandomNumberGenerator.Fill(dummyKey);
        var encrypted = EncryptedField.Encrypt("secret", dummyKey, "temporal:1999-01:v1");

        var evt = new AuditEntryRecordedEvent
        {
            Id = Guid.NewGuid(),
            CorrelationId = "c",
            Action = "x",
            EntityType = "E",
            Outcome = 0,
            TimestampUtc = DateTime.UtcNow,
            StartedAtUtc = DateTimeOffset.UtcNow,
            CompletedAtUtc = DateTimeOffset.UtcNow,
            EncryptedUserId = encrypted,
            TemporalKeyPeriod = "1999-01"
        };

        // Act
        var readModel = await projection.Create(evt, services);

        // Assert
        readModel.UserId.ShouldBe("<REDACTED>");
        readModel.IsShredded.ShouldBeTrue();
    }
}
