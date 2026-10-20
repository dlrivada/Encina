using System.Text.Json;
using Encina.Redis.PubSub;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using StackExchange.Redis;

namespace Encina.UnitTests.Messaging.RedisPubSub;

/// <summary>
/// Regression tests for #1792: the envelope timestamp comes from the injected <see cref="TimeProvider"/>
/// and the caller-supplied message id (for example the outbox id) is preserved.
/// </summary>
public sealed class RedisPubSubPublisherTimeAndMessageIdTests
{
    private static readonly DateTimeOffset FixedNow = new(2031, 5, 17, 8, 30, 0, TimeSpan.Zero);

    private readonly ISubscriber _subscriber = Substitute.For<ISubscriber>();
    private readonly RedisPubSubMessagePublisher _publisher;

    public RedisPubSubPublisherTimeAndMessageIdTests()
    {
        var redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetSubscriber(Arg.Any<object?>()).Returns(_subscriber);
        _publisher = new RedisPubSubMessagePublisher(
            redis,
            NullLogger<RedisPubSubMessagePublisher>.Instance,
            Options.Create(new EncinaRedisPubSubOptions()),
            new FakeTimeProvider(FixedNow));
    }

    private JsonElement SentEnvelope()
    {
        var call = _subscriber.ReceivedCalls().Single(c => c.GetMethodInfo().Name == nameof(ISubscriber.PublishAsync));
        var payload = call.GetArguments().OfType<RedisValue>().Single();
        return JsonDocument.Parse(payload.ToString()).RootElement;
    }

    [Fact]
    public async Task PublishAsync_UsesInjectedTimeProviderForTimestamp()
    {
        await _publisher.PublishAsync(new Payload("a"));

        SentEnvelope().GetProperty("TimestampUtc").GetDateTime().ShouldBe(FixedNow.UtcDateTime);
    }

    [Fact]
    public async Task PublishAsync_WithMessageId_PreservesIt()
    {
        await _publisher.PublishAsync(new Payload("a"), messageId: "outbox-1");

        SentEnvelope().GetProperty("MessageId").GetString().ShouldBe("outbox-1");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task PublishAsync_WithoutMessageId_GeneratesANewGuid(string? messageId)
    {
        await _publisher.PublishAsync(new Payload("a"), messageId: messageId);

        Guid.TryParse(SentEnvelope().GetProperty("MessageId").GetString(), out _).ShouldBeTrue();
    }

    [Fact]
    public void AddEncinaRedisPubSub_RegistersTimeProviderAndPassesProviderValidation()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IConnectionMultiplexer>());
        services.AddEncinaRedisPubSub();

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = provider.CreateScope();

        provider.GetRequiredService<TimeProvider>().ShouldBeSameAs(TimeProvider.System);
        scope.ServiceProvider.GetRequiredService<IRedisPubSubMessagePublisher>().ShouldNotBeNull();
    }

    private sealed record Payload(string Value);
}
