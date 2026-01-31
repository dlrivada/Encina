using Encina.DomainModeling.Auditing;

namespace Encina.UnitTests.DomainModeling.Auditing;

public class AuditLogEntryTests
{
    #region Construction Tests

    [Fact]
    public void AuditLogEntry_Constructor_SetsAllProperties()
    {
        // Arrange
        const string id = "entry-123";
        const string entityType = "Order";
        const string entityId = "order-456";
        var action = AuditAction.Created;
        const string userId = "user-789";
        var timestamp = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        const string? oldValues = null;
        const string newValues = "{\"Total\":100}";
        const string correlationId = "corr-abc";

        // Act
        var entry = new AuditLogEntry(
            Id: id,
            EntityType: entityType,
            EntityId: entityId,
            Action: action,
            UserId: userId,
            TimestampUtc: timestamp,
            OldValues: oldValues,
            NewValues: newValues,
            CorrelationId: correlationId);

        // Assert
        entry.Id.ShouldBe(id);
        entry.EntityType.ShouldBe(entityType);
        entry.EntityId.ShouldBe(entityId);
        entry.Action.ShouldBe(action);
        entry.UserId.ShouldBe(userId);
        entry.TimestampUtc.ShouldBe(timestamp);
        entry.OldValues.ShouldBeNull();
        entry.NewValues.ShouldBe(newValues);
        entry.CorrelationId.ShouldBe(correlationId);
    }

    [Fact]
    public void AuditLogEntry_Created_HasNullOldValues()
    {
        // Arrange & Act
        var entry = new AuditLogEntry(
            Id: "1",
            EntityType: "Order",
            EntityId: "123",
            Action: AuditAction.Created,
            UserId: "user",
            TimestampUtc: DateTime.UtcNow,
            OldValues: null,
            NewValues: "{\"Status\":\"Pending\"}",
            CorrelationId: null);

        // Assert
        entry.Action.ShouldBe(AuditAction.Created);
        entry.OldValues.ShouldBeNull();
        entry.NewValues.ShouldNotBeNull();
    }

    [Fact]
    public void AuditLogEntry_Deleted_HasNullNewValues()
    {
        // Arrange & Act
        var entry = new AuditLogEntry(
            Id: "1",
            EntityType: "Order",
            EntityId: "123",
            Action: AuditAction.Deleted,
            UserId: "user",
            TimestampUtc: DateTime.UtcNow,
            OldValues: "{\"Status\":\"Active\"}",
            NewValues: null,
            CorrelationId: null);

        // Assert
        entry.Action.ShouldBe(AuditAction.Deleted);
        entry.OldValues.ShouldNotBeNull();
        entry.NewValues.ShouldBeNull();
    }

    [Fact]
    public void AuditLogEntry_Updated_HasBothOldAndNewValues()
    {
        // Arrange & Act
        var entry = new AuditLogEntry(
            Id: "1",
            EntityType: "Order",
            EntityId: "123",
            Action: AuditAction.Updated,
            UserId: "user",
            TimestampUtc: DateTime.UtcNow,
            OldValues: "{\"Status\":\"Pending\"}",
            NewValues: "{\"Status\":\"Shipped\"}",
            CorrelationId: null);

        // Assert
        entry.Action.ShouldBe(AuditAction.Updated);
        entry.OldValues.ShouldNotBeNull();
        entry.NewValues.ShouldNotBeNull();
    }

    #endregion

    #region Nullable Property Tests

    [Fact]
    public void AuditLogEntry_UserId_CanBeNull()
    {
        // Arrange & Act
        var entry = new AuditLogEntry(
            Id: "1",
            EntityType: "Order",
            EntityId: "123",
            Action: AuditAction.Created,
            UserId: null,
            TimestampUtc: DateTime.UtcNow,
            OldValues: null,
            NewValues: "{}",
            CorrelationId: null);

        // Assert
        entry.UserId.ShouldBeNull();
    }

