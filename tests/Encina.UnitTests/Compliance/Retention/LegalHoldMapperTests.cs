using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Model;

using FluentAssertions;

namespace Encina.UnitTests.Compliance.Retention;

/// <summary>
/// Unit tests for <see cref="LegalHoldMapper"/> static mapping methods.
/// </summary>
public class LegalHoldMapperTests
{
    #region ToEntity Tests

    [Fact]
    public void ToEntity_ValidHold_ShouldMapAllProperties()
    {
        // Arrange
        var hold = CreateHold();

        // Act
        var entity = LegalHoldMapper.ToEntity(hold);

        // Assert
        entity.Id.Should().Be(hold.Id);
        entity.EntityId.Should().Be(hold.EntityId);
        entity.Reason.Should().Be(hold.Reason);
        entity.AppliedByUserId.Should().Be(hold.AppliedByUserId);
        entity.AppliedAtUtc.Should().Be(hold.AppliedAtUtc);
        entity.ReleasedAtUtc.Should().Be(hold.ReleasedAtUtc);
        entity.ReleasedByUserId.Should().Be(hold.ReleasedByUserId);
    }

    [Fact]
    public void ToEntity_WithActiveHold_ShouldMapNullReleasedAtUtc()
    {
        // Arrange
        var hold = new LegalHold
        {
            Id = "hold-active",
            EntityId = "invoice-001",
            Reason = "Pending audit",
            AppliedAtUtc = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
            ReleasedAtUtc = null,
            ReleasedByUserId = null
        };

        // Act
        var entity = LegalHoldMapper.ToEntity(hold);

        // Assert
        entity.ReleasedAtUtc.Should().BeNull();
        entity.ReleasedByUserId.Should().BeNull();
    }

    [Fact]
    public void ToEntity_WithReleasedHold_ShouldMapReleasedAtUtc()
    {
        // Arrange
        var releasedAt = new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var hold = new LegalHold
        {
            Id = "hold-released",
            EntityId = "invoice-002",
            Reason = "Resolved litigation",
            AppliedAtUtc = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
            ReleasedAtUtc = releasedAt,
            ReleasedByUserId = "admin@company.com"
        };

        // Act
        var entity = LegalHoldMapper.ToEntity(hold);

        // Assert
        entity.ReleasedAtUtc.Should().Be(releasedAt);
        entity.ReleasedByUserId.Should().Be("admin@company.com");
    }

    [Fact]
    public void ToEntity_NullHold_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => LegalHoldMapper.ToEntity(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region ToDomain Tests

    [Fact]
    public void ToDomain_ValidEntity_ShouldMapAllProperties()
    {
        // Arrange
        var entity = CreateEntity();

        // Act
        var domain = LegalHoldMapper.ToDomain(entity);

        // Assert
        domain.Id.Should().Be(entity.Id);
        domain.EntityId.Should().Be(entity.EntityId);
        domain.Reason.Should().Be(entity.Reason);
        domain.AppliedByUserId.Should().Be(entity.AppliedByUserId);
        domain.AppliedAtUtc.Should().Be(entity.AppliedAtUtc);
        domain.ReleasedAtUtc.Should().Be(entity.ReleasedAtUtc);
        domain.ReleasedByUserId.Should().Be(entity.ReleasedByUserId);
    }

    [Fact]
    public void ToDomain_ActiveHoldEntity_ShouldHaveIsActiveTrue()
    {
        // Arrange
        var entity = new LegalHoldEntity
        {
            Id = "hold-active",
            EntityId = "order-001",
            Reason = "Tax audit",
            AppliedAtUtc = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
            ReleasedAtUtc = null
        };

        // Act
        var domain = LegalHoldMapper.ToDomain(entity);

        // Assert
        domain.IsActive.Should().BeTrue();
    }

    [Fact]
    public void ToDomain_ReleasedHoldEntity_ShouldHaveIsActiveFalse()
    {
        // Arrange
        var entity = new LegalHoldEntity
        {
            Id = "hold-released",
            EntityId = "order-002",
            Reason = "Resolved audit",
            AppliedAtUtc = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
            ReleasedAtUtc = new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero),
            ReleasedByUserId = "legal@company.com"
        };

        // Act
        var domain = LegalHoldMapper.ToDomain(entity);

        // Assert
        domain.IsActive.Should().BeFalse();
    }

    [Fact]
    public void ToDomain_NullEntity_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => LegalHoldMapper.ToDomain(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Roundtrip Tests

    [Fact]
    public void Roundtrip_ToEntityThenToDomain_ShouldPreserveAllFields()
    {
        // Arrange
        var original = CreateHold();

        // Act
        var entity = LegalHoldMapper.ToEntity(original);
        var roundtripped = LegalHoldMapper.ToDomain(entity);

        // Assert
        roundtripped.Id.Should().Be(original.Id);
        roundtripped.EntityId.Should().Be(original.EntityId);
        roundtripped.Reason.Should().Be(original.Reason);
        roundtripped.AppliedByUserId.Should().Be(original.AppliedByUserId);
        roundtripped.AppliedAtUtc.Should().Be(original.AppliedAtUtc);
        roundtripped.ReleasedAtUtc.Should().Be(original.ReleasedAtUtc);
        roundtripped.ReleasedByUserId.Should().Be(original.ReleasedByUserId);
        roundtripped.IsActive.Should().Be(original.IsActive);
    }

    #endregion

    private static LegalHold CreateHold() => new()
    {
        Id = "hold-001",
        EntityId = "invoice-12345",
        Reason = "Pending tax audit for fiscal year 2024",
        AppliedByUserId = "legal-counsel@company.com",
        AppliedAtUtc = new DateTimeOffset(2025, 1, 10, 9, 0, 0, TimeSpan.Zero),
        ReleasedAtUtc = new DateTimeOffset(2025, 8, 1, 0, 0, 0, TimeSpan.Zero),
        ReleasedByUserId = "admin@company.com"
    };

    private static LegalHoldEntity CreateEntity() => new()
    {
        Id = "entity-001",
        EntityId = "order-99",
        Reason = "Litigation: Smith v. Company (Case #2024-456)",
        AppliedByUserId = "legal@company.com",
        AppliedAtUtc = new DateTimeOffset(2025, 2, 1, 0, 0, 0, TimeSpan.Zero),
        ReleasedAtUtc = null,
        ReleasedByUserId = null
    };
}
