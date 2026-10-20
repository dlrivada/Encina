using Encina.IntegrationTests.Messaging.DeadLetter;
using Encina.MongoDB;
using Encina.MongoDB.DeadLetter;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Encina.IntegrationTests.Infrastructure.MongoDB.Stores;

/// <summary>
/// The terminal failure of each source is dead-lettered once through the MongoDB registration on a real
/// MongoDB (#1991).
/// </summary>
[Collection(MongoDbCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "MongoDB")]
public sealed class DeadLetterSourcesEndToEndMongoDBTests : IAsyncLifetime
{
    private readonly MongoDbFixture _fixture;

    public DeadLetterSourcesEndToEndMongoDBTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        var collections = new EncinaMongoDbOptions().Collections;
        foreach (var name in new[] { collections.Outbox, collections.Inbox, collections.Sagas, collections.ScheduledMessages })
        {
            await _fixture.Database!.GetCollection<BsonDocument>(name).DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);
        }

        var deadLetters = _fixture.Database!.GetCollection<DeadLetterMessage>(collections.DeadLetterMessages);
        await deadLetters.DeleteManyAsync(Builders<DeadLetterMessage>.Filter.Empty);
        await deadLetters.Indexes.CreateOneAsync(new CreateIndexModel<DeadLetterMessage>(
            Builders<DeadLetterMessage>.IndexKeys.Ascending(m => m.SourcePattern).Ascending(m => m.SourceMessageId),
            new CreateIndexOptions { Name = "UX_DeadLetterMessages_Source", Unique = true }));
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Theory]
    [MemberData(nameof(DeadLetterSourcesEndToEndScenario.Sources), MemberType = typeof(DeadLetterSourcesEndToEndScenario))]
    public async Task Source_TerminalFailure_PersistsOneDeadLetter(string source)
    {
        var (services, clock) = DeadLetterSourcesEndToEndScenario.NewServices();
        services.AddEncinaMongoDB(options =>
        {
            options.ConnectionString = _fixture.ConnectionString;
            options.DatabaseName = MongoDbFixture.DatabaseName;
            options.UseOutbox = true;
            options.UseInbox = true;
            options.UseSagas = true;
            options.UseScheduling = true;
            options.UseDeadLetterQueue = true;
            DeadLetterSourcesEndToEndScenario.Tune(options.OutboxOptions, options.InboxOptions, options.SchedulingOptions);
        });

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        await DeadLetterSourcesEndToEndScenario.RunAsync(source, provider, clock);
    }
}
