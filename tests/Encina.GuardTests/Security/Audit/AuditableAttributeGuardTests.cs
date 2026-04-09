using Encina.Security.Audit;
using FluentAssertions;

namespace Encina.GuardTests.Security.Audit;

/// <summary>
/// Guard clause tests for <see cref="AuditableAttribute"/>.
/// Verifies attribute construction and property defaults.
/// </summary>
public class AuditableAttributeGuardTests
{
    [Fact]
    public void Constructor_DefaultValues_AllNull()
    {
        var attribute = new AuditableAttribute();

        attribute.EntityType.Should().BeNull();
        attribute.Action.Should().BeNull();
        attribute.SensitivityLevel.Should().BeNull();
        attribute.Skip.Should().BeFalse();
        attribute.IncludePayload.Should().BeNull();
        attribute.SensitiveFields.Should().BeNull();
    }

    [Fact]
    public void Skip_SetToTrue_ReturnsTrue()
    {
        var attribute = new AuditableAttribute { Skip = true };

        attribute.Skip.Should().BeTrue();
    }

    [Fact]
    public void IncludePayloadValue_SetToFalse_ReturnsNonNullFalse()
    {
        var attribute = new AuditableAttribute { IncludePayloadValue = false };

        attribute.IncludePayload.Should().NotBeNull();
        attribute.IncludePayload.Should().BeFalse();
    }

    [Fact]
    public void IncludePayloadValue_SetToTrue_ReturnsNonNullTrue()
    {
        var attribute = new AuditableAttribute { IncludePayloadValue = true };

        attribute.IncludePayload.Should().NotBeNull();
        attribute.IncludePayload.Should().BeTrue();
    }

    [Fact]
    public void SensitiveFields_SetToArray_ReturnsArray()
    {
        var fields = new[] { "Diagnosis", "SSN" };
        var attribute = new AuditableAttribute { SensitiveFields = fields };

        attribute.SensitiveFields.Should().BeSameAs(fields);
    }

    [Fact]
    public void EntityTypeAndAction_SetValues_ReturnCorrectly()
    {
        var attribute = new AuditableAttribute
        {
            EntityType = "Invoice",
            Action = "Generate"
        };

        attribute.EntityType.Should().Be("Invoice");
        attribute.Action.Should().Be("Generate");
    }
}
