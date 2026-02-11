using Microsoft.Extensions.DependencyInjection;
using PactNet;
using Xunit;

namespace Encina.Testing.Pact;

/// <summary>
/// xUnit test fixture for Pact contract testing with Encina.
/// </summary>
/// <remarks>
/// <para>
/// This fixture provides a complete setup for consumer-driven contract testing,
/// including mock server management and Encina integration.
/// </para>
/// <para>
/// Example usage as a class fixture:
/// <code>
/// public class OrderServiceConsumerTests : IClassFixture&lt;EncinaPactFixture&gt;
/// {
///     private readonly EncinaPactFixture _fixture;
///
///     public OrderServiceConsumerTests(EncinaPactFixture fixture)
///     {
///         _fixture = fixture;
///     }
///
///     [Fact]
///     public async Task GetOrder_WhenOrderExists_ReturnsOrder()
///     {
///         var orderId = Guid.Parse("12345678-1234-1234-1234-123456789012");
///
///         var consumer = _fixture.CreateConsumer("WebApp", "OrdersAPI")
///             .WithQueryExpectation(
///                 new GetOrderById(orderId),
///                 Either&lt;EncinaError, OrderDto&gt;.Right(new OrderDto { Id = orderId }));
///
///         await _fixture.VerifyAsync(consumer, async mockServerUri =>
///         {
///             var client = new HttpClient { BaseAddress = mockServerUri };
///             var response = await client.GetAsync($"/api/queries/GetOrderById");
///             response.StatusCode.Should().Be(HttpStatusCode.OK);
///         });
///     }
/// }
/// </code>
/// </para>
/// </remarks>
public sealed class EncinaPactFixture : IAsyncLifetime, IAsyncDisposable, IDisposable
{
    private readonly List<EncinaPactConsumerBuilder> _consumers = [];
    private readonly Dictionary<string, EncinaPactProviderVerifier> _verifiers = [];
    private IServiceProvider? _serviceProvider;
    private IEncina? _encina;
    private int _disposed;

    /// <summary>
    /// Gets the default pact directory for storing Pact files.
    /// </summary>
    /// <remarks>
    /// This property can only be set during object initialization.
    /// To use a custom directory, create the fixture with an object initializer:
    /// <c>new EncinaPactFixture { PactDirectory = "./custom-pacts" }</c>
    /// </remarks>
    public string PactDirectory { get; init; } = "./pacts";

    /// <summary>
    /// Gets the configured service provider, or null if not configured.
    /// </summary>
    /// <remarks>
    /// The service provider is configured by calling <see cref="WithServices"/> or <see cref="WithEncina"/>.
    /// </remarks>
    public IServiceProvider? ServiceProvider => _serviceProvider;

    /// <summary>
    /// Gets the xUnit test output helper for logging.
    /// </summary>
    public Xunit.ITestOutputHelper? OutputHelper { get; set; }

