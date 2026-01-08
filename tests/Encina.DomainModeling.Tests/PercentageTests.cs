using Encina.DomainModeling;
using LanguageExt;
using Shouldly;

namespace Encina.DomainModeling.Tests;

public sealed class PercentageTests
{
    [Fact]
    public void Percentage_Create_ValidValue_ReturnsRight()
    {
        // Act
        var result = Percentage.Create(50m);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: p => p.Value.ShouldBe(50m),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public void Percentage_Create_NegativeValue_ReturnsLeft()
    {
        // Act
        var result = Percentage.Create(-10m);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void Percentage_Create_OverHundred_ReturnsLeft()
    {
        // Act
        var result = Percentage.Create(150m);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void Percentage_From_ValidValue_ReturnsPercentage()
    {
        // Act
        var percentage = Percentage.From(75m);

        // Assert
        percentage.Value.ShouldBe(75m);
    }

    [Fact]
    public void Percentage_From_NegativeValue_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => Percentage.From(-1m));
    }

    [Fact]
    public void Percentage_From_OverHundred_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => Percentage.From(101m));
    }

    [Fact]
    public void Percentage_Zero_ReturnsZeroPercentage()
    {
        // Act
        var percentage = Percentage.Zero;

        // Assert
        percentage.Value.ShouldBe(0m);
    }

    [Fact]
    public void Percentage_Full_ReturnsHundredPercent()
    {
        // Act
        var percentage = Percentage.Full;

        // Assert
        percentage.Value.ShouldBe(100m);
    }

    [Fact]
    public void Percentage_Half_ReturnsFiftyPercent()
    {
        // Act
        var percentage = Percentage.Half;

        // Assert
        percentage.Value.ShouldBe(50m);
    }

    [Fact]
    public void Percentage_ApplyTo_CalculatesCorrectly()
    {
        // Arrange
        var percentage = Percentage.From(20m);

        // Act
        var result = percentage.ApplyTo(100m);

        // Assert
        result.ShouldBe(20m);
    }

    [Fact]
    public void Percentage_AsFraction_ReturnsCorrectValue()
    {
        // Arrange
        var percentage = Percentage.From(25m);

        // Act
        var fraction = percentage.AsFraction;

        // Assert
        fraction.ShouldBe(0.25m);
    }

    [Fact]
    public void Percentage_Complement_ReturnsCorrectValue()
    {
        // Arrange
        var percentage = Percentage.From(30m);

        // Act
        var complement = percentage.Complement;

        // Assert
        complement.Value.ShouldBe(70m);
    }

    [Fact]
    public void Percentage_Operators_WorkCorrectly()
    {
        // Arrange
        var p1 = Percentage.From(50m);
        var p2 = Percentage.From(25m);

        // Act & Assert
        (p1 == Percentage.From(50m)).ShouldBeTrue();
        (p1 != p2).ShouldBeTrue();
        (p1 > p2).ShouldBeTrue();
        (p2 < p1).ShouldBeTrue();
        (p1 >= Percentage.Half).ShouldBeTrue();
        (p2 <= p1).ShouldBeTrue();
    }

    [Fact]
    public void Percentage_Equality_WorksCorrectly()
    {
        // Arrange
        var p1 = Percentage.From(50m);
        var p2 = Percentage.From(50m);

        // Assert
        p1.Equals(p2).ShouldBeTrue();
        p1.Equals((object)p2).ShouldBeTrue();
        p1.GetHashCode().ShouldBe(p2.GetHashCode());
    }

    [Fact]
    public void Percentage_ToString_ReturnsFormattedString()
    {
        // Arrange
        var percentage = Percentage.From(42.5m);

        // Act
        var str = percentage.ToString();

        // Assert - culture-independent check
        str.ShouldEndWith("%");
        str.ShouldContain("42");
        str.ShouldContain("5");
    }
}
