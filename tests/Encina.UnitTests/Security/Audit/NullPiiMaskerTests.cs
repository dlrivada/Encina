using Encina.Security.Audit;
using Shouldly;

namespace Encina.UnitTests.Security.Audit;

/// <summary>
/// Unit tests for <see cref="NullPiiMasker"/>.
/// </summary>
public class NullPiiMaskerTests
{
    private readonly NullPiiMasker _masker = new();

    [Fact]
    public void MaskForAudit_Generic_ShouldReturnInputUnchanged()
    {
        // Arrange
        var input = new TestRequest { Name = "John Doe", Email = "john@example.com" };

        // Act
        var result = _masker.MaskForAudit(input);

        // Assert
        result.ShouldBeSameAs(input);
        result.Name.ShouldBe("John Doe");
        result.Email.ShouldBe("john@example.com");
    }

    [Fact]
    public void MaskForAudit_NonGeneric_ShouldReturnInputUnchanged()
    {
        // Arrange
        object input = new TestRequest { Name = "Jane Doe", Email = "jane@example.com" };

        // Act
        var result = _masker.MaskForAudit(input);

        // Assert
        result.ShouldBeSameAs(input);
    }

    [Fact]
    public void MaskForAudit_Generic_WithString_ShouldReturnSameString()
    {
        // Arrange
        var input = "sensitive data";

        // Act
        var result = _masker.MaskForAudit(input);

        // Assert
        result.ShouldBe("sensitive data");
    }

    [Fact]
    public void MaskForAudit_Generic_WithInt_ShouldReturnSameValue()
    {
        // Arrange
        var input = 12345;

        // Act
        var result = _masker.MaskForAudit(input);

        // Assert
        result.ShouldBe(12345);
    }

    [Fact]
    public void MaskForAudit_Generic_WithGuid_ShouldReturnSameValue()
    {
        // Arrange
        var input = Guid.NewGuid();

        // Act
        var result = _masker.MaskForAudit(input);

        // Assert
        result.ShouldBe(input);
    }

    [Fact]
    public void MaskForAudit_NonGeneric_WithString_ShouldReturnSameString()
    {
        // Arrange
        object input = "sensitive data";

        // Act
        var result = _masker.MaskForAudit(input);

        // Assert
        result.ShouldBe("sensitive data");
    }

    [Fact]
    public void MaskForAudit_Generic_WithCollection_ShouldReturnSameCollection()
    {
        // Arrange
        var input = new List<string> { "item1", "item2", "item3" };

        // Act
        var result = _masker.MaskForAudit(input);

        // Assert
        result.ShouldBeSameAs(input);
        result.Count.ShouldBe(3);
    }

    [Fact]
    public void MaskForAudit_Generic_WithAnonymousType_ShouldReturnSameObject()
    {
        // Arrange
        var input = new { Name = "Test", Value = 42 };

        // Act
        var result = _masker.MaskForAudit(input);

        // Assert
        result.ShouldBeSameAs(input);
        result.Name.ShouldBe("Test");
        result.Value.ShouldBe(42);
    }

    [Fact]
    public void Instance_ShouldImplementIPiiMasker()
    {
        // Assert
        _masker.ShouldBeAssignableTo<IPiiMasker>();
    }

    private sealed class TestRequest
    {
        public string? Name { get; init; }
        public string? Email { get; init; }
    }
}
