using System.Collections.Immutable;
using Encina.Security.Encryption;
using Encina.Security.Encryption.Abstractions;
using Encina.Security.Encryption.Algorithms;
using Encina.Security.Encryption.Health;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.Security.Encryption;

/// <summary>
/// Guard clause tests for Encina.Security.Encryption types.
/// Verifies that null arguments are properly rejected.
/// </summary>
public class EncryptionGuardTests
{
    #region AesGcmFieldEncryptor Guard Tests

    [Fact]
    public void AesGcmFieldEncryptor_Constructor_NullKeyProvider_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new AesGcmFieldEncryptor(null!);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("keyProvider");
    }

    [Fact]
    public async Task AesGcmFieldEncryptor_EncryptStringAsync_NullPlaintext_ThrowsArgumentNullException()
    {
        // Arrange
        var keyProvider = Substitute.For<IKeyProvider>();
        var encryptor = new AesGcmFieldEncryptor(keyProvider);
        var context = new EncryptionContext();

        // Act
        var act = async () => await encryptor.EncryptStringAsync(null!, context);

        // Assert
        (await Should.ThrowAsync<ArgumentNullException>(act))
            .ParamName.ShouldBe("plaintext");
    }

    [Fact]
    public async Task AesGcmFieldEncryptor_EncryptStringAsync_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var keyProvider = Substitute.For<IKeyProvider>();
        var encryptor = new AesGcmFieldEncryptor(keyProvider);

        // Act
        var act = async () => await encryptor.EncryptStringAsync("test", null!);

        // Assert
        (await Should.ThrowAsync<ArgumentNullException>(act))
            .ParamName.ShouldBe("context");
    }

    [Fact]
    public async Task AesGcmFieldEncryptor_DecryptStringAsync_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var keyProvider = Substitute.For<IKeyProvider>();
        var encryptor = new AesGcmFieldEncryptor(keyProvider);
        var encrypted = new EncryptedValue
        {
            Ciphertext = ImmutableArray<byte>.Empty,
            Algorithm = EncryptionAlgorithm.Aes256Gcm,
            KeyId = "test-key",
            Nonce = ImmutableArray<byte>.Empty
        };

        // Act
        var act = async () => await encryptor.DecryptStringAsync(encrypted, null!);

        // Assert
        (await Should.ThrowAsync<ArgumentNullException>(act))
            .ParamName.ShouldBe("context");
    }

    [Fact]
    public async Task AesGcmFieldEncryptor_EncryptBytesAsync_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var keyProvider = Substitute.For<IKeyProvider>();
        var encryptor = new AesGcmFieldEncryptor(keyProvider);

        // Act
        var act = async () => await encryptor.EncryptBytesAsync(new byte[] { 1, 2, 3 }, null!);

        // Assert
        (await Should.ThrowAsync<ArgumentNullException>(act))
            .ParamName.ShouldBe("context");
    }

    [Fact]
    public async Task AesGcmFieldEncryptor_DecryptBytesAsync_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var keyProvider = Substitute.For<IKeyProvider>();
        var encryptor = new AesGcmFieldEncryptor(keyProvider);
        var encrypted = new EncryptedValue
        {
            Ciphertext = ImmutableArray<byte>.Empty,
            Algorithm = EncryptionAlgorithm.Aes256Gcm,
            KeyId = "test-key",
            Nonce = ImmutableArray<byte>.Empty
        };

        // Act
        var act = async () => await encryptor.DecryptBytesAsync(encrypted, null!);

        // Assert
        (await Should.ThrowAsync<ArgumentNullException>(act))
            .ParamName.ShouldBe("context");
    }

    #endregion

    #region EncryptionOrchestrator Guard Tests

    [Fact]
    public void EncryptionOrchestrator_Constructor_NullFieldEncryptor_ThrowsArgumentNullException()
    {
        // Arrange
        var logger = NullLogger<EncryptionOrchestrator>.Instance;

        // Act
        var act = () => new EncryptionOrchestrator(null!, logger);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("fieldEncryptor");
    }

    [Fact]
    public void EncryptionOrchestrator_Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var fieldEncryptor = Substitute.For<IFieldEncryptor>();

        // Act
        var act = () => new EncryptionOrchestrator(fieldEncryptor, null!);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("logger");
    }

    [Fact]
    public async Task EncryptionOrchestrator_EncryptAsync_NullInstance_ThrowsArgumentNullException()
    {
        // Arrange
        var fieldEncryptor = Substitute.For<IFieldEncryptor>();
        var logger = NullLogger<EncryptionOrchestrator>.Instance;
        var orchestrator = new EncryptionOrchestrator(fieldEncryptor, logger);
        var context = RequestContext.CreateForTest();

        // Act
        var act = async () => await orchestrator.EncryptAsync<object>(null!, context);

        // Assert
        (await Should.ThrowAsync<ArgumentNullException>(act))
            .ParamName.ShouldBe("instance");
    }

    [Fact]
    public async Task EncryptionOrchestrator_EncryptAsync_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var fieldEncryptor = Substitute.For<IFieldEncryptor>();
        var logger = NullLogger<EncryptionOrchestrator>.Instance;
        var orchestrator = new EncryptionOrchestrator(fieldEncryptor, logger);

        // Act
        var act = async () => await orchestrator.EncryptAsync("test", null!);

        // Assert
        (await Should.ThrowAsync<ArgumentNullException>(act))
            .ParamName.ShouldBe("context");
    }

    [Fact]
    public async Task EncryptionOrchestrator_DecryptAsync_NullInstance_ThrowsArgumentNullException()
    {
        // Arrange
        var fieldEncryptor = Substitute.For<IFieldEncryptor>();
        var logger = NullLogger<EncryptionOrchestrator>.Instance;
        var orchestrator = new EncryptionOrchestrator(fieldEncryptor, logger);
        var context = RequestContext.CreateForTest();

        // Act
        var act = async () => await orchestrator.DecryptAsync<object>(null!, context);

        // Assert
        (await Should.ThrowAsync<ArgumentNullException>(act))
            .ParamName.ShouldBe("instance");
    }

    [Fact]
    public async Task EncryptionOrchestrator_DecryptAsync_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var fieldEncryptor = Substitute.For<IFieldEncryptor>();
        var logger = NullLogger<EncryptionOrchestrator>.Instance;
        var orchestrator = new EncryptionOrchestrator(fieldEncryptor, logger);

        // Act
        var act = async () => await orchestrator.DecryptAsync("test", null!);

        // Assert
        (await Should.ThrowAsync<ArgumentNullException>(act))
            .ParamName.ShouldBe("context");
    }

    #endregion

    #region EncryptionPipelineBehavior Guard Tests

    [Fact]
    public void EncryptionPipelineBehavior_Constructor_NullOrchestrator_ThrowsArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new EncryptionOptions());
        var logger = Substitute.For<ILogger<EncryptionPipelineBehavior<TestCommand, Unit>>>();

        // Act
        var act = () => new EncryptionPipelineBehavior<TestCommand, Unit>(null!, options, logger);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("orchestrator");
    }

    [Fact]
    public void EncryptionPipelineBehavior_Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var orchestrator = Substitute.For<IEncryptionOrchestrator>();
        var logger = Substitute.For<ILogger<EncryptionPipelineBehavior<TestCommand, Unit>>>();

        // Act
        var act = () => new EncryptionPipelineBehavior<TestCommand, Unit>(orchestrator, null!, logger);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("options");
    }

    [Fact]
    public void EncryptionPipelineBehavior_Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var orchestrator = Substitute.For<IEncryptionOrchestrator>();
        var options = Options.Create(new EncryptionOptions());

        // Act
        var act = () => new EncryptionPipelineBehavior<TestCommand, Unit>(orchestrator, options, null!);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("logger");
    }

    #endregion

    #region InMemoryKeyProvider Guard Tests

    [Fact]
    public void InMemoryKeyProvider_AddKey_NullKeyId_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = new InMemoryKeyProvider();

        // Act
        var act = () => provider.AddKey(null!, new byte[32]);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("keyId");
    }

    [Fact]
    public void InMemoryKeyProvider_AddKey_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = new InMemoryKeyProvider();

        // Act
        var act = () => provider.AddKey("key-1", null!);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("key");
    }

    [Fact]
    public void InMemoryKeyProvider_SetCurrentKey_NullKeyId_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = new InMemoryKeyProvider();

        // Act
        var act = () => provider.SetCurrentKey(null!);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("keyId");
    }

    [Fact]
    public async Task InMemoryKeyProvider_GetKeyAsync_NullKeyId_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = new InMemoryKeyProvider();

        // Act
        var act = async () => await provider.GetKeyAsync(null!);

        // Assert
        (await Should.ThrowAsync<ArgumentNullException>(act))
            .ParamName.ShouldBe("keyId");
    }

    #endregion

    #region ServiceCollectionExtensions Guard Tests

    [Fact]
    public void AddEncinaEncryption_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act
        var act = () => services.AddEncinaEncryption();

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddEncinaEncryption_Generic_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act
        var act = () => services.AddEncinaEncryption<InMemoryKeyProvider>();

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("services");
    }

    #endregion

    #region EncryptionHealthCheck Guard Tests

    [Fact]
    public void EncryptionHealthCheck_Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new EncryptionHealthCheck(null!);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("serviceProvider");
    }

    #endregion

    #region Test Types

    public sealed class TestCommand : ICommand<Unit> { }

    #endregion
}
