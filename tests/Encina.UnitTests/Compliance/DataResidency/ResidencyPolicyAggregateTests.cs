using Encina.Compliance.DataResidency.Aggregates;
using Encina.Compliance.DataResidency.Events;
using Encina.Compliance.DataResidency.Model;

namespace Encina.UnitTests.Compliance.DataResidency;

public class ResidencyPolicyAggregateTests
{
    private static readonly IReadOnlyList<string> DefaultRegionCodes = ["EU", "DE", "FR"];
    private static readonly IReadOnlyList<TransferLegalBasis> DefaultTransferBases =
        [TransferLegalBasis.AdequacyDecision, TransferLegalBasis.StandardContractualClauses];

    #region Create

    [Fact]
    public void Create_SetsAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var policy = CreateActivePolicy(id, "personal-data", tenantId: "tenant-1", moduleId: "module-1");

        // Assert
        policy.Id.ShouldBe(id);
        policy.DataCategory.ShouldBe("personal-data");
        policy.AllowedRegionCodes.ShouldBe(DefaultRegionCodes);
        policy.RequireAdequacyDecision.ShouldBeTrue();
        policy.AllowedTransferBases.ShouldBe(DefaultTransferBases);
        policy.TenantId.ShouldBe("tenant-1");
        policy.ModuleId.ShouldBe("module-1");
    }

    [Fact]
    public void Create_SetsIsActiveTrue()
    {
        // Arrange & Act
        var policy = CreateActivePolicy();

        // Assert
        policy.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Create_RaisesResidencyPolicyCreatedEvent()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var policy = CreateActivePolicy(id, "healthcare-data", tenantId: "t1", moduleId: "m1");

        // Assert
        policy.UncommittedEvents.Count.ShouldBe(1);
        var evt = policy.UncommittedEvents[0].ShouldBeOfType<ResidencyPolicyCreated>();
        evt.PolicyId.ShouldBe(id);
        evt.DataCategory.ShouldBe("healthcare-data");
        evt.AllowedRegionCodes.ShouldBe(DefaultRegionCodes);
        evt.RequireAdequacyDecision.ShouldBeTrue();
        evt.AllowedTransferBases.ShouldBe(DefaultTransferBases);
        evt.TenantId.ShouldBe("t1");
        evt.ModuleId.ShouldBe("m1");
    }

    [Fact]
    public void Create_WithNullDataCategory_ThrowsArgumentException()
    {
        // Arrange & Act
        var act = () => ResidencyPolicyAggregate.Create(
            Guid.NewGuid(), null!, DefaultRegionCodes, true, DefaultTransferBases);

        // Assert
        act.ShouldThrow<ArgumentException>()
            .ParamName.ShouldBe("dataCategory");
    }

    [Fact]
    public void Create_WithWhitespaceDataCategory_ThrowsArgumentException()
    {
        // Arrange & Act
        var act = () => ResidencyPolicyAggregate.Create(
            Guid.NewGuid(), "  ", DefaultRegionCodes, true, DefaultTransferBases);

        // Assert
        act.ShouldThrow<ArgumentException>()
            .ParamName.ShouldBe("dataCategory");
    }

    [Fact]
    public void Create_WithNullAllowedRegionCodes_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => ResidencyPolicyAggregate.Create(
            Guid.NewGuid(), "personal-data", null!, true, DefaultTransferBases);

        // Assert
        act.ShouldThrow<ArgumentNullException>()
            .ParamName.ShouldBe("allowedRegionCodes");
    }

    [Fact]
    public void Create_WithNullAllowedTransferBases_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => ResidencyPolicyAggregate.Create(
            Guid.NewGuid(), "personal-data", DefaultRegionCodes, true, null!);

        // Assert
        act.ShouldThrow<ArgumentNullException>()
            .ParamName.ShouldBe("allowedTransferBases");
    }

    #endregion

    #region Update

    [Fact]
    public void Update_ChangesAllowedRegions()
    {
        // Arrange
        var policy = CreateActivePolicy();
        IReadOnlyList<string> newRegions = ["US", "CA"];
        IReadOnlyList<TransferLegalBasis> newBases = [TransferLegalBasis.ExplicitConsent];

        // Act
        policy.Update(newRegions, false, newBases);

        // Assert
        policy.AllowedRegionCodes.ShouldBe(newRegions);
        policy.RequireAdequacyDecision.ShouldBeFalse();
        policy.AllowedTransferBases.ShouldBe(newBases);
    }

    [Fact]
    public void Update_RaisesResidencyPolicyUpdatedEvent()
    {
        // Arrange
        var policy = CreateActivePolicy();
        IReadOnlyList<string> newRegions = ["JP"];
        IReadOnlyList<TransferLegalBasis> newBases = [TransferLegalBasis.BindingCorporateRules];

        // Act
        policy.Update(newRegions, false, newBases);

        // Assert
        policy.UncommittedEvents.Count.ShouldBe(2); // Created + Updated
        var evt = policy.UncommittedEvents[1].ShouldBeOfType<ResidencyPolicyUpdated>();
        evt.PolicyId.ShouldBe(policy.Id);
        evt.AllowedRegionCodes.ShouldBe(newRegions);
        evt.RequireAdequacyDecision.ShouldBeFalse();
        evt.AllowedTransferBases.ShouldBe(newBases);
    }

    [Fact]
    public void Update_WhenDeleted_ThrowsInvalidOperationException()
    {
        // Arrange
        var policy = CreateDeletedPolicy();

        // Act
        var act = () => policy.Update(["US"], false, [TransferLegalBasis.AdequacyDecision]);

        // Assert
        act.ShouldThrow<InvalidOperationException>();
    }

    [Fact]
    public void Update_WithNullRegionCodes_ThrowsArgumentNullException()
    {
        // Arrange
        var policy = CreateActivePolicy();

        // Act
        var act = () => policy.Update(null!, true, DefaultTransferBases);

        // Assert
        act.ShouldThrow<ArgumentNullException>()
            .ParamName.ShouldBe("allowedRegionCodes");
    }

    [Fact]
    public void Update_WithNullTransferBases_ThrowsArgumentNullException()
    {
        // Arrange
        var policy = CreateActivePolicy();

        // Act
        var act = () => policy.Update(DefaultRegionCodes, true, null!);

        // Assert
        act.ShouldThrow<ArgumentNullException>()
            .ParamName.ShouldBe("allowedTransferBases");
    }

    #endregion

    #region Delete

    [Fact]
    public void Delete_SetsIsActiveFalse()
    {
        // Arrange
        var policy = CreateActivePolicy();

        // Act
        policy.Delete("No longer required");

        // Assert
        policy.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void Delete_RaisesResidencyPolicyDeletedEvent()
    {
        // Arrange
        var policy = CreateActivePolicy();

        // Act
        policy.Delete("Regulatory change");

        // Assert
        policy.UncommittedEvents.Count.ShouldBe(2); // Created + Deleted
        var evt = policy.UncommittedEvents[1].ShouldBeOfType<ResidencyPolicyDeleted>();
        evt.PolicyId.ShouldBe(policy.Id);
        evt.Reason.ShouldBe("Regulatory change");
    }

    [Fact]
    public void Delete_WhenAlreadyDeleted_ThrowsInvalidOperationException()
    {
        // Arrange
        var policy = CreateDeletedPolicy();

        // Act
        var act = () => policy.Delete("Another reason");

        // Assert
        act.ShouldThrow<InvalidOperationException>();
    }

    [Fact]
    public void Delete_WithNullReason_ThrowsArgumentException()
    {
        // Arrange
        var policy = CreateActivePolicy();

        // Act
        var act = () => policy.Delete(null!);

        // Assert
        act.ShouldThrow<ArgumentException>()
            .ParamName.ShouldBe("reason");
    }

    [Fact]
    public void Delete_WithWhitespaceReason_ThrowsArgumentException()
    {
        // Arrange
        var policy = CreateActivePolicy();

        // Act
        var act = () => policy.Delete("   ");

        // Assert
        act.ShouldThrow<ArgumentException>()
            .ParamName.ShouldBe("reason");
    }

    #endregion

    #region Helpers

    private static ResidencyPolicyAggregate CreateActivePolicy(
        Guid? id = null,
        string dataCategory = "personal-data",
        string? tenantId = null,
        string? moduleId = null)
    {
        return ResidencyPolicyAggregate.Create(
            id ?? Guid.NewGuid(),
            dataCategory,
            DefaultRegionCodes,
            requireAdequacyDecision: true,
            DefaultTransferBases,
            tenantId,
            moduleId);
    }

    private static ResidencyPolicyAggregate CreateDeletedPolicy()
    {
        var policy = CreateActivePolicy();
        policy.Delete("No longer needed");
        return policy;
    }

    #endregion
}
