using Encina.Security.Secrets;
using FluentAssertions;

namespace Encina.UnitTests.Security.Secrets;

public sealed class InjectSecretAttributeTests
{
    #region Constructor

    [Fact]
    public void Constructor_SetsSecretName()
    {
        var attribute = new InjectSecretAttribute("my-secret");

        attribute.SecretName.Should().Be("my-secret");
    }

    [Fact]
    public void Constructor_NullSecretName_ThrowsArgumentNullException()
    {
        var act = () => new InjectSecretAttribute(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("secretName");
    }

    #endregion

    #region Default Values

    [Fact]
    public void Version_DefaultsToNull()
    {
        var attribute = new InjectSecretAttribute("key");

        attribute.Version.Should().BeNull();
    }

    [Fact]
    public void FailOnError_DefaultsToTrue()
    {
        var attribute = new InjectSecretAttribute("key");

        attribute.FailOnError.Should().BeTrue();
    }

    #endregion

    #region Property Setters

    [Fact]
    public void Version_CanBeSet()
    {
        var attribute = new InjectSecretAttribute("key") { Version = "v2" };

        attribute.Version.Should().Be("v2");
    }

    [Fact]
    public void FailOnError_CanBeSetToFalse()
    {
        var attribute = new InjectSecretAttribute("key") { FailOnError = false };

        attribute.FailOnError.Should().BeFalse();
    }

    #endregion

    #region AttributeUsage

    [Fact]
    public void AttributeUsage_TargetsPropertyOnly()
    {
        var usage = typeof(InjectSecretAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        usage.ValidOn.Should().Be(AttributeTargets.Property);
    }

    [Fact]
    public void AttributeUsage_AllowMultiple_IsFalse()
    {
        var usage = typeof(InjectSecretAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        usage.AllowMultiple.Should().BeFalse();
    }

    [Fact]
    public void AttributeUsage_Inherited_IsTrue()
    {
        var usage = typeof(InjectSecretAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        usage.Inherited.Should().BeTrue();
    }

    #endregion
}
