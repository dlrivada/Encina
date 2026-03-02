using Encina.Compliance.Retention.Model;
using FluentAssertions;

namespace Encina.UnitTests.Compliance.Retention;

/// <summary>
/// Unit tests for <see cref="RetentionPolicy"/> factory methods and record behavior.
/// </summary>
public class RetentionPolicyTests
{
    #region Create Factory Method Tests

    [Fact]
    public void Create_ShouldSetDataCategory()
    {
        // Act
        var policy = RetentionPolicy.Create(
            dataCategory: "financial-records",
            retentionPeriod: TimeSpan.FromDays(365));

        // Assert
        policy.DataCategory.Should().Be("financial-records");
    }

    [Fact]
    public void Create_ShouldSetRetentionPeriod()
    {
        // Arrange
        var retentionPeriod = TimeSpan.FromDays(365);

        // Act
        var policy = RetentionPolicy.Create(
            dataCategory: "financial-records",
            retentionPeriod: retentionPeriod);

        // Assert
        policy.RetentionPeriod.Should().Be(retentionPeriod);
    }

    [Fact]
    public void Create_ShouldSetAutoDeleteToTrue_ByDefault()
    {
        // Act
        var policy = RetentionPolicy.Create(
            dataCategory: "financial-records",
            retentionPeriod: TimeSpan.FromDays(365));

        // Assert
        policy.AutoDelete.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldSetPolicyTypeToTimeBased_ByDefault()
    {
        // Act
        var policy = RetentionPolicy.Create(
            dataCategory: "financial-records",
            retentionPeriod: TimeSpan.FromDays(365));

        // Assert
        policy.PolicyType.Should().Be(RetentionPolicyType.TimeBased);
    }

    [Fact]
    public void Create_ShouldGenerateId_WithNoHyphens()
    {
        // Act
        var policy = RetentionPolicy.Create(
            dataCategory: "financial-records",
            retentionPeriod: TimeSpan.FromDays(365));

        // Assert
        policy.Id.Should().NotBeNullOrEmpty();
        policy.Id.Should().HaveLength(32);
        policy.Id.Should().NotContain("-");
    }

    [Fact]
    public void Create_TwoCalls_ShouldGenerateDifferentIds()
    {
        // Act
        var policy1 = RetentionPolicy.Create("financial-records", TimeSpan.FromDays(365));
        var policy2 = RetentionPolicy.Create("session-logs", TimeSpan.FromDays(90));

        // Assert
        policy1.Id.Should().NotBe(policy2.Id);
    }

    [Fact]
    public void Create_WithReason_ShouldSetReason()
    {
        // Act
        var policy = RetentionPolicy.Create(
            dataCategory: "financial-records",
            retentionPeriod: TimeSpan.FromDays(365 * 7),
            reason: "German tax law (AO section 147)");

        // Assert
        policy.Reason.Should().Be("German tax law (AO section 147)");
    }

    [Fact]
    public void Create_WithoutReason_ShouldLeaveItNull()
    {
        // Act
        var policy = RetentionPolicy.Create(
            dataCategory: "financial-records",
            retentionPeriod: TimeSpan.FromDays(365));

        // Assert
        policy.Reason.Should().BeNull();
    }

    [Fact]
    public void Create_WithLegalBasis_ShouldSetLegalBasis()
    {
        // Act
        var policy = RetentionPolicy.Create(
            dataCategory: "financial-records",
            retentionPeriod: TimeSpan.FromDays(365 * 7),
            legalBasis: "Legal obligation (Art. 6(1)(c))");

        // Assert
        policy.LegalBasis.Should().Be("Legal obligation (Art. 6(1)(c))");
    }

    [Fact]
    public void Create_WithoutLegalBasis_ShouldLeaveItNull()
    {
        // Act
        var policy = RetentionPolicy.Create(
            dataCategory: "financial-records",
            retentionPeriod: TimeSpan.FromDays(365));

        // Assert
        policy.LegalBasis.Should().BeNull();
    }

    [Fact]
    public void Create_WithPolicyType_ShouldSetPolicyType()
    {
        // Act
        var policy = RetentionPolicy.Create(
            dataCategory: "marketing-consent",
            retentionPeriod: TimeSpan.FromDays(365),
            policyType: RetentionPolicyType.ConsentBased);

        // Assert
        policy.PolicyType.Should().Be(RetentionPolicyType.ConsentBased);
    }

    [Fact]
    public void Create_WithAutoDeleteFalse_ShouldSetAutoDeleteFalse()
    {
        // Act
        var policy = RetentionPolicy.Create(
            dataCategory: "audit-logs",
            retentionPeriod: TimeSpan.FromDays(365 * 7),
            autoDelete: false);

        // Assert
        policy.AutoDelete.Should().BeFalse();
    }

    [Fact]
    public void Create_ShouldSetCreatedAtUtcToNow()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var policy = RetentionPolicy.Create(
            dataCategory: "financial-records",
            retentionPeriod: TimeSpan.FromDays(365));

        var after = DateTimeOffset.UtcNow;

        // Assert
        policy.CreatedAtUtc.Should().BeOnOrAfter(before);
        policy.CreatedAtUtc.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void Create_ShouldSetLastModifiedAtUtcToNull()
    {
        // Act
        var policy = RetentionPolicy.Create(
            dataCategory: "financial-records",
            retentionPeriod: TimeSpan.FromDays(365));

        // Assert
        policy.LastModifiedAtUtc.Should().BeNull();
    }

    #endregion

    #region FromDays Helper Tests

    [Fact]
    public void FromDays_ShouldReturnCorrectTimeSpan()
    {
        // Act
        var result = RetentionPolicy.FromDays(365);

        // Assert
        result.Should().Be(TimeSpan.FromDays(365));
    }

    [Fact]
    public void FromDays_WithZero_ShouldReturnZeroTimeSpan()
    {
        // Act
        var result = RetentionPolicy.FromDays(0);

        // Assert
        result.Should().Be(TimeSpan.Zero);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(90)]
    [InlineData(365)]
    [InlineData(365 * 7)]
    public void FromDays_WithVariousDays_ShouldReturnCorrectTimeSpan(int days)
    {
        // Act
        var result = RetentionPolicy.FromDays(days);

        // Assert
        result.TotalDays.Should().Be(days);
    }

    #endregion

    #region FromYears Helper Tests

    [Fact]
    public void FromYears_ShouldReturnTimeSpanOf365DaysPerYear()
    {
        // Act
        var result = RetentionPolicy.FromYears(1);

        // Assert
        result.Should().Be(TimeSpan.FromDays(365));
    }

    [Fact]
    public void FromYears_WithSevenYears_ShouldReturnCorrectTimeSpan()
    {
        // Act
        var result = RetentionPolicy.FromYears(7);

        // Assert
        result.Should().Be(TimeSpan.FromDays(7 * 365));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(7)]
    [InlineData(10)]
    public void FromYears_WithVariousYears_ShouldReturnCorrectDays(int years)
    {
        // Act
        var result = RetentionPolicy.FromYears(years);

        // Assert
        result.TotalDays.Should().Be(years * 365);
    }

    #endregion
}
