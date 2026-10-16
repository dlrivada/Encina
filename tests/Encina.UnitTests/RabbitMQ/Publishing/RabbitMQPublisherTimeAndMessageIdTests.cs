using Encina.RabbitMQ;
using Microsoft.Extensions.Time.Testing;
using RabbitMQ.Client;

namespace Encina.UnitTests.RabbitMQ.Publishing;

/// <summary>
/// Regression tests for #1792: the publisher reads time from the injected <see cref="TimeProvider"/>
/// and keeps the caller-supplied message id (for example the outbox id) as the broker MessageId.
/// </summary>
public sealed class RabbitMQPublisherTimeAndMessageIdTests
{
    private static readonly DateTimeOffset FixedNow = new(2031, 5, 17, 8, 30, 0, TimeSpan.Zero);

    private readonly IChannel _channel = Substitute.For<IChannel>();
    private readonly FakeTimeProvider _timeProvider = new(FixedNow);

    private RabbitMQMessagePublisher CreatePublisher(TimeProvider? timeProvider = null) =>
        new(
            _channel,
            Substitute.For<ILogger<RabbitMQMessagePublisher>>(),
            Options.Create(new EncinaRabbitMQOptions { ExchangeName = "ex", Durable = true }),
            timeProvider ?? _timeProvider);

    private IReadOnlyBasicProperties CapturedProperties() =>
        _channel.ReceivedCalls()
            .Single(c => c.GetMethodInfo().Name == nameof(IChannel.BasicPublishAsync))
            .GetArguments()
            .OfType<IReadOnlyBasicProperties>()
            .Single();

    [Fact]
    public async Task PublishAsync_UsesInjectedTimeProviderForTimestamp()
    {
        var result = await CreatePublisher().PublishAsync(new Payload("a"));

        result.IsRight.ShouldBeTrue();
        CapturedProperties().Timestamp.UnixTime.ShouldBe(FixedNow.ToUnixTimeSeconds());
    }

    [Fact]
    public async Task SendToQueueAsync_UsesInjectedTimeProviderForTimestamp()
    {
        var result = await CreatePublisher().SendToQueueAsync("q", new Payload("a"));

        result.IsRight.ShouldBeTrue();
        CapturedProperties().Timestamp.UnixTime.ShouldBe(FixedNow.ToUnixTimeSeconds());
    }

    [Fact]
    public async Task PublishAsync_WithMessageId_PreservesItAsBrokerMessageId()
    {
        var id = Guid.NewGuid().ToString();

        await CreatePublisher().PublishAsync(new Payload("a"), messageId: id);

        CapturedProperties().MessageId.ShouldBe(id);
    }

    [Fact]
    public async Task SendToQueueAsync_WithMessageId_PreservesItAsBrokerMessageId()
    {
        var id = Guid.NewGuid().ToString();

        await CreatePublisher().SendToQueueAsync("q", new Payload("a"), id);

        CapturedProperties().MessageId.ShouldBe(id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task PublishAsync_WithoutMessageId_GeneratesANewGuid(string? messageId)
    {
        await CreatePublisher().PublishAsync(new Payload("a"), messageId: messageId);

        Guid.TryParse(CapturedProperties().MessageId, out _).ShouldBeTrue();
    }

    [Fact]
    public async Task PublishAsync_WithoutTimeProvider_FallsBackToSystemTime()
    {
        var publisher = new RabbitMQMessagePublisher(
            _channel,
            Substitute.For<ILogger<RabbitMQMessagePublisher>>(),
            Options.Create(new EncinaRabbitMQOptions()));
        var before = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        await publisher.PublishAsync(new Payload("a"));

        CapturedProperties().Timestamp.UnixTime.ShouldBeGreaterThanOrEqualTo(before);
    }

    [Fact]
    public void AddEncinaRabbitMQ_RegistersTimeProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IConnection>());
        services.AddSingleton(_channel);
        services.AddEncinaRabbitMQ();

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = provider.CreateScope();

        provider.GetRequiredService<TimeProvider>().ShouldBeSameAs(TimeProvider.System);
        scope.ServiceProvider.GetRequiredService<IRabbitMQMessagePublisher>().ShouldNotBeNull();
    }

    private sealed record Payload(string Value);
}
