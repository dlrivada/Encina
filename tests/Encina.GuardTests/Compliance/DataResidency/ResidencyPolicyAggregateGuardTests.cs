using Encina.Compliance.DataResidency.Aggregates;
using Encina.Compliance.DataResidency.Model;

namespace Encina.GuardTests.Compliance.DataResidency;

/// <summary>
/// Guard tests for <see cref="ResidencyPolicyAggregate"/> to verify null, empty, whitespace,
/// and null collection parameter handling across all factory and instance methods.
/// </summary>
public class ResidencyPolicyAggregateGuardTests
{
    #region Create Guards — dataCategory

    [Fact]
    public void Create_NullDataCategory_ThrowsArgumentException()
    {
        var act = () => ResidencyPolicyAggregate.Create(
            Guid.NewGuid(), null!, ["DE"], false, [TransferLegalBasis.AdequacyDecision]);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("dataCategory");
    }

    [Fact]
    public void Create_EmptyDataCategory_ThrowsArgumentException()
    {
        var act = () => ResidencyPolicyAggregate.Create(
            Guid.NewGuid(), "", ["DE"], false, [TransferLegalBasis.AdequacyDecision]);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("dataCategory");
    }

    [Fact]
    public void Create_WhitespaceDataCategory_ThrowsArgumentException()
    {
        var act = () => ResidencyPolicyAggregate.Create(
            Guid.NewGuid(), "   ", ["DE"], false, [TransferLegalBasis.AdequacyDecision]);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("dataCategory");
    }

    #endregion

    #region Create Guards — allowedRegionCodes

    [Fact]
    public void Create_NullAllowedRegionCodes_ThrowsArgumentNullException()
    {
        var act = () => ResidencyPolicyAggregate.Create(
            Guid.NewGuid(), "personal-data", null!, false, [TransferLegalBasis.AdequacyDecision]);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("allowedRegionCodes");
    }

    #endregion

    #region Create Guards — allowedTransferBases

    [Fact]
    public void Create_NullAllowedTransferBases_ThrowsArgumentNullException()
    {
        var act = () => ResidencyPolicyAggregate.Create(
            Guid.NewGuid(), "personal-data", ["DE"], false, null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("allowedTransferBases");
    }

    #endregion

    #region Update Guards — allowedRegionCodes

    [Fact]
    public void Update_NullAllowedRegionCodes_ThrowsArgumentNullException()
    {
        var aggregate = CreateActiveAggregate();

        var act = () => aggregate.Update(null!, false, [TransferLegalBasis.AdequacyDecision]);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("allowedRegionCodes");
    }

    #endregion

    #region Update Guards — allowedTransferBases

    [Fact]
    public void Update_NullAllowedTransferBases_ThrowsArgumentNullException()
    {
        var aggregate = CreateActiveAggregate();

        var act = () => aggregate.Update(["FR"], false, null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("allowedTransferBases");
    }

    #endregion

    #region Delete Guards — reason

    [Fact]
    public void Delete_NullReason_ThrowsArgumentException()
    {
        var aggregate = CreateActiveAggregate();

        var act = () => aggregate.Delete(null!);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("reason");
    }

    [Fact]
    public void Delete_EmptyReason_ThrowsArgumentException()
    {
        var aggregate = CreateActiveAggregate();

        var act = () => aggregate.Delete("");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("reason");
    }

    [Fact]
    public void Delete_WhitespaceReason_ThrowsArgumentException()
    {
        var aggregate = CreateActiveAggregate();

        var act = () => aggregate.Delete("   ");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("reason");
    }

    #endregion

    #region Helpers

    private static ResidencyPolicyAggregate CreateActiveAggregate()
    {
        return ResidencyPolicyAggregate.Create(
            Guid.NewGuid(), "personal-data", ["DE", "FR"], false,
            [TransferLegalBasis.AdequacyDecision, TransferLegalBasis.StandardContractualClauses]);
    }

    #endregion
}
