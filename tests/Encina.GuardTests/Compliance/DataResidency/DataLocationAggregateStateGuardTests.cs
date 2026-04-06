using Encina.Compliance.DataResidency.Aggregates;
using Encina.Compliance.DataResidency.Model;

namespace Encina.GuardTests.Compliance.DataResidency;

/// <summary>
/// State transition guard tests for <see cref="DataLocationAggregate"/>.
/// Verifies InvalidOperationException is thrown when calling methods from invalid states.
/// </summary>
public class DataLocationAggregateStateGuardTests
{
    private static DataLocationAggregate CreateActive() =>
        DataLocationAggregate.Register(
            Guid.NewGuid(), "entity-1", "personal-data", "DE",
            StorageType.Primary, DateTimeOffset.UtcNow);

    private static DataLocationAggregate CreateRemoved()
    {
        var agg = CreateActive();
        agg.Remove("Data deleted");
        return agg;
    }

    private static DataLocationAggregate CreateWithViolation()
    {
        var agg = CreateActive();
        agg.DetectViolation("personal-data", "US", "Non-compliant region");
        return agg;
    }

    #region Migrate from removed

    [Fact]
    public void Migrate_FromRemovedStatus_ThrowsInvalidOperationException()
    {
        var agg = CreateRemoved();
        var act = () => agg.Migrate("FR", "Business need");
        Should.Throw<InvalidOperationException>(act);
    }

    #endregion

    #region Verify from removed

    [Fact]
    public void Verify_FromRemovedStatus_ThrowsInvalidOperationException()
    {
        var agg = CreateRemoved();
        var act = () => agg.Verify(DateTimeOffset.UtcNow);
        Should.Throw<InvalidOperationException>(act);
    }

    #endregion

    #region Remove from removed

    [Fact]
    public void Remove_FromRemovedStatus_ThrowsInvalidOperationException()
    {
        var agg = CreateRemoved();
        var act = () => agg.Remove("Already removed");
        Should.Throw<InvalidOperationException>(act);
    }

    #endregion

    #region DetectViolation state guards

    [Fact]
    public void DetectViolation_FromRemovedStatus_ThrowsInvalidOperationException()
    {
        var agg = CreateRemoved();
        var act = () => agg.DetectViolation("data", "US", "details");
        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void DetectViolation_WithExistingViolation_ThrowsInvalidOperationException()
    {
        var agg = CreateWithViolation();
        var act = () => agg.DetectViolation("data", "CN", "another violation");
        Should.Throw<InvalidOperationException>(act);
    }

    #endregion

    #region ResolveViolation state guards

    [Fact]
    public void ResolveViolation_WithoutViolation_ThrowsInvalidOperationException()
    {
        var agg = CreateActive();
        var act = () => agg.ResolveViolation("Migrated data");
        Should.Throw<InvalidOperationException>(act);
    }

    #endregion
}
