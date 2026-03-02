using Encina.Compliance.DataResidency.Model;

using FluentAssertions;

namespace Encina.UnitTests.Compliance.DataResidency;

public class ResidencyAuditEntryTests
{
    [Fact]
    public void Create_WithRequiredParameters_ShouldSetAllProperties()
    {
        // Act
        var entry = ResidencyAuditEntry.Create(
            dataCategory: "personal-data",
            sourceRegion: "DE",
            action: ResidencyAction.PolicyCheck,
            outcome: ResidencyOutcome.Allowed);

        // Assert
        entry.Id.Should().NotBeNullOrEmpty();
        entry.Id.Should().HaveLength(32);
        entry.DataCategory.Should().Be("personal-data");
        entry.SourceRegion.Should().Be("DE");
        entry.Action.Should().Be(ResidencyAction.PolicyCheck);
        entry.Outcome.Should().Be(ResidencyOutcome.Allowed);
        entry.TimestampUtc.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        entry.EntityId.Should().BeNull();
        entry.TargetRegion.Should().BeNull();
        entry.LegalBasis.Should().BeNull();
        entry.RequestType.Should().BeNull();
        entry.UserId.Should().BeNull();
        entry.Details.Should().BeNull();
    }

    [Fact]
    public void Create_WithAllOptionalParameters_ShouldPreserveValues()
    {
        // Act
        var entry = ResidencyAuditEntry.Create(
            dataCategory: "financial-records",
            sourceRegion: "DE",
            action: ResidencyAction.CrossBorderTransfer,
            outcome: ResidencyOutcome.Blocked,
            entityId: "customer-42",
            targetRegion: "US",
            legalBasis: "AdequacyDecision",
            requestType: "GetCustomerQuery",
            userId: "admin-1",
            details: "Transfer denied - no adequacy");

        // Assert
        entry.EntityId.Should().Be("customer-42");
        entry.TargetRegion.Should().Be("US");
        entry.LegalBasis.Should().Be("AdequacyDecision");
        entry.RequestType.Should().Be("GetCustomerQuery");
        entry.UserId.Should().Be("admin-1");
        entry.Details.Should().Be("Transfer denied - no adequacy");
    }

    [Fact]
    public void Create_ShouldGenerateUniqueIds()
    {
        // Act
        var entry1 = ResidencyAuditEntry.Create("cat1", "DE", ResidencyAction.PolicyCheck, ResidencyOutcome.Allowed);
        var entry2 = ResidencyAuditEntry.Create("cat2", "FR", ResidencyAction.PolicyCheck, ResidencyOutcome.Allowed);

        // Assert
        entry1.Id.Should().NotBe(entry2.Id);
    }

    [Theory]
    [InlineData(ResidencyAction.PolicyCheck)]
    [InlineData(ResidencyAction.CrossBorderTransfer)]
    [InlineData(ResidencyAction.LocationRecord)]
    [InlineData(ResidencyAction.Violation)]
    [InlineData(ResidencyAction.RegionRouting)]
    public void Create_WithAnyAction_ShouldPreserveValue(ResidencyAction action)
    {
        // Act
        var entry = ResidencyAuditEntry.Create("data", "DE", action, ResidencyOutcome.Allowed);

        // Assert
        entry.Action.Should().Be(action);
    }

    [Theory]
    [InlineData(ResidencyOutcome.Allowed)]
    [InlineData(ResidencyOutcome.Blocked)]
    [InlineData(ResidencyOutcome.Warning)]
    [InlineData(ResidencyOutcome.Skipped)]
    public void Create_WithAnyOutcome_ShouldPreserveValue(ResidencyOutcome outcome)
    {
        // Act
        var entry = ResidencyAuditEntry.Create("data", "DE", ResidencyAction.PolicyCheck, outcome);

        // Assert
        entry.Outcome.Should().Be(outcome);
    }
}
