using Encina.Compliance.Retention;

using FluentAssertions;

namespace Encina.UnitTests.Compliance.Retention;

/// <summary>
/// Unit tests for <see cref="RetentionEnforcementMode"/> enum covering all values.
/// </summary>
public sealed class RetentionEnforcementModeTests
{
    [Fact]
    public void Block_IsDefault_HasValueZero()
    {
        RetentionEnforcementMode.Block.Should().Be((RetentionEnforcementMode)0);
    }

    [Fact]
    public void Warn_HasValueOne()
    {
        RetentionEnforcementMode.Warn.Should().Be((RetentionEnforcementMode)1);
    }

    [Fact]
    public void Disabled_HasValueTwo()
    {
        RetentionEnforcementMode.Disabled.Should().Be((RetentionEnforcementMode)2);
    }

    [Fact]
    public void IsDefined_AllValues_ReturnsTrue()
    {
        Enum.IsDefined(RetentionEnforcementMode.Block).Should().BeTrue();
        Enum.IsDefined(RetentionEnforcementMode.Warn).Should().BeTrue();
        Enum.IsDefined(RetentionEnforcementMode.Disabled).Should().BeTrue();
    }

    [Fact]
    public void IsDefined_InvalidValue_ReturnsFalse()
    {
        Enum.IsDefined((RetentionEnforcementMode)99).Should().BeFalse();
    }

    [Theory]
    [InlineData(RetentionEnforcementMode.Block, "Block")]
    [InlineData(RetentionEnforcementMode.Warn, "Warn")]
    [InlineData(RetentionEnforcementMode.Disabled, "Disabled")]
    public void ToString_ReturnsExpectedName(RetentionEnforcementMode mode, string expected)
    {
        mode.ToString().Should().Be(expected);
    }
}
