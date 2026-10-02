using Encina.Audit.Marten;
using Encina.Audit.Marten.Crypto;
using Encina.Audit.Marten.Projections;
using Encina.Security.Audit;
using Encina.UnitTests.Support;

using Marten;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Encina.UnitTests.AuditMarten;

/// <summary>
/// Unit tests for the query paths of <see cref="MartenAuditStore"/>: the extracted filter helpers
/// run against in-memory data and the store methods run against a fake Marten queryable (#1557).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Provider", "Marten")]
public sealed class MartenAuditStoreQueryTests
{
    private const string Sentinel = "SENTINEL-CONNECTION-STRING-9f3";
    private static readonly DateTime Base = new(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc);

    private static AuditEntryReadModel Model(
        string user = "u1",
        string entityType = "Order",
        string? entityId = "e1",
        DateTime? at = null,
        double durationSeconds = 1,
        string correlation = "c1",
        string action = "Create",
        AuditOutcome outcome = AuditOutcome.Success,
        string? tenant = "t1",
        string? ip = "10.0.0.1")
    {
        var timestamp = at ?? Base;
        return new AuditEntryReadModel
        {
            Id = Guid.NewGuid(),
            UserId = user,
            EntityType = entityType,
            EntityId = entityId,
            TimestampUtc = timestamp,
            StartedAtUtc = new DateTimeOffset(timestamp, TimeSpan.Zero),
            CompletedAtUtc = new DateTimeOffset(timestamp, TimeSpan.Zero).AddSeconds(durationSeconds),
            CorrelationId = correlation,
            Action = action,
            Outcome = outcome,
            TenantId = tenant,
            IpAddress = ip
        };
    }

    private static List<AuditEntryReadModel> Corpus() =>
    [
        Model(user: "alice", at: Base),
        Model(user: "bob", entityType: "Invoice", entityId: "e2", at: Base.AddHours(1), correlation: "c2", action: "Delete", outcome: AuditOutcome.Failure, tenant: "t2", ip: "10.0.0.2", durationSeconds: 30),
        Model(user: "alice", entityType: "Order", entityId: "e3", at: Base.AddHours(2), correlation: "c3", durationSeconds: 5)
    ];

    private static (MartenAuditStore Store, FakeLogger<MartenAuditStore> Logger, IDocumentSession Session) CreateStore()
    {
        var session = Substitute.For<IDocumentSession>();
        var keyProvider = new InMemoryTemporalKeyProvider(TimeProvider.System, NullLogger<InMemoryTemporalKeyProvider>.Instance);
        var options = Options.Create(new MartenAuditOptions());
        var encryptor = new AuditEventEncryptor(keyProvider, options, NullLogger<AuditEventEncryptor>.Instance);
        var logger = new FakeLogger<MartenAuditStore>();
        return (new MartenAuditStore(session, encryptor, keyProvider, options, logger), logger, session);
    }

    private static IQueryable<AuditEntryReadModel> Filter(AuditQuery query)
        => MartenAuditStore.ApplyQueryFilters(Corpus().AsQueryable(), query);

    [Fact]
    public void ApplyQueryFilters_NoFilters_ReturnsEverything()
        => Filter(new AuditQuery()).Count().ShouldBe(3);

    [Fact]
    public void ApplyQueryFilters_ByUserId_KeepsOnlyThatUser()
        => Filter(new AuditQuery { UserId = "alice" }).Select(m => m.UserId).ShouldAllBe(u => u == "alice");

    [Fact]
    public void ApplyQueryFilters_ByTenantId_KeepsOnlyThatTenant()
        => Filter(new AuditQuery { TenantId = "t2" }).Single().UserId.ShouldBe("bob");

    [Fact]
    public void ApplyQueryFilters_ByEntityType_KeepsOnlyThatType()
        => Filter(new AuditQuery { EntityType = "Invoice" }).Single().UserId.ShouldBe("bob");

    [Fact]
    public void ApplyQueryFilters_ByEntityId_KeepsOnlyThatEntity()
        => Filter(new AuditQuery { EntityId = "e3" }).Single().CorrelationId.ShouldBe("c3");

    [Fact]
    public void ApplyQueryFilters_ByAction_KeepsOnlyThatAction()
        => Filter(new AuditQuery { Action = "Delete" }).Single().UserId.ShouldBe("bob");

    [Fact]
    public void ApplyQueryFilters_ByOutcome_KeepsOnlyThatOutcome()
        => Filter(new AuditQuery { Outcome = AuditOutcome.Failure }).Single().UserId.ShouldBe("bob");

