#pragma warning disable CA2012 // ValueTask instances used in NSubstitute mock setup

using Encina.Security.Secrets;
using Encina.Security.Secrets.Abstractions;
using Encina.Security.Secrets.Caching;
using Encina.Security.Secrets.Diagnostics;
using Encina.Security.Secrets.Health;
using Encina.Security.Secrets.Injection;
using Encina.Security.Secrets.Providers;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Encina.IntegrationTests.Security.Secrets;

/// <summary>
/// Integration tests for the full Encina.Security.Secrets pipeline.
/// Tests DI registration, configuration bridge, failover, health check,
/// and secret injection end-to-end flows.
/// No Docker containers needed — secrets management is pure in-process.
/// </summary>
[Trait("Category", "Integration")]
public sealed class SecretsIntegrationTests : IDisposable
{
    public SecretsIntegrationTests()
    {
        SecretPropertyCache.ClearCache();
    }

    public void Dispose()
    {
        SecretPropertyCache.ClearCache();
    }

    #region DI Registration

    [Fact]
    public void AddEncinaSecrets_RegistersAllCoreServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();

        services.AddEncinaSecrets();
        var provider = services.BuildServiceProvider();

        provider.GetService<ISecretReader>().Should().NotBeNull();
        provider.GetService<IOptions<SecretsOptions>>().Should().NotBeNull();
    }

    [Fact]
    public void AddEncinaSecrets_WithCaching_ResolvesCachedDecorator()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();

        services.AddEncinaSecrets(o => o.EnableCaching = true);
        var provider = services.BuildServiceProvider();

        var reader = provider.GetRequiredService<ISecretReader>();
        reader.Should().BeOfType<CachedSecretReaderDecorator>();
    }

    [Fact]
    public void AddEncinaSecrets_WithMetrics_ResolvesSecretsMetrics()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();

        services.AddEncinaSecrets(o => o.EnableMetrics = true);
        var provider = services.BuildServiceProvider();

        var metrics = provider.GetService<SecretsMetrics>();
        metrics.Should().NotBeNull();
    }

    [Fact]
    public void AddEncinaSecrets_WithInjection_RegistersBehavior()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaSecrets(o => o.EnableSecretInjection = true);

        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IPipelineBehavior<,>));
        descriptor.Should().NotBeNull();
        descriptor!.ImplementationType.Should().Be(typeof(SecretInjectionPipelineBehavior<,>));
    }

    #endregion

    #region Configuration Bridge

    [Fact]
    public void ConfigurationBridge_SecretsAppearInConfiguration()
    {
        var mockReader = Substitute.For<ISecretReader>();
        mockReader.GetSecretAsync("api-key", Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, string>>("my-api-key-123"));
        mockReader.GetSecretAsync("db-connection", Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, string>>("Server=prod;Database=app"));

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(mockReader);
        services.AddEncinaSecrets(o => o.EnableCaching = false);
        var sp = services.BuildServiceProvider();

        var config = new ConfigurationBuilder()
            .AddEncinaSecrets(sp, o =>
            {
                o.SecretNames = ["api-key", "db-connection"];
            })
            .Build();

        config["api-key"].Should().Be("my-api-key-123");
        config["db-connection"].Should().Be("Server=prod;Database=app");
    }

    [Fact]
    public void ConfigurationBridge_HierarchicalKeys_MappedCorrectly()
    {
        var mockReader = Substitute.For<ISecretReader>();
        mockReader.GetSecretAsync("Database--ConnectionString", Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, string>>("Server=localhost"));

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(mockReader);
        services.AddEncinaSecrets(o => o.EnableCaching = false);
        var sp = services.BuildServiceProvider();

        var config = new ConfigurationBuilder()
            .AddEncinaSecrets(sp, o =>
            {
                o.SecretNames = ["Database--ConnectionString"];
                o.KeyDelimiter = "--";
            })
            .Build();

        config.GetSection("Database")["ConnectionString"].Should().Be("Server=localhost");
    }

    [Fact]
    public void ConfigurationBridge_WithOptions_BindsToStronglyTypedObject()
    {
        var mockReader = Substitute.For<ISecretReader>();
        mockReader.GetSecretAsync("Database--Host", Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, string>>("prod-server"));
        mockReader.GetSecretAsync("Database--Port", Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, string>>("5432"));

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(mockReader);
        services.AddEncinaSecrets(o => o.EnableCaching = false);
        var sp = services.BuildServiceProvider();

        var config = new ConfigurationBuilder()
            .AddEncinaSecrets(sp, o =>
            {
                o.SecretNames = ["Database--Host", "Database--Port"];
                o.KeyDelimiter = "--";
            })
            .Build();

        var dbSettings = new DatabaseSettings();
        config.GetSection("Database").Bind(dbSettings);

        dbSettings.Host.Should().Be("prod-server");
        dbSettings.Port.Should().Be("5432");
    }

    #endregion

    #region Failover

    [Fact]
    public async Task Failover_PrimaryFails_FallbackSucceeds()
    {
        var primary = Substitute.For<ISecretReader>();
        primary.GetSecretAsync("my-secret", Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, string>>(
                SecretsErrors.ProviderUnavailable("primary")));

        var secondary = Substitute.For<ISecretReader>();
        secondary.GetSecretAsync("my-secret", Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, string>>("fallback-value"));

        var logger = NullLoggerFactory.Instance.CreateLogger<FailoverSecretReader>();
        var failoverReader = new FailoverSecretReader(
            [primary, secondary],
            logger);

        var result = await failoverReader.GetSecretAsync("my-secret");

        result.IsRight.Should().BeTrue();
        result.IfRight(v => v.Should().Be("fallback-value"));
    }

    [Fact]
    public async Task Failover_AllFail_ReturnsExhaustedError()
    {
        var primary = Substitute.For<ISecretReader>();
        primary.GetSecretAsync("my-secret", Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, string>>(
                SecretsErrors.ProviderUnavailable("primary")));

        var secondary = Substitute.For<ISecretReader>();
        secondary.GetSecretAsync("my-secret", Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, string>>(
                SecretsErrors.ProviderUnavailable("secondary")));

        var logger = NullLoggerFactory.Instance.CreateLogger<FailoverSecretReader>();
        var failoverReader = new FailoverSecretReader(
            [primary, secondary],
            logger);

        var result = await failoverReader.GetSecretAsync("my-secret");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.FailoverExhaustedCode));
    }

    #endregion

    #region Health Check

    [Fact]
    public async Task HealthCheck_WithRegisteredReader_ReportsHealthy()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();
        services.AddEncinaSecrets(o => o.ProviderHealthCheck = true);

        var provider = services.BuildServiceProvider();
        var healthCheckService = provider.GetRequiredService<HealthCheckService>();

        var report = await healthCheckService.CheckHealthAsync();

        report.Status.Should().Be(HealthStatus.Healthy);
        report.Entries.Should().ContainKey(SecretsHealthCheck.DefaultName);
    }

    [Fact]
    public async Task HealthCheck_WithProbe_Success_ReportsProbeResult()
    {
        var mockReader = Substitute.For<ISecretReader>();
        mockReader.GetSecretAsync("health-probe", Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, string>>("probe-ok"));

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(mockReader);
        services.AddEncinaSecrets(o =>
        {
            o.EnableCaching = false;
            o.ProviderHealthCheck = true;
            o.HealthCheckSecretName = "health-probe";
        });

        var provider = services.BuildServiceProvider();
        var healthCheck = new SecretsHealthCheck(provider);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("test", healthCheck, null, null)
        };

        var result = await healthCheck.CheckHealthAsync(context);

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data.Should().ContainKey("probeResult");
        result.Data["probeResult"].Should().Be("success");
    }

    #endregion

    #region Secret Injection End-to-End

    [Fact]
    public async Task SecretInjection_InjectsSecretsIntoRequest()
    {
        var mockReader = Substitute.For<ISecretReader>();
        mockReader.GetSecretAsync("db-password", Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, string>>("super-secret"));

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(mockReader);
        services.AddEncinaSecrets(o =>
        {
            o.EnableCaching = false;
            o.EnableSecretInjection = true;
        });
        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<SecretInjectionOrchestrator>();

        var request = new TestInjectionRequest();
        var result = await orchestrator.InjectAsync(request, CancellationToken.None);

        result.IsRight.Should().BeTrue();
        result.IfRight(count => count.Should().Be(1));
        request.DatabasePassword.Should().Be("super-secret");
    }

    [Fact]
    public async Task SecretInjection_FailOnErrorFalse_ContinuesOnMissing()
    {
        var mockReader = Substitute.For<ISecretReader>();
        mockReader.GetSecretAsync("optional-key", Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, string>>(
                SecretsErrors.NotFound("optional-key")));

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(mockReader);
        services.AddEncinaSecrets(o =>
        {
            o.EnableCaching = false;
            o.EnableSecretInjection = true;
        });
        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<SecretInjectionOrchestrator>();

        var request = new TestOptionalInjectionRequest();
        var result = await orchestrator.InjectAsync(request, CancellationToken.None);

        result.IsRight.Should().BeTrue();
        request.OptionalSecret.Should().BeNull(); // Not injected
    }

    #endregion

    #region Caching Integration

    [Fact]
    public async Task Caching_CachesResultsAndInvalidatesCorrectly()
    {
        // Build the decorator manually because TryAddSingleton<ISecretReader> in
        // AddEncinaSecrets is a no-op when a mock is pre-registered,
        // which skips the WrapWithDecorators caching layer.
        var callCount = 0;
        var mockReader = Substitute.For<ISecretReader>();
#pragma warning disable CA2012 // ValueTask instances used in NSubstitute mock setup
        mockReader.GetSecretAsync("cached-secret", Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                return new ValueTask<Either<EncinaError, string>>($"value-{callCount}");
            });
