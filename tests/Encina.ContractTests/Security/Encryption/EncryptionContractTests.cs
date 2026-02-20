using System.Collections.Immutable;
using System.Reflection;
using Encina.Security.Encryption;
using Encina.Security.Encryption.Abstractions;

namespace Encina.ContractTests.Security.Encryption;

/// <summary>
/// Contract tests for the Encina.Security.Encryption public surface area.
/// Verifies that interface shapes, data type structures, and error code conventions remain stable.
/// </summary>
[Trait("Category", "Contract")]
public sealed class EncryptionContractTests
{
    #region IFieldEncryptor Interface Shape

    [Fact]
    public void IFieldEncryptor_ShouldHave_FourMethods()
    {
        // Arrange
        var type = typeof(IFieldEncryptor);

        // Act
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        // Assert
        methods.Length.ShouldBe(4,
            "IFieldEncryptor must define exactly 4 methods: " +
            "EncryptStringAsync, DecryptStringAsync, EncryptBytesAsync, DecryptBytesAsync");
    }

    [Fact]
    public void IFieldEncryptor_AllMethods_ShouldReturn_ValueTaskEither()
    {
        // Arrange
        var type = typeof(IFieldEncryptor);
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        // Act & Assert
        foreach (var method in methods)
        {
            var returnType = method.ReturnType;

            returnType.IsGenericType.ShouldBeTrue(
                $"Method '{method.Name}' must return a generic type (ValueTask<Either<...>>)");

            var outerGenericDef = returnType.GetGenericTypeDefinition();
            outerGenericDef.ShouldBe(typeof(ValueTask<>),
                $"Method '{method.Name}' must return ValueTask<T>, got {returnType.Name}");

            var innerType = returnType.GetGenericArguments()[0];
            innerType.IsGenericType.ShouldBeTrue(
                $"Method '{method.Name}' inner type must be generic (Either<EncinaError, T>)");

            var fullName = innerType.GetGenericTypeDefinition().FullName ?? string.Empty;
            (fullName.StartsWith("LanguageExt.Either", StringComparison.Ordinal)).ShouldBeTrue(
                $"Method '{method.Name}' must return ValueTask<Either<EncinaError, T>>, got {innerType.Name}");

            var eitherArgs = innerType.GetGenericArguments();
            eitherArgs[0].ShouldBe(typeof(EncinaError),
                $"Method '{method.Name}' Either left type must be EncinaError");
        }
    }

    [Fact]
    public void IFieldEncryptor_EncryptStringAsync_ShouldAccept_StringContextCancellationToken()
    {
        // Arrange
        var method = typeof(IFieldEncryptor).GetMethod("EncryptStringAsync");

        // Assert
        method.ShouldNotBeNull("EncryptStringAsync must exist on IFieldEncryptor");

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(3,
            "EncryptStringAsync must accept (string plaintext, EncryptionContext context, CancellationToken cancellationToken)");

        parameters[0].ParameterType.ShouldBe(typeof(string));
        parameters[0].Name.ShouldBe("plaintext");

        parameters[1].ParameterType.ShouldBe(typeof(EncryptionContext));
        parameters[1].Name.ShouldBe("context");

        parameters[2].ParameterType.ShouldBe(typeof(CancellationToken));
        parameters[2].HasDefaultValue.ShouldBeTrue();
    }

    [Fact]
    public void IFieldEncryptor_DecryptStringAsync_ShouldAccept_EncryptedValueContextCancellationToken()
    {
        // Arrange
        var method = typeof(IFieldEncryptor).GetMethod("DecryptStringAsync");

        // Assert
        method.ShouldNotBeNull("DecryptStringAsync must exist on IFieldEncryptor");

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(3);

        parameters[0].ParameterType.ShouldBe(typeof(EncryptedValue));
        parameters[0].Name.ShouldBe("encryptedValue");

        parameters[1].ParameterType.ShouldBe(typeof(EncryptionContext));
        parameters[1].Name.ShouldBe("context");

        parameters[2].ParameterType.ShouldBe(typeof(CancellationToken));
        parameters[2].HasDefaultValue.ShouldBeTrue();
    }

    #endregion

    #region IKeyProvider Interface Shape

    [Fact]
    public void IKeyProvider_ShouldHave_ThreeMethods()
    {
        // Arrange
        var type = typeof(IKeyProvider);

        // Act
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        // Assert
        methods.Length.ShouldBe(3,
            "IKeyProvider must define exactly 3 methods: " +
            "GetKeyAsync, GetCurrentKeyIdAsync, RotateKeyAsync");
    }

