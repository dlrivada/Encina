using System.Collections.Concurrent;
using System.Text.Json;
using Encina.IntegrationTests.Infrastructure.Caching.Fixtures;
using Encina.Redis.PubSub;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Encina.IntegrationTests.Messaging.RedisPubSub;

/// <summary>
/// Publishes and subscribes through <see cref="RedisPubSubMessagePublisher"/> against a real Redis
/// container and checks the envelope message id (#1792) and typed delivery round-trip.
/// </summary>
[Collection(RedisCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Transport", "Redis")]
public sealed class RedisPubSubMessagePublisherIntegrationTests : IAsyncLifetime
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private readonly RedisFixture _fixture;
    private ServiceProvider? _provider;

    public RedisPubSubMessagePublisherIntegrationTests(RedisFixture fixture)
    {
        _fixture = fixture;
    }

    public ValueTask InitializeAsync()
    {
        Assert.True(_fixture.IsAvailable, "The Redis container must be running.");

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaRedisPubSub(o =>
        {
            o.ConnectionString = _fixture.ConnectionString;
            o.ChannelPrefix = "it1792";
        });
        _provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_provider is not null)
        {
            await _provider.DisposeAsync();
        }
    }

    [Fact]
    public async Task PublishAsync_WithMessageId_EnvelopeCarriesItToARawSubscriber()
    {
        var channel = $"it1792:{Guid.NewGuid():N}";
        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var multiplexer = _provider!.GetRequiredService<IConnectionMultiplexer>();
        var queue = await multiplexer.GetSubscriber().SubscribeAsync(RedisChannel.Literal(channel));
        queue.OnMessage(m =>
        {
            received.TrySetResult(m.Message.ToString());
            return Task.CompletedTask;
        });

        using var scope = _provider!.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IRedisPubSubMessagePublisher>();
        var result = await publisher.PublishAsync(new Payload("hello"), channel, "outbox-42");

        result.IsRight.ShouldBeTrue();
        var json = await received.Task.WaitAsync(Timeout);
        var envelope = JsonDocument.Parse(json).RootElement;
        envelope.GetProperty("MessageId").GetString().ShouldBe("outbox-42");
        envelope.GetProperty("Payload").GetProperty("Text").GetString().ShouldBe("hello");
        envelope.GetProperty("TimestampUtc").GetDateTime().ShouldBeGreaterThan(DateTime.UnixEpoch);
        await queue.UnsubscribeAsync();
    }

    [Fact]
    public async Task PublishAsync_WithoutMessageId_EnvelopeCarriesAGeneratedGuid()
    {
        var channel = $"it1792:{Guid.NewGuid():N}";
        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var multiplexer = _provider!.GetRequiredService<IConnectionMultiplexer>();
        var queue = await multiplexer.GetSubscriber().SubscribeAsync(RedisChannel.Literal(channel));
        queue.OnMessage(m =>
        {
            received.TrySetResult(m.Message.ToString());
            return Task.CompletedTask;
        });

        using var scope = _provider!.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IRedisPubSubMessagePublisher>()
            .PublishAsync(new Payload("hello"), channel);

        var envelope = JsonDocument.Parse(await received.Task.WaitAsync(Timeout)).RootElement;
        Guid.TryParse(envelope.GetProperty("MessageId").GetString(), out _).ShouldBeTrue();
        await queue.UnsubscribeAsync();
    }

    [Fact]
    public async Task SubscribeAsync_DeliversThePublishedPayloadToTheTypedHandler()
    {
        var channel = $"it1792:{Guid.NewGuid():N}";
        var received = new TaskCompletionSource<Payload>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var scope = _provider!.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IRedisPubSubMessagePublisher>();

        await using var subscription = await publisher.SubscribeAsync<Payload>(
            p =>
            {
                received.TrySetResult(p);
                return ValueTask.CompletedTask;
            },
            channel);
        await publisher.PublishAsync(new Payload("typed"), channel, "outbox-7");

        (await received.Task.WaitAsync(Timeout)).Text.ShouldBe("typed");
    }

    [Fact]
    public async Task SubscribePatternAsync_DeliversWithTheMatchingChannelName()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var received = new ConcurrentQueue<(string Channel, Payload Message)>();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var scope = _provider!.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IRedisPubSubMessagePublisher>();

        await using var subscription = await publisher.SubscribePatternAsync<Payload>(
            $"{suffix}:*",
            (channel, message) =>
            {
                received.Enqueue((channel, message));
                gate.TrySetResult();
                return ValueTask.CompletedTask;
            });
        await publisher.PublishAsync(new Payload("pattern"), $"it1792:{suffix}:a");

        await gate.Task.WaitAsync(Timeout);
        received.TryDequeue(out var item).ShouldBeTrue();
        item.Message.Text.ShouldBe("pattern");
        item.Channel.ShouldEndWith($"{suffix}:a");
    }

    private sealed record Payload(string Text);
}
