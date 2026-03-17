using Encina.Compliance.Retention.Aggregates;
using Encina.Compliance.Retention.Model;

namespace Encina.GuardTests.Compliance.Retention;

/// <summary>
/// Guard tests for <see cref="RetentionPolicyAggregate"/> to verify null, empty, whitespace,
/// and out-of-range parameter handling across all factory and instance methods.
/// </summary>
public class RetentionPolicyAggregateGuardTests
{
    #region Create Guards — dataCategory

    [Fact]
    public void Create_NullDataCategory_ThrowsArgumentException()
    {
        var act = () => RetentionPolicyAggregate.Create(
            Guid.NewGuid(), null!, TimeSpan.FromDays(365), false,
            RetentionPolicyType.TimeBased, null, null, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("dataCategory");
    }

    [Fact]
    public void Create_EmptyDataCategory_ThrowsArgumentException()
    {
        var act = () => RetentionPolicyAggregate.Create(
            Guid.NewGuid(), "", TimeSpan.FromDays(365), false,
            RetentionPolicyType.TimeBased, null, null, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("dataCategory");
    }

    [Fact]
    public void Create_WhitespaceDataCategory_ThrowsArgumentException()
    {
        var act = () => RetentionPolicyAggregate.Create(
            Guid.NewGuid(), "   ", TimeSpan.FromDays(365), false,
            RetentionPolicyType.TimeBased, null, null, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("dataCategory");
    }

    #endregion

    #region Create Guards — retentionPeriod

    [Fact]
    public void Create_ZeroRetentionPeriod_ThrowsArgumentOutOfRangeException()
    {
        var act = () => RetentionPolicyAggregate.Create(
            Guid.NewGuid(), "customer-data", TimeSpan.Zero, false,
            RetentionPolicyType.TimeBased, null, null, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentOutOfRangeException>(act).ParamName.ShouldBe("retentionPeriod");
    }

    [Fact]
    public void Create_NegativeRetentionPeriod_ThrowsArgumentOutOfRangeException()
    {
        var act = () => RetentionPolicyAggregate.Create(
            Guid.NewGuid(), "customer-data", TimeSpan.FromDays(-1), false,
            RetentionPolicyType.TimeBased, null, null, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentOutOfRangeException>(act).ParamName.ShouldBe("retentionPeriod");
    }

    #endregion

    #region Update Guards — retentionPeriod

    [Fact]
    public void Update_ZeroRetentionPeriod_ThrowsArgumentOutOfRangeException()
    {
        var aggregate = CreateActiveAggregate();

        var act = () => aggregate.Update(
            TimeSpan.Zero, false, null, null, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentOutOfRangeException>(act).ParamName.ShouldBe("retentionPeriod");
    }

    [Fact]
    public void Update_NegativeRetentionPeriod_ThrowsArgumentOutOfRangeException()
    {
        var aggregate = CreateActiveAggregate();

        var act = () => aggregate.Update(
            TimeSpan.FromDays(-1), false, null, null, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentOutOfRangeException>(act).ParamName.ShouldBe("retentionPeriod");
    }

    #endregion

    #region Deactivate Guards — reason

    [Fact]
    public void Deactivate_NullReason_ThrowsArgumentException()
    {
        var aggregate = CreateActiveAggregate();

        var act = () => aggregate.Deactivate(null!, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("reason");
    }

    [Fact]
    public void Deactivate_EmptyReason_ThrowsArgumentException()
    {
        var aggregate = CreateActiveAggregate();

        var act = () => aggregate.Deactivate("", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("reason");
    }

    [Fact]
    public void Deactivate_WhitespaceReason_ThrowsArgumentException()
    {
        var aggregate = CreateActiveAggregate();

        var act = () => aggregate.Deactivate("   ", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("reason");
    }

    #endregion

    #region Helpers

    private static RetentionPolicyAggregate CreateActiveAggregate()
    {
        return RetentionPolicyAggregate.Create(
            Guid.NewGuid(), "customer-data", TimeSpan.FromDays(365), false,
            RetentionPolicyType.TimeBased, null, null, DateTimeOffset.UtcNow);
    }

    #endregion
}
