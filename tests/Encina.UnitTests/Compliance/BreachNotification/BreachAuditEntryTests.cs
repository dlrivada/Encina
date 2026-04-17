#pragma warning disable CA2012

using Encina.Compliance.BreachNotification.Model;
using Shouldly;

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
        entry.BreachId.ShouldBe(breachId);
        entry.Action.ShouldBe(action);
        entry.Detail.ShouldBe(detail);
        entry.PerformedByUserId.ShouldBe(performedByUserId);
    }

    [Fact]
    public void Create_ShouldGenerateNonEmptyId()
    {
        // Act
        var entry = BreachAuditEntry.Create("breach-001", "BreachDetected");

        // Assert
        entry.Id.ShouldNotBeNullOrWhiteSpace();
        entry.Id.Length.ShouldBe(32);
        entry.Id.ShouldMatch("^[0-9a-f]{32}$");
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
        entry.OccurredAtUtc.ShouldBeGreaterThanOrEqualTo(before);
        entry.OccurredAtUtc.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public void Create_NullDetail_ShouldBeNull()
    {
        // Act
        var entry = BreachAuditEntry.Create("breach-001", "BreachDetected", detail: null);

        // Assert
        entry.Detail.ShouldBeNull();
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
        entry.PerformedByUserId.ShouldBeNull();
    }

    [Fact]
    public void Create_WithoutOptionalParameters_ShouldDefaultToNull()
    {
        // Act
        var entry = BreachAuditEntry.Create("breach-001", "StatusChanged");

        // Assert
        entry.Detail.ShouldBeNull();
        entry.PerformedByUserId.ShouldBeNull();
    }

    [Fact]
    public void Create_MultipleCalls_ShouldGenerateUniqueIds()
    {
        // Act
        var entry1 = BreachAuditEntry.Create("breach-001", "BreachDetected");
        var entry2 = BreachAuditEntry.Create("breach-001", "AuthorityNotified");

        // Assert
        entry1.Id.ShouldNotBe(entry2.Id);
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
        original.Action.ShouldBe("BreachDetected");
        modified.Action.ShouldBe("AuthorityNotified");
        modified.Id.ShouldBe(original.Id);
    }

    #endregion
}
