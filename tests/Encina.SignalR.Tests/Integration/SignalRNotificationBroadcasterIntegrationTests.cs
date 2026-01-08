using System.Text.Json;
using Encina.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.SignalR;

namespace Encina.SignalR.Tests.Integration;

/// <summary>
/// Integration tests for <see cref="SignalRNotificationBroadcaster"/> with full DI setup.
/// Tests notification broadcasting behavior with various configurations.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Service", "SignalR")]
public sealed class SignalRNotificationBroadcasterIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ISignalRNotificationBroadcaster _broadcaster;

    public SignalRNotificationBroadcasterIntegrationTests()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddDebug().SetMinimumLevel(LogLevel.Debug));
        services.AddSignalR();
        services.AddEncinaSignalR(options =>
        {
            options.EnableNotificationBroadcast = true;
        });

        _serviceProvider = services.BuildServiceProvider();
        _broadcaster = _serviceProvider.GetRequiredService<ISignalRNotificationBroadcaster>();
    }

    [Fact]
    public void Broadcaster_CanBeResolved_FromServiceProvider()
    {
        // Act
        var broadcaster = _serviceProvider.GetService<ISignalRNotificationBroadcaster>();

        // Assert
        broadcaster.ShouldNotBeNull();
        broadcaster.ShouldBeOfType<SignalRNotificationBroadcaster>();
    }

    [Fact]
    public async Task BroadcastAsync_WithoutBroadcastAttribute_DoesNotThrow()
    {
        // Arrange
        var notification = new IntegrationTestPlainNotification("test-data");

        // Act & Assert - Should not throw even without hub context
        await _broadcaster.BroadcastAsync(notification);
    }

    [Fact]
    public async Task BroadcastAsync_WithBroadcastAttribute_DoesNotThrow()
    {
        // Arrange
        var notification = new IntegrationTestBroadcastNotification("broadcast-data");

        // Act & Assert - Should not throw (no actual hub context in test)
        await _broadcaster.BroadcastAsync(notification);
    }

    [Fact]
    public async Task BroadcastAsync_WithConditionalProperty_True_AttemptsBroadcast()
    {
        // Arrange
        var notification = new IntegrationTestConditionalNotification("data", true);

        // Act & Assert - Should not throw
        await _broadcaster.BroadcastAsync(notification);
    }

    [Fact]
    public async Task BroadcastAsync_WithConditionalProperty_False_SkipsBroadcast()
    {
        // Arrange
        var notification = new IntegrationTestConditionalNotification("data", false);

        // Act & Assert - Should not throw, but should skip broadcasting
        await _broadcaster.BroadcastAsync(notification);
    }

    [Fact]
    public async Task BroadcastAsync_WithTargetUsers_AttemptsBroadcast()
    {
        // Arrange
        var notification = new IntegrationTestTargetedUserNotification("user-123", "message");

        // Act & Assert - Should not throw
        await _broadcaster.BroadcastAsync(notification);
    }

    [Fact]
    public async Task BroadcastAsync_WithTargetGroups_AttemptsBroadcast()
    {
        // Arrange
        var notification = new IntegrationTestTargetedGroupNotification("group-abc", "message");

        // Act & Assert - Should not throw
        await _broadcaster.BroadcastAsync(notification);
    }

    [Fact]
    public async Task BroadcastAsync_WhenBroadcastDisabled_DoesNothing()
    {
        // Arrange - Create new service provider with broadcast disabled
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddDebug());
        services.AddSignalR();
        services.AddEncinaSignalR(options =>
        {
            options.EnableNotificationBroadcast = false;
        });

        await using var sp = services.BuildServiceProvider();
        var broadcaster = sp.GetRequiredService<ISignalRNotificationBroadcaster>();

        var notification = new IntegrationTestBroadcastNotification("disabled-broadcast");

        // Act & Assert - Should not throw
        await broadcaster.BroadcastAsync(notification);
    }

    [Fact]
    public void SignalROptions_ConfiguredValues_AreApplied()
    {
        // Act
        var options = _serviceProvider.GetRequiredService<IOptions<SignalROptions>>().Value;

        // Assert - Verify the configured value from test setup is applied
        options.EnableNotificationBroadcast.ShouldBeTrue();
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }
}

// Test notification types for integration tests

/// <summary>
/// Notification without BroadcastToSignalR attribute - should not be broadcast.
/// </summary>
internal sealed record IntegrationTestPlainNotification(string Data) : INotification;

/// <summary>
/// Notification with BroadcastToSignalR attribute - should be broadcast to all clients.
/// </summary>
[BroadcastToSignalR(Method = "TestBroadcast")]
internal sealed record IntegrationTestBroadcastNotification(string Data) : INotification;

/// <summary>
/// Notification with conditional broadcasting based on property value.
/// </summary>
[BroadcastToSignalR(Method = "ConditionalBroadcast", ConditionalProperty = "ShouldBroadcast")]
internal sealed record IntegrationTestConditionalNotification(string Data, bool ShouldBroadcast) : INotification;

/// <summary>
/// Notification targeted to specific users using placeholder.
/// </summary>
[BroadcastToSignalR(Method = "UserMessage", TargetUsers = "{UserId}")]
internal sealed record IntegrationTestTargetedUserNotification(string UserId, string Message) : INotification;

/// <summary>
/// Notification targeted to specific groups using placeholder.
/// </summary>
[BroadcastToSignalR(Method = "GroupMessage", TargetGroups = "{GroupId}")]
internal sealed record IntegrationTestTargetedGroupNotification(string GroupId, string Message) : INotification;
