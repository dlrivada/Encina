using Encina.Compliance.Retention.Model;
using FluentAssertions;

namespace Encina.UnitTests.Compliance.Retention;

/// <summary>
/// Unit tests for <see cref="LegalHold"/> factory method and record behavior.
/// </summary>
public class LegalHoldTests
{
    #region Create Factory Method Tests

    [Fact]
    public void Create_ShouldSetEntityId()
    {
        // Act
        var hold = LegalHold.Create(
            entityId: "invoice-12345",
            reason: "Pending tax audit for fiscal year 2024");

        // Assert
        hold.EntityId.Should().Be("invoice-12345");
    }

    [Fact]
    public void Create_ShouldSetReason()
    {
        // Act
        var hold = LegalHold.Create(
            entityId: "invoice-12345",
            reason: "Pending tax audit for fiscal year 2024");

        // Assert
        hold.Reason.Should().Be("Pending tax audit for fiscal year 2024");
    }

    [Fact]
    public void Create_WithAppliedByUserId_ShouldSetAppliedByUserId()
    {
        // Act
        var hold = LegalHold.Create(
            entityId: "invoice-12345",
            reason: "Litigation: Smith v. Company (Case #2024-456)",
            appliedByUserId: "legal-counsel@company.com");

        // Assert
        hold.AppliedByUserId.Should().Be("legal-counsel@company.com");
    }

    [Fact]
    public void Create_WithoutAppliedByUserId_ShouldLeaveItNull()
    {
        // Act
        var hold = LegalHold.Create(
            entityId: "invoice-12345",
            reason: "Automated regulatory compliance hold");

        // Assert
        hold.AppliedByUserId.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldGenerateId_WithNoHyphens()
    {
        // Act
        var hold = LegalHold.Create(
            entityId: "invoice-12345",
            reason: "Pending tax audit for fiscal year 2024");

        // Assert
        hold.Id.Should().NotBeNullOrEmpty();
        hold.Id.Should().HaveLength(32);
        hold.Id.Should().NotContain("-");
    }

    [Fact]
    public void Create_TwoCalls_ShouldGenerateDifferentIds()
    {
        // Act
        var hold1 = LegalHold.Create("entity-1", "Reason A");
        var hold2 = LegalHold.Create("entity-2", "Reason B");

        // Assert
        hold1.Id.Should().NotBe(hold2.Id);
    }

    [Fact]
    public void Create_ShouldSetAppliedAtUtcToNow()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var hold = LegalHold.Create(
            entityId: "invoice-12345",
            reason: "Pending tax audit for fiscal year 2024");

        var after = DateTimeOffset.UtcNow;

        // Assert
        hold.AppliedAtUtc.Should().BeOnOrAfter(before);
        hold.AppliedAtUtc.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void Create_ShouldLeaveReleasedAtUtcNull()
    {
        // Act
        var hold = LegalHold.Create(
            entityId: "invoice-12345",
            reason: "Pending tax audit for fiscal year 2024");

        // Assert
        hold.ReleasedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldLeaveReleasedByUserIdNull()
    {
        // Act
        var hold = LegalHold.Create(
            entityId: "invoice-12345",
            reason: "Pending tax audit for fiscal year 2024");

        // Assert
        hold.ReleasedByUserId.Should().BeNull();
    }

    #endregion

    #region IsActive Tests

    [Fact]
    public void IsActive_WhenReleasedAtUtcIsNull_ShouldBeTrue()
    {
        // Arrange
        var hold = LegalHold.Create(
            entityId: "invoice-12345",
            reason: "Pending tax audit for fiscal year 2024");

        // Act & Assert
        hold.IsActive.Should().BeTrue();
    }

    [Fact]
    public void IsActive_WhenReleasedAtUtcHasValue_ShouldBeFalse()
    {
        // Arrange
        var hold = LegalHold.Create(
            entityId: "invoice-12345",
            reason: "Pending tax audit for fiscal year 2024") with
        {
            ReleasedAtUtc = DateTimeOffset.UtcNow
        };

        // Act & Assert
        hold.IsActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_WhenCreated_ShouldBeTrue()
    {
        // Act
        var hold = LegalHold.Create("entity-001", "Litigation hold");

        // Assert
        hold.IsActive.Should().BeTrue();
    }

    #endregion

    #region Init Property Tests

    [Fact]
    public void Properties_AreSettableViaInit()
    {
        // Arrange
        var appliedAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var releasedAt = new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero);

        // Act
        var hold = new LegalHold
        {
            Id = "holdabc123",
            EntityId = "order-99",
            Reason = "Tax audit",
            AppliedByUserId = "legal@company.com",
            AppliedAtUtc = appliedAt,
            ReleasedAtUtc = releasedAt,
            ReleasedByUserId = "admin@company.com"
        };

        // Assert
        hold.Id.Should().Be("holdabc123");
        hold.EntityId.Should().Be("order-99");
        hold.Reason.Should().Be("Tax audit");
        hold.AppliedByUserId.Should().Be("legal@company.com");
        hold.AppliedAtUtc.Should().Be(appliedAt);
        hold.ReleasedAtUtc.Should().Be(releasedAt);
        hold.ReleasedByUserId.Should().Be("admin@company.com");
    }

    #endregion
}
