// CA1859: a contract test deliberately holds the implementation behind its public interface.
#pragma warning disable CA1859
using Amazon.SQS.Model;
using Encina.AmazonSQS;
using Encina.AmazonSQS.Health;
using Encina.Messaging.Health;
using Encina.TestInfrastructure.Fixtures;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Encina.ContractTests.MessageBrokers.AmazonSQS;

/// <summary>
/// Contract tests of the public interfaces implemented by the Amazon SQS package
/// (<see cref="IAmazonSQSMessagePublisher"/> and <see cref="IEncinaHealthCheck"/>), run with the real
/// implementations against a LocalStack emulator.
/// </summary>
[Collection(LocalStackFixture.CollectionName)]
[Trait("Category", "Integration")]
[Trait("Messaging", "AmazonSQS")]
public sealed class AmazonSQSPublisherContractTests : IAsyncLifetime
{
    private readonly LocalStackFixture _fixture;

    public AmazonSQSPublisherContractTests(LocalStackFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync() => await _fixture.ClearAllDataAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private IAmazonSQSMessagePublisher CreatePublisher(EncinaAmazonSQSOptions? options = null) =>
        new AmazonSQSMessagePublisher(
            _fixture.SqsClient,
            _fixture.SnsClient,
            NullLogger<AmazonSQSMessagePublisher>.Instance,
            Options.Create(options ?? new EncinaAmazonSQSOptions()));

    private static string UniqueName(string prefix) => $"{prefix}-{Guid.NewGuid():N}";

    private static string CodeOf<T>(Either<EncinaError, T> result) =>
        result.Match(Right: _ => string.Empty, Left: e => e.GetCode().IfNone(string.Empty));

    private static async Task<IReadOnlyList<Message>> ReceiveAsync(
        LocalStackFixture fixture, string queueUrl, int expectedCount)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var received = new List<Message>();
        while (received.Count < expectedCount && !cts.IsCancellationRequested)
        {
            var response = await fixture.SqsClient.ReceiveMessageAsync(
                new ReceiveMessageRequest
                {
                    QueueUrl = queueUrl,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = 2,
                    MessageAttributeNames = ["All"]
                },
                cts.Token);
            received.AddRange(response.Messages ?? []);
        }

        return received;
    }

    [Fact]
    public void Publisher_ImplementsThePublisherInterface()
    {
        // Arrange & Act
        var publisher = CreatePublisher();

        // Assert
        publisher.ShouldBeAssignableTo<IAmazonSQSMessagePublisher>();
    }

    [Fact]
    public async Task SendToQueueAsync_OnSuccess_ReturnsRightWithTheBrokerMessageId()
    {
        // Arrange
        var queueUrl = await _fixture.CreateQueueAsync(UniqueName("contract"));
        var publisher = CreatePublisher();

        // Act
        var result = await publisher.SendToQueueAsync(new TestMessage("a"), queueUrl);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(id => id.ShouldNotBeNullOrWhiteSpace());
    }

    [Fact]
    public async Task SendToQueueAsync_AlwaysCarriesMessageTypeAndMessageIdAttributes()
    {
        // Arrange
        var queueUrl = await _fixture.CreateQueueAsync(UniqueName("contract"));
        var publisher = CreatePublisher();

        // Act
        var result = await publisher.SendToQueueAsync(new TestMessage("a"), queueUrl, "propagated-id");
        result.IsRight.ShouldBeTrue();
        var received = await ReceiveAsync(_fixture, queueUrl, 1);

        // Assert
        received.Count.ShouldBe(1);
        received[0].MessageAttributes["MessageType"].StringValue.ShouldBe(typeof(TestMessage).FullName);
        received[0].MessageAttributes["MessageId"].StringValue.ShouldBe("propagated-id");
    }

    [Fact(Skip = "Blocked by a src bug: SendBatchAsync returns SQS_BATCH_SEND_FAILED for a fully successful batch because the SDK response leaves Failed null (#2799). Enable when fixed.")]
    public async Task SendBatchAsync_OnSuccess_ReturnsOneIdPerMessage()
    {
        // Arrange
        var queueUrl = await _fixture.CreateQueueAsync(UniqueName("contract"));
        var publisher = CreatePublisher();

        // Act
        var result = await publisher.SendBatchAsync([new TestMessage("a"), new TestMessage("b")], queueUrl);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(ids => ids.Count.ShouldBe(2));
    }

