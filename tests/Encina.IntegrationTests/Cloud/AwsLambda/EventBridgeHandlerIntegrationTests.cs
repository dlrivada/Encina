using Amazon.Lambda.CloudWatchEvents;
using Encina.AwsLambda;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Testing;
using static Encina.IntegrationTests.Cloud.AwsLambda.AwsLambdaTestSupport;

namespace Encina.IntegrationTests.Cloud.AwsLambda;

/// <summary>
/// Drives <see cref="EventBridgeHandler"/> with EventBridge events deserialized by the Lambda runtime serializer and the real Encina pipeline.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Cloud", "AwsLambda")]
public sealed class EventBridgeHandlerIntegrationTests
{
    private const string OrderDetailJson = """{"OrderId":"o-1","Amount":10.5}""";

    private static readonly System.Text.Json.JsonSerializerOptions WebOptions = new(System.Text.Json.JsonSerializerDefaults.Web);

    [Fact]
    public async Task ProcessAsync_WithADeserializedEvent_RunsTheRealHandlerAndReturnsItsResult()
    {
        // Arrange
        await using var provider = CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var ledger = provider.GetRequiredService<OrderLedger>();
        var eventBridgeEvent = Deserialize<CloudWatchEvent<PlaceOrder>>(EventBridgeJson(OrderDetailJson));

        // Act
        var result = await EventBridgeHandler.ProcessAsync<PlaceOrder, OrderReceipt>(
            eventBridgeEvent,
            detail => encina.Send(detail).AsTask());

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(receipt => receipt.ShouldBe(new OrderReceipt("o-1", "Placed")));
        ledger.Entries.ShouldBe(["placed:o-1"]);
    }

    [Fact]
    public async Task ProcessAsync_WhenThePipelineReturnsAnError_ReturnsTheErrorAndLogsItsCode()
    {
        // Arrange
        await using var provider = CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var eventBridgeEvent = Deserialize<CloudWatchEvent<PlaceOrder>>(
            EventBridgeJson("""{"OrderId":"duplicate","Amount":1}"""));
        var logger = new FakeLogger();

        // Act
        var result = await EventBridgeHandler.ProcessAsync<PlaceOrder, OrderReceipt>(
            eventBridgeEvent,
            detail => encina.Send(detail).AsTask(),
            logger);

        // Assert
        result.IfLeft(error => error.GetCode().IfNone(string.Empty).ShouldBe("order.already_exists"));
        result.IsLeft.ShouldBeTrue();
        var entry = logger.Collector.GetSnapshot().ShouldHaveSingleItem();
        entry.Level.ShouldBe(LogLevel.Error);
        entry.Message.ShouldContain("order.already_exists");
        entry.Message.ShouldNotContain("The order already exists.");
    }

    [Fact]
    public async Task ProcessAsync_WithoutAnEventDetail_ReturnsTheNullDetailError()
    {
        // Arrange
        await using var provider = CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var eventBridgeEvent = Deserialize<CloudWatchEvent<PlaceOrder>>(EventBridgeJson("null"));

        // Act
        var result = await EventBridgeHandler.ProcessAsync<PlaceOrder, OrderReceipt>(
            eventBridgeEvent,
            detail => encina.Send(detail).AsTask());

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.GetCode().IfNone(string.Empty).ShouldBe("eventbridge.detail_null"));
    }

    [Fact]
    public async Task ProcessAsync_WhenTheHandlerThrows_NeverReportsSuccess()
    {
        // Arrange
        await using var provider = CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var eventBridgeEvent = Deserialize<CloudWatchEvent<PlaceOrder>>(
            EventBridgeJson($$"""{"OrderId":"{{PlaceOrderHandler.ExplodingOrderId}}","Amount":1}"""));

        // Act
        var result = await EventBridgeHandler.ProcessAsync<PlaceOrder, OrderReceipt>(
            eventBridgeEvent,
            detail => encina.Send(detail).AsTask());

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.GetCode().IfNone(string.Empty).ShouldNotBeNullOrEmpty());
    }

    [Fact]
    public async Task ProcessAsync_WhenTheProcessingDelegateThrows_ReturnsTheProcessingFailedError()
    {
        // Arrange
        var eventBridgeEvent = Deserialize<CloudWatchEvent<PlaceOrder>>(EventBridgeJson(OrderDetailJson));

        // Act
        var result = await EventBridgeHandler.ProcessAsync<PlaceOrder, OrderReceipt>(
            eventBridgeEvent,
            _ => throw new InvalidOperationException("boom"));

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.GetCode().IfNone(string.Empty).ShouldBe("eventbridge.processing_failed"));
    }

    [Fact]
    public async Task ProcessAsync_WithAUnitResult_ReturnsUnitAfterTheCommandRan()
    {
        // Arrange
        await using var provider = CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var ledger = provider.GetRequiredService<OrderLedger>();
        var eventBridgeEvent = Deserialize<CloudWatchEvent<CancelOrder>>(EventBridgeJson("""{"OrderId":"o-9"}"""));

        // Act
        var result = await EventBridgeHandler.ProcessAsync(
            eventBridgeEvent,
            detail => encina.Send(detail).AsTask());

        // Assert
        result.IsRight.ShouldBeTrue();
        ledger.Entries.ShouldBe(["cancelled:o-9"]);
    }

    [Fact]
    public async Task ProcessRawAsync_WithEventJson_DeserializesAndRunsThePipeline()
    {
        // Arrange
        await using var provider = CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var ledger = provider.GetRequiredService<OrderLedger>();

        // Act
        var result = await EventBridgeHandler.ProcessRawAsync<PlaceOrder, OrderReceipt>(
            EventBridgeJson(OrderDetailJson),
            detail => encina.Send(detail).AsTask(),
            jsonOptions: WebOptions);

        // Assert
        result.IsRight.ShouldBeTrue();
        ledger.Entries.ShouldBe(["placed:o-1"]);
    }

    [Theory]
    [InlineData("{ not json")]
    [InlineData("null")]
    public async Task ProcessRawAsync_WithAnUnreadableEvent_ReturnsTheDeserializationError(string eventJson)
    {
        // Arrange
        await using var provider = CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();

        // Act
        var result = await EventBridgeHandler.ProcessRawAsync<PlaceOrder, OrderReceipt>(
            eventJson,
            detail => encina.Send(detail).AsTask());

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.GetCode().IfNone(string.Empty).ShouldBe("eventbridge.deserialization_failed"));
    }

    [Fact]
    public void GetMetadata_FromADeserializedEvent_ExposesTheEnvelopeFields()
    {
        // Arrange
        var eventBridgeEvent = Deserialize<CloudWatchEvent<PlaceOrder>>(EventBridgeJson(OrderDetailJson));

        // Act
        var metadata = EventBridgeHandler.GetMetadata(eventBridgeEvent);

        // Assert
        metadata.Id.ShouldBe("evt-1");
        metadata.Source.ShouldBe("custom.orders");
        metadata.DetailType.ShouldBe("OrderPlaced");
        metadata.Account.ShouldBe("000000000000");
        metadata.Region.ShouldBe("eu-west-1");
        metadata.Time.ToUniversalTime().ShouldBe(new DateTime(2026, 10, 28, 10, 0, 0, DateTimeKind.Utc));
    }
}
