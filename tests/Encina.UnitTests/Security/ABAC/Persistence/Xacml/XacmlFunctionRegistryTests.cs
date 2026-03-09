using Encina.Security.ABAC;
using Encina.Security.ABAC.Persistence.Xacml;

using FluentAssertions;

namespace Encina.UnitTests.Security.ABAC.Persistence.Xacml;

/// <summary>
/// Unit tests for <see cref="XacmlFunctionRegistry"/>: verifies bidirectional mapping
/// between Encina short function IDs and full XACML 3.0 URNs.
/// </summary>
public sealed class XacmlFunctionRegistryTests
{
    // ── ToUrn (Short ID → URN) ──────────────────────────────────────

    #region ToUrn

    [Theory]
    [InlineData(XACMLFunctionIds.StringEqual, "urn:oasis:names:tc:xacml:1.0:function:string-equal")]
    [InlineData(XACMLFunctionIds.BooleanEqual, "urn:oasis:names:tc:xacml:1.0:function:boolean-equal")]
    [InlineData(XACMLFunctionIds.IntegerEqual, "urn:oasis:names:tc:xacml:1.0:function:integer-equal")]
    [InlineData(XACMLFunctionIds.DoubleEqual, "urn:oasis:names:tc:xacml:1.0:function:double-equal")]
    [InlineData(XACMLFunctionIds.And, "urn:oasis:names:tc:xacml:1.0:function:and")]
    [InlineData(XACMLFunctionIds.Or, "urn:oasis:names:tc:xacml:1.0:function:or")]
    [InlineData(XACMLFunctionIds.Not, "urn:oasis:names:tc:xacml:1.0:function:not")]
    public void ToUrn_V1Function_ReturnsV1Urn(string shortId, string expectedUrn)
    {
        // Act
        var result = XacmlFunctionRegistry.ToUrn(shortId);

        // Assert
        result.Should().Be(expectedUrn);
    }

    [Theory]
    [InlineData(XACMLFunctionIds.StringStartsWith, "urn:oasis:names:tc:xacml:3.0:function:string-starts-with")]
    [InlineData(XACMLFunctionIds.StringEndsWith, "urn:oasis:names:tc:xacml:3.0:function:string-ends-with")]
    [InlineData(XACMLFunctionIds.StringContains, "urn:oasis:names:tc:xacml:3.0:function:string-contains")]
    [InlineData(XACMLFunctionIds.AnyOfFunc, "urn:oasis:names:tc:xacml:3.0:function:any-of")]
    [InlineData(XACMLFunctionIds.AllOfFunc, "urn:oasis:names:tc:xacml:3.0:function:all-of")]
    [InlineData(XACMLFunctionIds.Map, "urn:oasis:names:tc:xacml:3.0:function:map")]
    public void ToUrn_V3Function_ReturnsV3Urn(string shortId, string expectedUrn)
    {
        // Act
        var result = XacmlFunctionRegistry.ToUrn(shortId);

        // Assert
        result.Should().Be(expectedUrn);
    }

    [Fact]
    public void ToUrn_AlreadyUrn_PassesThrough()
    {
        // Arrange
        const string urn = "urn:custom:function:my-function";

        // Act
        var result = XacmlFunctionRegistry.ToUrn(urn);

        // Assert
        result.Should().Be(urn);
    }

    [Fact]
    public void ToUrn_UnknownShortId_PassesThrough()
    {
        // Arrange
        const string unknown = "unknown-custom-function";

        // Act
        var result = XacmlFunctionRegistry.ToUrn(unknown);

        // Assert
        result.Should().Be(unknown);
    }

    #endregion

    // ── ToShortId (URN → Short ID) ──────────────────────────────────

    #region ToShortId

    [Theory]
    [InlineData("urn:oasis:names:tc:xacml:1.0:function:string-equal", XACMLFunctionIds.StringEqual)]
    [InlineData("urn:oasis:names:tc:xacml:1.0:function:and", XACMLFunctionIds.And)]
    [InlineData("urn:oasis:names:tc:xacml:1.0:function:or", XACMLFunctionIds.Or)]
    public void ToShortId_KnownV1Urn_ReturnsShortId(string urn, string expectedShortId)
    {
        // Act
        var result = XacmlFunctionRegistry.ToShortId(urn);

        // Assert
        result.Should().Be(expectedShortId);
    }

    [Theory]
    [InlineData("urn:oasis:names:tc:xacml:3.0:function:string-starts-with", XACMLFunctionIds.StringStartsWith)]
    [InlineData("urn:oasis:names:tc:xacml:3.0:function:any-of", XACMLFunctionIds.AnyOfFunc)]
    [InlineData("urn:oasis:names:tc:xacml:3.0:function:map", XACMLFunctionIds.Map)]
    public void ToShortId_KnownV3Urn_ReturnsShortId(string urn, string expectedShortId)
    {
        // Act
        var result = XacmlFunctionRegistry.ToShortId(urn);

        // Assert
        result.Should().Be(expectedShortId);
    }

    [Fact]
    public void ToShortId_UnknownUrn_PassesThrough()
    {
        // Arrange
        const string unknown = "urn:custom:function:unknown";

        // Act
        var result = XacmlFunctionRegistry.ToShortId(unknown);

        // Assert
        result.Should().Be(unknown);
    }

    #endregion

    // ── IsKnownUrn ──────────────────────────────────────────────────

    #region IsKnownUrn

    [Fact]
    public void IsKnownUrn_V1Function_ReturnsTrue()
    {
        // Act
        var result = XacmlFunctionRegistry.IsKnownUrn("urn:oasis:names:tc:xacml:1.0:function:string-equal");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsKnownUrn_V3Function_ReturnsTrue()
    {
        // Act
        var result = XacmlFunctionRegistry.IsKnownUrn("urn:oasis:names:tc:xacml:3.0:function:string-starts-with");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsKnownUrn_UnknownUrn_ReturnsFalse()
    {
        // Act
        var result = XacmlFunctionRegistry.IsKnownUrn("urn:custom:unknown");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    // ── Registry Symmetry ───────────────────────────────────────────

    #region Registry Symmetry

    [Fact]
    public void ShortIdToUrn_And_UrnToShortId_HaveSameCount()
    {
        // Assert — Both dictionaries must have the same number of entries
        XacmlFunctionRegistry.ShortIdToUrn.Count
            .Should().Be(XacmlFunctionRegistry.UrnToShortId.Count);
    }

    [Fact]
    public void AllShortIds_RoundTrip_Through_UrnAndBack()
    {
        // Act & Assert — Every short ID should map to a URN and back
        foreach (var (shortId, urn) in XacmlFunctionRegistry.ShortIdToUrn)
        {
            var roundTripped = XacmlFunctionRegistry.ToShortId(urn);
            roundTripped.Should().Be(shortId, $"Round-trip failed for short ID '{shortId}'");
        }
    }

    [Fact]
    public void AllUrns_RoundTrip_Through_ShortIdAndBack()
    {
        // Act & Assert — Every URN should map to a short ID and back
        foreach (var (urn, shortId) in XacmlFunctionRegistry.UrnToShortId)
        {
            var roundTripped = XacmlFunctionRegistry.ToUrn(shortId);
            roundTripped.Should().Be(urn, $"Round-trip failed for URN '{urn}'");
        }
    }

    #endregion
}
