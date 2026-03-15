using Encina.Compliance.Consent;
using Encina.Compliance.Consent.Aggregates;

namespace Encina.GuardTests.Compliance.Consent;

/// <summary>
/// Guard tests for <see cref="ConsentAggregate"/> to verify null and invalid parameter handling
/// across all factory and instance methods.
/// </summary>
public class ConsentAggregateGuardTests
{
    private static readonly IReadOnlyDictionary<string, object?> ValidMetadata = new Dictionary<string, object?>();

    #region Grant Guards — dataSubjectId

    [Fact]
    public void Grant_NullDataSubjectId_ThrowsArgumentException()
    {
        var act = () => ConsentAggregate.Grant(
            Guid.NewGuid(), null!, "marketing", "v1", "web", null, null,
            ValidMetadata, null, "admin", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("dataSubjectId");
    }

    [Fact]
    public void Grant_EmptyDataSubjectId_ThrowsArgumentException()
    {
        var act = () => ConsentAggregate.Grant(
            Guid.NewGuid(), "", "marketing", "v1", "web", null, null,
            ValidMetadata, null, "admin", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("dataSubjectId");
    }

    [Fact]
    public void Grant_WhitespaceDataSubjectId_ThrowsArgumentException()
    {
        var act = () => ConsentAggregate.Grant(
            Guid.NewGuid(), "   ", "marketing", "v1", "web", null, null,
            ValidMetadata, null, "admin", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("dataSubjectId");
    }

    #endregion

    #region Grant Guards — purpose

    [Fact]
    public void Grant_NullPurpose_ThrowsArgumentException()
    {
        var act = () => ConsentAggregate.Grant(
            Guid.NewGuid(), "user-1", null!, "v1", "web", null, null,
            ValidMetadata, null, "admin", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("purpose");
    }

    [Fact]
    public void Grant_EmptyPurpose_ThrowsArgumentException()
    {
        var act = () => ConsentAggregate.Grant(
            Guid.NewGuid(), "user-1", "", "v1", "web", null, null,
            ValidMetadata, null, "admin", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("purpose");
    }

    [Fact]
    public void Grant_WhitespacePurpose_ThrowsArgumentException()
    {
        var act = () => ConsentAggregate.Grant(
            Guid.NewGuid(), "user-1", "   ", "v1", "web", null, null,
            ValidMetadata, null, "admin", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("purpose");
    }

    #endregion

    #region Grant Guards — consentVersionId

    [Fact]
    public void Grant_NullConsentVersionId_ThrowsArgumentException()
    {
        var act = () => ConsentAggregate.Grant(
            Guid.NewGuid(), "user-1", "marketing", null!, "web", null, null,
            ValidMetadata, null, "admin", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("consentVersionId");
    }

    [Fact]
    public void Grant_EmptyConsentVersionId_ThrowsArgumentException()
    {
        var act = () => ConsentAggregate.Grant(
            Guid.NewGuid(), "user-1", "marketing", "", "web", null, null,
            ValidMetadata, null, "admin", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("consentVersionId");
    }

    [Fact]
    public void Grant_WhitespaceConsentVersionId_ThrowsArgumentException()
    {
        var act = () => ConsentAggregate.Grant(
            Guid.NewGuid(), "user-1", "marketing", "   ", "web", null, null,
            ValidMetadata, null, "admin", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("consentVersionId");
    }

    #endregion

    #region Grant Guards — source

    [Fact]
    public void Grant_NullSource_ThrowsArgumentException()
    {
        var act = () => ConsentAggregate.Grant(
            Guid.NewGuid(), "user-1", "marketing", "v1", null!, null, null,
            ValidMetadata, null, "admin", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("source");
    }

    [Fact]
    public void Grant_EmptySource_ThrowsArgumentException()
    {
        var act = () => ConsentAggregate.Grant(
            Guid.NewGuid(), "user-1", "marketing", "v1", "", null, null,
            ValidMetadata, null, "admin", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("source");
    }

    [Fact]
    public void Grant_WhitespaceSource_ThrowsArgumentException()
    {
        var act = () => ConsentAggregate.Grant(
            Guid.NewGuid(), "user-1", "marketing", "v1", "   ", null, null,
            ValidMetadata, null, "admin", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("source");
    }

    #endregion

    #region Grant Guards — metadata

    [Fact]
    public void Grant_NullMetadata_ThrowsArgumentNullException()
    {
        var act = () => ConsentAggregate.Grant(
            Guid.NewGuid(), "user-1", "marketing", "v1", "web", null, null,
            null!, null, "admin", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("metadata");
    }

    #endregion

    #region Grant Guards — grantedBy

    [Fact]
    public void Grant_NullGrantedBy_ThrowsArgumentException()
    {
        var act = () => ConsentAggregate.Grant(
            Guid.NewGuid(), "user-1", "marketing", "v1", "web", null, null,
            ValidMetadata, null, null!, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("grantedBy");
    }

    [Fact]
    public void Grant_EmptyGrantedBy_ThrowsArgumentException()
    {
        var act = () => ConsentAggregate.Grant(
            Guid.NewGuid(), "user-1", "marketing", "v1", "web", null, null,
            ValidMetadata, null, "", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("grantedBy");
    }

    [Fact]
    public void Grant_WhitespaceGrantedBy_ThrowsArgumentException()
    {
        var act = () => ConsentAggregate.Grant(
            Guid.NewGuid(), "user-1", "marketing", "v1", "web", null, null,
            ValidMetadata, null, "   ", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("grantedBy");
    }

    #endregion

    #region Withdraw Guards — withdrawnBy

    [Fact]
    public void Withdraw_NullWithdrawnBy_ThrowsArgumentException()
    {
        var aggregate = CreateActiveAggregate();

        var act = () => aggregate.Withdraw(null!, null, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("withdrawnBy");
    }

    [Fact]
    public void Withdraw_EmptyWithdrawnBy_ThrowsArgumentException()
    {
        var aggregate = CreateActiveAggregate();

        var act = () => aggregate.Withdraw("", null, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("withdrawnBy");
    }

    [Fact]
    public void Withdraw_WhitespaceWithdrawnBy_ThrowsArgumentException()
    {
        var aggregate = CreateActiveAggregate();

        var act = () => aggregate.Withdraw("   ", null, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("withdrawnBy");
    }

    #endregion

    #region Renew Guards — consentVersionId

    [Fact]
    public void Renew_NullConsentVersionId_ThrowsArgumentException()
    {
        var aggregate = CreateActiveAggregate();

        var act = () => aggregate.Renew(null!, null, "admin", null, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("consentVersionId");
    }

    [Fact]
    public void Renew_EmptyConsentVersionId_ThrowsArgumentException()
    {
        var aggregate = CreateActiveAggregate();

        var act = () => aggregate.Renew("", null, "admin", null, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("consentVersionId");
    }

    [Fact]
    public void Renew_WhitespaceConsentVersionId_ThrowsArgumentException()
    {
        var aggregate = CreateActiveAggregate();

        var act = () => aggregate.Renew("   ", null, "admin", null, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("consentVersionId");
    }

    #endregion

    #region Renew Guards — renewedBy

    [Fact]
    public void Renew_NullRenewedBy_ThrowsArgumentException()
    {
        var aggregate = CreateActiveAggregate();

        var act = () => aggregate.Renew("v2", null, null!, null, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("renewedBy");
    }

    [Fact]
    public void Renew_EmptyRenewedBy_ThrowsArgumentException()
    {
        var aggregate = CreateActiveAggregate();

        var act = () => aggregate.Renew("v2", null, "", null, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("renewedBy");
    }

    [Fact]
    public void Renew_WhitespaceRenewedBy_ThrowsArgumentException()
    {
        var aggregate = CreateActiveAggregate();

        var act = () => aggregate.Renew("v2", null, "   ", null, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("renewedBy");
    }

    #endregion

    #region ChangeVersion Guards — newVersionId

    [Fact]
    public void ChangeVersion_NullNewVersionId_ThrowsArgumentException()
    {
        var aggregate = CreateActiveAggregate();

        var act = () => aggregate.ChangeVersion(null!, "updated terms", true, "admin", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("newVersionId");
    }

    [Fact]
    public void ChangeVersion_EmptyNewVersionId_ThrowsArgumentException()
    {
        var aggregate = CreateActiveAggregate();

        var act = () => aggregate.ChangeVersion("", "updated terms", true, "admin", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("newVersionId");
    }

    [Fact]
    public void ChangeVersion_WhitespaceNewVersionId_ThrowsArgumentException()
    {
        var aggregate = CreateActiveAggregate();

        var act = () => aggregate.ChangeVersion("   ", "updated terms", true, "admin", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("newVersionId");
    }

    #endregion

    #region ChangeVersion Guards — description

    [Fact]
    public void ChangeVersion_NullDescription_ThrowsArgumentException()
    {
        var aggregate = CreateActiveAggregate();

        var act = () => aggregate.ChangeVersion("v2", null!, true, "admin", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("description");
    }

    [Fact]
    public void ChangeVersion_EmptyDescription_ThrowsArgumentException()
    {
        var aggregate = CreateActiveAggregate();

        var act = () => aggregate.ChangeVersion("v2", "", true, "admin", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("description");
    }

    [Fact]
    public void ChangeVersion_WhitespaceDescription_ThrowsArgumentException()
    {
        var aggregate = CreateActiveAggregate();

        var act = () => aggregate.ChangeVersion("v2", "   ", true, "admin", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("description");
    }

    #endregion

    #region ChangeVersion Guards — changedBy

    [Fact]
    public void ChangeVersion_NullChangedBy_ThrowsArgumentException()
    {
        var aggregate = CreateActiveAggregate();

        var act = () => aggregate.ChangeVersion("v2", "updated terms", true, null!, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("changedBy");
    }

    [Fact]
    public void ChangeVersion_EmptyChangedBy_ThrowsArgumentException()
    {
        var aggregate = CreateActiveAggregate();

        var act = () => aggregate.ChangeVersion("v2", "updated terms", true, "", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("changedBy");
    }

    [Fact]
    public void ChangeVersion_WhitespaceChangedBy_ThrowsArgumentException()
    {
        var aggregate = CreateActiveAggregate();

        var act = () => aggregate.ChangeVersion("v2", "updated terms", true, "   ", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("changedBy");
    }

    #endregion

    #region ProvideReconsent Guards — newConsentVersionId

    [Fact]
    public void ProvideReconsent_NullNewConsentVersionId_ThrowsArgumentException()
    {
        var aggregate = CreateRequiresReconsentAggregate();

        var act = () => aggregate.ProvideReconsent(
            null!, "web", null, null, ValidMetadata, null, "admin", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("newConsentVersionId");
    }

    [Fact]
    public void ProvideReconsent_EmptyNewConsentVersionId_ThrowsArgumentException()
    {
        var aggregate = CreateRequiresReconsentAggregate();

        var act = () => aggregate.ProvideReconsent(
            "", "web", null, null, ValidMetadata, null, "admin", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("newConsentVersionId");
    }

    [Fact]
    public void ProvideReconsent_WhitespaceNewConsentVersionId_ThrowsArgumentException()
    {
        var aggregate = CreateRequiresReconsentAggregate();

        var act = () => aggregate.ProvideReconsent(
            "   ", "web", null, null, ValidMetadata, null, "admin", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("newConsentVersionId");
    }

    #endregion

    #region ProvideReconsent Guards — source

    [Fact]
    public void ProvideReconsent_NullSource_ThrowsArgumentException()
    {
        var aggregate = CreateRequiresReconsentAggregate();

        var act = () => aggregate.ProvideReconsent(
            "v2", null!, null, null, ValidMetadata, null, "admin", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("source");
    }

    [Fact]
    public void ProvideReconsent_EmptySource_ThrowsArgumentException()
    {
        var aggregate = CreateRequiresReconsentAggregate();

        var act = () => aggregate.ProvideReconsent(
            "v2", "", null, null, ValidMetadata, null, "admin", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("source");
    }

    [Fact]
    public void ProvideReconsent_WhitespaceSource_ThrowsArgumentException()
    {
        var aggregate = CreateRequiresReconsentAggregate();

        var act = () => aggregate.ProvideReconsent(
            "v2", "   ", null, null, ValidMetadata, null, "admin", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("source");
    }

    #endregion

    #region ProvideReconsent Guards — metadata

    [Fact]
    public void ProvideReconsent_NullMetadata_ThrowsArgumentNullException()
    {
        var aggregate = CreateRequiresReconsentAggregate();

        var act = () => aggregate.ProvideReconsent(
            "v2", "web", null, null, null!, null, "admin", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("metadata");
    }

    #endregion

    #region ProvideReconsent Guards — grantedBy

    [Fact]
    public void ProvideReconsent_NullGrantedBy_ThrowsArgumentException()
    {
        var aggregate = CreateRequiresReconsentAggregate();

        var act = () => aggregate.ProvideReconsent(
            "v2", "web", null, null, ValidMetadata, null, null!, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("grantedBy");
    }

    [Fact]
    public void ProvideReconsent_EmptyGrantedBy_ThrowsArgumentException()
    {
        var aggregate = CreateRequiresReconsentAggregate();

        var act = () => aggregate.ProvideReconsent(
            "v2", "web", null, null, ValidMetadata, null, "", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("grantedBy");
    }

    [Fact]
    public void ProvideReconsent_WhitespaceGrantedBy_ThrowsArgumentException()
    {
        var aggregate = CreateRequiresReconsentAggregate();

        var act = () => aggregate.ProvideReconsent(
            "v2", "web", null, null, ValidMetadata, null, "   ", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("grantedBy");
    }

    #endregion

    #region Helpers

    private static ConsentAggregate CreateActiveAggregate()
    {
        return ConsentAggregate.Grant(
            Guid.NewGuid(), "user-1", "marketing", "v1", "web",
            null, null, ValidMetadata, null, "admin", DateTimeOffset.UtcNow);
    }

    private static ConsentAggregate CreateRequiresReconsentAggregate()
    {
        var aggregate = CreateActiveAggregate();
        aggregate.ChangeVersion("v2", "Updated terms", requiresReconsent: true, "admin", DateTimeOffset.UtcNow);
        return aggregate;
    }

    #endregion
}
