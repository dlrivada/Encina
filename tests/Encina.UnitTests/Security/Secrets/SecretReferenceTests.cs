using Encina.Security.Secrets;
using Shouldly;

namespace Encina.UnitTests.Security.Secrets;

public sealed class SecretReferenceTests
{
    [Fact]
    public void Name_IsRequired()
    {
        var reference = new SecretReference { Name = "my-secret" };

        reference.Name.ShouldBe("my-secret");
    }

    [Fact]
    public void Version_DefaultsToNull()
    {
        var reference = new SecretReference { Name = "s" };

        reference.Version.ShouldBeNull();
    }

    [Fact]
    public void CacheDuration_DefaultsToNull()
    {
        var reference = new SecretReference { Name = "s" };

        reference.CacheDuration.ShouldBeNull();
    }

    [Fact]
    public void AutoRotate_DefaultsToFalse()
    {
        var reference = new SecretReference { Name = "s" };

        reference.AutoRotate.ShouldBeFalse();
    }

    [Fact]
    public void RotationInterval_DefaultsToNull()
    {
        var reference = new SecretReference { Name = "s" };

        reference.RotationInterval.ShouldBeNull();
    }

    [Fact]
    public void AllProperties_AreSettable()
    {
        var reference = new SecretReference
        {
            Name = "db-password",
            Version = "v2",
            CacheDuration = TimeSpan.FromMinutes(15),
            AutoRotate = true,
            RotationInterval = TimeSpan.FromHours(24)
        };

        reference.Name.ShouldBe("db-password");
        reference.Version.ShouldBe("v2");
        reference.CacheDuration.ShouldBe(TimeSpan.FromMinutes(15));
        reference.AutoRotate.ShouldBeTrue();
        reference.RotationInterval.ShouldBe(TimeSpan.FromHours(24));
    }

    [Fact]
    public void Record_SupportsValueEquality()
    {
        var a = new SecretReference { Name = "secret-1", Version = "v1" };
        var b = new SecretReference { Name = "secret-1", Version = "v1" };

        a.ShouldBe(b);
    }

    [Fact]
    public void Record_DifferentValues_AreNotEqual()
    {
        var a = new SecretReference { Name = "secret-1", Version = "v1" };
        var b = new SecretReference { Name = "secret-1", Version = "v2" };

        a.ShouldNotBe(b);
    }

    [Fact]
    public void Record_SupportsWithExpression()
    {
        var original = new SecretReference
        {
            Name = "secret-1",
            Version = "v1",
            AutoRotate = false
        };

        var modified = original with { AutoRotate = true };

        modified.Name.ShouldBe("secret-1");
        modified.Version.ShouldBe("v1");
        modified.AutoRotate.ShouldBeTrue();
    }
}
