#pragma warning disable CA2012

using Encina.Compliance.CrossBorderTransfer.Aggregates;
using Encina.Compliance.CrossBorderTransfer.Model;

using Shouldly;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.CrossBorderTransfer.Aggregates;

public class ApprovedTransferAggregateTests
{
    [Fact]
    public void Approve_ValidParams_SetsAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var sccId = Guid.NewGuid();
        var tiaId = Guid.NewGuid();
        var expiresAt = DateTimeOffset.UtcNow.AddYears(1);

        // Act
        var transfer = ApprovedTransferAggregate.Approve(
            id, "DE", "US", "personal-data", TransferBasis.SCCs,
            sccAgreementId: sccId, tiaId: tiaId, approvedBy: "admin",
            expiresAtUtc: expiresAt, tenantId: "tenant-1", moduleId: "module-1");

        // Assert
        transfer.Id.ShouldBe(id);
        transfer.SourceCountryCode.ShouldBe("DE");
        transfer.DestinationCountryCode.ShouldBe("US");
        transfer.DataCategory.ShouldBe("personal-data");
        transfer.Basis.ShouldBe(TransferBasis.SCCs);
        transfer.SCCAgreementId.ShouldBe(sccId);
        transfer.TIAId.ShouldBe(tiaId);
        transfer.ApprovedBy.ShouldBe("admin");
        transfer.ExpiresAtUtc.ShouldBe(expiresAt);
        transfer.TenantId.ShouldBe("tenant-1");
        transfer.ModuleId.ShouldBe("module-1");
        transfer.IsRevoked.ShouldBeFalse();
        transfer.IsExpired.ShouldBeFalse();
        transfer.RevokedAtUtc.ShouldBeNull();
    }

    [Fact]
    public void Approve_NullSourceCountryCode_ThrowsArgumentException()
    {
        // Arrange & Act
        var act = () => ApprovedTransferAggregate.Approve(
            Guid.NewGuid(), null!, "US", "personal-data", TransferBasis.SCCs, approvedBy: "admin");

        // Assert
        Should.Throw<ArgumentException>(act)
            .ParamName.ShouldBe("sourceCountryCode");
    }

    [Fact]
    public void Approve_BlockedBasis_ThrowsArgumentException()
    {
        // Arrange & Act
        var act = () => ApprovedTransferAggregate.Approve(
            Guid.NewGuid(), "DE", "US", "personal-data", TransferBasis.Blocked, approvedBy: "admin");

        // Assert
        Should.Throw<ArgumentException>(act)
            .ParamName.ShouldBe("basis");
    }

    [Fact]
    public void Revoke_ActiveTransfer_SetsIsRevoked()
    {
        // Arrange
        var transfer = CreateActiveTransfer();

        // Act
        transfer.Revoke("Data breach detected", "compliance-officer");

        // Assert
        transfer.IsRevoked.ShouldBeTrue();
        transfer.RevokedAtUtc.ShouldNotBeNull();
        transfer.RevokedAtUtc!.Value.ShouldBeInRange(DateTimeOffset.UtcNow - TimeSpan.FromSeconds(5), DateTimeOffset.UtcNow + TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Revoke_AlreadyRevoked_ThrowsInvalidOperation()
    {
        // Arrange
        var transfer = CreateRevokedTransfer();

        // Act
        var act = () => transfer.Revoke("Second revocation", "admin");

        // Assert
        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void Expire_ActiveTransfer_SetsIsExpired()
    {
        // Arrange
        var transfer = CreateActiveTransfer();

        // Act
        transfer.Expire();

        // Assert
        transfer.IsExpired.ShouldBeTrue();
    }

    [Fact]
    public void Expire_AlreadyExpired_ThrowsInvalidOperation()
    {
        // Arrange
        var transfer = CreateActiveTransfer();
        transfer.Expire();

        // Act
        var act = () => transfer.Expire();

        // Assert
        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void Expire_RevokedTransfer_ThrowsInvalidOperation()
    {
        // Arrange
        var transfer = CreateRevokedTransfer();

        // Act
        var act = () => transfer.Expire();

        // Assert
        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void Renew_ActiveTransfer_UpdatesExpiresAtUtc()
    {
        // Arrange
        var transfer = CreateActiveTransfer();
        var newExpiration = DateTimeOffset.UtcNow.AddYears(2);

        // Act
        transfer.Renew(newExpiration, "admin");

        // Assert
        transfer.ExpiresAtUtc.ShouldBe(newExpiration);
        transfer.IsExpired.ShouldBeFalse();
    }

    [Fact]
    public void Renew_RevokedTransfer_ThrowsInvalidOperation()
    {
        // Arrange
        var transfer = CreateRevokedTransfer();

        // Act
        var act = () => transfer.Renew(DateTimeOffset.UtcNow.AddYears(1), "admin");

        // Assert
        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void IsValid_ActiveNotExpired_ReturnsTrue()
    {
        // Arrange
        var transfer = ApprovedTransferAggregate.Approve(
            Guid.NewGuid(), "DE", "US", "personal-data", TransferBasis.SCCs,
            approvedBy: "admin", expiresAtUtc: DateTimeOffset.UtcNow.AddYears(1));

        // Act
        var result = transfer.IsValid(DateTimeOffset.UtcNow);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsValid_Revoked_ReturnsFalse()
    {
        // Arrange
        var transfer = CreateRevokedTransfer();

        // Act
        var result = transfer.IsValid(DateTimeOffset.UtcNow);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsValid_PastExpiration_ReturnsFalse()
    {
        // Arrange
        var transfer = ApprovedTransferAggregate.Approve(
            Guid.NewGuid(), "DE", "US", "personal-data", TransferBasis.SCCs,
            approvedBy: "admin", expiresAtUtc: DateTimeOffset.UtcNow.AddHours(-1));

        // Act
        var result = transfer.IsValid(DateTimeOffset.UtcNow);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsValid_NoExpiration_ReturnsTrue()
    {
        // Arrange
        var transfer = ApprovedTransferAggregate.Approve(
            Guid.NewGuid(), "DE", "US", "personal-data", TransferBasis.AdequacyDecision,
            approvedBy: "admin", expiresAtUtc: null);

        // Act
        var result = transfer.IsValid(DateTimeOffset.UtcNow);

        // Assert
        result.ShouldBeTrue();
    }

    // --- Helper methods ---

    private static ApprovedTransferAggregate CreateActiveTransfer()
    {
        return ApprovedTransferAggregate.Approve(
            Guid.NewGuid(), "DE", "US", "personal-data", TransferBasis.SCCs, approvedBy: "admin");
    }

    private static ApprovedTransferAggregate CreateRevokedTransfer()
    {
        var transfer = CreateActiveTransfer();
        transfer.Revoke("Compliance issue", "compliance-officer");
        return transfer;
    }
}
