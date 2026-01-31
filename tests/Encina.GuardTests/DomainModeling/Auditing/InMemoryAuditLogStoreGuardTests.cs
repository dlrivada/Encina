using Encina.DomainModeling.Auditing;

namespace Encina.GuardTests.DomainModeling.Auditing;

/// <summary>
/// Guard tests for InMemoryAuditLogStore to verify null parameter handling.
/// </summary>
public class InMemoryAuditLogStoreGuardTests
{
    #region LogAsync Guard Tests

    [Fact]
    public async Task LogAsync_NullEntry_ThrowsArgumentNullException()
    {
        // Arrange
        var store = new InMemoryAuditLogStore();
        AuditLogEntry entry = null!;

        // Act
        var exception = await Should.ThrowAsync<ArgumentNullException>(async () =>
            await store.LogAsync(entry));

        // Assert
        exception.ParamName.ShouldBe("entry");
    }

    #endregion

    #region GetHistoryAsync Guard Tests

    [Fact]
    public async Task GetHistoryAsync_NullEntityType_ThrowsArgumentNullException()
    {
        // Arrange
        var store = new InMemoryAuditLogStore();

        // Act
        var exception = await Should.ThrowAsync<ArgumentNullException>(async () =>
            await store.GetHistoryAsync(null!, "123"));

        // Assert
        exception.ParamName.ShouldBe("entityType");
    }

    [Fact]
    public async Task GetHistoryAsync_NullEntityId_ThrowsArgumentNullException()
    {
        // Arrange
        var store = new InMemoryAuditLogStore();

        // Act
        var exception = await Should.ThrowAsync<ArgumentNullException>(async () =>
            await store.GetHistoryAsync("Order", null!));

        // Assert
        exception.ParamName.ShouldBe("entityId");
    }

    #endregion
}
