using Encina.DomainModeling.Auditing;
using Encina.MongoDB.Auditing;

namespace Encina.UnitTests.MongoDB.Auditing;

/// <summary>
/// Unit tests for <see cref="AuditLogDocument"/>.
/// </summary>
public sealed class AuditLogDocumentTests
{
    #region FromEntry Tests

    [Fact]
    public void FromEntry_ValidEntry_MapsAllProperties()
    {
        // Arrange
        var entry = new AuditLogEntry(
            Id: "test-id",
            EntityType: "Order",
            EntityId: "order-123",
            Action: AuditAction.Updated,
            UserId: "user-456",
            TimestampUtc: new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
            OldValues: "{\"Status\":\"Pending\"}",
            NewValues: "{\"Status\":\"Shipped\"}",
            CorrelationId: "corr-789");

        // Act
        var document = AuditLogDocument.FromEntry(entry);

        // Assert
        document.Id.ShouldBe(entry.Id);
        document.EntityType.ShouldBe(entry.EntityType);
        document.EntityId.ShouldBe(entry.EntityId);
        document.Action.ShouldBe((int)entry.Action);
        document.UserId.ShouldBe(entry.UserId);
        document.TimestampUtc.ShouldBe(entry.TimestampUtc);
        document.OldValues.ShouldBe(entry.OldValues);
        document.NewValues.ShouldBe(entry.NewValues);
        document.CorrelationId.ShouldBe(entry.CorrelationId);
    }

    [Fact]
    public void FromEntry_NullEntry_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            AuditLogDocument.FromEntry(null!));
    }

    [Fact]
    public void FromEntry_EntryWithNullOptionalFields_MapsCorrectly()
    {
        // Arrange
        var entry = new AuditLogEntry(
            Id: "test-id",
            EntityType: "Order",
            EntityId: "order-123",
            Action: AuditAction.Created,
            UserId: null,
            TimestampUtc: DateTime.UtcNow,
            OldValues: null,
            NewValues: null,
            CorrelationId: null);

        // Act
        var document = AuditLogDocument.FromEntry(entry);

        // Assert
        document.UserId.ShouldBeNull();
        document.OldValues.ShouldBeNull();
        document.NewValues.ShouldBeNull();
        document.CorrelationId.ShouldBeNull();
    }

    [Theory]
    [InlineData(AuditAction.Created, 0)]
    [InlineData(AuditAction.Updated, 1)]
    [InlineData(AuditAction.Deleted, 2)]
    public void FromEntry_CorrectlyMapsActionToInt(AuditAction action, int expectedValue)
    {
        // Arrange
        var entry = new AuditLogEntry(
            Id: "test-id",
            EntityType: "Order",
            EntityId: "order-123",
            Action: action,
            UserId: null,
            TimestampUtc: DateTime.UtcNow,
            OldValues: null,
            NewValues: null,
            CorrelationId: null);

        // Act
        var document = AuditLogDocument.FromEntry(entry);

        // Assert
        document.Action.ShouldBe(expectedValue);
    }

    #endregion

    #region ToEntry Tests

    [Fact]
    public void ToEntry_ValidDocument_MapsAllProperties()
    {
        // Arrange
        var document = new AuditLogDocument
        {
            Id = "test-id",
            EntityType = "Order",
            EntityId = "order-123",
            Action = (int)AuditAction.Updated,
            UserId = "user-456",
            TimestampUtc = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
            OldValues = "{\"Status\":\"Pending\"}",
            NewValues = "{\"Status\":\"Shipped\"}",
            CorrelationId = "corr-789"
        };

        // Act
        var entry = document.ToEntry();

        // Assert
        entry.Id.ShouldBe(document.Id);
        entry.EntityType.ShouldBe(document.EntityType);
        entry.EntityId.ShouldBe(document.EntityId);
        entry.Action.ShouldBe((AuditAction)document.Action);
        entry.UserId.ShouldBe(document.UserId);
        entry.TimestampUtc.ShouldBe(document.TimestampUtc);
        entry.OldValues.ShouldBe(document.OldValues);
        entry.NewValues.ShouldBe(document.NewValues);
        entry.CorrelationId.ShouldBe(document.CorrelationId);
    }

    [Fact]
    public void ToEntry_DocumentWithNullOptionalFields_MapsCorrectly()
    {
        // Arrange
        var document = new AuditLogDocument
        {
            Id = "test-id",
            EntityType = "Order",
            EntityId = "order-123",
            Action = (int)AuditAction.Created,
            UserId = null,
            TimestampUtc = DateTime.UtcNow,
            OldValues = null,
            NewValues = null,
            CorrelationId = null
        };

        // Act
        var entry = document.ToEntry();

        // Assert
        entry.UserId.ShouldBeNull();
        entry.OldValues.ShouldBeNull();
        entry.NewValues.ShouldBeNull();
        entry.CorrelationId.ShouldBeNull();
    }

    [Theory]
    [InlineData(0, AuditAction.Created)]
    [InlineData(1, AuditAction.Updated)]
    [InlineData(2, AuditAction.Deleted)]
    public void ToEntry_CorrectlyMapsIntToAction(int actionValue, AuditAction expectedAction)
    {
        // Arrange
        var document = new AuditLogDocument
        {
            Id = "test-id",
            EntityType = "Order",
            EntityId = "order-123",
            Action = actionValue,
            TimestampUtc = DateTime.UtcNow
        };

        // Act
        var entry = document.ToEntry();

        // Assert
        entry.Action.ShouldBe(expectedAction);
    }

    #endregion

    #region Roundtrip Tests

    [Fact]
    public void FromEntryToEntry_RoundTrip_PreservesData()
    {
        // Arrange
        var originalEntry = new AuditLogEntry(
            Id: Guid.NewGuid().ToString(),
            EntityType: "Customer",
            EntityId: "cust-456",
            Action: AuditAction.Deleted,
            UserId: "admin",
            TimestampUtc: new DateTime(2024, 6, 15, 14, 30, 45, DateTimeKind.Utc),
            OldValues: "{\"Name\":\"John\",\"Status\":\"Active\"}",
            NewValues: null,
            CorrelationId: "request-xyz");

        // Act
        var document = AuditLogDocument.FromEntry(originalEntry);
        var roundTrippedEntry = document.ToEntry();

        // Assert
        roundTrippedEntry.ShouldBe(originalEntry);
    }

    #endregion

    #region Default Values Tests

    [Fact]
    public void NewDocument_HasDefaultValues()
    {
        // Arrange & Act
        var document = new AuditLogDocument();

        // Assert
        document.Id.ShouldBe(string.Empty);
        document.EntityType.ShouldBe(string.Empty);
        document.EntityId.ShouldBe(string.Empty);
        document.Action.ShouldBe(0);
        document.UserId.ShouldBeNull();
        document.TimestampUtc.ShouldBe(default);
        document.OldValues.ShouldBeNull();
        document.NewValues.ShouldBeNull();
        document.CorrelationId.ShouldBeNull();
    }

    #endregion
}
