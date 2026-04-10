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
/// Unit tests for <see cref="ReadAuditEntryProjection"/> read-audit event projection logic.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Provider", "Marten")]
public sealed class ReadAuditEntryProjectionTests
{
    private static ServiceProvider BuildServices(InMemoryTemporalKeyProvider keyProvider, MartenAuditOptions? options = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ITemporalKeyProvider>(keyProvider);
        services.Configure<MartenAuditOptions>(o =>
        {
            var cfg = options ?? new MartenAuditOptions();
            o.ShreddedPlaceholder = cfg.ShreddedPlaceholder;
        });
        services.AddLogging();
        return services.BuildServiceProvider();
    }

    private static InMemoryTemporalKeyProvider CreateKeyProvider() =>
        new(new FakeTimeProvider(new DateTimeOffset(2026, 3, 15, 12, 0, 0, TimeSpan.Zero)),
            NullLogger<InMemoryTemporalKeyProvider>.Instance);

    [Fact]
    public void Constructor_SetsName()
    {
        var projection = new ReadAuditEntryProjection();
        projection.Name.ShouldBe("ReadAuditEntryProjection");
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
        var projection = new ReadAuditEntryProjection();

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
        var readModel = await projection.Create(evt, services);

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
    public async Task Create_WithMissingKey_ReturnsShreddedModel()
    {
        // Arrange
        var keyProvider = CreateKeyProvider();
        var services = BuildServices(keyProvider);
        var projection = new ReadAuditEntryProjection();

        var dummyKey = new byte[32];
        System.Security.Cryptography.RandomNumberGenerator.Fill(dummyKey);
        var encrypted = EncryptedField.Encrypt("user", dummyKey, "temporal:2099-12:v1");

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
        var readModel = await projection.Create(evt, services);

        // Assert
        readModel.IsShredded.ShouldBeTrue();
        readModel.UserId.ShouldBe("[SHREDDED]");
        readModel.AccessMethod.ShouldBe(ReadAccessMethod.DirectQuery);
    }

    [Fact]
    public async Task Create_WithNullEncryptedFields_ReturnsModelWithNulls()
    {
        // Arrange
        var keyProvider = CreateKeyProvider();
        await keyProvider.GetOrCreateKeyAsync("2026-03");
        var services = BuildServices(keyProvider);
        var projection = new ReadAuditEntryProjection();

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
        var readModel = await projection.Create(evt, services);

        // Assert
        readModel.UserId.ShouldBeNull();
        readModel.Purpose.ShouldBeNull();
        readModel.MetadataJson.ShouldBeNull();
        readModel.IsShredded.ShouldBeFalse();
        readModel.EntityCount.ShouldBe(10);
        readModel.AccessMethod.ShouldBe(ReadAccessMethod.Export);
    }
}
