using System.Security.Claims;
using BenchmarkDotNet.Attributes;
using Encina.AspNetCore.Blazor;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.AspNetCore.Benchmarks;

/// <summary>
/// Cost of binding the request identity (#1705, Phase 3 task 10, Q4): one inbound scope per HTTP
/// request through <c>UseEncinaContext()</c>, and one per Blazor circuit activity, against the same
/// pipeline without a scope. Shows whether the scope closure needs <c>TState</c> overloads.
/// </summary>
[MemoryDiagnoser]
[MarkdownExporter]
public class RequestIdentityScopeBenchmarks
{
    private ServiceProvider _services = null!;
    private IServiceScope _circuitScope = null!;
    private RequestDelegate _withoutScope = null!;
    private RequestDelegate _withScope = null!;
    private Func<CircuitInboundActivityContext, Task> _activity = null!;
    private ClaimsPrincipal _user = null!;

    [GlobalSetup]
    public void Setup()
    {
        var collection = new ServiceCollection();
        collection.AddLogging();
        collection.AddEncinaAspNetCore();
        collection.AddScoped<AuthenticationStateProvider, FixedAuthenticationStateProvider>();
        collection.AddEncinaBlazorAuthorization();
        _services = collection.BuildServiceProvider();

        _user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", "benchmark-user"), new Claim(ClaimTypes.Role, "Admin"), new Claim("permission", "orders:read")],
            "Benchmark"));

        var plain = new ApplicationBuilder(_services);
        Microsoft.AspNetCore.Builder.RunExtensions.Run(plain, static _ => Task.CompletedTask);
        _withoutScope = plain.Build();

        var scoped = new ApplicationBuilder(_services);
        scoped.UseEncinaContext();
        Microsoft.AspNetCore.Builder.RunExtensions.Run(scoped, static _ => Task.CompletedTask);
        _withScope = scoped.Build();

        _circuitScope = _services.CreateScope();
        var handler = _circuitScope.ServiceProvider.GetServices<CircuitHandler>().OfType<RequestIdentityCircuitHandler>().Single();
        _activity = handler.CreateInboundActivityHandler(static _ => Task.CompletedTask);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _circuitScope.Dispose();
        _services.Dispose();
    }

    [Benchmark(Baseline = true)]
    public Task RequestWithoutScope() => _withoutScope(NewContext(_user));

    [Benchmark]
    public Task AnonymousRequestScope() => _withScope(NewContext(user: null));

    [Benchmark]
    public Task AuthenticatedRequestScope() => _withScope(NewContext(_user));

    [Benchmark]
    public Task CircuitActivityScope() => _activity(null!);

    private DefaultHttpContext NewContext(ClaimsPrincipal? user)
    {
        var context = new DefaultHttpContext { RequestServices = _services };
        if (user is not null)
        {
            context.User = user;
        }

        context.Request.Headers["X-Correlation-ID"] = "benchmark-correlation";
        return context;
    }

    private sealed class FixedAuthenticationStateProvider : AuthenticationStateProvider
    {
        private static readonly Task<AuthenticationState> State = Task.FromResult(new AuthenticationState(new ClaimsPrincipal(
            new ClaimsIdentity([new Claim("sub", "circuit-user")], "Benchmark"))));

        public override Task<AuthenticationState> GetAuthenticationStateAsync() => State;
    }
}
