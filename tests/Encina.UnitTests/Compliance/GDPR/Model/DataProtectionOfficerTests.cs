using Encina.Compliance.GDPR;
using FluentAssertions;

namespace Encina.UnitTests.Compliance.GDPR.Model;

/// <summary>
/// Unit tests for <see cref="DataProtectionOfficer"/>.
/// </summary>
public class DataProtectionOfficerTests
{
    [Fact]
    public void Constructor_WithAllParameters_ShouldSetProperties()
    {
        // Act
        var dpo = new DataProtectionOfficer("Jane Smith", "dpo@company.com", "+34 600 000 000");

        // Assert
        dpo.Name.Should().Be("Jane Smith");
        dpo.Email.Should().Be("dpo@company.com");
        dpo.Phone.Should().Be("+34 600 000 000");
    }

    [Fact]
    public void Constructor_WithoutPhone_ShouldDefaultToNull()
    {
        // Act
        var dpo = new DataProtectionOfficer("Jane Smith", "dpo@company.com");

        // Assert
        dpo.Phone.Should().BeNull();
    }

    [Fact]
    public void Record_ShouldImplementIDataProtectionOfficer()
    {
        // Act
#pragma warning disable CA1859 // Testing interface contract explicitly
        IDataProtectionOfficer dpo = new DataProtectionOfficer("Jane Smith", "dpo@company.com");
#pragma warning restore CA1859

        // Assert
        dpo.Name.Should().Be("Jane Smith");
        dpo.Email.Should().Be("dpo@company.com");
        dpo.Phone.Should().BeNull();
    }

    [Fact]
    public void Equality_SameValues_ShouldBeEqual()
    {
        // Arrange
        var a = new DataProtectionOfficer("Jane Smith", "dpo@company.com", "+1-555-0100");
        var b = new DataProtectionOfficer("Jane Smith", "dpo@company.com", "+1-555-0100");

        // Assert
        a.Should().Be(b);
    }

    [Fact]
    public void Equality_DifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var a = new DataProtectionOfficer("Jane Smith", "dpo@a.com");
        var b = new DataProtectionOfficer("John Doe", "dpo@b.com");

        // Assert
        a.Should().NotBe(b);
    }
}
