using Encina.Audit.Marten;
using Encina.Audit.Marten.Crypto;
using Encina.Audit.Marten.Projections;
using Encina.IntegrationTests.Infrastructure.Marten.Fixtures;
using Encina.IntegrationTests.Security.Audit;
using Encina.Security.Audit;

using JasperFx.Events.Daemon;

using Marten;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Shouldly;

namespace Encina.IntegrationTests.Infrastructure.Marten.Audit;

/// <summary>
/// Regression tests for #2132: filtering the Marten operation audit store by a UTC
/// <see cref="OperationAuditQuery.FromUtc"/> / <see cref="OperationAuditQuery.ToUtc"/> used to fail with
/// <c>audit.marten.query_failed</c> because the read model column was a <see cref="DateTime"/>
/// (<c>timestamp without time zone</c>) that Npgsql refuses for UTC values. Runs on real PostgreSQL.
/// </summary>
[Collection(MartenCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
public sealed class MartenOperationAuditStoreUtcFilterIntegrationTests(MartenFixture fixture) : IAsyncLifetime
{
    private static readonly DateTime Base = new(2026, 10, 9, 8, 0, 0, DateTimeKind.Utc);

    private readonly IOptions<MartenOperationAuditOptions> _options = Options.Create(new MartenOperationAuditOptions());
    private DocumentStore? _store;
    private IProjectionDaemon? _daemon;
    private string _user = string.Empty;

    public async ValueTask InitializeAsync()
    {
        if (!fixture.IsAvailable)
        {
            return;
        }

        _store = DocumentStore.For(options =>
        {
            options.Connection(fixture.ConnectionString);
            options.DatabaseSchemaName = "audit_utc_filter";
            options.Events.StreamIdentity = JasperFx.Events.StreamIdentity.AsString;
            new ConfigureMartenOperationAuditProjections(_options, NullLoggerFactory.Instance).Configure(options);
        });

        await _store.Advanced.Clean.CompletelyRemoveAllAsync();
        await _store.Storage.ApplyAllConfiguredChangesToDatabaseAsync();
        _daemon = await _store.BuildProjectionDaemonAsync();
        await _daemon.StartAllAsync();

        _user = $"user-{Guid.NewGuid():N}";
        var store = CreateStore();

        // Three entries one hour apart: Base, Base + 1h, Base + 2h.
        for (var hour = 0; hour < 3; hour++)
        {
            var at = Base.AddHours(hour);
            var recorded = await store.RecordAsync(Entry(at, hour));
            recorded.IsRight.ShouldBeTrue();
        }

        await _daemon.WaitForNonStaleData(TimeSpan.FromSeconds(60));
    }

    public async ValueTask DisposeAsync()
    {
        if (_daemon is not null)
        {
            await _daemon.StopAllAsync();
            _daemon.Dispose();
        }

        _store?.Dispose();
    }

    [Fact]
    public async Task QueryAsync_FromUtc_ReturnsEntriesAtOrAfterTheBound()
    {
        SkipWhenUnavailable();

        var page = await Query(new OperationAuditQuery { UserId = _user, FromUtc = Base.AddHours(1) });

        Hours(page).ShouldBe([2, 1]);
    }

    [Fact]
    public async Task QueryAsync_ToUtc_ReturnsEntriesAtOrBeforeTheBound()
    {
        SkipWhenUnavailable();

        var page = await Query(new OperationAuditQuery { UserId = _user, ToUtc = Base.AddHours(1) });

        Hours(page).ShouldBe([1, 0]);
    }

    [Fact]
    public async Task QueryAsync_FromUtcAndToUtc_ReturnsOnlyTheEntriesInsideTheRange()
    {
        SkipWhenUnavailable();

        var page = await Query(new OperationAuditQuery
        {
            UserId = _user,
            FromUtc = Base.AddMinutes(30),
            ToUtc = Base.AddMinutes(90)
        });

        Hours(page).ShouldBe([1]);
    }

    [Fact]
    public async Task QueryAsync_UnspecifiedKindBound_IsReadAsUtc()
    {
        SkipWhenUnavailable();

        var page = await Query(new OperationAuditQuery
        {
            UserId = _user,
            FromUtc = DateTime.SpecifyKind(Base.AddHours(2), DateTimeKind.Unspecified)
        });

        Hours(page).ShouldBe([2]);
    }

    [Fact]
    public async Task GetByUserAsync_UtcRange_ReturnsTheEntriesInsideTheRange()
    {
        SkipWhenUnavailable();

        var result = await CreateStore().GetByUserAsync(_user, Base.AddMinutes(30), Base.AddMinutes(150));

        var entries = result.Match(Right: e => e, Left: error => throw new ShouldAssertException($"Query failed: {error.GetCode().IfNone("none")}"));
        entries.Select(e => e.TimestampUtc.Hour - Base.Hour).ShouldBe([2, 1]);
    }

    [Fact]
    public async Task QueryAsync_ReturnedTimestamps_AreUtcAndEqualTheRecordedInstant()
    {
        SkipWhenUnavailable();

        var page = await Query(new OperationAuditQuery { UserId = _user, ToUtc = Base });

        var entry = page.Items.ShouldHaveSingleItem();
        entry.TimestampUtc.Kind.ShouldBe(DateTimeKind.Utc);
        entry.TimestampUtc.ShouldBe(Base);
    }

    [Fact]
    public async Task QueryAsync_EntriesSharingATimestamp_PagesRepeatAndDropNothing()
    {
        SkipWhenUnavailable();

        await OperationAuditStableOrderCheck.AssertPagedReadsAreStableAsync(
            CreateStore(),
            ReadAuditStableOrderCheck.HexIdOrder,
            () => _daemon!.WaitForNonStaleData(TimeSpan.FromSeconds(60)));
    }

    [Fact]
    public async Task QueryAsync_DurationFilter_AppliesTheTotalOrderBeforePaging()
    {
        SkipWhenUnavailable();

        // Entries recorded in SetUp last 5 ms each (see Entry); a 1 ms floor keeps them all and takes the in-memory duration path.
        var page = await Query(new OperationAuditQuery { UserId = _user, MinDuration = TimeSpan.FromMilliseconds(1), PageSize = 2 });

        page.TotalCount.ShouldBe(3);
        Hours(page).ShouldBe([2, 1]);
    }

    [Fact]
    public async Task ReadStore_QueryAsync_EntriesSharingATimestamp_PagesRepeatAndDropNothing()
    {
        SkipWhenUnavailable();

        var session = _store!.LightweightSession();
        var keys = new MartenTemporalKeyProvider(session, TimeProvider.System, NullLogger<MartenTemporalKeyProvider>.Instance);
        var readStore = new MartenReadAuditStore(
            session,
            new AuditEventEncryptor(keys, _options, NullLogger<AuditEventEncryptor>.Instance),
            keys,
            _options,
            NullLogger<MartenReadAuditStore>.Instance);

        await ReadAuditStableOrderCheck.AssertPagedReadsAreStableAsync(
            readStore,
            ReadAuditStableOrderCheck.HexIdOrder,
            () => _daemon!.WaitForNonStaleData(TimeSpan.FromSeconds(60)));
    }

    private void SkipWhenUnavailable()
    {
        if (!fixture.IsAvailable)
        {
            Assert.Skip("PostgreSQL through Testcontainers is not available.");
        }
    }

    private static IReadOnlyList<int> Hours(PagedResult<OperationAuditEntry> page) =>
        [.. page.Items.Select(e => e.TimestampUtc.Hour - Base.Hour)];

    private async Task<PagedResult<OperationAuditEntry>> Query(OperationAuditQuery query)
    {
        var result = await CreateStore().QueryAsync(query);
        return result.Match(Right: p => p, Left: error => throw new ShouldAssertException($"Query failed: {error.GetCode().IfNone("none")}"));
    }

    private MartenOperationAuditStore CreateStore()
    {
        var session = _store!.LightweightSession();
        var keys = new MartenTemporalKeyProvider(session, TimeProvider.System, NullLogger<MartenTemporalKeyProvider>.Instance);

        return new MartenOperationAuditStore(
            session,
            new AuditEventEncryptor(keys, _options, NullLogger<AuditEventEncryptor>.Instance),
            keys,
            _options,
            NullLogger<MartenOperationAuditStore>.Instance);
    }

    private OperationAuditEntry Entry(DateTime timestampUtc, int index) => new()
    {
        Id = Guid.NewGuid(),
        CorrelationId = $"corr-{index}",
        UserId = _user,
        Action = "Create",
        EntityType = "Order",
        EntityId = $"order-{index}",
        Outcome = AuditOutcome.Success,
        TimestampUtc = timestampUtc,
        StartedAtUtc = new DateTimeOffset(timestampUtc, TimeSpan.Zero),
        CompletedAtUtc = new DateTimeOffset(timestampUtc, TimeSpan.Zero).AddMilliseconds(5)
    };
}
