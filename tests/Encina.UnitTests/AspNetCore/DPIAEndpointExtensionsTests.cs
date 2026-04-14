using System.Net;
using System.Net.Http.Json;
using Encina.AspNetCore;
using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Abstractions;
using Encina.Compliance.DPIA.Model;
using Encina.Compliance.DPIA.ReadModels;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

#pragma warning disable CA2012 // Use ValueTasks correctly - Required for NSubstitute mocking

namespace Encina.UnitTests.AspNetCore;

/// <summary>
/// Unit tests for <see cref="DPIAEndpointExtensions"/>.
/// </summary>
public sealed class DPIAEndpointExtensionsTests
{
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

    private static IDPIATemplateProvider CreateTemplateProvider(
        IReadOnlyList<DPIATemplate>? templates = null)
    {
        var provider = Substitute.For<IDPIATemplateProvider>();
        provider.GetAllTemplatesAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, IReadOnlyList<DPIATemplate>>>(
                Right<EncinaError, IReadOnlyList<DPIATemplate>>(templates ?? [])));
        return provider;
    }

    private static async Task<HttpClient> CreateClientAsync(
        IDPIAService service,
        IDPIATemplateProvider? templateProvider = null,
        string prefix = "/api/dpia")
    {
        var builder = new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(services =>
                {
                    services.AddLogging();
                    services.AddRouting();
                    services.AddSingleton(service);
                    services.AddSingleton(templateProvider ?? CreateTemplateProvider());
                    services.AddSingleton(Substitute.For<IDPIAAssessmentEngine>());
                    services.AddSingleton(TimeProvider.System);
                    services.Configure<DPIAOptions>(_ => { });
                });
                web.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapDPIAEndpoints(prefix);
                    });
                });
            });

        var host = await builder.StartAsync();
        return host.GetTestClient();
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
    public void TenantIdHeader_Constant_Value()
    {
        DPIAEndpointExtensions.TenantIdHeader.ShouldBe("X-Tenant-Id");
    }

    [Fact]
    public void ModuleIdHeader_Constant_Value()
    {
        DPIAEndpointExtensions.ModuleIdHeader.ShouldBe("X-Module-Id");
    }

    // ── Registration ────────────────────────────────────────────────────────

    [Fact]
    public void MapDPIAEndpoints_DefaultPrefix_ReturnsRouteGroupBuilder()
    {
        var endpoints = Substitute.For<IEndpointRouteBuilder>();
        endpoints.ServiceProvider.Returns(new ServiceCollection().BuildServiceProvider());
        var dataSources = new List<EndpointDataSource>();
        endpoints.DataSources.Returns(dataSources);

        var group = endpoints.MapDPIAEndpoints();

        group.ShouldNotBeNull();
    }

    [Fact]
    public void MapDPIAEndpoints_CustomPrefix_ReturnsRouteGroupBuilder()
    {
        var endpoints = Substitute.For<IEndpointRouteBuilder>();
        endpoints.ServiceProvider.Returns(new ServiceCollection().BuildServiceProvider());
        var dataSources = new List<EndpointDataSource>();
        endpoints.DataSources.Returns(dataSources);

        var group = endpoints.MapDPIAEndpoints("/compliance/dpia");

        group.ShouldNotBeNull();
    }

    // ── Endpoint execution: GET /assessments ────────────────────────────────

    [Fact]
    public async Task GetAssessments_EmptyList_ReturnsOk()
    {
        var service = CreateService();
        using var client = await CreateClientAsync(service);

        using var response = await client.GetAsync(new Uri("/api/dpia/assessments", UriKind.Relative));

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
        using var client = await CreateClientAsync(service);

        using var response = await client.GetAsync(new Uri("/api/dpia/assessments", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── Endpoint execution: GET /assessments/{id} ───────────────────────────

    [Fact]
    public async Task GetAssessmentById_NotFound_Returns404()
    {
        var service = CreateService(); // singleAssessment = null → returns NotFound error
        using var client = await CreateClientAsync(service);

        using var response = await client.GetAsync(
            new Uri($"/api/dpia/assessments/{Guid.NewGuid()}", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAssessmentById_Found_Returns200()
    {
        var id = Guid.NewGuid();
        var assessment = new DPIAReadModel { Id = id, RequestTypeName = "MyApp.OrderCommand" };
        var service = CreateService(singleAssessment: assessment);
        using var client = await CreateClientAsync(service);

        using var response = await client.GetAsync(
            new Uri($"/api/dpia/assessments/{id}", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── Endpoint execution: POST /assessments/{id}/approve ──────────────────

    [Fact]
    public async Task Approve_Success_Returns200()
    {
        var service = CreateService();
        using var client = await CreateClientAsync(service);

        using var response = await client.PostAsync(
            new Uri($"/api/dpia/assessments/{Guid.NewGuid()}/approve", UriKind.Relative),
            content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Approve_NotFound_Returns404()
    {
        var service = CreateService(
            approveResult: Left<EncinaError, Unit>(DPIAErrors.AssessmentNotFound(Guid.Empty)));
        using var client = await CreateClientAsync(service);

        using var response = await client.PostAsync(
            new Uri($"/api/dpia/assessments/{Guid.NewGuid()}/approve", UriKind.Relative),
            content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── Endpoint execution: POST /assessments/{id}/reject ───────────────────

    [Fact]
    public async Task Reject_Success_Returns200()
    {
        var service = CreateService();
        using var client = await CreateClientAsync(service);

        using var response = await client.PostAsJsonAsync(
            new Uri($"/api/dpia/assessments/{Guid.NewGuid()}/reject", UriKind.Relative),
            new RejectDPIARequest { Reason = "test" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Reject_NoBody_UsesDefaultReason_Returns200()
    {
        var service = CreateService();
        using var client = await CreateClientAsync(service);

        using var response = await client.PostAsync(
            new Uri($"/api/dpia/assessments/{Guid.NewGuid()}/reject", UriKind.Relative),
            content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Reject_NotFound_Returns404()
    {
        var service = CreateService(
            rejectResult: Left<EncinaError, Unit>(DPIAErrors.AssessmentNotFound(Guid.Empty)));
        using var client = await CreateClientAsync(service);

        using var response = await client.PostAsync(
            new Uri($"/api/dpia/assessments/{Guid.NewGuid()}/reject", UriKind.Relative),
            content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── Endpoint execution: GET /templates ──────────────────────────────────

    [Fact]
    public async Task GetTemplates_EmptyList_Returns200()
    {
        var service = CreateService();
        using var client = await CreateClientAsync(service);

        using var response = await client.GetAsync(new Uri("/api/dpia/templates", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── Endpoint execution: GET /expired ────────────────────────────────────

    [Fact]
    public async Task GetExpired_EmptyList_Returns200()
    {
        var service = CreateService();
        using var client = await CreateClientAsync(service);

        using var response = await client.GetAsync(new Uri("/api/dpia/expired", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── Endpoint execution: POST /assessments/{requestType}/assess ──────────

    [Fact]
    public async Task Assess_UnresolvableType_Returns400()
    {
        var service = CreateService();
        using var client = await CreateClientAsync(service);

        using var response = await client.PostAsync(
            new Uri("/api/dpia/assessments/Some.NonExistent.Type/assess", UriKind.Relative),
            content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // ── Custom prefix ───────────────────────────────────────────────────────

    [Fact]
    public async Task CustomPrefix_RoutesUnderNewPrefix()
    {
        var service = CreateService();
        using var client = await CreateClientAsync(service, prefix: "/compliance/dpia");

        using var response = await client.GetAsync(new Uri("/compliance/dpia/assessments", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
