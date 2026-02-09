using Encina.Cdc;
using Encina.Cdc.Abstractions;
using Encina.Cdc.Messaging;
using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Cdc.Messaging;

/// <summary>
/// Property-based tests for <see cref="CdcChangeNotification"/> invariants.
/// Verifies FromChangeEvent preserves data and topic generation follows expected patterns.
/// </summary>
[Trait("Category", "Property")]
public sealed class CdcChangeNotificationPropertyTests
{
    private static readonly ChangeOperation[] AllOperations =
        Enum.GetValues<ChangeOperation>();

    #region FromChangeEvent Preserves TableName

    [Property(MaxTest = 100)]
    public bool Property_FromChangeEvent_PreservesTableName(NonEmptyString tableName, int operationIndex, long positionValue)
    {
        // Property: FromChangeEvent always preserves the original TableName
        var operation = MapOperation(operationIndex);
        var changeEvent = CreateChangeEvent(tableName.Get, operation, positionValue);

        var notification = CdcChangeNotification.FromChangeEvent(changeEvent);

        return notification.TableName == tableName.Get;
    }

    #endregion

    #region FromChangeEvent Preserves Operation

    [Property(MaxTest = 100)]
    public bool Property_FromChangeEvent_PreservesOperation(NonEmptyString tableName, int operationIndex, long positionValue)
    {
        // Property: FromChangeEvent always preserves the original Operation
        var operation = MapOperation(operationIndex);
        var changeEvent = CreateChangeEvent(tableName.Get, operation, positionValue);

        var notification = CdcChangeNotification.FromChangeEvent(changeEvent);

        return notification.Operation == operation;
    }

    #endregion

    #region Default Topic Pattern Format

    [Property(MaxTest = 100)]
    public bool Property_DefaultTopic_ProducesTableNameDotOperation(NonEmptyString tableName, int operationIndex, long positionValue)
    {
        // Property: Default topic pattern produces "{TableName}.{operation}" format
        var operation = MapOperation(operationIndex);
        var changeEvent = CreateChangeEvent(tableName.Get, operation, positionValue);

        var notification = CdcChangeNotification.FromChangeEvent(changeEvent);

        var expected = $"{tableName.Get}.{operation.ToString().ToLowerInvariant()}";
        return notification.TopicName == expected;
    }

    #endregion

    #region Operation Is Lowercased In Default Topic

    [Property(MaxTest = 100)]
    public bool Property_DefaultTopic_OperationIsLowercased(NonEmptyString tableName, int operationIndex, long positionValue)
    {
        // Property: The operation portion of the default topic is always lowercased
        var operation = MapOperation(operationIndex);
        var changeEvent = CreateChangeEvent(tableName.Get, operation, positionValue);

        var notification = CdcChangeNotification.FromChangeEvent(changeEvent);

        var operationPart = notification.TopicName.Split('.').Last();
        return string.Equals(operationPart, operationPart.ToLowerInvariant(), StringComparison.Ordinal);
    }

    #endregion

    #region FromChangeEvent Preserves Metadata

    [Property(MaxTest = 100)]
    public bool Property_FromChangeEvent_PreservesMetadata(NonEmptyString tableName, int operationIndex, long positionValue)
    {
        // Property: FromChangeEvent preserves the full Metadata reference
        var operation = MapOperation(operationIndex);
        var changeEvent = CreateChangeEvent(tableName.Get, operation, positionValue);

        var notification = CdcChangeNotification.FromChangeEvent(changeEvent);

        return ReferenceEquals(notification.Metadata, changeEvent.Metadata);
    }

    #endregion

    #region FromChangeEvent Preserves Before/After

    [Property(MaxTest = 100)]
    public bool Property_FromChangeEvent_PreservesBefore(NonEmptyString tableName, long positionValue, int beforeValue)
    {
        // Property: FromChangeEvent preserves the Before reference
        var beforeObj = (object)beforeValue;
        var changeEvent = new ChangeEvent(
            tableName.Get,
            ChangeOperation.Update,
            beforeObj,
            null,
            CreateMetadata(positionValue));

        var notification = CdcChangeNotification.FromChangeEvent(changeEvent);

        return ReferenceEquals(notification.Before, beforeObj);
    }

