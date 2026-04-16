using Encina.Caching;
using Encina.Security.Secrets;
using Encina.Security.Secrets.Abstractions;
using Encina.Security.Secrets.Health;
using Shouldly;
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
        services.AddSingleton(NSubstitute.Substitute.For<ICacheProvider>());
        services.AddEncinaSecrets();
        var provider = services.BuildServiceProvider();

        var healthCheck = new SecretsHealthCheck(provider);

        var result = await healthCheck.CheckHealthAsync(CreateContext(healthCheck));

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description!.ShouldContain("healthy");
    }

    [Fact]
    public async Task CheckHealthAsync_NoSecretReader_ReturnsUnhealthy()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        var healthCheck = new SecretsHealthCheck(provider);

        var result = await healthCheck.CheckHealthAsync(CreateContext(healthCheck));

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("ISecretReader");
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

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Data.ShouldContainKey("readerType");
    }

    [Fact]
    public void DefaultName_IsCorrect()
    {
        SecretsHealthCheck.DefaultName.ShouldBe("encina-secrets");
    }

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        var act = () => new SecretsHealthCheck(null!);

        Should.Throw<ArgumentNullException>(act);
    }

    #endregion

    #region Metadata Reporting

    [Fact]
    public async Task CheckHealthAsync_CachingEnabled_ReportsCachingStatus()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(NSubstitute.Substitute.For<ICacheProvider>());
        services.AddEncinaSecrets(o => o.EnableCaching = true);
        var provider = services.BuildServiceProvider();

        var healthCheck = new SecretsHealthCheck(provider);

        var result = await healthCheck.CheckHealthAsync(CreateContext(healthCheck));

        result.Data.ShouldContainKey("cachingEnabled");
        result.Data["cachingEnabled"].ShouldBe(true);
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

        result.Data.ShouldContainKey("cachingEnabled");
        result.Data["cachingEnabled"].ShouldBe(false);
    }

    [Fact]
    public async Task CheckHealthAsync_WithCachedDecorator_ReportsDecoratorChain()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(NSubstitute.Substitute.For<ICacheProvider>());
        services.AddEncinaSecrets(o => o.EnableCaching = true);
        var provider = services.BuildServiceProvider();

        var healthCheck = new SecretsHealthCheck(provider);

        var result = await healthCheck.CheckHealthAsync(CreateContext(healthCheck));

        result.Data.ShouldContainKey("decorators");
        result.Data["decorators"].ShouldBe("cached");
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

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Data.ShouldContainKey("probeResult");
        result.Data["probeResult"].ShouldBe("success");
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

        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Data.ShouldContainKey("probeResult");
        result.Data["probeResult"].ShouldBe("failed");
        result.Data.ShouldContainKey("probeError");
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

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Data.ShouldNotContainKey("probeResult");
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

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Exception.ShouldBeOfType<InvalidOperationException>();
    }

    #endregion
}
