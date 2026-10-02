using Encina.MongoDB;
using Encina.MongoDB.Auditing;
using Encina.Security.Audit;
using Encina.UnitTests.Support;

using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;

using MongoDB.Driver;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Encina.UnitTests.MongoDB.Auditing;

/// <summary>
/// Unit tests for the query paths of <see cref="AuditStoreMongoDB"/> against a mocked collection:
/// paging, the in-memory duration filter and the redacted failure logging (#1557).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Provider", "MongoDB")]
public sealed class AuditStoreMongoDBQueryTests
{
    private const string Sentinel = "SENTINEL-MONGO-URI-5e2";
    private static readonly DateTime Now = new(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc);

    private readonly IMongoCollection<AuditEntryDocument> _collection =
        Substitute.For<IMongoCollection<AuditEntryDocument>>();

    private readonly FakeLogger<AuditStoreMongoDB> _logger = new();

    private AuditStoreMongoDB CreateSut()
    {
        var client = Substitute.For<IMongoClient>();
        var database = Substitute.For<IMongoDatabase>();
        client.GetDatabase(Arg.Any<string>(), Arg.Any<MongoDatabaseSettings>()).Returns(database);
        database.GetCollection<AuditEntryDocument>(Arg.Any<string>(), Arg.Any<MongoCollectionSettings>())
            .Returns(_collection);
        return new AuditStoreMongoDB(client, Options.Create(new EncinaMongoDbOptions()), _logger);
    }

    private static AuditEntryDocument Document(int durationSeconds) => new()
    {
        Id = Guid.NewGuid(),
        CorrelationId = "corr",
        UserId = "user-1",
        Action = "Read",
        EntityType = "Patient",
        TimestampUtc = Now,
        StartedAtUtc = Now,
        CompletedAtUtc = Now.AddSeconds(durationSeconds),
    };

    private void ReturnDocuments(params AuditEntryDocument[] documents)
    {
        var cursor = Substitute.For<IAsyncCursor<AuditEntryDocument>>();
        var consumed = false;
        cursor.Current.Returns(_ => documents);
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            var hasData = !consumed && documents.Length > 0;
            consumed = true;
            return Task.FromResult(hasData);
        });

        _collection.FindAsync(
                Arg.Any<FilterDefinition<AuditEntryDocument>>(),
                Arg.Any<FindOptions<AuditEntryDocument, AuditEntryDocument>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(cursor));
    }

    private void FailFind() =>
        _collection.FindAsync(
                Arg.Any<FilterDefinition<AuditEntryDocument>>(),
                Arg.Any<FindOptions<AuditEntryDocument, AuditEntryDocument>>(),
                Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException(Sentinel));

    private void ReturnCount(long count) =>
        _collection.CountDocumentsAsync(
                Arg.Any<FilterDefinition<AuditEntryDocument>>(),
                Arg.Any<CountOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(count));

    [Fact]
    public async Task QueryAsync_NoDurationFilter_ReturnsPagedEntries()
    {
        ReturnCount(2);
        ReturnDocuments(Document(1), Document(30));

        var result = await CreateSut().QueryAsync(new AuditQuery { PageNumber = 0, PageSize = 10 });

        var page = result.Match(r => r, e => throw new ShouldAssertException(e.Message));
        page.Items.Count.ShouldBe(2);
        page.TotalCount.ShouldBe(2);
        page.PageNumber.ShouldBe(1);
    }

    [Fact]
    public async Task QueryAsync_MinDuration_DropsShorterEntries()
    {
        ReturnCount(2);
        ReturnDocuments(Document(1), Document(30));

        var result = await CreateSut().QueryAsync(new AuditQuery { MinDuration = TimeSpan.FromSeconds(10) });

        var page = result.Match(r => r, e => throw new ShouldAssertException(e.Message));
        page.Items.Count.ShouldBe(1);
        page.Items[0].Duration.ShouldBe(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public async Task QueryAsync_MaxDuration_DropsLongerEntries()
    {
        ReturnCount(2);
        ReturnDocuments(Document(1), Document(30));

        var result = await CreateSut().QueryAsync(new AuditQuery { MaxDuration = TimeSpan.FromSeconds(10) });

        var page = result.Match(r => r, e => throw new ShouldAssertException(e.Message));
        page.Items.Count.ShouldBe(1);
        page.Items[0].Duration.ShouldBe(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task QueryAsync_MinAndMaxDuration_KeepsOnlyEntriesInRange()
    {
        ReturnCount(3);
        ReturnDocuments(Document(1), Document(20), Document(60));

        var result = await CreateSut().QueryAsync(new AuditQuery
        {
            MinDuration = TimeSpan.FromSeconds(10),
            MaxDuration = TimeSpan.FromSeconds(30),
        });

        var page = result.Match(r => r, e => throw new ShouldAssertException(e.Message));
        page.Items.Count.ShouldBe(1);
    }

    [Fact]
    public async Task QueryAsync_StoreFails_ReturnsLeftAndLogsRedactedException()
    {
        ReturnCount(1);
        FailFind();

        var result = await CreateSut().QueryAsync(new AuditQuery());

        result.IsLeft.ShouldBeTrue();
        RedactedExceptionLogAssert.LoggedOnlyRedacted(_logger, Sentinel);
    }

    [Fact]
    public async Task GetByUserAsync_WithoutBounds_ReturnsEntries()
    {
        ReturnDocuments(Document(1));

        var result = await CreateSut().GetByUserAsync("user-1", null, null);

        var entries = result.Match(r => r, e => throw new ShouldAssertException(e.Message));
        entries.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetByUserAsync_WithBounds_ReturnsEntries()
    {
        ReturnDocuments(Document(1), Document(2));

        var result = await CreateSut().GetByUserAsync("user-1", Now.AddDays(-1), Now.AddDays(1));

        var entries = result.Match(r => r, e => throw new ShouldAssertException(e.Message));
        entries.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetByUserAsync_StoreFails_ReturnsLeftAndLogsRedactedException()
    {
        FailFind();

        var result = await CreateSut().GetByUserAsync("user-1", Now, null);

        result.IsLeft.ShouldBeTrue();
        RedactedExceptionLogAssert.LoggedOnlyRedacted(_logger, Sentinel);
    }
}
