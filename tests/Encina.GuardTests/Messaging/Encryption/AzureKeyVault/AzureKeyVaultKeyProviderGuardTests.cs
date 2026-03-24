using Azure.Security.KeyVault.Keys;
using Encina.Messaging.Encryption.AzureKeyVault;

namespace Encina.GuardTests.Messaging.Encryption.AzureKeyVault;

/// <summary>
/// Guard clause tests for <see cref="AzureKeyVaultKeyProvider"/>.
/// Verifies that null/empty/whitespace parameters are properly guarded.
/// </summary>
public sealed class AzureKeyVaultKeyProviderGuardTests
{
    #region Constructor Guards

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when keyClient is null.
    /// </summary>
    [Fact]
    public void Constructor_NullKeyClient_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new AzureKeyVaultOptions());
        var logger = NullLogger<AzureKeyVaultKeyProvider>.Instance;

        // Act
        var act = () => new AzureKeyVaultKeyProvider(null!, options, logger);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("keyClient");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when options is null.
    /// </summary>
    [Fact]
    public void Constructor_NullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange
        var keyClient = new KeyClient(new Uri("https://test.vault.azure.net"), new Azure.Identity.DefaultAzureCredential());
        var logger = NullLogger<AzureKeyVaultKeyProvider>.Instance;

        // Act
        var act = () => new AzureKeyVaultKeyProvider(keyClient, null!, logger);

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
        var keyClient = new KeyClient(new Uri("https://test.vault.azure.net"), new Azure.Identity.DefaultAzureCredential());
        var options = Options.Create(new AzureKeyVaultOptions());

        // Act
        var act = () => new AzureKeyVaultKeyProvider(keyClient, options, null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    #endregion

    #region GetKeyAsync Guards

    /// <summary>
    /// Verifies that GetKeyAsync throws ArgumentNullException when keyId is null.
    /// </summary>
    [Fact]
    public async Task GetKeyAsync_NullKeyId_ShouldThrowArgumentNullException()
    {
        // Arrange
        var keyClient = new KeyClient(new Uri("https://test.vault.azure.net"), new Azure.Identity.DefaultAzureCredential());
        var options = Options.Create(new AzureKeyVaultOptions());
        var logger = NullLogger<AzureKeyVaultKeyProvider>.Instance;
        var provider = new AzureKeyVaultKeyProvider(keyClient, options, logger);

        // Act
        var act = async () => await provider.GetKeyAsync(null!);

        // Assert
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    /// <summary>
    /// Verifies that GetKeyAsync throws ArgumentException when keyId is empty.
    /// </summary>
    [Fact]
    public async Task GetKeyAsync_EmptyKeyId_ShouldThrowArgumentException()
    {
        // Arrange
        var keyClient = new KeyClient(new Uri("https://test.vault.azure.net"), new Azure.Identity.DefaultAzureCredential());
        var options = Options.Create(new AzureKeyVaultOptions());
        var logger = NullLogger<AzureKeyVaultKeyProvider>.Instance;
        var provider = new AzureKeyVaultKeyProvider(keyClient, options, logger);

        // Act
        var act = async () => await provider.GetKeyAsync("");

        // Assert
        await Should.ThrowAsync<ArgumentException>(act);
    }

    /// <summary>
    /// Verifies that GetKeyAsync throws ArgumentException when keyId is whitespace.
    /// </summary>
    [Fact]
    public async Task GetKeyAsync_WhitespaceKeyId_ShouldThrowArgumentException()
    {
        // Arrange
        var keyClient = new KeyClient(new Uri("https://test.vault.azure.net"), new Azure.Identity.DefaultAzureCredential());
        var options = Options.Create(new AzureKeyVaultOptions());
        var logger = NullLogger<AzureKeyVaultKeyProvider>.Instance;
        var provider = new AzureKeyVaultKeyProvider(keyClient, options, logger);

        // Act
        var act = async () => await provider.GetKeyAsync("   ");

        // Assert
        await Should.ThrowAsync<ArgumentException>(act);
    }

    #endregion
}
