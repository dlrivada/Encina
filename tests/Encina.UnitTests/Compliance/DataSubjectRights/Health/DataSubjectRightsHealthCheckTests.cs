using Encina.Compliance.DataSubjectRights;
using Encina.Compliance.DataSubjectRights.Abstractions;
using Encina.Compliance.DataSubjectRights.Health;
using Encina.Compliance.DataSubjectRights.Projections;

using Shouldly;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.DataSubjectRights.Health;

/// <summary>
/// Unit tests for <see cref="DataSubjectRightsHealthCheck"/>.
/// </summary>
public class DataSubjectRightsHealthCheckTests
{
    private readonly IServiceProvider _rootProvider = Substitute.For<IServiceProvider>();
    private readonly IServiceScope _scope = Substitute.For<IServiceScope>();
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();
    private readonly IServiceProvider _scopedProvider = Substitute.For<IServiceProvider>();
    private readonly IDSRService _dsrService = Substitute.For<IDSRService>();

    private readonly DataSubjectRightsHealthCheck _sut;

    public DataSubjectRightsHealthCheckTests()
    {
        _scope.ServiceProvider.Returns(_scopedProvider);
        _scopeFactory.CreateScope().Returns(_scope);
        _rootProvider.GetService(typeof(IServiceScopeFactory)).Returns(_scopeFactory);

        _sut = new DataSubjectRightsHealthCheck(
            _rootProvider,
            NullLogger<DataSubjectRightsHealthCheck>.Instance);
    }

    private void SetupOptions(DataSubjectRightsOptions? options = null)
    {
        if (options is not null)
        {
            var wrappedOptions = Options.Create(options);
            _scopedProvider.GetService(typeof(IOptions<DataSubjectRightsOptions>)).Returns(wrappedOptions);
        }
        else
        {
            _scopedProvider.GetService(typeof(IOptions<DataSubjectRightsOptions>)).Returns(null);
        }
    }

    private void SetupDsrService(bool register = true)
    {
        _scopedProvider.GetService(typeof(IDSRService)).Returns(register ? _dsrService : null);
    }

    private void SetupOptionalServices(bool locator = true, bool erasureExecutor = true)
    {
        _scopedProvider.GetService(typeof(IPersonalDataLocator))
            .Returns(locator ? Substitute.For<IPersonalDataLocator>() : null);
        _scopedProvider.GetService(typeof(IDataErasureExecutor))
            .Returns(erasureExecutor ? Substitute.For<IDataErasureExecutor>() : null);
    }

    private void SetupOverdueRequests(int count)
    {
        var overdueList = new List<DSRRequestReadModel>();
        for (var i = 0; i < count; i++)
        {
            overdueList.Add(new DSRRequestReadModel { Id = Guid.NewGuid() });
        }

        _dsrService.GetOverdueRequestsAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<DSRRequestReadModel>>(overdueList));
    }

    #region DefaultName and Tags

    [Fact]
    public void DefaultName_ShouldBeExpected()
    {
        DataSubjectRightsHealthCheck.DefaultName.ShouldBe("encina-dsr");
    }

    [Fact]
    public void Tags_ShouldContainExpectedValues()
    {
        DataSubjectRightsHealthCheck.Tags.ShouldContain("encina");
        DataSubjectRightsHealthCheck.Tags.ShouldContain("gdpr");
        DataSubjectRightsHealthCheck.Tags.ShouldContain("dsr");
        DataSubjectRightsHealthCheck.Tags.ShouldContain("compliance");
        DataSubjectRightsHealthCheck.Tags.ShouldContain("ready");
    }

    #endregion

    #region Unhealthy scenarios

    [Fact]
    public async Task CheckHealthAsync_OptionsNotConfigured_ReturnsUnhealthy()
    {
        SetupOptions(null);

        var result = await _sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("not configured");
    }

    [Fact]
    public async Task CheckHealthAsync_DsrServiceNotRegistered_ReturnsUnhealthy()
    {
        SetupOptions(new DataSubjectRightsOptions());
        SetupDsrService(false);

        var result = await _sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("IDSRService");
    }

    #endregion

    #region Degraded scenarios

    [Fact]
    public async Task CheckHealthAsync_OverdueRequests_ReturnsDegraded()
    {
        SetupOptions(new DataSubjectRightsOptions());
        SetupDsrService();
        SetupOptionalServices();
        SetupOverdueRequests(3);

        var result = await _sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description!.ShouldContain("3 overdue DSR request(s)");
    }

    [Fact]
    public async Task CheckHealthAsync_OverdueCheckError_ReturnsDegraded()
    {
        SetupOptions(new DataSubjectRightsOptions());
        SetupDsrService();
        SetupOptionalServices();
        _dsrService.GetOverdueRequestsAsync(Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, IReadOnlyList<DSRRequestReadModel>>(
                EncinaErrors.Create("store.error", "DB unavailable")));

        var result = await _sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description!.ShouldContain("Failed to check");
    }

    [Fact]
    public async Task CheckHealthAsync_MissingLocator_ReturnsDegraded()
    {
        SetupOptions(new DataSubjectRightsOptions());
        SetupDsrService();
        SetupOptionalServices(locator: false);
        SetupOverdueRequests(0);

        var result = await _sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description!.ShouldContain("IPersonalDataLocator");
    }

    [Fact]
    public async Task CheckHealthAsync_MissingErasureExecutor_ReturnsDegraded()
    {
        SetupOptions(new DataSubjectRightsOptions());
        SetupDsrService();
        SetupOptionalServices(erasureExecutor: false);
        SetupOverdueRequests(0);

        var result = await _sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description!.ShouldContain("IDataErasureExecutor");
    }

    [Fact]
    public async Task CheckHealthAsync_MultipleWarnings_AllIncludedInDegraded()
    {
        SetupOptions(new DataSubjectRightsOptions());
        SetupDsrService();
        SetupOptionalServices(locator: false, erasureExecutor: false);
        SetupOverdueRequests(2);

        var result = await _sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description!.ShouldContain("IPersonalDataLocator");
        result.Description!.ShouldContain("IDataErasureExecutor");
        result.Description!.ShouldContain("2 overdue DSR request(s)");
    }

    #endregion

    #region Healthy scenario

    [Fact]
    public async Task CheckHealthAsync_AllConfigured_ReturnsHealthy()
    {
        SetupOptions(new DataSubjectRightsOptions());
        SetupDsrService();
        SetupOptionalServices();
        SetupOverdueRequests(0);

        var result = await _sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description!.ShouldContain("fully configured");
        result.Data.ShouldContainKey("enforcementMode");
        result.Data.ShouldContainKey("dsrServiceType");
    }

    #endregion
}
