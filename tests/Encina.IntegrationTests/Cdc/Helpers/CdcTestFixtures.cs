using System.Text.Json;
using Encina.Cdc;
using Encina.Cdc.Abstractions;

namespace Encina.IntegrationTests.Cdc.Helpers;

/// <summary>
/// Provides factory methods for creating CDC test data used in integration tests.
/// </summary>
internal static class CdcTestFixtures
{
    private static readonly DateTime FixedUtcNow = new(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Creates a <see cref="ChangeEvent"/> with the given parameters and serialized entity data.
    /// </summary>
    public static ChangeEvent CreateChangeEvent(
        string tableName,
        ChangeOperation operation,
        object? before = null,
        object? after = null,
        long positionValue = 1)
    {
        var position = new TestCdcPosition(positionValue);
        var metadata = new ChangeMetadata(position, FixedUtcNow, null, null, null);
        return new ChangeEvent(tableName, operation, before, after, metadata);
    }

    /// <summary>
    /// Creates a <see cref="ChangeEvent"/> with JSON-serialized entity data for the dispatcher.
    /// </summary>
    public static ChangeEvent CreateJsonChangeEvent(
        string tableName,
        ChangeOperation operation,
        object? before = null,
        object? after = null,
        long positionValue = 1)
    {
        var serializedBefore = before is not null
            ? JsonSerializer.SerializeToElement(before)
            : (object?)null;
        var serializedAfter = after is not null
            ? JsonSerializer.SerializeToElement(after)
            : (object?)null;

        return CreateChangeEvent(tableName, operation, serializedBefore, serializedAfter, positionValue);
    }

    /// <summary>
    /// Creates a <see cref="TestEntity"/> with the given values.
    /// </summary>
    public static TestEntity CreateTestEntity(int id = 1, string name = "Test", decimal price = 9.99m) =>
        new() { Id = id, Name = name, Price = price };

    /// <summary>
    /// Creates an insert <see cref="ChangeEvent"/> for a <see cref="TestEntity"/>.
    /// </summary>
    public static ChangeEvent CreateInsertEvent(
        string tableName = "TestEntities",
        int id = 1,
        string name = "Test",
        long positionValue = 1) =>
        CreateJsonChangeEvent(
            tableName,
            ChangeOperation.Insert,
            after: CreateTestEntity(id, name),
            positionValue: positionValue);

    /// <summary>
    /// Creates an update <see cref="ChangeEvent"/> for a <see cref="TestEntity"/>.
    /// </summary>
    public static ChangeEvent CreateUpdateEvent(
        string tableName = "TestEntities",
        int id = 1,
        string oldName = "Old",
        string newName = "New",
        long positionValue = 1) =>
        CreateJsonChangeEvent(
            tableName,
            ChangeOperation.Update,
            before: CreateTestEntity(id, oldName),
            after: CreateTestEntity(id, newName),
            positionValue: positionValue);

    /// <summary>
    /// Creates a delete <see cref="ChangeEvent"/> for a <see cref="TestEntity"/>.
    /// </summary>
    public static ChangeEvent CreateDeleteEvent(
        string tableName = "TestEntities",
        int id = 1,
        string name = "Deleted",
        long positionValue = 1) =>
        CreateJsonChangeEvent(
            tableName,
            ChangeOperation.Delete,
            before: CreateTestEntity(id, name),
            positionValue: positionValue);
}
