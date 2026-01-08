using Encina.DomainModeling;
using LanguageExt;
using Shouldly;

namespace Encina.DomainModeling.Tests;

public sealed class QuantityTests
{
    [Fact]
    public void Quantity_Create_ValidValue_ReturnsRight()
    {
        // Act
        var result = Quantity.Create(10);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: q => q.Value.ShouldBe(10),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public void Quantity_Create_NegativeValue_ReturnsLeft()
    {
        // Act
        var result = Quantity.Create(-5);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void Quantity_From_ValidValue_ReturnsQuantity()
    {
        // Act
        var quantity = Quantity.From(15);

        // Assert
        quantity.Value.ShouldBe(15);
    }

    [Fact]
    public void Quantity_From_NegativeValue_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => Quantity.From(-1));
    }

    [Fact]
    public void Quantity_Zero_ReturnsZeroQuantity()
    {
        // Act
        var quantity = Quantity.Zero;

        // Assert
        quantity.Value.ShouldBe(0);
        quantity.IsZero.ShouldBeTrue();
        quantity.IsPositive.ShouldBeFalse();
    }

    [Fact]
    public void Quantity_One_ReturnsOneQuantity()
    {
        // Act
        var quantity = Quantity.One;

        // Assert
        quantity.Value.ShouldBe(1);
        quantity.IsPositive.ShouldBeTrue();
    }

    [Fact]
    public void Quantity_Add_AddsCorrectly()
    {
        // Arrange
        var q1 = Quantity.From(10);
        var q2 = Quantity.From(5);

        // Act
        var result = q1.Add(q2);

        // Assert
        result.Value.ShouldBe(15);
    }

    [Fact]
    public void Quantity_Subtract_SubtractsCorrectly()
    {
        // Arrange
        var q1 = Quantity.From(10);
        var q2 = Quantity.From(3);

        // Act
        var result = q1.Subtract(q2);

        // Assert
        result.Value.ShouldBe(7);
    }

    [Fact]
    public void Quantity_Subtract_FloorsAtZero()
    {
        // Arrange
        var q1 = Quantity.From(5);
        var q2 = Quantity.From(10);

        // Act
        var result = q1.Subtract(q2);

        // Assert
        result.Value.ShouldBe(0);
    }

    [Fact]
    public void Quantity_Multiply_MultipliesCorrectly()
    {
        // Arrange
        var quantity = Quantity.From(5);

        // Act
        var result = quantity.Multiply(3);

        // Assert
        result.Value.ShouldBe(15);
    }

    [Fact]
    public void Quantity_Multiply_NegativeFactor_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var quantity = Quantity.From(5);

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => quantity.Multiply(-1));
    }

    [Fact]
    public void Quantity_IsGreaterThan_ReturnsCorrectResult()
    {
        // Arrange
        var q1 = Quantity.From(10);
        var q2 = Quantity.From(5);

        // Act & Assert
        q1.IsGreaterThan(q2).ShouldBeTrue();
        q2.IsGreaterThan(q1).ShouldBeFalse();
    }

    [Fact]
    public void Quantity_Operators_WorkCorrectly()
    {
        // Arrange
        var q1 = Quantity.From(10);
        var q2 = Quantity.From(5);

        // Act & Assert
        (q1 + q2).Value.ShouldBe(15);
        (q1 - q2).Value.ShouldBe(5);
        (q1 * 2).Value.ShouldBe(20);
        (q1 == Quantity.From(10)).ShouldBeTrue();
        (q1 != q2).ShouldBeTrue();
        (q1 > q2).ShouldBeTrue();
        (q2 < q1).ShouldBeTrue();
        (q1 >= Quantity.From(10)).ShouldBeTrue();
        (q2 <= q1).ShouldBeTrue();
    }

    [Fact]
    public void Quantity_Equality_WorksCorrectly()
    {
        // Arrange
        var q1 = Quantity.From(10);
        var q2 = Quantity.From(10);
        var q3 = Quantity.From(5);

        // Assert
        q1.Equals(q2).ShouldBeTrue();
        q1.Equals(q3).ShouldBeFalse();
        q1.Equals((object)q2).ShouldBeTrue();
        q1.Equals("not a quantity").ShouldBeFalse();
        q1.GetHashCode().ShouldBe(q2.GetHashCode());
    }

    [Fact]
    public void Quantity_CompareTo_WorksCorrectly()
    {
        // Arrange
        var q1 = Quantity.From(10);
        var q2 = Quantity.From(5);
        var q3 = Quantity.From(10);

        // Assert
        q1.CompareTo(q2).ShouldBeGreaterThan(0);
        q2.CompareTo(q1).ShouldBeLessThan(0);
        q1.CompareTo(q3).ShouldBe(0);
    }

    [Fact]
    public void Quantity_ToString_ReturnsValueAsString()
    {
        // Arrange
        var quantity = Quantity.From(42);

        // Act
        var str = quantity.ToString();

        // Assert
        str.ShouldBe("42");
    }
}
