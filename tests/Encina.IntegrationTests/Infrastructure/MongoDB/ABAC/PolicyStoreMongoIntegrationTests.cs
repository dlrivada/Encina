using Encina.IntegrationTests.Infrastructure.MongoDB;
using Encina.MongoDB;
using Encina.MongoDB.ABAC;
using Encina.TestInfrastructure.Fixtures;
using Shouldly;

namespace Encina.IntegrationTests.Infrastructure.MongoDB.ABAC;

/// <summary>
/// Integration tests for PolicyStoreMongo against real MongoDB.
/// </summary>
[Collection(MongoDbTestCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "MongoDB")]
public sealed class PolicyStoreMongoIntegrationTests : IAsyncLifetime
{
    private readonly MongoDbFixture _fixture;

    public PolicyStoreMongoIntegrationTests(MongoDbFixture fixture) => _fixture = fixture;

    public ValueTask InitializeAsync()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "MongoDB container not available");
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private PolicyStoreMongo CreateStore()
    {
        return new PolicyStoreMongo(
            _fixture.Client!,
            Microsoft.Extensions.Options.Options.Create(new EncinaMongoDbOptions
            {
                DatabaseName = MongoDbFixture.DatabaseName
            }));
    }

    [Fact]
    public async Task GetPolicySetsAsync_EmptyStore_ReturnsEmpty()
    {
        var store = CreateStore();
        var result = await store.GetAllPolicySetsAsync();
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task GetPoliciesAsync_EmptyStore_ReturnsEmpty()
    {
        var store = CreateStore();
        var result = await store.GetAllStandalonePoliciesAsync();
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task GetPolicySetCountAsync_EmptyStore_ReturnsZero()
    {
        var store = CreateStore();
        var result = await store.GetPolicySetCountAsync();
        result.IsRight.ShouldBeTrue();
    }
}
