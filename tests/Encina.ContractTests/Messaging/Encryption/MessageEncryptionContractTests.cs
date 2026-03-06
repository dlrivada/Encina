using System.Collections.Immutable;
using System.Reflection;
using Encina.Messaging.Encryption;
using Encina.Messaging.Encryption.Abstractions;
using Encina.Messaging.Encryption.Attributes;
using Encina.Messaging.Encryption.Model;

namespace Encina.ContractTests.Messaging.Encryption;

/// <summary>
/// Contract tests for the Encina.Messaging.Encryption public surface area.
/// Verifies that interface shapes, data type structures, and error code conventions remain stable.
/// </summary>
[Trait("Category", "Contract")]
public sealed class MessageEncryptionContractTests
{
    #region IMessageEncryptionProvider Interface Shape

    [Fact]
    public void IMessageEncryptionProvider_ShouldHave_TwoMethods()
    {
        // Arrange
        var type = typeof(IMessageEncryptionProvider);

        // Act
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        // Assert
        methods.Length.ShouldBe(2,
            "IMessageEncryptionProvider must define exactly 2 methods: EncryptAsync, DecryptAsync");
    }

    [Fact]
    public void IMessageEncryptionProvider_EncryptAsync_ShouldAccept_ReadOnlyMemoryContextCancellationToken()
    {
        // Arrange
        var method = typeof(IMessageEncryptionProvider).GetMethod("EncryptAsync");

        // Assert
        method.ShouldNotBeNull("EncryptAsync must exist on IMessageEncryptionProvider");

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(3,
            "EncryptAsync must accept (ReadOnlyMemory<byte> plaintext, MessageEncryptionContext context, CancellationToken)");

        parameters[0].ParameterType.ShouldBe(typeof(ReadOnlyMemory<byte>));
        parameters[0].Name.ShouldBe("plaintext");

        parameters[1].ParameterType.ShouldBe(typeof(MessageEncryptionContext));
        parameters[1].Name.ShouldBe("context");

        parameters[2].ParameterType.ShouldBe(typeof(CancellationToken));
        parameters[2].HasDefaultValue.ShouldBeTrue();
    }

    [Fact]
    public void IMessageEncryptionProvider_EncryptAsync_ShouldReturn_ValueTaskEitherEncryptedPayload()
    {
        // Arrange
        var method = typeof(IMessageEncryptionProvider).GetMethod("EncryptAsync")!;
        var returnType = method.ReturnType;

        // Assert
        returnType.IsGenericType.ShouldBeTrue();
        returnType.GetGenericTypeDefinition().ShouldBe(typeof(ValueTask<>));

        var innerType = returnType.GetGenericArguments()[0];
        innerType.IsGenericType.ShouldBeTrue();

        var eitherArgs = innerType.GetGenericArguments();
        eitherArgs[0].ShouldBe(typeof(EncinaError));
        eitherArgs[1].ShouldBe(typeof(EncryptedPayload));
    }

    [Fact]
    public void IMessageEncryptionProvider_DecryptAsync_ShouldAccept_EncryptedPayloadContextCancellationToken()
    {
        // Arrange
        var method = typeof(IMessageEncryptionProvider).GetMethod("DecryptAsync");

        // Assert
        method.ShouldNotBeNull("DecryptAsync must exist on IMessageEncryptionProvider");

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(3);

        parameters[0].ParameterType.ShouldBe(typeof(EncryptedPayload));
        parameters[0].Name.ShouldBe("payload");

        parameters[1].ParameterType.ShouldBe(typeof(MessageEncryptionContext));
        parameters[1].Name.ShouldBe("context");

        parameters[2].ParameterType.ShouldBe(typeof(CancellationToken));
        parameters[2].HasDefaultValue.ShouldBeTrue();
    }

    [Fact]
    public void IMessageEncryptionProvider_DecryptAsync_ShouldReturn_ValueTaskEitherImmutableArrayByte()
    {
        // Arrange
        var method = typeof(IMessageEncryptionProvider).GetMethod("DecryptAsync")!;
        var returnType = method.ReturnType;

        // Assert
        returnType.IsGenericType.ShouldBeTrue();
        returnType.GetGenericTypeDefinition().ShouldBe(typeof(ValueTask<>));

        var innerType = returnType.GetGenericArguments()[0];
        innerType.IsGenericType.ShouldBeTrue();

        var eitherArgs = innerType.GetGenericArguments();
        eitherArgs[0].ShouldBe(typeof(EncinaError));
        eitherArgs[1].ShouldBe(typeof(ImmutableArray<byte>));
    }

