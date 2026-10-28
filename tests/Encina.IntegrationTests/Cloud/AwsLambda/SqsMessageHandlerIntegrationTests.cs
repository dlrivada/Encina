using System.Text.Json;
using Amazon.Lambda.SQSEvents;
using Encina.AwsLambda;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Testing;
using static Encina.IntegrationTests.Cloud.AwsLambda.AwsLambdaTestSupport;

namespace Encina.IntegrationTests.Cloud.AwsLambda;

/// <summary>
/// Drives <see cref="SqsMessageHandler"/> with SQS events deserialized by the Lambda runtime serializer and the real Encina pipeline.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Cloud", "AwsLambda")]
public sealed class SqsMessageHandlerIntegrationTests
{
    private static readonly JsonSerializerOptions CamelCase = new(JsonSerializerDefaults.Web);

    private static string Body(string orderId, decimal amount) =>
        JsonSerializer.Serialize(new PlaceOrder(orderId, amount), CamelCase);

    [Fact]
    public async Task ProcessBatchAsync_WithMixedRecords_ReportsOnlyTheFailedMessageIds()
    {
        // Arrange
        await using var provider = CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var ledger = provider.GetRequiredService<OrderLedger>();
        var sqsEvent = Deserialize<SQSEvent>(SqsEventJson(
            ("m-1", Body("o-1", 10m)),
            ("m-2", Body("o-2", -5m)),
            ("m-3", Body(PlaceOrderHandler.DuplicateOrderId, 10m)),
            ("m-4", Body("o-4", 20m))));
        var logger = new FakeLogger();

        // Act
        var response = await SqsMessageHandler.ProcessBatchAsync<PlaceOrder, OrderReceipt>(
            sqsEvent,
            command => encina.Send(command).AsTask(),
            logger,
            CamelCase);

        // Assert
        response.BatchItemFailures.Select(f => f.ItemIdentifier).ShouldBe(["m-2", "m-3"]);
        ledger.Entries.ShouldBe(["placed:o-1", "placed:o-4"]);
        logger.Collector.GetSnapshot().Count(r => r.Level == LogLevel.Error).ShouldBe(2);
    }

    [Fact]
    public async Task ProcessBatchAsync_WithMalformedBody_ReportsTheMessageAndKeepsProcessing()
    {
        // Arrange
        await using var provider = CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var ledger = provider.GetRequiredService<OrderLedger>();
        var sqsEvent = Deserialize<SQSEvent>(SqsEventJson(
            ("m-1", "{ not json"),
            ("m-2", Body("o-2", 5m))));

        // Act
        var response = await SqsMessageHandler.ProcessBatchAsync<PlaceOrder, OrderReceipt>(
            sqsEvent,
            command => encina.Send(command).AsTask(),
            jsonOptions: CamelCase);

        // Assert
        response.BatchItemFailures.Select(f => f.ItemIdentifier).ShouldBe(["m-1"]);
        ledger.Entries.ShouldBe(["placed:o-2"]);
    }

    [Fact]
    public async Task ProcessBatchAsync_WithNullJsonBody_ReportsTheMessage()
    {
        // Arrange
        await using var provider = CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var sqsEvent = Deserialize<SQSEvent>(SqsEventJson(("m-1", "null")));

        // Act
        var response = await SqsMessageHandler.ProcessBatchAsync<PlaceOrder, OrderReceipt>(
            sqsEvent,
            command => encina.Send(command).AsTask());

        // Assert
        response.BatchItemFailures.Select(f => f.ItemIdentifier).ShouldBe(["m-1"]);
    }

    [Fact]
    public async Task ProcessBatchAsync_WhenTheHandlerThrows_ReportsTheMessageInsteadOfFailingTheBatch()
    {
        // Arrange
        await using var provider = CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var ledger = provider.GetRequiredService<OrderLedger>();
        var sqsEvent = Deserialize<SQSEvent>(SqsEventJson(
            ("m-1", Body(PlaceOrderHandler.ExplodingOrderId, 1m)),
            ("m-2", Body("o-2", 5m))));
        var logger = new FakeLogger();

        // Act
        var response = await SqsMessageHandler.ProcessBatchAsync(
            sqsEvent,
            record => encina.Send(JsonSerializer.Deserialize<PlaceOrder>(record.Body, CamelCase)!).AsTask(),
            logger);

        // Assert
        response.BatchItemFailures.Select(f => f.ItemIdentifier).ShouldBe(["m-1"]);
        ledger.Entries.ShouldBe(["placed:o-2"]);
        logger.Collector.GetSnapshot().ShouldContain(r => r.Level == LogLevel.Error);
    }

