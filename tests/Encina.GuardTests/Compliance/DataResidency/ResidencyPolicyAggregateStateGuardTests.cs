using Encina.Compliance.DataResidency.Aggregates;
using Encina.Compliance.DataResidency.Model;

namespace Encina.GuardTests.Compliance.DataResidency;

/// <summary>
/// State transition guard tests for <see cref="ResidencyPolicyAggregate"/>.
/// Verifies InvalidOperationException is thrown when calling methods from invalid states.
/// </summary>
public class ResidencyPolicyAggregateStateGuardTests
{
    private static ResidencyPolicyAggregate CreateActive() =>
        ResidencyPolicyAggregate.Create(
            Guid.NewGuid(), "healthcare-data", ["DE", "FR"], false,
            [TransferLegalBasis.AdequacyDecision]);

    private static ResidencyPolicyAggregate CreateDeleted()
    {
        var agg = CreateActive();
        agg.Delete("No longer needed");
        return agg;
    }

    #region Update from deleted

    [Fact]
    public void Update_FromDeletedStatus_ThrowsInvalidOperationException()
    {
        var agg = CreateDeleted();
        var act = () => agg.Update(["US"], true, [TransferLegalBasis.StandardContractualClauses]);
        Should.Throw<InvalidOperationException>(act);
    }

    #endregion

    #region Delete from deleted

    [Fact]
    public void Delete_FromDeletedStatus_ThrowsInvalidOperationException()
    {
        var agg = CreateDeleted();
        var act = () => agg.Delete("Already deleted");
        Should.Throw<InvalidOperationException>(act);
    }

    #endregion
}