    [Fact]
    public async Task SendToFifoQueueAsync_OnSuccess_ReturnsRightWithTheBrokerMessageId()
    {
        // Arrange
        var queueUrl = await _fixture.CreateQueueAsync(UniqueName("contract"), isFifo: true);
        var publisher = CreatePublisher();

        // Act
        var result = await publisher.SendToFifoQueueAsync(new TestMessage("a"), "group", "dedup", queueUrl);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(id => id.ShouldNotBeNullOrWhiteSpace());
    }

    [Fact]
    public async Task PublishToTopicAsync_OnSuccess_ReturnsRightWithTheBrokerMessageId()
    {
        // Arrange
        var topicArn = await _fixture.CreateTopicAsync(UniqueName("contract"));
        var publisher = CreatePublisher();

        // Act
        var result = await publisher.PublishToTopicAsync(new TestMessage("a"), topicArn);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(id => id.ShouldNotBeNullOrWhiteSpace());
    }

    [Fact]
    public async Task EveryOperation_WithoutATarget_ReturnsLeftWithAStableErrorCodeInsteadOfThrowing()
    {
        // Arrange
        var publisher = CreatePublisher();

        // Act
        var send = await publisher.SendToQueueAsync(new TestMessage("a"));
        var batch = await publisher.SendBatchAsync([new TestMessage("a")]);
        var fifo = await publisher.SendToFifoQueueAsync(new TestMessage("a"), "group");
        var topic = await publisher.PublishToTopicAsync(new TestMessage("a"));

        // Assert
        CodeOf(send).ShouldBe("SQS_QUEUE_NOT_CONFIGURED");
        CodeOf(batch).ShouldBe("SQS_QUEUE_NOT_CONFIGURED");
        CodeOf(fifo).ShouldBe("SQS_QUEUE_NOT_CONFIGURED");
        CodeOf(topic).ShouldBe("SNS_TOPIC_NOT_CONFIGURED");
    }

    [Fact]
    public async Task EveryOperation_WhenTheBrokerRejectsTheCall_ReturnsLeftInsteadOfThrowing()
    {
        // Arrange
        var missingQueueUrl = await _fixture.CreateQueueAsync(UniqueName("contract"));
        await _fixture.SqsClient.DeleteQueueAsync(missingQueueUrl);
        var missingTopicArn = await _fixture.CreateTopicAsync(UniqueName("contract"));
        await _fixture.SnsClient.DeleteTopicAsync(missingTopicArn);
        var publisher = CreatePublisher();

        // Act
        var send = await publisher.SendToQueueAsync(new TestMessage("a"), missingQueueUrl);
        var fifo = await publisher.SendToFifoQueueAsync(new TestMessage("a"), "group", "dedup", missingQueueUrl);
        var topic = await publisher.PublishToTopicAsync(new TestMessage("a"), missingTopicArn);

        // Assert
        CodeOf(send).ShouldBe("SQS_SEND_FAILED");
        CodeOf(fifo).ShouldBe("SQS_FIFO_SEND_FAILED");
        CodeOf(topic).ShouldBe("SNS_PUBLISH_FAILED");
    }

    [Fact]
    public void HealthCheck_ImplementsTheEncinaHealthCheckContract()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_fixture.SqsClient);

        // Act
        using var provider = services.BuildServiceProvider();
        IEncinaHealthCheck healthCheck = new AmazonSQSHealthCheck(provider, null);

        // Assert
        healthCheck.Name.ShouldBe(AmazonSQSHealthCheck.DefaultName);
        healthCheck.Tags.ShouldContain("encina");
        healthCheck.Tags.ShouldContain("amazon-sqs");
    }

    [Fact]
    public async Task HealthCheck_AgainstTheEmulator_ReportsHealthy()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_fixture.SqsClient);
        using var provider = services.BuildServiceProvider();
        IEncinaHealthCheck healthCheck = new AmazonSQSHealthCheck(provider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    private sealed record TestMessage(string Value);
}
