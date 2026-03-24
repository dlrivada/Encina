using System.Reflection;
using Encina.Messaging.Encryption.AwsKms;
using Encina.Messaging.Encryption.AzureKeyVault;
using Encina.Security.Encryption.Abstractions;

namespace Encina.ContractTests.Messaging.Encryption;

/// <summary>
/// Contract tests verifying that all <see cref="IKeyProvider"/> implementations
/// (<see cref="AzureKeyVaultKeyProvider"/> and <see cref="AwsKmsKeyProvider"/>)
/// correctly satisfy the interface contract for key management operations.
/// Tests focus on interface shape, implementation conformance, and unconfigured behavior.
/// </summary>
[Trait("Category", "Contract")]
public sealed class KeyProviderContractTests
{
    #region IKeyProvider Interface Shape

    /// <summary>
    /// Contract: <see cref="IKeyProvider"/> must define exactly 3 methods.
    /// </summary>
    [Fact]
    public void Contract_IKeyProvider_ShouldHave_ThreeMethods()
    {
        // Arrange
        var type = typeof(IKeyProvider);

        // Act
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        // Assert
        methods.Length.ShouldBe(3,
            "IKeyProvider must define exactly 3 methods: GetKeyAsync, GetCurrentKeyIdAsync, RotateKeyAsync");
    }

    /// <summary>
    /// Contract: <see cref="IKeyProvider.GetKeyAsync"/> must accept string keyId and CancellationToken.
    /// </summary>
    [Fact]
    public void Contract_IKeyProvider_GetKeyAsync_ParameterSignature()
    {
        // Arrange
        var method = typeof(IKeyProvider).GetMethod("GetKeyAsync");

        // Assert
        method.ShouldNotBeNull("GetKeyAsync must exist on IKeyProvider");

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(2,
            "GetKeyAsync must accept (string keyId, CancellationToken)");

        parameters[0].ParameterType.ShouldBe(typeof(string));
        parameters[0].Name.ShouldBe("keyId");

        parameters[1].ParameterType.ShouldBe(typeof(CancellationToken));
        parameters[1].HasDefaultValue.ShouldBeTrue(
            "CancellationToken parameter must have a default value");
    }

    /// <summary>
    /// Contract: <see cref="IKeyProvider.GetCurrentKeyIdAsync"/> must accept only CancellationToken.
    /// </summary>
    [Fact]
    public void Contract_IKeyProvider_GetCurrentKeyIdAsync_ParameterSignature()
    {
        // Arrange
        var method = typeof(IKeyProvider).GetMethod("GetCurrentKeyIdAsync");

        // Assert
        method.ShouldNotBeNull("GetCurrentKeyIdAsync must exist on IKeyProvider");

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(1,
            "GetCurrentKeyIdAsync must accept (CancellationToken)");

        parameters[0].ParameterType.ShouldBe(typeof(CancellationToken));
        parameters[0].HasDefaultValue.ShouldBeTrue();
    }

    /// <summary>
    /// Contract: <see cref="IKeyProvider.RotateKeyAsync"/> must accept only CancellationToken.
    /// </summary>
    [Fact]
    public void Contract_IKeyProvider_RotateKeyAsync_ParameterSignature()
    {
        // Arrange
        var method = typeof(IKeyProvider).GetMethod("RotateKeyAsync");

        // Assert
        method.ShouldNotBeNull("RotateKeyAsync must exist on IKeyProvider");

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(1,
            "RotateKeyAsync must accept (CancellationToken)");

        parameters[0].ParameterType.ShouldBe(typeof(CancellationToken));
        parameters[0].HasDefaultValue.ShouldBeTrue();
    }

    /// <summary>
    /// Contract: All <see cref="IKeyProvider"/> methods must return <c>ValueTask&lt;Either&lt;EncinaError, T&gt;&gt;</c>
    /// following the Railway Oriented Programming pattern.
    /// </summary>
    [Theory]
    [InlineData("GetKeyAsync")]
    [InlineData("GetCurrentKeyIdAsync")]
    [InlineData("RotateKeyAsync")]
    public void Contract_IKeyProvider_AllMethods_ReturnValueTaskEither(string methodName)
    {
        // Arrange
        var method = typeof(IKeyProvider).GetMethod(methodName)!;
        var returnType = method.ReturnType;

        // Assert
        returnType.IsGenericType.ShouldBeTrue(
            $"{methodName} must return a generic type");
        returnType.GetGenericTypeDefinition().ShouldBe(typeof(ValueTask<>),
            $"{methodName} must return ValueTask<>");

        var innerType = returnType.GetGenericArguments()[0];
        innerType.IsGenericType.ShouldBeTrue(
            $"{methodName} must return ValueTask<Either<EncinaError, T>>");

        var eitherArgs = innerType.GetGenericArguments();
        eitherArgs[0].ShouldBe(typeof(EncinaError),
            $"{methodName} Left type must be EncinaError");
    }

    #endregion

    #region Implementation Conformance

    /// <summary>
    /// Contract: <see cref="AzureKeyVaultKeyProvider"/> must implement <see cref="IKeyProvider"/>.
    /// </summary>
    [Fact]
    public void Contract_AzureKeyVaultKeyProvider_ImplementsIKeyProvider()
    {
        typeof(IKeyProvider)
            .IsAssignableFrom(typeof(AzureKeyVaultKeyProvider))
            .ShouldBeTrue("AzureKeyVaultKeyProvider must implement IKeyProvider");
    }

