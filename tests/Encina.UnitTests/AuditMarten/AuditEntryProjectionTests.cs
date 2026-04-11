using System.Security.Cryptography;

using Encina.Audit.Marten;
using Encina.Audit.Marten.Crypto;
using Encina.Audit.Marten.Events;
using Encina.Audit.Marten.Projections;
using Encina.Security.Audit;

using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.UnitTests.AuditMarten;

/// <summary>
/// Unit tests for <see cref="AuditEntryProjection"/> event-to-read-model projection logic.
/// </summary>
/// <remarks>
/// These tests exercise the internal mapping helpers (<c>MapToReadModel</c>) directly. The
/// full <c>Create(IDocumentOperations, ...)</c> path cannot be exercised from unit tests
/// because it requires a real Marten document store to run the projection daemon — that
/// coverage belongs to the integration test suite.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Provider", "Marten")]
public sealed class AuditEntryProjectionTests
{
    private static AuditEntryProjection CreateProjection(string placeholder = "[SHREDDED]") =>
        new(placeholder, NullLogger<AuditEntryProjection>.Instance);

    [Fact]
    public void Constructor_Parameterless_SetsNameAndUsesDefaultShreddedPlaceholder()
    {
        // Arrange: parameterless ctor should fall back to MartenAuditOptions.DefaultShreddedPlaceholder
        var projection = new AuditEntryProjection();

        var evt = new AuditEntryRecordedEvent
        {
            Id = Guid.NewGuid(),
            CorrelationId = "c",
            Action = "a",
            EntityType = "E",
            Outcome = 0,
            TimestampUtc = DateTime.UtcNow,
            StartedAtUtc = DateTimeOffset.UtcNow,
            CompletedAtUtc = DateTimeOffset.UtcNow,
            // Field encrypted with a random throw-away key; key material is not supplied
            // to MapToReadModel so the shredded branch must fire.
            EncryptedUserId = EncryptedField.Encrypt(
                "secret",
                RandomNumberGenerator.GetBytes(32),
                "temporal:2026-03:v1"),
            TemporalKeyPeriod = "2026-03"
        };

        // Act
        var readModel = projection.MapToReadModel(evt, keyMaterial: null, isShredded: true);

        // Assert: the default placeholder propagates from the parameterless constructor
        projection.Name.ShouldBe("AuditEntryProjection");
        readModel.UserId.ShouldBe(MartenAuditOptions.DefaultShreddedPlaceholder);
        readModel.IsShredded.ShouldBeTrue();
    }

    [Fact]
    public void Constructor_WithPlaceholderAndLogger_SetsName()
    {
        var projection = CreateProjection("<HIDDEN>");
        projection.Name.ShouldBe("AuditEntryProjection");
    }

