using System.Security.Claims;
using Encina.AspNetCore;
using Encina.Testing;
using LanguageExt;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace Encina.UnitTests.AspNetCore;

/// <summary>
/// Spike reproduction for finding N1 (verification spike, branch spike/verify-request-context):
/// <c>Encina.Send</c> builds a brand-new, always-empty <see cref="IRequestContext"/> for every
/// pipeline behavior via <c>RequestContext.Create()</c>
/// (src/Encina/Core/Encina.cs, <c>RequestHandlerWrapper.Handle</c>, ~line 131), instead of reading the
/// context that <see cref="EncinaContextMiddleware"/> already extracted from the HTTP request (user id,
/// tenant id, ...) and stored on <see cref="IRequestContextAccessor"/>
/// (src/Encina.AspNetCore/EncinaContextMiddleware.cs). <see cref="IEncina.Send{TResponse}"/> has no
/// overload that accepts an <see cref="IRequestContext"/>, so there is no supported way for a caller —
/// HTTP or otherwise — to hand ambient context to the pipeline. The only two things ever present on
/// the context handlers/behaviors receive are the correlation id (from <see cref="System.Diagnostics.Activity.Current"/>)
/// and the timestamp; <c>UserId</c> and <c>TenantId</c> are always <c>null</c>.
/// </summary>
/// <remarks>
/// This reproduces the full path end-to-end: an ASP.NET Core <see cref="TestServer"/> host with
/// <see cref="EncinaContextMiddleware"/> wired in front of an authenticated request carrying a tenant
/// header, sending a command through <c>IEncina.Send</c>, and a pipeline behavior that captures both
/// (a) the <see cref="IRequestContext"/> it receives as a parameter and (b) what
/// <see cref="IRequestContextAccessor"/> holds at the same moment - proving the middleware did its job
/// correctly and the disconnect is entirely inside the mediator's <c>Send</c> path.
/// </remarks>
public class RequestContextPropagationSpikeTests
{
    private sealed class ContextCapture
    {
        public bool BehaviorInvoked { get; set; }
        public string? BehaviorContextUserId { get; set; }
        public string? BehaviorContextTenantId { get; set; }
        public string? AccessorUserId { get; set; }
        public string? AccessorTenantId { get; set; }
    }

    private sealed record CapturingCommand : ICommand<Unit>;

    private sealed class CapturingCommandHandler : IRequestHandler<CapturingCommand, Unit>
    {
        public Task<Either<EncinaError, Unit>> Handle(CapturingCommand request, CancellationToken cancellationToken)
            => Task.FromResult(LanguageExt.Prelude.Right<EncinaError, Unit>(Unit.Default));
    }