    [Fact]
    public void ApplyQueryFilters_ByCorrelationId_KeepsOnlyThatCorrelation()
        => Filter(new AuditQuery { CorrelationId = "c2" }).Single().UserId.ShouldBe("bob");

    [Fact]
    public void ApplyQueryFilters_ByFromUtc_KeepsEntriesAtOrAfter()
        => Filter(new AuditQuery { FromUtc = Base.AddHours(1) }).Count().ShouldBe(2);

    [Fact]
    public void ApplyQueryFilters_ByToUtc_KeepsEntriesAtOrBefore()
        => Filter(new AuditQuery { ToUtc = Base.AddHours(1) }).Count().ShouldBe(2);

    [Fact]
    public void ApplyQueryFilters_ByIpAddress_KeepsOnlyThatAddress()
        => Filter(new AuditQuery { IpAddress = "10.0.0.2" }).Single().UserId.ShouldBe("bob");

    [Fact]
    public void ApplyQueryFilters_WhitespaceStrings_AreIgnored()
        => Filter(new AuditQuery { UserId = " ", TenantId = "", EntityType = " ", EntityId = " ", Action = " ", CorrelationId = " ", IpAddress = " " })
            .Count().ShouldBe(3);

    [Fact]
    public void ApplyQueryFilters_CombinedFilters_AreAnded()
        => Filter(new AuditQuery { UserId = "alice", EntityType = "Order", FromUtc = Base.AddHours(1) })
            .Single().EntityId.ShouldBe("e3");

    [Fact]
    public void ApplyUserFilter_OnlyUser_FiltersByUser()
        => MartenAuditStore.ApplyUserFilter(Corpus().AsQueryable(), "alice", null, null).Count().ShouldBe(2);

    [Fact]
    public void ApplyUserFilter_WithRange_AppliesBothBounds()
        => MartenAuditStore.ApplyUserFilter(Corpus().AsQueryable(), "alice", Base.AddMinutes(30), Base.AddHours(3))
            .Single().EntityId.ShouldBe("e3");

    [Fact]
    public void HasDurationFilter_ReportsEitherBound()
    {
        MartenAuditStore.HasDurationFilter(new AuditQuery()).ShouldBeFalse();
        MartenAuditStore.HasDurationFilter(new AuditQuery { MinDuration = TimeSpan.FromSeconds(1) }).ShouldBeTrue();
        MartenAuditStore.HasDurationFilter(new AuditQuery { MaxDuration = TimeSpan.FromSeconds(1) }).ShouldBeTrue();
    }

    [Fact]
    public void FilterByDurationAndPage_MinAndMax_FilterThenPage()
    {
        var query = new AuditQuery { MinDuration = TimeSpan.FromSeconds(2), MaxDuration = TimeSpan.FromSeconds(40) };

        var (total, page) = MartenAuditStore.FilterByDurationAndPage(Corpus(), query, 1, 1);

        total.ShouldBe(2);
        page.Count.ShouldBe(1);
    }

    [Fact]
    public void FilterByDurationAndPage_OnlyMax_SecondPageHoldsRemainder()
    {
        var query = new AuditQuery { MaxDuration = TimeSpan.FromSeconds(10) };

        var (total, page) = MartenAuditStore.FilterByDurationAndPage(Corpus(), query, 2, 1);

        total.ShouldBe(2);
        page.Count.ShouldBe(1);
    }

    [Fact]
    public void FilterByDurationAndPage_NoBounds_ReturnsAll()
    {
        var (total, page) = MartenAuditStore.FilterByDurationAndPage(Corpus(), new AuditQuery(), 1, 10);

        total.ShouldBe(3);
        page.Count.ShouldBe(3);
    }

    [Fact]
    public async Task QueryAsync_WhenSessionFails_ReturnsLeftAndLogsRedactedException()
    {
        var (store, logger, session) = CreateStore();
        session.Query<AuditEntryReadModel>().Throws(new InvalidOperationException(Sentinel));

        var result = await store.QueryAsync(new AuditQuery(), TestContext.Current.CancellationToken);

        result.IsLeft.ShouldBeTrue();
        RedactedExceptionLogAssert.LoggedOnlyRedacted(logger, Sentinel);
    }

    [Fact]
    public async Task GetByUserAsync_WhenSessionFails_ReturnsLeftAndLogsRedactedException()
    {
        var (store, logger, session) = CreateStore();
        session.Query<AuditEntryReadModel>().Throws(new InvalidOperationException(Sentinel));

        var result = await store.GetByUserAsync("alice", null, null, TestContext.Current.CancellationToken);

        result.IsLeft.ShouldBeTrue();
        RedactedExceptionLogAssert.LoggedOnlyRedacted(logger, Sentinel);
    }
}
