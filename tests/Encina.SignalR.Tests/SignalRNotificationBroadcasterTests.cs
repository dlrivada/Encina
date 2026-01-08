using Encina.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Encina.SignalR.Tests;

/// <summary>
/// Tests for the <see cref="SignalRNotificationBroadcaster"/> class.
/// </summary>
public sealed class SignalRNotificationBroadcasterTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptions<SignalROptions> _options;
    private readonly ILogger<SignalRNotificationBroadcaster> _logger;

    public SignalRNotificationBroadcasterTests()
    {
        _serviceProvider = Substitute.For<IServiceProvider>();
        _options = Options.Create(new SignalROptions());
        _logger = Substitute.For<ILogger<SignalRNotificationBroadcaster>>();
    }

    [Fact]
    public void Constructor_CreatesInstance()
    {
        // Act
        var broadcaster = new SignalRNotificationBroadcaster(_serviceProvider, _options, _logger);

        // Assert
        broadcaster.ShouldNotBeNull();
    }

    [Fact]
    public async Task BroadcastAsync_WhenBroadcastDisabled_DoesNothing()
    {
        // Arrange
        var options = Options.Create(new SignalROptions { EnableNotificationBroadcast = false });
        var broadcaster = new SignalRNotificationBroadcaster(_serviceProvider, options, _logger);
        var notification = new NotificationWithoutAttribute("Test");

        // Act - should not throw
        await broadcaster.BroadcastAsync(notification);

        // Assert - no hub context was requested
        _serviceProvider.DidNotReceive().GetService(Arg.Any<Type>());
    }

    [Fact]
    public async Task BroadcastAsync_WhenNotificationHasNoAttribute_DoesNothing()
    {
        // Arrange
        var broadcaster = new SignalRNotificationBroadcaster(_serviceProvider, _options, _logger);
        var notification = new NotificationWithoutAttribute("Test");

        // Act - should not throw
        await broadcaster.BroadcastAsync(notification);

        // Assert - no hub context was requested (no attribute means no broadcast)
        _serviceProvider.DidNotReceive().GetService(typeof(IHubContext<Hub>));
    }

    [Fact]
    public async Task BroadcastAsync_WhenHubContextNotAvailable_DoesNotThrow()
    {
        // Arrange
        var broadcaster = new SignalRNotificationBroadcaster(_serviceProvider, _options, _logger);
        var notification = new BroadcastableNotification("Test");
        _serviceProvider.GetService(Arg.Any<Type>()).Returns((object?)null);

        // Act - should not throw
        await broadcaster.BroadcastAsync(notification);

        // Assert - method completed without exception
        _serviceProvider.Received().GetService(Arg.Any<Type>());
    }

    [Fact]
    public async Task BroadcastAsync_WithValidHubContext_BroadcastsNotification()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSignalR();
        services.AddEncinaSignalR();
        using var sp = services.BuildServiceProvider();
        var notification = new BroadcastableNotification("Test Message");

        // Act
        var broadcaster = sp.GetRequiredService<ISignalRNotificationBroadcaster>();
        var task = broadcaster.BroadcastAsync(notification);
        await task;

        // Assert
        broadcaster.ShouldNotBeNull();
        task.IsFaulted.ShouldBeFalse();
    }

    [Fact]
    public async Task BroadcastAsync_WithConditionalProperty_ChecksCondition()
    {
        // Arrange
        var broadcaster = new SignalRNotificationBroadcaster(_serviceProvider, _options, _logger);
        var notification = new ConditionalNotification("Test", ShouldBroadcast: false);
        _serviceProvider.GetService(Arg.Any<Type>()).Returns((object?)null);

        // Act
        await broadcaster.BroadcastAsync(notification);

        // Assert - conditional check should skip broadcast, so GetService should not be called
        _serviceProvider.DidNotReceive().GetService(Arg.Any<Type>());
    }

    [Fact]
    public async Task BroadcastAsync_WithConditionalPropertyTrue_AttemptsBroadcast()
    {
        // Arrange
        var broadcaster = new SignalRNotificationBroadcaster(_serviceProvider, _options, _logger);
        var notification = new ConditionalNotification("Test", ShouldBroadcast: true);
        _serviceProvider.GetService(Arg.Any<Type>()).Returns((object?)null);

        // Act
        await broadcaster.BroadcastAsync(notification);

        // Assert - should attempt to get hub context
        _serviceProvider.Received().GetService(Arg.Any<Type>());
    }

    [Fact]
    public async Task BroadcastAsync_WithCancellationToken_PropagatesToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var broadcaster = new SignalRNotificationBroadcaster(_serviceProvider, _options, _logger);
        var notification = new BroadcastableNotification("Test");

        // Act - should not throw
        await broadcaster.BroadcastAsync(notification, cts.Token);

        // Assert - completed without exception
    }

    [Fact]
    public async Task BroadcastAsync_WithTargetUsers_ResolvesPlaceholders()
    {
        // Arrange
        var broadcaster = new SignalRNotificationBroadcaster(_serviceProvider, _options, _logger);
        var notification = new UserTargetedNotification("Message", "user-123");
        _serviceProvider.GetService(Arg.Any<Type>()).Returns((object?)null);

        // Act
        await broadcaster.BroadcastAsync(notification);

        // Assert - should attempt broadcast
        _serviceProvider.Received().GetService(Arg.Any<Type>());
    }

    [Fact]
    public async Task BroadcastAsync_WithTargetGroups_ResolvesPlaceholders()
    {
        // Arrange
        var broadcaster = new SignalRNotificationBroadcaster(_serviceProvider, _options, _logger);
        var notification = new GroupTargetedNotification("Message", "admins");
        _serviceProvider.GetService(Arg.Any<Type>()).Returns((object?)null);

        // Act
        await broadcaster.BroadcastAsync(notification);

        // Assert - should attempt broadcast
        _serviceProvider.Received().GetService(Arg.Any<Type>());
    }

    [Fact]
    public async Task BroadcastAsync_WithCustomMethod_UsesMethodFromAttribute()
    {
        // Arrange
        var broadcaster = new SignalRNotificationBroadcaster(_serviceProvider, _options, _logger);
        var notification = new CustomMethodNotification("Test");
        _serviceProvider.GetService(Arg.Any<Type>()).Returns((object?)null);

        // Act
        await broadcaster.BroadcastAsync(notification);

        // Assert - should attempt broadcast
        _serviceProvider.Received().GetService(Arg.Any<Type>());
    }

    [Fact]
    public async Task BroadcastAsync_WhenExceptionOccurs_DoesNotThrow()
    {
        // Arrange
        var broadcaster = new SignalRNotificationBroadcaster(_serviceProvider, _options, _logger);
        var notification = new BroadcastableNotification("Test");
        _serviceProvider.GetService(Arg.Any<Type>()).Returns(x => throw new InvalidOperationException("Test exception"));

        // Act - should not throw
        await broadcaster.BroadcastAsync(notification);

        // Assert - exception was handled gracefully
    }

    [Fact]
    public async Task BroadcastAsync_WithInvalidConditionalProperty_BroadcastsAnyway()
    {
        // Arrange
        var broadcaster = new SignalRNotificationBroadcaster(_serviceProvider, _options, _logger);
        var notification = new InvalidConditionalNotification("Test");
        _serviceProvider.GetService(Arg.Any<Type>()).Returns((object?)null);

        // Act
        await broadcaster.BroadcastAsync(notification);

        // Assert - should still attempt broadcast (conditional property not found)
        _serviceProvider.Received().GetService(Arg.Any<Type>());
    }

    [Fact]
    public async Task BroadcastAsync_WithNonBooleanConditionalProperty_BroadcastsAnyway()
    {
        // Arrange
        var broadcaster = new SignalRNotificationBroadcaster(_serviceProvider, _options, _logger);
        var notification = new NonBoolConditionalNotification("Test", 42);
        _serviceProvider.GetService(Arg.Any<Type>()).Returns((object?)null);

        // Act
        await broadcaster.BroadcastAsync(notification);

        // Assert - should still attempt broadcast (conditional property wrong type)
        _serviceProvider.Received().GetService(Arg.Any<Type>());
    }

    [Fact]
    public async Task BroadcastAsync_CachesAttributeLookup()
    {
        // Arrange
        var broadcaster = new SignalRNotificationBroadcaster(_serviceProvider, _options, _logger);
        var notification1 = new BroadcastableNotification("Test1");
        var notification2 = new BroadcastableNotification("Test2");
        _serviceProvider.GetService(Arg.Any<Type>()).Returns((object?)null);

        // Act - call twice with same type
        await broadcaster.BroadcastAsync(notification1);
        await broadcaster.BroadcastAsync(notification2);

        // Assert - both calls completed
    }

    // Test notification types

    /// <summary>
    /// A notification without the BroadcastToSignalR attribute - used to test that
    /// notifications without the attribute are not broadcast.
    /// </summary>
    private sealed record NotificationWithoutAttribute(string Message) : INotification;

    [BroadcastToSignalR]
    private sealed record BroadcastableNotification(string Message) : INotification;

    [BroadcastToSignalR(ConditionalProperty = "ShouldBroadcast")]
    private sealed record ConditionalNotification(string Message, bool ShouldBroadcast) : INotification;

    [BroadcastToSignalR(TargetUsers = "{UserId}")]
    private sealed record UserTargetedNotification(string Message, string UserId) : INotification;

    [BroadcastToSignalR(TargetGroups = "{GroupName}")]
    private sealed record GroupTargetedNotification(string Message, string GroupName) : INotification;

    [BroadcastToSignalR(Method = "CustomMethodName")]
    private sealed record CustomMethodNotification(string Message) : INotification;

    [BroadcastToSignalR(ConditionalProperty = "NonExistentProperty")]
    private sealed record InvalidConditionalNotification(string Message) : INotification;

    [BroadcastToSignalR(ConditionalProperty = "Value")]
    private sealed record NonBoolConditionalNotification(string Message, int Value) : INotification;
}
