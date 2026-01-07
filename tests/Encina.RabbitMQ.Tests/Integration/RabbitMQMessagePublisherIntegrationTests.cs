using Encina.Testing;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using RabbitMQ.Client;

namespace Encina.RabbitMQ.Tests.Integration;

/// <summary>
/// Integration tests for <see cref="RabbitMQMessagePublisher"/> using Testcontainers.
/// </summary>
[Collection(RabbitMqCollection.Name)]
[Trait("Category", "Integration")]
public sealed class RabbitMQMessagePublisherIntegrationTests : IAsyncLifetime
{
    private readonly RabbitMqFixture _fixture;
    private readonly ILogger<RabbitMQMessagePublisher> _logger;
    private IConnection? _connection;
    private IChannel? _channel;
    private const string TestExchange = "test-exchange";
    private const string TestQueue = "test-queue";

    public RabbitMQMessagePublisherIntegrationTests(RabbitMqFixture fixture)
    {
        _fixture = fixture;
        _logger = Substitute.For<ILogger<RabbitMQMessagePublisher>>();
    }

    public async Task InitializeAsync()
    {
        if (_fixture.IsAvailable && _fixture.ConnectionFactory is not null)
        {
            _connection = await _fixture.ConnectionFactory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            // Declare exchange and queue for tests
            await _channel.ExchangeDeclareAsync(TestExchange, ExchangeType.Direct, durable: false, autoDelete: true);
            await _channel.QueueDeclareAsync(TestQueue, durable: false, exclusive: false, autoDelete: true);
            await _channel.QueueBindAsync(TestQueue, TestExchange, TestQueue);
        }
    }

    public async Task DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync();
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync();
        }
    }

    #region PublishAsync Integration Tests

    [SkippableFact]
    public async Task PublishAsync_WithRealRabbitMQ_ShouldSucceed()
    {
        Skip.IfNot(_fixture.IsAvailable, "RabbitMQ is not available (Docker may not be running)");

        // Arrange
        var options = Options.Create(new EncinaRabbitMQOptions
        {
            ExchangeName = TestExchange,
            Durable = false
        });
        var publisher = new RabbitMQMessagePublisher(_connection!, _channel!, _logger, options);
        var message = new TestMessage { Id = 1, Content = "Integration Test" };

        // Act
        var result = await publisher.PublishAsync(message, TestQueue);

        // Assert
        result.ShouldBeSuccess();
    }

    [SkippableFact]
    public async Task PublishAsync_MessageShouldBeReceivable()
    {
        Skip.IfNot(_fixture.IsAvailable, "RabbitMQ is not available (Docker may not be running)");

        // Arrange
        var options = Options.Create(new EncinaRabbitMQOptions
        {
            ExchangeName = TestExchange,
            Durable = false
        });
        var publisher = new RabbitMQMessagePublisher(_connection!, _channel!, _logger, options);
        var message = new TestMessage { Id = 2, Content = "Receivable Message" };

        // Act
        var publishResult = await publisher.PublishAsync(message, TestQueue);

        // Assert - Message was published successfully
        publishResult.ShouldBeSuccess();

        // Verify message can be received
        var basicGetResult = await _channel!.BasicGetAsync(TestQueue, autoAck: true);
        basicGetResult.ShouldNotBeNull();
    }

    #endregion

    #region SendToQueueAsync Integration Tests

    [SkippableFact]
    public async Task SendToQueueAsync_WithRealRabbitMQ_ShouldSucceed()
    {
        Skip.IfNot(_fixture.IsAvailable, "RabbitMQ is not available (Docker may not be running)");

        // Arrange
        var directQueue = "direct-test-queue";
        await _channel!.QueueDeclareAsync(directQueue, durable: false, exclusive: false, autoDelete: true);

        var options = Options.Create(new EncinaRabbitMQOptions
        {
            ExchangeName = TestExchange,
            Durable = false
        });
        var publisher = new RabbitMQMessagePublisher(_connection!, _channel!, _logger, options);
        var message = new TestMessage { Id = 3, Content = "Direct Queue Message" };

        // Act
        var result = await publisher.SendToQueueAsync(directQueue, message);

        // Assert
        result.ShouldBeSuccess();
    }

    [SkippableFact]
    public async Task SendToQueueAsync_MessageShouldBeReceivable()
    {
        Skip.IfNot(_fixture.IsAvailable, "RabbitMQ is not available (Docker may not be running)");

        // Arrange
        var directQueue = "direct-receive-queue";
        await _channel!.QueueDeclareAsync(directQueue, durable: false, exclusive: false, autoDelete: true);

        var options = Options.Create(new EncinaRabbitMQOptions
        {
            ExchangeName = TestExchange,
            Durable = false
        });
        var publisher = new RabbitMQMessagePublisher(_connection!, _channel!, _logger, options);
        var message = new TestMessage { Id = 4, Content = "Direct Receivable" };

        // Act
        var publishResult = await publisher.SendToQueueAsync(directQueue, message);

        // Assert
        publishResult.ShouldBeSuccess();

        // Verify message can be received
        var basicGetResult = await _channel!.BasicGetAsync(directQueue, autoAck: true);
        basicGetResult.ShouldNotBeNull();
    }

    #endregion

    #region Test Types

    private sealed record TestMessage
    {
        public int Id { get; init; }
        public string Content { get; init; } = string.Empty;
    }

    #endregion
}
