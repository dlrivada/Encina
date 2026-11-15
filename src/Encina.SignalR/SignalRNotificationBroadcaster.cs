using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Encina.Diagnostics;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

#pragma warning disable SYSLIB1045 // Use GeneratedRegexAttribute - Pattern contains placeholder syntax that requires runtime evaluation

namespace Encina.SignalR;

/// <summary>
/// Service that broadcasts notifications marked with <see cref="BroadcastToSignalRAttribute"/>
/// to SignalR clients.
/// </summary>
/// <remarks>
/// This service is automatically invoked after notifications are published when
/// <see cref="SignalROptions.EnableNotificationBroadcast"/> is enabled.
/// </remarks>
public interface ISignalRNotificationBroadcaster
{
    /// <summary>
    /// Broadcasts a notification to SignalR clients if it has the <see cref="BroadcastToSignalRAttribute"/>.
    /// </summary>
    /// <typeparam name="TNotification">The type of notification.</typeparam>
    /// <param name="notification">The notification to broadcast.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// A notification that declares no targets is sent to every client only when its type is
    /// registered with <see cref="SignalROptions.AllowBroadcastToAll{TNotification}"/>; otherwise, or
    /// when its declared targets resolve to no user or group, nothing is sent and an error is logged.
    /// </remarks>
    Task BroadcastAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : notnull;
}

/// <summary>
/// Default implementation of <see cref="ISignalRNotificationBroadcaster"/>.
/// </summary>
public sealed class SignalRNotificationBroadcaster : ISignalRNotificationBroadcaster
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SignalROptions _options;
    private readonly ILogger<SignalRNotificationBroadcaster> _logger;

    private static readonly ConcurrentDictionary<Type, BroadcastToSignalRAttribute?> AttributeCache = new();
    private static readonly ConcurrentDictionary<Type, PropertyInfo?> ConditionalPropertyCache = new();
    private static readonly Regex PlaceholderRegex = new(@"\{(\w+)\}", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    /// <summary>
    /// Initializes a new instance of the <see cref="SignalRNotificationBroadcaster"/> class.
    /// </summary>
    public SignalRNotificationBroadcaster(
        IServiceProvider serviceProvider,
        IOptions<SignalROptions> options,
        ILogger<SignalRNotificationBroadcaster> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task BroadcastAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : notnull
    {
        var attribute = GetAttributeToBroadcast(notification);
        if (attribute is null)
        {
            return;
        }

        var notificationType = typeof(TNotification);
        var methodName = attribute.Method ?? GetDefaultMethodName(notificationType);

        try
        {
            if (ResolveClients(attribute, notification) is not { } clients)
            {
                return;
            }

            var payload = JsonSerializer.Serialize(notification, _options.JsonSerializerOptions);

            await clients.SendAsync(methodName, payload, cancellationToken);

            Log.BroadcastNotification(_logger, notificationType.Name, methodName);
        }
        catch (Exception ex)
        {
            Log.FailedToBroadcastNotification(_logger, ex.ForLogging(), notificationType.Name);
        }
    }

    // The attribute when broadcasting is enabled and the conditional property allows it; otherwise null.
    private BroadcastToSignalRAttribute? GetAttributeToBroadcast<TNotification>(TNotification notification)
        where TNotification : notnull
    {
        if (!_options.EnableNotificationBroadcast
            || GetBroadcastAttribute(typeof(TNotification)) is not { } attribute)
        {
            return null;
        }

        if (!ShouldBroadcast(notification, attribute))
        {
            Log.NotificationSkippedConditional(_logger, typeof(TNotification).Name);
            return null;
        }

        return attribute;
    }

    // The audience to send to, or null (logged) when there is no hub context or the broadcast is refused.
    private IClientProxy? ResolveClients<TNotification>(BroadcastToSignalRAttribute attribute, TNotification notification)
        where TNotification : notnull
    {
        if (_serviceProvider.GetService(typeof(IHubContext<Hub>)) is not IHubContext<Hub> hubContext)
        {
            Log.NoHubContextAvailable(_logger, typeof(TNotification).Name);
            return null;
        }

        var clients = SelectClients(hubContext, attribute, notification);
        if (clients is null)
        {
            // Refused, never widened to every client: nothing is sent and success is not logged.
            Log.BroadcastRefusedNoTargets(_logger, typeof(TNotification).FullName!);
        }

        return clients;
    }

    // Users win over groups. Declared targets that resolve to none are refused (null); no declared
    // targets reach everyone only when the host registered the type with AllowBroadcastToAll.
    private IClientProxy? SelectClients<TNotification>(
        IHubContext<Hub> hubContext,
        BroadcastToSignalRAttribute attribute,
        TNotification notification)
        where TNotification : notnull
    {
        if (!string.IsNullOrWhiteSpace(attribute.TargetUsers))
        {
            var users = ResolveTargets(attribute.TargetUsers, notification);
            return users.Length == 0 ? null : hubContext.Clients.Users(users);
        }

        if (!string.IsNullOrWhiteSpace(attribute.TargetGroups))
        {
            var groups = ResolveTargets(attribute.TargetGroups, notification);
            return groups.Length == 0 ? null : hubContext.Clients.Groups(groups);
        }

        return _options.IsBroadcastToAllAllowed(typeof(TNotification)) ? hubContext.Clients.All : null;
    }

    private string[] ResolveTargets<TNotification>(string template, TNotification notification)
        where TNotification : notnull
        => ResolvePlaceholders(template, notification)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static BroadcastToSignalRAttribute? GetBroadcastAttribute(Type notificationType)
    {
        return AttributeCache.GetOrAdd(notificationType, type =>
            type.GetCustomAttribute<BroadcastToSignalRAttribute>());
    }

    private bool ShouldBroadcast<TNotification>(TNotification notification, BroadcastToSignalRAttribute attribute)
        where TNotification : notnull
    {
        if (string.IsNullOrWhiteSpace(attribute.ConditionalProperty))
        {
            return true;
        }

        var notificationType = typeof(TNotification);
        var property = ConditionalPropertyCache.GetOrAdd(notificationType, type =>
            type.GetProperty(attribute.ConditionalProperty, BindingFlags.Public | BindingFlags.Instance));

        if (property == null || property.PropertyType != typeof(bool))
        {
            Log.ConditionalPropertyNotFound(_logger, attribute.ConditionalProperty, notificationType.Name);
            return true;
        }

        return (bool)(property.GetValue(notification) ?? true);
    }

    private static string GetDefaultMethodName(Type notificationType)
    {
        var name = notificationType.Name;
        if (name.EndsWith("Notification", StringComparison.OrdinalIgnoreCase))
        {
            name = name[..^"Notification".Length];
        }

        return name;
    }

    private string ResolvePlaceholders<TNotification>(string template, TNotification notification)
        where TNotification : notnull
    {
        return PlaceholderRegex.Replace(template, match =>
        {
            var propertyName = match.Groups[1].Value;
            var property = typeof(TNotification).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

            if (property == null)
            {
                Log.PropertyNotFound(_logger, propertyName, typeof(TNotification).Name);
                return match.Value;
            }

            var value = property.GetValue(notification);
            return value?.ToString() ?? string.Empty;
        });
    }
}
