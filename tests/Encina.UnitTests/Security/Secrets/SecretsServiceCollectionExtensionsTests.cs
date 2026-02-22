using Encina.Security.Secrets;
using Encina.Security.Secrets.Abstractions;
using Encina.Security.Secrets.Caching;
using Encina.Security.Secrets.Diagnostics;
using Encina.Security.Secrets.Health;
using Encina.Security.Secrets.Injection;
using Encina.Security.Secrets.Providers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.UnitTests.Security.Secrets;

public sealed class SecretsServiceCollectionExtensionsTests
{
    #region AddEncinaSecrets (default)

    [Fact]
    public void AddEncinaSecrets_RegistersISecretReader()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();

        services.AddEncinaSecrets();

        var provider = services.BuildServiceProvider();
        provider.GetService<ISecretReader>().Should().NotBeNull();
    }

    [Fact]
    public void AddEncinaSecrets_DefaultReader_IsCachedDecorator_WhenCachingEnabled()
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
    public void AddEncinaSecrets_DefaultReader_IsEnvironmentProvider_WhenCachingDisabled()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaSecrets(o => o.EnableCaching = false);

        var provider = services.BuildServiceProvider();
        var reader = provider.GetRequiredService<ISecretReader>();
        reader.Should().BeOfType<EnvironmentSecretProvider>();
    }

    [Fact]
    public void AddEncinaSecrets_ConfigureOptions_AppliesConfiguration()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaSecrets(options =>
        {
            options.EnableCaching = false;
            options.DefaultCacheDuration = TimeSpan.FromMinutes(30);
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<SecretsOptions>>().Value;
        options.EnableCaching.Should().BeFalse();
        options.DefaultCacheDuration.Should().Be(TimeSpan.FromMinutes(30));
    }

    [Fact]
    public void AddEncinaSecrets_WithoutConfigure_UsesDefaults()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();

        services.AddEncinaSecrets();

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<SecretsOptions>>().Value;
        options.EnableCaching.Should().BeTrue();
        options.DefaultCacheDuration.Should().Be(TimeSpan.FromMinutes(5));
        options.ProviderHealthCheck.Should().BeFalse();
    }

    [Fact]
    public void AddEncinaSecrets_TryAdd_AllowsOverride()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        // Register custom reader BEFORE AddEncinaSecrets
        services.AddSingleton<ISecretReader>(sp =>
            new ConfigurationSecretProvider(
                new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build(),
                sp.GetRequiredService<ILogger<ConfigurationSecretProvider>>()));
        services.AddEncinaSecrets();

        var provider = services.BuildServiceProvider();
        var resolved = provider.GetRequiredService<ISecretReader>();
        resolved.Should().BeOfType<ConfigurationSecretProvider>();
    }

    [Fact]
    public void AddEncinaSecrets_ReturnsServiceCollection_ForChaining()
    {
        var services = new ServiceCollection();

        var result = services.AddEncinaSecrets();

        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddEncinaSecrets_CalledMultipleTimes_DoesNotThrow()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var act = () =>
        {
            services.AddEncinaSecrets();
            services.AddEncinaSecrets();
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void AddEncinaSecrets_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddEncinaSecrets();

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    #endregion

    #region AddEncinaSecrets<TReader>

    [Fact]
    public void AddEncinaSecrets_Generic_RegistersSpecificReader()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<Microsoft.Extensions.Configuration.IConfiguration>(
            new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());

        services.AddEncinaSecrets<ConfigurationSecretProvider>(o => o.EnableCaching = false);

        var provider = services.BuildServiceProvider();
        var reader = provider.GetRequiredService<ISecretReader>();
        reader.Should().BeOfType<ConfigurationSecretProvider>();
    }

    [Fact]
    public void AddEncinaSecrets_Generic_WithCaching_WrappsInDecorator()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();
        services.AddSingleton<Microsoft.Extensions.Configuration.IConfiguration>(
            new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());

        services.AddEncinaSecrets<ConfigurationSecretProvider>(o => o.EnableCaching = true);

        var provider = services.BuildServiceProvider();
        var reader = provider.GetRequiredService<ISecretReader>();
        reader.Should().BeOfType<CachedSecretReaderDecorator>();
    }

    [Fact]
    public void AddEncinaSecrets_Generic_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddEncinaSecrets<EnvironmentSecretProvider>();

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    #endregion

    #region AddSecretRotationHandler

    [Fact]
    public void AddSecretRotationHandler_RegistersHandler()
    {
        var services = new ServiceCollection();

        services.AddSecretRotationHandler<TestRotationHandler>();

        var descriptor = services.First(d => d.ServiceType == typeof(ISecretRotationHandler));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Transient);
        descriptor.ImplementationType.Should().Be(typeof(TestRotationHandler));
    }

    [Fact]
    public void AddSecretRotationHandler_ReturnsServiceCollection_ForChaining()
    {
        var services = new ServiceCollection();

        var result = services.AddSecretRotationHandler<TestRotationHandler>();

        result.Should().BeSameAs(services);
    }

    #endregion

    #region Health Check Registration

    [Fact]
    public void AddEncinaSecrets_HealthCheckEnabled_RegistersHealthCheck()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaSecrets(options => options.ProviderHealthCheck = true);

        var provider = services.BuildServiceProvider();
        var healthCheckOptions = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        healthCheckOptions.Registrations.Should().Contain(r => r.Name == SecretsHealthCheck.DefaultName);
    }

    [Fact]
    public void AddEncinaSecrets_HealthCheckDisabled_DoesNotRegisterHealthCheck()
    {
        var services = new ServiceCollection();

        services.AddEncinaSecrets(options => options.ProviderHealthCheck = false);

        var hasHealthChecks = services.Any(d => d.ServiceType == typeof(HealthCheckService));
        hasHealthChecks.Should().BeFalse();
    }

    [Fact]
    public void AddEncinaSecrets_DefaultOptions_DoesNotRegisterHealthCheck()
    {
        var services = new ServiceCollection();

        services.AddEncinaSecrets();

        var hasHealthChecks = services.Any(d => d.ServiceType == typeof(HealthCheckService));
        hasHealthChecks.Should().BeFalse();
    }

    #endregion

    #region Secret Injection Registration

    [Fact]
    public void AddEncinaSecrets_EnableSecretInjection_True_RegistersPipelineBehavior()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaSecrets(options => options.EnableSecretInjection = true);

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IPipelineBehavior<,>));
        descriptor.Should().NotBeNull();
        descriptor!.ImplementationType.Should().Be(typeof(SecretInjectionPipelineBehavior<,>));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Transient);
    }

    [Fact]
    public void AddEncinaSecrets_EnableSecretInjection_False_DoesNotRegisterPipelineBehavior()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaSecrets(options => options.EnableSecretInjection = false);

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IPipelineBehavior<,>));
        descriptor.Should().BeNull();
    }

    [Fact]
    public void AddEncinaSecrets_EnableSecretInjection_True_RegistersOrchestrator()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaSecrets(options => options.EnableSecretInjection = true);

        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(SecretInjectionOrchestrator));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddEncinaSecrets_Default_DoesNotRegisterSecretInjection()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaSecrets();

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IPipelineBehavior<,>));
        descriptor.Should().BeNull();
    }

    #endregion

    #region Metrics Registration

    [Fact]
    public void AddEncinaSecrets_EnableMetrics_True_RegistersSecretsMetrics()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaSecrets(options => options.EnableMetrics = true);

        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(SecretsMetrics));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaSecrets_EnableMetrics_False_DoesNotRegisterSecretsMetrics()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaSecrets(options => options.EnableMetrics = false);

        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(SecretsMetrics));
        descriptor.Should().BeNull();
    }

    [Fact]
    public void AddEncinaSecrets_Default_DoesNotRegisterMetrics()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaSecrets();

        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(SecretsMetrics));
        descriptor.Should().BeNull();
    }

    [Fact]
    public void AddEncinaSecrets_EnableMetrics_True_SecretsMetricsIsResolvable()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();

        services.AddEncinaSecrets(options => options.EnableMetrics = true);

        var provider = services.BuildServiceProvider();
        var metrics = provider.GetService<SecretsMetrics>();
        metrics.Should().NotBeNull();
    }

    #endregion

    #region Test Doubles

    private sealed class TestRotationHandler : ISecretRotationHandler
    {
        public ValueTask<LanguageExt.Either<EncinaError, string>> GenerateNewSecretAsync(
            string secretName, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<LanguageExt.Either<EncinaError, string>>("new-value");

        public ValueTask<LanguageExt.Either<EncinaError, LanguageExt.Unit>> OnRotationAsync(
            string secretName, string oldValue, string newValue,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<LanguageExt.Either<EncinaError, LanguageExt.Unit>>(LanguageExt.Unit.Default);
    }

    #endregion
}
