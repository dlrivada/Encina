using Encina.Compliance.Retention.Aggregates;
using Encina.Compliance.Retention.Events;
using Shouldly;

namespace Encina.UnitTests.Compliance.Retention;

/// <summary>
/// Unit tests for <see cref="LegalHoldAggregate"/>.
/// </summary>
public class LegalHoldAggregateTests
{
    private static readonly Guid DefaultId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);

    #region Place Tests

    [Fact]
    public void Place_ValidParameters_ShouldSetAllProperties()
    {
        // Arrange
        var entityId = "customer-42";
        var reason = "Ongoing litigation - Case #12345";
        var appliedByUserId = "legal-counsel-1";
        var tenantId = "tenant-1";
        var moduleId = "module-legal";

        // Act
        var hold = LegalHoldAggregate.Place(
            DefaultId, entityId, reason, appliedByUserId, Now, tenantId, moduleId);

        // Assert
        hold.Id.ShouldBe(DefaultId);
        hold.EntityId.ShouldBe(entityId);
        hold.Reason.ShouldBe(reason);
        hold.AppliedByUserId.ShouldBe(appliedByUserId);
        hold.IsActive.ShouldBeTrue();
        hold.AppliedAtUtc.ShouldBe(Now);
        hold.TenantId.ShouldBe(tenantId);
        hold.ModuleId.ShouldBe(moduleId);
        hold.ReleasedByUserId.ShouldBeNull();
        hold.ReleasedAtUtc.ShouldBeNull();
    }

    [Fact]
    public void Place_ValidParameters_ShouldRaiseLegalHoldPlacedEvent()
    {
        // Act
        var hold = CreateActiveHold();

        // Assert
        hold.UncommittedEvents.Count.ShouldBe(1);
        hold.UncommittedEvents[0].ShouldBeOfType<LegalHoldPlaced>();
    }

    [Fact]
    public void Place_ValidParameters_HoldIdShouldMatchUncommittedEvent()
    {
        // Act
        var hold = CreateActiveHold();

        // Assert
        var evt = hold.UncommittedEvents[0].ShouldBeOfType<LegalHoldPlaced>().Subject;
        evt.HoldId.ShouldBe(hold.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Place_NullOrWhitespaceEntityId_ShouldThrowArgumentException(string? entityId)
    {
        // Act
        var act = () => LegalHoldAggregate.Place(
            DefaultId, entityId!, "litigation reason", "user-1", Now);

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Place_NullOrWhitespaceReason_ShouldThrowArgumentException(string? reason)
    {
        // Act
        var act = () => LegalHoldAggregate.Place(
            DefaultId, "customer-42", reason!, "user-1", Now);

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Place_NullOrWhitespaceAppliedByUserId_ShouldThrowArgumentException(string? appliedByUserId)
    {
        // Act
        var act = () => LegalHoldAggregate.Place(
            DefaultId, "customer-42", "litigation reason", appliedByUserId!, Now);

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    #endregion

    #region Lift Tests

    [Fact]
    public void Lift_ActiveHold_ShouldSetIsActiveFalse()
    {
        // Arrange
        var hold = CreateActiveHold();

        // Act
        hold.Lift("legal-counsel-2", Now.AddDays(30));

        // Assert
        hold.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void Lift_ActiveHold_ShouldSetReleasedByUserId()
    {
        // Arrange
        var hold = CreateActiveHold();
        var releasedBy = "legal-counsel-2";

        // Act
        hold.Lift(releasedBy, Now.AddDays(30));

        // Assert
        hold.ReleasedByUserId.ShouldBe(releasedBy);
    }

    [Fact]
    public void Lift_ActiveHold_ShouldSetReleasedAtUtc()
    {
        // Arrange
        var hold = CreateActiveHold();
        var releaseTime = Now.AddDays(30);

        // Act
        hold.Lift("legal-counsel-2", releaseTime);

        // Assert
        hold.ReleasedAtUtc.ShouldBe(releaseTime);
    }

    [Fact]
    public void Lift_ActiveHold_ShouldRaiseLegalHoldLiftedEvent()
    {
        // Arrange
        var hold = CreateActiveHold();

        // Act
        hold.Lift("legal-counsel-2", Now.AddDays(30));

        // Assert
        hold.UncommittedEvents.Count.ShouldBe(2);
        hold.UncommittedEvents[1].ShouldBeOfType<LegalHoldLifted>();
    }

    [Fact]
    public void Lift_AlreadyLiftedHold_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var hold = CreateLiftedHold();

        // Act
        var act = () => hold.Lift("another-user", Now.AddDays(60));

        // Assert
        Should.Throw<InvalidOperationException>(act);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Lift_NullOrWhitespaceReleasedByUserId_ShouldThrowArgumentException(string? releasedByUserId)
    {
        // Arrange
        var hold = CreateActiveHold();

        // Act
        var act = () => hold.Lift(releasedByUserId!, Now.AddDays(30));

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    #endregion

    #region Helpers

    private static LegalHoldAggregate CreateActiveHold() =>
        LegalHoldAggregate.Place(
            DefaultId, "customer-42", "Ongoing litigation - Case #12345",
            "legal-counsel-1", Now, "tenant-1", "module-legal");

    private static LegalHoldAggregate CreateLiftedHold()
    {
        var hold = CreateActiveHold();
        hold.Lift("legal-counsel-2", Now.AddDays(30));
        return hold;
    }

    #endregion
}
