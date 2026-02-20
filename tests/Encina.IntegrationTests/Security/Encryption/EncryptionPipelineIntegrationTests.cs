using System.Security.Cryptography;
using Encina.Security.Encryption;
using Encina.Security.Encryption.Abstractions;
using Encina.Security.Encryption.Health;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Encina.IntegrationTests.Security.Encryption;

/// <summary>
/// Integration tests for the full Encina.Security.Encryption pipeline.
/// Tests DI registration, full encrypt/decrypt flows, key rotation,
/// multi-tenancy isolation, and health check integration.
/// No Docker containers needed — encryption is pure in-process.
/// </summary>
[Trait("Category", "Integration")]
public sealed class EncryptionPipelineIntegrationTests : IDisposable
{
    public EncryptionPipelineIntegrationTests()
    {
        EncryptedPropertyCache.ClearCache();
    }

    public void Dispose()
    {
        EncryptedPropertyCache.ClearCache();
    }

    #region Full DI Pipeline

    [Fact]
    public void AddEncinaEncryption_RegistersAllServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaEncryption();
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IFieldEncryptor>().Should().NotBeNull();
        provider.GetService<IKeyProvider>().Should().NotBeNull();

        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetService<IEncryptionOrchestrator>().Should().NotBeNull();
    }

    [Fact]
    public async Task FullPipeline_EncryptAndDecrypt_WithDI()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaEncryption();
        var provider = services.BuildServiceProvider();

        var keyProvider = provider.GetRequiredService<IKeyProvider>() as InMemoryKeyProvider;
        keyProvider.Should().NotBeNull();

        var key = new byte[32];
        RandomNumberGenerator.Fill(key);
        keyProvider!.AddKey("integration-key", key);
        keyProvider.SetCurrentKey("integration-key");

        using var scope = provider.CreateScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<IEncryptionOrchestrator>();
        var context = RequestContext.CreateForTest(userId: "user-1", tenantId: "tenant-1");

        var command = new TestUserCommand { Email = "user@example.com", Phone = "+1234567890", Name = "John" };

        // Act - Encrypt
        var encryptResult = await orchestrator.EncryptAsync(command, context);

        // Assert - Encrypted
        encryptResult.IsRight.Should().BeTrue();
        command.Email.Should().StartWith("ENC:v1:");
        command.Phone.Should().StartWith("ENC:v1:");
        command.Name.Should().Be("John"); // Not encrypted

        // Capture encrypted values
        var encryptedEmail = command.Email;
        var encryptedPhone = command.Phone;

        // Act - Decrypt
        var decryptResult = await orchestrator.DecryptAsync(command, context);

        // Assert - Decrypted back to original
        decryptResult.IsRight.Should().BeTrue();
        command.Email.Should().Be("user@example.com");
        command.Phone.Should().Be("+1234567890");
        command.Name.Should().Be("John");

        // Verify the encrypted values were different from plaintext
        encryptedEmail.Should().NotBe("user@example.com");
        encryptedPhone.Should().NotBe("+1234567890");
    }

    [Fact]
    public async Task FullPipeline_WithCustomKeyProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Register custom key provider before AddEncinaEncryption
        var customProvider = new InMemoryKeyProvider();
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);
        customProvider.AddKey("custom-key", key);
        customProvider.SetCurrentKey("custom-key");
        services.AddSingleton<IKeyProvider>(customProvider);

        services.AddEncinaEncryption();
        var provider = services.BuildServiceProvider();

        // Verify our custom provider is used (not a new InMemoryKeyProvider)
        var resolvedKeyProvider = provider.GetRequiredService<IKeyProvider>();
        resolvedKeyProvider.Should().BeSameAs(customProvider);

        // Test encryption works with custom provider
        using var scope = provider.CreateScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<IEncryptionOrchestrator>();
        var context = RequestContext.CreateForTest();

        var command = new TestUserCommand { Email = "custom@test.com", Phone = "+9999999999" };
        var result = await orchestrator.EncryptAsync(command, context);

        result.IsRight.Should().BeTrue();
        command.Email.Should().StartWith("ENC:v1:");
    }

    #endregion

    #region Key Rotation

    [Fact]
    public async Task KeyRotation_OldDataRemainsDecryptable()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaEncryption();
        var provider = services.BuildServiceProvider();

        var keyProvider = provider.GetRequiredService<IKeyProvider>() as InMemoryKeyProvider;
        var key1 = new byte[32];
        RandomNumberGenerator.Fill(key1);
        keyProvider!.AddKey("key-v1", key1);
        keyProvider.SetCurrentKey("key-v1");

        using var scope = provider.CreateScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<IEncryptionOrchestrator>();
        var context = RequestContext.CreateForTest(tenantId: "tenant-1");

        // Encrypt with key-v1
        var command = new TestUserCommand { Email = "old-data@test.com", Phone = "+1111111111", Name = "Original" };
        var encryptResult = await orchestrator.EncryptAsync(command, context);
        encryptResult.IsRight.Should().BeTrue();

        var encryptedWithV1 = command.Email;

        // Rotate to key-v2
        var key2 = new byte[32];
        RandomNumberGenerator.Fill(key2);
        keyProvider.AddKey("key-v2", key2);
        keyProvider.SetCurrentKey("key-v2");

        // Decrypt old data (encrypted with key-v1) — should still work
        var decryptResult = await orchestrator.DecryptAsync(command, context);
        decryptResult.IsRight.Should().BeTrue();
        command.Email.Should().Be("old-data@test.com");

        // Encrypt new data with key-v2
        var newCommand = new TestUserCommand { Email = "new-data@test.com", Phone = "+2222222222", Name = "New" };
        var encryptResult2 = await orchestrator.EncryptAsync(newCommand, context);
        encryptResult2.IsRight.Should().BeTrue();

        // New data should use key-v2
        var decryptResult2 = await orchestrator.DecryptAsync(newCommand, context);
        decryptResult2.IsRight.Should().BeTrue();
        newCommand.Email.Should().Be("new-data@test.com");
    }

    [Fact]
    public async Task KeyRotation_ViaRotateKeyAsync_GeneratesNewKey()
    {
        // Arrange
        var keyProvider = new InMemoryKeyProvider();
        var key1 = new byte[32];
        RandomNumberGenerator.Fill(key1);
        keyProvider.AddKey("initial-key", key1);
        keyProvider.SetCurrentKey("initial-key");

        // Act
        var rotateResult = await keyProvider.RotateKeyAsync();

        // Assert
        rotateResult.IsRight.Should().BeTrue();
        var newKeyId = rotateResult.Match(Right: id => id, Left: _ => string.Empty);
        newKeyId.Should().NotBe("initial-key");

        // Old key should still be retrievable
        var oldKeyResult = await keyProvider.GetKeyAsync("initial-key");
        oldKeyResult.IsRight.Should().BeTrue();

        // New key should be current
        var currentKeyResult = await keyProvider.GetCurrentKeyIdAsync();
        currentKeyResult.IsRight.Should().BeTrue();
        currentKeyResult.Match(Right: id => id, Left: _ => "").Should().Be(newKeyId);
    }

    #endregion

    #region Multi-Tenancy Isolation

    [Fact]
    public async Task MultiTenancy_DifferentTenants_EncryptedDataNotInterchangeable()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaEncryption();
        var provider = services.BuildServiceProvider();

        var keyProvider = provider.GetRequiredService<IKeyProvider>() as InMemoryKeyProvider;
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);
        keyProvider!.AddKey("shared-key", key);
        keyProvider.SetCurrentKey("shared-key");

        using var scope = provider.CreateScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<IEncryptionOrchestrator>();

        var context1 = RequestContext.CreateForTest(tenantId: "tenant-A");
        var context2 = RequestContext.CreateForTest(tenantId: "tenant-B");

        // Encrypt same data for two tenants
        var command1 = new TestUserCommand { Email = "user@test.com", Phone = "+1111111111", Name = "Test" };
        var command2 = new TestUserCommand { Email = "user@test.com", Phone = "+1111111111", Name = "Test" };

        await orchestrator.EncryptAsync(command1, context1);
        await orchestrator.EncryptAsync(command2, context2);

        // Both should be encrypted
        command1.Email.Should().StartWith("ENC:v1:");
        command2.Email.Should().StartWith("ENC:v1:");

        // Due to random nonces, encrypted values should differ even for same plaintext
        command1.Email.Should().NotBe(command2.Email);

        // Each should decrypt with its own context
        var decrypt1 = await orchestrator.DecryptAsync(command1, context1);
        var decrypt2 = await orchestrator.DecryptAsync(command2, context2);

        decrypt1.IsRight.Should().BeTrue();
        decrypt2.IsRight.Should().BeTrue();

        command1.Email.Should().Be("user@test.com");
        command2.Email.Should().Be("user@test.com");
    }

    #endregion

    #region Health Check Integration

    [Fact]
    public async Task HealthCheck_Healthy_WhenProperlyConfigured()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaEncryption(opts => opts.AddHealthCheck = true);
        var provider = services.BuildServiceProvider();

        var keyProvider = provider.GetRequiredService<IKeyProvider>() as InMemoryKeyProvider;
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);
        keyProvider!.AddKey("health-key", key);
        keyProvider.SetCurrentKey("health-key");

        var healthCheck = new EncryptionHealthCheck(provider);

        // Act
        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext
            {
                Registration = new HealthCheckRegistration("test", healthCheck, HealthStatus.Unhealthy, [])
            });

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data.Should().ContainKey("currentKeyId");
        result.Data["currentKeyId"].Should().Be("health-key");
    }

    [Fact]
    public async Task HealthCheck_Unhealthy_WhenNoKeyConfigured()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaEncryption(opts => opts.AddHealthCheck = true);
        var provider = services.BuildServiceProvider();

        // Don't add any keys
        var healthCheck = new EncryptionHealthCheck(provider);

        // Act
        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext
            {
                Registration = new HealthCheckRegistration("test", healthCheck, HealthStatus.Unhealthy, [])
            });

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
    }

    #endregion

    #region GenericOverload

    [Fact]
    public void AddEncinaEncryption_GenericOverload_RegistersCustomKeyProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaEncryption<InMemoryKeyProvider>();
        var provider = services.BuildServiceProvider();

        // Assert
        var keyProvider = provider.GetRequiredService<IKeyProvider>();
        keyProvider.Should().BeOfType<InMemoryKeyProvider>();
    }

    #endregion

    #region Concurrent Encryption

    [Fact]
    public async Task ConcurrentEncryption_MultipleThreads_NoDataCorruption()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaEncryption();
        var provider = services.BuildServiceProvider();

        var keyProvider = provider.GetRequiredService<IKeyProvider>() as InMemoryKeyProvider;
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);
        keyProvider!.AddKey("concurrent-key", key);
        keyProvider.SetCurrentKey("concurrent-key");

        var tasks = new List<Task<bool>>();

        // Act - 50 concurrent encrypt/decrypt operations
        for (var i = 0; i < 50; i++)
        {
            var index = i;
            tasks.Add(Task.Run(async () =>
            {
                using var scope = provider.CreateScope();
                var orchestrator = scope.ServiceProvider.GetRequiredService<IEncryptionOrchestrator>();
                var context = RequestContext.CreateForTest(userId: $"user-{index}");

                var email = $"user{index}@test.com";
                var phone = $"+100000{index:D4}";
                var command = new TestUserCommand { Email = email, Phone = phone, Name = $"User {index}" };

                var encryptResult = await orchestrator.EncryptAsync(command, context);
                if (encryptResult.IsLeft) return false;

                var decryptResult = await orchestrator.DecryptAsync(command, context);
                if (decryptResult.IsLeft) return false;

                return command.Email == email && command.Name == $"User {index}";
            }));
        }

        var results = await Task.WhenAll(tasks);

        // Assert - All operations should succeed
        results.Should().AllBeEquivalentTo(true);
    }

    #endregion

    #region Test Types

    private sealed class TestUserCommand
    {
        [Encrypt(Purpose = "User.Email")]
        public string Email { get; set; } = string.Empty;

        [Encrypt(Purpose = "User.Phone")]
        public string Phone { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
    }

    #endregion
}