    /// <summary>
    /// Initializes the fixture.
    /// </summary>
    /// <returns>A value task representing the async initialization.</returns>
    public ValueTask InitializeAsync()
    {
        // Ensure pact directory exists
        if (!Directory.Exists(PactDirectory))
        {
            Directory.CreateDirectory(PactDirectory);
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Cleans up resources when the fixture is disposed.
    /// </summary>
    /// <returns>A value task representing the async cleanup.</returns>
    /// <remarks>
    /// This method is required by <see cref="IAsyncLifetime"/> for xUnit integration.
    /// </remarks>
    public ValueTask DisposeAsync() => DisposeAsyncCore();

    /// <inheritdoc />
    ValueTask IAsyncDisposable.DisposeAsync() => DisposeAsyncCore();

    /// <summary>
    /// Configures the fixture with an Encina instance for provider verification.
    /// </summary>
    /// <param name="encina">The Encina instance.</param>
    /// <param name="serviceProvider">The service provider.</param>
    /// <returns>The fixture for fluent chaining.</returns>
    public EncinaPactFixture WithEncina(IEncina encina, IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(encina);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        _encina = encina;
        _serviceProvider = serviceProvider;
        return this;
    }

    /// <summary>
    /// Configures the fixture with services for Encina setup.
    /// </summary>
    /// <param name="configureServices">Action to configure services.</param>
    /// <returns>The fixture for fluent chaining.</returns>
    public EncinaPactFixture WithServices(Action<IServiceCollection> configureServices)
    {
        ArgumentNullException.ThrowIfNull(configureServices);

        var services = new ServiceCollection();
        configureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        if (_serviceProvider.GetService<IEncina>() is { } encina)
        {
            _encina = encina;
        }

        return this;
    }

    /// <summary>
    /// Creates a new consumer builder for defining contract expectations.
    /// </summary>
    /// <param name="consumerName">Name of the consumer service.</param>
    /// <param name="providerName">Name of the provider service.</param>
    /// <returns>A new consumer builder.</returns>
    public EncinaPactConsumerBuilder CreateConsumer(string consumerName, string providerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);

        var consumer = new EncinaPactConsumerBuilder(
            consumerName,
            providerName,
            PactDirectory,
            OutputHelper);

        _consumers.Add(consumer);
        return consumer;
    }

    /// <summary>
    /// Creates a new provider verifier for verifying contract compliance.
    /// </summary>
    /// <param name="providerName">Name of the provider being verified.</param>
    /// <returns>A new provider verifier.</returns>
    /// <exception cref="InvalidOperationException">Thrown when Encina or ServiceProvider is not configured.</exception>
    public EncinaPactProviderVerifier CreateVerifier(string providerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);

        if (_encina is null)
        {
            throw new InvalidOperationException(
                "Encina is not configured. Call WithEncina() or WithServices() first.");
        }

        if (_serviceProvider is null)
        {
            throw new InvalidOperationException(
                "ServiceProvider is not configured. Call WithServices() or WithEncina() first.");
        }

        var verifier = new EncinaPactProviderVerifier(_encina, _serviceProvider)
            .WithProviderName(providerName);

        _verifiers[providerName] = verifier;
        return verifier;
    }

    /// <summary>
    /// Verifies a consumer interaction against a mock server.
    /// </summary>
    /// <param name="consumer">The consumer builder with defined expectations.</param>
    /// <param name="testAction">The test action to execute against the mock server.</param>
    /// <returns>A task representing the async verification.</returns>
    /// <remarks>
    /// This method delegates to <see cref="EncinaPactConsumerBuilder.VerifyAsync"/> which
    /// starts the mock server and executes the test action with the dynamically assigned URI.
    /// This method is intentionally not static to maintain API consistency with the fixture pattern.
    /// </remarks>
#pragma warning disable CA1822 // Mark members as static - intentionally instance method for API consistency
    public async Task VerifyAsync( // NOSONAR S2325: Instance method for API consistency with fixture pattern
        EncinaPactConsumerBuilder consumer,
        Func<Uri, Task> testAction)
#pragma warning restore CA1822
    {
        ArgumentNullException.ThrowIfNull(consumer);
        ArgumentNullException.ThrowIfNull(testAction);

        await consumer.VerifyAsync(testAction);
    }

