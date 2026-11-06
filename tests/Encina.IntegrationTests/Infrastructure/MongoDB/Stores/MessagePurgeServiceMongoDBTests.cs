using Encina.TestInfrastructure.Fixtures;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.MongoDB.Stores;

/// <summary>
/// Runs the hosted <c>MessagePurgeService</c> end to end over the MongoDB inbox store (#3046).
/// </summary>
[Collection(MongoDbCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "MongoDB")]
public sealed class MessagePurgeServiceMongoDBTests : IAsyncLifetime
{
    private readonly MongoDbFixture _fixture;

    public MessagePurgeServiceMongoDBTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync() => await _fixture.ClearAllDataAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public Task PurgeCycle_ExpiredAndLiveRows_RemovesOnlyTheProcessedExpiredRow() =>
        global::Encina.IntegrationTests.Messaging.MessagePurgeScenario.AssertCycleRemovesOnlyProcessedExpiredRowsAsync(
            global::Encina.IntegrationTests.Messaging.InboxPipelineHarnesses.Mongo(_fixture));
}
