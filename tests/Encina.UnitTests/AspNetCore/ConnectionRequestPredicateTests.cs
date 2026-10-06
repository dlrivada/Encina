using Encina.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;

namespace Encina.UnitTests.AspNetCore;

/// <summary>
/// The connection-request predicate (#1705, Design 2 step 1, finding 1, minor 3, E3) over
/// <see cref="DefaultHttpContext"/> cases with <b>no</b> <see cref="IHttpWebSocketFeature"/>, the way a
/// request looks before <c>UseWebSockets</c> runs. Every case runs against both copies: the request
/// middleware's and the tenant middleware's.
/// </summary>
public sealed class ConnectionRequestPredicateTests
{
    public static TheoryData<string, bool> Cases => new()
    {
        { "upgrade:websocket", true },
        { "upgrade:h2c, WebSocket", true },
        { "upgrade:websocket/13", true },
        { "upgrade:h2c", false },
        { "upgrade-header-without-feature:websocket", false },
        { "connect:websocket", true },
        { "connect:webtransport", true },
        { "connect:null", true },
        { "not-connect:websocket", false },
        { "GET:text/event-stream;q=0", true },
        { "GET:TEXT/EVENT-STREAM", true },
        { "GET:application/json, text/event-stream", true },
        { "GET:text/event-stream;;;\"=", true },
        { "GET:application/json, text/event-stream x", true },
        { "GET:application/json, text/html", false },
        { "GET:text/event-streams", false },
        { "GET:*/*", false },
        { "GET:text/*", false },
        { "GET:", false },
        { "POST:text/event-stream", false },
        { "PUT:text/event-stream", false },
        { "HEAD:text/event-stream", false },
        { "hub-endpoint", true },
        { "plain-endpoint", false },
        { "plain-get", false }
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void TheRequestMiddlewarePredicate_ClassifiesTheCase(string scenario, bool expected) =>
        ConnectionRequestDetector.IsConnectionRequest(Build(scenario)).ShouldBe(expected, scenario);

    [Theory]
    [MemberData(nameof(Cases))]
    public void TheTenantMiddlewarePredicate_ClassifiesTheCaseTheSameWay(string scenario, bool expected) =>
        ConnectionRequests.IsConnectionRequest(Build(scenario)).ShouldBe(expected, scenario);

    [Fact]
    public void ACase_WithNoWebSocketFeature_IsNotAWebSocketRequestForAspNetCore()
    {
        // The reason the predicate reads the upgrade header: IsWebSocketRequest is false before UseWebSockets.
        var context = Build("upgrade:websocket");

        context.WebSockets.IsWebSocketRequest.ShouldBeFalse();
        ConnectionRequestDetector.IsConnectionRequest(context).ShouldBeTrue();
    }

    private static DefaultHttpContext Build(string scenario)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        var separator = scenario.IndexOf(':', StringComparison.Ordinal);
        var kind = separator < 0 ? scenario : scenario[..separator];
        var value = separator < 0 ? string.Empty : scenario[(separator + 1)..];

        switch (kind)
        {
            case "upgrade":
                context.Features.Set<IHttpUpgradeFeature>(new UpgradeFeature(true));
                context.Request.Headers.Upgrade = value;
                break;
            case "upgrade-header-without-feature":
                context.Request.Headers.Upgrade = value;
                break;
            case "connect":
            case "not-connect":
                context.Features.Set<IHttpExtendedConnectFeature>(new ExtendedConnectFeature(kind == "connect", value == "null" ? null : value));
                break;
            case "hub-endpoint":
                context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(new HubMetadata(typeof(Hub))), "hub"));
                break;
            case "plain-endpoint":
                context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, EndpointMetadataCollection.Empty, "plain"));
                break;
            case "plain-get":
                break;
            default:
                context.Request.Method = kind;
                context.Request.Headers.Accept = value;
                break;
        }

        return context;
    }

    private sealed class UpgradeFeature(bool upgradable) : IHttpUpgradeFeature
    {
        public bool IsUpgradableRequest => upgradable;

        public Task<Stream> UpgradeAsync() => throw new NotSupportedException();
    }

    private sealed class ExtendedConnectFeature(bool isExtendedConnect, string? protocol) : IHttpExtendedConnectFeature
    {
        public bool IsExtendedConnect => isExtendedConnect;

        public string? Protocol => protocol;

        public ValueTask<Stream> AcceptAsync() => throw new NotSupportedException();
    }
}