    [Fact]
    public async Task ProcessBatchAsync_WhenTheProcessingDelegateThrows_ReportsTheMessageAndKeepsProcessing()
    {
        // Arrange
        await using var provider = CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var ledger = provider.GetRequiredService<OrderLedger>();
        var sqsEvent = Deserialize<SQSEvent>(SqsEventJson(
            ("m-1", Body("o-1", 5m)),
            ("m-2", Body("o-2", 5m))));
        var logger = new FakeLogger();

        // Act
        var response = await SqsMessageHandler.ProcessBatchAsync(
            sqsEvent,
            record => record.MessageId == "m-1"
                ? throw new InvalidOperationException("secret detail")
                : encina.Send(JsonSerializer.Deserialize<PlaceOrder>(record.Body, CamelCase)!).AsTask(),
            logger);

        // Assert
        response.BatchItemFailures.Select(f => f.ItemIdentifier).ShouldBe(["m-1"]);
        ledger.Entries.ShouldBe(["placed:o-2"]);
        var failure = logger.Collector.GetSnapshot().ShouldHaveSingleItem();
        failure.Level.ShouldBe(LogLevel.Error);
        failure.Message.ShouldNotContain("secret detail");
    }

    [Fact]
    public async Task ProcessBatchAsync_WhenTheBatchIsCancelled_PropagatesTheCancellation()
    {
        // Arrange
        await using var provider = CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var sqsEvent = Deserialize<SQSEvent>(SqsEventJson(("m-1", Body("o-1", 5m))));
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var act = () => SqsMessageHandler.ProcessBatchAsync<PlaceOrder, OrderReceipt>(
            sqsEvent,
            command => encina.Send(command).AsTask(),
            cancellationToken: cts.Token);

        // Assert
        await Should.ThrowAsync<OperationCanceledException>(act);
    }

    [Fact]
    public async Task ProcessAllAsync_StopsAtTheFirstError()
    {
        // Arrange
        await using var provider = CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var ledger = provider.GetRequiredService<OrderLedger>();
        var sqsEvent = Deserialize<SQSEvent>(SqsEventJson(
            ("m-1", Body("o-1", 5m)),
            ("m-2", Body("o-2", 0m)),
            ("m-3", Body("o-3", 5m))));

        // Act
        var result = await SqsMessageHandler.ProcessAllAsync(
            sqsEvent,
            record => encina.Send(JsonSerializer.Deserialize<PlaceOrder>(record.Body, CamelCase)!).AsTask());

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.GetCode().IfNone(string.Empty).ShouldBe("validation.invalid_amount"));
        ledger.Entries.ShouldBe(["placed:o-1"]);
    }

    [Fact]
    public async Task ProcessAllAsync_WhenEveryRecordSucceeds_ReturnsUnit()
    {
        // Arrange
        await using var provider = CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var ledger = provider.GetRequiredService<OrderLedger>();
        var sqsEvent = Deserialize<SQSEvent>(SqsEventJson(
            ("m-1", Body("o-1", 5m)),
            ("m-2", Body("o-2", 6m))));

        // Act
        var result = await SqsMessageHandler.ProcessAllAsync(
            sqsEvent,
            record => encina.Send(JsonSerializer.Deserialize<PlaceOrder>(record.Body, CamelCase)!).AsTask());

        // Assert
        result.IsRight.ShouldBeTrue();
        ledger.Entries.ShouldBe(["placed:o-1", "placed:o-2"]);
    }

    [Fact]
    public void DeserializeMessage_WithAValidBody_ReturnsTheMessage()
    {
        // Arrange
        var sqsEvent = Deserialize<SQSEvent>(SqsEventJson(("m-1", Body("o-1", 12.5m))));

        // Act
        var result = SqsMessageHandler.DeserializeMessage<PlaceOrder>(sqsEvent.Records[0], CamelCase);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(message => message.ShouldBe(new PlaceOrder("o-1", 12.5m)));
    }

    [Theory]
    [InlineData("{ not json")]
    [InlineData("null")]
    public void DeserializeMessage_WithAnUnreadableBody_ReturnsTheDeserializationError(string body)
    {
        // Arrange
        var sqsEvent = Deserialize<SQSEvent>(SqsEventJson(("m-1", body)));

        // Act
        var result = SqsMessageHandler.DeserializeMessage<PlaceOrder>(sqsEvent.Records[0], CamelCase);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.GetCode().IfNone(string.Empty).ShouldBe("sqs.deserialization_failed"));
    }
}
