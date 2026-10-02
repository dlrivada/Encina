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
/// Unit tests for the query paths of <see cref="MartenReadAuditStore"/>: the extracted filter
/// helper runs against in-memory data and the failure path against a throwing session (#1557).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Provider", "Marten")]
public sealed class MartenReadAuditStoreQueryTests
{
    private const string Sentinel = "SENTINEL-CONNECTION-STRING-4c1";
    private static readonly DateTimeOffset Base = new(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);

    private static List<ReadAuditEntryReadModel> Corpus() =>
    [
        new()
        {
            Id = Guid.NewGuid(), UserId = "alice", TenantId = "t1", EntityType = "Patient", EntityId = "p1",
            AccessMethod = ReadAccessMethod.Api, Purpose = "Treatment review", CorrelationId = "c1", AccessedAtUtc = Base
        },
        new()
        {
            Id = Guid.NewGuid(), UserId = "bob", TenantId = "t2", EntityType = "Invoice", EntityId = "i1",
            AccessMethod = ReadAccessMethod.Export, Purpose = null, CorrelationId = "c2", AccessedAtUtc = Base.AddHours(1)
        },
        new()
        {
            Id = Guid.NewGuid(), UserId = "alice", TenantId = "t1", EntityType = "Patient", EntityId = "p2",
            AccessMethod = ReadAccessMethod.Api, Purpose = "Billing", CorrelationId = "c3", AccessedAtUtc = Base.AddHours(2)
        }
    ];

    private static IQueryable<ReadAuditEntryReadModel> Filter(ReadAuditQuery query)
        => MartenReadAuditStore.ApplyQueryFilters(Corpus().AsQueryable(), query);

    private static (MartenReadAuditStore Store, FakeLogger<MartenReadAuditStore> Logger, IDocumentSession Session) CreateStore()
    {
        var session = Substitute.For<IDocumentSession>();
        var keyProvider = new InMemoryTemporalKeyProvider(TimeProvider.System, NullLogger<InMemoryTemporalKeyProvider>.Instance);
        var options = Options.Create(new MartenAuditOptions());
        var encryptor = new AuditEventEncryptor(keyProvider, options, NullLogger<AuditEventEncryptor>.Instance);
        var logger = new FakeLogger<MartenReadAuditStore>();
        return (new MartenReadAuditStore(session, encryptor, keyProvider, options, logger), logger, session);
    }

    [Fact]
    public void ApplyQueryFilters_NoFilters_ReturnsEverything()
        => Filter(new ReadAuditQuery()).Count().ShouldBe(3);

    [Fact]
    public void ApplyQueryFilters_ByUserId_KeepsOnlyThatUser()
        => Filter(new ReadAuditQuery { UserId = "alice" }).Count().ShouldBe(2);

    [Fact]
    public void ApplyQueryFilters_ByTenantId_KeepsOnlyThatTenant()
        => Filter(new ReadAuditQuery { TenantId = "t2" }).Single().UserId.ShouldBe("bob");

    [Fact]
    public void ApplyQueryFilters_ByEntityType_KeepsOnlyThatType()
        => Filter(new ReadAuditQuery { EntityType = "Invoice" }).Single().UserId.ShouldBe("bob");

    [Fact]
    public void ApplyQueryFilters_ByEntityId_KeepsOnlyThatEntity()
        => Filter(new ReadAuditQuery { EntityId = "p2" }).Single().CorrelationId.ShouldBe("c3");

    [Fact]
    public void ApplyQueryFilters_ByAccessMethod_KeepsOnlyThatMethod()
        => Filter(new ReadAuditQuery { AccessMethod = ReadAccessMethod.Export }).Single().UserId.ShouldBe("bob");

    [Fact]
    public void ApplyQueryFilters_ByPurpose_MatchesSubstringAndSkipsNull()
        => Filter(new ReadAuditQuery { Purpose = "Treat" }).Single().EntityId.ShouldBe("p1");

    [Fact]
    public void ApplyQueryFilters_ByCorrelationId_KeepsOnlyThatCorrelation()
        => Filter(new ReadAuditQuery { CorrelationId = "c2" }).Single().UserId.ShouldBe("bob");

    [Fact]
    public void ApplyQueryFilters_ByFromUtc_KeepsEntriesAtOrAfter()
        => Filter(new ReadAuditQuery { FromUtc = Base.AddHours(1) }).Count().ShouldBe(2);

    [Fact]
    public void ApplyQueryFilters_ByToUtc_KeepsEntriesAtOrBefore()
        => Filter(new ReadAuditQuery { ToUtc = Base.AddHours(1) }).Count().ShouldBe(2);

    [Fact]
    public void ApplyQueryFilters_WhitespaceStrings_AreIgnored()
        => Filter(new ReadAuditQuery { UserId = " ", TenantId = "", EntityType = " ", EntityId = " ", Purpose = " ", CorrelationId = " " })
            .Count().ShouldBe(3);

    [Fact]
    public void ApplyQueryFilters_CombinedFilters_AreAnded()
        => Filter(new ReadAuditQuery { UserId = "alice", EntityType = "Patient", ToUtc = Base.AddHours(1) })
            .Single().EntityId.ShouldBe("p1");

    [Fact]
    public async Task QueryAsync_WhenSessionFails_ReturnsLeftAndLogsRedactedException()
    {
        var (store, logger, session) = CreateStore();
        session.Query<ReadAuditEntryReadModel>().Throws(new InvalidOperationException(Sentinel));

        var result = await store.QueryAsync(new ReadAuditQuery(), TestContext.Current.CancellationToken);

        result.IsLeft.ShouldBeTrue();
        RedactedExceptionLogAssert.LoggedOnlyRedacted(logger, Sentinel);
    }
}
