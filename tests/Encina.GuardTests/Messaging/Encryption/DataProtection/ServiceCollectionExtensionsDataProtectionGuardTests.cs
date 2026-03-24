using Encina.Messaging.Encryption.DataProtection;

namespace Encina.GuardTests.Messaging.Encryption.DataProtection;

/// <summary>
/// Guard clause tests for Data Protection <see cref="ServiceCollectionExtensions"/>.
/// Verifies that null parameters are properly guarded.
/// </summary>
public sealed class ServiceCollectionExtensionsDataProtectionGuardTests
{
    #region AddEncinaMessageEncryptionDataProtection Guards

    /// <summary>
    /// Verifies that AddEncinaMessageEncryptionDataProtection throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void AddEncinaMessageEncryptionDataProtection_NullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act
        var act = () => services.AddEncinaMessageEncryptionDataProtection();

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("services");
    }

    #endregion
}
