using System.Net;
using Encina.Recoverability;
using Encina.Refit;
using Encina.Testing.WireMock;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Refit;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Encina.IntegrationTests.Web.Refit;

/// <summary>
/// Outbound error taxonomy and retry rules end to end: a Refit client against WireMock, the Refit handler and the
/// Polly retry behavior (#1223, SPEC-002 AC-034 and the slice of S2 owned here).
/// </summary>
[Trait("Category", "Integration")]
public sealed class OutboundErrorTaxonomyIntegrationTests : IClassFixture<EncinaWireMockFixture>
{
    private const string BodySentinel = "BODY-SENTINEL-patient-42";
    private readonly EncinaWireMockFixture _wireMock;

    public OutboundErrorTaxonomyIntegrationTests(EncinaWireMockFixture wireMock)
    {
        _wireMock = wireMock;
        _wireMock.Reset();
    }

    public interface IOutboundApi
    {
        [Get("/items/{id}")]
        Task Fetch(int id, CancellationToken cancellationToken);

        [Post("/messages")]
        Task Send([Body] string body, CancellationToken cancellationToken);
    }

    public sealed record GetItem(int Id) : IRestApiRequest<IOutboundApi, string>
    {
        public async Task<string> ExecuteAsync(IOutboundApi apiClient, CancellationToken cancellationToken)
        {
            await apiClient.Fetch(Id, cancellationToken);
            return "got";
        }
    }

    [Retry(MaxAttempts = 3, BaseDelayMs = 10, MaxDelayMs = 5000)]
    public sealed record RetriedGet(int Id) : IRestApiRequest<IOutboundApi, string>
    {
        public async Task<string> ExecuteAsync(IOutboundApi apiClient, CancellationToken cancellationToken)
        {
            await apiClient.Fetch(Id, cancellationToken);
            return "got";
        }
    }

    [Retry(MaxAttempts = 3, BaseDelayMs = 10, MaxDelayMs = 5000)]
    public sealed record SendMessage : IRestApiRequest<IOutboundApi, string>
    {
        public async Task<string> ExecuteAsync(IOutboundApi apiClient, CancellationToken cancellationToken)
        {
            await apiClient.Send("hello", cancellationToken);
            return "sent";
        }
    }

    [Retry(MaxAttempts = 3, BaseDelayMs = 10, MaxDelayMs = 5000, RetryNonIdempotent = true)]
    public sealed record SendMessageOptIn : IRestApiRequest<IOutboundApi, string>
    {
        public async Task<string> ExecuteAsync(IOutboundApi apiClient, CancellationToken cancellationToken)
        {
            await apiClient.Send("hello", cancellationToken);
            return "sent";
        }
    }

    // A loopback port that was free a moment ago, so a connection to it is refused.
    private static string ClosedPortUrl()
    {
        var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return $"http://127.0.0.1:{port}";
    }

