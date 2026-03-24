using Encina.Messaging.Encryption.AwsKms;

namespace Encina.GuardTests.Messaging.Encryption.AwsKms;

/// <summary>
/// Guard clause tests for AWS KMS <see cref="ServiceCollectionExtensions"/>.
/// Verifies that null parameters are properly guarded.
/// </summary>
public sealed class ServiceCollectionExtensionsAwsKmsGuardTests
{
    #region AddEncinaMessageEncryptionAwsKms Guards

    /// <summary>
    /// Verifies that AddEncinaMessageEncryptionAwsKms throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void AddEncinaMessageEncryptionAwsKms_NullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act
        var act = () => services.AddEncinaMessageEncryptionAwsKms(_ => { });

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("services");
    }

    /// <summary>
    /// Verifies that AddEncinaMessageEncryptionAwsKms throws ArgumentNullException when configure is null.
    /// </summary>
    [Fact]
    public void AddEncinaMessageEncryptionAwsKms_NullConfigure_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddEncinaMessageEncryptionAwsKms(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("configure");
    }

    #endregion
}
