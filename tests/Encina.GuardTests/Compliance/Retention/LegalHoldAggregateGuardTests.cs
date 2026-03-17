using Encina.Compliance.Retention.Aggregates;

namespace Encina.GuardTests.Compliance.Retention;

/// <summary>
/// Guard tests for <see cref="LegalHoldAggregate"/> to verify null, empty, and whitespace
/// parameter handling across all factory and instance methods.
/// </summary>
public class LegalHoldAggregateGuardTests
{
    #region Place Guards — entityId

    [Fact]
    public void Place_NullEntityId_ThrowsArgumentException()
    {
        var act = () => LegalHoldAggregate.Place(
            Guid.NewGuid(), null!, "Ongoing litigation - Case #12345",
            "user-1", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("entityId");
    }

    [Fact]
    public void Place_EmptyEntityId_ThrowsArgumentException()
    {
        var act = () => LegalHoldAggregate.Place(
            Guid.NewGuid(), "", "Ongoing litigation - Case #12345",
            "user-1", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("entityId");
    }

    [Fact]
    public void Place_WhitespaceEntityId_ThrowsArgumentException()
    {
        var act = () => LegalHoldAggregate.Place(
            Guid.NewGuid(), "   ", "Ongoing litigation - Case #12345",
            "user-1", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("entityId");
    }

    #endregion

    #region Place Guards — reason

    [Fact]
    public void Place_NullReason_ThrowsArgumentException()
    {
        var act = () => LegalHoldAggregate.Place(
            Guid.NewGuid(), "entity-1", null!,
            "user-1", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("reason");
    }

    [Fact]
    public void Place_EmptyReason_ThrowsArgumentException()
    {
        var act = () => LegalHoldAggregate.Place(
            Guid.NewGuid(), "entity-1", "",
            "user-1", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("reason");
    }

    [Fact]
    public void Place_WhitespaceReason_ThrowsArgumentException()
    {
        var act = () => LegalHoldAggregate.Place(
            Guid.NewGuid(), "entity-1", "   ",
            "user-1", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("reason");
    }

    #endregion

    #region Place Guards — appliedByUserId

    [Fact]
    public void Place_NullAppliedByUserId_ThrowsArgumentException()
    {
        var act = () => LegalHoldAggregate.Place(
            Guid.NewGuid(), "entity-1", "Ongoing litigation - Case #12345",
            null!, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("appliedByUserId");
    }

    [Fact]
    public void Place_EmptyAppliedByUserId_ThrowsArgumentException()
    {
        var act = () => LegalHoldAggregate.Place(
            Guid.NewGuid(), "entity-1", "Ongoing litigation - Case #12345",
            "", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("appliedByUserId");
    }

    [Fact]
    public void Place_WhitespaceAppliedByUserId_ThrowsArgumentException()
    {
        var act = () => LegalHoldAggregate.Place(
            Guid.NewGuid(), "entity-1", "Ongoing litigation - Case #12345",
            "   ", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("appliedByUserId");
    }

    #endregion

    #region Lift Guards — releasedByUserId

    [Fact]
    public void Lift_NullReleasedByUserId_ThrowsArgumentException()
    {
        var aggregate = CreateActiveHold();

        var act = () => aggregate.Lift(null!, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("releasedByUserId");
    }

    [Fact]
    public void Lift_EmptyReleasedByUserId_ThrowsArgumentException()
    {
        var aggregate = CreateActiveHold();

        var act = () => aggregate.Lift("", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("releasedByUserId");
    }

    [Fact]
    public void Lift_WhitespaceReleasedByUserId_ThrowsArgumentException()
    {
        var aggregate = CreateActiveHold();

        var act = () => aggregate.Lift("   ", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("releasedByUserId");
    }

    #endregion

    #region Helpers

    private static LegalHoldAggregate CreateActiveHold()
    {
        return LegalHoldAggregate.Place(
            Guid.NewGuid(), "entity-1", "Ongoing litigation - Case #12345",
            "user-1", DateTimeOffset.UtcNow);
    }

    #endregion
}
