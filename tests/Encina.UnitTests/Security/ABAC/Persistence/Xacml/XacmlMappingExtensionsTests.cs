using Encina.Security.ABAC;
using Encina.Security.ABAC.Persistence.Xacml;

using FluentAssertions;

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
        result.Should().Be(expectedUrn);
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
        result.Should().Be(expected);
    }

    [Fact]
    public void ToXacmlUrn_InvalidAttributeCategory_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var invalid = (AttributeCategory)999;

        // Act
        var act = () => invalid.ToXacmlUrn();

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ToAttributeCategory_UnknownUrn_ThrowsArgumentException()
    {
        // Act
        var act = () => XacmlMappingExtensions.ToAttributeCategory("urn:unknown:category");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AttributeCategory_AllValues_RoundTrip()
    {
        // Act & Assert — Every category should map to a URN and back
        foreach (var category in Enum.GetValues<AttributeCategory>())
        {
            var urn = category.ToXacmlUrn();
            var roundTripped = XacmlMappingExtensions.ToAttributeCategory(urn);
            roundTripped.Should().Be(category);
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
        result.Should().Be(expectedUrn);
    }

    [Fact]
    public void ToCombiningAlgorithmId_RuleCombiningUrn_ReturnsCorrectAlgorithm()
    {
        // Act
        var result = XacmlMappingExtensions.ToCombiningAlgorithmId(
            "urn:oasis:names:tc:xacml:3.0:rule-combining-algorithm:deny-overrides");

        // Assert
        result.Should().Be(CombiningAlgorithmId.DenyOverrides);
    }

    [Fact]
    public void ToCombiningAlgorithmId_PolicyCombiningUrn_ReturnsCorrectAlgorithm()
    {
        // Act
        var result = XacmlMappingExtensions.ToCombiningAlgorithmId(
            "urn:oasis:names:tc:xacml:3.0:policy-combining-algorithm:permit-overrides");

        // Assert
        result.Should().Be(CombiningAlgorithmId.PermitOverrides);
    }

    [Fact]
    public void ToCombiningAlgorithmId_UnknownUrn_ThrowsArgumentException()
    {
        // Act
        var act = () => XacmlMappingExtensions.ToCombiningAlgorithmId(
            "urn:oasis:names:tc:xacml:3.0:rule-combining-algorithm:unknown-algorithm");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ToCombiningAlgorithmId_InvalidFormat_ThrowsArgumentException()
    {
        // Act
        var act = () => XacmlMappingExtensions.ToCombiningAlgorithmId("no-colon-at-end:");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CombiningAlgorithmId_AllValues_RoundTrip_RuleCombining()
    {
        // Act & Assert
        foreach (var algorithm in Enum.GetValues<CombiningAlgorithmId>())
        {
            var urn = algorithm.ToXacmlUrn(isRuleCombining: true);
            var roundTripped = XacmlMappingExtensions.ToCombiningAlgorithmId(urn);
            roundTripped.Should().Be(algorithm);
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
            roundTripped.Should().Be(algorithm);
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
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Permit", Effect.Permit)]
    [InlineData("Deny", Effect.Deny)]
    public void ToEffect_KnownString_ReturnsCorrectEffect(string value, Effect expected)
    {
        // Act
        var result = XacmlMappingExtensions.ToEffect(value);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ToXacmlString_NotApplicableEffect_ThrowsArgumentOutOfRangeException()
    {
        // Act
        var act = () => Effect.NotApplicable.ToXacmlString();

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ToEffect_UnknownString_ThrowsArgumentException()
    {
        // Act
        var act = () => XacmlMappingExtensions.ToEffect("Unknown");

        // Assert
        act.Should().Throw<ArgumentException>();
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
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Permit", FulfillOn.Permit)]
    [InlineData("Deny", FulfillOn.Deny)]
    public void ToFulfillOn_KnownString_ReturnsCorrectValue(string value, FulfillOn expected)
    {
        // Act
        var result = XacmlMappingExtensions.ToFulfillOn(value);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ToFulfillOn_UnknownString_ThrowsArgumentException()
    {
        // Act
        var act = () => XacmlMappingExtensions.ToFulfillOn("Unknown");

        // Assert
        act.Should().Throw<ArgumentException>();
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
        result.Should().Be(expected);
    }

    [Fact]
    public void FormatXacmlValue_Integer_ReturnsInvariantString()
    {
        // Act
        var result = XacmlMappingExtensions.FormatXacmlValue(42, XACMLDataTypes.Integer);

        // Assert
        result.Should().Be("42");
    }

    [Fact]
    public void FormatXacmlValue_Double_ReturnsRoundTripFormat()
    {
        // Act
        var result = XacmlMappingExtensions.FormatXacmlValue(3.14, XACMLDataTypes.Double);

        // Assert
        result.Should().Contain("3.14");
    }

    [Fact]
    public void FormatXacmlValue_Null_ReturnsEmptyString()
    {
        // Act
        var result = XacmlMappingExtensions.FormatXacmlValue(null, XACMLDataTypes.String);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void FormatXacmlValue_String_ReturnsToString()
    {
        // Act
        var result = XacmlMappingExtensions.FormatXacmlValue("hello", XACMLDataTypes.String);

        // Assert
        result.Should().Be("hello");
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
        result.Should().Be(expected);
    }

    [Fact]
    public void ParseXacmlValue_Integer_ReturnsLong()
    {
        // Act
        var result = XacmlMappingExtensions.ParseXacmlValue("42", XACMLDataTypes.Integer);

        // Assert
        result.Should().BeOfType<long>();
        result.Should().Be(42L);
    }

    [Fact]
    public void ParseXacmlValue_Double_ReturnsDouble()
    {
        // Act
        var result = XacmlMappingExtensions.ParseXacmlValue("3.14", XACMLDataTypes.Double);

        // Assert
        result.Should().BeOfType<double>();
        ((double)result!).Should().BeApproximately(3.14, 0.001);
    }

    [Fact]
    public void ParseXacmlValue_String_ReturnsAsIs()
    {
        // Act
        var result = XacmlMappingExtensions.ParseXacmlValue("hello", XACMLDataTypes.String);

        // Assert
        result.Should().Be("hello");
    }

    [Fact]
    public void ParseXacmlValue_Null_ReturnsNull()
    {
        // Act
        var result = XacmlMappingExtensions.ParseXacmlValue(null, XACMLDataTypes.String);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ParseXacmlValue_Empty_ReturnsNull()
    {
        // Act
        var result = XacmlMappingExtensions.ParseXacmlValue("", XACMLDataTypes.Integer);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ParseXacmlValue_AnyURI_ReturnsUri()
    {
        // Act
        var result = XacmlMappingExtensions.ParseXacmlValue("https://example.com", XACMLDataTypes.AnyURI);

        // Assert
        result.Should().BeOfType<Uri>();
        ((Uri)result!).ToString().Should().Be("https://example.com/");
    }

    [Fact]
    public void ParseXacmlValue_UnknownDataType_ReturnsRawText()
    {
        // Act
        var result = XacmlMappingExtensions.ParseXacmlValue("raw-value", "urn:custom:datatype");

        // Assert
        result.Should().Be("raw-value");
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
        result.Should().Be(expectedDataType);
    }

    [Fact]
    public void InferDataType_Null_ReturnsString()
    {
        // Act
        var result = XacmlDataTypeMap.InferDataType(null);

        // Assert
        result.Should().Be(XACMLDataTypes.String);
    }

    [Fact]
    public void InferDataType_DateTime_ReturnsDateTimeUri()
    {
        // Act
        var result = XacmlDataTypeMap.InferDataType(DateTime.UtcNow);

        // Assert
        result.Should().Be(XACMLDataTypes.DateTime);
    }

    [Fact]
    public void InferDataType_DateTimeOffset_ReturnsDateTimeUri()
    {
        // Act
        var result = XacmlDataTypeMap.InferDataType(DateTimeOffset.UtcNow);

        // Assert
        result.Should().Be(XACMLDataTypes.DateTime);
    }

    [Fact]
    public void InferDataType_Uri_ReturnsAnyURI()
    {
        // Act
        var result = XacmlDataTypeMap.InferDataType(new Uri("https://example.com"));

        // Assert
        result.Should().Be(XACMLDataTypes.AnyURI);
    }

    [Fact]
    public void InferDataType_ByteArray_ReturnsBase64Binary()
    {
        // Act
        var result = XacmlDataTypeMap.InferDataType(new byte[] { 1, 2, 3 });

        // Assert
        result.Should().Be(XACMLDataTypes.Base64Binary);
    }

    [Fact]
    public void IsKnownDataType_String_ReturnsTrue()
    {
        // Act
        var result = XacmlDataTypeMap.IsKnownDataType(XACMLDataTypes.String);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsKnownDataType_Unknown_ReturnsFalse()
    {
        // Act
        var result = XacmlDataTypeMap.IsKnownDataType("urn:custom:unknown");

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
