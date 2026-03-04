using Encina.Security.Audit;
using FluentAssertions;

namespace Encina.UnitTests.Security.Audit.ReadAudit;

/// <summary>
/// Unit tests for <see cref="ReadAuditEntry"/> record.
/// </summary>
public class ReadAuditEntryTests
{
    #region Construction Tests

    [Fact]
    public void Constructor_WithRequiredProperties_ShouldCreateEntry()
    {
        // Arrange & Act
        var entry = new ReadAuditEntry
        {
            Id = Guid.NewGuid(),
            EntityType = "Patient",
            EntityId = "P-123",
            AccessedAtUtc = DateTimeOffset.UtcNow,
            AccessMethod = ReadAccessMethod.Repository,
            EntityCount = 1
        };

        // Assert
        entry.Should().NotBeNull();
        entry.EntityType.Should().Be("Patient");
        entry.EntityId.Should().Be("P-123");
    }

    [Fact]
    public void Constructor_OptionalProperties_ShouldDefaultToNull()
    {
        // Arrange & Act
        var entry = new ReadAuditEntry
        {
            Id = Guid.NewGuid(),
            EntityType = "Patient",
            EntityId = null,
            AccessedAtUtc = DateTimeOffset.UtcNow,
            AccessMethod = ReadAccessMethod.Repository,
            EntityCount = 0
        };

        // Assert
        entry.UserId.Should().BeNull();
        entry.TenantId.Should().BeNull();
        entry.CorrelationId.Should().BeNull();
        entry.Purpose.Should().BeNull();
    }

    [Fact]
    public void Metadata_Default_ShouldBeEmptyDictionary()
    {
        // Arrange & Act
        var entry = new ReadAuditEntry
        {
            Id = Guid.NewGuid(),
            EntityType = "Patient",
            EntityId = null,
            AccessedAtUtc = DateTimeOffset.UtcNow,
            AccessMethod = ReadAccessMethod.Repository,
            EntityCount = 0
        };

        // Assert
        entry.Metadata.Should().NotBeNull();
        entry.Metadata.Should().BeEmpty();
    }

    [Fact]
    public void Metadata_WithCustomValues_ShouldBeAccessible()
    {
        // Arrange & Act
        var entry = new ReadAuditEntry
        {
            Id = Guid.NewGuid(),
            EntityType = "Patient",
            EntityId = null,
            AccessedAtUtc = DateTimeOffset.UtcNow,
            AccessMethod = ReadAccessMethod.Repository,
            EntityCount = 1,
            Metadata = new Dictionary<string, object?>
            {
                ["department"] = "Cardiology",
                ["source"] = "API"
            }
        };

        // Assert
        entry.Metadata.Should().HaveCount(2);
        entry.Metadata["department"].Should().Be("Cardiology");
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void Equality_SameValues_ShouldBeEqual()
    {
        // Arrange
        var id = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;

        var entry1 = new ReadAuditEntry
        {
            Id = id,
            EntityType = "Patient",
            EntityId = "P-1",
            AccessedAtUtc = timestamp,
            AccessMethod = ReadAccessMethod.Repository,
            EntityCount = 1
        };

        var entry2 = new ReadAuditEntry
        {
            Id = id,
            EntityType = "Patient",
            EntityId = "P-1",
            AccessedAtUtc = timestamp,
            AccessMethod = ReadAccessMethod.Repository,
            EntityCount = 1
        };

        // Assert — Record equality uses reference equality for Dictionary<string, object?>,
        // so we use BeEquivalentTo for deep structural comparison.
        entry1.Should().BeEquivalentTo(entry2);
    }

    [Fact]
    public void Equality_DifferentId_ShouldNotBeEqual()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;

        var entry1 = new ReadAuditEntry
        {
            Id = Guid.NewGuid(),
            EntityType = "Patient",
            EntityId = "P-1",
            AccessedAtUtc = timestamp,
            AccessMethod = ReadAccessMethod.Repository,
            EntityCount = 1
        };

        var entry2 = new ReadAuditEntry
        {
            Id = Guid.NewGuid(),
            EntityType = "Patient",
            EntityId = "P-1",
            AccessedAtUtc = timestamp,
            AccessMethod = ReadAccessMethod.Repository,
            EntityCount = 1
        };

        // Assert
        entry1.Should().NotBe(entry2);
    }

    #endregion

    #region ReadAccessMethod Tests

    [Theory]
    [InlineData(ReadAccessMethod.Repository, 0)]
    [InlineData(ReadAccessMethod.DirectQuery, 1)]
    [InlineData(ReadAccessMethod.Api, 2)]
    [InlineData(ReadAccessMethod.Export, 3)]
    [InlineData(ReadAccessMethod.Custom, 4)]
    public void ReadAccessMethod_ShouldHaveExpectedValues(ReadAccessMethod method, int expectedValue)
    {
        ((int)method).Should().Be(expectedValue);
    }

    #endregion
}
