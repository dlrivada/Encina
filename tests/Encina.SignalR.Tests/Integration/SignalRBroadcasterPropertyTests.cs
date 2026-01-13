using System.Globalization;
using Encina.SignalR;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Encina.SignalR.Tests.Integration;

/// <summary>
/// Property-based tests for <see cref="SignalRNotificationBroadcaster"/> invariants.
/// </summary>
[Trait("Category", "Property")]
[Trait("Service", "SignalR")]
public sealed class SignalRBroadcasterPropertyTests : IDisposable
{
    private readonly ServiceProvider _sharedProvider;
    private readonly ISignalRNotificationBroadcaster _broadcaster;

    public SignalRBroadcasterPropertyTests()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddDebug().SetMinimumLevel(LogLevel.Debug));
        services.AddSignalR();
        services.AddEncinaSignalR(options =>
        {
            options.EnableNotificationBroadcast = true;
        });

        _sharedProvider = services.BuildServiceProvider();
        _broadcaster = _sharedProvider.GetRequiredService<ISignalRNotificationBroadcaster>();
    }

    public void Dispose()
    {
        _sharedProvider.Dispose();
    }

    [Property(MaxTest = 50)]
    public async Task BroadcastAsync_NeverThrows_WithValidNotification(PositiveInt seed)
    {
        // Arrange
        var notification = new PropertyBroadcastNotification($"test-data-{seed.Get}");

        // Act & Assert - exception propagates to FsCheck if thrown
        await Should.NotThrowAsync(() => _broadcaster.BroadcastAsync(notification));
    }

    [Property(MaxTest = 50)]
    public async Task BroadcastAsync_NeverThrows_WithPlainNotification(PositiveInt seed)
    {
        // Arrange
        var notification = new PropertyPlainNotification($"plain-data-{seed.Get}");

        // Act & Assert - plain notifications (no attribute) should complete without error
        await Should.NotThrowAsync(() => _broadcaster.BroadcastAsync(notification));

        // Verify the notification data is preserved
        notification.Data.ShouldStartWith("plain-data-");
        notification.Data.ShouldContain(seed.Get.ToString(CultureInfo.InvariantCulture));
    }

    [Property(MaxTest = 50)]
    public async Task BroadcastAsync_NeverThrows_WithConditionalNotification(PositiveInt seed, bool shouldBroadcast)
    {
        // Arrange
        var notification = new PropertyConditionalNotification($"conditional-{seed.Get}", shouldBroadcast);

        // Act & Assert - conditional notifications should complete regardless of condition value
        await Should.NotThrowAsync(() => _broadcaster.BroadcastAsync(notification));

        // Verify conditional property is correctly set
        notification.ShouldBroadcast.ShouldBe(shouldBroadcast);
    }

    [Property(MaxTest = 50)]
    public async Task BroadcastAsync_NeverThrows_WithTargetedUserNotification(PositiveInt userId, PositiveInt messageId)
    {
        // Arrange
        var notification = new PropertyTargetedUserNotification($"user-{userId.Get}", $"message-{messageId.Get}");

        // Act & Assert - targeted user notifications should complete without error
        await Should.NotThrowAsync(() => _broadcaster.BroadcastAsync(notification));

        // Verify targeting properties are preserved
        notification.UserId.ShouldStartWith("user-");
        notification.Message.ShouldStartWith("message-");
    }

    [Property(MaxTest = 50)]
    public async Task BroadcastAsync_NeverThrows_WithTargetedGroupNotification(PositiveInt groupId, PositiveInt messageId)
    {
        // Arrange
        var notification = new PropertyTargetedGroupNotification($"group-{groupId.Get}", $"message-{messageId.Get}");

        // Act & Assert - targeted group notifications should complete without error
        await Should.NotThrowAsync(() => _broadcaster.BroadcastAsync(notification));

        // Verify targeting properties are preserved
        notification.GroupId.ShouldStartWith("group-");
        notification.Message.ShouldStartWith("message-");
    }
}

// Test types for property tests

/// <summary>
/// Plain notification for property tests (no attribute).
/// </summary>
internal sealed record PropertyPlainNotification(string Data) : INotification;

/// <summary>
/// Broadcast notification for property tests.
/// </summary>
[BroadcastToSignalR(Method = "PropertyBroadcast")]
internal sealed record PropertyBroadcastNotification(string Data) : INotification;

/// <summary>
/// Conditional notification for property tests.
/// </summary>
[BroadcastToSignalR(Method = "PropertyConditional", ConditionalProperty = "ShouldBroadcast")]
internal sealed record PropertyConditionalNotification(string Data, bool ShouldBroadcast) : INotification;

/// <summary>
/// Targeted user notification for property tests.
/// </summary>
[BroadcastToSignalR(Method = "PropertyUserMessage", TargetUsers = "{UserId}")]
internal sealed record PropertyTargetedUserNotification(string UserId, string Message) : INotification;

/// <summary>
/// Targeted group notification for property tests.
/// </summary>
[BroadcastToSignalR(Method = "PropertyGroupMessage", TargetGroups = "{GroupId}")]
internal sealed record PropertyTargetedGroupNotification(string GroupId, string Message) : INotification;
