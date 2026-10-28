using System.Collections.Concurrent;
using System.Text;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Encina.AwsLambda;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.IntegrationTests.Cloud.AwsLambda;

/// <summary>
/// A command to place an order; it is also the SQS body and the EventBridge detail of the tests.
/// </summary>
/// <param name="OrderId">The order identifier.</param>
/// <param name="Amount">The order amount.</param>
public sealed record PlaceOrder(string OrderId, decimal Amount) : ICommand<OrderReceipt>;

/// <summary>
/// The result of placing an order.
/// </summary>
/// <param name="OrderId">The order identifier.</param>
/// <param name="Status">The resulting status.</param>
public sealed record OrderReceipt(string OrderId, string Status);

/// <summary>
/// A command without a result value.
/// </summary>
/// <param name="OrderId">The order identifier.</param>
public sealed record CancelOrder(string OrderId) : ICommand<Unit>;

/// <summary>
/// Records what the real handlers did, so tests assert observable effects.
/// </summary>
public sealed class OrderLedger
{
    private readonly ConcurrentQueue<string> _entries = new();

    /// <summary>Gets the recorded entries in order.</summary>
    public IReadOnlyCollection<string> Entries => [.. _entries];

    /// <summary>Records an entry.</summary>
    public void Record(string entry) => _entries.Enqueue(entry);
}

/// <summary>
/// A real command handler registered in the DI container; its failures are the railway lefts of the pipeline.
/// </summary>
public sealed class PlaceOrderHandler(OrderLedger ledger) : ICommandHandler<PlaceOrder, OrderReceipt>
{
    /// <summary>The order identifier that raises a conflict.</summary>
    public const string DuplicateOrderId = "duplicate";

    /// <summary>The order identifier that raises an unexpected exception.</summary>
    public const string ExplodingOrderId = "explode";

    /// <inheritdoc/>
    public Task<Either<EncinaError, OrderReceipt>> Handle(PlaceOrder request, CancellationToken cancellationToken)
    {
        if (request.OrderId == ExplodingOrderId)
        {
            throw new InvalidOperationException("boom");
        }

        if (request.OrderId == DuplicateOrderId)
        {
            return Task.FromResult<Either<EncinaError, OrderReceipt>>(
                EncinaErrors.Create("order.already_exists", "The order already exists."));
        }

        if (request.Amount <= 0)
        {
            return Task.FromResult<Either<EncinaError, OrderReceipt>>(
                EncinaErrors.Create("validation.invalid_amount", "The amount must be positive."));
        }

        ledger.Record($"placed:{request.OrderId}");
        return Task.FromResult<Either<EncinaError, OrderReceipt>>(new OrderReceipt(request.OrderId, "Placed"));
    }
}

/// <summary>
/// A real handler of a command with no result value.
/// </summary>
public sealed class CancelOrderHandler(OrderLedger ledger) : ICommandHandler<CancelOrder, Unit>
{
    /// <inheritdoc/>
    public Task<Either<EncinaError, Unit>> Handle(CancelOrder request, CancellationToken cancellationToken)
    {
        ledger.Record($"cancelled:{request.OrderId}");
        return Task.FromResult<Either<EncinaError, Unit>>(Unit.Default);
    }
}

/// <summary>
/// Builds the real Encina pipeline with the AWS Lambda registration and deserializes events the way the Lambda runtime does.
/// </summary>
internal static class AwsLambdaTestSupport
{
    /// <summary>Builds a validated provider with the real pipeline, the two real handlers and the Lambda package.</summary>
    public static ServiceProvider CreateServiceProvider(Action<EncinaAwsLambdaOptions>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<OrderLedger>();
        services.AddEncina();
        services.AddTransient<IRequestHandler<PlaceOrder, OrderReceipt>, PlaceOrderHandler>();
        services.AddTransient<IRequestHandler<CancelOrder, Unit>, CancelOrderHandler>();
        services.AddEncinaAwsLambda(options => configure?.Invoke(options));

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
    }

    /// <summary>Deserializes JSON with the serializer the Lambda runtime uses for these event types.</summary>
    public static T Deserialize<T>(string json)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        return new DefaultLambdaJsonSerializer().Deserialize<T>(stream);
    }

    /// <summary>Serializes a value the way the Lambda runtime writes a function response.</summary>
    public static string Serialize<T>(T value)
    {
        using var stream = new MemoryStream();
        new DefaultLambdaJsonSerializer().Serialize(value, stream);
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>Builds an SQS event JSON document with one record per body.</summary>
    public static string SqsEventJson(params (string MessageId, string Body)[] records)
    {
        var items = records.Select(r =>
            $$"""{"messageId":"{{r.MessageId}}","receiptHandle":"rh-{{r.MessageId}}","body":{{System.Text.Json.JsonSerializer.Serialize(r.Body)}},"attributes":{"ApproximateReceiveCount":"1"},"messageAttributes":{},"md5OfBody":"x","eventSource":"aws:sqs","eventSourceARN":"arn:aws:sqs:eu-west-1:000000000000:orders","awsRegion":"eu-west-1"}""");
        return $$"""{"Records":[{{string.Join(",", items)}}]}""";
    }

    /// <summary>Builds an EventBridge event JSON document around a detail JSON object.</summary>
    public static string EventBridgeJson(string detailJson) =>
        $$"""{"version":"0","id":"evt-1","detail-type":"OrderPlaced","source":"custom.orders","account":"000000000000","time":"2026-10-28T10:00:00Z","region":"eu-west-1","resources":[],"detail":{{detailJson}}}""";
}

/// <summary>
/// A concrete Lambda context, as the Lambda runtime hands to a function.
/// </summary>
internal sealed class TestLambdaContext : ILambdaContext
{
    public string AwsRequestId { get; init; } = "req-123";

    public IClientContext ClientContext => null!;

    public string FunctionName { get; init; } = "orders-function";

    public string FunctionVersion => "$LATEST";

    public ICognitoIdentity Identity => null!;

    public string InvokedFunctionArn => "arn:aws:lambda:eu-west-1:000000000000:function:orders-function";

    public ILambdaLogger Logger => new TestLambdaLogger();

    public string LogGroupName => "/aws/lambda/orders-function";

    public string LogStreamName => "stream";

    public int MemoryLimitInMB => 256;

    public TimeSpan RemainingTime { get; init; } = TimeSpan.FromSeconds(30);

    private sealed class TestLambdaLogger : ILambdaLogger
    {
        public void Log(string message)
        {
        }

        public void LogLine(string message)
        {
        }
    }
}
