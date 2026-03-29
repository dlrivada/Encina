using Encina.Audit.Marten;
using Encina.Audit.Marten.Crypto;
using Encina.IntegrationTests.Infrastructure.Marten.Fixtures;
using Encina.Security.Audit;

using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

namespace Encina.IntegrationTests.Infrastructure.Marten.Audit;

/// <summary>
/// Integration tests for <see cref="MartenAuditStore"/> using a real PostgreSQL instance.
/// </summary>
[Collection(MartenCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
public sealed class MartenAuditStoreIntegrationTests : IAsyncLifetime
{
    private readonly MartenFixture _fixture;

    public MartenAuditStoreIntegrationTests(MartenFixture fixture)
    {
        _fixture = fixture;
    }

    public ValueTask InitializeAsync()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "Marten PostgreSQL container not available");
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static (MartenAuditStore Store, InMemoryTemporalKeyProvider KeyProvider) CreateStore(global::Marten.IDocumentSession session)
    {
        var keyProvider = new InMemoryTemporalKeyProvider(
            TimeProvider.System, NullLogger<InMemoryTemporalKeyProvider>.Instance);
        var encryptor = new AuditEventEncryptor(keyProvider,
            Microsoft.Extensions.Options.Options.Create(new MartenAuditOptions()),
            NullLogger<AuditEventEncryptor>.Instance);
        var store = new MartenAuditStore(session, encryptor, keyProvider,
            Microsoft.Extensions.Options.Options.Create(new MartenAuditOptions()),
            NullLogger<MartenAuditStore>.Instance);
        return (store, keyProvider);
    }

    [Fact]
    public async Task RecordAsync_ValidEntry_ShouldPersist()
    {
        // Arrange
        await using var session = _fixture.Store!.LightweightSession();
        var (store, _) = CreateStore(session);

        var now = DateTimeOffset.UtcNow;
        var entry = new AuditEntry
        {
            Id = Guid.NewGuid(),
            Action = "Create",
            EntityType = "Order",
            EntityId = "order-123",
            UserId = "user-1",
            CorrelationId = Guid.NewGuid().ToString(),
            Outcome = AuditOutcome.Success,
            TimestampUtc = now.UtcDateTime,
            StartedAtUtc = now,
            CompletedAtUtc = now
        };

        // Act
        var result = await store.RecordAsync(entry);

        // Assert
        result.IsRight.ShouldBeTrue($"RecordAsync should succeed, got: {result}");
    }

    [Fact]
    public async Task RecordAsync_MultipleTimes_ShouldNotFail()
    {
        // Arrange
        await using var session = _fixture.Store!.LightweightSession();
        var (store, _) = CreateStore(session);

        // Act — record 3 entries
        for (int i = 0; i < 3; i++)
        {
            var ts = DateTimeOffset.UtcNow;
            var entry = new AuditEntry
            {
                Id = Guid.NewGuid(),
                Action = $"Action-{i}",
                EntityType = "Product",
                EntityId = $"product-{i}",
                UserId = "user-1",
                CorrelationId = Guid.NewGuid().ToString(),
                Outcome = AuditOutcome.Success,
                TimestampUtc = ts.UtcDateTime,
                StartedAtUtc = ts,
                CompletedAtUtc = ts
            };

            var result = await store.RecordAsync(entry);
            result.IsRight.ShouldBeTrue($"Record {i} should succeed");
        }
    }
}
