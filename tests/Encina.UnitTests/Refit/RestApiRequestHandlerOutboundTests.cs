using System.Net;
using Encina.Recoverability;
using Encina.Refit;
using Encina.UnitTests.Recoverability;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;
using Refit;

namespace Encina.UnitTests.Refit;

/// <summary>
/// The outbound error taxonomy of <see cref="RestApiRequestHandler{TRequest, TApiClient, TResponse}"/> (#1223).
/// </summary>
public sealed class RestApiRequestHandlerOutboundTests
{
    private const string BodySentinel = "BODY-SENTINEL-patient-42";
    private static readonly DateTimeOffset Now = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);

    public interface IScriptedApi;

    public sealed class ScriptedRequest : IRestApiRequest<IScriptedApi, string>
    {
        public required Func<CancellationToken, Task<string>> Script { get; init; }

        public Task<string> ExecuteAsync(IScriptedApi apiClient, CancellationToken cancellationToken) => Script(cancellationToken);
    }

    private static async Task<ApiException> Api(HttpStatusCode status, HttpMethod? method = null, string? retryAfter = null)
    {
        var response = new HttpResponseMessage(status) { Content = new StringContent(BodySentinel) };
        if (retryAfter is not null)
        {
            response.Headers.TryAddWithoutValidation("Retry-After", retryAfter);
        }

        method ??= HttpMethod.Get;
        if (response.IsSuccessStatusCode)
        {
            // Refit raises this for a response that cannot be deserialized
            return await ApiException.Create(
                "Failed to deserialize the response",
                new HttpRequestMessage(method, "http://test.example/x"),
                method,
                response,
                new RefitSettings(),
                new FormatException("bad json"));
        }

        return await ApiException.Create(new HttpRequestMessage(method, "http://test.example/x"), method, response, new RefitSettings());
    }

    private static (RestApiRequestHandler<ScriptedRequest, IScriptedApi, string> Handler, FakeLogger<RestApiRequestHandler<ScriptedRequest, IScriptedApi, string>> Logger)
        Create(IRequestContext? context = null)
    {
        var accessor = Substitute.For<IRequestContextAccessor>();
        accessor.RequestContext.Returns(context);
        var logger = new FakeLogger<RestApiRequestHandler<ScriptedRequest, IScriptedApi, string>>();
        return (new(Substitute.For<IScriptedApi>(), logger, accessor, new FakeTimeProvider(Now)), logger);
    }

    private static async Task<EncinaError> Fail(Func<CancellationToken, Task<string>> script, IRequestContext? context = null, CancellationToken token = default)
    {
        var (handler, _) = Create(context);
        var result = await handler.Handle(new ScriptedRequest { Script = script }, token);
        return (EncinaError)result;
    }

    private static T Meta<T>(EncinaError error, string key) => (T)error.GetMetadata()[key]!;

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, OutboundErrorCodes.ClientError, false)]
    [InlineData(HttpStatusCode.NotFound, OutboundErrorCodes.ClientError, false)]
    [InlineData(HttpStatusCode.TooManyRequests, OutboundErrorCodes.RateLimited, true)]
    [InlineData(HttpStatusCode.RequestTimeout, OutboundErrorCodes.Timeout, true)]
    [InlineData(HttpStatusCode.ServiceUnavailable, OutboundErrorCodes.ServerError, true)]
    [InlineData(HttpStatusCode.InternalServerError, OutboundErrorCodes.ServerError, true)]
    [InlineData(HttpStatusCode.NotImplemented, OutboundErrorCodes.ServerError, true)]
    [InlineData(HttpStatusCode.Redirect, OutboundErrorCodes.ClientError, false)]
    public async Task Handle_ErrorStatus_IsCodedWithStatusAndTransientMetadata(HttpStatusCode status, string code, bool transient)
    {
        var error = await Fail(async _ => throw await Api(status));

        error.GetCode().IfNone(string.Empty).ShouldBe(code);
        Meta<int>(error, OutboundErrorMetadataKeys.HttpStatus).ShouldBe((int)status);
        Meta<bool>(error, OutboundErrorMetadataKeys.Transient).ShouldBe(transient);
        error.GetMetadata().ContainsKey(OutboundErrorMetadataKeys.HttpRetryAfter).ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_SuccessfulStatusThatCannotBeRead_IsAPermanentUnexpectedResponse()
    {
        var error = await Fail(async _ => throw await Api(HttpStatusCode.OK));

        error.GetCode().IfNone(string.Empty).ShouldBe(OutboundErrorCodes.UnexpectedResponse);
        Meta<bool>(error, OutboundErrorMetadataKeys.Transient).ShouldBeFalse();
    }

    [Theory]
    [InlineData("2", 2)]
    [InlineData("Wed, 21 Oct 2026 12:01:30 GMT", 90)]
    [InlineData("Wed, 21 Oct 2026 11:00:00 GMT", 0)]
    public async Task Handle_RateLimitedWithRetryAfter_CarriesTheDelay(string header, int seconds)
    {
        var error = await Fail(async _ => throw await Api(HttpStatusCode.TooManyRequests, retryAfter: header));

        Meta<TimeSpan>(error, OutboundErrorMetadataKeys.HttpRetryAfter).ShouldBe(TimeSpan.FromSeconds(seconds));
    }

    [Fact]
    public async Task Handle_UnparseableRetryAfter_HasNoRetryAfterMetadata()
    {
        var error = await Fail(async _ => throw await Api(HttpStatusCode.TooManyRequests, retryAfter: "later"));

        error.GetMetadata().ContainsKey(OutboundErrorMetadataKeys.HttpRetryAfter).ShouldBeFalse();
    }

    [Theory]
    [InlineData("POST", HttpStatusCode.BadGateway, true)]
    [InlineData("PATCH", HttpStatusCode.GatewayTimeout, true)]
    [InlineData("POST", HttpStatusCode.InternalServerError, true)]
    [InlineData("POST", HttpStatusCode.ServiceUnavailable, false)]
    [InlineData("POST", HttpStatusCode.TooManyRequests, false)]
    [InlineData("GET", HttpStatusCode.BadGateway, false)]
    [InlineData("PUT", HttpStatusCode.BadGateway, false)]
    public async Task Handle_ServerFailure_IsAmbiguousOnlyOnNonIdempotentMethodsAndAmbiguousStatuses(string method, HttpStatusCode status, bool ambiguous)
    {
        var error = await Fail(async _ => throw await Api(status, new HttpMethod(method)));

        Meta<bool>(error, OutboundErrorMetadataKeys.Transient).ShouldBe(status is not (HttpStatusCode.BadRequest));
        error.GetMetadata().ContainsKey(OutboundErrorMetadataKeys.Ambiguous).ShouldBe(ambiguous);
        OutboundRetryRules.AllowsAutomaticRetry(error, null, false).ShouldBe(!ambiguous);
    }

    [Fact]
    public async Task Handle_ErrorResponse_NeverCarriesTheBodyInMessageDetailsCauseOrLogs()
    {
        var (handler, logger) = Create();

        var result = await handler.Handle(
            new ScriptedRequest { Script = async _ => throw await Api(HttpStatusCode.BadRequest) }, CancellationToken.None);

        var error = (EncinaError)result;
        error.Message.ShouldNotContain(BodySentinel);
        error.GetMetadata().Values.Select(v => v?.ToString()).ShouldNotContain(BodySentinel);
        error.GetCause().IsNone.ShouldBeTrue();
        error.Exception.Filter(e => e.ToString().Contains(BodySentinel, StringComparison.Ordinal)).IsNone.ShouldBeTrue();
        logger.Collector.GetSnapshot().Select(r => r.Message + r.Exception).ShouldNotContain(m => m.Contains(BodySentinel, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(HttpRequestError.ConnectionError, false)]
    [InlineData(HttpRequestError.NameResolutionError, false)]
    [InlineData(HttpRequestError.SecureConnectionError, false)]
    [InlineData(HttpRequestError.ProxyTunnelError, false)]
    [InlineData(HttpRequestError.ResponseEnded, true)]
    [InlineData(HttpRequestError.Unknown, true)]
    public async Task Handle_NetworkFailure_IsTransientAndAmbiguousOnlyAfterTheRequestMayHaveBeenSent(HttpRequestError reason, bool ambiguous)
    {
        // The method is unknown without the capture handler: treated as non-idempotent
        var error = await Fail(_ => throw new HttpRequestException(reason, "contains-host.example"));

        error.GetCode().IfNone(string.Empty).ShouldBe(OutboundErrorCodes.NetworkError);
        Meta<bool>(error, OutboundErrorMetadataKeys.Transient).ShouldBeTrue();
        error.GetMetadata().ContainsKey(OutboundErrorMetadataKeys.HttpStatus).ShouldBeFalse();
        error.GetMetadata().ContainsKey(OutboundErrorMetadataKeys.Ambiguous).ShouldBe(ambiguous);
        error.Message.ShouldNotContain("contains-host.example");
    }

    [Fact]
    public async Task Handle_Timeout_IsTransientAndAmbiguous()
    {
        var error = await Fail(_ => throw new TaskCanceledException("t", new TimeoutException()));

        error.GetCode().IfNone(string.Empty).ShouldBe(OutboundErrorCodes.Timeout);
        Meta<bool>(error, OutboundErrorMetadataKeys.Transient).ShouldBeTrue();
        error.GetMetadata().ContainsKey(OutboundErrorMetadataKeys.Ambiguous).ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_CallerCancellation_IsCancelledAndNotTransient()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var error = await Fail(ct => throw new TaskCanceledException("c", null, ct), token: cts.Token);

        error.GetCode().IfNone(string.Empty).ShouldBe(EncinaErrorCodes.RequestCancelled);
        Meta<bool>(error, OutboundErrorMetadataKeys.Transient).ShouldBeFalse();
    }

    [Theory]
    [InlineData(typeof(TimeoutException), true)]
    [InlineData(typeof(IOException), true)]
    [InlineData(typeof(InvalidOperationException), false)]
    [InlineData(typeof(OperationCanceledException), false)]
    public async Task Handle_UnexpectedException_IsTransientOnlyForTimeoutAndIo(Type exceptionType, bool transient)
    {
        var error = await Fail(_ => throw (Exception)Activator.CreateInstance(exceptionType, "secret-text")!);

        error.GetCode().IfNone(string.Empty).ShouldBe(OutboundErrorCodes.Unexpected);
        Meta<bool>(error, OutboundErrorMetadataKeys.Transient).ShouldBe(transient);
        error.Message.ShouldNotContain("secret-text");
    }

    [Fact]
    public async Task Handle_LogsUseTheContextCorrelationIdAndNoBody()
    {
        var context = Substitute.For<IRequestContext>();
        context.CorrelationId.Returns("corr-123");
        var (handler, logger) = Create(context);

        await handler.Handle(new ScriptedRequest { Script = async _ => throw await Api(HttpStatusCode.BadGateway) }, CancellationToken.None);

        logger.Collector.GetSnapshot().ShouldAllBe(r => r.Message.Contains("corr-123", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Handle_Failures_RecordTheRequestCounterAndActivityWithTheTenantOnly()
    {
        using var metrics = new MetricCapture("encina.outbound.requests_total");
        using var activities = new ActivityCapture("Encina.Outbound.Send");
        var context = Substitute.For<IRequestContext>();
        context.TenantId.Returns("tenant-z");

        await Fail(async _ => throw await Api(HttpStatusCode.TooManyRequests), context: context);
        var (handler, _) = Create(context);
        await handler.Handle(new ScriptedRequest { Script = _ => Task.FromResult("ok") }, CancellationToken.None);

        var measurements = metrics.Tags("encina.outbound.requests_total", nameof(ScriptedRequest));
        measurements.Count.ShouldBe(2);
        measurements.ShouldAllBe(t => (string?)t["encina.tenant_id"] == "tenant-z");
        measurements.Single(t => (string)t["outcome"]! == "failure")["error_code"].ShouldBe(OutboundErrorCodes.RateLimited);
        measurements.SelectMany(t => t.Values).Select(v => v?.ToString()).ShouldNotContain(BodySentinel);
        var activity = activities.Activities(nameof(ScriptedRequest)).First(a => a.GetTagItem("Encina.failure_code") is not null);
        activity.GetTagItem("http.status").ShouldBe(429);
        activity.GetTagItem("transient").ShouldBe(true);
    }
}
