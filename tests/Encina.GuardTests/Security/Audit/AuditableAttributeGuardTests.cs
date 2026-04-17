using Encina.Security.Audit;
using Shouldly;

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

        attribute.EntityType.ShouldBeNull();
        attribute.Action.ShouldBeNull();
        attribute.SensitivityLevel.ShouldBeNull();
        attribute.Skip.ShouldBeFalse();
        attribute.IncludePayload.ShouldBeNull();
        attribute.SensitiveFields.ShouldBeNull();
    }

    [Fact]
    public void Skip_SetToTrue_ReturnsTrue()
    {
        var attribute = new AuditableAttribute { Skip = true };

        attribute.Skip.ShouldBeTrue();
    }

    [Fact]
    public void IncludePayloadValue_SetToFalse_ReturnsNonNullFalse()
    {
        var attribute = new AuditableAttribute { IncludePayloadValue = false };

        attribute.IncludePayload.ShouldNotBeNull();
        attribute.IncludePayload.Value.ShouldBeFalse();
    }

    [Fact]
    public void IncludePayloadValue_SetToTrue_ReturnsNonNullTrue()
    {
        var attribute = new AuditableAttribute { IncludePayloadValue = true };

        attribute.IncludePayload.ShouldNotBeNull();
        attribute.IncludePayload.Value.ShouldBeTrue();
    }

    [Fact]
    public void SensitiveFields_SetToArray_ReturnsArray()
    {
        var fields = new[] { "Diagnosis", "SSN" };
        var attribute = new AuditableAttribute { SensitiveFields = fields };

        attribute.SensitiveFields.ShouldBeSameAs(fields);
    }

    [Fact]
    public void EntityTypeAndAction_SetValues_ReturnCorrectly()
    {
        var attribute = new AuditableAttribute
        {
            EntityType = "Invoice",
            Action = "Generate"
        };

        attribute.EntityType.ShouldBe("Invoice");
        attribute.Action.ShouldBe("Generate");
    }
}
