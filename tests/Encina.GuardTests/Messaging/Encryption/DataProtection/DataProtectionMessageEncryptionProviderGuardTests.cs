using Encina.Messaging.Encryption.DataProtection;
using Encina.Messaging.Encryption.Abstractions;
using Encina.Messaging.Encryption.Model;
using Microsoft.AspNetCore.DataProtection;
using System.Collections.Immutable;

namespace Encina.GuardTests.Messaging.Encryption.DataProtection;

/// <summary>
/// Guard clause tests for <see cref="DataProtectionMessageEncryptionProvider"/>.
/// Verifies that null parameters are properly guarded.
/// </summary>
public sealed class DataProtectionMessageEncryptionProviderGuardTests
{
    #region Constructor Guards

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when dataProtectionProvider is null.
    /// </summary>
    [Fact]
    public void Constructor_NullDataProtectionProvider_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new DataProtectionEncryptionOptions());
        var logger = NullLogger<DataProtectionMessageEncryptionProvider>.Instance;

        // Act
        var act = () => new DataProtectionMessageEncryptionProvider(null!, options, logger);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("dataProtectionProvider");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when options is null.
    /// </summary>
    [Fact]
    public void Constructor_NullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange
        var dataProtectionProvider = Substitute.For<IDataProtectionProvider>();
        dataProtectionProvider.CreateProtector(Arg.Any<string>())
            .Returns(Substitute.For<IDataProtector>());
        var logger = NullLogger<DataProtectionMessageEncryptionProvider>.Instance;

        // Act
        var act = () => new DataProtectionMessageEncryptionProvider(dataProtectionProvider, null!, logger);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var dataProtectionProvider = Substitute.For<IDataProtectionProvider>();
        dataProtectionProvider.CreateProtector(Arg.Any<string>())
            .Returns(Substitute.For<IDataProtector>());
        var options = Options.Create(new DataProtectionEncryptionOptions());

        // Act
        var act = () => new DataProtectionMessageEncryptionProvider(dataProtectionProvider, options, null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    #endregion

    #region EncryptAsync Guards

    /// <summary>
    /// Verifies that EncryptAsync throws ArgumentNullException when context is null.
    /// </summary>
    [Fact]
    public async Task EncryptAsync_NullContext_ShouldThrowArgumentNullException()
    {
        // Arrange
        var dataProtectionProvider = Substitute.For<IDataProtectionProvider>();
        dataProtectionProvider.CreateProtector(Arg.Any<string>())
            .Returns(Substitute.For<IDataProtector>());
        var options = Options.Create(new DataProtectionEncryptionOptions());
        var logger = NullLogger<DataProtectionMessageEncryptionProvider>.Instance;
        var provider = new DataProtectionMessageEncryptionProvider(dataProtectionProvider, options, logger);

        // Act
        var act = async () => await provider.EncryptAsync(
            new ReadOnlyMemory<byte>([1, 2, 3]),
            null!);

        // Assert
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    #endregion

    #region DecryptAsync Guards

    /// <summary>
    /// Verifies that DecryptAsync throws ArgumentNullException when payload is null.
    /// </summary>
    [Fact]
    public async Task DecryptAsync_NullPayload_ShouldThrowArgumentNullException()
    {
        // Arrange
        var dataProtectionProvider = Substitute.For<IDataProtectionProvider>();
        dataProtectionProvider.CreateProtector(Arg.Any<string>())
            .Returns(Substitute.For<IDataProtector>());
        var options = Options.Create(new DataProtectionEncryptionOptions());
        var logger = NullLogger<DataProtectionMessageEncryptionProvider>.Instance;
        var provider = new DataProtectionMessageEncryptionProvider(dataProtectionProvider, options, logger);
        var context = new MessageEncryptionContext();

        // Act
        var act = async () => await provider.DecryptAsync(null!, context);

        // Assert
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    /// <summary>
    /// Verifies that DecryptAsync throws ArgumentNullException when context is null.
    /// </summary>
    [Fact]
    public async Task DecryptAsync_NullContext_ShouldThrowArgumentNullException()
    {
        // Arrange
        var dataProtectionProvider = Substitute.For<IDataProtectionProvider>();
        dataProtectionProvider.CreateProtector(Arg.Any<string>())
            .Returns(Substitute.For<IDataProtector>());
        var options = Options.Create(new DataProtectionEncryptionOptions());
        var logger = NullLogger<DataProtectionMessageEncryptionProvider>.Instance;
        var provider = new DataProtectionMessageEncryptionProvider(dataProtectionProvider, options, logger);
        var payload = new EncryptedPayload
        {
            Ciphertext = ImmutableArray<byte>.Empty,
            KeyId = "test",
            Algorithm = "test",
            Nonce = ImmutableArray<byte>.Empty,
            Tag = ImmutableArray<byte>.Empty,
            Version = 1
        };

        // Act
        var act = async () => await provider.DecryptAsync(payload, null!);

        // Assert
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    #endregion
}
