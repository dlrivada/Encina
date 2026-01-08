namespace Encina.SignalR;

/// <summary>
/// Marks a notification to be automatically broadcast to SignalR clients when published.
/// </summary>
/// <remarks>
/// <para>
/// Apply this attribute to notification types that should be sent to connected
/// SignalR clients. The notification will be serialized and sent using the specified
/// hub method name.
/// </para>
/// <para>
/// Supports targeting specific users, groups, or all connected clients.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Broadcast to all connected clients
/// [BroadcastToSignalR(Method = "OrderCreated")]
/// public record OrderCreatedNotification(Guid OrderId) : INotification;
///
/// // Broadcast to specific user (using property placeholder)
/// [BroadcastToSignalR(Method = "OrderUpdated", TargetUsers = "{CustomerId}")]
/// public record OrderUpdatedNotification(Guid OrderId, string CustomerId) : INotification;
///
/// // Broadcast to specific group
/// [BroadcastToSignalR(Method = "AdminAlert", TargetGroups = "Administrators")]
/// public record AdminAlertNotification(string Message) : INotification;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class BroadcastToSignalRAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the hub method name to invoke on clients.
    /// </summary>
    /// <remarks>
    /// This is the method name that clients should listen for.
    /// If not specified, defaults to the notification type name without "Notification" suffix.
    /// </remarks>
    public string? Method { get; set; }

    /// <summary>
    /// Gets or sets the target user IDs to send the notification to.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Supports property placeholders like "{UserId}" that will be resolved
    /// from the notification properties at runtime.
    /// </para>
    /// <para>
    /// Multiple users can be specified as comma-separated values.
    /// </para>
    /// </remarks>
    public string? TargetUsers { get; set; }

    /// <summary>
    /// Gets or sets the target group names to send the notification to.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Multiple groups can be specified as comma-separated values.
    /// </para>
    /// <para>
    /// Supports property placeholders like "{TenantId}" that will be resolved
    /// from the notification properties at runtime.
    /// </para>
    /// </remarks>
    public string? TargetGroups { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to exclude the caller from receiving
    /// the broadcast.
    /// </summary>
    /// <remarks>
    /// Only applicable when the notification is published from within a hub context.
    /// </remarks>
    public bool ExcludeCaller { get; set; }

    /// <summary>
    /// Gets or sets the name of a boolean property on the notification that determines
    /// whether to broadcast.
    /// </summary>
    /// <remarks>
    /// If specified, the notification will only be broadcast if this property returns true.
    /// Use for conditional broadcasting based on notification state.
    /// </remarks>
    /// <example>
    /// <code>
    /// [BroadcastToSignalR(Method = "PriceChanged", ConditionalProperty = "ShouldBroadcast")]
    /// public record PriceChangedNotification(decimal OldPrice, decimal NewPrice) : INotification
    /// {
    ///     public bool ShouldBroadcast => Math.Abs(NewPrice - OldPrice) / OldPrice > 0.05m;
    /// }
    /// </code>
    /// </example>
    public string? ConditionalProperty { get; set; }

    /// <summary>
    /// Gets the target users as a parsed array.
    /// </summary>
    /// <returns>
    /// An array of user identifiers, or an empty array if <see cref="TargetUsers"/> is null or empty.
    /// Values are trimmed and empty entries are removed.
    /// </returns>
    public string[] GetTargetUsersList() =>
        string.IsNullOrWhiteSpace(TargetUsers)
            ? []
            : TargetUsers.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>
    /// Gets the target groups as a parsed array.
    /// </summary>
    /// <returns>
    /// An array of group names, or an empty array if <see cref="TargetGroups"/> is null or empty.
    /// Values are trimmed and empty entries are removed.
    /// </returns>
    public string[] GetTargetGroupsList() =>
        string.IsNullOrWhiteSpace(TargetGroups)
            ? []
            : TargetGroups.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
