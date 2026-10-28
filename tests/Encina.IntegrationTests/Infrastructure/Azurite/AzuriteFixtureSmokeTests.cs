using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Encina.TestInfrastructure.Fixtures;

namespace Encina.IntegrationTests.Infrastructure.Azurite;

/// <summary>
/// Smoke tests proving the shared <see cref="AzuriteFixture"/> starts and that <c>ClearAllDataAsync</c> empties it.
/// No Encina package consumes Azurite yet; the later Azure issues (blob, queue and table features) do.
/// </summary>
[Collection(AzuriteFixture.CollectionName)]
[Trait("Category", "Integration")]
[Trait("Infrastructure", "Azurite")]
public sealed class AzuriteFixtureSmokeTests : IAsyncLifetime
{
    private readonly AzuriteFixture _fixture;

    public AzuriteFixtureSmokeTests(AzuriteFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync() => await _fixture.ClearAllDataAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public void Fixture_ExposesTheConnectionStringAndTheThreeEndpoints()
    {
        // Assert
        _fixture.IsAvailable.ShouldBeTrue();
        _fixture.ConnectionString.ShouldContain("BlobEndpoint=");
        _fixture.BlobEndpoint.ShouldStartWith("http://");
        _fixture.QueueEndpoint.ShouldStartWith("http://");
        _fixture.TableEndpoint.ShouldStartWith("http://");
    }

    [Fact]
    public async Task ClearAllDataAsync_AfterWritingABlobAQueueAndATable_EmptiesAllThree()
    {
        // Arrange
        var container = _fixture.BlobServiceClient.GetBlobContainerClient($"smoke-{Guid.NewGuid():N}");
        await container.CreateAsync();
        await container.GetBlobClient("hello.txt").UploadAsync(BinaryData.FromString("hello"));
        var queue = _fixture.QueueServiceClient.GetQueueClient($"smoke-{Guid.NewGuid():N}");
        await queue.CreateAsync();
        await queue.SendMessageAsync("hello");
        var table = _fixture.TableServiceClient.GetTableClient($"smoke{Guid.NewGuid():N}");
        await table.CreateAsync();
        await table.AddEntityAsync(new TableEntity("p", "r") { ["Value"] = "hello" });

        // Act
        await _fixture.ClearAllDataAsync();

        // Assert
        (await ToListAsync(_fixture.BlobServiceClient.GetBlobContainersAsync())).ShouldBeEmpty();
        (await ToListAsync(_fixture.QueueServiceClient.GetQueuesAsync())).ShouldBeEmpty();
        (await ToListAsync(_fixture.TableServiceClient.QueryAsync())).ShouldBeEmpty();
    }

    private static async Task<List<T>> ToListAsync<T>(Azure.AsyncPageable<T> pageable)
        where T : notnull
    {
        var items = new List<T>();
        await foreach (var item in pageable)
        {
            items.Add(item);
        }

        return items;
    }
}
