using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.CloudWatchEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Encina.AwsLambda;
using Microsoft.Extensions.Logging.Testing;

namespace Encina.UnitTests.AwsLambda;

/// <summary>
/// The category each error created by the EventBridge, SQS and tenant helpers of <c>Encina.AwsLambda</c> carries
/// (ADR-062), read back through <see cref="ErrorCategoryHttp"/> as the API Gateway helpers do.
/// </summary>
public class AwsLambdaErrorCategoryTests
{
    private sealed class TestEvent
    {
        public string OrderId { get; set; } = string.Empty;
    }

    private static EncinaError Left<T>(LanguageExt.Either<EncinaError, T> result) =>
        result.Match(Right: _ => throw new InvalidOperationException("Expected a Left."), Left: e => e);

    private static CloudWatchEvent<TestEvent> Event(TestEvent? detail) => new()
    {
        Id = "evt-1",
        Source = "orders",
        DetailType = "OrderPlaced",
        Detail = detail!,
    };

    [Fact]
    public async Task EventBridge_NullDetail_IsInvalidRequest()
    {
        var result = await EventBridgeHandler.ProcessAsync<TestEvent, string>(
            Event(null),
            _ => Task.FromResult(LanguageExt.Either<EncinaError, string>.Right("ok")));

        var error = Left(result);
        error.GetCode().IfNone(string.Empty).ShouldBe("eventbridge.detail_null");
        error.GetCategory().ShouldBe(ErrorCategory.InvalidRequest);
        error.GetStatusCode().ShouldBe(400);
    }

    [Fact]
    public async Task EventBridge_DelegateThrows_IsInternal()
    {
        var result = await EventBridgeHandler.ProcessAsync<TestEvent, string>(
            Event(new TestEvent { OrderId = "o-1" }),
            _ => throw new InvalidOperationException("boom"));

        var error = Left(result);
        error.GetCode().IfNone(string.Empty).ShouldBe("eventbridge.processing_failed");
        error.GetCategory().ShouldBe(ErrorCategory.Internal);
        error.GetStatusCode().ShouldBe(500);
    }

    [Theory]
    [InlineData(typeof(OperationCanceledException))]
    [InlineData(typeof(TaskCanceledException))]
    public async Task EventBridge_DelegateCancelled_IsCancelledAndNotAServerFault(Type exceptionType)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType)!;

        var result = await EventBridgeHandler.ProcessAsync<TestEvent, string>(
            Event(new TestEvent { OrderId = "o-1" }),
            _ => throw exception,
            new FakeLogger());

        var error = Left(result);
        error.GetCode().IfNone(string.Empty).ShouldBe("eventbridge.processing_cancelled");
        error.GetCategory().ShouldBe(ErrorCategory.Cancelled);
        error.GetStatusCode().ShouldBe(499);
    }

    [Fact]
    public async Task EventBridge_RawJsonNotDeserializable_IsInvalidRequest()
    {
        var result = await EventBridgeHandler.ProcessRawAsync<TestEvent, string>(
            "not json",
            _ => Task.FromResult(LanguageExt.Either<EncinaError, string>.Right("ok")));

        var error = Left(result);
        error.GetCode().IfNone(string.Empty).ShouldBe("eventbridge.deserialization_failed");
        error.GetCategory().ShouldBe(ErrorCategory.InvalidRequest);
    }

    [Fact]
    public async Task EventBridge_RawJsonNull_IsInvalidRequest()
    {
        var result = await EventBridgeHandler.ProcessRawAsync<TestEvent, string>(
            "null",
            _ => Task.FromResult(LanguageExt.Either<EncinaError, string>.Right("ok")));

        var error = Left(result);
        error.GetCode().IfNone(string.Empty).ShouldBe("eventbridge.deserialization_failed");
        error.GetCategory().ShouldBe(ErrorCategory.InvalidRequest);
    }

    [Fact]
    public void Sqs_InvalidJson_IsInvalidRequest()
    {
        var record = new SQSEvent.SQSMessage { MessageId = "m-1", Body = "invalid json" };

        var error = Left(SqsMessageHandler.DeserializeMessage<TestEvent>(record));

        error.GetCode().IfNone(string.Empty).ShouldBe("sqs.deserialization_failed");
        error.GetCategory().ShouldBe(ErrorCategory.InvalidRequest);
        error.GetStatusCode().ShouldBe(400);
    }

    [Fact]
    public void Sqs_NullBody_IsInvalidRequest()
    {
        var record = new SQSEvent.SQSMessage { MessageId = "m-1", Body = "null" };

        var error = Left(SqsMessageHandler.DeserializeMessage<TestEvent>(record));

        error.GetCode().IfNone(string.Empty).ShouldBe("sqs.deserialization_failed");
        error.GetCategory().ShouldBe(ErrorCategory.InvalidRequest);
    }

    private static APIGatewayProxyRequest TenantRequest(string? header, object? claim)
    {
        var authorizer = new APIGatewayCustomAuthorizerContext();
        if (claim is not null)
        {
            authorizer["tenant_id"] = claim;
        }

        return new APIGatewayProxyRequest
        {
            Headers = header is null ? new Dictionary<string, string>() : new Dictionary<string, string> { ["X-Tenant-ID"] = header },
            RequestContext = new APIGatewayProxyRequest.ProxyRequestContext { Authorizer = authorizer },
        };
    }

    [Fact]
    public void Tenancy_HeaderDiffersFromClaim_IsForbidden()
    {
        var context = Substitute.For<ILambdaContext>();

        var result = context.ResolveTenantId(TenantRequest("tenant-b", "tenant-a"), new EncinaAwsLambdaOptions(), new FakeLogger());

        var error = Left(result);
        error.GetCode().IfNone(string.Empty).ShouldBe(LambdaContextExtensions.TenantMismatchErrorCode);
        error.GetCategory().ShouldBe(ErrorCategory.Forbidden);
        error.GetStatusCode().ShouldBe(403);
    }

    [Fact]
    public void Tenancy_ClaimIsNotAString_IsForbidden()
    {
        var context = Substitute.For<ILambdaContext>();

        var result = context.ResolveTenantId(TenantRequest(null, JsonDocument.Parse("123").RootElement), new EncinaAwsLambdaOptions(), new FakeLogger());

        var error = Left(result);
        error.GetCode().IfNone(string.Empty).ShouldBe(LambdaContextExtensions.InvalidTenantClaimErrorCode);
        error.GetCategory().ShouldBe(ErrorCategory.Forbidden);
        error.GetStatusCode().ShouldBe(403);
    }
}