    [Fact]
    public void Constructor_NullPlaceholder_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new AuditEntryProjection(null!, NullLogger<AuditEntryProjection>.Instance));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new AuditEntryProjection("[SHREDDED]", null!));
    }

    [Fact]
    public void MapToReadModel_WithValidKey_DecryptsPiiFields()
    {
        // Arrange
        var keyMaterial = new byte[32];
        System.Security.Cryptography.RandomNumberGenerator.Fill(keyMaterial);
        var keyId = "temporal:2026-03:v1";

        var projection = CreateProjection();
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
        var readModel = projection.MapToReadModel(evt, keyMaterial, isShredded: false);

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
    public void MapToReadModel_WithShreddedKey_ReturnsShreddedPlaceholder()
    {
        // Arrange
        var dummyKey = new byte[32];
        System.Security.Cryptography.RandomNumberGenerator.Fill(dummyKey);
        var encrypted = EncryptedField.Encrypt("secret", dummyKey, "temporal:2099-12:v1");

        var projection = CreateProjection();
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

        // Act: pass null key material and isShredded=true as the Create method would
        var readModel = projection.MapToReadModel(evt, keyMaterial: null, isShredded: true);

        // Assert
        readModel.IsShredded.ShouldBeTrue();
        readModel.UserId.ShouldBe("[SHREDDED]");
        readModel.Outcome.ShouldBe(AuditOutcome.Failure);
        readModel.ErrorMessage.ShouldBe("denied");
    }

    [Fact]
    public void MapToReadModel_WithNullEncryptedFields_ReturnsModelWithNulls()
    {
        // Arrange
        var keyMaterial = new byte[32];
        System.Security.Cryptography.RandomNumberGenerator.Fill(keyMaterial);

        var projection = CreateProjection();
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
        var readModel = projection.MapToReadModel(evt, keyMaterial, isShredded: false);

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
    public void ClassifyTemporalKeyLookup_WithActiveKey_ReturnsKeyMaterialAndNotShredded()
    {
        // Arrange
        var keyMaterial = RandomNumberGenerator.GetBytes(32);
        var activeKey = new TemporalKeyDocument
        {
            Id = "temporal:2026-03:v2",
            Period = "2026-03",
            KeyMaterial = keyMaterial,
            Version = 2,
            Status = TemporalKeyStatus.Active,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        // Act
        var (resultKey, isShredded) = AuditEntryProjection.ClassifyTemporalKeyLookup(
            activeKey, destroyedMarker: null, "2026-03");

        // Assert
        resultKey.ShouldBeSameAs(keyMaterial);
        isShredded.ShouldBeFalse();
    }

    [Fact]
    public void ClassifyTemporalKeyLookup_WithNoActiveKeyButDestroyedMarker_ReturnsShredded()
    {
        // Arrange: simulates a period that was explicitly crypto-shredded via DestroyKeysBeforeAsync.
        var destroyedMarker = new TemporalKeyDestroyedMarker
        {
            Id = "temporal-destroyed:2020-01",
            Period = "2020-01",
            DestroyedAtUtc = DateTimeOffset.UtcNow,
            KeyVersionsDestroyed = 3
        };

        // Act
        var (resultKey, isShredded) = AuditEntryProjection.ClassifyTemporalKeyLookup(
            activeKey: null, destroyedMarker, "2020-01");

        // Assert
        resultKey.ShouldBeNull();
        isShredded.ShouldBeTrue();
    }

    [Fact]
    public void ClassifyTemporalKeyLookup_WithNeitherActiveKeyNorDestroyedMarker_ThrowsKeyNotFound()
    {
        // Arrange: simulates transient inconsistency — no active key written yet AND no
        // destroyed marker. This MUST throw so the Marten async daemon retries on the next
        // pass instead of persisting a read model with IsShredded=true (which would advance
        // the high-water mark and permanently corrupt the read model).
        // Act + Assert
        var ex = Should.Throw<KeyNotFoundException>(() =>
            AuditEntryProjection.ClassifyTemporalKeyLookup(
                activeKey: null, destroyedMarker: null, "2026-03"));

        ex.Message.ShouldContain("2026-03");
        ex.Message.ShouldContain("transient inconsistency");
        ex.Message.ShouldContain("retry");
    }

    [Fact]
    public void ClassifyTemporalKeyLookup_ActiveKey_TakesPrecedenceOverDestroyedMarker()
    {
        // Arrange: defensive — if both exist (e.g., a new key was issued after a prior
        // shredding cycle for a future period), the active key wins. The current projection
        // logic only fetches the destroyed marker when the active-key lookup came up empty,
        // so this path is not reachable from LoadTemporalKeyAsync, but ClassifyTemporalKeyLookup
        // is still defensively correct when called with both arguments non-null.
        var keyMaterial = RandomNumberGenerator.GetBytes(32);
        var activeKey = new TemporalKeyDocument
        {
            Id = "temporal:2026-03:v1",
            Period = "2026-03",
            KeyMaterial = keyMaterial,
            Version = 1,
            Status = TemporalKeyStatus.Active,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        var destroyedMarker = new TemporalKeyDestroyedMarker
        {
            Id = "temporal-destroyed:2026-03",
            Period = "2026-03",
            DestroyedAtUtc = DateTimeOffset.UtcNow,
            KeyVersionsDestroyed = 1
        };

        // Act
        var (resultKey, isShredded) = AuditEntryProjection.ClassifyTemporalKeyLookup(
            activeKey, destroyedMarker, "2026-03");

        // Assert
        resultKey.ShouldBeSameAs(keyMaterial);
        isShredded.ShouldBeFalse();
    }

    [Fact]
    public void MapToReadModel_UsesCustomShreddedPlaceholder()
    {
        // Arrange
        var dummyKey = new byte[32];
        System.Security.Cryptography.RandomNumberGenerator.Fill(dummyKey);
        var encrypted = EncryptedField.Encrypt("secret", dummyKey, "temporal:1999-01:v1");

        var projection = CreateProjection("<REDACTED>");
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
        var readModel = projection.MapToReadModel(evt, keyMaterial: null, isShredded: true);

        // Assert
        readModel.UserId.ShouldBe("<REDACTED>");
        readModel.IsShredded.ShouldBeTrue();
    }
}
