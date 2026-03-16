using Encina.Compliance.LawfulBasis.Aggregates;
using Encina.Compliance.LawfulBasis.Events;
using FluentAssertions;
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
        aggregate.Id.Should().Be(DefaultId);
        aggregate.RequestTypeName.Should().Be("MyApp.Commands.CreateOrder");
        aggregate.Basis.Should().Be(global::Encina.Compliance.GDPR.LawfulBasis.Contract);
        aggregate.Purpose.Should().Be("Order processing");
        aggregate.LIAReference.Should().BeNull();
        aggregate.LegalReference.Should().BeNull();
        aggregate.ContractReference.Should().Be("contract-ref-001");
        aggregate.IsRevoked.Should().BeFalse();
        aggregate.RevocationReason.Should().BeNull();
        aggregate.TenantId.Should().BeNull();
        aggregate.ModuleId.Should().BeNull();
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
        aggregate.Purpose.Should().Be("Fraud prevention");
        aggregate.LIAReference.Should().Be("LIA-2024-FRAUD-001");
        aggregate.LegalReference.Should().Be("GDPR Art. 6(1)(f)");
        aggregate.ContractReference.Should().Be("contract-ref");
        aggregate.TenantId.Should().Be("tenant-1");
        aggregate.ModuleId.Should().Be("module-1");
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
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("requestTypeName");
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
        aggregate.UncommittedEvents.Should().ContainSingle()
            .Which.Should().BeOfType<LawfulBasisRegistered>()
            .Which.Should().BeEquivalentTo(new
            {
                RegistrationId = DefaultId,
                RequestTypeName = "MyApp.Commands.CreateOrder",
                Basis = global::Encina.Compliance.GDPR.LawfulBasis.Contract,
                Purpose = (string?)"Order processing",
                ContractReference = (string?)"contract-ref-001",
                TenantId = (string?)"tenant-1",
                ModuleId = (string?)"module-1",
            });
        aggregate.Version.Should().Be(1);
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
        aggregate.Basis.Should().Be(global::Encina.Compliance.GDPR.LawfulBasis.LegitimateInterests);
        aggregate.Purpose.Should().Be("Updated purpose");
        aggregate.LIAReference.Should().Be("LIA-2024-001");
        aggregate.ContractReference.Should().BeNull();
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
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*revoked*");
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
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*same*");
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
        aggregate.UncommittedEvents.Should().HaveCount(2);
        var changedEvent = aggregate.UncommittedEvents[^1].Should().BeOfType<LawfulBasisChanged>().Subject;
        changedEvent.OldBasis.Should().Be(global::Encina.Compliance.GDPR.LawfulBasis.Contract);
        changedEvent.NewBasis.Should().Be(global::Encina.Compliance.GDPR.LawfulBasis.LegalObligation);
        changedEvent.Purpose.Should().Be("Legal requirement");
        changedEvent.LegalReference.Should().Be("EU Directive 2024/XXX");
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
        aggregate.IsRevoked.Should().BeTrue();
        aggregate.RevocationReason.Should().Be("Processing no longer needed");
    }

    [Fact]
    public void Revoke_WhenAlreadyRevoked_ThrowsInvalidOperation()
    {
        // Arrange
        var aggregate = CreateRevokedRegistration();

        // Act
        var act = () => aggregate.Revoke("Second revocation", Now.AddDays(120));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already been revoked*");
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
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("reason");
    }

    [Fact]
    public void Revoke_RaisesLawfulBasisRevokedEvent()
    {
        // Arrange
        var aggregate = CreateActiveRegistration();

        // Act
        aggregate.Revoke("No longer required", Now.AddDays(90));

        // Assert
        aggregate.UncommittedEvents.Should().HaveCount(2);
        var revokedEvent = aggregate.UncommittedEvents[^1].Should().BeOfType<LawfulBasisRevoked>().Subject;
        revokedEvent.Reason.Should().Be("No longer required");
        revokedEvent.RegistrationId.Should().Be(DefaultId);
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
        aggregate.Version.Should().Be(1);

        aggregate.ChangeBasis(
            global::Encina.Compliance.GDPR.LawfulBasis.LegalObligation, null,
            null, "legal-ref", null, Now.AddDays(30));
        aggregate.Version.Should().Be(2);

        aggregate.Revoke("Done", Now.AddDays(60));
        aggregate.Version.Should().Be(3);
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
