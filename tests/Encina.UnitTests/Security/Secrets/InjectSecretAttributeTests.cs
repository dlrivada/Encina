using Encina.Security.Secrets;
using Shouldly;

namespace Encina.UnitTests.Security.Secrets;

public sealed class InjectSecretAttributeTests
{
    #region Constructor

    [Fact]
    public void Constructor_SetsSecretName()
    {
        var attribute = new InjectSecretAttribute("my-secret");

        attribute.SecretName.ShouldBe("my-secret");
    }

    [Fact]
    public void Constructor_NullSecretName_ThrowsArgumentNullException()
    {
        var act = () => new InjectSecretAttribute(null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("secretName");
    }

    #endregion

    #region Default Values

    [Fact]
    public void Version_DefaultsToNull()
    {
        var attribute = new InjectSecretAttribute("key");

        attribute.Version.ShouldBeNull();
    }

    [Fact]
    public void FailOnError_DefaultsToTrue()
    {
        var attribute = new InjectSecretAttribute("key");

        attribute.FailOnError.ShouldBeTrue();
    }

    #endregion

    #region Property Setters

    [Fact]
    public void Version_CanBeSet()
    {
        var attribute = new InjectSecretAttribute("key") { Version = "v2" };

        attribute.Version.ShouldBe("v2");
    }

    [Fact]
    public void FailOnError_CanBeSetToFalse()
    {
        var attribute = new InjectSecretAttribute("key") { FailOnError = false };

        attribute.FailOnError.ShouldBeFalse();
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

        usage.ValidOn.ShouldBe(AttributeTargets.Property);
    }

    [Fact]
    public void AttributeUsage_AllowMultiple_IsFalse()
    {
        var usage = typeof(InjectSecretAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        usage.AllowMultiple.ShouldBeFalse();
    }

    [Fact]
    public void AttributeUsage_Inherited_IsTrue()
    {
        var usage = typeof(InjectSecretAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        usage.Inherited.ShouldBeTrue();
    }

    #endregion
}
