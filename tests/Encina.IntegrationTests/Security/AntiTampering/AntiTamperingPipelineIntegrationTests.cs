using System.Security.Cryptography;
using System.Text;
using Encina.Security.AntiTampering;
using Encina.Security.AntiTampering.Abstractions;
using Encina.Security.AntiTampering.Health;
using Encina.Security.AntiTampering.HMAC;
using Encina.Security.AntiTampering.Nonce;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Encina.IntegrationTests.Security.AntiTampering;

/// <summary>
/// Integration tests for the full Encina.Security.AntiTampering pipeline.
/// Tests DI registration, full sign/verify flows, and health check integration.
/// No Docker containers needed — all operations are pure in-process.
/// </summary>
[Trait("Category", "Integration")]
public sealed class AntiTamperingPipelineIntegrationTests
{
    #region DI Registration

    [Fact]
    public void AddEncinaAntiTampering_RegistersAllServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(TimeProvider.System);

        // Act
        services.AddEncinaAntiTampering(options =>
        {
            options.AddKey("test-key", "super-secret-value");
        });
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IRequestSigner>().Should().NotBeNull();
        provider.GetService<INonceStore>().Should().NotBeNull();
        provider.GetService<IKeyProvider>().Should().NotBeNull();
        provider.GetService<IRequestSigningClient>().Should().NotBeNull();
    }

    [Fact]
    public void AddEncinaAntiTampering_WithHealthCheck_RegistersHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaAntiTampering(options =>
        {
            options.AddKey("test-key", "my-secret");
            options.AddHealthCheck = true;
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var healthCheckService = provider.GetService<HealthCheckService>();
        healthCheckService.Should().NotBeNull();
    }

    #endregion

    #region Full Sign/Verify Flow

    [Fact]
    public async Task FullPipeline_SignAndVerify_WithDI()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaAntiTampering(options =>
        {
            options.AddKey("integration-key", "integration-secret-value-32-bytes!");
        });
        var provider = services.BuildServiceProvider();

        var signer = provider.GetRequiredService<IRequestSigner>();
        var payload = Encoding.UTF8.GetBytes("{\"amount\":99.99}");
        var context = new SigningContext
        {
            KeyId = "integration-key",
            HttpMethod = "POST",
            RequestPath = "/api/orders",
            Timestamp = DateTimeOffset.UtcNow,
            Nonce = Guid.NewGuid().ToString("N")
        };

        // Act - Sign
        var signResult = await signer.SignAsync(payload.AsMemory(), context);

        // Assert - Signed
        signResult.IsRight.Should().BeTrue();
        var signature = (string)signResult;
        signature.Should().NotBeNullOrWhiteSpace();

        // Act - Verify
        var verifyResult = await signer.VerifyAsync(payload.AsMemory(), signature, context);

        // Assert - Verified
        verifyResult.IsRight.Should().BeTrue();
        ((bool)verifyResult).Should().BeTrue();
    }

    [Theory]
    [InlineData(HMACAlgorithm.SHA256)]
    [InlineData(HMACAlgorithm.SHA384)]
    [InlineData(HMACAlgorithm.SHA512)]
    public async Task FullPipeline_AllAlgorithms_SignAndVerifySucceed(HMACAlgorithm algorithm)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaAntiTampering(options =>
        {
            options.Algorithm = algorithm;
            options.AddKey("algo-key", "secret-for-algorithm-testing-32b!");
        });
        var provider = services.BuildServiceProvider();

        var signer = provider.GetRequiredService<IRequestSigner>();
        var payload = Encoding.UTF8.GetBytes("{\"test\":true}");
        var context = new SigningContext
        {
            KeyId = "algo-key",
            HttpMethod = "POST",
            RequestPath = "/api/test",
            Timestamp = DateTimeOffset.UtcNow,
            Nonce = Guid.NewGuid().ToString("N")
        };

        // Act
        var signResult = await signer.SignAsync(payload.AsMemory(), context);
        signResult.IsRight.Should().BeTrue();

        var verifyResult = await signer.VerifyAsync(
            payload.AsMemory(), (string)signResult, context);

        // Assert
        verifyResult.IsRight.Should().BeTrue();
        ((bool)verifyResult).Should().BeTrue();
    }

    #endregion

    #region Nonce Replay Protection

    [Fact]
    public async Task NonceStore_ReplayDetection_RejectsSecondAttempt()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaAntiTampering();
        var provider = services.BuildServiceProvider();

        var nonceStore = provider.GetRequiredService<INonceStore>();
        var nonce = Guid.NewGuid().ToString("N");
        var expiry = TimeSpan.FromMinutes(10);

        // Act
        var firstAdd = await nonceStore.TryAddAsync(nonce, expiry);
        var secondAdd = await nonceStore.TryAddAsync(nonce, expiry);

        // Assert
        firstAdd.Should().BeTrue();
        secondAdd.Should().BeFalse();
    }

    [Fact]
    public async Task NonceStore_ConcurrentAdds_OnlyOneSucceeds()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaAntiTampering();
        var provider = services.BuildServiceProvider();

        var nonceStore = provider.GetRequiredService<INonceStore>();
        var nonce = "concurrent-nonce-test";
        var expiry = TimeSpan.FromMinutes(10);

        // Act - run 10 concurrent TryAdd for the same nonce
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => nonceStore.TryAddAsync(nonce, expiry).AsTask())
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert - exactly one should succeed
        results.Count(r => r).Should().Be(1);
        results.Count(r => !r).Should().Be(9);
    }

    #endregion

    #region Client Sign + Server Verify

    [Fact]
    public async Task RequestSigningClient_SignedRequest_CanBeVerifiedBySigner()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(TimeProvider.System);
        services.AddEncinaAntiTampering(options =>
        {
            options.AddKey("client-key", "shared-secret-for-client-server-flow!");
        });
        var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<IRequestSigningClient>();
        var signer = provider.GetRequiredService<IRequestSigner>();
        var optionsSnapshot = provider.GetRequiredService<IOptions<AntiTamperingOptions>>();
        var options = optionsSnapshot.Value;

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.example.com/api/orders")
        {
            Content = new StringContent("{\"amount\":42}", Encoding.UTF8, "application/json")
        };

        // Act - Client signs
        var signResult = await client.SignRequestAsync(request, "client-key");
        signResult.IsRight.Should().BeTrue();

        var signedRequest = (HttpRequestMessage)signResult;

        // Extract what was signed
        var signature = signedRequest.Headers.GetValues(options.SignatureHeader).First();
        var timestamp = signedRequest.Headers.GetValues(options.TimestampHeader).First();
        var nonce = signedRequest.Headers.GetValues(options.NonceHeader).First();
        var keyId = signedRequest.Headers.GetValues(options.KeyIdHeader).First();

        // Reconstruct signing context for server-side verification
        var payload = await signedRequest.Content!.ReadAsByteArrayAsync();
        var serverContext = new SigningContext
        {
            KeyId = keyId,
            HttpMethod = signedRequest.Method.Method.ToUpperInvariant(),
            RequestPath = signedRequest.RequestUri!.PathAndQuery,
            Timestamp = DateTimeOffset.Parse(timestamp, System.Globalization.CultureInfo.InvariantCulture),
            Nonce = nonce
        };

        // Act - Server verifies
        var verifyResult = await signer.VerifyAsync(payload.AsMemory(), signature, serverContext);

        // Assert
        verifyResult.IsRight.Should().BeTrue();
        ((bool)verifyResult).Should().BeTrue();
    }

    #endregion

    #region Health Check

    [Fact]
    public async Task HealthCheck_AllServicesRegistered_ReturnsHealthy()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaAntiTampering(options =>
        {
            options.AddKey("health-key", "health-check-secret");
            options.AddHealthCheck = true;
        });
        var provider = services.BuildServiceProvider();

        var healthCheck = new AntiTamperingHealthCheck(provider);

        // Act
        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext
            {
                Registration = new HealthCheckRegistration(
                    AntiTamperingHealthCheck.DefaultName,
                    healthCheck,
                    HealthStatus.Unhealthy,
                    AntiTamperingHealthCheck.Tags)
            });

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data.Should().ContainKey("keyProvider");
        result.Data.Should().ContainKey("requestSigner");
        result.Data.Should().ContainKey("nonceStore");
    }

    #endregion

    #region Custom Key Provider Override

    [Fact]
    public void AddEncinaAntiTampering_CustomKeyProviderRegisteredFirst_IsNotOverridden()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Register custom key provider BEFORE AddEncinaAntiTampering
        var customKeyProvider = new InMemoryKeyProvider(Options.Create(new AntiTamperingOptions()));
        services.AddSingleton<IKeyProvider>(customKeyProvider);

        // Act
        services.AddEncinaAntiTampering();
        var provider = services.BuildServiceProvider();

        // Assert - should be our custom instance (TryAdd doesn't override)
        var resolved = provider.GetRequiredService<IKeyProvider>();
        resolved.Should().BeSameAs(customKeyProvider);
    }

    #endregion
}
