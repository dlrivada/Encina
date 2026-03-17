using Encina.Compliance.Retention.Aggregates;

namespace Encina.GuardTests.Compliance.Retention;

/// <summary>
/// Guard tests for <see cref="RetentionRecordAggregate"/> to verify null, empty, and whitespace
/// parameter handling across all factory and instance methods.
/// </summary>
public class RetentionRecordAggregateGuardTests
{
    #region Track Guards — entityId

    [Fact]
    public void Track_NullEntityId_ThrowsArgumentException()
    {
        var act = () => RetentionRecordAggregate.Track(
            Guid.NewGuid(), null!, "customer-data", Guid.NewGuid(),
            TimeSpan.FromDays(365), DateTimeOffset.UtcNow.AddDays(365), DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("entityId");
    }

    [Fact]
    public void Track_EmptyEntityId_ThrowsArgumentException()
    {
        var act = () => RetentionRecordAggregate.Track(
            Guid.NewGuid(), "", "customer-data", Guid.NewGuid(),
            TimeSpan.FromDays(365), DateTimeOffset.UtcNow.AddDays(365), DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("entityId");
    }

    [Fact]
    public void Track_WhitespaceEntityId_ThrowsArgumentException()
    {
        var act = () => RetentionRecordAggregate.Track(
            Guid.NewGuid(), "   ", "customer-data", Guid.NewGuid(),
            TimeSpan.FromDays(365), DateTimeOffset.UtcNow.AddDays(365), DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("entityId");
    }

    #endregion

    #region Track Guards — dataCategory

    [Fact]
    public void Track_NullDataCategory_ThrowsArgumentException()
    {
        var act = () => RetentionRecordAggregate.Track(
            Guid.NewGuid(), "entity-1", null!, Guid.NewGuid(),
            TimeSpan.FromDays(365), DateTimeOffset.UtcNow.AddDays(365), DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("dataCategory");
    }

    [Fact]
    public void Track_EmptyDataCategory_ThrowsArgumentException()
    {
        var act = () => RetentionRecordAggregate.Track(
            Guid.NewGuid(), "entity-1", "", Guid.NewGuid(),
            TimeSpan.FromDays(365), DateTimeOffset.UtcNow.AddDays(365), DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("dataCategory");
    }

    [Fact]
    public void Track_WhitespaceDataCategory_ThrowsArgumentException()
    {
        var act = () => RetentionRecordAggregate.Track(
            Guid.NewGuid(), "entity-1", "   ", Guid.NewGuid(),
            TimeSpan.FromDays(365), DateTimeOffset.UtcNow.AddDays(365), DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("dataCategory");
    }

    #endregion
}
