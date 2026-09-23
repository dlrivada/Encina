using System.Globalization;
using System.Security.Claims;
using Encina.AspNetCore;
using Encina.Messaging.Inbox;
using Encina.Messaging.Serialization;
using Encina.Testing;
using Encina.Testing.Fakes.Models;
using Encina.Testing.Fakes.Stores;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace Encina.UnitTests.AspNetCore;

/// <summary>
/// End-to-end propagation of the request context from <see cref="EncinaContextMiddleware"/> into the
/// Encina pipeline (issue #1147).
/// </summary>
/// <remarks>
/// <para>
/// Ported from the verification spike (branch <c>spike/verify-request-context</c>, commit 9215e9a8),
/// which reproduced the bug: <c>IEncina.Send</c> used to build an empty <see cref="IRequestContext"/>
/// for every pipeline, so behaviors never saw the user, tenant or idempotency key that the
/// middleware had extracted.
/// </para>
/// <para>
/// The HTTP tests run a real ASP.NET Core pipeline on a <see cref="TestServer"/>: an authentication
/// stand-in, <c>UseEncinaContext()</c>, and an endpoint that calls <c>IEncina.Send</c>.
/// </para>
/// </remarks>
public sealed class RequestContextPropagationTests
{
    private sealed class ContextCapture
    {
        public bool BehaviorInvoked { get; set; }
        public string? BehaviorContextUserId { get; set; }
        public string? BehaviorContextTenantId { get; set; }
        public string? BehaviorContextIdempotencyKey { get; set; }
        public string? BehaviorContextCorrelationId { get; set; }
        public string? AccessorUserId { get; set; }
        public string? AccessorTenantId { get; set; }
    }

    private sealed record CapturingCommand : ICommand<Unit>;

    private sealed class CapturingCommandHandler : IRequestHandler<CapturingCommand, Unit>
    {
        public Task<Either<EncinaError, Unit>> Handle(CapturingCommand request, CancellationToken cancellationToken)
            => Task.FromResult(LanguageExt.Prelude.Right<EncinaError, Unit>(Unit.Default));
    }

