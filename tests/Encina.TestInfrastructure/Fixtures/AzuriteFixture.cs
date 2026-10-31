using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Testcontainers.Azurite;
using Xunit;

namespace Encina.TestInfrastructure.Fixtures;

/// <summary>
/// Azurite (Azure Storage emulator) fixture using Testcontainers.
/// Provides a throwaway Azurite instance with the Blob, Queue and Table services for the Azure cloud integration tests.
/// </summary>
/// <remarks>
/// <para>
/// The image is pinned to <c>mcr.microsoft.com/azure-storage/azurite:3.37.0</c> (the newest explicit 3.x tag of
/// 2026-10-27; issue #2699). Azurite is MIT licensed (Copyright (c) Microsoft Corporation), so running it accepts no
/// extra licence terms. No <c>:latest</c> tag is used.
/// </para>
/// <para>
/// Azurite is started with <c>--skipApiVersionCheck</c> (the documented switch): the Azure Storage SDKs request a
/// service API version newer than the one Azurite 3.37.0 implements, and Azurite rejects it otherwise.
/// </para>
/// <para>
/// One container is shared by every test class of the <c>Cloud-Azurite</c> collection (<see cref="CollectionName"/>).
/// Each test assembly declares its own <c>[CollectionDefinition]</c> class. Test classes call
/// <see cref="ClearAllDataAsync"/> in <c>InitializeAsync()</c> and never dispose the fixture.
/// </para>
/// </remarks>
public sealed class AzuriteFixture : IAsyncLifetime
{
    /// <summary>
    /// The name of the shared xUnit collection that owns the Azurite container.
    /// </summary>
    public const string CollectionName = "Cloud-Azurite";

    /// <summary>
    /// The pinned Azurite image.
    /// </summary>
    public const string Image = "mcr.microsoft.com/azure-storage/azurite:3.37.0";

    private AzuriteContainer? _container;
    private BlobServiceClient? _blobServiceClient;
    private QueueServiceClient? _queueServiceClient;
    private TableServiceClient? _tableServiceClient;

    /// <summary>
    /// Gets the Azurite connection string covering the Blob, Queue and Table endpoints.
    /// </summary>
    public string ConnectionString => _container?.GetConnectionString() ?? string.Empty;

    /// <summary>
    /// Gets the Blob service endpoint.
    /// </summary>
    public string BlobEndpoint => _container?.GetBlobEndpoint() ?? string.Empty;

    /// <summary>
    /// Gets the Queue service endpoint.
    /// </summary>
    public string QueueEndpoint => _container?.GetQueueEndpoint() ?? string.Empty;

    /// <summary>
    /// Gets the Table service endpoint.
    /// </summary>
    public string TableEndpoint => _container?.GetTableEndpoint() ?? string.Empty;

    /// <summary>
    /// Gets a value indicating whether the Azurite container is available.
    /// </summary>
    public bool IsAvailable => _container is not null && !string.IsNullOrEmpty(ConnectionString);

    /// <summary>
    /// Gets the Blob service client shared by the collection. The fixture owns it.
    /// </summary>
    public BlobServiceClient BlobServiceClient =>
        _blobServiceClient ?? throw new InvalidOperationException("The fixture is not initialized.");

    /// <summary>
    /// Gets the Queue service client shared by the collection. The fixture owns it.
    /// </summary>
    public QueueServiceClient QueueServiceClient =>
        _queueServiceClient ?? throw new InvalidOperationException("The fixture is not initialized.");

    /// <summary>
    /// Gets the Table service client shared by the collection. The fixture owns it.
    /// </summary>
    public TableServiceClient TableServiceClient =>
        _tableServiceClient ?? throw new InvalidOperationException("The fixture is not initialized.");

    /// <summary>
    /// Deletes every blob container, queue and table so the next test starts empty.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when everything is deleted.</returns>
    public async Task ClearAllDataAsync(CancellationToken cancellationToken = default)
    {
        await foreach (var container in BlobServiceClient.GetBlobContainersAsync(cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            await BlobServiceClient.DeleteBlobContainerAsync(container.Name, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        await foreach (var queue in QueueServiceClient.GetQueuesAsync(cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            await QueueServiceClient.DeleteQueueAsync(queue.Name, cancellationToken).ConfigureAwait(false);
        }

        await foreach (var table in TableServiceClient.QueryAsync(cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            await TableServiceClient.DeleteTableAsync(table.Name, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        _container = new AzuriteBuilder(Image)
            .WithInMemoryPersistence()
            .WithCommand("--skipApiVersionCheck")
            .WithCleanUp(true)
            .Build();

        await _container.StartAsync();

        _blobServiceClient = new BlobServiceClient(ConnectionString);
        _queueServiceClient = new QueueServiceClient(ConnectionString);
        _tableServiceClient = new TableServiceClient(ConnectionString);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }
}
