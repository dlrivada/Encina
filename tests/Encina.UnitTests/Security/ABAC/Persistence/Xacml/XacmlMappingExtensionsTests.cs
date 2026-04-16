using Encina.Security.ABAC;
using Encina.Security.ABAC.Persistence.Xacml;

using Shouldly;

namespace Encina.UnitTests.Security.ABAC.Persistence.Xacml;

/// <summary>
/// Unit tests for <see cref="XacmlMappingExtensions"/>: verifies enum-to-URN mapping,
/// value formatting/parsing, and error handling for all supported XACML data types.
/// </summary>
public sealed class XacmlMappingExtensionsTests
{
    // ── AttributeCategory Mapping ──────────────────────────────────

    #region AttributeCategory Mapping

    [Theory]
    [InlineData(AttributeCategory.Subject, "urn:oasis:names:tc:xacml:1.0:subject-category:access-subject")]
    [InlineData(AttributeCategory.Resource, "urn:oasis:names:tc:xacml:3.0:attribute-category:resource")]
    [InlineData(AttributeCategory.Action, "urn:oasis:names:tc:xacml:3.0:attribute-category:action")]
    [InlineData(AttributeCategory.Environment, "urn:oasis:names:tc:xacml:3.0:attribute-category:environment")]
    public void ToXacmlUrn_AttributeCategory_ReturnsCorrectUrn(AttributeCategory category, string expectedUrn)
    {
        // Act
        var result = category.ToXacmlUrn();

        // Assert
        result.ShouldBe(expectedUrn);
    }

