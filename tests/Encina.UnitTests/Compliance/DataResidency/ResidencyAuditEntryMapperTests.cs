using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;

using FluentAssertions;

namespace Encina.UnitTests.Compliance.DataResidency;

public class ResidencyAuditEntryMapperTests
{
    [Fact]
    public void ToEntity_ShouldMapAllProperties()
    {
        // Arrange
        var entry = ResidencyAuditEntry.Create(
            dataCategory: "personal-data",
            sourceRegion: "DE",
            action: ResidencyAction.CrossBorderTransfer,
            outcome: ResidencyOutcome.Blocked,
            entityId: "customer-42",
            targetRegion: "US",
            legalBasis: "AdequacyDecision",
            requestType: "GetCustomerQuery",
            userId: "admin-1",
            details: "Transfer denied");

        // Act
        var entity = ResidencyAuditEntryMapper.ToEntity(entry);

        // Assert
        entity.Id.Should().Be(entry.Id);
        entity.EntityId.Should().Be("customer-42");
        entity.DataCategory.Should().Be("personal-data");
        entity.SourceRegion.Should().Be("DE");
        entity.TargetRegion.Should().Be("US");
        entity.ActionValue.Should().Be((int)ResidencyAction.CrossBorderTransfer);
        entity.OutcomeValue.Should().Be((int)ResidencyOutcome.Blocked);
        entity.LegalBasis.Should().Be("AdequacyDecision");
        entity.RequestType.Should().Be("GetCustomerQuery");
        entity.UserId.Should().Be("admin-1");
        entity.Details.Should().Be("Transfer denied");
        entity.TimestampUtc.Should().Be(entry.TimestampUtc);
    }

    [Fact]
    public void ToEntity_NullEntry_ShouldThrow()
    {
        var act = () => ResidencyAuditEntryMapper.ToEntity(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToDomain_ValidEntity_ShouldMapAllProperties()
    {
        // Arrange
        var entity = new ResidencyAuditEntryEntity
        {
            Id = "abc123",
            EntityId = "entity-1",
            DataCategory = "personal-data",
            SourceRegion = "DE",
            TargetRegion = "FR",
            ActionValue = (int)ResidencyAction.PolicyCheck,
            OutcomeValue = (int)ResidencyOutcome.Allowed,
            LegalBasis = "AdequacyDecision",
            RequestType = "MyQuery",
            UserId = "user-1",
            TimestampUtc = DateTimeOffset.UtcNow,
            Details = "All good"
        };

        // Act
        var result = ResidencyAuditEntryMapper.ToDomain(entity);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("abc123");
        result.Action.Should().Be(ResidencyAction.PolicyCheck);
        result.Outcome.Should().Be(ResidencyOutcome.Allowed);
        result.EntityId.Should().Be("entity-1");
        result.LegalBasis.Should().Be("AdequacyDecision");
    }

    [Fact]
    public void ToDomain_InvalidActionValue_ShouldReturnNull()
    {
        // Arrange
        var entity = new ResidencyAuditEntryEntity
        {
            Id = "id1",
            DataCategory = "data",
            SourceRegion = "DE",
            ActionValue = 999,
            OutcomeValue = 0,
            TimestampUtc = DateTimeOffset.UtcNow
        };

        // Act
        var result = ResidencyAuditEntryMapper.ToDomain(entity);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ToDomain_InvalidOutcomeValue_ShouldReturnNull()
    {
        // Arrange
        var entity = new ResidencyAuditEntryEntity
        {
            Id = "id1",
            DataCategory = "data",
            SourceRegion = "DE",
            ActionValue = 0,
            OutcomeValue = 999,
            TimestampUtc = DateTimeOffset.UtcNow
        };

        // Act
        var result = ResidencyAuditEntryMapper.ToDomain(entity);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ToDomain_NullEntity_ShouldThrow()
    {
        var act = () => ResidencyAuditEntryMapper.ToDomain(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(ResidencyAction.PolicyCheck, ResidencyOutcome.Allowed)]
    [InlineData(ResidencyAction.CrossBorderTransfer, ResidencyOutcome.Blocked)]
    [InlineData(ResidencyAction.LocationRecord, ResidencyOutcome.Warning)]
    [InlineData(ResidencyAction.Violation, ResidencyOutcome.Skipped)]
    [InlineData(ResidencyAction.RegionRouting, ResidencyOutcome.Allowed)]
    public void RoundTrip_AllEnumCombinations_ShouldPreserve(ResidencyAction action, ResidencyOutcome outcome)
    {
        // Arrange
        var entry = ResidencyAuditEntry.Create("data", "DE", action, outcome);

        // Act
        var entity = ResidencyAuditEntryMapper.ToEntity(entry);
        var roundTripped = ResidencyAuditEntryMapper.ToDomain(entity);

        // Assert
        roundTripped.Should().NotBeNull();
        roundTripped!.Action.Should().Be(action);
        roundTripped.Outcome.Should().Be(outcome);
    }
}
