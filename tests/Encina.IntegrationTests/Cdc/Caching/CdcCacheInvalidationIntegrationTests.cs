using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Encina.Caching;
using Encina.Cdc;
using Encina.Cdc.Abstractions;
using Encina.Cdc.Caching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace Encina.IntegrationTests.Cdc.Caching;

/// <summary>
/// Integration tests for CDC-driven cache invalidation. Validates end-to-end flow:
/// CDC change event → handler → local cache invalidated → pub/sub broadcast → subscriber → local cache invalidated.
/// Uses in-memory substitutes for cache and pub/sub providers.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Feature", "CDC")]
[SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly",
    Justification = "NSubstitute mock setup pattern")]
public sealed class CdcCacheInvalidationIntegrationTests
{
    #region End-to-End Cache Invalidation

    /// <summary>
    /// Verifies that a CDC insert event triggers local cache invalidation with the correct pattern.
    /// </summary>
    [Fact]
    public async Task CdcInsertEvent_InvalidatesLocalCache_WithCorrectPattern()
    {
        // Arrange
        using var fixture = CreateFixture();
        var context = CreateContext("dbo.Orders");

        // Act
        var result = await fixture.Handler.HandleInsertAsync(
            JsonElement.Parse("{}"), context);

        // Assert
        result.IsRight.ShouldBeTrue();
        await fixture.CacheProvider.Received(1).RemoveByPatternAsync(
            "sm:qc:*:Orders:*", Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that a CDC update event triggers cache invalidation.
    /// </summary>
    [Fact]
    public async Task CdcUpdateEvent_InvalidatesLocalCache()
    {
        // Arrange
        using var fixture = CreateFixture();
        var context = CreateContext("Products");

        // Act
        var result = await fixture.Handler.HandleUpdateAsync(
            JsonElement.Parse("{}"), JsonElement.Parse("{}"), context);

        // Assert
        result.IsRight.ShouldBeTrue();
        await fixture.CacheProvider.Received(1).RemoveByPatternAsync(
            "sm:qc:*:Products:*", Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that a CDC delete event triggers cache invalidation.
    /// </summary>
    [Fact]
    public async Task CdcDeleteEvent_InvalidatesLocalCache()
    {
        // Arrange
        using var fixture = CreateFixture();
        var context = CreateContext("Products");

        // Act
        var result = await fixture.Handler.HandleDeleteAsync(
            JsonElement.Parse("{}"), context);

        // Assert
        result.IsRight.ShouldBeTrue();
        await fixture.CacheProvider.Received(1).RemoveByPatternAsync(
            "sm:qc:*:Products:*", Arg.Any<CancellationToken>());
    }

    #endregion

    #region PubSub Broadcast and Subscriber

    /// <summary>
    /// Verifies that after local cache invalidation, the handler broadcasts the invalidation
    /// pattern via pub/sub.
    /// </summary>
    [Fact]
    public async Task CdcEvent_BroadcastsInvalidationPattern_ViaPubSub()
    {
        // Arrange
        var options = new QueryCacheInvalidationOptions
        {
            UsePubSubBroadcast = true,
            PubSubChannel = "test:cache:invalidate"
        };
        using var fixture = CreateFixture(options);
        var context = CreateContext("Orders");

        // Act
        await fixture.Handler.HandleInsertAsync(JsonElement.Parse("{}"), context);

        // Assert
        await fixture.PubSubProvider!.Received(1).PublishAsync(
            "test:cache:invalidate",
            "sm:qc:*:Orders:*",
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies the subscriber service calls <c>RemoveByPatternAsync</c> when it receives
    /// an invalidation message on the pub/sub channel.
    /// </summary>
    [Fact]
    public async Task Subscriber_ReceivesMessage_InvalidatesLocalCache()
    {
        // Arrange
        var cacheProvider = Substitute.For<ICacheProvider>();
        var pubSubProvider = Substitute.For<IPubSubProvider>();
        Func<string, Task>? capturedCallback = null;

        pubSubProvider.SubscribeAsync(
                Arg.Any<string>(),
                Arg.Do<Func<string, Task>>(cb => capturedCallback = cb),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Substitute.For<IAsyncDisposable>()));

        var options = Options.Create(new QueryCacheInvalidationOptions
        {
            PubSubChannel = "test:cache:invalidate"
        });
        var logger = NullLogger<CacheInvalidationSubscriberService>.Instance;
        var subscriber = new CacheInvalidationSubscriberService(
            pubSubProvider, cacheProvider, options, logger);

        // Act — start the subscriber to register the callback
        await subscriber.StartAsync(CancellationToken.None);
        capturedCallback.ShouldNotBeNull("Subscriber must register a callback on StartAsync");

        // Simulate receiving an invalidation pattern
        await capturedCallback("sm:qc:*:Orders:*");

        // Assert
        await cacheProvider.Received(1).RemoveByPatternAsync(
            "sm:qc:*:Orders:*", Arg.Any<CancellationToken>());

        // Cleanup
        await subscriber.StopAsync(CancellationToken.None);
    }

    /// <summary>
    /// Verifies that the subscriber handles errors from the cache provider gracefully.
    /// </summary>
    [Fact]
    public async Task Subscriber_CacheProviderThrows_DoesNotCrash()
    {
        // Arrange
        var cacheProvider = Substitute.For<ICacheProvider>();
        cacheProvider.RemoveByPatternAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Cache down")));

        var pubSubProvider = Substitute.For<IPubSubProvider>();
        Func<string, Task>? capturedCallback = null;

        pubSubProvider.SubscribeAsync(
                Arg.Any<string>(),
                Arg.Do<Func<string, Task>>(cb => capturedCallback = cb),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Substitute.For<IAsyncDisposable>()));

        var options = Options.Create(new QueryCacheInvalidationOptions());
        var logger = NullLogger<CacheInvalidationSubscriberService>.Instance;
        var subscriber = new CacheInvalidationSubscriberService(
            pubSubProvider, cacheProvider, options, logger);

        await subscriber.StartAsync(CancellationToken.None);
        capturedCallback.ShouldNotBeNull();

        // Act & Assert — must not throw
        await Should.NotThrowAsync(async () => await capturedCallback("sm:qc:*:Orders:*"));

        // Cleanup
        await subscriber.StopAsync(CancellationToken.None);
    }

    #endregion

    #region Multiple Events Flow

    /// <summary>
    /// Verifies that multiple CDC events for different tables each trigger their own
    /// cache invalidation with distinct patterns.
    /// </summary>
    [Fact]
    public async Task MultipleCdcEvents_DifferentTables_EachInvalidatedSeparately()
    {
        // Arrange
        using var fixture = CreateFixture();

        // Act
        await fixture.Handler.HandleInsertAsync(
            JsonElement.Parse("{}"), CreateContext("Orders"));
        await fixture.Handler.HandleUpdateAsync(
            JsonElement.Parse("{}"), JsonElement.Parse("{}"), CreateContext("Products"));
        await fixture.Handler.HandleDeleteAsync(
            JsonElement.Parse("{}"), CreateContext("Customers"));

        // Assert
        await fixture.CacheProvider.Received(1).RemoveByPatternAsync(
            "sm:qc:*:Orders:*", Arg.Any<CancellationToken>());
        await fixture.CacheProvider.Received(1).RemoveByPatternAsync(
            "sm:qc:*:Products:*", Arg.Any<CancellationToken>());
        await fixture.CacheProvider.Received(1).RemoveByPatternAsync(
            "sm:qc:*:Customers:*", Arg.Any<CancellationToken>());
    }

    #endregion

    #region DI Registration

    /// <summary>
    /// Verifies that the cache invalidation handler is correctly resolved from the DI container
    /// when <c>UseCacheInvalidation</c> is enabled.
    /// </summary>
    [Fact]
    public void ServiceRegistration_UseCacheInvalidation_RegistersHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));
        services.AddSingleton(Substitute.For<ICacheProvider>());
        services.AddSingleton(Substitute.For<IPubSubProvider>());
        services.AddSingleton(Substitute.For<ICdcConnector>());

        services.AddEncinaCdc(config =>
        {
            config.UseCdc()
                  .WithCacheInvalidation();
        });

        using var sp = services.BuildServiceProvider();

        // Act
        var handlers = sp.GetServices<IChangeEventHandler<JsonElement>>();

        // Assert
        handlers.ShouldContain(h => h is QueryCacheInvalidationCdcHandler);
    }

    #endregion

    #region Test Helpers

    private sealed record CacheInvalidationFixture(
        QueryCacheInvalidationCdcHandler Handler,
        ICacheProvider CacheProvider,
        IPubSubProvider? PubSubProvider) : IDisposable
    {
        public void Dispose() { }
    }

    private static CacheInvalidationFixture CreateFixture(
        QueryCacheInvalidationOptions? options = null)
    {
        var cacheProvider = Substitute.For<ICacheProvider>();
        var pubSubProvider = Substitute.For<IPubSubProvider>();
        var opts = options ?? new QueryCacheInvalidationOptions();
        var wrappedOptions = Options.Create(opts);
        var logger = NullLogger<QueryCacheInvalidationCdcHandler>.Instance;

        var handler = new QueryCacheInvalidationCdcHandler(
            cacheProvider, pubSubProvider, wrappedOptions, logger);

        return new CacheInvalidationFixture(handler, cacheProvider, pubSubProvider);
    }

    private static ChangeContext CreateContext(string tableName)
    {
        var position = new TestCdcPosition(1);
        var metadata = new ChangeMetadata(position, DateTime.UtcNow, null, null, null);
        return new ChangeContext(tableName, metadata, CancellationToken.None);
    }

    private sealed class TestCdcPosition : CdcPosition
    {
        public TestCdcPosition(long value) => Value = value;
        public long Value { get; }
        public override byte[] ToBytes() => BitConverter.GetBytes(Value);
        public override int CompareTo(CdcPosition? other) =>
            other is TestCdcPosition tcp ? Value.CompareTo(tcp.Value) : 1;
        public override string ToString() => $"TestPosition({Value})";
    }

    #endregion
}
