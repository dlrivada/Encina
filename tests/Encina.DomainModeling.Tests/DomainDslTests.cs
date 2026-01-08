using System;
using Encina.DomainModeling;
using LanguageExt;
using Shouldly;
using Xunit;

namespace Encina.DomainModeling.Tests;

public sealed class DomainDslTests
{
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

    [Fact]
    public void TimeRange_Create_ValidRange_ReturnsRight()
    {
        // Arrange
        var start = new TimeOnly(9, 0);
        var end = new TimeOnly(17, 0);

        // Act
        var result = TimeRange.Create(start, end);

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
    public void TimeRange_Create_EndNotAfterStart_ReturnsLeft()
    {
        // Arrange
        var start = new TimeOnly(17, 0);
        var end = new TimeOnly(9, 0);

        // Act
        var result = TimeRange.Create(start, end);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void TimeRange_Create_SameStartAndEnd_ReturnsLeft()
    {
        // Arrange
        var time = new TimeOnly(12, 0);

        // Act
        var result = TimeRange.Create(time, time);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void TimeRange_From_ValidRange_ReturnsTimeRange()
    {
        // Arrange
        var start = new TimeOnly(9, 0);
        var end = new TimeOnly(17, 0);

        // Act
        var range = TimeRange.From(start, end);

        // Assert
        range.Start.ShouldBe(start);
        range.End.ShouldBe(end);
    }

    [Fact]
    public void TimeRange_From_EndNotAfterStart_ThrowsArgumentException()
    {
        // Arrange
        var start = new TimeOnly(17, 0);
        var end = new TimeOnly(9, 0);

        // Act & Assert
        Should.Throw<ArgumentException>(() => TimeRange.From(start, end));
    }

    [Fact]
    public void TimeRange_Duration_CalculatesCorrectly()
    {
        // Arrange
        var range = TimeRange.From(new TimeOnly(9, 0), new TimeOnly(17, 30));

        // Act
        var duration = range.Duration;

        // Assert
        duration.ShouldBe(TimeSpan.FromHours(8.5));
    }

    [Fact]
    public void TimeRange_Contains_ReturnsTrue_WhenTimeInRange()
    {
        // Arrange
        var range = TimeRange.From(new TimeOnly(9, 0), new TimeOnly(17, 0));

        // Act & Assert
        range.Contains(new TimeOnly(12, 0)).ShouldBeTrue();
        range.Contains(new TimeOnly(9, 0)).ShouldBeTrue();
        range.Contains(new TimeOnly(17, 0)).ShouldBeTrue();
    }

    [Fact]
    public void TimeRange_Contains_ReturnsFalse_WhenTimeOutOfRange()
    {
        // Arrange
        var range = TimeRange.From(new TimeOnly(9, 0), new TimeOnly(17, 0));

        // Act & Assert
        range.Contains(new TimeOnly(8, 59)).ShouldBeFalse();
        range.Contains(new TimeOnly(17, 1)).ShouldBeFalse();
    }

    [Fact]
    public void TimeRange_Overlaps_ReturnsTrue_WhenRangesOverlap()
    {
        // Arrange
        var range1 = TimeRange.From(new TimeOnly(9, 0), new TimeOnly(12, 0));
        var range2 = TimeRange.From(new TimeOnly(11, 0), new TimeOnly(14, 0));

        // Act & Assert
        range1.Overlaps(range2).ShouldBeTrue();
    }

    [Fact]
    public void TimeRange_Overlaps_ReturnsFalse_WhenRangesDoNotOverlap()
    {
        // Arrange
        var range1 = TimeRange.From(new TimeOnly(9, 0), new TimeOnly(12, 0));
        var range2 = TimeRange.From(new TimeOnly(14, 0), new TimeOnly(17, 0));

        // Act & Assert
        range1.Overlaps(range2).ShouldBeFalse();
    }

    [Fact]
    public void TimeRange_Equality_WorksCorrectly()
    {
        // Arrange
        var range1 = TimeRange.From(new TimeOnly(9, 0), new TimeOnly(17, 0));
        var range2 = TimeRange.From(new TimeOnly(9, 0), new TimeOnly(17, 0));
        var range3 = TimeRange.From(new TimeOnly(10, 0), new TimeOnly(17, 0));

        // Assert
        (range1 == range2).ShouldBeTrue();
        (range1 != range3).ShouldBeTrue();
        range1.Equals(range2).ShouldBeTrue();
        range1.Equals((object)range2).ShouldBeTrue();
    }

    [Fact]
    public void TimeRange_ToString_ReturnsFormattedString()
    {
        // Arrange
        var range = TimeRange.From(new TimeOnly(9, 30), new TimeOnly(17, 45));

        // Act
        var str = range.ToString();

        // Assert
        str.ShouldBe("09:30 - 17:45");
    }
}