    private sealed class CapturingContextPipelineBehavior(ContextCapture capture, IRequestContextAccessor? accessor = null)
        : IPipelineBehavior<CapturingCommand, Unit>
    {
        public async ValueTask<Either<EncinaError, Unit>> Handle(
            CapturingCommand request,
            IRequestContext context,
            RequestHandlerCallback<Unit> nextStep,
            CancellationToken cancellationToken)
        {
            capture.BehaviorInvoked = true;
            capture.BehaviorContextUserId = context.UserId;
            capture.BehaviorContextTenantId = context.TenantId;
            // What EncinaContextMiddleware actually stored for this same HTTP request/scope
            // (null in the non-HTTP scenario, where no accessor is registered at all).
            capture.AccessorUserId = accessor?.RequestContext?.UserId;
            capture.AccessorTenantId = accessor?.RequestContext?.TenantId;
            return await nextStep().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// CONFIRMED (N1, HTTP path): after <see cref="EncinaContextMiddleware"/> correctly extracts the
    /// authenticated user id and the <c>X-Tenant-ID</c> header into <see cref="IRequestContextAccessor"/>,
    /// a pipeline behavior invoked via <c>IEncina.Send</c> for that same HTTP request should observe the
    /// same user id / tenant id on its <see cref="IRequestContext"/> parameter. Instead it observes an
    /// empty context.
    /// </summary>
    [Fact]
    public async Task Send_ThroughHttpPipeline_BehaviorContext_ShouldCarryTheSameUserAndTenant_AsTheAccessor()
    {
        // Arrange
        var capture = new ContextCapture();

        var builder = new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(services =>
                {
                    services.AddLogging();
                    services.AddSingleton(capture);
                    services.AddEncinaAspNetCore();
                    services.AddEncina();
                    services.AddScoped<IRequestHandler<CapturingCommand, Unit>, CapturingCommandHandler>();
                    services.AddScoped<IPipelineBehavior<CapturingCommand, Unit>, CapturingContextPipelineBehavior>();
                });
                web.Configure(app =>
                {
                    // Simulates authentication middleware running before Encina's context middleware,
                    // exactly as EncinaContextMiddleware's own docs prescribe (app.UseAuthentication()
                    // before app.UseEncinaContext()).
                    // Fully qualified to avoid overload-resolution ambiguity between
                    // Microsoft.AspNetCore.Builder.UseExtensions.Use and LanguageExt's own
                    // extension methods named Use/Run, both in scope via the project's global usings.
                    Microsoft.AspNetCore.Builder.UseExtensions.Use(app, async (httpContext, next) =>
                    {
                        var identity = new ClaimsIdentity(
                            [new Claim(ClaimTypes.NameIdentifier, "user-123")],
                            authenticationType: "TestScheme");
                        httpContext.User = new ClaimsPrincipal(identity);
                        await next();
                    });

                    app.UseEncinaContext();

                    Microsoft.AspNetCore.Builder.RunExtensions.Run(app, async httpContext =>
                    {
                        var encina = httpContext.RequestServices.GetRequiredService<IEncina>();
                        var result = await encina.Send(new CapturingCommand());
                        httpContext.Response.StatusCode = result.IsRight ? StatusCodes.Status200OK : StatusCodes.Status500InternalServerError;
                    });
                });
            });

        using var host = await builder.StartAsync();
        using var client = host.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Tenant-ID", "tenant-abc");

        // Act
        using var response = await client.GetAsync("/send");

        // Assert
        response.EnsureSuccessStatusCode();
        capture.BehaviorInvoked.ShouldBeTrue();

        // Sanity check: the middleware itself worked and stored the right values on the accessor.
        capture.AccessorUserId.ShouldBe("user-123");
        capture.AccessorTenantId.ShouldBe("tenant-abc");

        // Expected (correct) behavior: the same values should reach the pipeline behavior's context.
        // Actual (current, buggy) behavior: both are null, because Encina.Send builds a fresh
        // RequestContext.Create() instead of reading IRequestContextAccessor.RequestContext.
        capture.BehaviorContextUserId.ShouldBe("user-123");
        capture.BehaviorContextTenantId.ShouldBe("tenant-abc");
    }

    /// <summary>
    /// CONFIRMED (N1, non-HTTP path): outside of ASP.NET Core entirely (e.g. a Hangfire/Quartz
    /// background job with no <see cref="IRequestContextAccessor"/> registered at all), there is no
    /// way to supply a user id or tenant id to <c>IEncina.Send</c> - not through an overload, not
    /// through DI, not through any ambient mechanism. The context handlers/behaviors observe is always
    /// freshly created and always empty for these fields.
    /// </summary>
    [Fact]
    public async Task Send_OutsideAnyHttpContext_HasNoSupportedWayToCarryUserOrTenant()
    {
        // Arrange - a plain DI container, the way a background job host would look. No ASP.NET Core,
        // no IRequestContextAccessor registered anywhere.
        var capture = new ContextCapture();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(capture);
        services.AddEncina();
        services.AddScoped<IRequestHandler<CapturingCommand, Unit>, CapturingCommandHandler>();
        services.AddScoped<IPipelineBehavior<CapturingCommand, Unit>, CapturingContextPipelineBehavior>();

        // Pretend the background job "knows" who it is acting for - there is no parameter on Send,
        // no ambient accessor, and no other supported extension point to convey that.
        const string intendedUserId = "background-job-owner";
        const string intendedTenantId = "tenant-for-the-job";

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();

        // Act
        var result = await encina.Send(new CapturingCommand());

        // Assert
        result.ShouldBeSuccess();
        capture.BehaviorInvoked.ShouldBeTrue();

        // Expected (if a supported non-HTTP propagation mechanism existed): the job's intended
        // identity would show up on the context.
        // Actual (current, buggy) behavior: there is no such mechanism, so both are always null,
        // regardless of what the caller "intended".
        capture.BehaviorContextUserId.ShouldBe(intendedUserId);
        capture.BehaviorContextTenantId.ShouldBe(intendedTenantId);
    }
}
