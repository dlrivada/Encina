using Encina.Security.Secrets;
using Encina.Security.Secrets.Abstractions;
using Encina.Security.Secrets.Health;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Encina.UnitTests.Security.Secrets;

public sealed class SecretsHealthCheckTests
{
    private static HealthCheckContext CreateContext(SecretsHealthCheck healthCheck) =>
        new()
        {
            Registration = new HealthCheckRegistration("test", healthCheck, null, null)
        };

    #region Basic Health Checks

    [Fact]
    public async Task CheckHealthAsync_SecretReaderRegistered_ReturnsHealthy()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();
        services.AddEncinaSecrets();
        var provider = services.BuildServiceProvider();

        var healthCheck = new SecretsHealthCheck(provider);

        var result = await healthCheck.CheckHealthAsync(CreateContext(healthCheck));

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("healthy");
    }

    [Fact]
    public async Task CheckHealthAsync_NoSecretReader_ReturnsUnhealthy()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        var healthCheck = new SecretsHealthCheck(provider);

        var result = await healthCheck.CheckHealthAsync(CreateContext(healthCheck));

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("ISecretReader");
    }

    [Fact]
    public async Task CheckHealthAsync_HealthyResult_ContainsReaderTypeData()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaSecrets(o => o.EnableCaching = false);
        var provider = services.BuildServiceProvider();

        var healthCheck = new SecretsHealthCheck(provider);

        var result = await healthCheck.CheckHealthAsync(CreateContext(healthCheck));

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data.Should().ContainKey("readerType");
    }

    [Fact]
    public void DefaultName_IsCorrect()
    {
        SecretsHealthCheck.DefaultName.Should().Be("encina-secrets");
    }

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        var act = () => new SecretsHealthCheck(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Metadata Reporting

    [Fact]
    public async Task CheckHealthAsync_CachingEnabled_ReportsCachingStatus()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();
        services.AddEncinaSecrets(o => o.EnableCaching = true);
        var provider = services.BuildServiceProvider();

        var healthCheck = new SecretsHealthCheck(provider);

        var result = await healthCheck.CheckHealthAsync(CreateContext(healthCheck));

        result.Data.Should().ContainKey("cachingEnabled");
        result.Data["cachingEnabled"].Should().Be(true);
    }

    [Fact]
    public async Task CheckHealthAsync_CachingDisabled_ReportsCachingStatusFalse()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaSecrets(o => o.EnableCaching = false);
        var provider = services.BuildServiceProvider();

        var healthCheck = new SecretsHealthCheck(provider);

        var result = await healthCheck.CheckHealthAsync(CreateContext(healthCheck));

        result.Data.Should().ContainKey("cachingEnabled");
        result.Data["cachingEnabled"].Should().Be(false);
    }

    [Fact]
    public async Task CheckHealthAsync_WithCachedDecorator_ReportsDecoratorChain()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();
        services.AddEncinaSecrets(o => o.EnableCaching = true);
        var provider = services.BuildServiceProvider();

        var healthCheck = new SecretsHealthCheck(provider);

        var result = await healthCheck.CheckHealthAsync(CreateContext(healthCheck));

        result.Data.Should().ContainKey("decorators");
        result.Data["decorators"].Should().Be("cached");
    }

    #endregion

    #region Probing

    [Fact]
    public async Task CheckHealthAsync_ProbeSecret_Success_ReturnsHealthy()
    {
        var mockReader = Substitute.For<ISecretReader>();
#pragma warning disable CA2012 // Use ValueTasks correctly - Required for NSubstitute mock setup
        mockReader.GetSecretAsync("health-probe", Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, string>>("probe-value"));
#pragma warning restore CA2012

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(mockReader);
        services.AddEncinaSecrets(o =>
        {
            o.EnableCaching = false;
            o.HealthCheckSecretName = "health-probe";
        });
        var provider = services.BuildServiceProvider();

        var healthCheck = new SecretsHealthCheck(provider);

        var result = await healthCheck.CheckHealthAsync(CreateContext(healthCheck));

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data.Should().ContainKey("probeResult");
        result.Data["probeResult"].Should().Be("success");
    }

    [Fact]
    public async Task CheckHealthAsync_ProbeSecret_Failure_ReturnsDegraded()
    {
        var mockReader = Substitute.For<ISecretReader>();
#pragma warning disable CA2012 // Use ValueTasks correctly - Required for NSubstitute mock setup
        mockReader.GetSecretAsync("health-probe", Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, string>>(
                SecretsErrors.NotFound("health-probe")));
#pragma warning restore CA2012

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(mockReader);
        services.AddEncinaSecrets(o =>
        {
            o.EnableCaching = false;
            o.HealthCheckSecretName = "health-probe";
        });
        var provider = services.BuildServiceProvider();

        var healthCheck = new SecretsHealthCheck(provider);

        var result = await healthCheck.CheckHealthAsync(CreateContext(healthCheck));

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Data.Should().ContainKey("probeResult");
        result.Data["probeResult"].Should().Be("failed");
        result.Data.Should().ContainKey("probeError");
    }

    [Fact]
    public async Task CheckHealthAsync_NoProbeConfigured_ReturnsHealthyWithoutProbe()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaSecrets(o =>
        {
            o.EnableCaching = false;
            o.HealthCheckSecretName = null;
        });
        var provider = services.BuildServiceProvider();

        var healthCheck = new SecretsHealthCheck(provider);

        var result = await healthCheck.CheckHealthAsync(CreateContext(healthCheck));

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data.Should().NotContainKey("probeResult");
    }

    #endregion

    #region Exception Handling

    [Fact]
    public async Task CheckHealthAsync_Exception_ReturnsUnhealthy()
    {
        var mockReader = Substitute.For<ISecretReader>();
#pragma warning disable CA2012 // Use ValueTasks correctly - Required for NSubstitute mock setup
        mockReader.When(r => r.GetSecretAsync("health-probe", Arg.Any<CancellationToken>()))
            .Do(_ => throw new InvalidOperationException("Provider crashed"));
#pragma warning restore CA2012

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(mockReader);
        services.AddEncinaSecrets(o =>
        {
            o.EnableCaching = false;
            o.HealthCheckSecretName = "health-probe";
        });
        var provider = services.BuildServiceProvider();

        var healthCheck = new SecretsHealthCheck(provider);

        var result = await healthCheck.CheckHealthAsync(CreateContext(healthCheck));

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Exception.Should().BeOfType<InvalidOperationException>();
    }

    #endregion
}