    [Fact]
    public void IKeyProvider_AllMethods_ShouldReturn_ValueTaskEither()
    {
        // Arrange
        var type = typeof(IKeyProvider);
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        // Act & Assert
        foreach (var method in methods)
        {
            var returnType = method.ReturnType;

            returnType.IsGenericType.ShouldBeTrue(
                $"Method '{method.Name}' must return a generic type");

            var outerGenericDef = returnType.GetGenericTypeDefinition();
            outerGenericDef.ShouldBe(typeof(ValueTask<>),
                $"Method '{method.Name}' must return ValueTask<T>");

            var innerType = returnType.GetGenericArguments()[0];
            innerType.IsGenericType.ShouldBeTrue(
                $"Method '{method.Name}' inner type must be generic");

            var eitherArgs = innerType.GetGenericArguments();
            eitherArgs[0].ShouldBe(typeof(EncinaError),
                $"Method '{method.Name}' Either left type must be EncinaError");
        }
    }

    [Fact]
    public void IKeyProvider_GetKeyAsync_ShouldAccept_StringAndCancellationToken()
    {
        // Arrange
        var method = typeof(IKeyProvider).GetMethod("GetKeyAsync");

        // Assert
        method.ShouldNotBeNull("GetKeyAsync must exist on IKeyProvider");

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(2);

        parameters[0].ParameterType.ShouldBe(typeof(string));
        parameters[0].Name.ShouldBe("keyId");

        parameters[1].ParameterType.ShouldBe(typeof(CancellationToken));
        parameters[1].HasDefaultValue.ShouldBeTrue();
    }

    [Fact]
    public void IKeyProvider_GetCurrentKeyIdAsync_ShouldAccept_OnlyCancellationToken()
    {
        // Arrange
        var method = typeof(IKeyProvider).GetMethod("GetCurrentKeyIdAsync");

        // Assert
        method.ShouldNotBeNull("GetCurrentKeyIdAsync must exist on IKeyProvider");

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(1);

        parameters[0].ParameterType.ShouldBe(typeof(CancellationToken));
        parameters[0].HasDefaultValue.ShouldBeTrue();
    }

    [Fact]
    public void IKeyProvider_RotateKeyAsync_ShouldAccept_OnlyCancellationToken()
    {
        // Arrange
        var method = typeof(IKeyProvider).GetMethod("RotateKeyAsync");

        // Assert
        method.ShouldNotBeNull("RotateKeyAsync must exist on IKeyProvider");

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(1);

        parameters[0].ParameterType.ShouldBe(typeof(CancellationToken));
        parameters[0].HasDefaultValue.ShouldBeTrue();
    }

    #endregion

    #region IEncryptionOrchestrator Interface Shape

    [Fact]
    public void IEncryptionOrchestrator_ShouldHave_TwoMethods()
    {
        // Arrange
        var type = typeof(IEncryptionOrchestrator);

        // Act
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        // Assert
        methods.Length.ShouldBe(2,
            "IEncryptionOrchestrator must define exactly 2 methods: EncryptAsync, DecryptAsync");
    }

    [Fact]
    public void IEncryptionOrchestrator_EncryptAsync_ShouldBeGeneric()
    {
        // Arrange
        var method = typeof(IEncryptionOrchestrator).GetMethod("EncryptAsync");

        // Assert
        method.ShouldNotBeNull("EncryptAsync must exist on IEncryptionOrchestrator");
        method.IsGenericMethod.ShouldBeTrue("EncryptAsync must be a generic method");
        method.GetGenericArguments().Length.ShouldBe(1, "EncryptAsync must have one generic type parameter");

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(3);
        parameters[1].ParameterType.ShouldBe(typeof(IRequestContext));
        parameters[1].Name.ShouldBe("context");
        parameters[2].ParameterType.ShouldBe(typeof(CancellationToken));
    }

    #endregion

    #region EncryptedValue Data Type Contract

    [Fact]
    public void EncryptedValue_ShouldHave_FiveProperties()
    {
        // Arrange
        var type = typeof(EncryptedValue);

        // Assert
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        properties.Length.ShouldBe(5,
            "EncryptedValue must have exactly 5 properties: " +
            "Algorithm, KeyId, Nonce, Tag, Ciphertext");

        var propertyNames = properties.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        propertyNames.ShouldContain("Algorithm");
        propertyNames.ShouldContain("KeyId");
        propertyNames.ShouldContain("Nonce");
        propertyNames.ShouldContain("Tag");
        propertyNames.ShouldContain("Ciphertext");
    }

