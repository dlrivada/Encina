using System.Net;
using System.Security.Claims;
using Encina.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Encina.UnitTests.Tenancy.AspNetCore;

/// <summary>
/// UseEncinaContext() followed by UseTenantResolution() on a real pipeline (#2686): the verified claim
/// wins over the tenant header, and a header that disagrees is answered 403, never the header's tenant.
/// </summary>
public sealed class TenantHeaderWithEncinaContextPipelineTests
{
    private static async Task<(HttpStatusCode Status, string? Tenant)> SendAsync(
        string? claimTenant, string? headerTenant, bool trustEncinaContextHeader)
    {
        string? seenTenant = null;
        using var host = await new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(services =>
                {
                    services.AddLogging();
                    services.AddEncina();
                    services.AddEncinaAspNetCore(options => options.TrustTenantIdHeader = trustEncinaContextHeader);
                    services.AddEncinaTenancy(options => options.ValidateTenantOnRequest = false);
                    services.AddEncinaTenancyAspNetCore(options =>
                    {
                        options.HeaderResolver.Enabled = true;
                        options.HeaderResolver.TrustAnyPeer = true;
                    });
                });
                web.Configure(app =>
                {
                    UseExtensions.Use(app, async (httpContext, next) =>
                    {
                        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "user-1") };
                        if (claimTenant is not null)
                        {
                            claims.Add(new Claim("tenant_id", claimTenant));
                        }

                        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestScheme"));
                        await next();
                    });
                    app.UseEncinaContext();
                    app.UseTenantResolution();
                    RunExtensions.Run(app, httpContext =>
                    {
                        seenTenant = httpContext.RequestServices.GetRequiredService<IRequestContextAccessor>().RequestContext?.TenantId;
                        return Task.CompletedTask;
                    });
                });
            })
            .StartAsync();
        using var client = host.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/", UriKind.Relative));
        if (headerTenant is not null)
        {
            request.Headers.Add("X-Tenant-ID", headerTenant);
        }

        using var response = await client.SendAsync(request);
        return (response.StatusCode, seenTenant);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ClaimAWithHeaderB_IsAnswered403_AndNeverRunsAsB(bool trustEncinaContextHeader)
    {
        var (status, tenant) = await SendAsync("tenant-a", "tenant-b", trustEncinaContextHeader);

        status.ShouldBe(HttpStatusCode.Forbidden);
        tenant.ShouldBeNull();
    }

    [Fact]
    public async Task ClaimAWithHeaderA_RunsAsA()
    {
        var (status, tenant) = await SendAsync("tenant-a", "tenant-a", trustEncinaContextHeader: true);

        status.ShouldBe(HttpStatusCode.OK);
        tenant.ShouldBe("tenant-a");
    }

    [Fact]
    public async Task ClaimAWithoutHeader_RunsAsA()
    {
        var (status, tenant) = await SendAsync("tenant-a", null, trustEncinaContextHeader: false);

        status.ShouldBe(HttpStatusCode.OK);
        tenant.ShouldBe("tenant-a");
    }
}
