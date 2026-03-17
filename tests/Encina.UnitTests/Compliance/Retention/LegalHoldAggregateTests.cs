using Encina.Compliance.Retention.Aggregates;
using Encina.Compliance.Retention.Events;
using FluentAssertions;

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
        hold.Id.Should().Be(DefaultId);
        hold.EntityId.Should().Be(entityId);
        hold.Reason.Should().Be(reason);
        hold.AppliedByUserId.Should().Be(appliedByUserId);
        hold.IsActive.Should().BeTrue();
        hold.AppliedAtUtc.Should().Be(Now);
        hold.TenantId.Should().Be(tenantId);
        hold.ModuleId.Should().Be(moduleId);
        hold.ReleasedByUserId.Should().BeNull();
        hold.ReleasedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Place_ValidParameters_ShouldRaiseLegalHoldPlacedEvent()
    {
        // Act
        var hold = CreateActiveHold();

        // Assert
        hold.UncommittedEvents.Should().HaveCount(1);
        hold.UncommittedEvents[0].Should().BeOfType<LegalHoldPlaced>();
    }

    [Fact]
    public void Place_ValidParameters_HoldIdShouldMatchUncommittedEvent()
    {
        // Act
        var hold = CreateActiveHold();

        // Assert
        var evt = hold.UncommittedEvents[0].Should().BeOfType<LegalHoldPlaced>().Subject;
        evt.HoldId.Should().Be(hold.Id);
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
        act.Should().Throw<ArgumentException>();
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
        act.Should().Throw<ArgumentException>();
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
        act.Should().Throw<ArgumentException>();
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
        hold.IsActive.Should().BeFalse();
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
        hold.ReleasedByUserId.Should().Be(releasedBy);
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
        hold.ReleasedAtUtc.Should().Be(releaseTime);
    }

    [Fact]
    public void Lift_ActiveHold_ShouldRaiseLegalHoldLiftedEvent()
    {
        // Arrange
        var hold = CreateActiveHold();

        // Act
        hold.Lift("legal-counsel-2", Now.AddDays(30));

        // Assert
        hold.UncommittedEvents.Should().HaveCount(2);
        hold.UncommittedEvents[1].Should().BeOfType<LegalHoldLifted>();
    }

    [Fact]
    public void Lift_AlreadyLiftedHold_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var hold = CreateLiftedHold();

        // Act
        var act = () => hold.Lift("another-user", Now.AddDays(60));

        // Assert
        act.Should().Throw<InvalidOperationException>();
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
        act.Should().Throw<ArgumentException>();
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
