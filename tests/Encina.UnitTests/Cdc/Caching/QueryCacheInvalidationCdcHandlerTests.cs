using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Encina.Caching;
using Encina.Cdc;
using Encina.Cdc.Abstractions;
using Encina.Cdc.Caching;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Cdc.Caching;

/// <summary>
/// Unit tests for <see cref="QueryCacheInvalidationCdcHandler"/>. Validates entity type
/// resolution, cache key pattern generation, table filtering, pub/sub broadcast behavior,
/// and graceful error handling.
/// </summary>
[SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly",
    Justification = "NSubstitute mock setup pattern")]
public sealed class QueryCacheInvalidationCdcHandlerTests
{
    #region Entity Type Resolution

    [Fact]
    public async Task HandleInsertAsync_SimpleTableName_ResolvesEntityType()
    {
        // Arrange
        var (handler, cacheProvider, _) = CreateHandler();
        var context = CreateContext("Orders");

        // Act
        var result = await handler.HandleInsertAsync(JsonElement.Parse("{}"), context);

        // Assert
        result.IsRight.ShouldBeTrue();
        await cacheProvider.Received(1).RemoveByPatternAsync(
            "sm:qc:*:Orders:*", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleInsertAsync_SchemaQualifiedTableName_StripsSchema()
    {
        // Arrange
        var (handler, cacheProvider, _) = CreateHandler();
        var context = CreateContext("dbo.Orders");

        // Act
        var result = await handler.HandleInsertAsync(JsonElement.Parse("{}"), context);

        // Assert
        result.IsRight.ShouldBeTrue();
        await cacheProvider.Received(1).RemoveByPatternAsync(
            "sm:qc:*:Orders:*", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleInsertAsync_PostgresSchemaQualifiedTableName_StripsSchema()
    {
        // Arrange
        var (handler, cacheProvider, _) = CreateHandler();
        var context = CreateContext("public.products");

        // Act
        var result = await handler.HandleInsertAsync(JsonElement.Parse("{}"), context);

        // Assert
        result.IsRight.ShouldBeTrue();
        await cacheProvider.Received(1).RemoveByPatternAsync(
            "sm:qc:*:products:*", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleInsertAsync_ExplicitMapping_UsesMapping()
    {
        // Arrange
        var options = new QueryCacheInvalidationOptions
        {
            TableToEntityTypeMappings = new Dictionary<string, string>
            {
                ["dbo.Orders"] = "Order"
            }
        };
        var (handler, cacheProvider, _) = CreateHandler(options);
        var context = CreateContext("dbo.Orders");

        // Act
        var result = await handler.HandleInsertAsync(JsonElement.Parse("{}"), context);

        // Assert
        result.IsRight.ShouldBeTrue();
        await cacheProvider.Received(1).RemoveByPatternAsync(
            "sm:qc:*:Order:*", Arg.Any<CancellationToken>());
    }

    #endregion

    #region Cache Key Pattern Generation

    [Fact]
    public async Task HandleInsertAsync_DefaultPrefix_GeneratesCorrectPattern()
    {
        // Arrange
        var (handler, cacheProvider, _) = CreateHandler();
        var context = CreateContext("Products");

        // Act
        await handler.HandleInsertAsync(JsonElement.Parse("{}"), context);

        // Assert
        await cacheProvider.Received(1).RemoveByPatternAsync(
            "sm:qc:*:Products:*", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleInsertAsync_CustomPrefix_GeneratesCorrectPattern()
    {
        // Arrange
        var options = new QueryCacheInvalidationOptions { CacheKeyPrefix = "custom:prefix" };
        var (handler, cacheProvider, _) = CreateHandler(options);
        var context = CreateContext("Products");

        // Act
        await handler.HandleInsertAsync(JsonElement.Parse("{}"), context);

        // Assert
        await cacheProvider.Received(1).RemoveByPatternAsync(
            "custom:prefix:*:Products:*", Arg.Any<CancellationToken>());
    }

    #endregion

    #region Table Filtering

    [Fact]
    public async Task HandleInsertAsync_TableInFilter_ProcessesInvalidation()
    {
        // Arrange
        var options = new QueryCacheInvalidationOptions
        {
            Tables = ["Orders", "Products"]
        };
        var (handler, cacheProvider, _) = CreateHandler(options);
        var context = CreateContext("Orders");

        // Act
        var result = await handler.HandleInsertAsync(JsonElement.Parse("{}"), context);

        // Assert
        result.IsRight.ShouldBeTrue();
        await cacheProvider.Received(1).RemoveByPatternAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleInsertAsync_TableNotInFilter_SkipsInvalidation()
    {
        // Arrange
        var options = new QueryCacheInvalidationOptions
        {
            Tables = ["Orders", "Products"]
        };
        var (handler, cacheProvider, _) = CreateHandler(options);
        var context = CreateContext("AuditLog");

        // Act
        var result = await handler.HandleInsertAsync(JsonElement.Parse("{}"), context);

        // Assert
        result.IsRight.ShouldBeTrue();
        await cacheProvider.DidNotReceive().RemoveByPatternAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleInsertAsync_TableFilterCaseInsensitive_ProcessesInvalidation()
    {
        // Arrange
        var options = new QueryCacheInvalidationOptions
        {
            Tables = ["orders"]
        };
        var (handler, cacheProvider, _) = CreateHandler(options);
        var context = CreateContext("ORDERS");

        // Act
        var result = await handler.HandleInsertAsync(JsonElement.Parse("{}"), context);

        // Assert
        result.IsRight.ShouldBeTrue();
        await cacheProvider.Received(1).RemoveByPatternAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleInsertAsync_NullTableFilter_ProcessesAllTables()
    {
        // Arrange
        var options = new QueryCacheInvalidationOptions { Tables = null };
        var (handler, cacheProvider, _) = CreateHandler(options);
        var context = CreateContext("AnyTable");

        // Act
        var result = await handler.HandleInsertAsync(JsonElement.Parse("{}"), context);

        // Assert
        result.IsRight.ShouldBeTrue();
        await cacheProvider.Received(1).RemoveByPatternAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region PubSub Broadcast

    [Fact]
    public async Task HandleInsertAsync_PubSubEnabled_BroadcastsPattern()
    {
        // Arrange
        var options = new QueryCacheInvalidationOptions { UsePubSubBroadcast = true };
        var (handler, _, pubSubProvider) = CreateHandler(options);
        var context = CreateContext("Orders");

        // Act
        var result = await handler.HandleInsertAsync(JsonElement.Parse("{}"), context);

        // Assert
        result.IsRight.ShouldBeTrue();
        await pubSubProvider!.Received(1).PublishAsync(
            "sm:cache:invalidate",
            "sm:qc:*:Orders:*",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleInsertAsync_PubSubDisabled_DoesNotBroadcast()
    {
        // Arrange
        var options = new QueryCacheInvalidationOptions { UsePubSubBroadcast = false };
        var (handler, _, pubSubProvider) = CreateHandler(options);
        var context = CreateContext("Orders");

        // Act
        var result = await handler.HandleInsertAsync(JsonElement.Parse("{}"), context);

        // Assert
        result.IsRight.ShouldBeTrue();
        await pubSubProvider!.DidNotReceive().PublishAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleInsertAsync_NullPubSubProvider_DoesNotThrow()
    {
        // Arrange
        var options = new QueryCacheInvalidationOptions { UsePubSubBroadcast = true };
        var (handler, _, _) = CreateHandler(options, withPubSub: false);
        var context = CreateContext("Orders");

        // Act
        var result = await handler.HandleInsertAsync(JsonElement.Parse("{}"), context);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleInsertAsync_CustomChannel_UsesConfiguredChannel()
    {
        // Arrange
        var options = new QueryCacheInvalidationOptions
        {
            UsePubSubBroadcast = true,
            PubSubChannel = "custom:channel"
        };
        var (handler, _, pubSubProvider) = CreateHandler(options);
        var context = CreateContext("Orders");

        // Act
        await handler.HandleInsertAsync(JsonElement.Parse("{}"), context);

        // Assert
        await pubSubProvider!.Received(1).PublishAsync(
            "custom:channel",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Error Handling

    [Fact]
    public async Task HandleInsertAsync_CacheProviderThrows_ReturnsRightAndDoesNotBlock()
    {
        // Arrange
        var (handler, cacheProvider, _) = CreateHandler();
        cacheProvider.RemoveByPatternAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Cache connection lost"));
        var context = CreateContext("Orders");

        // Act
        var result = await handler.HandleInsertAsync(JsonElement.Parse("{}"), context);

        // Assert — handler must not block CDC pipeline
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleInsertAsync_PubSubThrows_ReturnsRightAndDoesNotBlock()
    {
        // Arrange
        var options = new QueryCacheInvalidationOptions { UsePubSubBroadcast = true };
        var (handler, _, pubSubProvider) = CreateHandler(options);
        pubSubProvider!.PublishAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("PubSub connection lost"));
        var context = CreateContext("Orders");

        // Act
        var result = await handler.HandleInsertAsync(JsonElement.Parse("{}"), context);

        // Assert — handler must not block CDC pipeline
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleInsertAsync_CacheProviderThrows_SkipsPubSub()
    {
        // Arrange
        var options = new QueryCacheInvalidationOptions { UsePubSubBroadcast = true };
        var (handler, cacheProvider, pubSubProvider) = CreateHandler(options);
        cacheProvider.RemoveByPatternAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Cache down"));
        var context = CreateContext("Orders");

        // Act
        await handler.HandleInsertAsync(JsonElement.Parse("{}"), context);

        // Assert — if cache fails, do not attempt broadcast
        await pubSubProvider!.DidNotReceive().PublishAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region All Handler Methods

    [Fact]
    public async Task HandleUpdateAsync_InvokesInvalidation()
    {
        // Arrange
        var (handler, cacheProvider, _) = CreateHandler();
        var context = CreateContext("Orders");

        // Act
        var result = await handler.HandleUpdateAsync(
            JsonElement.Parse("{}"), JsonElement.Parse("{}"), context);

        // Assert
        result.IsRight.ShouldBeTrue();
        await cacheProvider.Received(1).RemoveByPatternAsync(
            "sm:qc:*:Orders:*", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleDeleteAsync_InvokesInvalidation()
    {
        // Arrange
        var (handler, cacheProvider, _) = CreateHandler();
        var context = CreateContext("Orders");

        // Act
        var result = await handler.HandleDeleteAsync(JsonElement.Parse("{}"), context);

        // Assert
        result.IsRight.ShouldBeTrue();
        await cacheProvider.Received(1).RemoveByPatternAsync(
            "sm:qc:*:Orders:*", Arg.Any<CancellationToken>());
    }

    #endregion

    #region Test Helpers

    private static (QueryCacheInvalidationCdcHandler Handler, ICacheProvider CacheProvider, IPubSubProvider? PubSubProvider) CreateHandler(
        QueryCacheInvalidationOptions? options = null,
        bool withPubSub = true)
    {
        var cacheProvider = Substitute.For<ICacheProvider>();
        var pubSubProvider = withPubSub ? Substitute.For<IPubSubProvider>() : null;
        var opts = options ?? new QueryCacheInvalidationOptions();
        var wrappedOptions = Options.Create(opts);
        var logger = NullLogger<QueryCacheInvalidationCdcHandler>.Instance;

        var handler = new QueryCacheInvalidationCdcHandler(
            cacheProvider, pubSubProvider, wrappedOptions, logger);

        return (handler, cacheProvider, pubSubProvider);
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