#pragma warning restore CA2012

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();
        var provider = services.BuildServiceProvider();

        var cache = provider.GetRequiredService<IMemoryCache>();
        var options = Options.Create(new SecretsOptions
        {
            EnableCaching = true,
            DefaultCacheDuration = TimeSpan.FromMinutes(5)
        });
        var logger = NullLoggerFactory.Instance.CreateLogger<CachedSecretReaderDecorator>();

        var reader = new CachedSecretReaderDecorator(mockReader, cache, options, logger);

        // First call - cache miss
        var result1 = await reader.GetSecretAsync("cached-secret");
        result1.IfRight(v => v.Should().Be("value-1"));

        // Second call - cache hit (same value)
        var result2 = await reader.GetSecretAsync("cached-secret");
        result2.IfRight(v => v.Should().Be("value-1"));

        // Invalidate cache
        reader.Invalidate("cached-secret");

        // Third call - cache miss again (new value)
        var result3 = await reader.GetSecretAsync("cached-secret");
        result3.IfRight(v => v.Should().Be("value-2"));
    }

    #endregion

    #region Environment Provider Integration

    [Fact]
    public async Task EnvironmentProvider_FullDI_RetrievesEnvVar()
    {
        const string envVarName = "ENCINA_INTEG_TEST_SECRET";
        Environment.SetEnvironmentVariable(envVarName, "integration-test-value");

        try
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddEncinaSecrets(o => o.EnableCaching = false);
            var provider = services.BuildServiceProvider();

            var reader = provider.GetRequiredService<ISecretReader>();
            var result = await reader.GetSecretAsync(envVarName);

            result.IsRight.Should().BeTrue();
            result.IfRight(v => v.Should().Be("integration-test-value"));
        }
        finally
        {
            Environment.SetEnvironmentVariable(envVarName, null);
        }
    }

    #endregion

    #region Configuration Provider Integration

    [Fact]
    public async Task ConfigurationProvider_FullDI_RetrievesFromConfiguration()
    {
        var configData = new Dictionary<string, string?>
        {
            ["Secrets:api-key"] = "config-api-key",
            ["Secrets:db-password"] = "config-db-password"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddEncinaSecrets<ConfigurationSecretProvider>(o => o.EnableCaching = false);
        var provider = services.BuildServiceProvider();

        var reader = provider.GetRequiredService<ISecretReader>();

        var result = await reader.GetSecretAsync("api-key");
        result.IsRight.Should().BeTrue();
        result.IfRight(v => v.Should().Be("config-api-key"));
    }

    #endregion

    #region Test Types

    private sealed class TestInjectionRequest
    {
        [InjectSecret("db-password")]
        public string? DatabasePassword { get; set; }
    }

    private sealed class TestOptionalInjectionRequest
    {
        [InjectSecret("optional-key", FailOnError = false)]
        public string? OptionalSecret { get; set; }
    }

    private sealed class DatabaseSettings
    {
        public string Host { get; set; } = "";
        public string Port { get; set; } = "";
    }

    #endregion
}
