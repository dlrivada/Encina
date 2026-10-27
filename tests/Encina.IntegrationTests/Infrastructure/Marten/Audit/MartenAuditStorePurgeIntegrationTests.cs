using Encina.Audit.Marten;
using Encina.Audit.Marten.Crypto;
using Encina.IntegrationTests.Infrastructure.Marten.Fixtures;
using Encina.Security.Audit;

using Marten;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Shouldly;

namespace Encina.IntegrationTests.Infrastructure.Marten.Audit;

/// <summary>
/// Retention purge of the Marten operation and read audit stores on real PostgreSQL: the purge destroys the
/// temporal keys of the elapsed periods (crypto-shredding) and reports how many periods it destroyed.
/// </summary>
[Collection(MartenCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
public sealed class MartenAuditStorePurgeIntegrationTests(MartenFixture fixture) : IAsyncLifetime
{
    private static readonly DateTime Old = new(2020, 1, 15, 8, 0, 0, DateTimeKind.Utc);

    private readonly IOptions<MartenOperationAuditOptions> _options = Options.Create(new MartenOperationAuditOptions());
    private DocumentStore? _store;

    public async ValueTask InitializeAsync()
    {
        if (!fixture.IsAvailable)
        {
            return;
        }

        _store = DocumentStore.For(options =>
        {
            options.Connection(fixture.ConnectionString);
            options.DatabaseSchemaName = "audit_purge";
            options.Events.StreamIdentity = JasperFx.Events.StreamIdentity.AsString;
        });

        await _store.Advanced.Clean.CompletelyRemoveAllAsync();
        await _store.Storage.ApplyAllConfiguredChangesToDatabaseAsync();
    }

    public ValueTask DisposeAsync()
    {
        _store?.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task OperationStore_PurgeEntriesAsync_DestroysTheKeysOfTheElapsedPeriods()
    {
        SkipWhenUnavailable();
        var (operationStore, _) = CreateStores();
        (await operationStore.RecordAsync(Entry())).IsRight.ShouldBeTrue();

        var result = await operationStore.PurgeEntriesAsync(Old.AddYears(1));

        DestroyedCount(result).ShouldBeGreaterThan(0);
        await AssertRetentionMarkerAsync(Old.AddYears(1));
    }

    [Fact]
    public async Task ReadStore_PurgeEntriesAsync_DestroysTheKeysOfTheElapsedPeriods()
    {
        SkipWhenUnavailable();
        var (operationStore, readStore) = CreateStores();
        (await operationStore.RecordAsync(Entry())).IsRight.ShouldBeTrue();

        var result = await readStore.PurgeEntriesAsync(Old.AddYears(1));

        DestroyedCount(result).ShouldBeGreaterThan(0);
        await AssertRetentionMarkerAsync(Old.AddYears(1));
    }

    /// <summary>
    /// The purge records a retention reference (policy, cutoff, execution time, actor system) and no erasure
    /// reference, so the projections read the destroyed period as <c>[EXPIRED]</c> and keep running (issue #2770, Q3).
    /// </summary>
    private async Task AssertRetentionMarkerAsync(DateTime cutoffUtc)
    {
        await using var query = _store!.QuerySession();
        var marker = await query.LoadAsync<TemporalKeyDestroyedMarker>(TemporalPeriodHelper.FormatDestroyedMarkerId("2020-01"));

        marker.ShouldNotBeNull();
        marker.Classify().ShouldBe(KeyDestructionKind.Retention);
        marker.HasErasureReference().ShouldBeFalse();
        marker.RetentionPolicyId.ShouldBe("audit-retention:P2555D");
        marker.RetentionActor.ShouldBe("system");
        marker.RetentionCutoffUtc.ShouldBe(new DateTimeOffset(cutoffUtc, TimeSpan.Zero));
        marker.RetentionExecutedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public async Task OperationStore_PurgeEntriesAsync_NothingOlderThanTheCutoff_DestroysNothing()
    {
        SkipWhenUnavailable();
        var (operationStore, _) = CreateStores();
        (await operationStore.RecordAsync(Entry())).IsRight.ShouldBeTrue();

        var result = await operationStore.PurgeEntriesAsync(Old.AddYears(-1));

        DestroyedCount(result).ShouldBe(0);
    }

    private static int DestroyedCount(LanguageExt.Either<EncinaError, int> result) =>
        result.Match(
            Right: count => count,
            Left: error => throw new ShouldAssertException($"Purge failed: {error.GetCode().IfNone("none")}"));

    private void SkipWhenUnavailable()
    {
        if (!fixture.IsAvailable)
        {
            Assert.Skip("PostgreSQL through Testcontainers is not available.");
        }
    }

    private (MartenOperationAuditStore Operation, MartenReadAuditStore Read) CreateStores()
    {
        var session = _store!.LightweightSession();
        var keys = new MartenTemporalKeyProvider(session, TimeProvider.System, NullLogger<MartenTemporalKeyProvider>.Instance);
        var encryptor = new AuditEventEncryptor(keys, _options, NullLogger<AuditEventEncryptor>.Instance);

        return (
            new MartenOperationAuditStore(session, encryptor, keys, _options, NullLogger<MartenOperationAuditStore>.Instance),
            new MartenReadAuditStore(session, encryptor, keys, _options, NullLogger<MartenReadAuditStore>.Instance));
    }

    private static OperationAuditEntry Entry() => new()
    {
        Id = Guid.NewGuid(),
        CorrelationId = "corr-purge",
        UserId = "user-purge",
        Action = "Create",
        EntityType = "Order",
        EntityId = "order-purge",
        Outcome = AuditOutcome.Success,
        TimestampUtc = Old,
        StartedAtUtc = new DateTimeOffset(Old, TimeSpan.Zero),
        CompletedAtUtc = new DateTimeOffset(Old, TimeSpan.Zero).AddMilliseconds(5)
    };
}
