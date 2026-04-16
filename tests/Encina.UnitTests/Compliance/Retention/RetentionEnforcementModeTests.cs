using Encina.Compliance.Retention;

using Shouldly;

namespace Encina.UnitTests.Compliance.Retention;

/// <summary>
/// Unit tests for <see cref="RetentionEnforcementMode"/> enum covering all values.
/// </summary>
public sealed class RetentionEnforcementModeTests
{
    [Fact]
    public void Block_IsDefault_HasValueZero()
    {
        RetentionEnforcementMode.Block.ShouldBe((RetentionEnforcementMode)0);
    }

    [Fact]
    public void Warn_HasValueOne()
    {
        RetentionEnforcementMode.Warn.ShouldBe((RetentionEnforcementMode)1);
    }

    [Fact]
    public void Disabled_HasValueTwo()
    {
        RetentionEnforcementMode.Disabled.ShouldBe((RetentionEnforcementMode)2);
    }

    [Fact]
    public void IsDefined_AllValues_ReturnsTrue()
    {
        Enum.IsDefined(RetentionEnforcementMode.Block).ShouldBeTrue();
        Enum.IsDefined(RetentionEnforcementMode.Warn).ShouldBeTrue();
        Enum.IsDefined(RetentionEnforcementMode.Disabled).ShouldBeTrue();
    }

    [Fact]
    public void IsDefined_InvalidValue_ReturnsFalse()
    {
        Enum.IsDefined((RetentionEnforcementMode)99).ShouldBeFalse();
    }

    [Theory]
    [InlineData(RetentionEnforcementMode.Block, "Block")]
    [InlineData(RetentionEnforcementMode.Warn, "Warn")]
    [InlineData(RetentionEnforcementMode.Disabled, "Disabled")]
    public void ToString_ReturnsExpectedName(RetentionEnforcementMode mode, string expected)
    {
        mode.ToString().ShouldBe(expected);
    }
}
