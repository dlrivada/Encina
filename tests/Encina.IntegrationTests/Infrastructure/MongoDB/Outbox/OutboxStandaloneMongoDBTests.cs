using Encina.Messaging.Transactions;
using Encina.MongoDB;
using Encina.TestInfrastructure.Fixtures;
using Encina.Testing.Shouldly;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Shouldly;
using Xunit;
using static Encina.IntegrationTests.Infrastructure.MongoDB.Outbox.OutboxAtomicityMongoDB;

namespace Encina.IntegrationTests.Infrastructure.MongoDB.Outbox;

/// <summary>
/// On a standalone MongoDB server (no replica set) a request with notifications fails closed (#718, ADR-050):
/// <c>transaction.not_supported</c> is returned before any transaction starts and nothing is written.
/// </summary>
[Collection(MongoDbCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "MongoDB")]
public sealed class OutboxStandaloneMongoDBTests : IAsyncLifetime
{
    private readonly MongoDbFixture _fixture;

    public OutboxStandaloneMongoDBTests(MongoDbFixture fixture) => _fixture = fixture;

    public async ValueTask InitializeAsync() => await _fixture.ClearAllDataAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task RequestWithNotifications_ReturnsNotSupportedAndWritesNothing()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncina();
        services.AddScoped<IRequestHandler<PlaceOrder, int>, PlaceOrderHandler>();
        services.AddEncinaMongoDB(options =>
        {
            options.ConnectionString = _fixture.ConnectionString;
            options.DatabaseName = MongoDbFixture.DatabaseName;
            options.UseOutbox = true;
        });
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        await using var scope = provider.CreateAsyncScope();

        var result = await scope.ServiceProvider.GetRequiredService<IEncina>().Send(new PlaceOrder(Guid.NewGuid(), 2, Mode.Commit));

        // The cluster type check rejects it before StartTransaction, so the handler never ran.
        result.ShouldBeErrorWithCode(TransactionErrors.NotSupportedCode);
        var database = _fixture.Database!;
        (await database.GetCollection<BsonDocument>(BusinessCollection).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).ShouldBe(0L);
        (await database.GetCollection<BsonDocument>("outbox_messages").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).ShouldBe(0L);
    }
}
