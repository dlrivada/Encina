using Encina.DomainModeling;
using LanguageExt;
using Shouldly;

namespace Encina.DomainModeling.Tests;

public sealed class DateRangeTests
{
    [Fact]
    public void DateRange_Create_ValidRange_ReturnsRight()
    {
        // Arrange
        var start = new DateOnly(2024, 1, 1);
        var end = new DateOnly(2024, 12, 31);

        // Act
        var result = DateRange.Create(start, end);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: r =>
            {
                r.Start.ShouldBe(start);
                r.End.ShouldBe(end);
            },
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public void DateRange_Create_EndBeforeStart_ReturnsLeft()
    {
        // Arrange
        var start = new DateOnly(2024, 12, 31);
        var end = new DateOnly(2024, 1, 1);

        // Act
        var result = DateRange.Create(start, end);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void DateRange_From_ValidRange_ReturnsDateRange()
    {
        // Arrange
        var start = new DateOnly(2024, 1, 1);
        var end = new DateOnly(2024, 1, 31);

        // Act
        var range = DateRange.From(start, end);

        // Assert
        range.Start.ShouldBe(start);
        range.End.ShouldBe(end);
    }

    [Fact]
    public void DateRange_From_EndBeforeStart_ThrowsArgumentException()
    {
        // Arrange
        var start = new DateOnly(2024, 12, 31);
        var end = new DateOnly(2024, 1, 1);

        // Act & Assert
        Should.Throw<ArgumentException>(() => DateRange.From(start, end));
    }

    [Fact]
    public void DateRange_SingleDay_CreatesSingleDayRange()
    {
        // Arrange
        var date = new DateOnly(2024, 6, 15);

        // Act
        var range = DateRange.SingleDay(date);

        // Assert
        range.Start.ShouldBe(date);
        range.End.ShouldBe(date);
        range.TotalDays.ShouldBe(1);
    }

    [Fact]
    public void DateRange_Days_CreatesRangeWithCorrectDays()
    {
        // Arrange
        var start = new DateOnly(2024, 1, 1);

        // Act
        var range = DateRange.Days(start, 7);

        // Assert
        range.Start.ShouldBe(start);
        range.End.ShouldBe(new DateOnly(2024, 1, 7));
        range.TotalDays.ShouldBe(7);
    }

    [Fact]
    public void DateRange_Days_ZeroCount_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var start = new DateOnly(2024, 1, 1);

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => DateRange.Days(start, 0));
    }

    [Fact]
    public void DateRange_TotalDays_CalculatesCorrectly()
    {
        // Arrange
        var range = DateRange.From(new DateOnly(2024, 1, 1), new DateOnly(2024, 1, 10));

        // Act
        var days = range.TotalDays;

        // Assert
        days.ShouldBe(10);
    }

    [Fact]
    public void DateRange_Contains_ReturnsTrue_WhenDateInRange()
    {
        // Arrange
        var range = DateRange.From(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31));

        // Act & Assert
        range.Contains(new DateOnly(2024, 6, 15)).ShouldBeTrue();
        range.Contains(new DateOnly(2024, 1, 1)).ShouldBeTrue();
        range.Contains(new DateOnly(2024, 12, 31)).ShouldBeTrue();
    }

    [Fact]
    public void DateRange_Contains_ReturnsFalse_WhenDateOutOfRange()
    {
        // Arrange
        var range = DateRange.From(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31));

        // Act & Assert
        range.Contains(new DateOnly(2023, 12, 31)).ShouldBeFalse();
        range.Contains(new DateOnly(2025, 1, 1)).ShouldBeFalse();
    }

    [Fact]
    public void DateRange_Overlaps_ReturnsTrue_WhenRangesOverlap()
    {
        // Arrange
        var range1 = DateRange.From(new DateOnly(2024, 1, 1), new DateOnly(2024, 6, 30));
        var range2 = DateRange.From(new DateOnly(2024, 4, 1), new DateOnly(2024, 12, 31));

        // Act & Assert
        range1.Overlaps(range2).ShouldBeTrue();
    }

    [Fact]
    public void DateRange_Overlaps_ReturnsFalse_WhenRangesDoNotOverlap()
    {
        // Arrange
        var range1 = DateRange.From(new DateOnly(2024, 1, 1), new DateOnly(2024, 3, 31));
        var range2 = DateRange.From(new DateOnly(2024, 7, 1), new DateOnly(2024, 12, 31));

        // Act & Assert
        range1.Overlaps(range2).ShouldBeFalse();
    }

    [Fact]
    public void DateRange_FullyContains_ReturnsTrue_WhenRangeFullyContained()
    {
        // Arrange
        var outer = DateRange.From(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31));
        var inner = DateRange.From(new DateOnly(2024, 3, 1), new DateOnly(2024, 6, 30));

        // Act & Assert
        outer.FullyContains(inner).ShouldBeTrue();
    }

    [Fact]
    public void DateRange_Intersect_ReturnsIntersection_WhenRangesOverlap()
    {
        // Arrange
        var range1 = DateRange.From(new DateOnly(2024, 1, 1), new DateOnly(2024, 6, 30));
        var range2 = DateRange.From(new DateOnly(2024, 4, 1), new DateOnly(2024, 12, 31));

        // Act
        var intersection = range1.Intersect(range2);

        // Assert
        intersection.IsSome.ShouldBeTrue();
        intersection.Match(
            Some: r =>
            {
                r.Start.ShouldBe(new DateOnly(2024, 4, 1));
                r.End.ShouldBe(new DateOnly(2024, 6, 30));
            },
            None: () => throw new InvalidOperationException("Expected Some"));
    }

    [Fact]
    public void DateRange_Intersect_ReturnsNone_WhenRangesDoNotOverlap()
    {
        // Arrange
        var range1 = DateRange.From(new DateOnly(2024, 1, 1), new DateOnly(2024, 3, 31));
        var range2 = DateRange.From(new DateOnly(2024, 7, 1), new DateOnly(2024, 12, 31));

        // Act
        var intersection = range1.Intersect(range2);

        // Assert
        intersection.IsNone.ShouldBeTrue();
    }

    [Fact]
    public void DateRange_ExtendBy_ExtendsEndDate()
    {
        // Arrange
        var range = DateRange.From(new DateOnly(2024, 1, 1), new DateOnly(2024, 1, 31));

        // Act
        var extended = range.ExtendBy(10);

        // Assert
        extended.Start.ShouldBe(new DateOnly(2024, 1, 1));
        extended.End.ShouldBe(new DateOnly(2024, 2, 10));
    }

    [Fact]
    public void DateRange_Equality_WorksCorrectly()
    {
        // Arrange
        var range1 = DateRange.From(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31));
        var range2 = DateRange.From(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31));
        var range3 = DateRange.From(new DateOnly(2024, 1, 1), new DateOnly(2024, 6, 30));

        // Assert
        (range1 == range2).ShouldBeTrue();
        (range1 != range3).ShouldBeTrue();
        range1.Equals(range2).ShouldBeTrue();
        range1.Equals((object)range2).ShouldBeTrue();
    }

    [Fact]
    public void DateRange_ToString_ReturnsFormattedString()
    {
        // Arrange
        var range = DateRange.From(new DateOnly(2024, 1, 15), new DateOnly(2024, 6, 30));

        // Act
        var str = range.ToString();

        // Assert
        str.ShouldBe("2024-01-15 to 2024-06-30");
    }
}
