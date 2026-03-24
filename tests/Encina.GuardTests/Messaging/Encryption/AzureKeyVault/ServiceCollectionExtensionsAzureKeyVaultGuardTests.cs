using Encina.Messaging.Encryption.AzureKeyVault;

namespace Encina.GuardTests.Messaging.Encryption.AzureKeyVault;

/// <summary>
/// Guard clause tests for Azure Key Vault <see cref="ServiceCollectionExtensions"/>.
/// Verifies that null parameters are properly guarded.
/// </summary>
public sealed class ServiceCollectionExtensionsAzureKeyVaultGuardTests
{
    #region AddEncinaMessageEncryptionAzureKeyVault Guards

    /// <summary>
    /// Verifies that AddEncinaMessageEncryptionAzureKeyVault throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void AddEncinaMessageEncryptionAzureKeyVault_NullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act
        var act = () => services.AddEncinaMessageEncryptionAzureKeyVault(_ => { });

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("services");
    }

    /// <summary>
    /// Verifies that AddEncinaMessageEncryptionAzureKeyVault throws ArgumentNullException when configure is null.
    /// </summary>
    [Fact]
    public void AddEncinaMessageEncryptionAzureKeyVault_NullConfigure_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddEncinaMessageEncryptionAzureKeyVault(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("configure");
    }

    #endregion
}
