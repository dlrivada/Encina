using Encina.Compliance.Retention;

using FluentAssertions;

namespace Encina.UnitTests.Compliance.Retention;

/// <summary>
/// Unit tests for <see cref="RetentionPeriodAttribute"/> properties and computed values.
/// </summary>
public class RetentionPeriodAttributeTests
{
    #region AttributeUsage Tests

    [Fact]
    public void Attribute_ShouldTarget_ClassAndProperty()
    {
        // Arrange
        var usage = typeof(RetentionPeriodAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        // Assert
        usage.ValidOn.Should().HaveFlag(AttributeTargets.Class);
        usage.ValidOn.Should().HaveFlag(AttributeTargets.Property);
    }

    [Fact]
    public void Attribute_ShouldNotAllowMultiple()
    {
        // Arrange
        var usage = typeof(RetentionPeriodAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        // Assert
        usage.AllowMultiple.Should().BeFalse();
    }

    #endregion

    #region Days Property Tests

    [Fact]
    public void RetentionPeriod_WhenDaysIsSet_ShouldReturnFromDays()
    {
        // Arrange
        var attribute = new RetentionPeriodAttribute { Days = 90 };

        // Act
        var period = attribute.RetentionPeriod;

        // Assert
        period.Should().Be(TimeSpan.FromDays(90));
    }

    [Fact]
    public void RetentionPeriod_WhenDaysIsSet_ShouldMatchTimeSpanFromDays()
    {
        // Arrange
        var attribute = new RetentionPeriodAttribute { Days = 365 };

        // Act & Assert
        attribute.RetentionPeriod.Should().Be(TimeSpan.FromDays(365));
    }

    #endregion

    #region Years Property Tests

    [Fact]
    public void RetentionPeriod_WhenYearsIsSet_ShouldReturnFromYears()
    {
        // Arrange
        var attribute = new RetentionPeriodAttribute { Years = 7 };

        // Act
        var period = attribute.RetentionPeriod;

        // Assert
        period.Should().Be(TimeSpan.FromDays(7 * 365));
    }

    [Fact]
    public void RetentionPeriod_WhenYearsIsSet_ShouldUse365DaysPerYear()
    {
        // Arrange
        var attribute = new RetentionPeriodAttribute { Years = 1 };

        // Act & Assert
        attribute.RetentionPeriod.TotalDays.Should().Be(365);
    }

    #endregion

    #region Neither Days nor Years Set

    [Fact]
    public void RetentionPeriod_WhenNeitherDaysNorYearsSet_ShouldReturnZero()
    {
        // Arrange
        var attribute = new RetentionPeriodAttribute();

        // Act
        var period = attribute.RetentionPeriod;

        // Assert
        period.Should().Be(TimeSpan.Zero);
    }

    #endregion

    #region Days Takes Precedence

    [Fact]
    public void RetentionPeriod_WhenBothDaysAndYearsSet_DaysTakesPrecedence()
    {
        // Arrange
        var attribute = new RetentionPeriodAttribute { Days = 30, Years = 7 };

        // Act
        var period = attribute.RetentionPeriod;

        // Assert
        period.Should().Be(TimeSpan.FromDays(30));
    }

    #endregion

    #region Reason Property Tests

    [Fact]
    public void Reason_DefaultValue_ShouldBeNull()
    {
        // Arrange
        var attribute = new RetentionPeriodAttribute();

        // Assert
        attribute.Reason.Should().BeNull();
    }

    [Fact]
    public void Reason_WhenSet_ShouldReturnValue()
    {
        // Arrange
        var attribute = new RetentionPeriodAttribute { Reason = "German tax law (AO section 147)" };

        // Assert
        attribute.Reason.Should().Be("German tax law (AO section 147)");
    }

    #endregion

    #region AutoDelete Property Tests

    [Fact]
    public void AutoDelete_DefaultValue_ShouldBeTrue()
    {
        // Arrange
        var attribute = new RetentionPeriodAttribute();

        // Assert
        attribute.AutoDelete.Should().BeTrue();
    }

    [Fact]
    public void AutoDelete_WhenSetToFalse_ShouldReturnFalse()
    {
        // Arrange
        var attribute = new RetentionPeriodAttribute { AutoDelete = false };

        // Assert
        attribute.AutoDelete.Should().BeFalse();
    }

    #endregion

    #region DataCategory Property Tests

    [Fact]
    public void DataCategory_DefaultValue_ShouldBeNull()
    {
        // Arrange
        var attribute = new RetentionPeriodAttribute();

        // Assert
        attribute.DataCategory.Should().BeNull();
    }

    [Fact]
    public void DataCategory_WhenSet_ShouldReturnValue()
    {
        // Arrange
        var attribute = new RetentionPeriodAttribute { DataCategory = "financial-records" };

        // Assert
        attribute.DataCategory.Should().Be("financial-records");
    }

    #endregion

    #region Days and Years Independence

    [Fact]
    public void Days_WhenYearsIsZero_ShouldUseDays()
    {
        // Arrange
        var attribute = new RetentionPeriodAttribute { Days = 180 };

        // Assert
        attribute.RetentionPeriod.Should().Be(TimeSpan.FromDays(180));
    }

    [Fact]
    public void Years_WhenDaysIsZero_ShouldUseYears()
    {
        // Arrange
        var attribute = new RetentionPeriodAttribute { Years = 3 };

        // Assert
        attribute.RetentionPeriod.Should().Be(TimeSpan.FromDays(3 * 365));
    }

    #endregion
}
