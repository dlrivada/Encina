using Encina.Security.Audit;
using Shouldly;

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

        var startedAtUtc = DateTimeOffset.UtcNow.AddSeconds(-1);
        var completedAtUtc = DateTimeOffset.UtcNow;

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
            StartedAtUtc = startedAtUtc,
            CompletedAtUtc = completedAtUtc,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            RequestPayloadHash = payloadHash,
            Metadata = metadata
        };

        // Assert
        entry.Id.ShouldBe(id);
        entry.CorrelationId.ShouldBe(correlationId);
        entry.UserId.ShouldBe(userId);
        entry.TenantId.ShouldBe(tenantId);
        entry.Action.ShouldBe(action);
        entry.EntityType.ShouldBe(entityType);
        entry.EntityId.ShouldBe(entityId);
        entry.Outcome.ShouldBe(outcome);
        entry.TimestampUtc.ShouldBe(timestampUtc);
        entry.IpAddress.ShouldBe(ipAddress);
        entry.UserAgent.ShouldBe(userAgent);
        entry.RequestPayloadHash.ShouldBe(payloadHash);
        entry.Metadata.ShouldContainKey("key");
    }

    [Fact]
    public void Metadata_ShouldBeReadOnly()
    {
        // Arrange
        var metadata = new Dictionary<string, object?> { ["initial"] = "value" };
        var entry = CreateEntry(metadata: metadata);

        // Assert - IReadOnlyDictionary prevents modification
        entry.Metadata.ShouldBeAssignableTo<IReadOnlyDictionary<string, object?>>();
    }

    [Fact]
    public void Metadata_ShouldDefaultToEmptyDictionary()
    {
        // Arrange & Act
        var entry = CreateEntry();

        // Assert
        entry.Metadata.ShouldNotBeNull().And.BeEmpty();
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
        entry.Outcome.ShouldBe(outcome);
    }

    [Fact]
    public void ErrorMessage_ShouldBeNullableAndSettable()
    {
        // Arrange & Act
        var entryWithError = CreateEntry(errorMessage: "Something went wrong");
        var entryWithoutError = CreateEntry(errorMessage: null);

        // Assert
        entryWithError.ErrorMessage.ShouldBe("Something went wrong");
        entryWithoutError.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public void Record_ShouldSupportWithExpression()
    {
        // Arrange
        var original = CreateEntry(action: "Create");

        // Act
        var modified = original with { Action = "Update" };

        // Assert
        modified.Action.ShouldBe("Update");
        modified.Id.ShouldBe(original.Id);
        modified.CorrelationId.ShouldBe(original.CorrelationId);
        modified.EntityType.ShouldBe(original.EntityType);
    }

    [Fact]
    public void Equality_WithDifferentAction_ShouldNotBeEqual()
    {
        // Arrange - two entries with only Action different
        var id = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;
        var metadata = new Dictionary<string, object?>();
        var startedAtUtc = DateTimeOffset.UtcNow.AddSeconds(-1);
        var completedAtUtc = DateTimeOffset.UtcNow;
        var entry1 = new AuditEntry
        {
            Id = id,
            CorrelationId = "corr",
            Action = "Create",
            EntityType = "Order",
            Outcome = AuditOutcome.Success,
            TimestampUtc = timestamp,
            StartedAtUtc = startedAtUtc,
            CompletedAtUtc = completedAtUtc,
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
            StartedAtUtc = startedAtUtc,
            CompletedAtUtc = completedAtUtc,
            Metadata = metadata
        };

        // Assert - Records should not be equal when a property differs
        entry1.ShouldNotBe(entry2);
    }

    [Fact]
    public void Equality_WithSameMetadataInstance_ShouldBeEqual()
    {
        // Arrange - same metadata instance shared between entries
        var id = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;
        var metadata = new Dictionary<string, object?>();
        var startedAtUtc = DateTimeOffset.UtcNow.AddSeconds(-1);
        var completedAtUtc = DateTimeOffset.UtcNow;
        var entry1 = new AuditEntry
        {
            Id = id,
            CorrelationId = "corr",
            Action = "Create",
            EntityType = "Order",
            Outcome = AuditOutcome.Success,
            TimestampUtc = timestamp,
            StartedAtUtc = startedAtUtc,
            CompletedAtUtc = completedAtUtc,
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
            StartedAtUtc = startedAtUtc,
            CompletedAtUtc = completedAtUtc,
            Metadata = metadata
        };

        // Assert - When metadata is same instance, records are equal
        entry1.ShouldBe(entry2);
        entry1.GetHashCode().ShouldBe(entry2.GetHashCode());
    }

    [Fact]
    public void OptionalProperties_ShouldDefaultToNull()
    {
        // Arrange & Act
        var entry = CreateEntry();

        // Assert
        entry.UserId.ShouldBeNull();
        entry.TenantId.ShouldBeNull();
        entry.EntityId.ShouldBeNull();
        entry.ErrorMessage.ShouldBeNull();
        entry.IpAddress.ShouldBeNull();
        entry.UserAgent.ShouldBeNull();
        entry.RequestPayloadHash.ShouldBeNull();
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
            StartedAtUtc = DateTimeOffset.UtcNow.AddSeconds(-1),
            CompletedAtUtc = DateTimeOffset.UtcNow,
            ErrorMessage = errorMessage,
            Metadata = metadata ?? new Dictionary<string, object?>()
        };
    }
}
