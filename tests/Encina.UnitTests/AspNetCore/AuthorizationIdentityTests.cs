using System.Net;
using System.Security.Claims;
using Encina.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.AspNetCore;

/// <summary>
/// The request identity that <c>UseEncinaContext()</c> binds, end to end through <c>IEncina.Send</c>
/// and the <c>[Authorize]</c> gate on a <see cref="TestServer"/> (#1705, Phase 3 task 9): the claim
/// map, the anonymous mappings (no subject, reserved subject) and their denial, the custom claim map,
/// the injected clock and the absence of leaks between requests.
/// </summary>
public sealed class AuthorizationIdentityTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 6, 12, 0, 0, TimeSpan.Zero);

    [Authorize]
    public sealed record WhoAmI : ICommand<string>;

    public sealed class WhoAmIHandler(IRequestContextAccessor accessor) : IRequestHandler<WhoAmI, string>
    {
        public Task<Either<EncinaError, string>> Handle(WhoAmI request, CancellationToken cancellationToken)
        {
            var context = accessor.RequestContext!;
            return Task.FromResult(Right<EncinaError, string>($"{context.UserId}|{context.Timestamp:O}"));
        }
    }

    private static Task<IHost> StartAsync(Action<RequestIdentityOptions>? claimMap = null)
    {
        var builder = new HostBuilder().ConfigureWebHost(web =>
        {
            web.UseTestServer();
            web.ConfigureServices(services =>
            {
                services.AddFakeLogging();
                services.AddRouting();
                services.AddSingleton<TimeProvider>(new FakeTimeProvider(Now));
                services.AddEncinaAspNetCore();
                services.AddEncinaRequestIdentity(claimMap);
                services.AddEncina();
                services.AddEncinaAuthorization();
                services.AddScoped<IRequestHandler<WhoAmI, string>, WhoAmIHandler>();
            });
            web.Configure(app =>
            {
                app.UseRouting();
                // Authentication stand-in: X-Claims "type=value;type=value" becomes an authenticated principal.
                Microsoft.AspNetCore.Builder.UseExtensions.Use(app, async (context, next) =>
                {
                    if (context.Request.Headers.TryGetValue("X-Claims", out var raw))
                    {
                        var claims = raw.ToString().Split(';', StringSplitOptions.RemoveEmptyEntries)
                            .Select(static pair => pair.Split('=', 2))
                            .Select(static parts => new Claim(parts[0], parts[1]));
                        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestScheme"));
                    }

                    await next();
                });
                app.UseEncinaContext();
                app.UseEndpoints(endpoints => endpoints.MapGet("/send", (RequestDelegate)(async context =>
                {
                    var result = await context.RequestServices.GetRequiredService<IEncina>().Send(new WhoAmI());
                    await context.Response.WriteAsync(result.Match(Right: value => value, Left: error => error.GetCode().IfNone("left")));
                })));
            });
        });
        return builder.StartAsync();
    }

    private static async Task<string> SendAsync(IHost host, string? claims)
    {
        using var client = host.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/send", UriKind.Relative));
        if (claims is not null)
        {
            request.Headers.Add("X-Claims", claims);
        }

        using var response = await client.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await response.Content.ReadAsStringAsync();
    }

    private static List<int> EventIds(IHost host) =>
        [.. host.Services.GetFakeLogCollector().GetSnapshot().Select(static record => record.Id.Id)];

    [Fact]
    public async Task AnAuthenticatedSubject_IsTheCallerTheGateAndTheHandlerSee_StampedByTheInjectedClock()
    {
        using var host = await StartAsync();

        var body = await SendAsync(host, "sub=alice");

        body.ShouldBe($"alice|{Now:O}");
    }

    [Fact]
    public async Task AnAnonymousRequest_IsDeniedByAuthorize()
    {
        using var host = await StartAsync();

        (await SendAsync(host, claims: null)).ShouldBe(EncinaErrorCodes.AuthorizationUnauthenticated);
    }

    [Fact]
    public async Task AnAuthenticatedPrincipalWithoutSubject_MapsToAnonymous_LogsWarning162_AndIsDenied()
    {
        using var host = await StartAsync();

        var body = await SendAsync(host, "email=alice@example.test");

        body.ShouldBe(EncinaErrorCodes.AuthorizationUnauthenticated);
        EventIds(host).ShouldContain(162);
    }

    [Fact]
    public async Task AReservedServiceSubject_MapsToAnonymous_LogsWarning163_AndIsDenied()
    {
        using var host = await StartAsync();

        var body = await SendAsync(host, "sub=service:billing-job");

        body.ShouldBe(EncinaErrorCodes.AuthorizationUnauthenticated);
        EventIds(host).ShouldContain(163);
    }

    [Fact]
    public async Task ACustomUserIdClaimType_IsHonoured()
    {
        using var host = await StartAsync(options =>
        {
            options.UserIdClaimTypes.Clear();
            options.UserIdClaimTypes.Add("employee_id");
        });

        (await SendAsync(host, "employee_id=e-42;sub=ignored")).ShouldStartWith("e-42|");
    }

    [Fact]
    public async Task SequentialRequests_NeverSeeEachOthersIdentity()
    {
        using var host = await StartAsync();

        var first = await SendAsync(host, "sub=alice");
        var anonymous = await SendAsync(host, claims: null);
        var second = await SendAsync(host, "sub=bob");

        first.ShouldStartWith("alice|");
        anonymous.ShouldBe(EncinaErrorCodes.AuthorizationUnauthenticated);
        second.ShouldStartWith("bob|");
    }
}