    [Fact]
    public void AuditLogEntry_CorrelationId_CanBeNull()
    {
        // Arrange & Act
        var entry = new AuditLogEntry(
            Id: "1",
            EntityType: "Order",
            EntityId: "123",
            Action: AuditAction.Created,
            UserId: "user",
            TimestampUtc: DateTime.UtcNow,
            OldValues: null,
            NewValues: "{}",
            CorrelationId: null);

        // Assert
        entry.CorrelationId.ShouldBeNull();
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void AuditLogEntry_WithSameValues_AreEqual()
    {
        // Arrange
        var timestamp = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var entry1 = new AuditLogEntry(
            Id: "1",
            EntityType: "Order",
            EntityId: "123",
            Action: AuditAction.Created,
            UserId: "user",
            TimestampUtc: timestamp,
            OldValues: null,
            NewValues: "{}",
            CorrelationId: "corr");

        var entry2 = new AuditLogEntry(
            Id: "1",
            EntityType: "Order",
            EntityId: "123",
            Action: AuditAction.Created,
            UserId: "user",
            TimestampUtc: timestamp,
            OldValues: null,
            NewValues: "{}",
            CorrelationId: "corr");

        // Assert
        entry1.ShouldBe(entry2);
        (entry1 == entry2).ShouldBeTrue();
        entry1.GetHashCode().ShouldBe(entry2.GetHashCode());
    }

    [Fact]
    public void AuditLogEntry_WithDifferentId_AreNotEqual()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var entry1 = new AuditLogEntry("1", "Order", "123", AuditAction.Created,
            "user", timestamp, null, "{}", null);
        var entry2 = new AuditLogEntry("2", "Order", "123", AuditAction.Created,
            "user", timestamp, null, "{}", null);

        // Assert
        entry1.ShouldNotBe(entry2);
        (entry1 != entry2).ShouldBeTrue();
    }

    [Fact]
    public void AuditLogEntry_WithDifferentAction_AreNotEqual()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var entry1 = new AuditLogEntry("1", "Order", "123", AuditAction.Created,
            "user", timestamp, null, "{}", null);
        var entry2 = new AuditLogEntry("1", "Order", "123", AuditAction.Updated,
            "user", timestamp, null, "{}", null);

        // Assert
        entry1.ShouldNotBe(entry2);
    }

    #endregion

    #region Record With Expression Tests

    [Fact]
    public void AuditLogEntry_WithExpression_CreatesCopyWithModifiedProperty()
    {
        // Arrange
        var original = new AuditLogEntry(
            Id: "1",
            EntityType: "Order",
            EntityId: "123",
            Action: AuditAction.Created,
            UserId: "user1",
            TimestampUtc: DateTime.UtcNow,
            OldValues: null,
            NewValues: "{}",
            CorrelationId: null);

        // Act
        var modified = original with { UserId = "user2" };

        // Assert
        modified.Id.ShouldBe(original.Id);
        modified.EntityType.ShouldBe(original.EntityType);
        modified.UserId.ShouldBe("user2");
        modified.ShouldNotBe(original);
    }

    #endregion

    #region AuditAction Enum Tests

    [Fact]
    public void AuditAction_Created_HasValue0()
    {
        // Assert
        ((int)AuditAction.Created).ShouldBe(0);
    }

    [Fact]
    public void AuditAction_Updated_HasValue1()
    {
        // Assert
        ((int)AuditAction.Updated).ShouldBe(1);
    }

    [Fact]
    public void AuditAction_Deleted_HasValue2()
    {
        // Assert
        ((int)AuditAction.Deleted).ShouldBe(2);
    }

    [Fact]
    public void AuditAction_AllValues_AreDefined()
    {
        // Act
        var values = Enum.GetValues<AuditAction>();

        // Assert
        values.ShouldContain(AuditAction.Created);
        values.ShouldContain(AuditAction.Updated);
        values.ShouldContain(AuditAction.Deleted);
        values.Length.ShouldBe(3);
    }

    #endregion
}
