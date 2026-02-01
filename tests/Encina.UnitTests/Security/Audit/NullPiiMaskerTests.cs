using Encina.Security.Audit;
using FluentAssertions;

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
        result.Should().BeSameAs(input);
        result.Name.Should().Be("John Doe");
        result.Email.Should().Be("john@example.com");
    }

    [Fact]
    public void MaskForAudit_NonGeneric_ShouldReturnInputUnchanged()
    {
        // Arrange
        object input = new TestRequest { Name = "Jane Doe", Email = "jane@example.com" };

        // Act
        var result = _masker.MaskForAudit(input);

        // Assert
        result.Should().BeSameAs(input);
    }

    [Fact]
    public void MaskForAudit_Generic_WithString_ShouldReturnSameString()
    {
        // Arrange
        var input = "sensitive data";

        // Act
        var result = _masker.MaskForAudit(input);

        // Assert
        result.Should().Be("sensitive data");
    }

    [Fact]
    public void MaskForAudit_Generic_WithInt_ShouldReturnSameValue()
    {
        // Arrange
        var input = 12345;

        // Act
        var result = _masker.MaskForAudit(input);

        // Assert
        result.Should().Be(12345);
    }

    [Fact]
    public void MaskForAudit_Generic_WithGuid_ShouldReturnSameValue()
    {
        // Arrange
        var input = Guid.NewGuid();

        // Act
        var result = _masker.MaskForAudit(input);

        // Assert
        result.Should().Be(input);
    }

    [Fact]
    public void MaskForAudit_NonGeneric_WithString_ShouldReturnSameString()
    {
        // Arrange
        object input = "sensitive data";

        // Act
        var result = _masker.MaskForAudit(input);

        // Assert
        result.Should().Be("sensitive data");
    }

    [Fact]
    public void MaskForAudit_Generic_WithCollection_ShouldReturnSameCollection()
    {
        // Arrange
        var input = new List<string> { "item1", "item2", "item3" };

        // Act
        var result = _masker.MaskForAudit(input);

        // Assert
        result.Should().BeSameAs(input);
        result.Should().HaveCount(3);
    }

    [Fact]
    public void MaskForAudit_Generic_WithAnonymousType_ShouldReturnSameObject()
    {
        // Arrange
        var input = new { Name = "Test", Value = 42 };

        // Act
        var result = _masker.MaskForAudit(input);

        // Assert
        result.Should().BeSameAs(input);
        result.Name.Should().Be("Test");
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Instance_ShouldImplementIPiiMasker()
    {
        // Assert
        _masker.Should().BeAssignableTo<IPiiMasker>();
    }

    private sealed class TestRequest
    {
        public string? Name { get; init; }
        public string? Email { get; init; }
    }
}
