using Encina.SignalR;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.SignalR.Tests.Integration;

/// <summary>
/// Property-based tests for <see cref="SignalRNotificationBroadcaster"/> invariants.
/// </summary>
[Trait("Category", "Property")]
[Trait("Service", "SignalR")]
public sealed class SignalRBroadcasterPropertyTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
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

        _serviceProvider = services.BuildServiceProvider();
        _broadcaster = _serviceProvider.GetRequiredService<ISignalRNotificationBroadcaster>();
    }

    [Property(MaxTest = 20)]
    public bool BroadcastAsync_NeverThrows_WithValidNotification(PositiveInt seed)
    {
        var notification = new PropertyBroadcastNotification($"test-data-{seed.Get}");

        try
        {
            _broadcaster.BroadcastAsync(notification).GetAwaiter().GetResult();
            return true;
        }
        catch
        {
            return false;
        }
    }

    [Property(MaxTest = 20)]
    public bool BroadcastAsync_NeverThrows_WithPlainNotification(PositiveInt seed)
    {
        var notification = new PropertyPlainNotification($"plain-data-{seed.Get}");

        try
        {
            _broadcaster.BroadcastAsync(notification).GetAwaiter().GetResult();
            return true;
        }
        catch
        {
            return false;
        }
    }

    [Property(MaxTest = 20)]
    public bool BroadcastAsync_NeverThrows_WithConditionalNotification(PositiveInt seed, bool shouldBroadcast)
    {
        var notification = new PropertyConditionalNotification($"conditional-{seed.Get}", shouldBroadcast);

        try
        {
            _broadcaster.BroadcastAsync(notification).GetAwaiter().GetResult();
            return true;
        }
        catch
        {
            return false;
        }
    }

    [Property(MaxTest = 20)]
    public bool BroadcastAsync_NeverThrows_WithTargetedUserNotification(PositiveInt userId, PositiveInt messageId)
    {
        var notification = new PropertyTargetedUserNotification($"user-{userId.Get}", $"message-{messageId.Get}");

        try
        {
            _broadcaster.BroadcastAsync(notification).GetAwaiter().GetResult();
            return true;
        }
        catch
        {
            return false;
        }
    }

    [Property(MaxTest = 20)]
    public bool BroadcastAsync_NeverThrows_WithTargetedGroupNotification(PositiveInt groupId, PositiveInt messageId)
    {
        var notification = new PropertyTargetedGroupNotification($"group-{groupId.Get}", $"message-{messageId.Get}");

        try
        {
            _broadcaster.BroadcastAsync(notification).GetAwaiter().GetResult();
            return true;
        }
        catch
        {
            return false;
        }
    }

    [Fact]
    public void SignalROptions_EnableNotificationBroadcast_DefaultsToTrue()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddDebug());
        services.AddEncinaSignalR();

        using var sp = services.BuildServiceProvider();

        // Act
        var options = sp.GetRequiredService<IOptions<SignalROptions>>().Value;

        // Assert
        Assert.True(options.EnableNotificationBroadcast);
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }
}

// Test types for property tests

/// <summary>
/// Plain notification for property tests (no attribute).
/// </summary>
public sealed record PropertyPlainNotification(string Data) : INotification;

/// <summary>
/// Broadcast notification for property tests.
/// </summary>
[BroadcastToSignalR(Method = "PropertyBroadcast")]
public sealed record PropertyBroadcastNotification(string Data) : INotification;

/// <summary>
/// Conditional notification for property tests.
/// </summary>
[BroadcastToSignalR(Method = "PropertyConditional", ConditionalProperty = "ShouldBroadcast")]
public sealed record PropertyConditionalNotification(string Data, bool ShouldBroadcast) : INotification;

/// <summary>
/// Targeted user notification for property tests.
/// </summary>
[BroadcastToSignalR(Method = "PropertyUserMessage", TargetUsers = "{UserId}")]
public sealed record PropertyTargetedUserNotification(string UserId, string Message) : INotification;

/// <summary>
/// Targeted group notification for property tests.
/// </summary>
[BroadcastToSignalR(Method = "PropertyGroupMessage", TargetGroups = "{GroupId}")]
public sealed record PropertyTargetedGroupNotification(string GroupId, string Message) : INotification;
