using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Model;

using FluentAssertions;

namespace Encina.UnitTests.Compliance.Retention;

/// <summary>
/// Unit tests for <see cref="RetentionPolicyMapper"/> static mapping methods.
/// </summary>
public class RetentionPolicyMapperTests
{
    #region ToEntity Tests

    [Fact]
    public void ToEntity_ValidPolicy_ShouldMapAllProperties()
    {
        // Arrange
        var policy = CreatePolicy();

        // Act
        var entity = RetentionPolicyMapper.ToEntity(policy);

        // Assert
        entity.Id.Should().Be(policy.Id);
        entity.DataCategory.Should().Be(policy.DataCategory);
        entity.RetentionPeriodTicks.Should().Be(policy.RetentionPeriod.Ticks);
        entity.AutoDelete.Should().Be(policy.AutoDelete);
        entity.Reason.Should().Be(policy.Reason);
        entity.LegalBasis.Should().Be(policy.LegalBasis);
        entity.PolicyTypeValue.Should().Be((int)policy.PolicyType);
        entity.CreatedAtUtc.Should().Be(policy.CreatedAtUtc);
        entity.LastModifiedAtUtc.Should().Be(policy.LastModifiedAtUtc);
    }

    [Fact]
    public void ToEntity_ShouldConvertRetentionPeriodToTicks()
    {
        // Arrange
        var period = TimeSpan.FromDays(365 * 7);
        var policy = new RetentionPolicy
        {
            Id = "p1",
            DataCategory = "financial-records",
            RetentionPeriod = period,
            AutoDelete = true,
            PolicyType = RetentionPolicyType.TimeBased,
            CreatedAtUtc = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)
        };

        // Act
        var entity = RetentionPolicyMapper.ToEntity(policy);

