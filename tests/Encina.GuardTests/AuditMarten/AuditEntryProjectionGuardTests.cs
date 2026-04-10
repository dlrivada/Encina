using Encina.Audit.Marten;
using Encina.Audit.Marten.Crypto;
using Encina.Audit.Marten.Events;
using Encina.Audit.Marten.Projections;
using Encina.Security.Audit;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace Encina.GuardTests.AuditMarten;

/// <summary>
/// Guard tests for <see cref="AuditEntryProjection"/> and <see cref="ReadAuditEntryProjection"/>.
/// Ensures the projection types can be constructed and invoked with a real service provider.
/// </summary>
public class AuditEntryProjectionGuardTests
{
    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ITemporalKeyProvider>(new InMemoryTemporalKeyProvider(
            new FakeTimeProvider(new DateTimeOffset(2026, 3, 15, 12, 0, 0, TimeSpan.Zero)),
            NullLogger<InMemoryTemporalKeyProvider>.Instance));
        services.Configure<MartenAuditOptions>(_ => { });
        services.AddLogging();
        return services.BuildServiceProvider();
    }

    [Fact]
    public void AuditEntryProjection_Constructor_DoesNotThrow()
    {
        var projection = new AuditEntryProjection();
        projection.ShouldNotBeNull();
        projection.Name.ShouldBe("AuditEntryProjection");
    }

    [Fact]
    public void ReadAuditEntryProjection_Constructor_DoesNotThrow()
    {
        var projection = new ReadAuditEntryProjection();
        projection.ShouldNotBeNull();
        projection.Name.ShouldBe("ReadAuditEntryProjection");
    }

    [Fact]
    public async Task AuditEntryProjection_Create_MissingKey_ReturnsShredded()
    {
        var projection = new AuditEntryProjection();
        var services = BuildServiceProvider();

        var dummyKey = new byte[32];
        System.Security.Cryptography.RandomNumberGenerator.Fill(dummyKey);
        var encrypted = EncryptedField.Encrypt("data", dummyKey, "temporal:9999-12:v1");

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
            EncryptedUserId = encrypted,
            TemporalKeyPeriod = "9999-12"
        };

        var result = await projection.Create(evt, services);

        result.IsShredded.ShouldBeTrue();
        result.UserId.ShouldBe("[SHREDDED]");
    }

    [Fact]
    public async Task ReadAuditEntryProjection_Create_MissingKey_ReturnsShredded()
    {
        var projection = new ReadAuditEntryProjection();
        var services = BuildServiceProvider();

        var dummyKey = new byte[32];
        System.Security.Cryptography.RandomNumberGenerator.Fill(dummyKey);
        var encrypted = EncryptedField.Encrypt("data", dummyKey, "temporal:9999-12:v1");

        var evt = new ReadAuditEntryRecordedEvent
        {
            Id = Guid.NewGuid(),
            EntityType = "E",
            EntityId = null,
            AccessedAtUtc = DateTimeOffset.UtcNow,
            AccessMethod = (int)ReadAccessMethod.Repository,
            EntityCount = 0,
            EncryptedUserId = encrypted,
            TemporalKeyPeriod = "9999-12"
        };

        var result = await projection.Create(evt, services);

        result.IsShredded.ShouldBeTrue();
        result.UserId.ShouldBe("[SHREDDED]");
    }
}
