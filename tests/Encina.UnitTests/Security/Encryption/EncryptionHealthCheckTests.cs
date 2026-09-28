#pragma warning disable CA2012 // Use ValueTasks correctly - required for NSubstitute mock setup

using Encina.Security.Encryption;
using Encina.Security.Encryption.Abstractions;
using Encina.Security.Encryption.Health;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Security.Encryption;

public sealed class EncryptionHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_AllServicesRegistered_ReturnsHealthy()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaEncryption(options => options.AddHealthCheck = true);
        var provider = services.BuildServiceProvider();

        // Ensure key exists for roundtrip
        var keyProvider = provider.GetRequiredService<IKeyProvider>() as InMemoryKeyProvider;
        await keyProvider!.RotateKeyAsync();

        var healthCheck = new EncryptionHealthCheck(provider);

        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext
            {
                Registration = new HealthCheckRegistration("test", healthCheck, null, null)
            });

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description!.ShouldContain("healthy");
    }

    [Fact]
    public async Task CheckHealthAsync_NoKeyProvider_ReturnsUnhealthy()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        var healthCheck = new EncryptionHealthCheck(provider);

        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext
            {
                Registration = new HealthCheckRegistration("test", healthCheck, null, null)
            });

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("IKeyProvider");
    }

    [Fact]
    public async Task CheckHealthAsync_NoCurrentKey_ReturnsUnhealthy()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaEncryption();
        var provider = services.BuildServiceProvider();

        // Don't set up any keys — provider is empty
        var healthCheck = new EncryptionHealthCheck(provider);

        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext
            {
                Registration = new HealthCheckRegistration("test", healthCheck, null, null)
            });

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("current key");
    }

    [Fact]
    public async Task CheckHealthAsync_HealthyResult_ContainsKeyIdData()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaEncryption();
        var provider = services.BuildServiceProvider();

        var keyProvider = provider.GetRequiredService<IKeyProvider>() as InMemoryKeyProvider;
        await keyProvider!.RotateKeyAsync();

        var healthCheck = new EncryptionHealthCheck(provider);

        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext
            {
                Registration = new HealthCheckRegistration("test", healthCheck, null, null)
            });

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Data.ShouldContainKey("currentKeyId");
        result.Data.ShouldContainKey("algorithm");
    }

    [Fact]
    public async Task CheckHealthAsync_KeyProviderReturnsError_DoesNotLeakErrorMessage()
    {
        const string sentinel = "sensitive-connection-string-sentinel";
        var keyProvider = Substitute.For<IKeyProvider>();
        keyProvider.GetCurrentKeyIdAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Left<EncinaError, string>(EncinaErrors.Create("key.provider.failed", sentinel))));

        var services = new ServiceCollection();
        services.AddSingleton(keyProvider);
        var provider = services.BuildServiceProvider();

        var healthCheck = new EncryptionHealthCheck(provider);

        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext
            {
                Registration = new HealthCheckRegistration("test", healthCheck, null, null)
            });

        result.Description!.ShouldNotContain(sentinel);
        result.Description!.ShouldContain("key.provider.failed");
    }

    [Fact]
    public void DefaultName_IsCorrect()
    {
        EncryptionHealthCheck.DefaultName.ShouldBe("encina-encryption");
    }

    [Fact]
    public void Tags_ContainsExpectedValues()
    {
        EncryptionHealthCheck.Tags.ShouldContain("encina");
        EncryptionHealthCheck.Tags.ShouldContain("encryption");
        EncryptionHealthCheck.Tags.ShouldContain("ready");
    }
}
