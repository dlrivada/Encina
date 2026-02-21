using System.Reflection;
using Encina.Security.PII;
using Encina.Security.PII.Abstractions;
using Encina.Security.PII.Attributes;
using Encina.Security.PII.Health;

namespace Encina.ContractTests.Security.PII;

/// <summary>
/// Contract tests for the Encina.Security.PII public surface area.
/// Verifies that interface shapes, enum structures, attribute properties,
/// and error code conventions remain stable.
/// </summary>
[Trait("Category", "Contract")]
public sealed class PIIContractTests
{
    #region IPIIMasker Interface Shape

    [Fact]
    public void IPIIMasker_ShouldHave_ThreeMethods()
    {
        // Arrange
        var type = typeof(IPIIMasker);

        // Act
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        // Assert
        methods.Length.ShouldBe(3,
            "IPIIMasker must define exactly 3 methods: " +
            "Mask(string, PIIType), Mask(string, string), MaskObject<T>");

        var methodNames = methods.Select(m => m.Name).ToHashSet(StringComparer.Ordinal);
        methodNames.ShouldContain("Mask", "IPIIMasker must have Mask method(s)");
        methodNames.ShouldContain("MaskObject", "IPIIMasker must have MaskObject method");
    }

    #endregion

    #region PIIType Enum Contract

    [Fact]
    public void PIIType_ShouldDefine_NineValues()
    {
        // Act
        var values = Enum.GetValues<PIIType>();

        // Assert
        values.Length.ShouldBe(9,
            "PIIType must define exactly 9 values: " +
            "Email, Phone, CreditCard, SSN, Name, Address, DateOfBirth, IPAddress, Custom");
    }

    [Theory]
    [InlineData(PIIType.Email, 0)]
    [InlineData(PIIType.Phone, 1)]
    [InlineData(PIIType.CreditCard, 2)]
    [InlineData(PIIType.SSN, 3)]
    [InlineData(PIIType.Name, 4)]
    [InlineData(PIIType.Address, 5)]
    [InlineData(PIIType.DateOfBirth, 6)]
    [InlineData(PIIType.IPAddress, 7)]
    [InlineData(PIIType.Custom, 8)]
    public void PIIType_ShouldHaveExpectedValue(PIIType type, int expectedValue)
    {
        ((int)type).ShouldBe(expectedValue,
            $"PIIType.{type} must have underlying value {expectedValue}");
    }

    #endregion

    #region MaskingMode Enum Contract

    [Fact]
    public void MaskingMode_ShouldDefine_FiveValues()
    {
        // Act
        var values = Enum.GetValues<MaskingMode>();

        // Assert
        values.Length.ShouldBe(5,
            "MaskingMode must define exactly 5 values: " +
            "Partial, Full, Hash, Tokenize, Redact");
    }

    #endregion

    #region PIIAttribute Contract

    [Fact]
    public void PIIAttribute_ShouldHave_TypeProperty()
    {
        // Arrange
        var type = typeof(PIIAttribute);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Assert
        var propertyNames = properties.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        propertyNames.ShouldContain("Type", "PIIAttribute must have a Type property");
    }

    [Fact]
    public void PIIAttribute_Type_ShouldBe_PIIType()
    {
        // Arrange
        var prop = typeof(PIIAttribute).GetProperty("Type", BindingFlags.Public | BindingFlags.Instance);

        // Assert
        prop.ShouldNotBeNull("PIIAttribute must have a Type property");
        prop!.PropertyType.ShouldBe(typeof(PIIType));
    }

    [Fact]
    public void PIIAttribute_ShouldHave_PatternAndReplacementAndMode()
    {
        // Arrange
        var type = typeof(PIIAttribute);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Assert
        var propertyNames = properties.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        propertyNames.ShouldContain("Pattern", "PIIAttribute must have a Pattern property");
        propertyNames.ShouldContain("Replacement", "PIIAttribute must have a Replacement property");
        propertyNames.ShouldContain("Mode", "PIIAttribute must have a Mode property");
    }

    #endregion

    #region PIIErrors Contract

    [Fact]
    public void PIIErrors_ShouldDefine_ThreeErrorCodes()
    {
        // Arrange
        var type = typeof(PIIErrors);

        // Act
        var constants = type
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .ToList();

        // Assert
        constants.Count.ShouldBe(3,
            "PIIErrors must define exactly 3 error code constants: " +
            "MaskingFailedCode, StrategyNotFoundCode, SerializationFailedCode");

        var names = constants.Select(f => f.Name).ToHashSet(StringComparer.Ordinal);
        names.ShouldContain(nameof(PIIErrors.MaskingFailedCode));
        names.ShouldContain(nameof(PIIErrors.StrategyNotFoundCode));
        names.ShouldContain(nameof(PIIErrors.SerializationFailedCode));
    }

    [Fact]
    public void AllErrorCodes_ShouldStartWith_PiiPrefix()
    {
        // Arrange
        const string expectedPrefix = "pii.";
        var type = typeof(PIIErrors);

        var constants = type
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .ToList();

        // Assert
        constants.ShouldNotBeEmpty("PIIErrors must define at least one error code constant");

        foreach (var constant in constants)
        {
            var value = (string?)constant.GetRawConstantValue();
            value.ShouldNotBeNull($"Error code constant '{constant.Name}' must not be null");
            (value!.StartsWith(expectedPrefix, StringComparison.Ordinal)).ShouldBeTrue(
                $"Error code '{constant.Name}' = '{value}' must start with '{expectedPrefix}'");
        }
    }

    #endregion

    #region PIIHealthCheck Contract

    [Fact]
    public void PIIHealthCheck_DefaultName_ShouldBe_encina_pii()
    {
        // Assert
        PIIHealthCheck.DefaultName.ShouldBe("encina_pii",
            "PIIHealthCheck.DefaultName must be 'encina_pii'");
    }

    #endregion

    #region MaskingOptions Contract

    [Fact]
    public void MaskingOptions_ShouldHave_SevenProperties()
    {
        // Arrange
        var type = typeof(MaskingOptions);

        // Act
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        // Assert
        properties.Length.ShouldBe(7,
            "MaskingOptions must define exactly 7 properties: " +
            "Mode, MaskCharacter, PreserveLength, VisibleCharactersStart, " +
            "VisibleCharactersEnd, RedactedPlaceholder, HashSalt");

        var propertyNames = properties.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        propertyNames.ShouldContain("Mode");
        propertyNames.ShouldContain("MaskCharacter");
        propertyNames.ShouldContain("PreserveLength");
        propertyNames.ShouldContain("VisibleCharactersStart");
        propertyNames.ShouldContain("VisibleCharactersEnd");
        propertyNames.ShouldContain("RedactedPlaceholder");
        propertyNames.ShouldContain("HashSalt");
    }

    [Fact]
    public void MaskingOptions_ShouldBeRecordStruct()
    {
        // Arrange
        var type = typeof(MaskingOptions);

        // Assert
        type.IsValueType.ShouldBeTrue("MaskingOptions must be a value type (record struct)");
    }

    [Fact]
    public void MaskingOptions_Defaults_ShouldBeStable()
    {
        // Arrange
        var options = new MaskingOptions();

        // Assert
        options.Mode.ShouldBe(MaskingMode.Partial);
        options.MaskCharacter.ShouldBe('*');
        options.PreserveLength.ShouldBeTrue();
        options.VisibleCharactersStart.ShouldBe(0);
        options.VisibleCharactersEnd.ShouldBe(0);
        options.RedactedPlaceholder.ShouldBe("[REDACTED]");
        options.HashSalt.ShouldBeNull();
    }

    #endregion
}
