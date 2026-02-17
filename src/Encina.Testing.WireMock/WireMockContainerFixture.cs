using WireMock.Client;
using WireMock.Net.Testcontainers;
using Xunit;

namespace Encina.Testing.WireMock;

/// <summary>
/// xUnit fixture for WireMock running in a Docker container via Testcontainers.
/// Use this when you need complete isolation between test runs or when testing
/// in a CI/CD environment where the WireMock server should run in Docker.
/// </summary>
/// <remarks>
/// Requires Docker to be running on the host machine.
/// The container is automatically cleaned up after the test run.
/// </remarks>
/// <example>
/// <code>
/// public class ExternalApiTests : IClassFixture&lt;WireMockContainerFixture&gt;
/// {
///     private readonly WireMockContainerFixture _wireMock;
///
///     public ExternalApiTests(WireMockContainerFixture wireMock)
///     {
///         _wireMock = wireMock;
///     }
///
///     [Fact]
///     public async Task Should_Call_External_Api()
///     {
///         // Configure stub via admin client
///         var adminClient = _wireMock.CreateAdminClient();
///         // ... configure mappings
///
///         var client = new HttpClient { BaseAddress = new Uri(_wireMock.BaseUrl) };
///         var response = await client.GetAsync("/api/data");
///
///         response.StatusCode.ShouldBe(HttpStatusCode.OK);
///     }
/// }
/// </code>
/// </example>
public class WireMockContainerFixture : IAsyncLifetime
{
    private WireMockContainer? _container;

    /// <summary>
    /// Gets the running WireMock container instance.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessed before initialization.</exception>
    public WireMockContainer Container => _container ?? throw new InvalidOperationException("Container not initialized. Call InitializeAsync first.");

    /// <summary>
    /// Gets the base URL of the WireMock server running in the container.
    /// </summary>
    public string BaseUrl => Container.GetPublicUrl();

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        _container = new WireMockContainerBuilder()
            .WithAutoRemove(true)
            .WithCleanUp(true)
            .Build();

        await _container.StartAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync().ConfigureAwait(false);
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Creates a WireMock admin REST client for configuring mappings.
    /// </summary>
    /// <returns>A RestEase admin client for the WireMock server.</returns>
    public IWireMockAdminApi CreateAdminClient()
    {
        return Container.CreateWireMockAdminClient();
    }

    /// <summary>
    /// Creates an <see cref="HttpClient"/> configured to use this WireMock container.
    /// </summary>
    /// <returns>An HttpClient with BaseAddress set to the container URL.</returns>
    public HttpClient CreateClient()
    {
        return Container.CreateClient();
    }

    /// <summary>
    /// Resets all mappings and request logs in the WireMock server.
    /// </summary>
    public async Task ResetAsync()
    {
        var client = CreateAdminClient();
        await client.ResetMappingsAsync().ConfigureAwait(false);
        await client.DeleteRequestsAsync().ConfigureAwait(false);
    }
}
