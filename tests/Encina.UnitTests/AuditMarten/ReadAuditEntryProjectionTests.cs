using Encina.Audit.Marten;
using Encina.Audit.Marten.Crypto;
using Encina.Audit.Marten.Events;
using Encina.Audit.Marten.Projections;
using Encina.Security.Audit;

using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.UnitTests.AuditMarten;

/// <summary>
/// Unit tests for <see cref="ReadAuditEntryProjection"/> read-audit event projection logic.
/// </summary>
/// <remarks>
/// These tests exercise the internal mapping helper (<c>MapToReadModel</c>) directly. The
/// full <c>Create(IDocumentOperations, ...)</c> path is covered by integration tests that
/// boot a real Marten store.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Provider", "Marten")]
public sealed class ReadAuditEntryProjectionTests
{
    private static ReadAuditEntryProjection CreateProjection(string placeholder = "[SHREDDED]") =>
        new(placeholder, NullLogger<ReadAuditEntryProjection>.Instance);

    [Fact]
    public void Constructor_Parameterless_SetsName()
    {
        var projection = new ReadAuditEntryProjection();
        projection.Name.ShouldBe("ReadAuditEntryProjection");
    }

    [Fact]
    public void Constructor_WithPlaceholderAndLogger_SetsName()
    {
        var projection = CreateProjection("<HIDDEN>");
        projection.Name.ShouldBe("ReadAuditEntryProjection");
    }

    [Fact]
    public void Constructor_NullPlaceholder_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ReadAuditEntryProjection(null!, NullLogger<ReadAuditEntryProjection>.Instance));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ReadAuditEntryProjection("[SHREDDED]", null!));
    }

    [Fact]
    public void MapToReadModel_WithValidKey_DecryptsPiiFields()
    {
        // Arrange
        var keyMaterial = new byte[32];
        System.Security.Cryptography.RandomNumberGenerator.Fill(keyMaterial);
        var keyId = "temporal:2026-03:v1";

        var projection = CreateProjection();
        var evt = new ReadAuditEntryRecordedEvent
        {
            Id = Guid.NewGuid(),
            EntityType = "Patient",
            EntityId = "patient-42",
            AccessedAtUtc = DateTimeOffset.UtcNow,
            AccessMethod = (int)ReadAccessMethod.Repository,
            EntityCount = 1,
            CorrelationId = "corr-r",
            TenantId = "hospital-1",
            EncryptedUserId = EncryptedField.Encrypt("doctor@hospital", keyMaterial, keyId),
            EncryptedPurpose = EncryptedField.Encrypt("treatment review", keyMaterial, keyId),
            EncryptedMetadata = EncryptedField.Encrypt("{\"dept\":\"oncology\"}", keyMaterial, keyId),
            TemporalKeyPeriod = "2026-03"
        };

        // Act
        var readModel = projection.MapToReadModel(evt, keyMaterial, isShredded: false);

        // Assert
        readModel.ShouldNotBeNull();
        readModel.Id.ShouldBe(evt.Id);
        readModel.EntityType.ShouldBe("Patient");
        readModel.EntityId.ShouldBe("patient-42");
        readModel.AccessMethod.ShouldBe(ReadAccessMethod.Repository);
        readModel.EntityCount.ShouldBe(1);
        readModel.CorrelationId.ShouldBe("corr-r");
        readModel.TenantId.ShouldBe("hospital-1");
        readModel.UserId.ShouldBe("doctor@hospital");
        readModel.Purpose.ShouldBe("treatment review");
        readModel.MetadataJson.ShouldBe("{\"dept\":\"oncology\"}");
        readModel.IsShredded.ShouldBeFalse();
        readModel.TemporalKeyPeriod.ShouldBe("2026-03");
    }

    [Fact]
    public void MapToReadModel_WithShreddedKey_ReturnsShreddedPlaceholder()
    {
        // Arrange
        var dummyKey = new byte[32];
        System.Security.Cryptography.RandomNumberGenerator.Fill(dummyKey);
        var encrypted = EncryptedField.Encrypt("user", dummyKey, "temporal:2099-12:v1");

        var projection = CreateProjection();
        var evt = new ReadAuditEntryRecordedEvent
        {
            Id = Guid.NewGuid(),
            EntityType = "Record",
            EntityId = null,
            AccessedAtUtc = DateTimeOffset.UtcNow,
            AccessMethod = (int)ReadAccessMethod.DirectQuery,
            EntityCount = 0,
            CorrelationId = null,
            TenantId = null,
            EncryptedUserId = encrypted,
            EncryptedPurpose = null,
            EncryptedMetadata = null,
            TemporalKeyPeriod = "2099-12"
        };

        // Act
        var readModel = projection.MapToReadModel(evt, keyMaterial: null, isShredded: true);

        // Assert
        readModel.IsShredded.ShouldBeTrue();
        readModel.UserId.ShouldBe("[SHREDDED]");
        readModel.AccessMethod.ShouldBe(ReadAccessMethod.DirectQuery);
    }

    [Fact]
    public void MapToReadModel_WithNullEncryptedFields_ReturnsModelWithNulls()
    {
        // Arrange
        var keyMaterial = new byte[32];
        System.Security.Cryptography.RandomNumberGenerator.Fill(keyMaterial);

        var projection = CreateProjection();
        var evt = new ReadAuditEntryRecordedEvent
        {
            Id = Guid.NewGuid(),
            EntityType = "Log",
            EntityId = null,
            AccessedAtUtc = DateTimeOffset.UtcNow,
            AccessMethod = (int)ReadAccessMethod.Export,
            EntityCount = 10,
            EncryptedUserId = null,
            EncryptedPurpose = null,
            EncryptedMetadata = null,
            TemporalKeyPeriod = "2026-03"
        };

        // Act
        var readModel = projection.MapToReadModel(evt, keyMaterial, isShredded: false);

        // Assert
        readModel.UserId.ShouldBeNull();
        readModel.Purpose.ShouldBeNull();
        readModel.MetadataJson.ShouldBeNull();
        readModel.IsShredded.ShouldBeFalse();
        readModel.EntityCount.ShouldBe(10);
        readModel.AccessMethod.ShouldBe(ReadAccessMethod.Export);
    }
}
