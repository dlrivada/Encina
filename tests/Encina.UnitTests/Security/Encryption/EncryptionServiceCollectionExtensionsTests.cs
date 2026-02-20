using Encina.Security.Encryption;
using Encina.Security.Encryption.Abstractions;
using Encina.Security.Encryption.Health;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
        provider.GetService<IFieldEncryptor>().Should().NotBeNull();
        provider.GetService<IKeyProvider>().Should().NotBeNull();

        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetService<IEncryptionOrchestrator>().Should().NotBeNull();
    }

    [Fact]
    public void AddEncinaEncryption_RegistersIFieldEncryptor_AsSingleton()
    {
        var services = new ServiceCollection();

        services.AddEncinaEncryption();

        var descriptor = services.First(d => d.ServiceType == typeof(IFieldEncryptor));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaEncryption_RegistersIKeyProvider_AsSingleton()
    {
        var services = new ServiceCollection();

        services.AddEncinaEncryption();

        var descriptor = services.First(d => d.ServiceType == typeof(IKeyProvider));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaEncryption_RegistersIEncryptionOrchestrator_AsScoped()
    {
        var services = new ServiceCollection();

        services.AddEncinaEncryption();

        var descriptor = services.First(d => d.ServiceType == typeof(IEncryptionOrchestrator));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddEncinaEncryption_RegistersPipelineBehavior_AsTransient()
    {
        var services = new ServiceCollection();

        services.AddEncinaEncryption();

        var descriptor = services.First(d => d.ServiceType == typeof(IPipelineBehavior<,>));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Transient);
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
        options.FailOnDecryptionError.Should().BeFalse();
        options.EnableTracing.Should().BeTrue();
    }

    [Fact]
    public void AddEncinaEncryption_WithoutConfigure_UsesDefaults()
    {
        var services = new ServiceCollection();

        services.AddEncinaEncryption();

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<EncryptionOptions>>().Value;
        options.DefaultAlgorithm.Should().Be(EncryptionAlgorithm.Aes256Gcm);
        options.FailOnDecryptionError.Should().BeTrue();
        options.AddHealthCheck.Should().BeFalse();
        options.EnableTracing.Should().BeFalse();
        options.EnableMetrics.Should().BeFalse();
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
        resolved.Should().BeSameAs(customProvider);
    }

    [Fact]
    public void AddEncinaEncryption_ReturnsServiceCollection_ForChaining()
    {
        var services = new ServiceCollection();

        var result = services.AddEncinaEncryption();

        result.Should().BeSameAs(services);
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

        act.Should().NotThrow();
    }

    [Fact]
    public void AddEncinaEncryption_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddEncinaEncryption();

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
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
        keyProvider.Should().BeOfType<InMemoryKeyProvider>();
    }

    [Fact]
    public void AddEncinaEncryption_Generic_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddEncinaEncryption<InMemoryKeyProvider>();

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
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
        healthCheckOptions.Registrations.Should().Contain(r => r.Name == EncryptionHealthCheck.DefaultName);
    }

    [Fact]
    public void AddEncinaEncryption_HealthCheckDisabled_DoesNotRegisterHealthCheck()
    {
        var services = new ServiceCollection();

        services.AddEncinaEncryption(options => options.AddHealthCheck = false);

        var hasHealthChecks = services.Any(d => d.ServiceType == typeof(HealthCheckService));
        hasHealthChecks.Should().BeFalse();
    }

    [Fact]
    public void AddEncinaEncryption_DefaultOptions_DoesNotRegisterHealthCheck()
    {
        var services = new ServiceCollection();

        services.AddEncinaEncryption();

        var hasHealthChecks = services.Any(d => d.ServiceType == typeof(HealthCheckService));
        hasHealthChecks.Should().BeFalse();
    }

    #endregion
}
