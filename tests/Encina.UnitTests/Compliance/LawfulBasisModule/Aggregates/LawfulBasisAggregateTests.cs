using Encina.Compliance.LawfulBasis.Aggregates;
using Encina.Compliance.LawfulBasis.Events;
using Shouldly;
using GDPR = global::Encina.Compliance.GDPR;

namespace Encina.UnitTests.Compliance.LawfulBasisModule.Aggregates;

/// <summary>
/// Unit tests for <see cref="LawfulBasisAggregate"/>.
/// </summary>
public class LawfulBasisAggregateTests
{
    private static readonly Guid DefaultId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);

    #region Register (Static Factory)

    [Fact]
    public void Register_WithValidParameters_SetsProperties()
    {
        // Act
        var aggregate = LawfulBasisAggregate.Register(
            DefaultId, "MyApp.Commands.CreateOrder", global::Encina.Compliance.GDPR.LawfulBasis.Contract,
            "Order processing", null, null, "contract-ref-001",
            Now);

        // Assert
        aggregate.Id.ShouldBe(DefaultId);
        aggregate.RequestTypeName.ShouldBe("MyApp.Commands.CreateOrder");
        aggregate.Basis.ShouldBe(global::Encina.Compliance.GDPR.LawfulBasis.Contract);
        aggregate.Purpose.ShouldBe("Order processing");
        aggregate.LIAReference.ShouldBeNull();
        aggregate.LegalReference.ShouldBeNull();
        aggregate.ContractReference.ShouldBe("contract-ref-001");
        aggregate.IsRevoked.ShouldBeFalse();
        aggregate.RevocationReason.ShouldBeNull();
        aggregate.TenantId.ShouldBeNull();
        aggregate.ModuleId.ShouldBeNull();
    }

    [Fact]
    public void Register_WithAllOptionalParameters_SetsAll()
    {
        // Act
        var aggregate = LawfulBasisAggregate.Register(
            DefaultId, "MyApp.Commands.ProcessFraudCheck", global::Encina.Compliance.GDPR.LawfulBasis.LegitimateInterests,
            "Fraud prevention", "LIA-2024-FRAUD-001", "GDPR Art. 6(1)(f)", "contract-ref",
            Now, "tenant-1", "module-1");

        // Assert
        aggregate.Purpose.ShouldBe("Fraud prevention");
        aggregate.LIAReference.ShouldBe("LIA-2024-FRAUD-001");
        aggregate.LegalReference.ShouldBe("GDPR Art. 6(1)(f)");
        aggregate.ContractReference.ShouldBe("contract-ref");
        aggregate.TenantId.ShouldBe("tenant-1");
        aggregate.ModuleId.ShouldBe("module-1");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_WithNullOrWhiteSpaceRequestTypeName_ThrowsArgumentException(string? requestTypeName)
    {
        // Act
        var act = () => LawfulBasisAggregate.Register(
            DefaultId, requestTypeName!, global::Encina.Compliance.GDPR.LawfulBasis.Consent,
            null, null, null, null, Now);

        // Assert
        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("requestTypeName");
    }

    [Fact]
    public void Register_RaisesLawfulBasisRegisteredEvent()
    {
        // Act
        var aggregate = LawfulBasisAggregate.Register(
            DefaultId, "MyApp.Commands.CreateOrder", global::Encina.Compliance.GDPR.LawfulBasis.Contract,
            "Order processing", null, null, "contract-ref-001",
            Now, "tenant-1", "module-1");

        // Assert
        aggregate.UncommittedEvents.ShouldHaveSingleItem().ShouldBeOfType<LawfulBasisRegistered>().ShouldSatisfyAllConditions(
            e => e.RegistrationId.ShouldBe(DefaultId),
            e => e.RequestTypeName.ShouldBe("MyApp.Commands.CreateOrder"),
            e => e.Basis.ShouldBe(global::Encina.Compliance.GDPR.LawfulBasis.Contract),
            e => e.Purpose.ShouldBe("Order processing"),
            e => e.ContractReference.ShouldBe("contract-ref-001"),
            e => e.TenantId.ShouldBe("tenant-1"),
            e => e.ModuleId.ShouldBe("module-1"));
        aggregate.Version.ShouldBe(1);
    }

    #endregion

    #region ChangeBasis

    [Fact]
    public void ChangeBasis_WhenActive_UpdatesBasis()
    {
        // Arrange
        var aggregate = CreateActiveRegistration();

        // Act
        aggregate.ChangeBasis(
            global::Encina.Compliance.GDPR.LawfulBasis.LegitimateInterests, "Updated purpose",
            "LIA-2024-001", null, null, Now.AddDays(30));

        // Assert
        aggregate.Basis.ShouldBe(global::Encina.Compliance.GDPR.LawfulBasis.LegitimateInterests);
        aggregate.Purpose.ShouldBe("Updated purpose");
        aggregate.LIAReference.ShouldBe("LIA-2024-001");
        aggregate.ContractReference.ShouldBeNull();
    }

    [Fact]
    public void ChangeBasis_WhenRevoked_ThrowsInvalidOperation()
    {
        // Arrange
        var aggregate = CreateRevokedRegistration();

        // Act
        var act = () => aggregate.ChangeBasis(
            global::Encina.Compliance.GDPR.LawfulBasis.LegitimateInterests, null,
            null, null, null, Now.AddDays(60));

        // Assert
        Should.Throw<InvalidOperationException>(act).Message.ShouldContain("revoked");
    }

    [Fact]
    public void ChangeBasis_ToSameBasis_ThrowsInvalidOperation()
    {
        // Arrange
        var aggregate = CreateActiveRegistration();

        // Act
        var act = () => aggregate.ChangeBasis(
            global::Encina.Compliance.GDPR.LawfulBasis.Contract, null,
            null, null, null, Now.AddDays(30));

        // Assert
        Should.Throw<InvalidOperationException>(act).Message.ShouldContain("same");
    }

    [Fact]
    public void ChangeBasis_RaisesLawfulBasisChangedEvent()
    {
        // Arrange
        var aggregate = CreateActiveRegistration();

        // Act
        aggregate.ChangeBasis(
            global::Encina.Compliance.GDPR.LawfulBasis.LegalObligation, "Legal requirement",
            null, "EU Directive 2024/XXX", null, Now.AddDays(30));

        // Assert
        aggregate.UncommittedEvents.Count.ShouldBe(2);
        var changedEvent = aggregate.UncommittedEvents[^1].ShouldBeOfType<LawfulBasisChanged>();
        changedEvent.OldBasis.ShouldBe(global::Encina.Compliance.GDPR.LawfulBasis.Contract);
        changedEvent.NewBasis.ShouldBe(global::Encina.Compliance.GDPR.LawfulBasis.LegalObligation);
        changedEvent.Purpose.ShouldBe("Legal requirement");
        changedEvent.LegalReference.ShouldBe("EU Directive 2024/XXX");
    }

    #endregion

    #region Revoke

    [Fact]
    public void Revoke_WhenActive_SetsIsRevoked()
    {
        // Arrange
        var aggregate = CreateActiveRegistration();

        // Act
        aggregate.Revoke("Processing no longer needed", Now.AddDays(90));

        // Assert
        aggregate.IsRevoked.ShouldBeTrue();
        aggregate.RevocationReason.ShouldBe("Processing no longer needed");
    }

    [Fact]
    public void Revoke_WhenAlreadyRevoked_ThrowsInvalidOperation()
    {
        // Arrange
        var aggregate = CreateRevokedRegistration();

        // Act
        var act = () => aggregate.Revoke("Second revocation", Now.AddDays(120));

        // Assert
        Should.Throw<InvalidOperationException>(act).Message.ShouldContain("already been revoked");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Revoke_WithNullOrWhiteSpaceReason_ThrowsArgumentException(string? reason)
    {
        // Arrange
        var aggregate = CreateActiveRegistration();

        // Act
        var act = () => aggregate.Revoke(reason!, Now.AddDays(90));

        // Assert
        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("reason");
    }

    [Fact]
    public void Revoke_RaisesLawfulBasisRevokedEvent()
    {
        // Arrange
        var aggregate = CreateActiveRegistration();

        // Act
        aggregate.Revoke("No longer required", Now.AddDays(90));

        // Assert
        aggregate.UncommittedEvents.Count.ShouldBe(2);
        var revokedEvent = aggregate.UncommittedEvents[^1].ShouldBeOfType<LawfulBasisRevoked>();
        revokedEvent.Reason.ShouldBe("No longer required");
        revokedEvent.RegistrationId.ShouldBe(DefaultId);
    }

    #endregion

    #region Version Tracking

    [Fact]
    public void Version_IncreasesWithEachEvent()
    {
        // Arrange & Act
        var aggregate = LawfulBasisAggregate.Register(
            DefaultId, "MyApp.Commands.CreateOrder", global::Encina.Compliance.GDPR.LawfulBasis.Contract,
            "Order processing", null, null, "contract-ref",
            Now);
        aggregate.Version.ShouldBe(1);

        aggregate.ChangeBasis(
            global::Encina.Compliance.GDPR.LawfulBasis.LegalObligation, null,
            null, "legal-ref", null, Now.AddDays(30));
        aggregate.Version.ShouldBe(2);

        aggregate.Revoke("Done", Now.AddDays(60));
        aggregate.Version.ShouldBe(3);
    }

    #endregion

    #region Helper Methods

    private static LawfulBasisAggregate CreateActiveRegistration()
    {
        return LawfulBasisAggregate.Register(
            DefaultId, "MyApp.Commands.CreateOrder", global::Encina.Compliance.GDPR.LawfulBasis.Contract,
            "Order processing", null, null, "contract-ref-001",
            Now, "tenant-1", "module-1");
    }

    private static LawfulBasisAggregate CreateRevokedRegistration()
    {
        var aggregate = CreateActiveRegistration();
        aggregate.Revoke("No longer needed", Now.AddDays(30));
        return aggregate;
    }

    #endregion
}
