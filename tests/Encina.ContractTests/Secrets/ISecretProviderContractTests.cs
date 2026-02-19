#pragma warning disable CA1859 // Contract tests intentionally use interface types to verify contracts

using Encina.Secrets;
using System.Reflection;

namespace Encina.ContractTests.Secrets;

/// <summary>
/// Contract tests for the <see cref="ISecretProvider"/> public interface and related types.
/// Verifies that the surface area, method signatures, and error code conventions remain stable.
/// </summary>
[Trait("Category", "Contract")]
public sealed class ISecretProviderContractTests
{
    // -- Interface shape contracts --

    [Fact]
    public void ISecretProvider_ShouldHave_SixMethods()
    {
        // Arrange
        var type = typeof(ISecretProvider);

        // Act
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        // Assert
        methods.Length.ShouldBe(6,
            "ISecretProvider must define exactly 6 methods: " +
            "GetSecretAsync, GetSecretVersionAsync, SetSecretAsync, DeleteSecretAsync, ListSecretsAsync, ExistsAsync");
    }

    [Fact]
    public void AllMethods_ShouldReturn_EitherEncinaError()
    {
        // Arrange
        var type = typeof(ISecretProvider);
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

            var innerGenericDef = innerType.GetGenericTypeDefinition();
            var fullName = innerGenericDef.FullName ?? string.Empty;
            (fullName.StartsWith("LanguageExt.Either", StringComparison.Ordinal)).ShouldBeTrue(
                $"Method '{method.Name}' must return ValueTask<Either<EncinaError, T>>, got {innerType.Name}");

            var eitherArgs = innerType.GetGenericArguments();
            eitherArgs[0].ShouldBe(typeof(EncinaError),
                $"Method '{method.Name}' Either left type must be EncinaError");
        }
    }

    [Fact]
    public void GetSecretAsync_ShouldAccept_StringAndCancellationToken()
    {
        // Arrange
        var type = typeof(ISecretProvider);
        var method = type.GetMethod("GetSecretAsync");

        // Assert
        method.ShouldNotBeNull("GetSecretAsync must exist on ISecretProvider");

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(2,
            "GetSecretAsync must accept (string name, CancellationToken cancellationToken)");

        parameters[0].ParameterType.ShouldBe(typeof(string),
            "GetSecretAsync first parameter must be string");
        parameters[0].Name.ShouldBe("name",
            "GetSecretAsync first parameter must be named 'name'");

        parameters[1].ParameterType.ShouldBe(typeof(CancellationToken),
            "GetSecretAsync second parameter must be CancellationToken");
        parameters[1].HasDefaultValue.ShouldBeTrue(
            "GetSecretAsync CancellationToken must have a default value");
    }

    [Fact]
    public void SetSecretAsync_ShouldAccept_NameValueOptionsAndCancellationToken()
    {
        // Arrange
        var type = typeof(ISecretProvider);
        var method = type.GetMethod("SetSecretAsync");

        // Assert
        method.ShouldNotBeNull("SetSecretAsync must exist on ISecretProvider");

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(4,
            "SetSecretAsync must accept (string name, string value, SecretOptions? options, CancellationToken cancellationToken)");

        parameters[0].ParameterType.ShouldBe(typeof(string),
            "SetSecretAsync first parameter must be string (name)");
        parameters[1].ParameterType.ShouldBe(typeof(string),
            "SetSecretAsync second parameter must be string (value)");
        parameters[2].ParameterType.ShouldBe(typeof(SecretOptions),
            "SetSecretAsync third parameter must be SecretOptions?");
        parameters[2].HasDefaultValue.ShouldBeTrue(
            "SetSecretAsync options parameter must be optional (nullable with default)");
        parameters[3].ParameterType.ShouldBe(typeof(CancellationToken),
            "SetSecretAsync fourth parameter must be CancellationToken");
        parameters[3].HasDefaultValue.ShouldBeTrue(
            "SetSecretAsync CancellationToken must have a default value");
    }

    // -- Data type contracts --

    [Fact]
    public void Secret_Record_ShouldHave_FourProperties()
    {
        // Arrange
        var type = typeof(Secret);

        // Assert
        type.IsValueType.ShouldBeFalse("Secret must be a reference type (record class)");

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        properties.Length.ShouldBe(4,
            "Secret must have exactly 4 properties: Name, Value, Version, ExpiresAtUtc");

        var propertyNames = properties.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        propertyNames.ShouldContain("Name", "Secret must have a Name property");
        propertyNames.ShouldContain("Value", "Secret must have a Value property");
        propertyNames.ShouldContain("Version", "Secret must have a Version property");
        propertyNames.ShouldContain("ExpiresAtUtc", "Secret must have an ExpiresAtUtc property");

        type.GetProperty("Name")!.PropertyType.ShouldBe(typeof(string));
        type.GetProperty("Value")!.PropertyType.ShouldBe(typeof(string));
        type.GetProperty("Version")!.PropertyType.ShouldBe(typeof(string),
            "Version must be string (nullable allowed at runtime)");
        type.GetProperty("ExpiresAtUtc")!.PropertyType.ShouldBe(typeof(DateTime?),
            "ExpiresAtUtc must be nullable DateTime");
    }

    [Fact]
    public void SecretMetadata_Record_ShouldHave_FourProperties()
    {
        // Arrange
        var type = typeof(SecretMetadata);

        // Assert
        type.IsValueType.ShouldBeFalse("SecretMetadata must be a reference type (record class)");

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        properties.Length.ShouldBe(4,
            "SecretMetadata must have exactly 4 properties: Name, Version, CreatedAtUtc, ExpiresAtUtc");

        var propertyNames = properties.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        propertyNames.ShouldContain("Name", "SecretMetadata must have a Name property");
        propertyNames.ShouldContain("Version", "SecretMetadata must have a Version property");
        propertyNames.ShouldContain("CreatedAtUtc", "SecretMetadata must have a CreatedAtUtc property");
        propertyNames.ShouldContain("ExpiresAtUtc", "SecretMetadata must have an ExpiresAtUtc property");

        type.GetProperty("Name")!.PropertyType.ShouldBe(typeof(string));
        type.GetProperty("Version")!.PropertyType.ShouldBe(typeof(string));
        type.GetProperty("CreatedAtUtc")!.PropertyType.ShouldBe(typeof(DateTime));
        type.GetProperty("ExpiresAtUtc")!.PropertyType.ShouldBe(typeof(DateTime?),
            "ExpiresAtUtc must be nullable DateTime");
    }

    // -- Error code contracts --

    [Fact]
    public void SecretsErrorCodes_ShouldDefine_SixErrorCodes()
    {
        // Arrange
        var type = typeof(SecretsErrorCodes);

        // Act - find all public const string fields (the error code constants)
        var constants = type
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .ToList();

        // Assert
        constants.Count.ShouldBe(6,
            "SecretsErrorCodes must define exactly 6 error code constants: " +
            "NotFoundCode, AccessDeniedCode, InvalidNameCode, ProviderUnavailableCode, VersionNotFoundCode, OperationFailedCode");

        var names = constants.Select(f => f.Name).ToHashSet(StringComparer.Ordinal);
        names.ShouldContain(nameof(SecretsErrorCodes.NotFoundCode));
        names.ShouldContain(nameof(SecretsErrorCodes.AccessDeniedCode));
        names.ShouldContain(nameof(SecretsErrorCodes.InvalidNameCode));
        names.ShouldContain(nameof(SecretsErrorCodes.ProviderUnavailableCode));
        names.ShouldContain(nameof(SecretsErrorCodes.VersionNotFoundCode));
        names.ShouldContain(nameof(SecretsErrorCodes.OperationFailedCode));
    }

    [Fact]
    public void AllErrorCodes_ShouldStartWith_EncinaSecretsPrefix()
    {
        // Arrange
        const string expectedPrefix = "encina.secrets.";
        var type = typeof(SecretsErrorCodes);

        var constants = type
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .ToList();

        // Assert
        constants.ShouldNotBeEmpty("SecretsErrorCodes must define at least one error code constant");

        foreach (var constant in constants)
        {
            var value = (string?)constant.GetRawConstantValue();
            value.ShouldNotBeNull($"Error code constant '{constant.Name}' must not be null");
            (value!.StartsWith(expectedPrefix, StringComparison.Ordinal)).ShouldBeTrue(
                $"Error code '{constant.Name}' = '{value}' must start with '{expectedPrefix}'");
        }
    }
}
