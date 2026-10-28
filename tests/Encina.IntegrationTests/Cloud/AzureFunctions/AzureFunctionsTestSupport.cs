using System.Collections.Immutable;
using System.Net;
using System.Security.Claims;
using Encina.AzureFunctions;
using LanguageExt;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.IntegrationTests.Cloud.AzureFunctions;

/// <summary>
/// A command to place an order, handled by the real Encina pipeline of the tests.
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
/// A real command handler registered in the DI container; its failures are the railway lefts of the pipeline.
/// </summary>
public sealed class PlaceOrderHandler : IRequestHandler<PlaceOrder, OrderReceipt>
{
    /// <summary>The order identifier that raises a conflict.</summary>
    public const string DuplicateOrderId = "duplicate";

    /// <inheritdoc/>
    public Task<Either<EncinaError, OrderReceipt>> Handle(PlaceOrder request, CancellationToken cancellationToken)
    {
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

        return Task.FromResult<Either<EncinaError, OrderReceipt>>(new OrderReceipt(request.OrderId, "Placed"));
    }
}

/// <summary>
/// A real handler of a command with no result value.
/// </summary>
public sealed class CancelOrderHandler : IRequestHandler<CancelOrder, Unit>
{
    /// <inheritdoc/>
    public Task<Either<EncinaError, Unit>> Handle(CancelOrder request, CancellationToken cancellationToken) =>
        Task.FromResult<Either<EncinaError, Unit>>(Unit.Default);
}

/// <summary>
/// Builds the real Encina pipeline with the Azure Functions registration.
/// </summary>
internal static class AzureFunctionsTestSupport
{
    /// <summary>Builds a validated provider with the real pipeline, the two real handlers and the Functions package.</summary>
    public static ServiceProvider CreateServiceProvider(Action<EncinaAzureFunctionsOptions>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncina();
        services.AddTransient<IRequestHandler<PlaceOrder, OrderReceipt>, PlaceOrderHandler>();
        services.AddTransient<IRequestHandler<CancelOrder, Unit>, CancelOrderHandler>();
        services.AddEncinaAzureFunctions(options => configure?.Invoke(options));

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
    }
}

/// <summary>
/// A concrete function definition, as the worker host builds for a discovered function.
/// </summary>
internal sealed class TestFunctionDefinition(string name) : FunctionDefinition
{
    public override ImmutableArray<FunctionParameter> Parameters => [];

    public override string PathToAssembly => "orders.dll";

    public override string EntryPoint => $"Orders.{name}.Run";

    public override string Id => $"id-{name}";

    public override string Name => name;

    public override IImmutableDictionary<string, BindingMetadata> InputBindings =>
        ImmutableDictionary<string, BindingMetadata>.Empty;

    public override IImmutableDictionary<string, BindingMetadata> OutputBindings =>
        ImmutableDictionary<string, BindingMetadata>.Empty;
}

/// <summary>
/// A concrete function context, as the worker host hands to the middleware pipeline.
/// </summary>
internal sealed class TestFunctionContext(IServiceProvider services, string functionName, string invocationId)
    : FunctionContext
{
    private readonly TestFunctionDefinition _definition = new(functionName);

    public override string InvocationId { get; } = invocationId;

    public override string FunctionId => _definition.Id;

    public override TraceContext TraceContext => null!;

    public override BindingContext BindingContext => null!;

    public override RetryContext RetryContext => null!;

    public override IServiceProvider InstanceServices { get; set; } = services;

    public override FunctionDefinition FunctionDefinition => _definition;

    public override IDictionary<object, object> Items { get; set; } = new Dictionary<object, object>();

    public override IInvocationFeatures Features => null!;
}

/// <summary>
/// A concrete HTTP request, as the worker host builds for an HTTP trigger.
/// </summary>
internal sealed class TestHttpRequestData : HttpRequestData
{
    private readonly FunctionContext _context;

    public TestHttpRequestData(
        FunctionContext context,
        string url = "https://functions.local/api/orders?x=1",
        IEnumerable<KeyValuePair<string, string>>? headers = null,
        IEnumerable<ClaimsIdentity>? identities = null)
        : base(context)
    {
        _context = context;
        Url = new Uri(url);
        Headers = [];
        foreach (var (name, value) in headers ?? [])
        {
            Headers.Add(name, value);
        }

        Identities = [.. identities ?? []];
    }

    public override Stream Body { get; } = new MemoryStream();

    public override HttpHeadersCollection Headers { get; }

    public override IReadOnlyCollection<IHttpCookie> Cookies { get; } = [];

    public override Uri Url { get; }

    public override IEnumerable<ClaimsIdentity> Identities { get; }

    public override string Method => "POST";

    public override HttpResponseData CreateResponse() => new TestHttpResponseData(_context);
}

/// <summary>
/// A concrete HTTP response that keeps what the function wrote.
/// </summary>
internal sealed class TestHttpResponseData : HttpResponseData
{
    public TestHttpResponseData(FunctionContext context)
        : base(context)
    {
        Headers = [];
    }

    public override HttpStatusCode StatusCode { get; set; }

    public override HttpHeadersCollection Headers { get; set; }

    public override Stream Body { get; set; } = new MemoryStream();

    public override HttpCookies Cookies => null!;

    /// <summary>Reads the response body as text.</summary>
    public string ReadBody()
    {
        Body.Position = 0;
        using var reader = new StreamReader(Body, leaveOpen: true);
        return reader.ReadToEnd();
    }
}
