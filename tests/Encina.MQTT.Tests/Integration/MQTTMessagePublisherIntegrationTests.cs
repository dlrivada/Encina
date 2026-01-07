using Encina.Testing;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using NSubstitute;

namespace Encina.MQTT.Tests.Integration;

/// <summary>
/// Integration tests for <see cref="MQTTMessagePublisher"/> using Testcontainers.
/// </summary>
[Collection(MqttCollection.Name)]
[Trait("Category", "Integration")]
public sealed class MQTTMessagePublisherIntegrationTests : IAsyncLifetime
{
    private readonly MqttFixture _fixture;
    private readonly ILogger<MQTTMessagePublisher> _logger;

    public MQTTMessagePublisherIntegrationTests(MqttFixture fixture)
    {
        _fixture = fixture;
        _logger = Substitute.For<ILogger<MQTTMessagePublisher>>();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => Task.CompletedTask;

    #region PublishAsync Integration Tests

    [SkippableFact]
    public async Task PublishAsync_WithRealMQTT_ShouldSucceed()
    {
        Skip.IfNot(_fixture.IsAvailable, "MQTT is not available (Docker may not be running)");

        // Arrange
        var options = Options.Create(new EncinaMQTTOptions
        {
            TopicPrefix = "test",
            QualityOfService = MqttQualityOfService.AtLeastOnce
        });
        var publisher = new MQTTMessagePublisher(_fixture.Client!, _logger, options);
        var message = new TestMessage { Id = 1, Content = "Integration Test" };

        // Act
        var result = await publisher.PublishAsync(message);

        // Assert
        result.ShouldBeSuccess();
    }

    [SkippableFact]
    public async Task PublishAsync_WithCustomTopic_ShouldSucceed()
    {
        Skip.IfNot(_fixture.IsAvailable, "MQTT is not available (Docker may not be running)");

        // Arrange
        var options = Options.Create(new EncinaMQTTOptions
        {
            TopicPrefix = "test",
            QualityOfService = MqttQualityOfService.AtLeastOnce
        });
        var publisher = new MQTTMessagePublisher(_fixture.Client!, _logger, options);
        var message = new TestMessage { Id = 2, Content = "Custom Topic Test" };
        var customTopic = "sensors/temperature/room1";

        // Act
        var result = await publisher.PublishAsync(message, customTopic);

        // Assert
        result.ShouldBeSuccess();
    }

    [SkippableFact]
    public async Task PublishAsync_WithQoSAtMostOnce_ShouldSucceed()
    {
        Skip.IfNot(_fixture.IsAvailable, "MQTT is not available (Docker may not be running)");

        // Arrange
        var options = Options.Create(new EncinaMQTTOptions
        {
            TopicPrefix = "test",
            QualityOfService = MqttQualityOfService.AtLeastOnce
        });
        var publisher = new MQTTMessagePublisher(_fixture.Client!, _logger, options);
        var message = new TestMessage { Id = 3, Content = "QoS 0 Test" };

        // Act
        var result = await publisher.PublishAsync(message, qos: MqttQualityOfService.AtMostOnce);

        // Assert
        result.ShouldBeSuccess();
    }

    [SkippableFact]
    public async Task PublishAsync_WithQoSExactlyOnce_ShouldSucceed()
    {
        Skip.IfNot(_fixture.IsAvailable, "MQTT is not available (Docker may not be running)");

        // Arrange
        var options = Options.Create(new EncinaMQTTOptions
        {
            TopicPrefix = "test",
            QualityOfService = MqttQualityOfService.AtLeastOnce
        });
        var publisher = new MQTTMessagePublisher(_fixture.Client!, _logger, options);
        var message = new TestMessage { Id = 4, Content = "QoS 2 Test" };

        // Act
        var result = await publisher.PublishAsync(message, qos: MqttQualityOfService.ExactlyOnce);

        // Assert
        result.ShouldBeSuccess();
    }

    [SkippableFact]
    public async Task PublishAsync_WithRetainFlag_ShouldSucceed()
    {
        Skip.IfNot(_fixture.IsAvailable, "MQTT is not available (Docker may not be running)");

        // Arrange
        var options = Options.Create(new EncinaMQTTOptions
        {
            TopicPrefix = "test",
            QualityOfService = MqttQualityOfService.AtLeastOnce
        });
        var publisher = new MQTTMessagePublisher(_fixture.Client!, _logger, options);
        var message = new TestMessage { Id = 5, Content = "Retain Test" };

        // Act
        var result = await publisher.PublishAsync(message, retain: true);

        // Assert
        result.ShouldBeSuccess();
    }

    [SkippableFact]
    public async Task PublishAsync_MultipleMessages_ShouldAllSucceed()
    {
        Skip.IfNot(_fixture.IsAvailable, "MQTT is not available (Docker may not be running)");

        // Arrange
        var options = Options.Create(new EncinaMQTTOptions
        {
            TopicPrefix = "test",
            QualityOfService = MqttQualityOfService.AtLeastOnce
        });
        var publisher = new MQTTMessagePublisher(_fixture.Client!, _logger, options);

        // Act & Assert
        for (var i = 0; i < 10; i++)
        {
            var message = new TestMessage { Id = i, Content = $"Message {i}" };
            var result = await publisher.PublishAsync(message);
            result.ShouldBeSuccess();
        }
    }

    #endregion

    #region IsConnected Integration Tests

    [SkippableFact]
    public void IsConnected_WhenConnectedToRealBroker_ShouldReturnTrue()
    {
        Skip.IfNot(_fixture.IsAvailable, "MQTT is not available (Docker may not be running)");

        // Arrange
        var options = Options.Create(new EncinaMQTTOptions
        {
            TopicPrefix = "test",
            QualityOfService = MqttQualityOfService.AtLeastOnce
        });
        var publisher = new MQTTMessagePublisher(_fixture.Client!, _logger, options);

        // Act
        var isConnected = publisher.IsConnected;

        // Assert
        isConnected.ShouldBeTrue();
    }

    #endregion

    #region SubscribeAsync Integration Tests

    [SkippableFact]
    public async Task SubscribeAsync_WithValidTopic_ShouldReceiveMessages()
    {
        Skip.IfNot(_fixture.IsAvailable, "MQTT is not available (Docker may not be running)");

        // Arrange
        var options = Options.Create(new EncinaMQTTOptions
        {
            TopicPrefix = "test",
            QualityOfService = MqttQualityOfService.AtLeastOnce
        });
        var publisher = new MQTTMessagePublisher(_fixture.Client!, _logger, options);
        var receivedMessages = new List<TestMessage>();
        var messageReceivedEvent = new TaskCompletionSource<bool>();
        var topic = $"test/subscribe/{Guid.NewGuid():N}";

        // Act - Subscribe first
        await using var subscription = await publisher.SubscribeAsync<TestMessage>(
            async msg =>
            {
                receivedMessages.Add(msg);
                messageReceivedEvent.TrySetResult(true);
                await ValueTask.CompletedTask;
            },
            topic);

        // Give subscription time to register
        await Task.Delay(100);

        // Publish a message
        var message = new TestMessage { Id = 1, Content = "Subscribe Test" };
        var publishResult = await publisher.PublishAsync(message, topic);

        // Wait for message to be received (with timeout)
        var received = await Task.WhenAny(
            messageReceivedEvent.Task,
            Task.Delay(TimeSpan.FromSeconds(5))) == messageReceivedEvent.Task;

        // Assert
        publishResult.ShouldBeSuccess();
        received.ShouldBeTrue();
        receivedMessages.ShouldContain(m => m.Id == 1 && m.Content == "Subscribe Test");
    }

    [SkippableFact]
    public async Task SubscribeAsync_WithQoSAtMostOnce_ShouldReceiveMessages()
    {
        Skip.IfNot(_fixture.IsAvailable, "MQTT is not available (Docker may not be running)");

        // Arrange
        var options = Options.Create(new EncinaMQTTOptions
        {
            TopicPrefix = "test",
            QualityOfService = MqttQualityOfService.AtLeastOnce
        });
        var publisher = new MQTTMessagePublisher(_fixture.Client!, _logger, options);
        var receivedMessages = new List<TestMessage>();
        var messageReceivedEvent = new TaskCompletionSource<bool>();
        var topic = $"test/qos0/{Guid.NewGuid():N}";

        // Act - Subscribe with QoS 0
        await using var subscription = await publisher.SubscribeAsync<TestMessage>(
            async msg =>
            {
                receivedMessages.Add(msg);
                messageReceivedEvent.TrySetResult(true);
                await ValueTask.CompletedTask;
            },
            topic,
            MqttQualityOfService.AtMostOnce);

        await Task.Delay(100);

        var message = new TestMessage { Id = 2, Content = "QoS 0 Subscribe Test" };
        var publishResult = await publisher.PublishAsync(message, topic, MqttQualityOfService.AtMostOnce);

        var received = await Task.WhenAny(
            messageReceivedEvent.Task,
            Task.Delay(TimeSpan.FromSeconds(5))) == messageReceivedEvent.Task;

        // Assert
        publishResult.ShouldBeSuccess();
        received.ShouldBeTrue();
        receivedMessages.ShouldNotBeEmpty();
    }

    [SkippableFact]
    public async Task SubscribeAsync_WithQoSExactlyOnce_ShouldReceiveMessages()
    {
        Skip.IfNot(_fixture.IsAvailable, "MQTT is not available (Docker may not be running)");

        // Arrange
        var options = Options.Create(new EncinaMQTTOptions
        {
            TopicPrefix = "test",
            QualityOfService = MqttQualityOfService.AtLeastOnce
        });
        var publisher = new MQTTMessagePublisher(_fixture.Client!, _logger, options);
        var receivedMessages = new List<TestMessage>();
        var messageReceivedEvent = new TaskCompletionSource<bool>();
        var topic = $"test/qos2/{Guid.NewGuid():N}";

        // Act - Subscribe with QoS 2
        await using var subscription = await publisher.SubscribeAsync<TestMessage>(
            async msg =>
            {
                receivedMessages.Add(msg);
                messageReceivedEvent.TrySetResult(true);
                await ValueTask.CompletedTask;
            },
            topic,
            MqttQualityOfService.ExactlyOnce);

        await Task.Delay(100);

        var message = new TestMessage { Id = 3, Content = "QoS 2 Subscribe Test" };
        var publishResult = await publisher.PublishAsync(message, topic, MqttQualityOfService.ExactlyOnce);

        var received = await Task.WhenAny(
            messageReceivedEvent.Task,
            Task.Delay(TimeSpan.FromSeconds(5))) == messageReceivedEvent.Task;

        // Assert
        publishResult.ShouldBeSuccess();
        received.ShouldBeTrue();
        receivedMessages.ShouldNotBeEmpty();
    }

    [SkippableFact]
    public async Task SubscribeAsync_WhenDisposed_ShouldUnsubscribe()
    {
        Skip.IfNot(_fixture.IsAvailable, "MQTT is not available (Docker may not be running)");

        // Arrange
        var options = Options.Create(new EncinaMQTTOptions
        {
            TopicPrefix = "test",
            QualityOfService = MqttQualityOfService.AtLeastOnce
        });
        var publisher = new MQTTMessagePublisher(_fixture.Client!, _logger, options);
        var receivedCount = 0;
        var topic = $"test/unsubscribe/{Guid.NewGuid():N}";

        // Act - Subscribe
        var subscription = await publisher.SubscribeAsync<TestMessage>(
            _ =>
            {
                Interlocked.Increment(ref receivedCount);
                return ValueTask.CompletedTask;
            },
            topic);

        await Task.Delay(100);

        // Dispose (unsubscribe)
        await subscription.DisposeAsync();

        // Reset count and publish
        receivedCount = 0;
        var message = new TestMessage { Id = 4, Content = "After Unsubscribe" };
        await publisher.PublishAsync(message, topic);

        // Wait a bit to ensure message would have been received
        await Task.Delay(500);

        // Assert - Should not receive message after unsubscribe
        receivedCount.ShouldBe(0);
    }

    #endregion

    #region SubscribePatternAsync Integration Tests

    [SkippableFact]
    public async Task SubscribePatternAsync_WithHashWildcard_ShouldReceiveMessages()
    {
        Skip.IfNot(_fixture.IsAvailable, "MQTT is not available (Docker may not be running)");

        // Arrange
        var options = Options.Create(new EncinaMQTTOptions
        {
            TopicPrefix = "test",
            QualityOfService = MqttQualityOfService.AtLeastOnce
        });
        var publisher = new MQTTMessagePublisher(_fixture.Client!, _logger, options);
        var receivedMessages = new List<(string Topic, TestMessage Message)>();
        var messageReceivedEvent = new TaskCompletionSource<bool>();
        var topicBase = $"sensors/{Guid.NewGuid():N}";
        var topicPattern = $"{topicBase}/#";

        // Act - Subscribe with # wildcard
        await using var subscription = await publisher.SubscribePatternAsync<TestMessage>(
            async (topic, msg) =>
            {
                receivedMessages.Add((topic, msg));
                messageReceivedEvent.TrySetResult(true);
                await ValueTask.CompletedTask;
            },
            topicPattern);

        await Task.Delay(100);

        // Publish to matching topic
        var message = new TestMessage { Id = 1, Content = "Temperature Reading" };
        var publishTopic = $"{topicBase}/temperature/room1";
        var publishResult = await publisher.PublishAsync(message, publishTopic);

        var received = await Task.WhenAny(
            messageReceivedEvent.Task,
            Task.Delay(TimeSpan.FromSeconds(5))) == messageReceivedEvent.Task;

        // Assert
        publishResult.ShouldBeSuccess();
        received.ShouldBeTrue();
        receivedMessages.ShouldContain(m => m.Topic == publishTopic && m.Message.Id == 1);
    }

    [SkippableFact]
    public async Task SubscribePatternAsync_WithPlusWildcard_ShouldReceiveMessages()
    {
        Skip.IfNot(_fixture.IsAvailable, "MQTT is not available (Docker may not be running)");

        // Arrange
        var options = Options.Create(new EncinaMQTTOptions
        {
            TopicPrefix = "test",
            QualityOfService = MqttQualityOfService.AtLeastOnce
        });
        var publisher = new MQTTMessagePublisher(_fixture.Client!, _logger, options);
        var receivedMessages = new List<(string Topic, TestMessage Message)>();
        var messageReceivedEvent = new TaskCompletionSource<bool>();
        var topicBase = $"home/{Guid.NewGuid():N}";
        var topicPattern = $"{topicBase}/+/temperature";

        // Act - Subscribe with + wildcard
        await using var subscription = await publisher.SubscribePatternAsync<TestMessage>(
            async (topic, msg) =>
            {
                receivedMessages.Add((topic, msg));
                messageReceivedEvent.TrySetResult(true);
                await ValueTask.CompletedTask;
            },
            topicPattern);

        await Task.Delay(100);

        // Publish to matching topic
        var message = new TestMessage { Id = 2, Content = "Living Room Temp" };
        var publishTopic = $"{topicBase}/livingroom/temperature";
        var publishResult = await publisher.PublishAsync(message, publishTopic);

        var received = await Task.WhenAny(
            messageReceivedEvent.Task,
            Task.Delay(TimeSpan.FromSeconds(5))) == messageReceivedEvent.Task;

        // Assert
        publishResult.ShouldBeSuccess();
        received.ShouldBeTrue();
        receivedMessages.ShouldContain(m => m.Topic == publishTopic && m.Message.Id == 2);
    }

    [SkippableFact]
    public async Task SubscribePatternAsync_WithExactMatch_ShouldReceiveMessages()
    {
        Skip.IfNot(_fixture.IsAvailable, "MQTT is not available (Docker may not be running)");

        // Arrange
        var options = Options.Create(new EncinaMQTTOptions
        {
            TopicPrefix = "test",
            QualityOfService = MqttQualityOfService.AtLeastOnce
        });
        var publisher = new MQTTMessagePublisher(_fixture.Client!, _logger, options);
        var receivedMessages = new List<(string Topic, TestMessage Message)>();
        var messageReceivedEvent = new TaskCompletionSource<bool>();
        var topic = $"exact/match/{Guid.NewGuid():N}";

        // Act - Subscribe with exact match (no wildcards)
        await using var subscription = await publisher.SubscribePatternAsync<TestMessage>(
            async (topicReceived, msg) =>
            {
                receivedMessages.Add((topicReceived, msg));
                messageReceivedEvent.TrySetResult(true);
                await ValueTask.CompletedTask;
            },
            topic);

        await Task.Delay(100);

        // Publish to matching topic
        var message = new TestMessage { Id = 3, Content = "Exact Match" };
        var publishResult = await publisher.PublishAsync(message, topic);

        var received = await Task.WhenAny(
            messageReceivedEvent.Task,
            Task.Delay(TimeSpan.FromSeconds(5))) == messageReceivedEvent.Task;

        // Assert
        publishResult.ShouldBeSuccess();
        received.ShouldBeTrue();
        receivedMessages.ShouldContain(m => m.Topic == topic && m.Message.Id == 3);
    }

    [SkippableFact]
    public async Task SubscribePatternAsync_WithQoSAtMostOnce_ShouldReceiveMessages()
    {
        Skip.IfNot(_fixture.IsAvailable, "MQTT is not available (Docker may not be running)");

        // Arrange
        var options = Options.Create(new EncinaMQTTOptions
        {
            TopicPrefix = "test",
            QualityOfService = MqttQualityOfService.AtLeastOnce
        });
        var publisher = new MQTTMessagePublisher(_fixture.Client!, _logger, options);
        var receivedMessages = new List<(string Topic, TestMessage Message)>();
        var messageReceivedEvent = new TaskCompletionSource<bool>();
        var topicBase = $"qos0pattern/{Guid.NewGuid():N}";
        var topicPattern = $"{topicBase}/#";

        // Act - Subscribe with QoS 0
        await using var subscription = await publisher.SubscribePatternAsync<TestMessage>(
            async (topic, msg) =>
            {
                receivedMessages.Add((topic, msg));
                messageReceivedEvent.TrySetResult(true);
                await ValueTask.CompletedTask;
            },
            topicPattern,
            MqttQualityOfService.AtMostOnce);

        await Task.Delay(100);

        var message = new TestMessage { Id = 4, Content = "Pattern QoS 0" };
        var publishTopic = $"{topicBase}/test";
        var publishResult = await publisher.PublishAsync(message, publishTopic, MqttQualityOfService.AtMostOnce);

        var received = await Task.WhenAny(
            messageReceivedEvent.Task,
            Task.Delay(TimeSpan.FromSeconds(5))) == messageReceivedEvent.Task;

        // Assert
        publishResult.ShouldBeSuccess();
        received.ShouldBeTrue();
    }

    [SkippableFact]
    public async Task SubscribePatternAsync_WithQoSExactlyOnce_ShouldReceiveMessages()
    {
        Skip.IfNot(_fixture.IsAvailable, "MQTT is not available (Docker may not be running)");

        // Arrange
        var options = Options.Create(new EncinaMQTTOptions
        {
            TopicPrefix = "test",
            QualityOfService = MqttQualityOfService.AtLeastOnce
        });
        var publisher = new MQTTMessagePublisher(_fixture.Client!, _logger, options);
        var receivedMessages = new List<(string Topic, TestMessage Message)>();
        var messageReceivedEvent = new TaskCompletionSource<bool>();
        var topicBase = $"qos2pattern/{Guid.NewGuid():N}";
        var topicPattern = $"{topicBase}/#";

        // Act - Subscribe with QoS 2
        await using var subscription = await publisher.SubscribePatternAsync<TestMessage>(
            async (topic, msg) =>
            {
                receivedMessages.Add((topic, msg));
                messageReceivedEvent.TrySetResult(true);
                await ValueTask.CompletedTask;
            },
            topicPattern,
            MqttQualityOfService.ExactlyOnce);

        await Task.Delay(100);

        var message = new TestMessage { Id = 5, Content = "Pattern QoS 2" };
        var publishTopic = $"{topicBase}/test";
        var publishResult = await publisher.PublishAsync(message, publishTopic, MqttQualityOfService.ExactlyOnce);

        var received = await Task.WhenAny(
            messageReceivedEvent.Task,
            Task.Delay(TimeSpan.FromSeconds(5))) == messageReceivedEvent.Task;

        // Assert
        publishResult.ShouldBeSuccess();
        received.ShouldBeTrue();
    }

    [SkippableFact]
    public async Task SubscribePatternAsync_WhenDisposed_ShouldUnsubscribe()
    {
        Skip.IfNot(_fixture.IsAvailable, "MQTT is not available (Docker may not be running)");

        // Arrange
        var options = Options.Create(new EncinaMQTTOptions
        {
            TopicPrefix = "test",
            QualityOfService = MqttQualityOfService.AtLeastOnce
        });
        var publisher = new MQTTMessagePublisher(_fixture.Client!, _logger, options);
        var receivedCount = 0;
        var topicBase = $"patternunsub/{Guid.NewGuid():N}";
        var topicPattern = $"{topicBase}/#";

        // Act - Subscribe
        var subscription = await publisher.SubscribePatternAsync<TestMessage>(
            (_, _) =>
            {
                Interlocked.Increment(ref receivedCount);
                return ValueTask.CompletedTask;
            },
            topicPattern);

        await Task.Delay(100);

        // Dispose (unsubscribe)
        await subscription.DisposeAsync();

        // Reset count and publish
        receivedCount = 0;
        var message = new TestMessage { Id = 6, Content = "After Pattern Unsubscribe" };
        await publisher.PublishAsync(message, $"{topicBase}/test");

        // Wait a bit to ensure message would have been received
        await Task.Delay(500);

        // Assert - Should not receive message after unsubscribe
        receivedCount.ShouldBe(0);
    }

    [SkippableFact]
    public async Task SubscribePatternAsync_WithPlusWildcard_ShouldNotMatchDifferentLength()
    {
        Skip.IfNot(_fixture.IsAvailable, "MQTT is not available (Docker may not be running)");

        // Arrange
        var options = Options.Create(new EncinaMQTTOptions
        {
            TopicPrefix = "test",
            QualityOfService = MqttQualityOfService.AtLeastOnce
        });
        var publisher = new MQTTMessagePublisher(_fixture.Client!, _logger, options);
        var receivedCount = 0;
        var topicBase = $"pluswild/{Guid.NewGuid():N}";
        var topicPattern = $"{topicBase}/+/temp"; // Expects exactly 3 levels

        // Act - Subscribe with + wildcard
        await using var subscription = await publisher.SubscribePatternAsync<TestMessage>(
            (_, _) =>
            {
                Interlocked.Increment(ref receivedCount);
                return ValueTask.CompletedTask;
            },
            topicPattern);

        await Task.Delay(100);

        // Publish to non-matching topic (4 levels instead of 3)
        var message = new TestMessage { Id = 7, Content = "Non-matching" };
        var nonMatchingTopic = $"{topicBase}/room1/sensor/temp";
        await publisher.PublishAsync(message, nonMatchingTopic);

        // Wait a bit
        await Task.Delay(500);

        // Assert - Should not receive message (topic doesn't match pattern)
        receivedCount.ShouldBe(0);
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
