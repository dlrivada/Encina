using Encina.Caching.Redis;
using StackExchange.Redis;

namespace Encina.GuardTests.Infrastructure.Caching;

/// <summary>
/// Guard tests for <see cref="RedisPubSubProvider"/>.
/// </summary>
public class RedisPubSubProviderGuardTests
{
    private static RedisPubSubProvider Create() =>
        new(Substitute.For<IConnectionMultiplexer>(), NullLogger<RedisPubSubProvider>.Instance);

    private static Task Handler(string _) => Task.CompletedTask;

    private static Task PatternHandler(string _, string __) => Task.CompletedTask;

    [Fact]
    public void Constructor_NullConnection_Throws() =>
        Should.Throw<ArgumentNullException>(() => new RedisPubSubProvider(null!, NullLogger<RedisPubSubProvider>.Instance))
            .ParamName.ShouldBe("connection");

    [Fact]
    public void Constructor_NullLogger_Throws() =>
        Should.Throw<ArgumentNullException>(() => new RedisPubSubProvider(Substitute.For<IConnectionMultiplexer>(), null!))
            .ParamName.ShouldBe("logger");

    [Fact]
    public async Task PublishAsync_NullChannel_Throws() =>
        (await Should.ThrowAsync<ArgumentNullException>(() => Create().PublishAsync(null!, "m", default))).ParamName.ShouldBe("channel");

    [Fact]
    public async Task PublishAsync_NullMessage_Throws() =>
        (await Should.ThrowAsync<ArgumentNullException>(() => Create().PublishAsync("c", (string)null!, default))).ParamName.ShouldBe("message");

    [Fact]
    public async Task PublishAsyncGeneric_NullChannel_Throws() =>
        (await Should.ThrowAsync<ArgumentNullException>(() => Create().PublishAsync<int>(null!, 1, default))).ParamName.ShouldBe("channel");

    [Fact]
    public async Task SubscribeAsync_NullChannel_Throws() =>
        (await Should.ThrowAsync<ArgumentNullException>(() => Create().SubscribeAsync(null!, Handler, default))).ParamName.ShouldBe("channel");

    [Fact]
    public async Task SubscribeAsync_NullHandler_Throws() =>
        (await Should.ThrowAsync<ArgumentNullException>(() => Create().SubscribeAsync("c", (Func<string, Task>)null!, default))).ParamName.ShouldBe("handler");

    [Fact]
    public async Task SubscribeAsyncGeneric_NullChannel_Throws() =>
        (await Should.ThrowAsync<ArgumentNullException>(() => Create().SubscribeAsync<int>(null!, _ => Task.CompletedTask, default))).ParamName.ShouldBe("channel");

    [Fact]
    public async Task SubscribeAsyncGeneric_NullHandler_Throws() =>
        (await Should.ThrowAsync<ArgumentNullException>(() => Create().SubscribeAsync<int>("c", null!, default))).ParamName.ShouldBe("handler");

    [Fact]
    public async Task SubscribePatternAsync_NullPattern_Throws() =>
        (await Should.ThrowAsync<ArgumentNullException>(() => Create().SubscribePatternAsync(null!, PatternHandler, default))).ParamName.ShouldBe("pattern");

    [Fact]
    public async Task SubscribePatternAsync_NullHandler_Throws() =>
        (await Should.ThrowAsync<ArgumentNullException>(() => Create().SubscribePatternAsync("p", null!, default))).ParamName.ShouldBe("handler");

    [Fact]
    public async Task UnsubscribeAsync_NullChannel_Throws() =>
        (await Should.ThrowAsync<ArgumentNullException>(() => Create().UnsubscribeAsync(null!, default))).ParamName.ShouldBe("channel");

    [Fact]
    public async Task UnsubscribePatternAsync_NullPattern_Throws() =>
        (await Should.ThrowAsync<ArgumentNullException>(() => Create().UnsubscribePatternAsync(null!, default))).ParamName.ShouldBe("pattern");

    [Fact]
    public async Task GetSubscriberCountAsync_NullChannel_Throws() =>
        (await Should.ThrowAsync<ArgumentNullException>(() => Create().GetSubscriberCountAsync(null!, default))).ParamName.ShouldBe("channel");
}
