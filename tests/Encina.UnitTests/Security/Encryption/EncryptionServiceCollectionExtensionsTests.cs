using Encina.Security.Encryption;
using Encina.Security.Encryption.Abstractions;
using Encina.Security.Encryption.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Encina.UnitTests.Security.Encryption;

public sealed class EncryptionServiceCollectionExtensionsTests
{
    #region AddEncinaEncryption

    [Fact]
    public void AddEncinaEncryption_RegistersAllRequiredServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaEncryption();

        var provider = services.BuildServiceProvider();
        provider.GetService<IFieldEncryptor>().ShouldNotBeNull();
        provider.GetService<IKeyProvider>().ShouldNotBeNull();

        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetService<IEncryptionOrchestrator>().ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaEncryption_RegistersIFieldEncryptor_AsSingleton()
    {
        var services = new ServiceCollection();

        services.AddEncinaEncryption();

        var descriptor = services.First(d => d.ServiceType == typeof(IFieldEncryptor));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaEncryption_RegistersIKeyProvider_AsSingleton()
    {
        var services = new ServiceCollection();

        services.AddEncinaEncryption();

        var descriptor = services.First(d => d.ServiceType == typeof(IKeyProvider));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaEncryption_RegistersIEncryptionOrchestrator_AsScoped()
    {
        var services = new ServiceCollection();

        services.AddEncinaEncryption();

        var descriptor = services.First(d => d.ServiceType == typeof(IEncryptionOrchestrator));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddEncinaEncryption_RegistersPipelineBehavior_AsTransient()
    {
        var services = new ServiceCollection();

        services.AddEncinaEncryption();

        var descriptor = services.First(d => d.ServiceType == typeof(IPipelineBehavior<,>));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Transient);
    }

    [Fact]
    public void AddEncinaEncryption_ConfigureOptions_AppliesConfiguration()
    {
        var services = new ServiceCollection();

        services.AddEncinaEncryption(options =>
        {
            options.FailOnDecryptionError = false;
            options.EnableTracing = true;
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<EncryptionOptions>>().Value;
        options.FailOnDecryptionError.ShouldBeFalse();
        options.EnableTracing.ShouldBeTrue();
    }

    [Fact]
    public void AddEncinaEncryption_WithoutConfigure_UsesDefaults()
    {
        var services = new ServiceCollection();

        services.AddEncinaEncryption();

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<EncryptionOptions>>().Value;
        options.DefaultAlgorithm.ShouldBe(EncryptionAlgorithm.Aes256Gcm);
        options.FailOnDecryptionError.ShouldBeTrue();
        options.AddHealthCheck.ShouldBeFalse();
        options.EnableTracing.ShouldBeFalse();
        options.EnableMetrics.ShouldBeFalse();
    }

    [Fact]
    public void AddEncinaEncryption_TryAdd_AllowsOverride()
    {
        var services = new ServiceCollection();
        var customProvider = new InMemoryKeyProvider();

        // Register custom provider BEFORE AddEncinaEncryption
        services.AddSingleton<IKeyProvider>(customProvider);
        services.AddEncinaEncryption();

        var provider = services.BuildServiceProvider();
        var resolved = provider.GetRequiredService<IKeyProvider>();
        resolved.ShouldBeSameAs(customProvider);
    }

    [Fact]
    public void AddEncinaEncryption_ReturnsServiceCollection_ForChaining()
    {
        var services = new ServiceCollection();

        var result = services.AddEncinaEncryption();

        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddEncinaEncryption_CalledMultipleTimes_DoesNotThrow()
    {
        var services = new ServiceCollection();

        var act = () =>
        {
            services.AddEncinaEncryption();
            services.AddEncinaEncryption();
        };

        Should.NotThrow(act);
    }

    [Fact]
    public void AddEncinaEncryption_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddEncinaEncryption();

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("services");
    }

    #endregion

    #region AddEncinaEncryption<TKeyProvider>

    [Fact]
    public void AddEncinaEncryption_Generic_RegistersCustomKeyProvider()
    {
        var services = new ServiceCollection();

        services.AddEncinaEncryption<InMemoryKeyProvider>();

        var provider = services.BuildServiceProvider();
        var keyProvider = provider.GetRequiredService<IKeyProvider>();
        keyProvider.ShouldBeOfType<InMemoryKeyProvider>();
    }

    [Fact]
    public void AddEncinaEncryption_Generic_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddEncinaEncryption<InMemoryKeyProvider>();

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("services");
    }

    #endregion

    #region Health Check Registration

    [Fact]
    public void AddEncinaEncryption_HealthCheckEnabled_RegistersHealthCheck()
    {
        var services = new ServiceCollection();

        services.AddEncinaEncryption(options => options.AddHealthCheck = true);

        var provider = services.BuildServiceProvider();
        var healthCheckOptions = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        healthCheckOptions.Registrations.ShouldContain(r => r.Name == EncryptionHealthCheck.DefaultName);
    }

    [Fact]
    public void AddEncinaEncryption_HealthCheckDisabled_DoesNotRegisterHealthCheck()
    {
        var services = new ServiceCollection();

        services.AddEncinaEncryption(options => options.AddHealthCheck = false);

        var hasHealthChecks = services.Any(d => d.ServiceType == typeof(HealthCheckService));
        hasHealthChecks.ShouldBeFalse();
    }

    [Fact]
    public void AddEncinaEncryption_DefaultOptions_DoesNotRegisterHealthCheck()
    {
        var services = new ServiceCollection();

        services.AddEncinaEncryption();

        var hasHealthChecks = services.Any(d => d.ServiceType == typeof(HealthCheckService));
        hasHealthChecks.ShouldBeFalse();
    }

    #endregion
}
