#pragma warning disable CA2012

using Encina.Compliance.CrossBorderTransfer.Aggregates;
using Encina.Compliance.CrossBorderTransfer.Model;

using FluentAssertions;

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
        transfer.Id.Should().Be(id);
        transfer.SourceCountryCode.Should().Be("DE");
        transfer.DestinationCountryCode.Should().Be("US");
        transfer.DataCategory.Should().Be("personal-data");
        transfer.Basis.Should().Be(TransferBasis.SCCs);
        transfer.SCCAgreementId.Should().Be(sccId);
        transfer.TIAId.Should().Be(tiaId);
        transfer.ApprovedBy.Should().Be("admin");
        transfer.ExpiresAtUtc.Should().Be(expiresAt);
        transfer.TenantId.Should().Be("tenant-1");
        transfer.ModuleId.Should().Be("module-1");
        transfer.IsRevoked.Should().BeFalse();
        transfer.IsExpired.Should().BeFalse();
        transfer.RevokedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Approve_NullSourceCountryCode_ThrowsArgumentException()
    {
        // Arrange & Act
        var act = () => ApprovedTransferAggregate.Approve(
            Guid.NewGuid(), null!, "US", "personal-data", TransferBasis.SCCs, approvedBy: "admin");

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("sourceCountryCode");
    }

    [Fact]
    public void Approve_BlockedBasis_ThrowsArgumentException()
    {
        // Arrange & Act
        var act = () => ApprovedTransferAggregate.Approve(
            Guid.NewGuid(), "DE", "US", "personal-data", TransferBasis.Blocked, approvedBy: "admin");

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("basis");
    }

    [Fact]
    public void Revoke_ActiveTransfer_SetsIsRevoked()
    {
        // Arrange
        var transfer = CreateActiveTransfer();

        // Act
        transfer.Revoke("Data breach detected", "compliance-officer");

        // Assert
        transfer.IsRevoked.Should().BeTrue();
        transfer.RevokedAtUtc.Should().NotBeNull();
        transfer.RevokedAtUtc.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Revoke_AlreadyRevoked_ThrowsInvalidOperation()
    {
        // Arrange
        var transfer = CreateRevokedTransfer();

        // Act
        var act = () => transfer.Revoke("Second revocation", "admin");

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Expire_ActiveTransfer_SetsIsExpired()
    {
        // Arrange
        var transfer = CreateActiveTransfer();

        // Act
        transfer.Expire();

        // Assert
        transfer.IsExpired.Should().BeTrue();
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
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Expire_RevokedTransfer_ThrowsInvalidOperation()
    {
        // Arrange
        var transfer = CreateRevokedTransfer();

        // Act
        var act = () => transfer.Expire();

        // Assert
        act.Should().Throw<InvalidOperationException>();
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
        transfer.ExpiresAtUtc.Should().Be(newExpiration);
        transfer.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void Renew_RevokedTransfer_ThrowsInvalidOperation()
    {
        // Arrange
        var transfer = CreateRevokedTransfer();

        // Act
        var act = () => transfer.Renew(DateTimeOffset.UtcNow.AddYears(1), "admin");

        // Assert
        act.Should().Throw<InvalidOperationException>();
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
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValid_Revoked_ReturnsFalse()
    {
        // Arrange
        var transfer = CreateRevokedTransfer();

        // Act
        var result = transfer.IsValid(DateTimeOffset.UtcNow);

        // Assert
        result.Should().BeFalse();
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
        result.Should().BeFalse();
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
        result.Should().BeTrue();
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
