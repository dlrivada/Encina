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
/// Unit tests for the query paths of <see cref="MartenOperationAuditStore"/>: the extracted filter helpers
/// run against in-memory data and the store methods run against a fake Marten queryable (#1557).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Provider", "Marten")]
public sealed class MartenOperationAuditStoreQueryTests
{
    private const string Sentinel = "SENTINEL-CONNECTION-STRING-9f3";
    private static readonly DateTime Base = new(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc);

    private static OperationAuditEntryReadModel Model(
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
        return new OperationAuditEntryReadModel
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

    private static List<OperationAuditEntryReadModel> Corpus() =>
    [
        Model(user: "alice", at: Base),
        Model(user: "bob", entityType: "Invoice", entityId: "e2", at: Base.AddHours(1), correlation: "c2", action: "Delete", outcome: AuditOutcome.Failure, tenant: "t2", ip: "10.0.0.2", durationSeconds: 30),
        Model(user: "alice", entityType: "Order", entityId: "e3", at: Base.AddHours(2), correlation: "c3", durationSeconds: 5)
    ];

    private static (MartenOperationAuditStore Store, FakeLogger<MartenOperationAuditStore> Logger, IDocumentSession Session) CreateStore()
    {
        var session = Substitute.For<IDocumentSession>();
        var keyProvider = new InMemoryTemporalKeyProvider(TimeProvider.System, NullLogger<InMemoryTemporalKeyProvider>.Instance);
        var options = Options.Create(new MartenOperationAuditOptions());
        var encryptor = new AuditEventEncryptor(keyProvider, options, NullLogger<AuditEventEncryptor>.Instance);
        var logger = new FakeLogger<MartenOperationAuditStore>();
        return (new MartenOperationAuditStore(session, encryptor, keyProvider, options, logger), logger, session);
    }

    private static IQueryable<OperationAuditEntryReadModel> Filter(OperationAuditQuery query)
        => MartenOperationAuditStore.ApplyQueryFilters(Corpus().AsQueryable(), query);

    [Fact]
    public void ApplyQueryFilters_NoFilters_ReturnsEverything()
        => Filter(new OperationAuditQuery()).Count().ShouldBe(3);

    [Fact]
    public void ApplyQueryFilters_ByUserId_KeepsOnlyThatUser()
        => Filter(new OperationAuditQuery { UserId = "alice" }).Select(m => m.UserId).ShouldAllBe(u => u == "alice");

    [Fact]
    public void ApplyQueryFilters_ByTenantId_KeepsOnlyThatTenant()
        => Filter(new OperationAuditQuery { TenantId = "t2" }).Single().UserId.ShouldBe("bob");

    [Fact]
    public void ApplyQueryFilters_ByEntityType_KeepsOnlyThatType()
        => Filter(new OperationAuditQuery { EntityType = "Invoice" }).Single().UserId.ShouldBe("bob");

    [Fact]
    public void ApplyQueryFilters_ByEntityId_KeepsOnlyThatEntity()
        => Filter(new OperationAuditQuery { EntityId = "e3" }).Single().CorrelationId.ShouldBe("c3");

    [Fact]
    public void ApplyQueryFilters_ByAction_KeepsOnlyThatAction()
        => Filter(new OperationAuditQuery { Action = "Delete" }).Single().UserId.ShouldBe("bob");

    [Fact]
    public void ApplyQueryFilters_ByOutcome_KeepsOnlyThatOutcome()
        => Filter(new OperationAuditQuery { Outcome = AuditOutcome.Failure }).Single().UserId.ShouldBe("bob");

    [Fact]
    public void ApplyQueryFilters_ByCorrelationId_KeepsOnlyThatCorrelation()
        => Filter(new OperationAuditQuery { CorrelationId = "c2" }).Single().UserId.ShouldBe("bob");

    [Fact]
    public void ApplyQueryFilters_ByFromUtc_KeepsEntriesAtOrAfter()
        => Filter(new OperationAuditQuery { FromUtc = Base.AddHours(1) }).Count().ShouldBe(2);

    [Fact]
    public void ApplyQueryFilters_ByToUtc_KeepsEntriesAtOrBefore()
        => Filter(new OperationAuditQuery { ToUtc = Base.AddHours(1) }).Count().ShouldBe(2);

    [Fact]
    public void ApplyQueryFilters_ByIpAddress_KeepsOnlyThatAddress()
        => Filter(new OperationAuditQuery { IpAddress = "10.0.0.2" }).Single().UserId.ShouldBe("bob");

    [Fact]
    public void ApplyQueryFilters_WhitespaceStrings_AreIgnored()
        => Filter(new OperationAuditQuery { UserId = " ", TenantId = "", EntityType = " ", EntityId = " ", Action = " ", CorrelationId = " ", IpAddress = " " })
            .Count().ShouldBe(3);

    [Fact]
    public void ApplyQueryFilters_CombinedFilters_AreAnded()
        => Filter(new OperationAuditQuery { UserId = "alice", EntityType = "Order", FromUtc = Base.AddHours(1) })
            .Single().EntityId.ShouldBe("e3");

    [Fact]
    public void ApplyUserFilter_OnlyUser_FiltersByUser()
        => MartenOperationAuditStore.ApplyUserFilter(Corpus().AsQueryable(), "alice", null, null).Count().ShouldBe(2);

    [Fact]
    public void ApplyUserFilter_WithRange_AppliesBothBounds()
        => MartenOperationAuditStore.ApplyUserFilter(Corpus().AsQueryable(), "alice", Base.AddMinutes(30), Base.AddHours(3))
            .Single().EntityId.ShouldBe("e3");

    [Fact]
    public void HasDurationFilter_ReportsEitherBound()
    {
        MartenOperationAuditStore.HasDurationFilter(new OperationAuditQuery()).ShouldBeFalse();
        MartenOperationAuditStore.HasDurationFilter(new OperationAuditQuery { MinDuration = TimeSpan.FromSeconds(1) }).ShouldBeTrue();
        MartenOperationAuditStore.HasDurationFilter(new OperationAuditQuery { MaxDuration = TimeSpan.FromSeconds(1) }).ShouldBeTrue();
    }

    [Fact]
    public void FilterByDurationAndPage_MinAndMax_FilterThenPage()
    {
        var query = new OperationAuditQuery { MinDuration = TimeSpan.FromSeconds(2), MaxDuration = TimeSpan.FromSeconds(40) };

        var (total, page) = MartenOperationAuditStore.FilterByDurationAndPage(Corpus(), query, 1, 1);

        total.ShouldBe(2);
        page.Count.ShouldBe(1);
    }

    [Fact]
    public void FilterByDurationAndPage_OnlyMax_SecondPageHoldsRemainder()
    {
        var query = new OperationAuditQuery { MaxDuration = TimeSpan.FromSeconds(10) };

        var (total, page) = MartenOperationAuditStore.FilterByDurationAndPage(Corpus(), query, 2, 1);

        total.ShouldBe(2);
        page.Count.ShouldBe(1);
    }

    [Fact]
    public void FilterByDurationAndPage_NoBounds_ReturnsAll()
    {
        var (total, page) = MartenOperationAuditStore.FilterByDurationAndPage(Corpus(), new OperationAuditQuery(), 1, 10);

        total.ShouldBe(3);
        page.Count.ShouldBe(3);
    }

    [Fact]
    public async Task QueryAsync_WhenSessionFails_ReturnsLeftAndLogsRedactedException()
    {
        var (store, logger, session) = CreateStore();
        session.Query<OperationAuditEntryReadModel>().Throws(new InvalidOperationException(Sentinel));

        var result = await store.QueryAsync(new OperationAuditQuery(), TestContext.Current.CancellationToken);

        result.IsLeft.ShouldBeTrue();
        RedactedExceptionLogAssert.LoggedOnlyRedacted(logger, Sentinel);
    }

    [Fact]
    public async Task GetByUserAsync_WhenSessionFails_ReturnsLeftAndLogsRedactedException()
    {
        var (store, logger, session) = CreateStore();
        session.Query<OperationAuditEntryReadModel>().Throws(new InvalidOperationException(Sentinel));

        var result = await store.GetByUserAsync("alice", null, null, TestContext.Current.CancellationToken);

        result.IsLeft.ShouldBeTrue();
        RedactedExceptionLogAssert.LoggedOnlyRedacted(logger, Sentinel);
    }
}
