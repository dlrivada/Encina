using Encina.Compliance.Consent;
using Encina.Compliance.Consent.Aggregates;
using Encina.Compliance.Consent.Events;
using Shouldly;

namespace Encina.UnitTests.Compliance.Consent;

/// <summary>
/// Unit tests for <see cref="ConsentAggregate"/>.
/// </summary>
public class ConsentAggregateTests
{
    private static readonly Guid DefaultId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ExpiresAt = Now.AddDays(365);
    private static readonly IReadOnlyDictionary<string, object?> DefaultMetadata =
        new Dictionary<string, object?> { ["campaign"] = "spring-2026" };

    #region Grant (Static Factory)

    [Fact]
    public void Grant_ValidParameters_ShouldCreateActiveConsent()
    {
        // Act
        var aggregate = ConsentAggregate.Grant(
            DefaultId, "user-1", "marketing", "v1", "web-form",
            "192.168.1.1", "proof-hash", DefaultMetadata, ExpiresAt,
            "admin", Now, "tenant-1", "module-1");

        // Assert
        aggregate.Id.ShouldBe(DefaultId);
        aggregate.DataSubjectId.ShouldBe("user-1");
        aggregate.Purpose.ShouldBe("marketing");
        aggregate.Status.ShouldBe(ConsentStatus.Active);
        aggregate.ConsentVersionId.ShouldBe("v1");
        aggregate.Source.ShouldBe("web-form");
        aggregate.IpAddress.ShouldBe("192.168.1.1");
        aggregate.ProofOfConsent.ShouldBe("proof-hash");
        aggregate.Metadata.ShouldBeSameAs(DefaultMetadata);
        aggregate.GivenAtUtc.ShouldBe(Now);
        aggregate.ExpiresAtUtc.ShouldBe(ExpiresAt);
        aggregate.TenantId.ShouldBe("tenant-1");
        aggregate.ModuleId.ShouldBe("module-1");
        aggregate.WithdrawnAtUtc.ShouldBeNull();
        aggregate.WithdrawalReason.ShouldBeNull();
    }

    [Fact]
    public void Grant_ValidParameters_ShouldRaiseConsentGrantedEvent()
    {
        // Act
        var aggregate = ConsentAggregate.Grant(
            DefaultId, "user-1", "marketing", "v1", "web-form",
            null, null, DefaultMetadata, null, "admin", Now);

        // Assert
        aggregate.UncommittedEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<ConsentGranted>();
        aggregate.Version.ShouldBe(1);
    }