        // Assert
        entity.RetentionPeriodTicks.Should().Be(period.Ticks);
    }

    [Fact]
    public void ToEntity_ShouldConvertPolicyTypeToInt()
    {
        // Arrange
        var policy = new RetentionPolicy
        {
            Id = "p2",
            DataCategory = "marketing-consent",
            RetentionPeriod = TimeSpan.FromDays(365),
            AutoDelete = false,
            PolicyType = RetentionPolicyType.ConsentBased,
            CreatedAtUtc = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)
        };

        // Act
        var entity = RetentionPolicyMapper.ToEntity(policy);

        // Assert
        entity.PolicyTypeValue.Should().Be(2);
    }

    [Fact]
    public void ToEntity_WithNullOptionalFields_ShouldMapNulls()
    {
        // Arrange
        var policy = new RetentionPolicy
        {
            Id = "p3",
            DataCategory = "session-logs",
            RetentionPeriod = TimeSpan.FromDays(90),
            AutoDelete = true,
            PolicyType = RetentionPolicyType.TimeBased,
            CreatedAtUtc = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
            Reason = null,
            LegalBasis = null,
            LastModifiedAtUtc = null
        };

        // Act
        var entity = RetentionPolicyMapper.ToEntity(policy);

        // Assert
        entity.Reason.Should().BeNull();
        entity.LegalBasis.Should().BeNull();
        entity.LastModifiedAtUtc.Should().BeNull();
    }

    [Fact]
    public void ToEntity_NullPolicy_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => RetentionPolicyMapper.ToEntity(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region ToDomain Tests

    [Fact]
    public void ToDomain_ValidEntity_ShouldMapAllProperties()
    {
        // Arrange
        var entity = CreateEntity();

        // Act
        var domain = RetentionPolicyMapper.ToDomain(entity);

        // Assert
        domain.Should().NotBeNull();
        domain!.Id.Should().Be(entity.Id);
        domain.DataCategory.Should().Be(entity.DataCategory);
        domain.RetentionPeriod.Should().Be(new TimeSpan(entity.RetentionPeriodTicks));
        domain.AutoDelete.Should().Be(entity.AutoDelete);
        domain.Reason.Should().Be(entity.Reason);
        domain.LegalBasis.Should().Be(entity.LegalBasis);
        domain.PolicyType.Should().Be((RetentionPolicyType)entity.PolicyTypeValue);
        domain.CreatedAtUtc.Should().Be(entity.CreatedAtUtc);
        domain.LastModifiedAtUtc.Should().Be(entity.LastModifiedAtUtc);
    }

    [Fact]
    public void ToDomain_ShouldConvertTicksToRetentionPeriod()
    {
        // Arrange
        var period = TimeSpan.FromDays(365 * 7);
        var entity = CreateEntity();
        entity.RetentionPeriodTicks = period.Ticks;

        // Act
        var domain = RetentionPolicyMapper.ToDomain(entity);

        // Assert
        domain.Should().NotBeNull();
        domain!.RetentionPeriod.Should().Be(period);
    }

    [Fact]
    public void ToDomain_InvalidPolicyTypeValue_ShouldReturnNull()
    {
        // Arrange
        var entity = CreateEntity();
        entity.PolicyTypeValue = 999;

        // Act
        var domain = RetentionPolicyMapper.ToDomain(entity);

        // Assert
        domain.Should().BeNull();
    }

    [Fact]
    public void ToDomain_NullEntity_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => RetentionPolicyMapper.ToDomain(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(0, RetentionPolicyType.TimeBased)]
    [InlineData(1, RetentionPolicyType.EventBased)]
    [InlineData(2, RetentionPolicyType.ConsentBased)]
    public void ToDomain_AllValidPolicyTypeValues_ShouldMapCorrectly(int policyTypeValue, RetentionPolicyType expectedType)
    {
        // Arrange
        var entity = CreateEntity();
        entity.PolicyTypeValue = policyTypeValue;

        // Act
        var domain = RetentionPolicyMapper.ToDomain(entity);

        // Assert
        domain.Should().NotBeNull();
        domain!.PolicyType.Should().Be(expectedType);
    }

    #endregion

    #region Roundtrip Tests

    [Fact]
    public void Roundtrip_ToEntityThenToDomain_ShouldPreserveAllFields()
    {
        // Arrange
        var original = CreatePolicy();

        // Act
        var entity = RetentionPolicyMapper.ToEntity(original);
        var roundtripped = RetentionPolicyMapper.ToDomain(entity);

        // Assert
        roundtripped.Should().NotBeNull();
        roundtripped!.Id.Should().Be(original.Id);
        roundtripped.DataCategory.Should().Be(original.DataCategory);
        roundtripped.RetentionPeriod.Should().Be(original.RetentionPeriod);
        roundtripped.AutoDelete.Should().Be(original.AutoDelete);
        roundtripped.Reason.Should().Be(original.Reason);
        roundtripped.LegalBasis.Should().Be(original.LegalBasis);
        roundtripped.PolicyType.Should().Be(original.PolicyType);
        roundtripped.CreatedAtUtc.Should().Be(original.CreatedAtUtc);
        roundtripped.LastModifiedAtUtc.Should().Be(original.LastModifiedAtUtc);
    }

    #endregion

    private static RetentionPolicy CreatePolicy() => new()
    {
        Id = "policy-001",
        DataCategory = "financial-records",
        RetentionPeriod = TimeSpan.FromDays(365 * 7),
        AutoDelete = true,
        Reason = "German tax law (AO section 147)",
        LegalBasis = "Legal obligation (Art. 6(1)(c))",
        PolicyType = RetentionPolicyType.TimeBased,
        CreatedAtUtc = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
        LastModifiedAtUtc = new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero)
    };

    private static RetentionPolicyEntity CreateEntity() => new()
    {
        Id = "entity-001",
        DataCategory = "session-logs",
        RetentionPeriodTicks = TimeSpan.FromDays(90).Ticks,
        AutoDelete = false,
        Reason = "Internal data governance policy",
        LegalBasis = "Legitimate interest (Art. 6(1)(f))",
        PolicyTypeValue = 0,
        CreatedAtUtc = new DateTimeOffset(2025, 2, 1, 0, 0, 0, TimeSpan.Zero),
        LastModifiedAtUtc = null
    };
}
