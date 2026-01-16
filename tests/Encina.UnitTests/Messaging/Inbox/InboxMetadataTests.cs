using Encina.Messaging.Inbox;
using Shouldly;

namespace Encina.UnitTests.Messaging.Inbox;

/// <summary>
/// Unit tests for <see cref="InboxMetadata"/>.
/// </summary>
public sealed class InboxMetadataTests
{
    [Fact]
    public void InboxMetadata_DefaultValues_AreNull()
    {
        // Arrange & Act
        var metadata = new InboxMetadata();

        // Assert
        metadata.CorrelationId.ShouldBeNull();
        metadata.UserId.ShouldBeNull();
        metadata.TenantId.ShouldBeNull();
        metadata.Timestamp.ShouldBe(default);
    }

    [Fact]
    public void InboxMetadata_SetCorrelationId_ReturnsCorrectValue()
    {
        // Arrange
        var metadata = new InboxMetadata();
        const string correlationId = "correlation-123";

        // Act
        metadata.CorrelationId = correlationId;

        // Assert
        metadata.CorrelationId.ShouldBe(correlationId);
    }

    [Fact]
    public void InboxMetadata_SetUserId_ReturnsCorrectValue()
    {
        // Arrange
        var metadata = new InboxMetadata();
        const string userId = "user-456";

        // Act
        metadata.UserId = userId;

        // Assert
        metadata.UserId.ShouldBe(userId);
    }

    [Fact]
    public void InboxMetadata_SetTenantId_ReturnsCorrectValue()
    {
        // Arrange
        var metadata = new InboxMetadata();
        const string tenantId = "tenant-789";

        // Act
        metadata.TenantId = tenantId;

        // Assert
        metadata.TenantId.ShouldBe(tenantId);
    }

    [Fact]
    public void InboxMetadata_SetTimestamp_ReturnsCorrectValue()
    {
        // Arrange
        var metadata = new InboxMetadata();
        var timestamp = DateTimeOffset.UtcNow;

        // Act
        metadata.Timestamp = timestamp;

        // Assert
        metadata.Timestamp.ShouldBe(timestamp);
    }

    [Fact]
    public void InboxMetadata_SetAllProperties_ReturnsCorrectValues()
    {
        // Arrange
        var metadata = new InboxMetadata();
        const string correlationId = "corr-001";
        const string userId = "user-001";
        const string tenantId = "tenant-001";
        var timestamp = new DateTimeOffset(2026, 1, 7, 12, 0, 0, TimeSpan.Zero);

        // Act
        metadata.CorrelationId = correlationId;
        metadata.UserId = userId;
        metadata.TenantId = tenantId;
        metadata.Timestamp = timestamp;

        // Assert
        metadata.CorrelationId.ShouldBe(correlationId);
        metadata.UserId.ShouldBe(userId);
        metadata.TenantId.ShouldBe(tenantId);
        metadata.Timestamp.ShouldBe(timestamp);
    }
}
