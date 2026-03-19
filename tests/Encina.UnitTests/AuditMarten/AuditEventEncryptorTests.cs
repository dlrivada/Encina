using Encina.Audit.Marten;
using Encina.Audit.Marten.Crypto;
using Encina.Audit.Marten.Events;
using Encina.Security.Audit;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.AuditMarten;

/// <summary>
/// Unit tests for <see cref="AuditEventEncryptor"/> mapping and encryption logic.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Provider", "Marten")]
public sealed class AuditEventEncryptorTests
{
    private readonly InMemoryTemporalKeyProvider _keyProvider;
    private readonly AuditEventEncryptor _sut;

    public AuditEventEncryptorTests()
    {
        _keyProvider = new InMemoryTemporalKeyProvider(
            TimeProvider.System,
            NullLogger<InMemoryTemporalKeyProvider>.Instance);

        var options = Options.Create(new MartenAuditOptions
        {
            TemporalGranularity = TemporalKeyGranularity.Monthly
        });

        _sut = new AuditEventEncryptor(
            _keyProvider,
            options,
            NullLogger<AuditEventEncryptor>.Instance);
    }

    [Fact]
    public async Task EncryptAuditEntryAsync_ValidEntry_ReturnsEncryptedEvent()
    {
        // Arrange
        var entry = CreateAuditEntry();

        // Act
        var result = await _sut.EncryptAuditEntryAsync(entry);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(evt =>
        {
            // Structural fields should be plaintext
            evt.Id.ShouldBe(entry.Id);
            evt.Action.ShouldBe(entry.Action);
            evt.EntityType.ShouldBe(entry.EntityType);
            evt.EntityId.ShouldBe(entry.EntityId);
            evt.Outcome.ShouldBe((int)entry.Outcome);
            evt.CorrelationId.ShouldBe(entry.CorrelationId);
            evt.TimestampUtc.ShouldBe(entry.TimestampUtc);
            evt.TenantId.ShouldBe(entry.TenantId);

            // PII fields should be encrypted
            evt.EncryptedUserId.ShouldNotBeNull();
            evt.EncryptedUserId!.IsEncrypted.ShouldBeTrue();
            evt.EncryptedIpAddress.ShouldNotBeNull();
            evt.EncryptedIpAddress!.IsEncrypted.ShouldBeTrue();

            // Period should be set
            evt.TemporalKeyPeriod.ShouldNotBeNullOrWhiteSpace();
        });
    }

    [Fact]
    public async Task EncryptAuditEntryAsync_NullPiiFields_SetsEncryptedFieldsToNull()
    {
        // Arrange
        var entry = new AuditEntry
        {
            Id = Guid.NewGuid(),
            CorrelationId = "corr-1",
            Action = "Create",
            EntityType = "Order",
            Outcome = AuditOutcome.Success,
            TimestampUtc = DateTime.UtcNow,
            StartedAtUtc = DateTimeOffset.UtcNow.AddMilliseconds(-100),
            CompletedAtUtc = DateTimeOffset.UtcNow,
            // All PII fields null by default
        };

        // Act
        var result = await _sut.EncryptAuditEntryAsync(entry);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(evt =>
        {
            evt.EncryptedUserId.ShouldBeNull();
            evt.EncryptedIpAddress.ShouldBeNull();
            evt.EncryptedUserAgent.ShouldBeNull();
            evt.EncryptedRequestPayload.ShouldBeNull();
            evt.EncryptedResponsePayload.ShouldBeNull();
            evt.EncryptedMetadata.ShouldBeNull();
        });
    }

    [Fact]
    public async Task EncryptReadAuditEntryAsync_ValidEntry_ReturnsEncryptedEvent()
    {
        // Arrange
        var entry = CreateReadAuditEntry();

        // Act
        var result = await _sut.EncryptReadAuditEntryAsync(entry);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(evt =>
        {
            evt.Id.ShouldBe(entry.Id);
            evt.EntityType.ShouldBe(entry.EntityType);
            evt.EntityId.ShouldBe(entry.EntityId);
            evt.AccessMethod.ShouldBe((int)entry.AccessMethod);
            evt.EntityCount.ShouldBe(entry.EntityCount);

            // PII encrypted
            evt.EncryptedUserId.ShouldNotBeNull();
            evt.EncryptedUserId!.IsEncrypted.ShouldBeTrue();
            evt.EncryptedPurpose.ShouldNotBeNull();
            evt.EncryptedPurpose!.IsEncrypted.ShouldBeTrue();
        });
    }

    [Fact]
    public async Task EncryptAuditEntryAsync_KeyProviderFails_ReturnsLeft()
    {
        // Arrange
        var failingProvider = Substitute.For<ITemporalKeyProvider>();
        Either<EncinaError, TemporalKeyInfo> failureResult = Left(EncinaError.New("Key provider unavailable"));
#pragma warning disable CA2012 // Use ValueTasks correctly - NSubstitute .Returns() pattern
        failingProvider.GetOrCreateKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, TemporalKeyInfo>>(failureResult));
#pragma warning restore CA2012

        var options = Options.Create(new MartenAuditOptions());
        var sut = new AuditEventEncryptor(
            failingProvider,
            options,
            NullLogger<AuditEventEncryptor>.Instance);

        var entry = CreateAuditEntry();

        // Act
        var result = await sut.EncryptAuditEntryAsync(entry);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task EncryptAuditEntryAsync_WithMetadata_EncryptsMetadataAsJson()
    {
        // Arrange
        var entry = new AuditEntry
        {
            Id = Guid.NewGuid(),
            CorrelationId = "corr-1",
            Action = "Create",
            EntityType = "Order",
            Outcome = AuditOutcome.Success,
            TimestampUtc = DateTime.UtcNow,
            StartedAtUtc = DateTimeOffset.UtcNow.AddMilliseconds(-100),
            CompletedAtUtc = DateTimeOffset.UtcNow,
            Metadata = new Dictionary<string, object?>
            {
                ["workflow"] = "approval",
                ["step"] = 3
            }
        };

        // Act
        var result = await _sut.EncryptAuditEntryAsync(entry);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(evt =>
        {
            evt.EncryptedMetadata.ShouldNotBeNull();
            evt.EncryptedMetadata!.IsEncrypted.ShouldBeTrue();
        });
    }

    private static AuditEntry CreateAuditEntry() => new()
    {
        Id = Guid.NewGuid(),
        CorrelationId = "test-correlation-id",
        UserId = "user-123",
        TenantId = "tenant-abc",
        Action = "Create",
        EntityType = "Order",
        EntityId = "ORD-456",
        Outcome = AuditOutcome.Success,
        TimestampUtc = DateTime.UtcNow,
        StartedAtUtc = DateTimeOffset.UtcNow.AddMilliseconds(-50),
        CompletedAtUtc = DateTimeOffset.UtcNow,
        IpAddress = "192.168.1.100",
        UserAgent = "Mozilla/5.0",
        RequestPayloadHash = "abc123"
    };

    private static ReadAuditEntry CreateReadAuditEntry() => new()
    {
        Id = Guid.NewGuid(),
        EntityType = "Patient",
        EntityId = "PAT-123",
        UserId = "user-456",
        TenantId = "tenant-abc",
        AccessedAtUtc = DateTimeOffset.UtcNow,
        CorrelationId = "corr-789",
        Purpose = "Patient care review",
        AccessMethod = ReadAccessMethod.Repository,
        EntityCount = 1
    };
}