    [Fact]
    public void EncryptedValue_PropertyTypes_ShouldBeCorrect()
    {
        // Arrange
        var type = typeof(EncryptedValue);

        // Assert
        type.GetProperty("Algorithm")!.PropertyType.ShouldBe(typeof(EncryptionAlgorithm));
        type.GetProperty("KeyId")!.PropertyType.ShouldBe(typeof(string));
        type.GetProperty("Nonce")!.PropertyType.ShouldBe(typeof(ImmutableArray<byte>));
        type.GetProperty("Tag")!.PropertyType.ShouldBe(typeof(ImmutableArray<byte>));
        type.GetProperty("Ciphertext")!.PropertyType.ShouldBe(typeof(ImmutableArray<byte>));
    }

    [Fact]
    public void EncryptedValue_ShouldBeRecordStruct()
    {
        // Arrange
        var type = typeof(EncryptedValue);

        // Assert
        type.IsValueType.ShouldBeTrue("EncryptedValue must be a value type (record struct)");
    }

    #endregion

    #region EncryptionContext Data Type Contract

    [Fact]
    public void EncryptionContext_ShouldHave_FourProperties()
    {
        // Arrange
        var type = typeof(EncryptionContext);

        // Assert
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        properties.Length.ShouldBe(4,
            "EncryptionContext must have exactly 4 properties: " +
            "KeyId, Purpose, TenantId, AssociatedData");

        var propertyNames = properties.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        propertyNames.ShouldContain("KeyId");
        propertyNames.ShouldContain("Purpose");
        propertyNames.ShouldContain("TenantId");
        propertyNames.ShouldContain("AssociatedData");
    }

    [Fact]
    public void EncryptionContext_ShouldBeSealedRecord()
    {
        // Arrange
        var type = typeof(EncryptionContext);

        // Assert
        type.IsClass.ShouldBeTrue("EncryptionContext must be a reference type (sealed record)");
        type.IsSealed.ShouldBeTrue("EncryptionContext must be sealed");

        // Verify it implements IEquatable<T> (record type marker)
        var implementsEquatable = type.GetInterfaces()
            .Any(i => i == typeof(IEquatable<EncryptionContext>));
        implementsEquatable.ShouldBeTrue("EncryptionContext must implement IEquatable<EncryptionContext> (record type)");
    }

    #endregion

    #region EncryptionAlgorithm Enum Contract

    [Fact]
    public void EncryptionAlgorithm_ShouldHave_Aes256Gcm()
    {
        // Assert
        Enum.IsDefined(typeof(EncryptionAlgorithm), 0).ShouldBeTrue(
            "EncryptionAlgorithm must define value 0 (Aes256Gcm)");

        var name = Enum.GetName(typeof(EncryptionAlgorithm), 0);
        name.ShouldBe("Aes256Gcm");
    }

    #endregion

    #region EncryptionErrors Contract

    [Fact]
    public void EncryptionErrors_ShouldDefine_FiveErrorCodes()
    {
        // Arrange
        var type = typeof(EncryptionErrors);

        // Act
        var constants = type
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .ToList();

        // Assert
        constants.Count.ShouldBe(5,
            "EncryptionErrors must define exactly 5 error code constants: " +
            "KeyNotFoundCode, DecryptionFailedCode, InvalidCiphertextCode, " +
            "AlgorithmNotSupportedCode, KeyRotationFailedCode");

        var names = constants.Select(f => f.Name).ToHashSet(StringComparer.Ordinal);
        names.ShouldContain(nameof(EncryptionErrors.KeyNotFoundCode));
        names.ShouldContain(nameof(EncryptionErrors.DecryptionFailedCode));
        names.ShouldContain(nameof(EncryptionErrors.InvalidCiphertextCode));
        names.ShouldContain(nameof(EncryptionErrors.AlgorithmNotSupportedCode));
        names.ShouldContain(nameof(EncryptionErrors.KeyRotationFailedCode));
    }

