using System.Text.Json.Serialization;
using Amazon.Lambda.CloudWatchEvents;
using Encina.TestInfrastructure.Extensions;
using Shouldly;
using LanguageExt;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Encina.AwsLambda.Tests;

public class EventBridgeHandlerTests
{
    private readonly ILogger _logger;

    public EventBridgeHandlerTests()
    {
        _logger = Substitute.For<ILogger>();
    }

    [Fact]
    public async Task ProcessAsync_WithValidEvent_ReturnsSuccess()
    {
        // Arrange
        var eventBridgeEvent = CreateEvent(new TestEvent { OrderId = "order-123" });

        // Act
        var result = await EventBridgeHandler.ProcessAsync<TestEvent, string>(
            eventBridgeEvent,
            detail => Task.FromResult(Either<EncinaError, string>.Right(detail.OrderId)),
            _logger);

        // Assert
        result.ShouldBeRight("order-123");
    }

    [Fact]
    public async Task ProcessAsync_WithNullDetail_ReturnsError()
    {
        // Arrange
        var eventBridgeEvent = new CloudWatchEvent<TestEvent>
        {
            Id = "event-1",
            Source = "test.source",
            Detail = null!
        };

        // Act
        var result = await EventBridgeHandler.ProcessAsync<TestEvent, string>(
            eventBridgeEvent,
            detail => Task.FromResult(Either<EncinaError, string>.Right(detail.OrderId)),
            _logger);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var error = result.LeftToSeq().Single();
        var code = error.GetCode();
        code.IsSome.ShouldBeTrue();
        code.IfSome(c => c.ShouldBe("eventbridge.detail_null"));
    }

    [Fact]
    public async Task ProcessAsync_WithProcessingError_ReturnsError()
    {
        // Arrange
        var eventBridgeEvent = CreateEvent(new TestEvent { OrderId = "order-123" });
        var expectedError = EncinaErrors.Create("order.failed", "Order processing failed");

        // Act
        var result = await EventBridgeHandler.ProcessAsync<TestEvent, string>(
            eventBridgeEvent,
            _ => Task.FromResult(Either<EncinaError, string>.Left(expectedError)),
            _logger);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var error = result.LeftToSeq().Single();
        var code = error.GetCode();
        code.IsSome.ShouldBeTrue();
        code.IfSome(c => c.ShouldBe("order.failed"));
    }

    [Fact]
    public async Task ProcessAsync_WithException_ReturnsError()
    {
        // Arrange
        var eventBridgeEvent = CreateEvent(new TestEvent { OrderId = "order-123" });

        // Act
        var result = await EventBridgeHandler.ProcessAsync<TestEvent, string>(
            eventBridgeEvent,
            _ => throw new InvalidOperationException("Test exception"),
            _logger);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var error = result.LeftToSeq().Single();
        var code = error.GetCode();
        code.IsSome.ShouldBeTrue();
        code.IfSome(c => c.ShouldBe("eventbridge.processing_failed"));
    }

    [Fact]
    public async Task ProcessAsync_UnitOverload_WithSuccess_ReturnsUnit()
    {
        // Arrange
        var eventBridgeEvent = CreateEvent(new TestEvent { OrderId = "order-123" });

        // Act
        var result = await EventBridgeHandler.ProcessAsync<TestEvent>(
            eventBridgeEvent,
            _ => Task.FromResult(Either<EncinaError, Unit>.Right(Unit.Default)),
            _logger);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task ProcessRawAsync_WithValidJson_ReturnsSuccess()
    {
        // Arrange - Use PascalCase matching CloudWatchEvent<T> properties
        var json = """
        {
            "Id": "event-1",
            "Source": "test.source",
            "DetailType": "OrderCreated",
            "Account": "123456789012",
            "Region": "us-east-1",
            "Time": "2024-01-01T00:00:00Z",
            "Detail": { "orderId": "order-123" }
        }
        """;

        // Act
        var result = await EventBridgeHandler.ProcessRawAsync<TestEvent, string>(
            json,
            detail => Task.FromResult(Either<EncinaError, string>.Right(detail.OrderId)),
            _logger);

        // Assert
        result.IsRight.ShouldBeTrue();
        var value = result.RightToSeq().Single();
        value.ShouldBe("order-123");
    }

    [Fact]
    public async Task ProcessRawAsync_WithInvalidJson_ReturnsError()
    {
        // Arrange
        var invalidJson = "not valid json";

        // Act
        var result = await EventBridgeHandler.ProcessRawAsync<TestEvent, string>(
            invalidJson,
            detail => Task.FromResult(Either<EncinaError, string>.Right(detail.OrderId)),
            _logger);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.GetCode().IfSome(code => code.ShouldBe("eventbridge.deserialization_failed")));
    }

    [Fact]
    public void GetMetadata_ReturnsCorrectMetadata()
    {
        // Arrange
        var eventTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var eventBridgeEvent = new CloudWatchEvent<TestEvent>
        {
            Id = "event-123",
            Source = "test.source",
            DetailType = "OrderCreated",
            Account = "123456789012",
            Region = "us-east-1",
            Time = eventTime,
            Detail = new TestEvent { OrderId = "order-123" }
        };

        // Act
        var metadata = EventBridgeHandler.GetMetadata(eventBridgeEvent);

        // Assert
        metadata.Id.ShouldBe("event-123");
        metadata.Source.ShouldBe("test.source");
        metadata.DetailType.ShouldBe("OrderCreated");
        metadata.Account.ShouldBe("123456789012");
        metadata.Region.ShouldBe("us-east-1");
        metadata.Time.ShouldBe(eventTime);
    }

    [Fact]
    public async Task ProcessAsync_WithNullEvent_ThrowsArgumentNullException()
    {
        // Act
        var action = () => EventBridgeHandler.ProcessAsync<TestEvent, string>(
            null!,
            _ => Task.FromResult(Either<EncinaError, string>.Right("ok")));

        // Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(action);
        ex.ParamName.ShouldBe("eventBridgeEvent");
    }

    [Fact]
    public async Task ProcessAsync_WithNullProcessor_ThrowsArgumentNullException()
    {
        // Arrange
        var eventBridgeEvent = CreateEvent(new TestEvent { OrderId = "order-123" });

        // Act
        var action = () => EventBridgeHandler.ProcessAsync<TestEvent, string>(
            eventBridgeEvent,
            null!);

        // Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(action);
        ex.ParamName.ShouldBe("processEvent");
    }

    [Fact]
    public async Task ProcessRawAsync_WithNullJson_ThrowsArgumentNullException()
    {
        // Act
        var action = () => EventBridgeHandler.ProcessRawAsync<TestEvent, string>(
            null!,
            _ => Task.FromResult(Either<EncinaError, string>.Right("ok")));

        // Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(action);
        ex.ParamName.ShouldBe("eventJson");
    }

    [Fact]
    public void GetMetadata_WithNullEvent_ThrowsArgumentNullException()
    {
        // Act
        var action = () => EventBridgeHandler.GetMetadata<TestEvent>(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(action);
        ex.ParamName.ShouldBe("eventBridgeEvent");
    }

    private static CloudWatchEvent<TDetail> CreateEvent<TDetail>(TDetail detail)
    {
        return new CloudWatchEvent<TDetail>
        {
            Id = "event-1",
            Source = "test.source",
            DetailType = "TestEvent",
            Account = "123456789012",
            Region = "us-east-1",
            Time = DateTime.UtcNow,
            Detail = detail
        };
    }

    private sealed class TestEvent
    {
        [JsonPropertyName("orderId")]
        public string OrderId { get; set; } = string.Empty;
    }
}
