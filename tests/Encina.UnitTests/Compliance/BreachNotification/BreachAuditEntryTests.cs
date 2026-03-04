#pragma warning disable CA2012

using Encina.Compliance.BreachNotification.Model;
using FluentAssertions;

namespace Encina.UnitTests.Compliance.BreachNotification;

/// <summary>
/// Unit tests for <see cref="BreachAuditEntry"/>.
/// </summary>
public class BreachAuditEntryTests
{
    #region Create Tests

    [Fact]
    public void Create_ValidParameters_ShouldSetAllProperties()
    {
        // Arrange
        var breachId = "breach-001";
        var action = "BreachDetected";
        var detail = "Unauthorized access detected via MassDataExfiltrationRule";
        var performedByUserId = "admin-42";

        // Act
        var entry = BreachAuditEntry.Create(breachId, action, detail, performedByUserId);

        // Assert
        entry.BreachId.Should().Be(breachId);
        entry.Action.Should().Be(action);
        entry.Detail.Should().Be(detail);
        entry.PerformedByUserId.Should().Be(performedByUserId);
    }

    [Fact]
    public void Create_ShouldGenerateNonEmptyId()
    {
        // Act
        var entry = BreachAuditEntry.Create("breach-001", "BreachDetected");

        // Assert
        entry.Id.Should().NotBeNullOrWhiteSpace();
        entry.Id.Should().HaveLength(32);
        entry.Id.Should().MatchRegex("^[0-9a-f]{32}$");
    }

    [Fact]
    public void Create_ShouldSetOccurredAtUtcToCurrentTime()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var entry = BreachAuditEntry.Create("breach-001", "AuthorityNotified");

        // Assert
        var after = DateTimeOffset.UtcNow;
        entry.OccurredAtUtc.Should().BeOnOrAfter(before);
        entry.OccurredAtUtc.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void Create_NullDetail_ShouldBeNull()
    {
        // Act
        var entry = BreachAuditEntry.Create("breach-001", "BreachDetected", detail: null);

        // Assert
        entry.Detail.Should().BeNull();
    }

    [Fact]
    public void Create_NullPerformedByUserId_ShouldBeNull()
    {
        // Act
        var entry = BreachAuditEntry.Create(
            "breach-001",
            "BreachDetected",
            detail: "Auto-detected",
            performedByUserId: null);

        // Assert
        entry.PerformedByUserId.Should().BeNull();
    }

    [Fact]
    public void Create_WithoutOptionalParameters_ShouldDefaultToNull()
    {
        // Act
        var entry = BreachAuditEntry.Create("breach-001", "StatusChanged");

        // Assert
        entry.Detail.Should().BeNull();
        entry.PerformedByUserId.Should().BeNull();
    }

    [Fact]
    public void Create_MultipleCalls_ShouldGenerateUniqueIds()
    {
        // Act
        var entry1 = BreachAuditEntry.Create("breach-001", "BreachDetected");
        var entry2 = BreachAuditEntry.Create("breach-001", "AuthorityNotified");

        // Assert
        entry1.Id.Should().NotBe(entry2.Id);
    }

    #endregion

    #region Record Immutability Tests

    [Fact]
    public void BreachAuditEntry_WithExpression_ShouldCreateNewInstance()
    {
        // Arrange
        var original = BreachAuditEntry.Create("breach-001", "BreachDetected");

        // Act
        var modified = original with { Action = "AuthorityNotified" };

        // Assert
        original.Action.Should().Be("BreachDetected");
        modified.Action.Should().Be("AuthorityNotified");
        modified.Id.Should().Be(original.Id);
    }

    #endregion
}