    /// <summary>
    /// Verifies a consumer interaction against a mock server with synchronous test logic.
    /// </summary>
    /// <param name="consumer">The consumer builder with defined expectations.</param>
    /// <param name="testAction">The test action to execute against the mock server.</param>
    /// <returns>A task representing the async verification.</returns>
    /// <remarks>
    /// This method delegates to <see cref="EncinaPactConsumerBuilder.Verify"/> which
    /// starts the mock server and executes the test action with the dynamically assigned URI.
    /// This method is intentionally not static to maintain API consistency with the fixture pattern.
    /// </remarks>
#pragma warning disable CA1822 // Mark members as static - intentionally instance method for API consistency
    public Task VerifyAsync( // NOSONAR S2325: Instance method for API consistency with fixture pattern
        EncinaPactConsumerBuilder consumer,
        Action<Uri> testAction)
#pragma warning restore CA1822
    {
        ArgumentNullException.ThrowIfNull(consumer);
        ArgumentNullException.ThrowIfNull(testAction);

        consumer.Verify(testAction);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies all registered Pact files against the provider implementation.
    /// </summary>
    /// <param name="providerName">The provider name to verify.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The verification results.</returns>
    public async Task<IReadOnlyList<PactVerificationResult>> VerifyProviderAsync(
        string providerName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);

        if (!_verifiers.TryGetValue(providerName, out var verifier))
        {
            verifier = CreateVerifier(providerName);
        }

        var results = new List<PactVerificationResult>();
        var pactFiles = Directory.GetFiles(PactDirectory, "*.json");

        foreach (var pactFile in pactFiles)
        {
            var result = await verifier.VerifyAsync(pactFile, cancellationToken);
            results.Add(result);
        }

        return results;
    }

    /// <summary>
    /// Gets all Pact files in the configured directory.
    /// </summary>
    /// <returns>A list of Pact file paths.</returns>
    public IReadOnlyList<string> GetPactFiles()
    {
        if (!Directory.Exists(PactDirectory))
        {
            return [];
        }

        return Directory.GetFiles(PactDirectory, "*.json");
    }

    /// <summary>
    /// Clears all Pact files from the configured directory.
    /// </summary>
    /// <remarks>
    /// If a file cannot be deleted due to IO errors or permissions, the error is traced
    /// and the operation continues with remaining files.
    /// </remarks>
    public void ClearPactFiles()
    {
        if (!Directory.Exists(PactDirectory))
        {
            return;
        }

        foreach (var file in Directory.GetFiles(PactDirectory, "*.json"))
        {
            try
            {
                File.Delete(file);
            }
            catch (IOException ex)
            {
                // File may be locked by another process; log and continue
                System.Diagnostics.Trace.TraceWarning(
                    "Failed to delete Pact file '{0}': {1}", file, ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                // Insufficient permissions; log and continue
                System.Diagnostics.Trace.TraceWarning(
                    "Failed to delete Pact file '{0}': {1}", file, ex.Message);
            }
        }
    }

    /// <summary>
    /// Resets the fixture state for a new test.
    /// </summary>
    public void Reset()
    {
        foreach (var consumer in _consumers)
        {
            consumer.Dispose();
        }

        _consumers.Clear();

        foreach (var verifier in _verifiers.Values)
        {
            verifier.Dispose();
        }

        _verifiers.Clear();
    }

    /// <summary>
    /// Synchronously disposes the fixture by blocking on <see cref="DisposeAsyncCore"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method intentionally uses <c>GetAwaiter().GetResult()</c> to synchronously wait on
    /// the async disposal. While this pattern is generally discouraged due to potential deadlocks,
    /// it is safe here because <see cref="DisposeAsyncCore"/> uses <c>ConfigureAwait(false)</c>
    /// on all awaits, preventing synchronization context capture and avoiding deadlocks.
    /// </para>
    /// <para>
    /// This approach follows the recommended pattern for implementing both <see cref="IDisposable"/>
    /// and <see cref="IAsyncDisposable"/> with shared disposal logic.
    /// See: https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-disposeasync#implement-both-dispose-and-async-dispose
    /// </para>
    /// </remarks>
    /// <inheritdoc />
    public void Dispose() => DisposeAsyncCore().AsTask().GetAwaiter().GetResult();

    /// <summary>
    /// Core disposal logic shared by all dispose paths.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> representing the async disposal.</returns>
    /// <remarks>
    /// This method uses ConfigureAwait(false) to avoid capturing the synchronization context,
    /// preventing potential deadlocks when called synchronously from <see cref="Dispose"/>.
    /// </remarks>
    private async ValueTask DisposeAsyncCore()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
        {
            return;
        }

        Reset();

        if (_serviceProvider is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
        }
        else if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
