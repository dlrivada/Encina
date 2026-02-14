using System.Diagnostics.CodeAnalysis;

namespace Encina.Cdc.Messaging;

/// <summary>
/// Notification published when a CDC change event is captured and routed through
/// the messaging bridge. Wraps the full change data for downstream handlers.
/// </summary>
/// <remarks>
/// <para>
/// This notification is published by <see cref="CdcMessagingBridge"/> when the
/// messaging bridge is enabled via <see cref="CdcConfiguration.WithMessagingBridge"/>.
/// Handlers implementing <c>INotificationHandler&lt;CdcChangeNotification&gt;</c>
/// will receive all captured changes that pass the configured filters.
/// </para>
/// <para>
/// The <see cref="TopicName"/> property provides a routing key based on the configured
/// topic pattern (default: <c>{tableName}.{operation}</c>), useful for downstream
/// transport routing.
/// </para>
/// </remarks>
/// <param name="TableName">The name of the database table where the change occurred.</param>
/// <param name="Operation">The type of change operation (Insert, Update, Delete, Snapshot).</param>
/// <param name="Before">The state of the row before the change, or <c>null</c> for Insert/Snapshot operations.</param>
/// <param name="After">The state of the row after the change, or <c>null</c> for Delete operations.</param>
/// <param name="Metadata">Metadata associated with the change, including position and timestamp.</param>
/// <param name="TopicName">The computed topic name based on the configured topic pattern.</param>
public sealed record CdcChangeNotification(
    string TableName,
    ChangeOperation Operation,
    object? Before,
    object? After,
    ChangeMetadata Metadata,
    string TopicName) : INotification
{
    /// <summary>
    /// Gets the shard identifier when this notification originates from a sharded CDC connector.
    /// Returns <c>null</c> for non-sharded CDC events.
    /// </summary>
    public string? ShardId { get; init; }

    /// <summary>
    /// Creates a <see cref="CdcChangeNotification"/> from a <see cref="ChangeEvent"/>
    /// using the specified topic pattern.
    /// </summary>
    /// <param name="changeEvent">The source change event.</param>
    /// <param name="topicPattern">
    /// The topic name pattern. Supports <c>{tableName}</c> and <c>{operation}</c> placeholders.
    /// Default: <c>{tableName}.{operation}</c>.
    /// </param>
    /// <returns>A new notification wrapping the change event data.</returns>
    [SuppressMessage("ApiDesign", "RS0027:API with optional parameter(s) should have the most parameters amongst its public overloads", Justification = "Pre-1.0: backward compatibility not required. The 3-param overload adds shard context.")]
    public static CdcChangeNotification FromChangeEvent(
        ChangeEvent changeEvent,
        string topicPattern = "{tableName}.{operation}")
    {
        return FromChangeEvent(changeEvent, topicPattern, shardId: null);
    }

    /// <summary>
    /// Creates a <see cref="CdcChangeNotification"/> from a <see cref="ChangeEvent"/>
    /// with optional shard context. When a <paramref name="shardId"/> is provided,
    /// the <c>{shardId}</c> placeholder in the topic pattern is resolved.
    /// </summary>
    /// <param name="changeEvent">The source change event.</param>
    /// <param name="topicPattern">
    /// The topic name pattern. Supports <c>{tableName}</c>, <c>{operation}</c>, and <c>{shardId}</c> placeholders.
    /// Default: <c>{tableName}.{operation}</c>.
    /// </param>
    /// <param name="shardId">
    /// The shard identifier, or <c>null</c> for non-sharded events. When <c>null</c>,
    /// the <c>{shardId}</c> placeholder resolves to an empty string.
    /// </param>
    /// <returns>A new notification wrapping the change event data with shard context.</returns>
    public static CdcChangeNotification FromChangeEvent(
        ChangeEvent changeEvent,
        string topicPattern,
        string? shardId)
    {
        ArgumentNullException.ThrowIfNull(changeEvent);
        ArgumentException.ThrowIfNullOrWhiteSpace(topicPattern);

        var topicName = topicPattern
            .Replace("{tableName}", changeEvent.TableName, StringComparison.OrdinalIgnoreCase)
            .Replace("{operation}", changeEvent.Operation.ToString().ToLowerInvariant(), StringComparison.OrdinalIgnoreCase)
            .Replace("{shardId}", shardId ?? string.Empty, StringComparison.OrdinalIgnoreCase);

        return new CdcChangeNotification(
            changeEvent.TableName,
            changeEvent.Operation,
            changeEvent.Before,
            changeEvent.After,
            changeEvent.Metadata,
            topicName)
        {
            ShardId = shardId
        };
    }
}