    [Fact]
    public void AllErrorCodes_ShouldStartWith_EncryptionPrefix()
    {
        // Arrange
        const string expectedPrefix = "encryption.";
        var type = typeof(EncryptionErrors);

        var constants = type
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .ToList();

        // Assert
        constants.ShouldNotBeEmpty("EncryptionErrors must define at least one error code constant");

        foreach (var constant in constants)
        {
            var value = (string?)constant.GetRawConstantValue();
            value.ShouldNotBeNull($"Error code constant '{constant.Name}' must not be null");
            (value!.StartsWith(expectedPrefix, StringComparison.Ordinal)).ShouldBeTrue(
                $"Error code '{constant.Name}' = '{value}' must start with '{expectedPrefix}'");
        }
    }

    #endregion

    #region Attribute Contracts

    [Fact]
    public void EncryptAttribute_ShouldInherit_EncryptionAttribute()
    {
        // Assert
        typeof(EncryptAttribute).IsSubclassOf(typeof(EncryptionAttribute)).ShouldBeTrue(
            "EncryptAttribute must inherit from EncryptionAttribute");
    }

    [Fact]
    public void EncryptAttribute_ShouldHave_PurposeAndKeyId()
    {
        // Arrange
        var type = typeof(EncryptAttribute);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        // Assert
        var propertyNames = properties.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        propertyNames.ShouldContain("Purpose", "EncryptAttribute must have a Purpose property");
        propertyNames.ShouldContain("KeyId", "EncryptAttribute must have a KeyId property");
    }

    [Fact]
    public void EncryptionAttribute_ShouldHave_AlgorithmAndFailOnError()
    {
        // Arrange
        var type = typeof(EncryptionAttribute);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        // Assert
        var propertyNames = properties.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        propertyNames.ShouldContain("Algorithm", "EncryptionAttribute must have an Algorithm property");
        propertyNames.ShouldContain("FailOnError", "EncryptionAttribute must have a FailOnError property");
    }

    [Fact]
    public void EncryptedResponseAttribute_ShouldInherit_Attribute()
    {
        // Assert
        typeof(EncryptedResponseAttribute).IsSubclassOf(typeof(Attribute)).ShouldBeTrue(
            "EncryptedResponseAttribute must inherit from System.Attribute");
    }

    [Fact]
    public void DecryptOnReceiveAttribute_ShouldInherit_Attribute()
    {
        // Assert
        typeof(DecryptOnReceiveAttribute).IsSubclassOf(typeof(Attribute)).ShouldBeTrue(
            "DecryptOnReceiveAttribute must inherit from System.Attribute");
    }

    #endregion

    #region InMemoryKeyProvider Contract

    [Fact]
    public void InMemoryKeyProvider_ShouldImplement_IKeyProvider()
    {
        // Assert
        typeof(IKeyProvider).IsAssignableFrom(typeof(InMemoryKeyProvider)).ShouldBeTrue(
            "InMemoryKeyProvider must implement IKeyProvider");
    }

    [Fact]
    public void InMemoryKeyProvider_ShouldHave_ManagementMethods()
    {
        // Arrange
        var type = typeof(InMemoryKeyProvider);

        // Assert
        type.GetMethod("AddKey").ShouldNotBeNull("InMemoryKeyProvider must have AddKey method");
        type.GetMethod("SetCurrentKey").ShouldNotBeNull("InMemoryKeyProvider must have SetCurrentKey method");
        type.GetMethod("Clear").ShouldNotBeNull("InMemoryKeyProvider must have Clear method");

        var countProp = type.GetProperty("Count");
        countProp.ShouldNotBeNull("InMemoryKeyProvider must have Count property");
        countProp!.PropertyType.ShouldBe(typeof(int));
    }

    #endregion

    #region EncryptionOptions Contract

    [Fact]
    public void EncryptionOptions_ShouldHave_ExpectedProperties()
    {
        // Arrange
        var type = typeof(EncryptionOptions);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Assert
        var propertyNames = properties.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);

        propertyNames.ShouldContain("DefaultAlgorithm");
        propertyNames.ShouldContain("FailOnDecryptionError");
        propertyNames.ShouldContain("EnableTracing");
        propertyNames.ShouldContain("EnableMetrics");
        propertyNames.ShouldContain("AddHealthCheck");
    }

    [Fact]
    public void EncryptionOptions_Defaults_ShouldBeStable()
    {
        // Arrange
        var options = new EncryptionOptions();

        // Assert
        options.DefaultAlgorithm.ShouldBe(EncryptionAlgorithm.Aes256Gcm);
        options.FailOnDecryptionError.ShouldBeTrue();
        options.EnableTracing.ShouldBeFalse();
        options.EnableMetrics.ShouldBeFalse();
        options.AddHealthCheck.ShouldBeFalse();
    }

    #endregion
}
