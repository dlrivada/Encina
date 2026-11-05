using System.Net;
using System.Net.Http.Json;
using Encina.AspNetCore;
using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Abstractions;
using Encina.Compliance.DPIA.Model;
using Encina.Compliance.DPIA.ReadModels;
using LanguageExt;
using Encina.Testing.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.AspNetCore;

/// <summary>
/// Unit tests for <see cref="DPIAEndpointExtensions"/>.
/// </summary>
public sealed class DPIAEndpointExtensionsTests
{
    /// <summary>
    /// Holds the started test host and its HTTP client so both are disposed together.
    /// </summary>
    private sealed class TestHarness(IHost host, HttpClient client, CapturingLoggerProvider logs) : IAsyncDisposable
    {
        public IHost Host { get; } = host;

        public HttpClient Client { get; } = client;

        public CapturingLoggerProvider Logs { get; } = logs;

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await Host.StopAsync();
            Host.Dispose();
        }
    }

    private const string PolicyName = "DPOPolicy";
    private const string AuthHeader = "X-Test-Auth";
    private const string SecretUserId = "user-4711-secret";

    /// <summary>
    /// Authenticates a request when it carries the <see cref="AuthHeader"/> header; otherwise no result,
    /// so the authorization policy challenges with 401.
    /// </summary>
    private sealed class HeaderAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory loggerFactory,
        System.Text.Encodings.Web.UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, loggerFactory, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey(AuthHeader))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var identity = new System.Security.Claims.ClaimsIdentity("Test");
            var ticket = new AuthenticationTicket(new System.Security.Claims.ClaimsPrincipal(identity), Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    /// <summary>Collects every log entry written through the host's logging.</summary>
    private sealed class CapturingLoggerProvider : ILoggerProvider, ILogger
    {
        public List<(LogLevel Level, int EventId, string Message)> Entries { get; } = [];

        public ILogger CreateLogger(string categoryName) => this;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            lock (Entries)
            {
                Entries.Add((logLevel, eventId.Id, formatter(state, exception)));
            }
        }

        public void Dispose()
        {
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Reliability", "CA2012:Use ValueTasks correctly",
        Justification = "NSubstitute mock setup pattern for ValueTask-returning members.")]
    private static IDPIAService CreateService(
        IReadOnlyList<DPIAReadModel>? all = null,
        IReadOnlyList<DPIAReadModel>? expired = null,
        DPIAReadModel? singleAssessment = null,
        Either<EncinaError, Unit>? approveResult = null,
        Either<EncinaError, Unit>? rejectResult = null)
    {
        var service = Substitute.For<IDPIAService>();

        service.GetAllAssessmentsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, IReadOnlyList<DPIAReadModel>>>(
                Right<EncinaError, IReadOnlyList<DPIAReadModel>>(all ?? [])));

        service.GetExpiredAssessmentsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, IReadOnlyList<DPIAReadModel>>>(
                Right<EncinaError, IReadOnlyList<DPIAReadModel>>(expired ?? [])));

        if (singleAssessment is not null)
        {
            service.GetAssessmentAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(ValueTask.FromResult<Either<EncinaError, DPIAReadModel>>(
                    Right<EncinaError, DPIAReadModel>(singleAssessment)));
        }
        else
        {
            service.GetAssessmentAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(ValueTask.FromResult<Either<EncinaError, DPIAReadModel>>(
                    Left<EncinaError, DPIAReadModel>(DPIAErrors.AssessmentNotFound(Guid.Empty))));
        }

        service.ApproveAssessmentAsync(Arg.Any<Guid>(), Arg.Any<string>(),
                Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(approveResult
                ?? Right<EncinaError, Unit>(Unit.Default)));

        service.RejectAssessmentAsync(Arg.Any<Guid>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(rejectResult
                ?? Right<EncinaError, Unit>(Unit.Default)));

        return service;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Reliability", "CA2012:Use ValueTasks correctly",
        Justification = "NSubstitute mock setup pattern for ValueTask-returning members.")]
    private static IDPIATemplateProvider CreateTemplateProvider(
        IReadOnlyList<DPIATemplate>? templates = null)
    {
        var provider = Substitute.For<IDPIATemplateProvider>();
        provider.GetAllTemplatesAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, IReadOnlyList<DPIATemplate>>>(
                Right<EncinaError, IReadOnlyList<DPIATemplate>>(templates ?? [])));
        return provider;
    }

    private static async Task<TestHarness> CreateHarnessAsync(
        IDPIAService service,
        IDPIATemplateProvider? templateProvider = null,
        string prefix = "/api/dpia",
        string? contextTenant = null,
        bool authenticated = true,
        RequestIdentity? identity = null,
        bool withRequestContext = true)
    {
        var logs = new CapturingLoggerProvider();
        var builder = new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(services =>
                {
                    services.AddLogging(logging => logging.AddProvider(logs).SetMinimumLevel(LogLevel.Trace));
                    services.AddRouting();
                    services.AddAuthentication("Test")
                        .AddScheme<AuthenticationSchemeOptions, HeaderAuthHandler>("Test", _ => { });
                    services.AddAuthorization(options =>
                        options.AddPolicy(PolicyName, policy => policy.RequireAuthenticatedUser()));
                    services.AddSingleton(service);
                    services.AddSingleton(templateProvider ?? CreateTemplateProvider());
                    services.AddSingleton(Substitute.For<IDPIAAssessmentEngine>());
                    services.AddSingleton(TimeProvider.System);
                    services.Configure<DPIAOptions>(_ => { });
                    if (withRequestContext)
                    {
                        var requestContext = Substitute.For<IRequestContext>();
                        requestContext.TenantId.Returns(contextTenant);
                        requestContext.Identity.Returns(identity ?? TestIdentity.User(SecretUserId));
                        var accessor = Substitute.For<IRequestContextAccessor>();
                        accessor.RequestContext.Returns(requestContext);
                        services.AddSingleton(accessor);
                    }
                });
                web.Configure(app =>
                {
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapDPIAEndpoints(PolicyName, prefix);
                    });
                });
            });

        var host = await builder.StartAsync();
        var client = host.GetTestClient();
        if (authenticated)
        {
            client.DefaultRequestHeaders.Add(AuthHeader, "1");
        }

        return new TestHarness(host, client, logs);
    }

    // ── DTO defaults ────────────────────────────────────────────────────────

    [Fact]
    public void AssessDPIARequest_Defaults_AreEmpty()
    {
        var request = new AssessDPIARequest();

        request.ProcessingType.ShouldBeNull();
        request.DataCategories.ShouldBeEmpty();
        request.HighRiskTriggers.ShouldBeEmpty();
    }

    [Fact]
    public void AssessDPIARequest_PropertiesCanBeSet()
    {
        var request = new AssessDPIARequest
        {
            ProcessingType = "AutomatedDecisionMaking",
            DataCategories = ["BiometricData", "HealthData"],
            HighRiskTriggers = ["BiometricData"]
        };

        request.ProcessingType.ShouldBe("AutomatedDecisionMaking");
        request.DataCategories.Count.ShouldBe(2);
        request.HighRiskTriggers.Count.ShouldBe(1);
    }

    [Fact]
    public void RejectDPIARequest_DefaultReason_IsNull()
    {
        var request = new RejectDPIARequest();
        request.Reason.ShouldBeNull();
    }

    [Fact]
    public void RejectDPIARequest_ReasonCanBeSet()
    {
        var request = new RejectDPIARequest { Reason = "Insufficient mitigation." };
        request.Reason.ShouldBe("Insufficient mitigation.");
    }

    // ── Constants ───────────────────────────────────────────────────────────

    [Fact]
    public void ModuleIdHeader_Constant_Value()
    {
        DPIAEndpointExtensions.ModuleIdHeader.ShouldBe("X-Module-Id");
    }

    // ── Registration ────────────────────────────────────────────────────────

    private static IEndpointRouteBuilder CreateEndpointBuilder(string? registeredPolicy)
    {
        var services = new ServiceCollection();
        if (registeredPolicy is not null)
        {
            services.AddAuthorization(options =>
                options.AddPolicy(registeredPolicy, policy => policy.RequireAuthenticatedUser()));
        }

        var endpoints = Substitute.For<IEndpointRouteBuilder>();
        endpoints.ServiceProvider.Returns(services.BuildServiceProvider());
        endpoints.DataSources.Returns(new List<EndpointDataSource>());
        return endpoints;
    }

    [Fact]
    public void MapDPIAEndpoints_DefaultPrefix_ReturnsRouteGroupBuilder()
    {
        var endpoints = CreateEndpointBuilder(PolicyName);

        var group = endpoints.MapDPIAEndpoints(PolicyName);

        group.ShouldNotBeNull();
    }

    [Fact]
    public void MapDPIAEndpoints_CustomPrefix_ReturnsRouteGroupBuilder()
    {
        var endpoints = CreateEndpointBuilder(PolicyName);

        var group = endpoints.MapDPIAEndpoints(PolicyName, "/compliance/dpia");

        group.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void MapDPIAEndpoints_NullOrBlankPolicy_Throws(string? policy)
    {
        var endpoints = CreateEndpointBuilder(PolicyName);

        Should.Throw<ArgumentException>(() => endpoints.MapDPIAEndpoints(policy!));
    }

    [Fact]
    public void MapDPIAEndpoints_PolicyNotRegistered_FailsAtStartup()
    {
        var endpoints = CreateEndpointBuilder("OtherPolicy");

        var ex = Should.Throw<InvalidOperationException>(() => endpoints.MapDPIAEndpoints(PolicyName));

        ex.Message.ShouldContain(PolicyName);
    }

    [Fact]
    public void MapDPIAEndpoints_AuthorizationNotRegistered_FailsAtStartup()
    {
        var endpoints = CreateEndpointBuilder(registeredPolicy: null);

        Should.Throw<InvalidOperationException>(() => endpoints.MapDPIAEndpoints(PolicyName));
    }

    [Fact]
    public async Task EveryEndpoint_CarriesTheAuthorizationPolicyMetadata()
    {
        await using var harness = await CreateHarnessAsync(CreateService());
        var routes = harness.Host.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(e => e.RoutePattern.RawText!.StartsWith("/api/dpia", StringComparison.Ordinal))
            .ToList();

        routes.Count.ShouldBe(7);
        foreach (var route in routes)
        {
            route.Metadata.GetOrderedMetadata<IAuthorizeData>()
                .ShouldContain(d => d.Policy == PolicyName, route.RoutePattern.RawText);
        }
    }

    // ── Authorization: nothing is served anonymously ────────────────────────

    [Theory]
    [InlineData("GET", "/api/dpia/assessments")]
    [InlineData("GET", "/api/dpia/assessments/00000000-0000-0000-0000-000000000001")]
    [InlineData("POST", "/api/dpia/assessments/System.String/assess")]
    [InlineData("POST", "/api/dpia/assessments/00000000-0000-0000-0000-000000000001/approve")]
    [InlineData("POST", "/api/dpia/assessments/00000000-0000-0000-0000-000000000001/reject")]
    [InlineData("GET", "/api/dpia/templates")]
    [InlineData("GET", "/api/dpia/expired")]
    public async Task EveryEndpoint_Unauthenticated_IsDeniedAndTheServiceIsNotCalled(string method, string path)
    {
        var service = CreateService();
        await using var harness = await CreateHarnessAsync(service, authenticated: false);
        using var message = new HttpRequestMessage(new HttpMethod(method), new Uri(path, UriKind.Relative));

        using var response = await harness.Client.SendAsync(message);

        ((int)response.StatusCode).ShouldBeOneOf(401, 403);
        service.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task Approve_Unauthenticated_Returns401_AndChangesNothing()
    {
        var service = CreateService();
        await using var harness = await CreateHarnessAsync(service, authenticated: false);

        using var response = await harness.Client.PostAsync(
            new Uri($"/api/dpia/assessments/{Guid.NewGuid()}/approve", UriKind.Relative), content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        await service.DidNotReceive().ApproveAssessmentAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>());
    }

    // ── Defense in depth: no usable request identity ────────────────────────

    public static TheoryData<string> DeniedIdentityScenarios => new() { "no-context", "anonymous" };

    private static async Task<TestHarness> CreateDeniedHarnessAsync(IDPIAService service, string scenario) =>
        scenario == "no-context"
            ? await CreateHarnessAsync(service, withRequestContext: false)
            : await CreateHarnessAsync(service, identity: TestIdentity.Anonymous);

    [Theory]
    [MemberData(nameof(DeniedIdentityScenarios))]
    public async Task Approve_WithoutAuthenticatedRequestIdentity_Returns401_WithTheCode_AndDoesNotCallTheService(string scenario)
    {
        var service = CreateService();
        await using var harness = await CreateDeniedHarnessAsync(service, scenario);

        using var response = await harness.Client.PostAsync(
            new Uri($"/api/dpia/assessments/{Guid.NewGuid()}/approve", UriKind.Relative), content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await response.Content.ReadAsStringAsync()).ShouldContain("encina.authorization.unauthenticated");
        await service.DidNotReceive().ApproveAssessmentAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [MemberData(nameof(DeniedIdentityScenarios))]
    public async Task Reject_WithoutAuthenticatedRequestIdentity_Returns401_WithTheCode_AndDoesNotCallTheService(string scenario)
    {
        var service = CreateService();
        await using var harness = await CreateDeniedHarnessAsync(service, scenario);

        using var response = await harness.Client.PostAsync(
            new Uri($"/api/dpia/assessments/{Guid.NewGuid()}/reject", UriKind.Relative), content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await response.Content.ReadAsStringAsync()).ShouldContain("encina.authorization.unauthenticated");
        await service.DidNotReceive().RejectAssessmentAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Denial_LogsAWarningWithTheCodeOnly()
    {
        await using var harness = await CreateHarnessAsync(CreateService(), withRequestContext: false);

        using var response = await harness.Client.PostAsync(
            new Uri($"/api/dpia/assessments/{Guid.NewGuid()}/approve", UriKind.Relative), content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        var denial = harness.Logs.Entries.Single(e => e.EventId == 8896);
        denial.Level.ShouldBe(LogLevel.Warning);
        denial.Message.ShouldContain("encina.authorization.unauthenticated");
        denial.Message.ShouldNotContain(SecretUserId);
        denial.Message.ShouldNotContain("Authentication is required");
    }

    // ── The recorded actor is the request identity's user id ────────────────

    [Fact]
    public async Task Approve_RecordsTheRequestIdentityUserId_NeverDpo()
    {
        var service = CreateService();
        await using var harness = await CreateHarnessAsync(service);

        using var response = await harness.Client.PostAsync(
            new Uri($"/api/dpia/assessments/{Guid.NewGuid()}/approve", UriKind.Relative), content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        await service.Received(1).ApproveAssessmentAsync(
            Arg.Any<Guid>(), SecretUserId, Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>());
        await service.DidNotReceive().ApproveAssessmentAsync(
            Arg.Any<Guid>(), "DPO", Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>());
        harness.Logs.Entries.ShouldAllBe(e => !e.Message.Contains(SecretUserId, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Reject_RecordsTheRequestIdentityUserId_NeverDpo()
    {
        var service = CreateService();
        await using var harness = await CreateHarnessAsync(service);

        using var response = await harness.Client.PostAsJsonAsync(
            new Uri($"/api/dpia/assessments/{Guid.NewGuid()}/reject", UriKind.Relative),
            new RejectDPIARequest { Reason = "test" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        await service.Received(1).RejectAssessmentAsync(
            Arg.Any<Guid>(), SecretUserId, "test", Arg.Any<CancellationToken>());
        await service.DidNotReceive().RejectAssessmentAsync(
            Arg.Any<Guid>(), "DPO", Arg.Any<string>(), Arg.Any<CancellationToken>());
        harness.Logs.Entries.ShouldAllBe(e => !e.Message.Contains(SecretUserId, StringComparison.Ordinal));
    }

    // ── Endpoint execution: GET /assessments ────────────────────────────────

    [Fact]
    public async Task GetAssessments_EmptyList_ReturnsOk()
    {
        var service = CreateService();
        await using var harness = await CreateHarnessAsync(service);

        using var response = await harness.Client.GetAsync(new Uri("/api/dpia/assessments", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAssessments_PopulatedList_ReturnsOk()
    {
        var assessments = new List<DPIAReadModel>
        {
            new() { Id = Guid.NewGuid(), RequestTypeName = "MyApp.OrderCommand" }
        };
        var service = CreateService(all: assessments);
        await using var harness = await CreateHarnessAsync(service);

        using var response = await harness.Client.GetAsync(new Uri("/api/dpia/assessments", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── Endpoint execution: GET /assessments/{id} ───────────────────────────

    [Fact]
    public async Task GetAssessmentById_NotFound_Returns404()
    {
        var service = CreateService(); // singleAssessment = null → returns NotFound error
        await using var harness = await CreateHarnessAsync(service);

        using var response = await harness.Client.GetAsync(
            new Uri($"/api/dpia/assessments/{Guid.NewGuid()}", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Reliability", "CA2012:Use ValueTasks correctly",
        Justification = "NSubstitute mock setup pattern for ValueTask-returning members.")]
    [Fact]
    public async Task EndpointError_ResponseBody_DoesNotCarryTheErrorMessage()
    {
        const string secret = "secret-internal-detail-4711";
        var service = CreateService();
        service.GetAssessmentAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, DPIAReadModel>>(
                Left<EncinaError, DPIAReadModel>(EncinaErrors.Create("dpia.not_found", secret))));
        await using var harness = await CreateHarnessAsync(service);

        using var response = await harness.Client.GetAsync(
            new Uri($"/api/dpia/assessments/{Guid.NewGuid()}", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldNotContain(secret);
        body.ShouldContain("dpia.not_found");
    }

    [Fact]
    public async Task GetAssessmentById_Found_Returns200()
    {
        var id = Guid.NewGuid();
        var assessment = new DPIAReadModel { Id = id, RequestTypeName = "MyApp.OrderCommand" };
        var service = CreateService(singleAssessment: assessment);
        await using var harness = await CreateHarnessAsync(service);

        using var response = await harness.Client.GetAsync(
            new Uri($"/api/dpia/assessments/{id}", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── Endpoint execution: POST /assessments/{id}/approve ──────────────────

    [Fact]
    public async Task Approve_Success_Returns200()
    {
        var service = CreateService();
        await using var harness = await CreateHarnessAsync(service);

        using var response = await harness.Client.PostAsync(
            new Uri($"/api/dpia/assessments/{Guid.NewGuid()}/approve", UriKind.Relative),
            content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Approve_NotFound_Returns404()
    {
        var service = CreateService(
            approveResult: Left<EncinaError, Unit>(DPIAErrors.AssessmentNotFound(Guid.Empty)));
        await using var harness = await CreateHarnessAsync(service);

        using var response = await harness.Client.PostAsync(
            new Uri($"/api/dpia/assessments/{Guid.NewGuid()}/approve", UriKind.Relative),
            content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── Endpoint execution: POST /assessments/{id}/reject ───────────────────

    [Fact]
    public async Task Reject_Success_Returns200()
    {
        var service = CreateService();
        await using var harness = await CreateHarnessAsync(service);

        using var response = await harness.Client.PostAsJsonAsync(
            new Uri($"/api/dpia/assessments/{Guid.NewGuid()}/reject", UriKind.Relative),
            new RejectDPIARequest { Reason = "test" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Reject_NoBody_UsesDefaultReason_Returns200()
    {
        var service = CreateService();
        await using var harness = await CreateHarnessAsync(service);

        using var response = await harness.Client.PostAsync(
            new Uri($"/api/dpia/assessments/{Guid.NewGuid()}/reject", UriKind.Relative),
            content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Reject_NotFound_Returns404()
    {
        var service = CreateService(
            rejectResult: Left<EncinaError, Unit>(DPIAErrors.AssessmentNotFound(Guid.Empty)));
        await using var harness = await CreateHarnessAsync(service);

        using var response = await harness.Client.PostAsync(
            new Uri($"/api/dpia/assessments/{Guid.NewGuid()}/reject", UriKind.Relative),
            content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── Endpoint execution: GET /templates ──────────────────────────────────

    [Fact]
    public async Task GetTemplates_EmptyList_Returns200()
    {
        var service = CreateService();
        await using var harness = await CreateHarnessAsync(service);

        using var response = await harness.Client.GetAsync(new Uri("/api/dpia/templates", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── Endpoint execution: GET /expired ────────────────────────────────────

    [Fact]
    public async Task GetExpired_EmptyList_Returns200()
    {
        var service = CreateService();
        await using var harness = await CreateHarnessAsync(service);

        using var response = await harness.Client.GetAsync(new Uri("/api/dpia/expired", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── Endpoint execution: POST /assessments/{requestType}/assess ──────────

    [Fact]
    public async Task Assess_UnresolvableType_Returns400()
    {
        var service = CreateService();
        await using var harness = await CreateHarnessAsync(service);

        using var response = await harness.Client.PostAsync(
            new Uri("/api/dpia/assessments/Some.NonExistent.Type/assess", UriKind.Relative),
            content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Assess_TakesTenantFromRequestContext_AndIgnoresTenantHeader()
    {
        var service = CreateAssessService();
        await using var harness = await CreateHarnessAsync(service, contextTenant: "context-tenant");
        using var message = new HttpRequestMessage(HttpMethod.Post, new Uri("/api/dpia/assessments/System.String/assess", UriKind.Relative));
        message.Headers.Add("X-Tenant-Id", "header-tenant");
        message.Headers.Add("X-Tenant-ID", "other-header-tenant");

        using var response = await harness.Client.SendAsync(message);

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        await service.Received(1).CreateAssessmentAsync(
            "System.String", Arg.Any<string?>(), Arg.Any<string?>(), "context-tenant", null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Assess_NoRequestContextTenant_IgnoresTenantHeader_AndCreatesUnscopedAssessment()
    {
        var service = CreateAssessService();
        await using var harness = await CreateHarnessAsync(service);
        using var message = new HttpRequestMessage(HttpMethod.Post, new Uri("/api/dpia/assessments/System.String/assess", UriKind.Relative));
        message.Headers.Add("X-Tenant-Id", "header-tenant");

        using var response = await harness.Client.SendAsync(message);

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        await service.Received(1).CreateAssessmentAsync(
            "System.String", Arg.Any<string?>(), Arg.Any<string?>(), null, null, Arg.Any<CancellationToken>());
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Reliability", "CA2012:Use ValueTasks correctly",
        Justification = "NSubstitute mock setup pattern for ValueTask-returning members.")]
    private static IDPIAService CreateAssessService()
    {
        var service = CreateService();
        service.GetAssessmentByRequestTypeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, DPIAReadModel>>(
                Left<EncinaError, DPIAReadModel>(DPIAErrors.AssessmentNotFound(Guid.Empty))));
        service.CreateAssessmentAsync(
                Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Guid>>(
                Left<EncinaError, Guid>(DPIAErrors.AssessmentNotFound(Guid.Empty))));
        return service;
    }

    // ── Custom prefix ───────────────────────────────────────────────────────

    [Fact]
    public async Task CustomPrefix_RoutesUnderNewPrefix()
    {
        var service = CreateService();
        await using var harness = await CreateHarnessAsync(service, prefix: "/compliance/dpia");

        using var response = await harness.Client.GetAsync(new Uri("/compliance/dpia/assessments", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
