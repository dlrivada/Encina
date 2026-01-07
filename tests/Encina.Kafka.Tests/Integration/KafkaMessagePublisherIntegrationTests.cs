using Confluent.Kafka;
using Encina.Testing;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Encina.Kafka.Tests.Integration;

/// <summary>
/// Integration tests for <see cref="KafkaMessagePublisher"/> using Testcontainers.
/// </summary>
[Collection(KafkaCollection.Name)]
[Trait("Category", "Integration")]
public sealed class KafkaMessagePublisherIntegrationTests : IAsyncLifetime
{
    private readonly KafkaFixture _fixture;
    private readonly ILogger<KafkaMessagePublisher> _logger;
    private IProducer<string, byte[]>? _producer;
    private const string TestTopic = "test-topic";

    public KafkaMessagePublisherIntegrationTests(KafkaFixture fixture)
    {
        _fixture = fixture;
        _logger = Substitute.For<ILogger<KafkaMessagePublisher>>();
    }

    public Task InitializeAsync()
    {
        if (_fixture.IsAvailable)
        {
            var producerConfig = _fixture.CreateProducerConfig();
            _producer = new ProducerBuilder<string, byte[]>(producerConfig).Build();
        }

        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _producer?.Dispose();
        return Task.CompletedTask;
    }

    #region ProduceAsync Integration Tests

    [SkippableFact]
    public async Task ProduceAsync_WithRealKafka_ShouldSucceed()
    {
        Skip.IfNot(_fixture.IsAvailable, "Kafka is not available (Docker may not be running)");

        // Arrange
        var options = Options.Create(new EncinaKafkaOptions
        {
            DefaultEventTopic = TestTopic
        });
        var publisher = new KafkaMessagePublisher(_producer!, _logger, options);
        var message = new TestMessage { Id = 1, Content = "Integration Test" };

        // Act
        var result = await publisher.ProduceAsync(message);

        // Assert
        result.ShouldBeSuccess();
    }

    [SkippableFact]
    public async Task ProduceAsync_WithRealKafka_ShouldReturnDeliveryResult()
    {
        Skip.IfNot(_fixture.IsAvailable, "Kafka is not available (Docker may not be running)");

        // Arrange
        var options = Options.Create(new EncinaKafkaOptions
        {
            DefaultEventTopic = TestTopic
        });
        var publisher = new KafkaMessagePublisher(_producer!, _logger, options);
        var message = new TestMessage { Id = 2, Content = "Delivery Result Test" };

        // Act
        var result = await publisher.ProduceAsync(message);

        // Assert
        result.ShouldBeSuccessAnd()
            .ShouldSatisfy(deliveryResult =>
            {
                deliveryResult.Topic.ShouldBe(TestTopic);
                deliveryResult.Partition.ShouldBeGreaterThanOrEqualTo(0);
                deliveryResult.Offset.ShouldBeGreaterThanOrEqualTo(0);
            });
    }

    [SkippableFact]
    public async Task ProduceAsync_WithCustomTopic_ShouldSucceed()
    {
        Skip.IfNot(_fixture.IsAvailable, "Kafka is not available (Docker may not be running)");

        // Arrange
        var customTopic = "custom-topic";
        var options = Options.Create(new EncinaKafkaOptions
        {
            DefaultEventTopic = TestTopic
        });
        var publisher = new KafkaMessagePublisher(_producer!, _logger, options);
        var message = new TestMessage { Id = 3, Content = "Custom Topic Test" };

        // Act
        var result = await publisher.ProduceAsync(message, customTopic);

        // Assert
        result.ShouldBeSuccessAnd()
            .ShouldSatisfy(deliveryResult =>
            {
                deliveryResult.Topic.ShouldBe(customTopic);
            });
    }

    [SkippableFact]
    public async Task ProduceAsync_WithCustomKey_ShouldSucceed()
    {
        Skip.IfNot(_fixture.IsAvailable, "Kafka is not available (Docker may not be running)");

        // Arrange
        var options = Options.Create(new EncinaKafkaOptions
        {
            DefaultEventTopic = TestTopic
        });
        var publisher = new KafkaMessagePublisher(_producer!, _logger, options);
        var message = new TestMessage { Id = 4, Content = "Custom Key Test" };
        var customKey = "my-custom-key";

        // Act
        var result = await publisher.ProduceAsync(message, key: customKey);

        // Assert
        result.ShouldBeSuccess();
    }

    #endregion

    #region ProduceBatchAsync Integration Tests

    [SkippableFact]
    public async Task ProduceBatchAsync_WithRealKafka_ShouldSucceed()
    {
        Skip.IfNot(_fixture.IsAvailable, "Kafka is not available (Docker may not be running)");

        // Arrange
        var options = Options.Create(new EncinaKafkaOptions
        {
            DefaultEventTopic = TestTopic
        });
        var publisher = new KafkaMessagePublisher(_producer!, _logger, options);
        var messages = new[]
        {
            (new TestMessage { Id = 10, Content = "Batch 1" }, (string?)null),
            (new TestMessage { Id = 11, Content = "Batch 2" }, (string?)null),
            (new TestMessage { Id = 12, Content = "Batch 3" }, (string?)null)
        };

        // Act
        var result = await publisher.ProduceBatchAsync(messages);

        // Assert
        result.ShouldBeSuccess();
    }

    [SkippableFact]
    public async Task ProduceBatchAsync_WithRealKafka_ShouldReturnAllDeliveryResults()
    {
        Skip.IfNot(_fixture.IsAvailable, "Kafka is not available (Docker may not be running)");

        // Arrange
        var options = Options.Create(new EncinaKafkaOptions
        {
            DefaultEventTopic = TestTopic
        });
        var publisher = new KafkaMessagePublisher(_producer!, _logger, options);
        var messages = new[]
        {
            (new TestMessage { Id = 20, Content = "Batch A" }, (string?)"key-a"),
            (new TestMessage { Id = 21, Content = "Batch B" }, (string?)"key-b"),
            (new TestMessage { Id = 22, Content = "Batch C" }, (string?)"key-c")
        };

        // Act
        var result = await publisher.ProduceBatchAsync(messages);

        // Assert
        result.ShouldBeSuccessAnd()
            .ShouldSatisfy(results =>
            {
                results.Count.ShouldBe(3);
            });
    }

    #endregion

    #region ProduceWithHeadersAsync Integration Tests

    [SkippableFact]
    public async Task ProduceWithHeadersAsync_WithRealKafka_ShouldSucceed()
    {
        Skip.IfNot(_fixture.IsAvailable, "Kafka is not available (Docker may not be running)");

        // Arrange
        var options = Options.Create(new EncinaKafkaOptions
        {
            DefaultEventTopic = TestTopic
        });
        var publisher = new KafkaMessagePublisher(_producer!, _logger, options);
        var message = new TestMessage { Id = 30, Content = "Headers Test" };
        var headers = new Dictionary<string, byte[]>
        {
            ["X-Correlation-Id"] = System.Text.Encoding.UTF8.GetBytes("correlation-123"),
            ["X-Source"] = System.Text.Encoding.UTF8.GetBytes("integration-test")
        };

        // Act
        var result = await publisher.ProduceWithHeadersAsync(message, headers);

        // Assert
        result.ShouldBeSuccess();
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
