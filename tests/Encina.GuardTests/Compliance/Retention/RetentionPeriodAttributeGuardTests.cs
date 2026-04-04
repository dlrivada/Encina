using Encina.Compliance.Retention;

namespace Encina.GuardTests.Compliance.Retention;

/// <summary>
/// Guard tests for <see cref="RetentionPeriodAttribute"/> verifying computed properties.
/// </summary>
public class RetentionPeriodAttributeGuardTests
{
    [Fact]
    public void RetentionPeriod_NeitherDaysNorYears_ReturnsZero()
    {
        var attr = new RetentionPeriodAttribute();
        attr.RetentionPeriod.ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    public void RetentionPeriod_DaysSet_ReturnsFromDays()
    {
        var attr = new RetentionPeriodAttribute { Days = 90 };
        attr.RetentionPeriod.ShouldBe(TimeSpan.FromDays(90));
    }

    [Fact]
    public void RetentionPeriod_YearsSet_ReturnsFromYears()
    {
        var attr = new RetentionPeriodAttribute { Years = 7 };
        attr.RetentionPeriod.ShouldBe(TimeSpan.FromDays(7 * 365));
    }

    [Fact]
    public void RetentionPeriod_BothDaysAndYears_DaysTakesPrecedence()
    {
        var attr = new RetentionPeriodAttribute { Days = 30, Years = 2 };
        attr.RetentionPeriod.ShouldBe(TimeSpan.FromDays(30));
    }

    [Fact]
    public void AutoDelete_Default_IsTrue()
    {
        var attr = new RetentionPeriodAttribute();
        attr.AutoDelete.ShouldBeTrue();
    }

    [Fact]
    public void Reason_Default_IsNull()
    {
        var attr = new RetentionPeriodAttribute();
        attr.Reason.ShouldBeNull();
    }

    [Fact]
    public void DataCategory_Default_IsNull()
    {
        var attr = new RetentionPeriodAttribute();
        attr.DataCategory.ShouldBeNull();
    }

    [Fact]
    public void Reason_WhenSet_ReturnsValue()
    {
        var attr = new RetentionPeriodAttribute { Reason = "GDPR Article 5(1)(e)" };
        attr.Reason.ShouldBe("GDPR Article 5(1)(e)");
    }

    [Fact]
    public void DataCategory_WhenSet_ReturnsValue()
    {
        var attr = new RetentionPeriodAttribute { DataCategory = "financial-records" };
        attr.DataCategory.ShouldBe("financial-records");
    }
}