    [Property(MaxTest = 100)]
    public bool Property_FromChangeEvent_PreservesAfter(NonEmptyString tableName, long positionValue, int afterValue)
    {
        // Property: FromChangeEvent preserves the After reference
        var afterObj = (object)afterValue;
        var changeEvent = new ChangeEvent(
            tableName.Get,
            ChangeOperation.Insert,
            null,
            afterObj,
            CreateMetadata(positionValue));

        var notification = CdcChangeNotification.FromChangeEvent(changeEvent);

        return ReferenceEquals(notification.After, afterObj);
    }

    #endregion

    #region Custom Topic Pattern

    [Property(MaxTest = 100)]
    public bool Property_CustomTopicPattern_ReplacesPlaceholders(NonEmptyString tableName, int operationIndex, long positionValue)
    {
        // Property: Custom topic pattern correctly replaces both {tableName} and {operation} placeholders
        var operation = MapOperation(operationIndex);
        var changeEvent = CreateChangeEvent(tableName.Get, operation, positionValue);

        var notification = CdcChangeNotification.FromChangeEvent(changeEvent, "cdc.{tableName}.{operation}");

        var expected = $"cdc.{tableName.Get}.{operation.ToString().ToLowerInvariant()}";
        return notification.TopicName == expected;
    }

    [Property(MaxTest = 100)]
    public bool Property_CustomTopicPattern_WithOnlyTableName(NonEmptyString tableName, int operationIndex, long positionValue)
    {
        // Property: A pattern with only {tableName} replaces just that placeholder
        var operation = MapOperation(operationIndex);
        var changeEvent = CreateChangeEvent(tableName.Get, operation, positionValue);

        var notification = CdcChangeNotification.FromChangeEvent(changeEvent, "changes.{tableName}");

        var expected = $"changes.{tableName.Get}";
        return notification.TopicName == expected;
    }

    #endregion

    #region TopicName Is Never Null Or Empty

    [Property(MaxTest = 100)]
    public bool Property_TopicName_IsNeverNullOrEmpty(NonEmptyString tableName, int operationIndex, long positionValue)
    {
        // Property: TopicName is always non-null and non-empty for valid inputs
        var operation = MapOperation(operationIndex);
        var changeEvent = CreateChangeEvent(tableName.Get, operation, positionValue);

        var notification = CdcChangeNotification.FromChangeEvent(changeEvent);

        return !string.IsNullOrEmpty(notification.TopicName);
    }

    #endregion

    #region Determinism

    [Property(MaxTest = 100)]
    public bool Property_FromChangeEvent_IsDeterministic(NonEmptyString tableName, int operationIndex, long positionValue)
    {
        // Property: Calling FromChangeEvent twice with the same input produces the same result
        var operation = MapOperation(operationIndex);
        var changeEvent = CreateChangeEvent(tableName.Get, operation, positionValue);

        var notification1 = CdcChangeNotification.FromChangeEvent(changeEvent);
        var notification2 = CdcChangeNotification.FromChangeEvent(changeEvent);

        return notification1.TableName == notification2.TableName
            && notification1.Operation == notification2.Operation
            && notification1.TopicName == notification2.TopicName
            && ReferenceEquals(notification1.Metadata, notification2.Metadata);
    }

    #endregion

    private static ChangeOperation MapOperation(int index)
    {
        var normalized = ((index % AllOperations.Length) + AllOperations.Length) % AllOperations.Length;
        return AllOperations[normalized];
    }

    private static ChangeEvent CreateChangeEvent(string tableName, ChangeOperation operation, long positionValue)
    {
        return new ChangeEvent(
            tableName,
            operation,
            null,
            null,
            CreateMetadata(positionValue));
    }

    private static ChangeMetadata CreateMetadata(long positionValue)
    {
        return new ChangeMetadata(
            new TestCdcPosition(positionValue),
            DateTime.UtcNow,
            null,
            null,
            null);
    }

    private sealed class TestCdcPosition : CdcPosition
    {
        public TestCdcPosition(long value) => Value = value;

        public long Value { get; }

        public override byte[] ToBytes() => BitConverter.GetBytes(Value);

        public override int CompareTo(CdcPosition? other) =>
            other is TestCdcPosition tcp ? Value.CompareTo(tcp.Value) : 1;

        public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
