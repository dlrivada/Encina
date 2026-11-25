using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Encina.Cdc.Debezium;
using Microsoft.Extensions.Logging.Testing;
using Shouldly;

namespace Encina.UnitTests.Cdc.Debezium;

/// <summary>
/// Unit tests for <see cref="DebeziumHttpListener"/>, driven through a real loopback listener.
/// </summary>
public sealed class DebeziumHttpListenerTests
{
    private const string Path = "/debezium";

    [Fact]
    public async Task Post_ValidJson_Returns200AndWritesEventToChannel()
    {
        await using var sut = await StartAsync(capacity: 10);

        var response = await sut.PostAsync("""{"id":"abc","value":42}""");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        sut.Channel.Reader.TryRead(out var element).ShouldBeTrue();
        element.GetProperty("id").GetString().ShouldBe("abc");
        element.GetProperty("value").GetInt32().ShouldBe(42);
    }

    [Fact]
    public async Task Post_MalformedJson_Returns400AndWritesNothing()
    {
        await using var sut = await StartAsync(capacity: 10);

        var response = await sut.PostAsync("{not json");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        sut.Channel.Reader.TryRead(out _).ShouldBeFalse();
    }

    [Fact]
    public async Task Post_WhitespaceBody_Returns400()
    {
        await using var sut = await StartAsync(capacity: 10);

        var response = await sut.PostAsync("   ");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_ChannelFull_Returns503()
    {
        await using var sut = await StartAsync(capacity: 1);

        (await sut.PostAsync("{}")).StatusCode.ShouldBe(HttpStatusCode.OK);
        var second = await sut.PostAsync("{}");

        second.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Get_Returns405()
    {
        await using var sut = await StartAsync(capacity: 10);

        var response = await sut.Client.GetAsync(sut.Url);

        response.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public async Task Post_BearerTokenConfigured_RejectsMissingOrWrongTokenAndAcceptsMatch()
    {
        await using var sut = await StartAsync(capacity: 10, bearerToken: "secret");

        (await sut.PostAsync("{}")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await sut.PostAsync("{}", "Bearer wrong")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await sut.PostAsync("{}", "Bearer secret")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static async Task<ListenerHarness> StartAsync(int capacity, string? bearerToken = null)
    {
        var port = GetFreePort();
        var options = new DebeziumCdcOptions
        {
            ListenUrl = "http://127.0.0.1",
            ListenPort = port,
            ListenPath = Path,
            BearerToken = bearerToken,
            ChannelCapacity = capacity,
            MaxListenerRetries = 0
        };
        var channel = Channel.CreateBounded<JsonElement>(capacity);
        var listener = new DebeziumHttpListener(options, channel, new FakeLogger<DebeziumHttpListener>());
        await listener.StartAsync(CancellationToken.None);

        var harness = new ListenerHarness(listener, channel, $"http://127.0.0.1:{port}{Path}/");
        await harness.WaitUntilListeningAsync();
        return harness;
    }

    private static int GetFreePort()
    {
        using var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        return ((IPEndPoint)probe.LocalEndpoint).Port;
    }

    private sealed class ListenerHarness(DebeziumHttpListener listener, Channel<JsonElement> channel, string url)
        : IAsyncDisposable
    {
        public Channel<JsonElement> Channel { get; } = channel;

        public string Url { get; } = url;

        public HttpClient Client { get; } = new();

        public async Task WaitUntilListeningAsync()
        {
            // The hosted service binds asynchronously; probe with GET (405) until it answers.
            for (var attempt = 0; attempt < 100; attempt++)
            {
                try
                {
                    using var probe = await Client.GetAsync(Url);
                    return;
                }
                catch (HttpRequestException)
                {
                    await Task.Delay(50);
                }
            }

            throw new InvalidOperationException("The Debezium listener did not start.");
        }

        public Task<HttpResponseMessage> PostAsync(string body, string? authorization = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, Url)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };

            if (authorization is not null)
            {
                request.Headers.TryAddWithoutValidation("Authorization", authorization);
            }

            return Client.SendAsync(request);
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await listener.StopAsync(CancellationToken.None);
            listener.Dispose();
        }
    }
}
