#pragma warning disable CA2012

using Encina.Compliance.CrossBorderTransfer.Aggregates;
using Encina.Compliance.CrossBorderTransfer.Model;

namespace Encina.GuardTests.Compliance.CrossBorderTransfer;

/// <summary>
/// Guard tests for <see cref="ApprovedTransferAggregate"/> verifying argument validation
/// and state transition guards on all factory and instance methods.
/// </summary>
public class ApprovedTransferAggregateGuardTests
{
    #region Approve (Factory) Guards

    [Fact]
    public void Approve_NullSourceCountryCode_ThrowsArgumentException()
    {
        var act = () => ApprovedTransferAggregate.Approve(
            Guid.NewGuid(), null!, "US", "personal-data", TransferBasis.SCCs, approvedBy: "admin1");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("sourceCountryCode");
    }

    [Fact]
    public void Approve_EmptySourceCountryCode_ThrowsArgumentException()
    {
        var act = () => ApprovedTransferAggregate.Approve(
            Guid.NewGuid(), "", "US", "personal-data", TransferBasis.SCCs, approvedBy: "admin1");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("sourceCountryCode");
    }

    [Fact]
    public void Approve_WhitespaceSourceCountryCode_ThrowsArgumentException()
    {
        var act = () => ApprovedTransferAggregate.Approve(
            Guid.NewGuid(), "   ", "US", "personal-data", TransferBasis.SCCs, approvedBy: "admin1");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("sourceCountryCode");
    }

    [Fact]
    public void Approve_NullDestinationCountryCode_ThrowsArgumentException()
    {
        var act = () => ApprovedTransferAggregate.Approve(
            Guid.NewGuid(), "DE", null!, "personal-data", TransferBasis.SCCs, approvedBy: "admin1");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("destinationCountryCode");
    }

    [Fact]
    public void Approve_EmptyDestinationCountryCode_ThrowsArgumentException()
    {
        var act = () => ApprovedTransferAggregate.Approve(
            Guid.NewGuid(), "DE", "", "personal-data", TransferBasis.SCCs, approvedBy: "admin1");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("destinationCountryCode");
    }

    [Fact]
    public void Approve_WhitespaceDestinationCountryCode_ThrowsArgumentException()
    {
        var act = () => ApprovedTransferAggregate.Approve(
            Guid.NewGuid(), "DE", "   ", "personal-data", TransferBasis.SCCs, approvedBy: "admin1");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("destinationCountryCode");
    }

    [Fact]
    public void Approve_NullDataCategory_ThrowsArgumentException()
    {
        var act = () => ApprovedTransferAggregate.Approve(
            Guid.NewGuid(), "DE", "US", null!, TransferBasis.SCCs, approvedBy: "admin1");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("dataCategory");
    }

    [Fact]
    public void Approve_EmptyDataCategory_ThrowsArgumentException()
    {
        var act = () => ApprovedTransferAggregate.Approve(
            Guid.NewGuid(), "DE", "US", "", TransferBasis.SCCs, approvedBy: "admin1");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("dataCategory");
    }

    [Fact]
    public void Approve_WhitespaceDataCategory_ThrowsArgumentException()
    {
        var act = () => ApprovedTransferAggregate.Approve(
            Guid.NewGuid(), "DE", "US", "   ", TransferBasis.SCCs, approvedBy: "admin1");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("dataCategory");
    }

    [Fact]
    public void Approve_NullApprovedBy_ThrowsArgumentException()
    {
        var act = () => ApprovedTransferAggregate.Approve(
            Guid.NewGuid(), "DE", "US", "personal-data", TransferBasis.SCCs, approvedBy: null!);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("approvedBy");
    }

    [Fact]
    public void Approve_EmptyApprovedBy_ThrowsArgumentException()
    {
        var act = () => ApprovedTransferAggregate.Approve(
            Guid.NewGuid(), "DE", "US", "personal-data", TransferBasis.SCCs, approvedBy: "");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("approvedBy");
    }

    [Fact]
    public void Approve_WhitespaceApprovedBy_ThrowsArgumentException()
    {
        var act = () => ApprovedTransferAggregate.Approve(
            Guid.NewGuid(), "DE", "US", "personal-data", TransferBasis.SCCs, approvedBy: "   ");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("approvedBy");
    }

    [Fact]
    public void Approve_BlockedBasis_ThrowsArgumentException()
    {
        var act = () => ApprovedTransferAggregate.Approve(
            Guid.NewGuid(), "DE", "US", "personal-data", TransferBasis.Blocked, approvedBy: "admin1");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("basis");
    }

    [Fact]
    public void Approve_ValidParameters_ReturnsAggregate()
    {
        var aggregate = ApprovedTransferAggregate.Approve(
            Guid.NewGuid(), "DE", "US", "personal-data", TransferBasis.SCCs, approvedBy: "admin1");

        aggregate.ShouldNotBeNull();
        aggregate.SourceCountryCode.ShouldBe("DE");
        aggregate.DestinationCountryCode.ShouldBe("US");
        aggregate.DataCategory.ShouldBe("personal-data");
        aggregate.Basis.ShouldBe(TransferBasis.SCCs);
        aggregate.ApprovedBy.ShouldBe("admin1");
    }

    #endregion

    #region Revoke Guards

