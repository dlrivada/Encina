using Encina.MongoDB;
using Encina.MongoDB.Auditing;
using Encina.MongoDB.Inbox;
using Encina.MongoDB.Outbox;
using Encina.MongoDB.Sagas;
using Encina.MongoDB.Scheduling;
using Encina.UnitTests.Support;

using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;

using MongoDB.Driver;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Encina.UnitTests.MongoDB;

/// <summary>
/// Unit tests for <see cref="MongoDbIndexCreator"/>: each enabled feature creates its indexes,
/// disabled features create none and a failure is logged redacted and rethrown (#1557).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Provider", "MongoDB")]
public sealed class MongoDbIndexCreatorTests
{
    private const string Sentinel = "SENTINEL-INDEX-HOST-3b8";

    private readonly IMongoClient _client = Substitute.For<IMongoClient>();
    private readonly IMongoDatabase _database = Substitute.For<IMongoDatabase>();
    private readonly FakeLogger<MongoDbIndexCreator> _logger = new();

    public MongoDbIndexCreatorTests()
    {
        _client.GetDatabase(Arg.Any<string>(), Arg.Any<MongoDatabaseSettings>()).Returns(_database);
    }

    private MongoDbIndexCreator CreateSut(EncinaMongoDbOptions options) =>
        new(_client, Options.Create(options), _logger);

    private IMongoIndexManager<T> Indexes<T>()
    {
        var collection = Substitute.For<IMongoCollection<T>>();
        var indexes = Substitute.For<IMongoIndexManager<T>>();
        collection.Indexes.Returns(indexes);
        _database.GetCollection<T>(Arg.Any<string>(), Arg.Any<MongoCollectionSettings>()).Returns(collection);
        return indexes;
    }

    [Fact]
    public async Task StartAsync_AllFeaturesEnabled_CreatesIndexesForEveryCollection()
    {
        var outbox = Indexes<OutboxMessage>();
        var inbox = Indexes<InboxMessage>();
        var sagas = Indexes<SagaState>();
        var scheduled = Indexes<ScheduledMessage>();
        var audit = Indexes<AuditLogDocument>();
        var sut = CreateSut(new EncinaMongoDbOptions
        {
            UseOutbox = true,
            UseInbox = true,
            UseSagas = true,
            UseScheduling = true,
            UseAuditLogStore = true,
        });

        await sut.StartAsync(CancellationToken.None);

        await outbox.Received(1).CreateManyAsync(Arg.Any<IEnumerable<CreateIndexModel<OutboxMessage>>>(), Arg.Any<CancellationToken>());
        await inbox.Received(1).CreateManyAsync(Arg.Any<IEnumerable<CreateIndexModel<InboxMessage>>>(), Arg.Any<CancellationToken>());
        await sagas.Received(1).CreateManyAsync(Arg.Any<IEnumerable<CreateIndexModel<SagaState>>>(), Arg.Any<CancellationToken>());
        await scheduled.Received(1).CreateManyAsync(Arg.Any<IEnumerable<CreateIndexModel<ScheduledMessage>>>(), Arg.Any<CancellationToken>());
        await audit.Received(1).CreateManyAsync(Arg.Any<IEnumerable<CreateIndexModel<AuditLogDocument>>>(), Arg.Any<CancellationToken>());
        _logger.Collector.GetSnapshot().Any(r => r.Exception is not null).ShouldBeFalse();
    }

    [Fact]
    public async Task StartAsync_AllFeaturesDisabled_CreatesNoIndexes()
    {
        var outbox = Indexes<OutboxMessage>();
        var sut = CreateSut(new EncinaMongoDbOptions
        {
            UseOutbox = false,
            UseInbox = false,
            UseSagas = false,
            UseScheduling = false,
            UseAuditLogStore = false,
        });

        await sut.StartAsync(CancellationToken.None);

        await outbox.DidNotReceiveWithAnyArgs().CreateManyAsync(
            Arg.Any<IEnumerable<CreateIndexModel<OutboxMessage>>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_IndexCreationFails_LogsRedactedExceptionAndRethrows()
    {
        var outbox = Indexes<OutboxMessage>();
        outbox.CreateManyAsync(Arg.Any<IEnumerable<CreateIndexModel<OutboxMessage>>>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException(Sentinel));
        var sut = CreateSut(new EncinaMongoDbOptions { UseOutbox = true });

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => sut.StartAsync(CancellationToken.None));

        ex.Message.ShouldBe(Sentinel);
        RedactedExceptionLogAssert.LoggedOnlyRedacted(_logger, Sentinel);
    }

    [Fact]
    public async Task StopAsync_Completes()
    {
        await CreateSut(new EncinaMongoDbOptions()).StopAsync(CancellationToken.None);
    }
}
