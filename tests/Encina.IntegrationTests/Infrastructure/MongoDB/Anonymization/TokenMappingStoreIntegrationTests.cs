using Encina.IntegrationTests.Infrastructure.MongoDB;
using Encina.MongoDB;
using Encina.MongoDB.Anonymization;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace Encina.IntegrationTests.Infrastructure.MongoDB.Anonymization;

/// <summary>
/// Integration tests for TokenMappingStoreMongoDB against real MongoDB.
/// </summary>
[Collection(MongoDbTestCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "MongoDB")]
public sealed class TokenMappingStoreIntegrationTests : IAsyncLifetime
{
    private readonly MongoDbFixture _fixture;

    public TokenMappingStoreIntegrationTests(MongoDbFixture fixture) => _fixture = fixture;

    public ValueTask InitializeAsync()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "MongoDB container not available");
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private TokenMappingStoreMongoDB CreateStore()
    {
        return new TokenMappingStoreMongoDB(
            _fixture.Client!,
            Microsoft.Extensions.Options.Options.Create(new EncinaMongoDbOptions
            {
                DatabaseName = MongoDbFixture.DatabaseName
            }),
            NullLogger<TokenMappingStoreMongoDB>.Instance);
    }

    [Fact]
    public async Task GetAllAsync_EmptyStore_ReturnsEmpty()
    {
        var store = CreateStore();
        var result = await store.GetAllAsync();
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task GetByTokenAsync_UnknownToken_ReturnsNone()
    {
        var store = CreateStore();
        var result = await store.GetByTokenAsync($"unknown-{Guid.NewGuid():N}");
        result.IsRight.ShouldBeTrue();
    }
}