    [Theory]
    [InlineData("urn:oasis:names:tc:xacml:1.0:subject-category:access-subject", AttributeCategory.Subject)]
    [InlineData("urn:oasis:names:tc:xacml:3.0:attribute-category:resource", AttributeCategory.Resource)]
    [InlineData("urn:oasis:names:tc:xacml:3.0:attribute-category:action", AttributeCategory.Action)]
    [InlineData("urn:oasis:names:tc:xacml:3.0:attribute-category:environment", AttributeCategory.Environment)]
    public void ToAttributeCategory_KnownUrn_ReturnsCorrectCategory(string urn, AttributeCategory expected)
    {
        // Act
        var result = XacmlMappingExtensions.ToAttributeCategory(urn);

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    public void ToXacmlUrn_InvalidAttributeCategory_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var invalid = (AttributeCategory)999;

        // Act
        var act = () => invalid.ToXacmlUrn();

        // Assert
        Should.Throw<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void ToAttributeCategory_UnknownUrn_ThrowsArgumentException()
    {
        // Act
        var act = () => XacmlMappingExtensions.ToAttributeCategory("urn:unknown:category");

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void AttributeCategory_AllValues_RoundTrip()
    {
        // Act & Assert — Every category should map to a URN and back
        foreach (var category in Enum.GetValues<AttributeCategory>())
        {
            var urn = category.ToXacmlUrn();
            var roundTripped = XacmlMappingExtensions.ToAttributeCategory(urn);
            roundTripped.ShouldBe(category);
        }
    }

    #endregion

    // ── CombiningAlgorithmId Mapping ───────────────────────────────

    #region CombiningAlgorithmId Mapping

    [Theory]
    [InlineData(CombiningAlgorithmId.DenyOverrides, true, "urn:oasis:names:tc:xacml:3.0:rule-combining-algorithm:deny-overrides")]
    [InlineData(CombiningAlgorithmId.DenyOverrides, false, "urn:oasis:names:tc:xacml:3.0:policy-combining-algorithm:deny-overrides")]
    [InlineData(CombiningAlgorithmId.PermitOverrides, true, "urn:oasis:names:tc:xacml:3.0:rule-combining-algorithm:permit-overrides")]
    [InlineData(CombiningAlgorithmId.FirstApplicable, true, "urn:oasis:names:tc:xacml:1.0:rule-combining-algorithm:first-applicable")]
    [InlineData(CombiningAlgorithmId.FirstApplicable, false, "urn:oasis:names:tc:xacml:1.0:policy-combining-algorithm:first-applicable")]
    public void ToXacmlUrn_CombiningAlgorithm_ReturnsCorrectUrn(
        CombiningAlgorithmId algorithm, bool isRuleCombining, string expectedUrn)
    {
        // Act
        var result = algorithm.ToXacmlUrn(isRuleCombining);

        // Assert
        result.ShouldBe(expectedUrn);
    }

    [Fact]
    public void ToCombiningAlgorithmId_RuleCombiningUrn_ReturnsCorrectAlgorithm()
    {
        // Act
        var result = XacmlMappingExtensions.ToCombiningAlgorithmId(
            "urn:oasis:names:tc:xacml:3.0:rule-combining-algorithm:deny-overrides");

        // Assert
        result.ShouldBe(CombiningAlgorithmId.DenyOverrides);
    }

    [Fact]
    public void ToCombiningAlgorithmId_PolicyCombiningUrn_ReturnsCorrectAlgorithm()
    {
        // Act
        var result = XacmlMappingExtensions.ToCombiningAlgorithmId(
            "urn:oasis:names:tc:xacml:3.0:policy-combining-algorithm:permit-overrides");

        // Assert
        result.ShouldBe(CombiningAlgorithmId.PermitOverrides);
    }

    [Fact]
    public void ToCombiningAlgorithmId_UnknownUrn_ThrowsArgumentException()
    {
        // Act
        var act = () => XacmlMappingExtensions.ToCombiningAlgorithmId(
            "urn:oasis:names:tc:xacml:3.0:rule-combining-algorithm:unknown-algorithm");

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void ToCombiningAlgorithmId_InvalidFormat_ThrowsArgumentException()
    {
        // Act
        var act = () => XacmlMappingExtensions.ToCombiningAlgorithmId("no-colon-at-end:");

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void CombiningAlgorithmId_AllValues_RoundTrip_RuleCombining()
    {
        // Act & Assert
        foreach (var algorithm in Enum.GetValues<CombiningAlgorithmId>())
        {
            var urn = algorithm.ToXacmlUrn(isRuleCombining: true);
            var roundTripped = XacmlMappingExtensions.ToCombiningAlgorithmId(urn);
            roundTripped.ShouldBe(algorithm);
        }
    }

    [Fact]
    public void CombiningAlgorithmId_AllValues_RoundTrip_PolicyCombining()
    {
        // Act & Assert
        foreach (var algorithm in Enum.GetValues<CombiningAlgorithmId>())
        {
            var urn = algorithm.ToXacmlUrn(isRuleCombining: false);
            var roundTripped = XacmlMappingExtensions.ToCombiningAlgorithmId(urn);
            roundTripped.ShouldBe(algorithm);
        }
    }

    #endregion

    // ── Effect Mapping ─────────────────────────────────────────────

    #region Effect Mapping

    [Theory]
    [InlineData(Effect.Permit, "Permit")]
    [InlineData(Effect.Deny, "Deny")]
    public void ToXacmlString_Effect_ReturnsCorrectString(Effect effect, string expected)
    {
        // Act
        var result = effect.ToXacmlString();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("Permit", Effect.Permit)]
    [InlineData("Deny", Effect.Deny)]
    public void ToEffect_KnownString_ReturnsCorrectEffect(string value, Effect expected)
    {
        // Act
        var result = XacmlMappingExtensions.ToEffect(value);

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    public void ToXacmlString_NotApplicableEffect_ThrowsArgumentOutOfRangeException()
    {
        // Act
        var act = () => Effect.NotApplicable.ToXacmlString();

        // Assert
        Should.Throw<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void ToEffect_UnknownString_ThrowsArgumentException()
    {
        // Act
        var act = () => XacmlMappingExtensions.ToEffect("Unknown");

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    #endregion

    // ── FulfillOn Mapping ──────────────────────────────────────────

    #region FulfillOn Mapping

    [Theory]
    [InlineData(FulfillOn.Permit, "Permit")]
    [InlineData(FulfillOn.Deny, "Deny")]
    public void ToXacmlString_FulfillOn_ReturnsCorrectString(FulfillOn fulfillOn, string expected)
    {
        // Act
        var result = fulfillOn.ToXacmlString();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("Permit", FulfillOn.Permit)]
    [InlineData("Deny", FulfillOn.Deny)]
    public void ToFulfillOn_KnownString_ReturnsCorrectValue(string value, FulfillOn expected)
    {
        // Act
        var result = XacmlMappingExtensions.ToFulfillOn(value);

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    public void ToFulfillOn_UnknownString_ThrowsArgumentException()
    {
        // Act
        var act = () => XacmlMappingExtensions.ToFulfillOn("Unknown");

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    #endregion

    // ── FormatXacmlValue ───────────────────────────────────────────

    #region FormatXacmlValue

    [Theory]
    [InlineData(true, "true")]
    [InlineData(false, "false")]
    public void FormatXacmlValue_Boolean_ReturnsXsdBoolean(bool value, string expected)
    {
        // Act
        var result = XacmlMappingExtensions.FormatXacmlValue(value, XACMLDataTypes.Boolean);

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    public void FormatXacmlValue_Integer_ReturnsInvariantString()
    {
        // Act
        var result = XacmlMappingExtensions.FormatXacmlValue(42, XACMLDataTypes.Integer);

        // Assert
        result.ShouldBe("42");
    }

    [Fact]
    public void FormatXacmlValue_Double_ReturnsRoundTripFormat()
    {
        // Act
        var result = XacmlMappingExtensions.FormatXacmlValue(3.14, XACMLDataTypes.Double);

        // Assert
        result.ShouldContain("3.14");
    }

    [Fact]
    public void FormatXacmlValue_Null_ReturnsEmptyString()
    {
        // Act
        var result = XacmlMappingExtensions.FormatXacmlValue(null, XACMLDataTypes.String);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void FormatXacmlValue_String_ReturnsToString()
    {
        // Act
        var result = XacmlMappingExtensions.FormatXacmlValue("hello", XACMLDataTypes.String);

        // Assert
        result.ShouldBe("hello");
    }

    #endregion

    // ── ParseXacmlValue ────────────────────────────────────────────

    #region ParseXacmlValue

    [Theory]
    [InlineData("true", true)]
    [InlineData("1", true)]
    [InlineData("false", false)]
    [InlineData("0", false)]
    public void ParseXacmlValue_Boolean_ParsesCorrectly(string text, bool expected)
    {
        // Act
        var result = XacmlMappingExtensions.ParseXacmlValue(text, XACMLDataTypes.Boolean);

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    public void ParseXacmlValue_Integer_ReturnsLong()
    {
        // Act
        var result = XacmlMappingExtensions.ParseXacmlValue("42", XACMLDataTypes.Integer);

        // Assert
        result.ShouldBeOfType<long>();
        result.ShouldBe(42L);
    }

    [Fact]
    public void ParseXacmlValue_Double_ReturnsDouble()
    {
        // Act
        var result = XacmlMappingExtensions.ParseXacmlValue("3.14", XACMLDataTypes.Double);

        // Assert
        result.ShouldBeOfType<double>();
        ((double)result!).ShouldBeInRange(3.14 - 0.001, 3.14 + 0.001);
    }

    [Fact]
    public void ParseXacmlValue_String_ReturnsAsIs()
    {
        // Act
        var result = XacmlMappingExtensions.ParseXacmlValue("hello", XACMLDataTypes.String);

        // Assert
        result.ShouldBe("hello");
    }

    [Fact]
    public void ParseXacmlValue_Null_ReturnsNull()
    {
        // Act
        var result = XacmlMappingExtensions.ParseXacmlValue(null, XACMLDataTypes.String);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void ParseXacmlValue_Empty_ReturnsNull()
    {
        // Act
        var result = XacmlMappingExtensions.ParseXacmlValue("", XACMLDataTypes.Integer);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void ParseXacmlValue_AnyURI_ReturnsUri()
    {
        // Act
        var result = XacmlMappingExtensions.ParseXacmlValue("https://example.com", XACMLDataTypes.AnyURI);

        // Assert
        result.ShouldBeOfType<Uri>();
        ((Uri)result!).ToString().ShouldBe("https://example.com/");
    }

    [Fact]
    public void ParseXacmlValue_UnknownDataType_ReturnsRawText()
    {
        // Act
        var result = XacmlMappingExtensions.ParseXacmlValue("raw-value", "urn:custom:datatype");

        // Assert
        result.ShouldBe("raw-value");
    }

    #endregion

    // ── XacmlDataTypeMap ───────────────────────────────────────────

    #region XacmlDataTypeMap

    [Theory]
    [InlineData("hello", XACMLDataTypes.String)]
    [InlineData(true, XACMLDataTypes.Boolean)]
    [InlineData(42, XACMLDataTypes.Integer)]
    [InlineData(3.14, XACMLDataTypes.Double)]
    public void InferDataType_KnownTypes_ReturnsCorrectUrn(object value, string expectedDataType)
    {
        // Act
        var result = XacmlDataTypeMap.InferDataType(value);

        // Assert
        result.ShouldBe(expectedDataType);
    }

    [Fact]
    public void InferDataType_Null_ReturnsString()
    {
        // Act
        var result = XacmlDataTypeMap.InferDataType(null);

        // Assert
        result.ShouldBe(XACMLDataTypes.String);
    }

    [Fact]
    public void InferDataType_DateTime_ReturnsDateTimeUri()
    {
        // Act
        var result = XacmlDataTypeMap.InferDataType(DateTime.UtcNow);

        // Assert
        result.ShouldBe(XACMLDataTypes.DateTime);
    }

    [Fact]
    public void InferDataType_DateTimeOffset_ReturnsDateTimeUri()
    {
        // Act
        var result = XacmlDataTypeMap.InferDataType(DateTimeOffset.UtcNow);

        // Assert
        result.ShouldBe(XACMLDataTypes.DateTime);
    }

    [Fact]
    public void InferDataType_Uri_ReturnsAnyURI()
    {
        // Act
        var result = XacmlDataTypeMap.InferDataType(new Uri("https://example.com"));

        // Assert
        result.ShouldBe(XACMLDataTypes.AnyURI);
    }

    [Fact]
    public void InferDataType_ByteArray_ReturnsBase64Binary()
    {
        // Act
        var result = XacmlDataTypeMap.InferDataType(new byte[] { 1, 2, 3 });

        // Assert
        result.ShouldBe(XACMLDataTypes.Base64Binary);
    }

    [Fact]
    public void IsKnownDataType_String_ReturnsTrue()
    {
        // Act
        var result = XacmlDataTypeMap.IsKnownDataType(XACMLDataTypes.String);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsKnownDataType_Unknown_ReturnsFalse()
    {
        // Act
        var result = XacmlDataTypeMap.IsKnownDataType("urn:custom:unknown");

        // Assert
        result.ShouldBeFalse();
    }

    #endregion
}
