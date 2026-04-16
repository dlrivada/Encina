using Encina.Security.ABAC;
using Encina.Security.ABAC.Persistence.Xacml;

using Shouldly;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.GuardTests.Security.ABAC.Persistence.Xacml;

/// <summary>
/// Guard clause tests for <see cref="XacmlXmlPolicySerializer"/>.
/// Verifies that null and invalid arguments are properly rejected.
/// </summary>
public sealed class XacmlXmlPolicySerializerGuardTests
{
    private readonly XacmlXmlPolicySerializer _sut =
        new(NullLoggerFactory.Instance.CreateLogger<XacmlXmlPolicySerializer>());

    // ── Constructor Guards ───────────────────────────────────────────

    #region Constructor Guards

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new XacmlXmlPolicySerializer(null!);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("logger");
    }

    #endregion

    // ── Serialize Guards ────────────────────────────────────────────

    #region Serialize Guards

    [Fact]
    public void Serialize_NullPolicySet_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _sut.Serialize((PolicySet)null!);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("policySet");
    }

    [Fact]
    public void Serialize_NullPolicy_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _sut.Serialize((Policy)null!);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("policy");
    }

    #endregion

    // ── XacmlMappingExtensions Guards ────────────────────────────────

    #region XacmlMappingExtensions Guards

    [Fact]
    public void ToXacmlUrn_InvalidAttributeCategory_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var invalid = (AttributeCategory)999;

        // Act
        var act = (Action)(() => invalid.ToXacmlUrn());

        // Assert
        Should.Throw<ArgumentOutOfRangeException>(act)
            .ParamName.ShouldBe("category");
    }

    [Fact]
    public void ToAttributeCategory_UnknownUrn_ThrowsArgumentException()
    {
        // Act
        var act = (Action)(() => XacmlMappingExtensions.ToAttributeCategory("urn:unknown"));

        // Assert
        Should.Throw<ArgumentException>(act)
            .ParamName.ShouldBe("urn");
    }

    [Fact]
    public void ToXacmlUrn_InvalidCombiningAlgorithm_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var invalid = (CombiningAlgorithmId)999;

        // Act
        var act = (Action)(() => invalid.ToXacmlUrn(isRuleCombining: true));

        // Assert
        Should.Throw<ArgumentOutOfRangeException>(act)
            .ParamName.ShouldBe("algorithm");
    }

    [Fact]
    public void ToCombiningAlgorithmId_UnknownSuffix_ThrowsArgumentException()
    {
        // Act
        var act = (Action)(() => XacmlMappingExtensions.ToCombiningAlgorithmId(
            "urn:oasis:names:tc:xacml:3.0:rule-combining-algorithm:nonexistent"));

        // Assert
        Should.Throw<ArgumentException>(act)
            .ParamName.ShouldBe("urn");
    }

    [Fact]
    public void ToCombiningAlgorithmId_NoColonSuffix_ThrowsArgumentException()
    {
        // Act
        var act = (Action)(() => XacmlMappingExtensions.ToCombiningAlgorithmId("nocolons"));

        // Assert
        Should.Throw<ArgumentException>(act)
            .ParamName.ShouldBe("urn");
    }

    [Fact]
    public void ToXacmlString_NotApplicableEffect_ThrowsArgumentOutOfRangeException()
    {
        // Act
        var act = (Action)(() => Effect.NotApplicable.ToXacmlString());

        // Assert
        Should.Throw<ArgumentOutOfRangeException>(act)
            .ParamName.ShouldBe("effect");
    }

    [Fact]
    public void ToXacmlString_IndeterminateEffect_ThrowsArgumentOutOfRangeException()
    {
        // Act
        var act = (Action)(() => Effect.Indeterminate.ToXacmlString());

        // Assert
        Should.Throw<ArgumentOutOfRangeException>(act)
            .ParamName.ShouldBe("effect");
    }

    [Fact]
    public void ToEffect_UnknownString_ThrowsArgumentException()
    {
        // Act
        var act = (Action)(() => XacmlMappingExtensions.ToEffect("NotAnEffect"));

        // Assert
        Should.Throw<ArgumentException>(act)
            .ParamName.ShouldBe("value");
    }

    [Fact]
    public void ToXacmlString_InvalidFulfillOn_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var invalid = (FulfillOn)999;

        // Act
        var act = (Action)(() => invalid.ToXacmlString());

        // Assert
        Should.Throw<ArgumentOutOfRangeException>(act)
            .ParamName.ShouldBe("fulfillOn");
    }

    [Fact]
    public void ToFulfillOn_UnknownString_ThrowsArgumentException()
    {
        // Act
        var act = (Action)(() => XacmlMappingExtensions.ToFulfillOn("NotAValue"));

        // Assert
        Should.Throw<ArgumentException>(act)
            .ParamName.ShouldBe("value");
    }

    #endregion
}
