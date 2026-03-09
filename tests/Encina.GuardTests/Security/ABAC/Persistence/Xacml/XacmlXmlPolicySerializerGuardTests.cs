using Encina.Security.ABAC;
using Encina.Security.ABAC.Persistence.Xacml;

using FluentAssertions;

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
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
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
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("policySet");
    }

    [Fact]
    public void Serialize_NullPolicy_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _sut.Serialize((Policy)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("policy");
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
        var act = () => invalid.ToXacmlUrn();

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("category");
    }

    [Fact]
    public void ToAttributeCategory_UnknownUrn_ThrowsArgumentException()
    {
        // Act
        var act = () => XacmlMappingExtensions.ToAttributeCategory("urn:unknown");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("urn");
    }

    [Fact]
    public void ToXacmlUrn_InvalidCombiningAlgorithm_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var invalid = (CombiningAlgorithmId)999;

        // Act
        var act = () => invalid.ToXacmlUrn(isRuleCombining: true);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("algorithm");
    }

    [Fact]
    public void ToCombiningAlgorithmId_UnknownSuffix_ThrowsArgumentException()
    {
        // Act
        var act = () => XacmlMappingExtensions.ToCombiningAlgorithmId(
            "urn:oasis:names:tc:xacml:3.0:rule-combining-algorithm:nonexistent");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("urn");
    }

    [Fact]
    public void ToCombiningAlgorithmId_NoColonSuffix_ThrowsArgumentException()
    {
        // Act
        var act = () => XacmlMappingExtensions.ToCombiningAlgorithmId("nocolons");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("urn");
    }

    [Fact]
    public void ToXacmlString_NotApplicableEffect_ThrowsArgumentOutOfRangeException()
    {
        // Act
        var act = () => Effect.NotApplicable.ToXacmlString();

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("effect");
    }

    [Fact]
    public void ToXacmlString_IndeterminateEffect_ThrowsArgumentOutOfRangeException()
    {
        // Act
        var act = () => Effect.Indeterminate.ToXacmlString();

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("effect");
    }

    [Fact]
    public void ToEffect_UnknownString_ThrowsArgumentException()
    {
        // Act
        var act = () => XacmlMappingExtensions.ToEffect("NotAnEffect");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("value");
    }

    [Fact]
    public void ToXacmlString_InvalidFulfillOn_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var invalid = (FulfillOn)999;

        // Act
        var act = () => invalid.ToXacmlString();

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("fulfillOn");
    }

    [Fact]
    public void ToFulfillOn_UnknownString_ThrowsArgumentException()
    {
        // Act
        var act = () => XacmlMappingExtensions.ToFulfillOn("NotAValue");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("value");
    }

    #endregion
}