    #endregion

    #region ITenantKeyResolver Interface Shape

    [Fact]
    public void ITenantKeyResolver_ShouldHave_OneMethod()
    {
        // Arrange
        var type = typeof(ITenantKeyResolver);

        // Act
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        // Assert
        methods.Length.ShouldBe(1,
            "ITenantKeyResolver must define exactly 1 method: ResolveKeyId");
    }

    [Fact]
    public void ITenantKeyResolver_ResolveKeyId_ShouldAccept_StringAndReturn_String()
    {
        // Arrange
        var method = typeof(ITenantKeyResolver).GetMethod("ResolveKeyId");

        // Assert
        method.ShouldNotBeNull("ResolveKeyId must exist on ITenantKeyResolver");
        method.ReturnType.ShouldBe(typeof(string));

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(1);
        parameters[0].ParameterType.ShouldBe(typeof(string));
        parameters[0].Name.ShouldBe("tenantId");
    }

    #endregion

    #region EncryptedPayload Data Type Contract

    [Fact]
    public void EncryptedPayload_ShouldHave_SixProperties()
    {
        // Arrange
        var type = typeof(EncryptedPayload);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Assert
        properties.Length.ShouldBe(6,
            "EncryptedPayload must have exactly 6 properties: " +
            "Ciphertext, KeyId, Algorithm, Nonce, Tag, Version");

        var names = properties.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        names.ShouldContain("Ciphertext");
        names.ShouldContain("KeyId");
        names.ShouldContain("Algorithm");
        names.ShouldContain("Nonce");
        names.ShouldContain("Tag");
        names.ShouldContain("Version");
    }

    [Fact]
    public void EncryptedPayload_PropertyTypes_ShouldBeCorrect()
    {
        // Arrange
        var type = typeof(EncryptedPayload);

        // Assert
        type.GetProperty("Ciphertext")!.PropertyType.ShouldBe(typeof(ImmutableArray<byte>));
        type.GetProperty("KeyId")!.PropertyType.ShouldBe(typeof(string));
        type.GetProperty("Algorithm")!.PropertyType.ShouldBe(typeof(string));
        type.GetProperty("Nonce")!.PropertyType.ShouldBe(typeof(ImmutableArray<byte>));
        type.GetProperty("Tag")!.PropertyType.ShouldBe(typeof(ImmutableArray<byte>));
        type.GetProperty("Version")!.PropertyType.ShouldBe(typeof(int));
    }

