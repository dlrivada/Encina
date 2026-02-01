using Encina.Security.Audit;
using FluentAssertions;

namespace Encina.UnitTests.Security.Audit;

/// <summary>
/// Unit tests for <see cref="AuditEntry"/>.
/// </summary>
public class AuditEntryTests
{
    [Fact]
    public void InitProperties_ShouldSetAllValues()
    {
        // Arrange
        var id = Guid.NewGuid();
        var correlationId = "correlation-123";
        var userId = "user-456";
        var tenantId = "tenant-789";
        var action = "Create";
        var entityType = "Order";
        var entityId = "order-001";
        var outcome = AuditOutcome.Success;
        var timestampUtc = DateTime.UtcNow;
        var ipAddress = "192.168.1.1";
        var userAgent = "Mozilla/5.0";
        var payloadHash = "abc123";
        var metadata = new Dictionary<string, object?> { ["key"] = "value" };

        // Act
        var entry = new AuditEntry
        {
            Id = id,
            CorrelationId = correlationId,
            UserId = userId,
            TenantId = tenantId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Outcome = outcome,
            TimestampUtc = timestampUtc,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            RequestPayloadHash = payloadHash,
            Metadata = metadata
        };

        // Assert
        entry.Id.Should().Be(id);
        entry.CorrelationId.Should().Be(correlationId);
        entry.UserId.Should().Be(userId);
        entry.TenantId.Should().Be(tenantId);
        entry.Action.Should().Be(action);
        entry.EntityType.Should().Be(entityType);
        entry.EntityId.Should().Be(entityId);
        entry.Outcome.Should().Be(outcome);
        entry.TimestampUtc.Should().Be(timestampUtc);
        entry.IpAddress.Should().Be(ipAddress);
        entry.UserAgent.Should().Be(userAgent);
        entry.RequestPayloadHash.Should().Be(payloadHash);
        entry.Metadata.Should().ContainKey("key");
    }

    [Fact]
    public void Metadata_ShouldBeReadOnly()
    {
        // Arrange
        var metadata = new Dictionary<string, object?> { ["initial"] = "value" };
        var entry = CreateEntry(metadata: metadata);

        // Assert - IReadOnlyDictionary prevents modification
        entry.Metadata.Should().BeAssignableTo<IReadOnlyDictionary<string, object?>>();
    }

    [Fact]
    public void Metadata_ShouldDefaultToEmptyDictionary()
    {
        // Arrange & Act
        var entry = CreateEntry();

        // Assert
        entry.Metadata.Should().NotBeNull().And.BeEmpty();
    }

    [Theory]
    [InlineData(AuditOutcome.Success)]
    [InlineData(AuditOutcome.Failure)]
    [InlineData(AuditOutcome.Denied)]
    [InlineData(AuditOutcome.Error)]
    public void Outcome_ShouldAcceptAllValidValues(AuditOutcome outcome)
    {
        // Act
        var entry = CreateEntry(outcome: outcome);

        // Assert
        entry.Outcome.Should().Be(outcome);
    }

    [Fact]
    public void ErrorMessage_ShouldBeNullableAndSettable()
    {
        // Arrange & Act
        var entryWithError = CreateEntry(errorMessage: "Something went wrong");
        var entryWithoutError = CreateEntry(errorMessage: null);

        // Assert
        entryWithError.ErrorMessage.Should().Be("Something went wrong");
        entryWithoutError.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Record_ShouldSupportWithExpression()
    {
        // Arrange
        var original = CreateEntry(action: "Create");

        // Act
        var modified = original with { Action = "Update" };

        // Assert
        modified.Action.Should().Be("Update");
        modified.Id.Should().Be(original.Id);
        modified.CorrelationId.Should().Be(original.CorrelationId);
        modified.EntityType.Should().Be(original.EntityType);
    }

    [Fact]
    public void Equality_WithDifferentAction_ShouldNotBeEqual()
    {
        // Arrange - two entries with only Action different
        var id = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;
        var metadata = new Dictionary<string, object?>();
        var entry1 = new AuditEntry
        {
            Id = id,
            CorrelationId = "corr",
            Action = "Create",
            EntityType = "Order",
            Outcome = AuditOutcome.Success,
            TimestampUtc = timestamp,
            Metadata = metadata
        };
        var entry2 = new AuditEntry
        {
            Id = id,
            CorrelationId = "corr",
            Action = "Update", // Different action
            EntityType = "Order",
            Outcome = AuditOutcome.Success,
            TimestampUtc = timestamp,
            Metadata = metadata
        };

        // Assert - Records should not be equal when a property differs
        entry1.Should().NotBe(entry2);
    }

    [Fact]
    public void Equality_WithSameMetadataInstance_ShouldBeEqual()
    {
        // Arrange - same metadata instance shared between entries
        var id = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;
        var metadata = new Dictionary<string, object?>();
        var entry1 = new AuditEntry
        {
            Id = id,
            CorrelationId = "corr",
            Action = "Create",
            EntityType = "Order",
            Outcome = AuditOutcome.Success,
            TimestampUtc = timestamp,
            Metadata = metadata
        };
        var entry2 = new AuditEntry
        {
            Id = id,
            CorrelationId = "corr",
            Action = "Create",
            EntityType = "Order",
            Outcome = AuditOutcome.Success,
            TimestampUtc = timestamp,
            Metadata = metadata
        };

        // Assert - When metadata is same instance, records are equal
        entry1.Should().Be(entry2);
        entry1.GetHashCode().Should().Be(entry2.GetHashCode());
    }

    [Fact]
    public void OptionalProperties_ShouldDefaultToNull()
    {
        // Arrange & Act
        var entry = CreateEntry();

        // Assert
        entry.UserId.Should().BeNull();
        entry.TenantId.Should().BeNull();
        entry.EntityId.Should().BeNull();
        entry.ErrorMessage.Should().BeNull();
        entry.IpAddress.Should().BeNull();
        entry.UserAgent.Should().BeNull();
        entry.RequestPayloadHash.Should().BeNull();
    }

    private static AuditEntry CreateEntry(
        Guid? id = null,
        string? correlationId = null,
        string? action = null,
        string? entityType = null,
        AuditOutcome? outcome = null,
        DateTime? timestampUtc = null,
        string? errorMessage = null,
        IReadOnlyDictionary<string, object?>? metadata = null)
    {
        return new AuditEntry
        {
            Id = id ?? Guid.NewGuid(),
            CorrelationId = correlationId ?? "test-correlation",
            Action = action ?? "Test",
            EntityType = entityType ?? "TestEntity",
            Outcome = outcome ?? AuditOutcome.Success,
            TimestampUtc = timestampUtc ?? DateTime.UtcNow,
            ErrorMessage = errorMessage,
            Metadata = metadata ?? new Dictionary<string, object?>()
        };
    }
}