    private sealed class CapturingContextPipelineBehavior(ContextCapture capture, IRequestContextAccessor accessor)
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
            capture.BehaviorContextIdempotencyKey = context.IdempotencyKey;
            capture.BehaviorContextCorrelationId = context.CorrelationId;
            capture.AccessorUserId = accessor.RequestContext?.UserId;
            capture.AccessorTenantId = accessor.RequestContext?.TenantId;
            return await nextStep().ConfigureAwait(false);
        }
    }

    private sealed record IdempotentCounterCommand : ICommand<int>, IIdempotentRequest;

    private sealed class HandlerInvocations
    {
        private int _count;

        public int Count => Volatile.Read(ref _count);

        public int Increment() => Interlocked.Increment(ref _count);
    }

    private sealed class IdempotentCounterHandler(HandlerInvocations invocations) : IRequestHandler<IdempotentCounterCommand, int>
    {
        public Task<Either<EncinaError, int>> Handle(IdempotentCounterCommand request, CancellationToken cancellationToken)
            => Task.FromResult(LanguageExt.Prelude.Right<EncinaError, int>(invocations.Increment()));
    }

    private sealed class FakeInboxMessageFactory : IInboxMessageFactory
    {
        public IInboxMessage Create(string messageId, string requestType, DateTime receivedAtUtc, DateTime expiresAtUtc, InboxMetadata? metadata)
            => new FakeInboxMessage
            {
                MessageId = messageId,
                RequestType = requestType,
                ReceivedAtUtc = receivedAtUtc,
                ExpiresAtUtc = expiresAtUtc
            };
    }

    [Fact]
    public async Task Send_ThroughHttpPipeline_BehaviorContext_CarriesTheSameUserAndTenant_AsTheAccessor()
    {
        // Arrange
        var capture = new ContextCapture();
        using var host = await StartHostAsync(
            services =>
            {
                services.AddSingleton(capture);
                services.AddScoped<IRequestHandler<CapturingCommand, Unit>, CapturingCommandHandler>();
                services.AddScoped<IPipelineBehavior<CapturingCommand, Unit>, CapturingContextPipelineBehavior>();
            },
            async (encina, httpContext) =>
            {
                var result = await encina.Send(new CapturingCommand());
                httpContext.Response.StatusCode = result.IsRight ? StatusCodes.Status200OK : StatusCodes.Status500InternalServerError;
            });
        using var client = host.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Tenant-ID", "tenant-abc");

        // Act
        using var response = await client.GetAsync(new Uri("/send", UriKind.Relative));

        // Assert
        response.EnsureSuccessStatusCode();
        capture.BehaviorInvoked.ShouldBeTrue();
        capture.AccessorUserId.ShouldBe("user-123");
        capture.AccessorTenantId.ShouldBe("tenant-abc");
        capture.BehaviorContextUserId.ShouldBe("user-123");
        capture.BehaviorContextTenantId.ShouldBe("tenant-abc");
    }

    [Fact]
    public async Task Send_ThroughHttpPipeline_BehaviorContext_CarriesIdempotencyKeyAndCorrelationIdHeaders()
    {
        // Arrange
        var capture = new ContextCapture();
        using var host = await StartHostAsync(
            services =>
            {
                services.AddSingleton(capture);
                services.AddScoped<IRequestHandler<CapturingCommand, Unit>, CapturingCommandHandler>();
                services.AddScoped<IPipelineBehavior<CapturingCommand, Unit>, CapturingContextPipelineBehavior>();
            },
            async (encina, httpContext) =>
            {
                var result = await encina.Send(new CapturingCommand());
                httpContext.Response.StatusCode = result.IsRight ? StatusCodes.Status200OK : StatusCodes.Status500InternalServerError;
            });
        using var client = host.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Tenant-ID", "tenant-abc");
        client.DefaultRequestHeaders.Add("X-Idempotency-Key", "idem-42");
        client.DefaultRequestHeaders.Add("X-Correlation-ID", "corr-7");

        // Act
        using var response = await client.GetAsync(new Uri("/send", UriKind.Relative));

        // Assert
        response.EnsureSuccessStatusCode();
        capture.BehaviorContextUserId.ShouldBe("user-123");
        capture.BehaviorContextTenantId.ShouldBe("tenant-abc");
        capture.BehaviorContextIdempotencyKey.ShouldBe("idem-42");
        capture.BehaviorContextCorrelationId.ShouldBe("corr-7");
    }

    [Fact]
    public async Task InboxPipelineBehavior_OverHttp_ProcessesARepeatedIdempotencyKeyOnlyOnce()
    {
        // Arrange
        var invocations = new HandlerInvocations();
        var inboxStore = new FakeInboxStore();
        using var host = await StartHostAsync(
            services =>
            {
                services.AddSingleton(invocations);
                services.AddSingleton<IInboxStore>(inboxStore);
                services.AddSingleton(new InboxOptions());
                services.AddSingleton<IInboxMessageFactory, FakeInboxMessageFactory>();
                services.AddSingleton<IMessageSerializer, JsonMessageSerializer>();
                services.AddScoped<InboxOrchestrator>();
                services.AddScoped<IRequestHandler<IdempotentCounterCommand, int>, IdempotentCounterHandler>();
                services.AddScoped<IPipelineBehavior<IdempotentCounterCommand, int>, InboxPipelineBehavior<IdempotentCounterCommand, int>>();
            },
            async (encina, httpContext) =>
            {
                var result = await encina.Send(new IdempotentCounterCommand());
                await result.Match(
                    Right: count => httpContext.Response.WriteAsync(count.ToString(CultureInfo.InvariantCulture)),
                    Left: error =>
                    {
                        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
                        return httpContext.Response.WriteAsync(error.Message);
                    });
            });
        using var client = host.GetTestClient();

        // Act
        var first = await SendWithIdempotencyKeyAsync(client, "order-1");
        var repeated = await SendWithIdempotencyKeyAsync(client, "order-1");
        var other = await SendWithIdempotencyKeyAsync(client, "order-2");
        var missingKey = await SendWithIdempotencyKeyAsync(client, idempotencyKey: null);

        // Assert
        first.ShouldBe((StatusCodes.Status200OK, "1"));
        repeated.ShouldBe((StatusCodes.Status200OK, "1"));
        other.ShouldBe((StatusCodes.Status200OK, "2"));
        missingKey.Status.ShouldBe(StatusCodes.Status409Conflict);
        invocations.Count.ShouldBe(2);
        inboxStore.IsMessageProcessed("order-1").ShouldBeTrue();
        inboxStore.IsMessageProcessed("order-2").ShouldBeTrue();
    }

    [Fact]
    public async Task Send_OutsideAnyHttpContext_WithExplicitContext_CarriesUserAndTenant()
    {
        // Arrange - a plain DI container, the way a background job host looks: no ASP.NET Core.
        var capture = new ContextCapture();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(capture);
        services.AddEncina();
        services.AddScoped<IRequestHandler<CapturingCommand, Unit>, CapturingCommandHandler>();
        services.AddScoped<IPipelineBehavior<CapturingCommand, Unit>, CapturingContextPipelineBehavior>();

        const string intendedUserId = "background-job-owner";
        const string intendedTenantId = "tenant-for-the-job";
        var jobContext = RequestContext.Create("job-correlation")
            .WithUserId(intendedUserId)
            .WithTenantId(intendedTenantId);

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();

        // Act
        var result = await encina.Send(new CapturingCommand(), jobContext);

        // Assert
        result.ShouldBeSuccess();
        capture.BehaviorInvoked.ShouldBeTrue();
        capture.BehaviorContextUserId.ShouldBe(intendedUserId);
        capture.BehaviorContextTenantId.ShouldBe(intendedTenantId);
        capture.BehaviorContextCorrelationId.ShouldBe("job-correlation");

        // The explicit context is also the ambient one while the pipeline runs.
        capture.AccessorUserId.ShouldBe(intendedUserId);
        capture.AccessorTenantId.ShouldBe(intendedTenantId);
    }

    private static async Task<(int Status, string Body)> SendWithIdempotencyKeyAsync(HttpClient client, string? idempotencyKey)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/orders", UriKind.Relative));
        if (idempotencyKey is not null)
        {
            request.Headers.Add("X-Idempotency-Key", idempotencyKey);
        }

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        return ((int)response.StatusCode, body);
    }

    private static Task<IHost> StartHostAsync(
        Action<IServiceCollection> configureServices,
        Func<IEncina, HttpContext, Task> endpoint)
    {
        var builder = new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(services =>
                {
                    services.AddLogging();
                    services.AddEncinaAspNetCore();
                    services.AddEncina();
                    configureServices(services);
                });
                web.Configure(app =>
                {
                    // Authentication stand-in, running before UseEncinaContext() as the middleware's
                    // documentation prescribes. Fully qualified to avoid the ambiguity with
                    // LanguageExt's Use/Run extension methods brought in by the global usings.
                    UseExtensions.Use(app, async (httpContext, next) =>
                    {
                        var identity = new ClaimsIdentity(
                            [new Claim(ClaimTypes.NameIdentifier, "user-123")],
                            authenticationType: "TestScheme");
                        httpContext.User = new ClaimsPrincipal(identity);
                        await next();
                    });

                    app.UseEncinaContext();

                    RunExtensions.Run(app, httpContext =>
                        endpoint(httpContext.RequestServices.GetRequiredService<IEncina>(), httpContext));
                });
            });

        return builder.StartAsync();
    }
}
