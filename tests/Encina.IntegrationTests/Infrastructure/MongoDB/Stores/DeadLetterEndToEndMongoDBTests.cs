using Encina.IntegrationTests.Messaging.DeadLetter;
using Encina.MongoDB;
using Encina.MongoDB.DeadLetter;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Encina.IntegrationTests.Infrastructure.MongoDB.Stores;

/// <summary>
/// Capture, replay and expiry through the MongoDB registration (<c>UseDeadLetterQueue = true</c>) on a real
/// MongoDB.
/// </summary>
[Collection(MongoDbCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "MongoDB")]
public sealed class DeadLetterEndToEndMongoDBTests
{
    private readonly MongoDbFixture _fixture;

    public DeadLetterEndToEndMongoDBTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task DeadLetterQueue_CapturesReplaysAndExpires_ThroughTheRegisteredServices()
    {
        var collection = _fixture.Database!.GetCollection<DeadLetterMessage>(new EncinaMongoDbOptions().Collections.DeadLetterMessages);
        await collection.DeleteManyAsync(Builders<DeadLetterMessage>.Filter.Empty);
        var keys = Builders<DeadLetterMessage>.IndexKeys;
        await collection.Indexes.CreateOneAsync(new CreateIndexModel<DeadLetterMessage>(
            keys.Ascending(m => m.SourcePattern).Ascending(m => m.SourceMessageId),
            new CreateIndexOptions { Name = "UX_DeadLetterMessages_Source", Unique = true }));

        var (services, clock, encina) = DeadLetterEndToEndScenario.NewServices();
        services.AddEncinaMongoDB(options =>
        {
            options.ConnectionString = _fixture.ConnectionString;
            options.DatabaseName = MongoDbFixture.DatabaseName;
            options.UseDeadLetterQueue = true;
        });

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        await DeadLetterEndToEndScenario.RunAsync(provider, clock, encina);
    }
}
