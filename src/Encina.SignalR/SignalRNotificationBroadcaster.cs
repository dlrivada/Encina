using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
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
        if (!_options.EnableNotificationBroadcast)
        {
            return;
        }

        var notificationType = typeof(TNotification);
        var attribute = GetBroadcastAttribute(notificationType);

        if (attribute == null)
        {
            return;
        }

        // Check conditional property
        if (!ShouldBroadcast(notification, attribute))
        {
            Log.NotificationSkippedConditional(_logger, notificationType.Name);
            return;
        }

        var methodName = attribute.Method ?? GetDefaultMethodName(notificationType);

        try
        {
            // Get the hub context - we use a generic approach to support any hub type
            var hubContextType = typeof(IHubContext<>).MakeGenericType(typeof(Hub));

            if (_serviceProvider.GetService(hubContextType) is not IHubContext<Hub> hubContext)
            {
                Log.NoHubContextAvailable(_logger, notificationType.Name);
                return;
            }

            var payload = JsonSerializer.Serialize(notification, _options.JsonSerializerOptions);

            // Determine target clients
            IClientProxy clients;

            if (!string.IsNullOrWhiteSpace(attribute.TargetUsers))
            {
                var userIds = ResolvePlaceholders(attribute.TargetUsers, notification)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                clients = hubContext.Clients.Users(userIds);
            }
            else if (!string.IsNullOrWhiteSpace(attribute.TargetGroups))
            {
                var groupNames = ResolvePlaceholders(attribute.TargetGroups, notification)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                clients = hubContext.Clients.Groups(groupNames);
            }
            else
            {
                clients = hubContext.Clients.All;
            }

            await clients.SendAsync(methodName, payload, cancellationToken);

            Log.BroadcastNotification(_logger, notificationType.Name, methodName);
        }
        catch (Exception ex)
        {
            Log.FailedToBroadcastNotification(_logger, ex, notificationType.Name);
        }
    }

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
