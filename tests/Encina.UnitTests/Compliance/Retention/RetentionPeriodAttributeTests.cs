using Encina.Compliance.Retention;

using Shouldly;

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
        usage.ValidOn.HasFlag(AttributeTargets.Class).ShouldBeTrue();
        usage.ValidOn.HasFlag(AttributeTargets.Property).ShouldBeTrue();
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
        usage.AllowMultiple.ShouldBeFalse();
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
        period.ShouldBe(TimeSpan.FromDays(90));
    }

    [Fact]
    public void RetentionPeriod_WhenDaysIsSet_ShouldMatchTimeSpanFromDays()
    {
        // Arrange
        var attribute = new RetentionPeriodAttribute { Days = 365 };

        // Act & Assert
        attribute.RetentionPeriod.ShouldBe(TimeSpan.FromDays(365));
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
        period.ShouldBe(TimeSpan.FromDays(7 * 365));
    }

    [Fact]
    public void RetentionPeriod_WhenYearsIsSet_ShouldUse365DaysPerYear()
    {
        // Arrange
        var attribute = new RetentionPeriodAttribute { Years = 1 };

        // Act & Assert
        attribute.RetentionPeriod.TotalDays.ShouldBe(365);
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
        period.ShouldBe(TimeSpan.Zero);
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
        period.ShouldBe(TimeSpan.FromDays(30));
    }

    #endregion

    #region Reason Property Tests

    [Fact]
    public void Reason_DefaultValue_ShouldBeNull()
    {
        // Arrange
        var attribute = new RetentionPeriodAttribute();

        // Assert
        attribute.Reason.ShouldBeNull();
    }

    [Fact]
    public void Reason_WhenSet_ShouldReturnValue()
    {
        // Arrange
        var attribute = new RetentionPeriodAttribute { Reason = "German tax law (AO section 147)" };

        // Assert
        attribute.Reason.ShouldBe("German tax law (AO section 147)");
    }

    #endregion

    #region AutoDelete Property Tests

    [Fact]
    public void AutoDelete_DefaultValue_ShouldBeTrue()
    {
        // Arrange
        var attribute = new RetentionPeriodAttribute();

        // Assert
        attribute.AutoDelete.ShouldBeTrue();
    }

    [Fact]
    public void AutoDelete_WhenSetToFalse_ShouldReturnFalse()
    {
        // Arrange
        var attribute = new RetentionPeriodAttribute { AutoDelete = false };

        // Assert
        attribute.AutoDelete.ShouldBeFalse();
    }

    #endregion

    #region DataCategory Property Tests

    [Fact]
    public void DataCategory_DefaultValue_ShouldBeNull()
    {
        // Arrange
        var attribute = new RetentionPeriodAttribute();

        // Assert
        attribute.DataCategory.ShouldBeNull();
    }

    [Fact]
    public void DataCategory_WhenSet_ShouldReturnValue()
    {
        // Arrange
        var attribute = new RetentionPeriodAttribute { DataCategory = "financial-records" };

        // Assert
        attribute.DataCategory.ShouldBe("financial-records");
    }

    #endregion

    #region Days and Years Independence

    [Fact]
    public void Days_WhenYearsIsZero_ShouldUseDays()
    {
        // Arrange
        var attribute = new RetentionPeriodAttribute { Days = 180 };

        // Assert
        attribute.RetentionPeriod.ShouldBe(TimeSpan.FromDays(180));
    }

    [Fact]
    public void Years_WhenDaysIsZero_ShouldUseYears()
    {
        // Arrange
        var attribute = new RetentionPeriodAttribute { Years = 3 };

        // Assert
        attribute.RetentionPeriod.ShouldBe(TimeSpan.FromDays(3 * 365));
    }

    #endregion
}