    /// <summary>
    /// Contract: <see cref="AwsKmsKeyProvider"/> must implement <see cref="IKeyProvider"/>.
    /// </summary>
    [Fact]
    public void Contract_AwsKmsKeyProvider_ImplementsIKeyProvider()
    {
        typeof(IKeyProvider)
            .IsAssignableFrom(typeof(AwsKmsKeyProvider))
            .ShouldBeTrue("AwsKmsKeyProvider must implement IKeyProvider");
    }

    /// <summary>
    /// Contract: Both key providers must be sealed to prevent unintended inheritance.
    /// </summary>
    [Theory]
    [InlineData(typeof(AzureKeyVaultKeyProvider))]
    [InlineData(typeof(AwsKmsKeyProvider))]
    public void Contract_AllKeyProviders_AreSealed(Type providerType)
    {
        providerType.IsSealed.ShouldBeTrue(
            $"{providerType.Name} must be sealed");
    }

    #endregion

    #region Unconfigured Behavior Contract

    /// <summary>
    /// Contract: <see cref="AwsKmsKeyProvider.GetCurrentKeyIdAsync"/> must return Left
    /// when KeyId is not configured in <see cref="AwsKmsOptions"/>.
    /// </summary>
    [Fact]
    public async Task Contract_AwsKms_GetCurrentKeyIdAsync_UnconfiguredKeyId_ReturnsLeft()
    {
        // Arrange
        var kmsClient = NSubstitute.Substitute.For<Amazon.KeyManagementService.IAmazonKeyManagementService>();
        var options = Microsoft.Extensions.Options.Options.Create(new AwsKmsOptions { KeyId = null });
        var logger = NSubstitute.Substitute.For<Microsoft.Extensions.Logging.ILogger<AwsKmsKeyProvider>>();
        var provider = new AwsKmsKeyProvider(kmsClient, options, logger);

        // Act
        var result = await provider.GetCurrentKeyIdAsync();

        // Assert
        result.IsLeft.ShouldBeTrue(
            "GetCurrentKeyIdAsync must return Left when KeyId is not configured");
    }

    /// <summary>
    /// Contract: <see cref="AwsKmsKeyProvider.RotateKeyAsync"/> must return Left
    /// when KeyId is not configured in <see cref="AwsKmsOptions"/>.
    /// </summary>
    [Fact]
    public async Task Contract_AwsKms_RotateKeyAsync_UnconfiguredKeyId_ReturnsLeft()
    {
        // Arrange
        var kmsClient = NSubstitute.Substitute.For<Amazon.KeyManagementService.IAmazonKeyManagementService>();
        var options = Microsoft.Extensions.Options.Options.Create(new AwsKmsOptions { KeyId = null });
        var logger = NSubstitute.Substitute.For<Microsoft.Extensions.Logging.ILogger<AwsKmsKeyProvider>>();
        var provider = new AwsKmsKeyProvider(kmsClient, options, logger);

        // Act
        var result = await provider.RotateKeyAsync();

        // Assert
        result.IsLeft.ShouldBeTrue(
            "RotateKeyAsync must return Left when KeyId is not configured");
    }

    /// <summary>
    /// Contract: <see cref="AzureKeyVaultKeyProvider.GetCurrentKeyIdAsync"/> must return Left
    /// when KeyName is not configured in <see cref="AzureKeyVaultOptions"/>.
    /// </summary>
    [Fact]
    public async Task Contract_AzureKeyVault_GetCurrentKeyIdAsync_UnconfiguredKeyName_ReturnsLeft()
    {
        // Arrange
        var keyClient = NSubstitute.Substitute.For<Azure.Security.KeyVault.Keys.KeyClient>();
        var options = Microsoft.Extensions.Options.Options.Create(new AzureKeyVaultOptions { KeyName = null });
        var logger = NSubstitute.Substitute.For<Microsoft.Extensions.Logging.ILogger<AzureKeyVaultKeyProvider>>();
        var provider = new AzureKeyVaultKeyProvider(keyClient, options, logger);

        // Act
        var result = await provider.GetCurrentKeyIdAsync();

        // Assert
        result.IsLeft.ShouldBeTrue(
            "GetCurrentKeyIdAsync must return Left when KeyName is not configured");
    }

    /// <summary>
    /// Contract: <see cref="AzureKeyVaultKeyProvider.RotateKeyAsync"/> must return Left
    /// when KeyName is not configured in <see cref="AzureKeyVaultOptions"/>.
    /// </summary>
    [Fact]
    public async Task Contract_AzureKeyVault_RotateKeyAsync_UnconfiguredKeyName_ReturnsLeft()
    {
        // Arrange
        var keyClient = NSubstitute.Substitute.For<Azure.Security.KeyVault.Keys.KeyClient>();
        var options = Microsoft.Extensions.Options.Options.Create(new AzureKeyVaultOptions { KeyName = null });
        var logger = NSubstitute.Substitute.For<Microsoft.Extensions.Logging.ILogger<AzureKeyVaultKeyProvider>>();
        var provider = new AzureKeyVaultKeyProvider(keyClient, options, logger);

        // Act
        var result = await provider.RotateKeyAsync();

        // Assert
        result.IsLeft.ShouldBeTrue(
            "RotateKeyAsync must return Left when KeyName is not configured");
    }

    #endregion
}
