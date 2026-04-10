using Encina.Compliance.LawfulBasis;

namespace Encina.UnitTests.Compliance.LawfulBasisModule;

/// <summary>
/// Unit tests for <see cref="LawfulBasisEnforcementMode"/> enum.
/// </summary>
public class LawfulBasisEnforcementModeTests
{
    [Fact]
    public void Block_HasExpectedValue()
    {
        ((int)LawfulBasisEnforcementMode.Block).ShouldBe(0);
    }

    [Fact]
    public void Warn_HasExpectedValue()
    {
        ((int)LawfulBasisEnforcementMode.Warn).ShouldBe(1);
    }

    [Fact]
    public void Disabled_HasExpectedValue()
    {
        ((int)LawfulBasisEnforcementMode.Disabled).ShouldBe(2);
    }

    [Fact]
    public void EnumGetValues_HasThreeModes()
    {
        var values = Enum.GetValues<LawfulBasisEnforcementMode>();
        values.Length.ShouldBe(3);
    }

    [Theory]
    [InlineData(LawfulBasisEnforcementMode.Block)]
    [InlineData(LawfulBasisEnforcementMode.Warn)]
    [InlineData(LawfulBasisEnforcementMode.Disabled)]
    public void AllModes_AreDefined(LawfulBasisEnforcementMode mode)
    {
        Enum.IsDefined(mode).ShouldBeTrue();
    }

    [Fact]
    public void UndefinedMode_IsNotDefined()
    {
        Enum.IsDefined((LawfulBasisEnforcementMode)999).ShouldBeFalse();
    }

    [Fact]
    public void DefaultEnumValue_IsBlock()
    {
        default(LawfulBasisEnforcementMode).ShouldBe(LawfulBasisEnforcementMode.Block);
    }
}