    [Fact]
    public void Grant_NullableParametersAsNull_ShouldCreateConsent()
    {
        // Act
        var aggregate = ConsentAggregate.Grant(
            DefaultId, "user-1", "marketing", "v1", "web-form",
            null, null, DefaultMetadata, null, "admin", Now);

        // Assert
        aggregate.IpAddress.ShouldBeNull();
        aggregate.ProofOfConsent.ShouldBeNull();
        aggregate.ExpiresAtUtc.ShouldBeNull();
        aggregate.TenantId.ShouldBeNull();
        aggregate.ModuleId.ShouldBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Grant_NullOrWhiteSpaceDataSubjectId_ShouldThrow(string? dataSubjectId)
    {
        // Act
        var act = () => ConsentAggregate.Grant(
            DefaultId, dataSubjectId!, "marketing", "v1", "web-form",
            null, null, DefaultMetadata, null, "admin", Now);

        // Assert
        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("dataSubjectId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Grant_NullOrWhiteSpacePurpose_ShouldThrow(string? purpose)
    {
        // Act
        var act = () => ConsentAggregate.Grant(
            DefaultId, "user-1", purpose!, "v1", "web-form",
            null, null, DefaultMetadata, null, "admin", Now);

        // Assert
        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("purpose");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Grant_NullOrWhiteSpaceConsentVersionId_ShouldThrow(string? consentVersionId)
    {
        // Act
        var act = () => ConsentAggregate.Grant(
            DefaultId, "user-1", "marketing", consentVersionId!, "web-form",
            null, null, DefaultMetadata, null, "admin", Now);

        // Assert
        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("consentVersionId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Grant_NullOrWhiteSpaceSource_ShouldThrow(string? source)
    {
        // Act
        var act = () => ConsentAggregate.Grant(
            DefaultId, "user-1", "marketing", "v1", source!,
            null, null, DefaultMetadata, null, "admin", Now);

        // Assert
        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("source");
    }

    [Fact]
    public void Grant_NullMetadata_ShouldThrow()
    {
        // Act
        var act = () => ConsentAggregate.Grant(
            DefaultId, "user-1", "marketing", "v1", "web-form",
            null, null, null!, null, "admin", Now);

        // Assert
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("metadata");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Grant_NullOrWhiteSpaceGrantedBy_ShouldThrow(string? grantedBy)
    {
        // Act
        var act = () => ConsentAggregate.Grant(
            DefaultId, "user-1", "marketing", "v1", "web-form",
            null, null, DefaultMetadata, null, grantedBy!, Now);

        // Assert
        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("grantedBy");
    }

    #endregion

    #region Withdraw

    [Fact]
    public void Withdraw_FromActive_ShouldSetStatusToWithdrawn()
    {
        // Arrange
        var aggregate = CreateActiveConsent();

        // Act
        aggregate.Withdraw("admin", "no longer needed", Now.AddDays(30));

        // Assert
        aggregate.Status.ShouldBe(ConsentStatus.Withdrawn);
        aggregate.WithdrawnAtUtc.ShouldBe(Now.AddDays(30));
        aggregate.WithdrawalReason.ShouldBe("no longer needed");
    }

    [Fact]
    public void Withdraw_FromActive_ShouldRaiseConsentWithdrawnEvent()
    {
        // Arrange
        var aggregate = CreateActiveConsent();

        // Act
        aggregate.Withdraw("admin", "reason", Now.AddDays(1));

        // Assert
        aggregate.UncommittedEvents.Count.ShouldBe(2);
        aggregate.UncommittedEvents[^1].ShouldBeOfType<ConsentWithdrawn>();
    }

    [Fact]
    public void Withdraw_FromRequiresReconsent_ShouldSucceed()
    {
        // Arrange
        var aggregate = CreateRequiresReconsentConsent();

        // Act
        aggregate.Withdraw("admin", null, Now.AddDays(30));

        // Assert
        aggregate.Status.ShouldBe(ConsentStatus.Withdrawn);
    }

    [Fact]
    public void Withdraw_WithNullReason_ShouldAllowNullReason()
    {
        // Arrange
        var aggregate = CreateActiveConsent();

        // Act
        aggregate.Withdraw("admin", null, Now.AddDays(1));

        // Assert
        aggregate.WithdrawalReason.ShouldBeNull();
    }

    [Fact]
    public void Withdraw_FromWithdrawn_ShouldThrow()
    {
        // Arrange
        var aggregate = CreateWithdrawnConsent();

        // Act
        var act = () => aggregate.Withdraw("admin", null, Now.AddDays(60));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Withdrawn");
    }

    [Fact]
    public void Withdraw_FromExpired_ShouldThrow()
    {
        // Arrange
        var aggregate = CreateActiveConsent(withExpiry: true);
        aggregate.Expire(Now.AddDays(366));

        // Act
        var act = () => aggregate.Withdraw("admin", null, Now.AddDays(367));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Expired");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Withdraw_NullOrWhiteSpaceWithdrawnBy_ShouldThrow(string? withdrawnBy)
    {
        // Arrange
        var aggregate = CreateActiveConsent();

        // Act
        var act = () => aggregate.Withdraw(withdrawnBy!, null, Now.AddDays(1));

        // Assert
        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("withdrawnBy");
    }

    #endregion

    #region Expire

    [Fact]
    public void Expire_FromActiveWithExpiry_ShouldSetStatusToExpired()
    {
        // Arrange
        var aggregate = CreateActiveConsent(withExpiry: true);

        // Act
        aggregate.Expire(Now.AddDays(366));

        // Assert
        aggregate.Status.ShouldBe(ConsentStatus.Expired);
    }

    [Fact]
    public void Expire_FromActiveWithExpiry_ShouldRaiseConsentExpiredEvent()
    {
        // Arrange
        var aggregate = CreateActiveConsent(withExpiry: true);

        // Act
        aggregate.Expire(Now.AddDays(366));

        // Assert
        aggregate.UncommittedEvents.Count.ShouldBe(2);
        aggregate.UncommittedEvents[^1].ShouldBeOfType<ConsentExpired>();
    }

    [Fact]
    public void Expire_FromActiveWithoutExpiry_ShouldThrow()
    {
        // Arrange
        var aggregate = CreateActiveConsent(withExpiry: false);

        // Act
        var act = () => aggregate.Expire(Now.AddDays(1));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("no expiration date");
    }

    [Fact]
    public void Expire_FromWithdrawn_ShouldThrow()
    {
        // Arrange
        var aggregate = CreateWithdrawnConsent();

        // Act
        var act = () => aggregate.Expire(Now.AddDays(60));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Withdrawn");
    }

    [Fact]
    public void Expire_FromRequiresReconsent_ShouldThrow()
    {
        // Arrange
        var aggregate = CreateRequiresReconsentConsent();

        // Act
        var act = () => aggregate.Expire(Now.AddDays(60));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("RequiresReconsent");
    }

    #endregion

    #region Renew

    [Fact]
    public void Renew_FromActive_ShouldUpdateVersionAndExpiry()
    {
        // Arrange
        var aggregate = CreateActiveConsent(withExpiry: true);
        var newExpiry = Now.AddDays(730);

        // Act
        aggregate.Renew("v2", newExpiry, "admin", "mobile-app", Now.AddDays(100));

        // Assert
        aggregate.Status.ShouldBe(ConsentStatus.Active);
        aggregate.ConsentVersionId.ShouldBe("v2");
        aggregate.ExpiresAtUtc.ShouldBe(newExpiry);
        aggregate.Source.ShouldBe("mobile-app");
    }

    [Fact]
    public void Renew_WithNullSource_ShouldKeepOriginalSource()
    {
        // Arrange
        var aggregate = CreateActiveConsent();

        // Act
        aggregate.Renew("v2", null, "admin", null, Now.AddDays(100));

        // Assert
        aggregate.Source.ShouldBe("web-form");
    }

    [Fact]
    public void Renew_FromActive_ShouldRaiseConsentRenewedEvent()
    {
        // Arrange
        var aggregate = CreateActiveConsent();

        // Act
        aggregate.Renew("v2", null, "admin", null, Now.AddDays(100));

        // Assert
        aggregate.UncommittedEvents.Count.ShouldBe(2);
        aggregate.UncommittedEvents[^1].ShouldBeOfType<ConsentRenewed>();
    }

    [Fact]
    public void Renew_FromWithdrawn_ShouldThrow()
    {
        // Arrange
        var aggregate = CreateWithdrawnConsent();

        // Act
        var act = () => aggregate.Renew("v2", null, "admin", null, Now.AddDays(60));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Withdrawn");
    }

    [Fact]
    public void Renew_FromExpired_ShouldThrow()
    {
        // Arrange
        var aggregate = CreateActiveConsent(withExpiry: true);
        aggregate.Expire(Now.AddDays(366));

        // Act
        var act = () => aggregate.Renew("v2", null, "admin", null, Now.AddDays(400));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Expired");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Renew_NullOrWhiteSpaceConsentVersionId_ShouldThrow(string? consentVersionId)
    {
        // Arrange
        var aggregate = CreateActiveConsent();

        // Act
        var act = () => aggregate.Renew(consentVersionId!, null, "admin", null, Now.AddDays(1));

        // Assert
        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("consentVersionId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Renew_NullOrWhiteSpaceRenewedBy_ShouldThrow(string? renewedBy)
    {
        // Arrange
        var aggregate = CreateActiveConsent();

        // Act
        var act = () => aggregate.Renew("v2", null, renewedBy!, null, Now.AddDays(1));

        // Assert
        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("renewedBy");
    }

    #endregion

    #region ChangeVersion

    [Fact]
    public void ChangeVersion_WithReconsentRequired_ShouldTransitionToRequiresReconsent()
    {
        // Arrange
        var aggregate = CreateActiveConsent();

        // Act
        aggregate.ChangeVersion("v2", "Updated privacy terms", true, "legal-team", Now.AddDays(50));

        // Assert
        aggregate.Status.ShouldBe(ConsentStatus.RequiresReconsent);
        aggregate.ConsentVersionId.ShouldBe("v2");
    }

    [Fact]
    public void ChangeVersion_WithoutReconsentRequired_ShouldRemainActive()
    {
        // Arrange
        var aggregate = CreateActiveConsent();

        // Act
        aggregate.ChangeVersion("v2", "Minor clarification", false, "legal-team", Now.AddDays(50));

        // Assert
        aggregate.Status.ShouldBe(ConsentStatus.Active);
        aggregate.ConsentVersionId.ShouldBe("v2");
    }

    [Fact]
    public void ChangeVersion_FromActive_ShouldRaiseConsentVersionChangedEvent()
    {
        // Arrange
        var aggregate = CreateActiveConsent();

        // Act
        aggregate.ChangeVersion("v2", "Updated terms", true, "legal-team", Now.AddDays(50));

        // Assert
        aggregate.UncommittedEvents.Count.ShouldBe(2);
        aggregate.UncommittedEvents[^1].ShouldBeOfType<ConsentVersionChanged>();
    }

    [Fact]
    public void ChangeVersion_FromWithdrawn_ShouldThrow()
    {
        // Arrange
        var aggregate = CreateWithdrawnConsent();

        // Act
        var act = () => aggregate.ChangeVersion("v2", "desc", true, "admin", Now.AddDays(60));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Withdrawn");
    }

    [Fact]
    public void ChangeVersion_FromRequiresReconsent_ShouldThrow()
    {
        // Arrange
        var aggregate = CreateRequiresReconsentConsent();

        // Act
        var act = () => aggregate.ChangeVersion("v3", "desc", true, "admin", Now.AddDays(60));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("RequiresReconsent");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ChangeVersion_NullOrWhiteSpaceNewVersionId_ShouldThrow(string? newVersionId)
    {
        // Arrange
        var aggregate = CreateActiveConsent();

        // Act
        var act = () => aggregate.ChangeVersion(newVersionId!, "desc", true, "admin", Now.AddDays(1));

        // Assert
        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("newVersionId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ChangeVersion_NullOrWhiteSpaceDescription_ShouldThrow(string? description)
    {
        // Arrange
        var aggregate = CreateActiveConsent();

        // Act
        var act = () => aggregate.ChangeVersion("v2", description!, true, "admin", Now.AddDays(1));

        // Assert
        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("description");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ChangeVersion_NullOrWhiteSpaceChangedBy_ShouldThrow(string? changedBy)
    {
        // Arrange
        var aggregate = CreateActiveConsent();

        // Act
        var act = () => aggregate.ChangeVersion("v2", "desc", true, changedBy!, Now.AddDays(1));

        // Assert
        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("changedBy");
    }

    #endregion

    #region ProvideReconsent

    [Fact]
    public void ProvideReconsent_FromRequiresReconsent_ShouldReactivateConsent()
    {
        // Arrange
        var aggregate = CreateRequiresReconsentConsent();
        var newMetadata = new Dictionary<string, object?> { ["source"] = "reconsent-form" };
        var newExpiry = Now.AddDays(730);

        // Act
        aggregate.ProvideReconsent("v3", "mobile-app", "10.0.0.1", "new-proof", newMetadata, newExpiry, "user-1", Now.AddDays(60));

        // Assert
        aggregate.Status.ShouldBe(ConsentStatus.Active);
        aggregate.ConsentVersionId.ShouldBe("v3");
        aggregate.Source.ShouldBe("mobile-app");
        aggregate.IpAddress.ShouldBe("10.0.0.1");
        aggregate.ProofOfConsent.ShouldBe("new-proof");
        aggregate.Metadata.ShouldBeSameAs(newMetadata);
        aggregate.ExpiresAtUtc.ShouldBe(newExpiry);
        aggregate.WithdrawnAtUtc.ShouldBeNull();
        aggregate.WithdrawalReason.ShouldBeNull();
    }

    [Fact]
    public void ProvideReconsent_FromRequiresReconsent_ShouldRaiseReconsentProvidedEvent()
    {
        // Arrange
        var aggregate = CreateRequiresReconsentConsent();

        // Act
        aggregate.ProvideReconsent("v3", "web", null, null, DefaultMetadata, null, "admin", Now.AddDays(60));

        // Assert
        aggregate.UncommittedEvents[^1].ShouldBeOfType<ConsentReconsentProvided>();
    }

    [Fact]
    public void ProvideReconsent_FromActive_ShouldThrow()
    {
        // Arrange
        var aggregate = CreateActiveConsent();

        // Act
        var act = () => aggregate.ProvideReconsent("v2", "web", null, null, DefaultMetadata, null, "admin", Now.AddDays(1));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Active");
    }

    [Fact]
    public void ProvideReconsent_FromWithdrawn_ShouldThrow()
    {
        // Arrange
        var aggregate = CreateWithdrawnConsent();

        // Act
        var act = () => aggregate.ProvideReconsent("v2", "web", null, null, DefaultMetadata, null, "admin", Now.AddDays(60));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Withdrawn");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ProvideReconsent_NullOrWhiteSpaceConsentVersionId_ShouldThrow(string? versionId)
    {
        // Arrange
        var aggregate = CreateRequiresReconsentConsent();

        // Act
        var act = () => aggregate.ProvideReconsent(versionId!, "web", null, null, DefaultMetadata, null, "admin", Now.AddDays(1));

        // Assert
        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("newConsentVersionId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ProvideReconsent_NullOrWhiteSpaceSource_ShouldThrow(string? source)
    {
        // Arrange
        var aggregate = CreateRequiresReconsentConsent();

        // Act
        var act = () => aggregate.ProvideReconsent("v3", source!, null, null, DefaultMetadata, null, "admin", Now.AddDays(1));

        // Assert
        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("source");
    }

    [Fact]
    public void ProvideReconsent_NullMetadata_ShouldThrow()
    {
        // Arrange
        var aggregate = CreateRequiresReconsentConsent();

        // Act
        var act = () => aggregate.ProvideReconsent("v3", "web", null, null, null!, null, "admin", Now.AddDays(1));

        // Assert
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("metadata");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ProvideReconsent_NullOrWhiteSpaceGrantedBy_ShouldThrow(string? grantedBy)
    {
        // Arrange
        var aggregate = CreateRequiresReconsentConsent();

        // Act
        var act = () => aggregate.ProvideReconsent("v3", "web", null, null, DefaultMetadata, null, grantedBy!, Now.AddDays(1));

        // Assert
        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("grantedBy");
    }

    #endregion

    #region Full Lifecycle

    [Fact]
    public void FullLifecycle_Grant_ChangeVersion_ProvideReconsent_Withdraw()
    {
        // Grant
        var aggregate = ConsentAggregate.Grant(
            DefaultId, "user-1", "marketing", "v1", "web-form",
            "192.168.1.1", "proof-v1", DefaultMetadata, ExpiresAt,
            "admin", Now);
        aggregate.Status.ShouldBe(ConsentStatus.Active);

        // Change version requiring reconsent
        aggregate.ChangeVersion("v2", "Major policy update", true, "legal-team", Now.AddDays(30));
        aggregate.Status.ShouldBe(ConsentStatus.RequiresReconsent);

        // Provide reconsent
        var reconsentMetadata = new Dictionary<string, object?> { ["accepted"] = true };
        aggregate.ProvideReconsent("v2", "mobile-app", "10.0.0.1", "proof-v2", reconsentMetadata, ExpiresAt.AddDays(365), "user-1", Now.AddDays(35));
        aggregate.Status.ShouldBe(ConsentStatus.Active);
        aggregate.ConsentVersionId.ShouldBe("v2");

        // Withdraw
        aggregate.Withdraw("user-1", "GDPR request", Now.AddDays(60));
        aggregate.Status.ShouldBe(ConsentStatus.Withdrawn);

        // All events should be recorded
        aggregate.UncommittedEvents.Count.ShouldBe(4);
        aggregate.Version.ShouldBe(4);
    }

    [Fact]
    public void FullLifecycle_Grant_Renew_Expire()
    {
        // Grant with expiry
        var aggregate = ConsentAggregate.Grant(
            DefaultId, "user-1", "analytics", "v1", "web-form",
            null, null, DefaultMetadata, ExpiresAt,
            "admin", Now);

        // Renew with new expiry
        var newExpiry = ExpiresAt.AddDays(365);
        aggregate.Renew("v1", newExpiry, "admin", null, Now.AddDays(300));
        aggregate.ExpiresAtUtc.ShouldBe(newExpiry);

        // Expire
        aggregate.Expire(newExpiry.AddDays(1));
        aggregate.Status.ShouldBe(ConsentStatus.Expired);

        aggregate.UncommittedEvents.Count.ShouldBe(3);
        aggregate.Version.ShouldBe(3);
    }

    #endregion

    #region Helper Methods

    private static ConsentAggregate CreateActiveConsent(bool withExpiry = false)
    {
        return ConsentAggregate.Grant(
            DefaultId, "user-1", "marketing", "v1", "web-form",
            "192.168.1.1", "proof-hash", DefaultMetadata,
            withExpiry ? ExpiresAt : null,
            "admin", Now);
    }

    private static ConsentAggregate CreateWithdrawnConsent()
    {
        var aggregate = CreateActiveConsent();
        aggregate.Withdraw("admin", "user requested", Now.AddDays(30));
        return aggregate;
    }

    private static ConsentAggregate CreateRequiresReconsentConsent()
    {
        var aggregate = CreateActiveConsent();
        aggregate.ChangeVersion("v2", "Updated privacy policy", true, "legal-team", Now.AddDays(30));
        return aggregate;
    }

    #endregion
}