    private ServiceProvider Provider(string? baseUrl = null, TimeSpan? timeout = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaRefitClient<IOutboundApi>(client =>
        {
            client.BaseAddress = new Uri(baseUrl ?? _wireMock.BaseUrl);
            if (timeout is { } t)
            {
                client.Timeout = t;
            }
        }).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { UseProxy = false });
        services.AddEncinaPolly();
        return services.BuildServiceProvider();
    }

    private static async Task<Either<EncinaError, string>> Send<TRequest>(
        ServiceProvider provider,
        TRequest request,
        FakeLogger<RestApiRequestHandler<TRequest, IOutboundApi, string>>? logger = null,
        string? idempotencyKey = null)
        where TRequest : class, IRestApiRequest<IOutboundApi, string>
    {
        var api = provider.GetRequiredService<IOutboundApi>();
        var handlerLogger = logger ?? new FakeLogger<RestApiRequestHandler<TRequest, IOutboundApi, string>>();
        var handler = new RestApiRequestHandler<TRequest, IOutboundApi, string>(api, handlerLogger);
        var behavior = new RetryPipelineBehavior<TRequest, string>(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<RetryPipelineBehavior<TRequest, string>>.Instance,
            provider.GetRequiredService<IErrorClassifier>());

        var context = Substitute.For<IRequestContext>();
        context.IdempotencyKey.Returns(idempotencyKey);

        return await behavior.Handle(
            request,
            context,
            () => new ValueTask<Either<EncinaError, string>>(handler.Handle(request, CancellationToken.None)),
            CancellationToken.None);
    }

    private void Respond(string path, string method, HttpStatusCode status, string? retryAfter = null)
    {
        var response = Response.Create().WithStatusCode(status).WithBody(BodySentinel);
        if (retryAfter is not null)
        {
            response = response.WithHeader("Retry-After", retryAfter);
        }

        _wireMock.Server.Given(Request.Create().WithPath(path).UsingMethod(method)).RespondWith(response);
    }

    // Answers with the given statuses in order, then 200.
    private void RespondInSequence(string path, string method, params HttpStatusCode[] failures)
    {
        var scenario = Guid.NewGuid().ToString("N");
        var state = "after-0";
        for (var i = 0; i < failures.Length; i++)
        {
            var next = $"after-{i + 1}";
            var request = Request.Create().WithPath(path).UsingMethod(method);
            var response = Response.Create().WithStatusCode(failures[i]).WithBody(BodySentinel);

            // The first mapping has no state condition: the initial state is implicit
            if (i == 0)
            {
                _wireMock.Server.Given(request).InScenario(scenario).WillSetStateTo(next).RespondWith(response);
            }
            else
            {
                _wireMock.Server.Given(request).InScenario(scenario).WhenStateIs(state).WillSetStateTo(next).RespondWith(response);
            }

            state = next;
        }

        _wireMock.Server.Given(Request.Create().WithPath(path).UsingMethod(method))
            .InScenario(scenario).WhenStateIs(state)
            .RespondWith(Response.Create().WithStatusCode(200));
    }

    private int Calls(string path) => _wireMock.Server.LogEntries.Count(e => e.RequestMessage?.Path == path);

    private static void ShouldSucceed(Either<EncinaError, string> result)
    {
        var detail = result.IsLeft
            ? $"{((EncinaError)result).GetCode().IfNone("?")} {string.Join(',', ((EncinaError)result).GetMetadata().Select(kv => $"{kv.Key}={kv.Value}"))}"
            : string.Empty;
        result.IsRight.ShouldBeTrue(detail);
    }

    private static EncinaError Error(Either<EncinaError, string> result)
    {
        result.IsLeft.ShouldBeTrue("expected a failure");
        return (EncinaError)result;
    }

    [Fact]
    public async Task ClientError400_IsPermanentWithStatusAndNoBody_AndIsNotRetried()
    {
        Respond("/items/1", "GET", HttpStatusCode.BadRequest);
        using var provider = Provider();
        var logger = new FakeLogger<RestApiRequestHandler<RetriedGet, IOutboundApi, string>>();

        var error = Error(await Send(provider, new RetriedGet(1), logger));

        error.GetCode().IfNone(string.Empty).ShouldBe(OutboundErrorCodes.ClientError);
        error.GetMetadata()[OutboundErrorMetadataKeys.HttpStatus].ShouldBe(400);
        error.GetMetadata()[OutboundErrorMetadataKeys.Transient].ShouldBe(false);
        Calls("/items/1").ShouldBe(1, "a 400 is not retried");
        error.Message.ShouldNotContain(BodySentinel);
        error.GetMetadata().Values.Select(v => v?.ToString()).ShouldNotContain(BodySentinel);
        error.Exception.Filter(e => e.ToString().Contains(BodySentinel, StringComparison.Ordinal)).IsNone.ShouldBeTrue();
        logger.Collector.GetSnapshot().Select(r => r.Message + r.Exception).ShouldNotContain(m => m.Contains(BodySentinel, StringComparison.Ordinal));
    }

    [Fact]
    public async Task RateLimited429WithRetryAfter_IsTransientWithTheHeaderDelay()
    {
        Respond("/items/2", "GET", HttpStatusCode.TooManyRequests, retryAfter: "120");
        using var provider = Provider();

        var error = Error(await Send(provider, new GetItem(2)));

        error.GetCode().IfNone(string.Empty).ShouldBe(OutboundErrorCodes.RateLimited);
        error.GetMetadata()[OutboundErrorMetadataKeys.Transient].ShouldBe(true);
        error.GetMetadata()[OutboundErrorMetadataKeys.HttpRetryAfter].ShouldBe(TimeSpan.FromSeconds(120));
    }

    [Fact]
    public async Task RetryAfterBeyondTheMaximum_IsReturnedWithoutRetry()
    {
        Respond("/items/3", "GET", HttpStatusCode.TooManyRequests, retryAfter: "120");
        using var provider = Provider();

        var error = Error(await Send(provider, new RetriedGet(3)));

        Calls("/items/3").ShouldBe(1);
        OutboundRetryRules.GetRetryAfter(error).ShouldBe(TimeSpan.FromSeconds(120));
    }

    [Fact]
    public async Task RateLimitedWithShortRetryAfter_WaitsForTheHeaderAndThenSucceeds()
    {
        var scenario = "ra";
        _wireMock.Server.Given(Request.Create().WithPath("/items/4").UsingGet())
            .InScenario(scenario).WillSetStateTo("second")
            .RespondWith(Response.Create().WithStatusCode(429).WithHeader("Retry-After", "1"));
        _wireMock.Server.Given(Request.Create().WithPath("/items/4").UsingGet())
            .InScenario(scenario).WhenStateIs("second")
            .RespondWith(Response.Create().WithStatusCode(200));
        using var provider = Provider();
        var started = DateTimeOffset.UtcNow;

        var result = await Send(provider, new RetriedGet(4));

        ShouldSucceed(result);
        (DateTimeOffset.UtcNow - started).ShouldBeGreaterThanOrEqualTo(TimeSpan.FromSeconds(1));
        Calls("/items/4").ShouldBe(2);
    }

    [Fact]
    public async Task ServiceUnavailable503_IsATransientServerError()
    {
        Respond("/items/5", "GET", HttpStatusCode.ServiceUnavailable);
        using var provider = Provider();

        var error = Error(await Send(provider, new GetItem(5)));

        error.GetCode().IfNone(string.Empty).ShouldBe(OutboundErrorCodes.ServerError);
        error.GetMetadata()[OutboundErrorMetadataKeys.Transient].ShouldBe(true);
        error.GetMetadata()[OutboundErrorMetadataKeys.HttpStatus].ShouldBe(503);
    }

    [Fact]
    public async Task NetworkFailure_IsATransientNetworkErrorWithoutStatus()
    {
        using var provider = Provider(baseUrl: ClosedPortUrl());

        var error = Error(await Send(provider, new GetItem(6)));

        error.GetCode().IfNone(string.Empty).ShouldBe(OutboundErrorCodes.NetworkError);
        error.GetMetadata()[OutboundErrorMetadataKeys.Transient].ShouldBe(true);
        error.GetMetadata().ContainsKey(OutboundErrorMetadataKeys.HttpStatus).ShouldBeFalse();
    }

    [Fact]
    public async Task Timeout_IsATransientTimeoutError()
    {
        _wireMock.Server.Given(Request.Create().WithPath("/items/7").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithDelay(TimeSpan.FromSeconds(3)));
        using var provider = Provider(timeout: TimeSpan.FromMilliseconds(200));

        var error = Error(await Send(provider, new GetItem(7)));

        error.GetCode().IfNone(string.Empty).ShouldBe(OutboundErrorCodes.Timeout);
        error.GetMetadata()[OutboundErrorMetadataKeys.Transient].ShouldBe(true);
    }

    [Fact]
    public async Task S2Slice_ServerErrorTwiceThenSuccessOnAGet_IsDeliveredOnce()
    {
        RespondInSequence("/items/8", "GET", HttpStatusCode.InternalServerError, HttpStatusCode.BadGateway);
        using var provider = Provider();

        var result = await Send(provider, new RetriedGet(8));

        ShouldSucceed(result);
        Calls("/items/8").ShouldBe(3);
    }

    [Fact]
    public async Task S2Slice_PostServiceUnavailableTwiceThenSuccess_IsDeliveredOnce()
    {
        // 503 proves the request was not processed: transient even on a POST
        RespondInSequence("/messages", "POST", HttpStatusCode.ServiceUnavailable, HttpStatusCode.ServiceUnavailable);
        using var provider = Provider();

        var result = await Send(provider, new SendMessage());

        ShouldSucceed(result);
        Calls("/messages").ShouldBe(3);
    }

    [Fact]
    public async Task PostBadGateway_IsNotRetriedByDefault()
    {
        Respond("/messages", "POST", HttpStatusCode.BadGateway);
        using var provider = Provider();

        var error = Error(await Send(provider, new SendMessage()));

        Calls("/messages").ShouldBe(1, "an ambiguous failure of a non-idempotent send is not retried automatically");
        error.GetMetadata()[OutboundErrorMetadataKeys.Transient].ShouldBe(true);
        error.GetMetadata()[OutboundErrorMetadataKeys.Ambiguous].ShouldBe(true);
    }

    [Fact]
    public async Task S2Slice_PostBadGatewayTwiceThenSuccess_IsDeliveredOnceWithTheOptInOrAnIdempotencyKey()
    {
        RespondInSequence("/messages", "POST", HttpStatusCode.BadGateway, HttpStatusCode.BadGateway);
        using var provider = Provider();
        ShouldSucceed(await Send(provider, new SendMessageOptIn()));
        Calls("/messages").ShouldBe(3);

        _wireMock.Server.Reset();
        RespondInSequence("/messages", "POST", HttpStatusCode.BadGateway, HttpStatusCode.BadGateway);
        ShouldSucceed(await Send(provider, new SendMessage(), idempotencyKey: "key-1"));
        Calls("/messages").ShouldBe(3);
    }

    [Fact]
    public async Task PostWithAConnectionThatNeverOpened_IsRetried()
    {
        // ConnectionError proves the request was not sent: transient and not ambiguous even on a POST
        using var provider = Provider(baseUrl: ClosedPortUrl());

        var error = Error(await Send(provider, new SendMessage()));

        error.GetCode().IfNone(string.Empty).ShouldBe(OutboundErrorCodes.NetworkError);
        error.GetMetadata().ContainsKey(OutboundErrorMetadataKeys.Ambiguous).ShouldBeFalse();
        OutboundRetryRules.AllowsAutomaticRetry(error, null, false).ShouldBeTrue();
    }
}
