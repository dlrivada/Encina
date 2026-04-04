using Encina.Compliance.DataResidency.Events;
using Encina.Compliance.DataResidency.Model;
using Encina.Compliance.DataResidency.ReadModels;
using Encina.Marten.Projections;

using Shouldly;

namespace Encina.UnitTests.Compliance.DataResidency;

/// <summary>
/// Unit tests for <see cref="ResidencyPolicyProjection"/> covering all event handlers.
/// </summary>
public class ResidencyPolicyProjectionTests
{
    private readonly ResidencyPolicyProjection _sut = new();

    private static ProjectionContext CreateContext(DateTime? timestamp = null) =>
        new() { Timestamp = timestamp ?? new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc) };

    [Fact]
    public void ProjectionName_ShouldBeResidencyPolicyProjection()
    {
        _sut.ProjectionName.ShouldBe("ResidencyPolicyProjection");
    }

    [Fact]
    public void Create_ResidencyPolicyCreated_ShouldCreateActiveReadModel()
    {
        // Arrange
        var policyId = Guid.NewGuid();
        var @event = new ResidencyPolicyCreated(
            policyId, "healthcare-data", ["DE", "FR"], true,
            [TransferLegalBasis.AdequacyDecision], "tenant-1", "mod-1");

        // Act
        var result = _sut.Create(@event, CreateContext());

        // Assert
        result.Id.ShouldBe(policyId);
        result.DataCategory.ShouldBe("healthcare-data");
        result.AllowedRegionCodes.ShouldBe(["DE", "FR"]);
        result.RequireAdequacyDecision.ShouldBeTrue();
        result.AllowedTransferBases.ShouldContain(TransferLegalBasis.AdequacyDecision);
        result.IsActive.ShouldBeTrue();
        result.TenantId.ShouldBe("tenant-1");
        result.ModuleId.ShouldBe("mod-1");
        result.Version.ShouldBe(1);
    }

    [Fact]
    public void Apply_ResidencyPolicyUpdated_ShouldUpdateFields()
    {
        // Arrange
        var current = CreateDefaultReadModel();
        var @event = new ResidencyPolicyUpdated(
            current.Id, ["DE", "FR", "NL"], false,
            [TransferLegalBasis.StandardContractualClauses]);

        // Act
        var result = _sut.Apply(@event, current, CreateContext());

        // Assert
        result.AllowedRegionCodes.ShouldBe(["DE", "FR", "NL"]);
        result.RequireAdequacyDecision.ShouldBeFalse();
        result.AllowedTransferBases.ShouldContain(TransferLegalBasis.StandardContractualClauses);
        result.Version.ShouldBe(2);
    }

    [Fact]
    public void Apply_ResidencyPolicyDeleted_ShouldMarkInactive()
    {
        // Arrange
        var current = CreateDefaultReadModel();
        var @event = new ResidencyPolicyDeleted(current.Id, "No longer needed");

        // Act
        var result = _sut.Apply(@event, current, CreateContext());

        // Assert
        result.IsActive.ShouldBeFalse();
        result.DeletionReason.ShouldBe("No longer needed");
        result.Version.ShouldBe(2);
    }

    private static ResidencyPolicyReadModel CreateDefaultReadModel() => new()
    {
        Id = Guid.NewGuid(),
        DataCategory = "personal-data",
        AllowedRegionCodes = ["DE"],
        RequireAdequacyDecision = true,
        AllowedTransferBases = [TransferLegalBasis.AdequacyDecision],
        IsActive = true,
        Version = 1
    };
}
