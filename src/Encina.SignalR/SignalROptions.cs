namespace Encina.SignalR;

/// <summary>
/// Configuration options for Encina SignalR integration.
/// </summary>
public sealed class SignalROptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to automatically broadcast notifications
    /// marked with <see cref="BroadcastToSignalRAttribute"/> to SignalR clients.
    /// </summary>
    /// <remarks>
    /// When enabled, notifications with <c>[BroadcastToSignalR]</c> attribute will be
    /// automatically sent to connected SignalR clients after being published.
    /// </remarks>
    public bool EnableNotificationBroadcast { get; set; } = true;

    /// <summary>
    /// Gets or sets the authorization policy name required for clients to invoke
    /// Encina methods through the hub.
    /// </summary>
    /// <remarks>
    /// When set, clients must satisfy this policy to call SendCommand, SendQuery, etc.
    /// Leave null to allow anonymous access (not recommended for production).
    /// </remarks>
    public string? AuthorizationPolicy { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to include detailed error information
    /// in responses sent to clients.
    /// </summary>
    /// <remarks>
    /// When disabled, only generic error messages are sent to clients.
    /// Enable only in development environments.
    /// </remarks>
    public bool IncludeDetailedErrors { get; set; }

    /// <summary>
    /// Gets or sets the JSON serializer options for serializing messages.
    /// </summary>
    public System.Text.Json.JsonSerializerOptions? JsonSerializerOptions { get; set; }
}
