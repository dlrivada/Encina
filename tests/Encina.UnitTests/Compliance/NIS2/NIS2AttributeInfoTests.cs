using Encina.Compliance.NIS2;
using Encina.Compliance.NIS2.Model;

using FluentAssertions;

namespace Encina.UnitTests.Compliance.NIS2;

/// <summary>
/// Unit tests for <see cref="NIS2AttributeInfo"/>.
/// </summary>
public class NIS2AttributeInfoTests
{
    #region Test Types

    private sealed record NoAttributeType;

    [NIS2Critical(Description = "Test critical")]
    private sealed record CriticalOnlyType;

    [RequireMFA(Reason = "Sensitive")]
    private sealed record MFAOnlyType;

    [NIS2SupplyChainCheck("sup-1")]
    [NIS2SupplyChainCheck("sup-2")]
    private sealed record MultiSupplyChainType;

    [NIS2Critical]
    [RequireMFA]
    [NIS2SupplyChainCheck("sup-1")]
    private sealed record FullyDecoratedType;

    #endregion

    #region FromType_NoAttributes

    [Fact]
    public void FromType_NoAttributes_HasAnyAttribute_ShouldBeFalse()
    {
        // Act
        var info = NIS2AttributeInfo.FromType(typeof(NoAttributeType));

        // Assert
        info.HasAnyAttribute.Should().BeFalse();
    }

    #endregion

    #region FromType_CriticalOnly

    [Fact]
    public void FromType_CriticalOnly_ShouldSetIsNIS2Critical()
    {
        // Act
        var info = NIS2AttributeInfo.FromType(typeof(CriticalOnlyType));

        // Assert
        info.IsNIS2Critical.Should().BeTrue();
    }

    [Fact]
    public void FromType_CriticalOnly_ShouldCaptureDescription()
    {
        // Act
        var info = NIS2AttributeInfo.FromType(typeof(CriticalOnlyType));

        // Assert
        info.CriticalDescription.Should().Be("Test critical");
    }

    #endregion

    #region FromType_MFAOnly

    [Fact]
    public void FromType_MFAOnly_ShouldSetRequiresMFA()
    {
        // Act
        var info = NIS2AttributeInfo.FromType(typeof(MFAOnlyType));

        // Assert
        info.RequiresMFA.Should().BeTrue();
    }

    [Fact]
    public void FromType_MFAOnly_ShouldCaptureReason()
    {
        // Act
        var info = NIS2AttributeInfo.FromType(typeof(MFAOnlyType));

        // Assert
        info.MFAReason.Should().Be("Sensitive");
    }

    #endregion

    #region FromType_MultiSupplyChain

    [Fact]
    public void FromType_MultiSupplyChain_ShouldCaptureAllSupplierIds()
    {
        // Act
        var info = NIS2AttributeInfo.FromType(typeof(MultiSupplyChainType));

        // Assert
        info.SupplyChainChecks.Should().HaveCount(2);
        info.SupplyChainChecks.Should().Contain("sup-1");
        info.SupplyChainChecks.Should().Contain("sup-2");
    }

    #endregion

    #region FromType_FullyDecorated

    [Fact]
    public void FromType_FullyDecorated_HasAnyAttribute_ShouldBeTrue()
    {
        // Act
        var info = NIS2AttributeInfo.FromType(typeof(FullyDecoratedType));

        // Assert
        info.HasAnyAttribute.Should().BeTrue();
    }

    [Fact]
    public void FromType_FullyDecorated_ShouldHaveAllFlags()
    {
        // Act
        var info = NIS2AttributeInfo.FromType(typeof(FullyDecoratedType));

        // Assert
        info.IsNIS2Critical.Should().BeTrue();
        info.RequiresMFA.Should().BeTrue();
        info.SupplyChainChecks.Should().HaveCount(1);
        info.SupplyChainChecks.Should().Contain("sup-1");
    }

    #endregion
}
