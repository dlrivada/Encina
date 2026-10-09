using Encina.ContractTests.Messaging.DeadLetter;
using Encina.Messaging.DeadLetter;
using Encina.MongoDB;
using Encina.MongoDB.DeadLetter;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;

namespace Encina.IntegrationTests.Infrastructure.MongoDB.Stores;

/// <summary>
/// Runs the dead letter store contract against <see cref="DeadLetterStoreMongoDB"/> on a real MongoDB
/// via Testcontainers.
/// </summary>
[Collection(MongoDbCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "MongoDB")]
public sealed class DeadLetterStoreMongoDBIntegrationTests : DeadLetterStoreContract
{
    private readonly MongoDbFixture _fixture;
    private readonly IOptions<EncinaMongoDbOptions> _options;

    public DeadLetterStoreMongoDBIntegrationTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
        _options = Options.Create(new EncinaMongoDbOptions
        {
            DatabaseName = MongoDbFixture.DatabaseName,
            UseDeadLetterQueue = true
        });
    }

    protected override async Task<IDeadLetterStore> CreateStoreAsync(TimeProvider timeProvider)
    {
        var collection = _fixture.Database!.GetCollection<DeadLetterMessage>(_options.Value.Collections.DeadLetterMessages);
        await collection.DeleteManyAsync(Builders<DeadLetterMessage>.Filter.Empty);

        // The unique source key is what makes a capture idempotent; the index creator hosted service is not
        // running in this test, so the index is created here with the same name and keys.
        var keys = Builders<DeadLetterMessage>.IndexKeys;
        await collection.Indexes.CreateOneAsync(new CreateIndexModel<DeadLetterMessage>(
            keys.Ascending(m => m.SourcePattern).Ascending(m => m.SourceMessageId),
            new CreateIndexOptions { Name = "UX_DeadLetterMessages_Source", Unique = true }));

        return NewStore(timeProvider);
    }

    protected override IDeadLetterStore CreateSecondStore() => NewStore(Clock);

    protected override IDeadLetterMessage CreateMessage(DeadLetterData data)
        => new DeadLetterMessageFactory().Create(data);

    private DeadLetterStoreMongoDB NewStore(TimeProvider timeProvider)
        => new(_fixture.Client!, _options, NullLogger<DeadLetterStoreMongoDB>.Instance, timeProvider);
}
