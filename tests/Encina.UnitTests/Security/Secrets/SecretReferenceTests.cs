using Encina.Security.Secrets;
using FluentAssertions;

namespace Encina.UnitTests.Security.Secrets;

public sealed class SecretReferenceTests
{
    [Fact]
    public void Name_IsRequired()
    {
        var reference = new SecretReference { Name = "my-secret" };

        reference.Name.Should().Be("my-secret");
    }

    [Fact]
    public void Version_DefaultsToNull()
    {
        var reference = new SecretReference { Name = "s" };

        reference.Version.Should().BeNull();
    }

    [Fact]
    public void CacheDuration_DefaultsToNull()
    {
        var reference = new SecretReference { Name = "s" };

        reference.CacheDuration.Should().BeNull();
    }

    [Fact]
    public void AutoRotate_DefaultsToFalse()
    {
        var reference = new SecretReference { Name = "s" };

        reference.AutoRotate.Should().BeFalse();
    }

    [Fact]
    public void RotationInterval_DefaultsToNull()
    {
        var reference = new SecretReference { Name = "s" };

        reference.RotationInterval.Should().BeNull();
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

        reference.Name.Should().Be("db-password");
        reference.Version.Should().Be("v2");
        reference.CacheDuration.Should().Be(TimeSpan.FromMinutes(15));
        reference.AutoRotate.Should().BeTrue();
        reference.RotationInterval.Should().Be(TimeSpan.FromHours(24));
    }

    [Fact]
    public void Record_SupportsValueEquality()
    {
        var a = new SecretReference { Name = "secret-1", Version = "v1" };
        var b = new SecretReference { Name = "secret-1", Version = "v1" };

        a.Should().Be(b);
    }

    [Fact]
    public void Record_DifferentValues_AreNotEqual()
    {
        var a = new SecretReference { Name = "secret-1", Version = "v1" };
        var b = new SecretReference { Name = "secret-1", Version = "v2" };

        a.Should().NotBe(b);
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

        modified.Name.Should().Be("secret-1");
        modified.Version.Should().Be("v1");
        modified.AutoRotate.Should().BeTrue();
    }
}