    [Fact]
    public void Revoke_NullReason_ThrowsArgumentException()
    {
        var sut = CreateActiveAggregate();

        var act = () => sut.Revoke(null!, "admin1");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("reason");
    }

    [Fact]
    public void Revoke_EmptyReason_ThrowsArgumentException()
    {
        var sut = CreateActiveAggregate();

        var act = () => sut.Revoke("", "admin1");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("reason");
    }

    [Fact]
    public void Revoke_WhitespaceReason_ThrowsArgumentException()
    {
        var sut = CreateActiveAggregate();

        var act = () => sut.Revoke("   ", "admin1");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("reason");
    }

    [Fact]
    public void Revoke_NullRevokedBy_ThrowsArgumentException()
    {
        var sut = CreateActiveAggregate();

        var act = () => sut.Revoke("Non-compliance", null!);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("revokedBy");
    }

    [Fact]
    public void Revoke_EmptyRevokedBy_ThrowsArgumentException()
    {
        var sut = CreateActiveAggregate();

        var act = () => sut.Revoke("Non-compliance", "");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("revokedBy");
    }

    [Fact]
    public void Revoke_WhitespaceRevokedBy_ThrowsArgumentException()
    {
        var sut = CreateActiveAggregate();

        var act = () => sut.Revoke("Non-compliance", "   ");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("revokedBy");
    }

    [Fact]
    public void Revoke_AlreadyRevoked_ThrowsInvalidOperationException()
    {
        var sut = CreateRevokedAggregate();

        var act = () => sut.Revoke("Another reason", "admin2");

        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void Revoke_ValidOnActive_SetsRevoked()
    {
        var sut = CreateActiveAggregate();

        sut.Revoke("Non-compliance", "admin1");

        sut.IsRevoked.ShouldBeTrue();
        sut.RevokedAtUtc.ShouldNotBeNull();
    }

    #endregion

    #region Expire Guards

    [Fact]
    public void Expire_AlreadyExpired_ThrowsInvalidOperationException()
    {
        var sut = CreateExpiredAggregate();

        var act = () => sut.Expire();

        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void Expire_WhenRevoked_ThrowsInvalidOperationException()
    {
        var sut = CreateRevokedAggregate();

        var act = () => sut.Expire();

        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void Expire_ValidOnActive_SetsExpired()
    {
        var sut = CreateActiveAggregate();

        sut.Expire();

        sut.IsExpired.ShouldBeTrue();
    }

    #endregion

    #region Renew Guards

    [Fact]
    public void Renew_NullRenewedBy_ThrowsArgumentException()
    {
        var sut = CreateActiveAggregate();

        var act = () => sut.Renew(DateTimeOffset.UtcNow.AddDays(365), null!);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("renewedBy");
    }

    [Fact]
    public void Renew_EmptyRenewedBy_ThrowsArgumentException()
    {
        var sut = CreateActiveAggregate();

        var act = () => sut.Renew(DateTimeOffset.UtcNow.AddDays(365), "");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("renewedBy");
    }

    [Fact]
    public void Renew_WhitespaceRenewedBy_ThrowsArgumentException()
    {
        var sut = CreateActiveAggregate();

        var act = () => sut.Renew(DateTimeOffset.UtcNow.AddDays(365), "   ");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("renewedBy");
    }

    [Fact]
    public void Renew_WhenRevoked_ThrowsInvalidOperationException()
    {
        var sut = CreateRevokedAggregate();

        var act = () => sut.Renew(DateTimeOffset.UtcNow.AddDays(365), "admin1");

        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void Renew_ValidOnActive_UpdatesExpiration()
    {
        var sut = CreateActiveAggregate();
        var newExpiry = DateTimeOffset.UtcNow.AddDays(365);

        sut.Renew(newExpiry, "admin1");

        sut.ExpiresAtUtc.ShouldBe(newExpiry);
        sut.IsExpired.ShouldBeFalse();
    }

    #endregion

    #region IsValid Guards

    [Fact]
    public void IsValid_ActiveTransfer_ReturnsTrue()
    {
        var sut = CreateActiveAggregate();

        sut.IsValid(DateTimeOffset.UtcNow).ShouldBeTrue();
    }

    [Fact]
    public void IsValid_RevokedTransfer_ReturnsFalse()
    {
        var sut = CreateRevokedAggregate();

        sut.IsValid(DateTimeOffset.UtcNow).ShouldBeFalse();
    }

    [Fact]
    public void IsValid_ExpiredTransfer_ReturnsFalse()
    {
        var sut = CreateExpiredAggregate();

        sut.IsValid(DateTimeOffset.UtcNow).ShouldBeFalse();
    }

    #endregion

    #region Helpers

    private static ApprovedTransferAggregate CreateActiveAggregate() =>
        ApprovedTransferAggregate.Approve(
            Guid.NewGuid(), "DE", "US", "personal-data", TransferBasis.SCCs, approvedBy: "admin1");

    private static ApprovedTransferAggregate CreateRevokedAggregate()
    {
        var agg = CreateActiveAggregate();
        agg.Revoke("Compliance violation", "admin1");
        return agg;
    }

    private static ApprovedTransferAggregate CreateExpiredAggregate()
    {
        var agg = CreateActiveAggregate();
        agg.Expire();
        return agg;
    }

    #endregion
}