    [Fact]
    public void EncryptedPayload_ShouldBeSealedRecord()
    {
        // Arrange
        var type = typeof(EncryptedPayload);

        // Assert
        type.IsClass.ShouldBeTrue("EncryptedPayload must be a reference type (sealed record)");
        type.IsSealed.ShouldBeTrue("EncryptedPayload must be sealed");

        var implementsEquatable = type.GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEquatable<>));
        implementsEquatable.ShouldBeTrue("EncryptedPayload must implement IEquatable<T> (record type)");
    }

    [Fact]
    public void EncryptedPayload_Version_ShouldDefaultTo1()
    {
        // Arrange
        var payload = new EncryptedPayload
        {
            Ciphertext = ImmutableArray<byte>.Empty,
            KeyId = "test",
            Algorithm = "AES-256-GCM",
            Nonce = ImmutableArray<byte>.Empty
        };

        // Assert
        payload.Version.ShouldBe(1);
    }

    #endregion

    #region MessageEncryptionContext Data Type Contract

    [Fact]
    public void MessageEncryptionContext_ShouldHave_FiveProperties()
    {
        // Arrange
        var type = typeof(MessageEncryptionContext);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Assert
        properties.Length.ShouldBe(5,
            "MessageEncryptionContext must have exactly 5 properties: " +
            "KeyId, TenantId, MessageType, MessageId, AssociatedData");

        var names = properties.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        names.ShouldContain("KeyId");
        names.ShouldContain("TenantId");
        names.ShouldContain("MessageType");
        names.ShouldContain("MessageId");
        names.ShouldContain("AssociatedData");
    }

    [Fact]
    public void MessageEncryptionContext_ShouldBeSealedRecord()
    {
        // Arrange
        var type = typeof(MessageEncryptionContext);

        // Assert
        type.IsClass.ShouldBeTrue("MessageEncryptionContext must be a reference type");
        type.IsSealed.ShouldBeTrue("MessageEncryptionContext must be sealed");

        var implementsEquatable = type.GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEquatable<>));
        implementsEquatable.ShouldBeTrue("MessageEncryptionContext must implement IEquatable<T> (record type)");
    }

    [Fact]
    public void MessageEncryptionContext_PropertyTypes_ShouldBeCorrect()
    {
        // Arrange
        var type = typeof(MessageEncryptionContext);

        // Assert
        type.GetProperty("KeyId")!.PropertyType.ShouldBe(typeof(string));
        type.GetProperty("TenantId")!.PropertyType.ShouldBe(typeof(string));
        type.GetProperty("MessageType")!.PropertyType.ShouldBe(typeof(string));
        type.GetProperty("MessageId")!.PropertyType.ShouldBe(typeof(Guid?));
        type.GetProperty("AssociatedData")!.PropertyType.ShouldBe(typeof(ImmutableArray<byte>));
    }

    #endregion

    #region MessageEncryptionErrors Contract

    [Fact]
    public void MessageEncryptionErrors_ShouldDefine_NineErrorCodeConstants()
    {
        // Arrange
        var type = typeof(MessageEncryptionErrors);

        // Act
        var constants = type
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .ToList();

        // Assert
        constants.Count.ShouldBe(9,
            "MessageEncryptionErrors must define exactly 9 error code constants");

        var names = constants.Select(f => f.Name).ToHashSet(StringComparer.Ordinal);
        names.ShouldContain(nameof(MessageEncryptionErrors.EncryptionFailedCode));
        names.ShouldContain(nameof(MessageEncryptionErrors.DecryptionFailedCode));
        names.ShouldContain(nameof(MessageEncryptionErrors.KeyNotFoundCode));
        names.ShouldContain(nameof(MessageEncryptionErrors.InvalidPayloadCode));
        names.ShouldContain(nameof(MessageEncryptionErrors.UnsupportedVersionCode));
        names.ShouldContain(nameof(MessageEncryptionErrors.TenantKeyResolutionFailedCode));
        names.ShouldContain(nameof(MessageEncryptionErrors.SerializationFailedCode));
        names.ShouldContain(nameof(MessageEncryptionErrors.DeserializationFailedCode));
        names.ShouldContain(nameof(MessageEncryptionErrors.ProviderUnavailableCode));
    }

    [Fact]
    public void AllErrorCodes_ShouldStartWith_MsgEncryptionPrefix()
    {
        // Arrange
        const string expectedPrefix = "msg_encryption.";
        var type = typeof(MessageEncryptionErrors);

        var constants = type
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .ToList();

        // Assert
        constants.ShouldNotBeEmpty("MessageEncryptionErrors must define at least one error code constant");

        foreach (var constant in constants)
        {
            var value = (string?)constant.GetRawConstantValue();
            value.ShouldNotBeNull($"Error code constant '{constant.Name}' must not be null");
            (value!.StartsWith(expectedPrefix, StringComparison.Ordinal)).ShouldBeTrue(
                $"Error code '{constant.Name}' = '{value}' must start with '{expectedPrefix}'");
        }
    }

    #endregion

    #region EncryptedMessageAttribute Contract

    [Fact]
    public void EncryptedMessageAttribute_ShouldInherit_Attribute()
    {
        typeof(EncryptedMessageAttribute).IsSubclassOf(typeof(Attribute)).ShouldBeTrue(
            "EncryptedMessageAttribute must inherit from System.Attribute");
    }

    [Fact]
    public void EncryptedMessageAttribute_ShouldBeSealed()
    {
        typeof(EncryptedMessageAttribute).IsSealed.ShouldBeTrue(
            "EncryptedMessageAttribute must be sealed");
    }

    [Fact]
    public void EncryptedMessageAttribute_ShouldHave_ThreeProperties()
    {
        // Arrange
        var type = typeof(EncryptedMessageAttribute);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        // Assert
        var names = properties.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        names.ShouldContain("Enabled", "EncryptedMessageAttribute must have Enabled property");
        names.ShouldContain("KeyId", "EncryptedMessageAttribute must have KeyId property");
        names.ShouldContain("UseTenantKey", "EncryptedMessageAttribute must have UseTenantKey property");
    }

    [Fact]
    public void EncryptedMessageAttribute_Defaults_ShouldBeStable()
    {
        // Arrange
        var attr = new EncryptedMessageAttribute();

        // Assert
        attr.Enabled.ShouldBeTrue("Enabled must default to true");
        attr.KeyId.ShouldBeNull("KeyId must default to null");
        attr.UseTenantKey.ShouldBeFalse("UseTenantKey must default to false");
    }

    [Fact]
    public void EncryptedMessageAttribute_ShouldTarget_ClassOnly()
    {
        // Arrange
        var usage = typeof(EncryptedMessageAttribute)
            .GetCustomAttribute<AttributeUsageAttribute>();

        // Assert
        usage.ShouldNotBeNull();
        usage.ValidOn.ShouldBe(AttributeTargets.Class);
        usage.AllowMultiple.ShouldBeFalse();
        usage.Inherited.ShouldBeTrue();
    }

    #endregion

    #region MessageEncryptionOptions Contract

    [Fact]
    public void MessageEncryptionOptions_ShouldHave_ExpectedProperties()
    {
        // Arrange
        var type = typeof(MessageEncryptionOptions);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Assert
        var names = properties.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);

        names.ShouldContain("Enabled");
        names.ShouldContain("EncryptAllMessages");
        names.ShouldContain("DefaultKeyId");
        names.ShouldContain("UseTenantKeys");
        names.ShouldContain("TenantKeyPattern");
        names.ShouldContain("AuditDecryption");
        names.ShouldContain("AddHealthCheck");
        names.ShouldContain("EnableTracing");
        names.ShouldContain("EnableMetrics");
    }

    [Fact]
    public void MessageEncryptionOptions_Defaults_ShouldBeStable()
    {
        // Arrange
        var options = new MessageEncryptionOptions();

        // Assert
        options.Enabled.ShouldBeTrue();
        options.EncryptAllMessages.ShouldBeFalse();
        options.DefaultKeyId.ShouldBeNull();
        options.UseTenantKeys.ShouldBeFalse();
        options.TenantKeyPattern.ShouldBe("tenant-{0}-key");
        options.AuditDecryption.ShouldBeFalse();
        options.AddHealthCheck.ShouldBeFalse();
        options.EnableTracing.ShouldBeFalse();
        options.EnableMetrics.ShouldBeFalse();
    }

    #endregion

    #region DefaultMessageEncryptionProvider Contract

    [Fact]
    public void DefaultMessageEncryptionProvider_ShouldImplement_IMessageEncryptionProvider()
    {
        typeof(IMessageEncryptionProvider)
            .IsAssignableFrom(typeof(DefaultMessageEncryptionProvider))
            .ShouldBeTrue("DefaultMessageEncryptionProvider must implement IMessageEncryptionProvider");
    }

    #endregion

    #region DefaultTenantKeyResolver Contract

    [Fact]
    public void DefaultTenantKeyResolver_ShouldImplement_ITenantKeyResolver()
    {
        typeof(ITenantKeyResolver)
            .IsAssignableFrom(typeof(DefaultTenantKeyResolver))
            .ShouldBeTrue("DefaultTenantKeyResolver must implement ITenantKeyResolver");
    }

    #endregion

    #region EncryptedPayloadFormatter Contract

    [Fact]
    public void EncryptedPayloadFormatter_ShouldBeStatic()
    {
        var type = typeof(EncryptedPayloadFormatter);

        type.IsAbstract.ShouldBeTrue("EncryptedPayloadFormatter must be static (abstract + sealed)");
        type.IsSealed.ShouldBeTrue("EncryptedPayloadFormatter must be static (abstract + sealed)");
    }

    [Fact]
    public void EncryptedPayloadFormatter_ShouldHave_FormatTryParseIsEncrypted()
    {
        var type = typeof(EncryptedPayloadFormatter);
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

        var names = methods.Select(m => m.Name).ToHashSet(StringComparer.Ordinal);
        names.ShouldContain("Format", "EncryptedPayloadFormatter must have Format method");
        names.ShouldContain("TryParse", "EncryptedPayloadFormatter must have TryParse method");
        names.ShouldContain("IsEncrypted", "EncryptedPayloadFormatter must have IsEncrypted method");
    }

    #endregion
}
