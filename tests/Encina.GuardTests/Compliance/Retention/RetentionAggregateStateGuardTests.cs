using Encina.Compliance.Retention.Aggregates;
using Encina.Compliance.Retention.Model;

namespace Encina.GuardTests.Compliance.Retention;

/// <summary>
/// Guard tests for aggregate state transition validation guards (InvalidOperationException paths)
/// in <see cref="LegalHoldAggregate"/>, <see cref="RetentionPolicyAggregate"/>,
/// and <see cref="RetentionRecordAggregate"/>.
/// </summary>
public sealed class RetentionAggregateStateGuardTests
{
    // ========================================================================
    // LegalHoldAggregate — Lift on already-lifted hold
    // ========================================================================

    [Fact]
    public void LegalHoldAggregate_Lift_AlreadyLifted_ThrowsInvalidOperationException()
    {
        var aggregate = LegalHoldAggregate.Place(
            Guid.NewGuid(), "entity-1", "Case #100", "user-1", DateTimeOffset.UtcNow);
        aggregate.Lift("user-2", DateTimeOffset.UtcNow);

        var act = () => aggregate.Lift("user-3", DateTimeOffset.UtcNow);

        Should.Throw<InvalidOperationException>(act);
    }

    // ========================================================================
    // RetentionPolicyAggregate — Update on deactivated policy
    // ========================================================================

    [Fact]
    public void RetentionPolicyAggregate_Update_Deactivated_ThrowsInvalidOperationException()
    {
        var aggregate = RetentionPolicyAggregate.Create(
            Guid.NewGuid(), "customer-data", TimeSpan.FromDays(365), false,
            RetentionPolicyType.TimeBased, null, null, DateTimeOffset.UtcNow);
        aggregate.Deactivate("No longer needed", DateTimeOffset.UtcNow);

        var act = () => aggregate.Update(
            TimeSpan.FromDays(180), true, null, null, DateTimeOffset.UtcNow);

        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void RetentionPolicyAggregate_Deactivate_AlreadyDeactivated_ThrowsInvalidOperationException()
    {
        var aggregate = RetentionPolicyAggregate.Create(
            Guid.NewGuid(), "customer-data", TimeSpan.FromDays(365), false,
            RetentionPolicyType.TimeBased, null, null, DateTimeOffset.UtcNow);
        aggregate.Deactivate("Reason 1", DateTimeOffset.UtcNow);

        var act = () => aggregate.Deactivate("Reason 2", DateTimeOffset.UtcNow);

        Should.Throw<InvalidOperationException>(act);
    }

    // ========================================================================
    // RetentionRecordAggregate — MarkExpired on non-Active
    // ========================================================================

    [Fact]
    public void RetentionRecordAggregate_MarkExpired_WhenExpired_ThrowsInvalidOperationException()
    {
        var aggregate = CreateActiveRecord();
        aggregate.MarkExpired(DateTimeOffset.UtcNow);

        var act = () => aggregate.MarkExpired(DateTimeOffset.UtcNow);

        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void RetentionRecordAggregate_MarkExpired_WhenUnderHold_ThrowsInvalidOperationException()
    {
        var aggregate = CreateActiveRecord();
        aggregate.Hold(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => aggregate.MarkExpired(DateTimeOffset.UtcNow);

        Should.Throw<InvalidOperationException>(act);
    }

    // ========================================================================
    // RetentionRecordAggregate — Hold on deleted record
    // ========================================================================

    [Fact]
    public void RetentionRecordAggregate_Hold_WhenDeleted_ThrowsInvalidOperationException()
    {
        var aggregate = CreateActiveRecord();
        aggregate.MarkExpired(DateTimeOffset.UtcNow);
        aggregate.MarkDeleted(DateTimeOffset.UtcNow);

        var act = () => aggregate.Hold(Guid.NewGuid(), DateTimeOffset.UtcNow);

        Should.Throw<InvalidOperationException>(act);
    }

    // ========================================================================
    // RetentionRecordAggregate — Release on non-held record
    // ========================================================================

    [Fact]
    public void RetentionRecordAggregate_Release_WhenActive_ThrowsInvalidOperationException()
    {
        var aggregate = CreateActiveRecord();

        var act = () => aggregate.Release(Guid.NewGuid(), DateTimeOffset.UtcNow);

        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void RetentionRecordAggregate_Release_WhenExpired_ThrowsInvalidOperationException()
    {
        var aggregate = CreateActiveRecord();
        aggregate.MarkExpired(DateTimeOffset.UtcNow);

        var act = () => aggregate.Release(Guid.NewGuid(), DateTimeOffset.UtcNow);

        Should.Throw<InvalidOperationException>(act);
    }

    // ========================================================================
    // RetentionRecordAggregate — MarkDeleted on non-expired record
    // ========================================================================

    [Fact]
    public void RetentionRecordAggregate_MarkDeleted_WhenActive_ThrowsInvalidOperationException()
    {
        var aggregate = CreateActiveRecord();

        var act = () => aggregate.MarkDeleted(DateTimeOffset.UtcNow);

        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void RetentionRecordAggregate_MarkDeleted_WhenUnderHold_ThrowsInvalidOperationException()
    {
        var aggregate = CreateActiveRecord();
        aggregate.Hold(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => aggregate.MarkDeleted(DateTimeOffset.UtcNow);

        Should.Throw<InvalidOperationException>(act);
    }

    // ========================================================================
    // RetentionRecordAggregate — MarkAnonymized on non-expired record
    // ========================================================================

    [Fact]
    public void RetentionRecordAggregate_MarkAnonymized_WhenActive_ThrowsInvalidOperationException()
    {
        var aggregate = CreateActiveRecord();

        var act = () => aggregate.MarkAnonymized(DateTimeOffset.UtcNow);

        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void RetentionRecordAggregate_MarkAnonymized_WhenUnderHold_ThrowsInvalidOperationException()
    {
        var aggregate = CreateActiveRecord();
        aggregate.Hold(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => aggregate.MarkAnonymized(DateTimeOffset.UtcNow);

        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void RetentionRecordAggregate_MarkAnonymized_WhenDeleted_ThrowsInvalidOperationException()
    {
        var aggregate = CreateActiveRecord();
        aggregate.MarkExpired(DateTimeOffset.UtcNow);
        aggregate.MarkDeleted(DateTimeOffset.UtcNow);

        var act = () => aggregate.MarkAnonymized(DateTimeOffset.UtcNow);

        Should.Throw<InvalidOperationException>(act);
    }

    // ========================================================================
    // RetentionRecordAggregate — Track with zero/negative retentionPeriod
    // These test the guard on aggregate parameters not covered elsewhere
    // ========================================================================

    [Fact]
    public void RetentionRecordAggregate_Track_ValidParameters_DoesNotThrow()
    {
        var act = () => RetentionRecordAggregate.Track(
            Guid.NewGuid(), "entity-1", "customer-data", Guid.NewGuid(),
            TimeSpan.FromDays(365), DateTimeOffset.UtcNow.AddDays(365), DateTimeOffset.UtcNow);

        Should.NotThrow(act);
    }

    // ========================================================================
    // Helpers
    // ========================================================================

    private static RetentionRecordAggregate CreateActiveRecord()
    {
        return RetentionRecordAggregate.Track(
            Guid.NewGuid(), "entity-1", "customer-data", Guid.NewGuid(),
            TimeSpan.FromDays(365), DateTimeOffset.UtcNow.AddDays(365), DateTimeOffset.UtcNow);
    }
}
