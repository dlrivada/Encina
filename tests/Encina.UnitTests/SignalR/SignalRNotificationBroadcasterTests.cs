using Encina.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Testing;

namespace Encina.UnitTests.SignalR;

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
        services.AddEncinaSignalR(o => o.AllowBroadcastToAll<BroadcastableNotification>());
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

    [Fact]
    public async Task BroadcastAsync_NoTargetsAndNoRegistration_SendsNothingAndLogsRefusal()
    {
        var hub = new FakeHub(_serviceProvider);
        var logger = new FakeLogger<SignalRNotificationBroadcaster>();
        var broadcaster = new SignalRNotificationBroadcaster(_serviceProvider, _options, logger);

        await broadcaster.BroadcastAsync(new BroadcastableNotification("secret"));

        hub.AssertNothingSent();
        var record = logger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Id.Id.ShouldBe(4614);
        record.Level.ShouldBe(LogLevel.Error);
        record.Message.ShouldContain(typeof(BroadcastableNotification).FullName!);
        record.Message.ShouldNotContain("secret");
    }

    [Fact]
    public async Task BroadcastAsync_WhitespaceOnlyTargetsAndNoRegistration_SendsNothingAndLogsRefusal()
    {
        var hub = new FakeHub(_serviceProvider);
        var logger = new FakeLogger<SignalRNotificationBroadcaster>();
        var broadcaster = new SignalRNotificationBroadcaster(_serviceProvider, _options, logger);

        await broadcaster.BroadcastAsync(new WhitespaceTargetsNotification("Test"));

        hub.AssertNothingSent();
        logger.Collector.GetSnapshot().ShouldHaveSingleItem().Id.Id.ShouldBe(4614);
    }

    [Fact]
    public async Task BroadcastAsync_WhitespaceOnlyTargetsWithRegistration_SendsToAllClients()
    {
        var hub = new FakeHub(_serviceProvider);
        var options = Options.Create(new SignalROptions().AllowBroadcastToAll<WhitespaceTargetsNotification>());
        var broadcaster = new SignalRNotificationBroadcaster(_serviceProvider, options, _logger);

        await broadcaster.BroadcastAsync(new WhitespaceTargetsNotification("Test"));

        await hub.All.Received(1).SendCoreAsync("WhitespaceTargets", Arg.Any<object?[]>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" , ,")]
    public async Task BroadcastAsync_UserTargetsResolvingToEmpty_SendsNothingAndLogsRefusal(string userId)
    {
        var hub = new FakeHub(_serviceProvider);
        var logger = new FakeLogger<SignalRNotificationBroadcaster>();
        var broadcaster = new SignalRNotificationBroadcaster(_serviceProvider, _options, logger);

        await broadcaster.BroadcastAsync(new UserTargetedNotification("Test", userId));

        hub.AssertNothingSent();
        logger.Collector.GetSnapshot().ShouldHaveSingleItem().Id.Id.ShouldBe(4614);
    }

    [Fact]
    public async Task BroadcastAsync_UserTargetsResolvingToEmpty_AreRefusedWithoutFallingBackToGroups()
    {
        var hub = new FakeHub(_serviceProvider);
        var broadcaster = new SignalRNotificationBroadcaster(_serviceProvider, _options, _logger);

        await broadcaster.BroadcastAsync(new UserAndGroupTargetedNotification(null!, "admins"));

        hub.AssertNothingSent();
        hub.Clients.DidNotReceive().Groups(Arg.Any<IReadOnlyList<string>>());
    }

    [Fact]
    public async Task BroadcastAsync_GroupTargetsResolvingToEmpty_IsRefusedEvenWhenRegisteredForAll()
    {
        var hub = new FakeHub(_serviceProvider);
        var logger = new FakeLogger<SignalRNotificationBroadcaster>();
        var options = Options.Create(new SignalROptions().AllowBroadcastToAll<GroupTargetedNotification>());
        var broadcaster = new SignalRNotificationBroadcaster(_serviceProvider, options, logger);

        await broadcaster.BroadcastAsync(new GroupTargetedNotification("Test", null!));

        hub.AssertNothingSent();
        logger.Collector.GetSnapshot().ShouldHaveSingleItem().Id.Id.ShouldBe(4614);
    }

    [Fact]
    public async Task BroadcastAsync_NoTargetsWithRegistration_SendsPayloadToAllClients()
    {
        var hub = new FakeHub(_serviceProvider);
        var logger = new FakeLogger<SignalRNotificationBroadcaster>();
        var options = Options.Create(new SignalROptions().AllowBroadcastToAll<BroadcastableNotification>());
        var broadcaster = new SignalRNotificationBroadcaster(_serviceProvider, options, logger);

        await broadcaster.BroadcastAsync(new BroadcastableNotification("hello"));

        await hub.All.Received(1).SendCoreAsync(
            "Broadcastable",
            Arg.Is<object?[]>(args => args.Length == 1 && ((string)args[0]!).Contains("hello")),
            Arg.Any<CancellationToken>());
        hub.Clients.DidNotReceive().Users(Arg.Any<IReadOnlyList<string>>());
        hub.Clients.DidNotReceive().Groups(Arg.Any<IReadOnlyList<string>>());
        logger.Collector.GetSnapshot().ShouldHaveSingleItem().Id.Id.ShouldBe(4602);
    }

    [Fact]
    public async Task BroadcastAsync_RegistrationOfAnotherType_DoesNotOpenBroadcastToAll()
    {
        var hub = new FakeHub(_serviceProvider);
        var options = Options.Create(new SignalROptions()
            .AllowBroadcastToAll<CustomMethodNotification>()
            .AllowNotification<BroadcastableNotification>());
        var broadcaster = new SignalRNotificationBroadcaster(_serviceProvider, options, _logger);

        await broadcaster.BroadcastAsync(new BroadcastableNotification("Test"));

        hub.AssertNothingSent();
    }

    [Fact]
    public async Task BroadcastAsync_UserTargets_SendToResolvedUsersOnly()
    {
        var hub = new FakeHub(_serviceProvider);
        var broadcaster = new SignalRNotificationBroadcaster(_serviceProvider, _options, _logger);

        await broadcaster.BroadcastAsync(new UserTargetedNotification("Test", "user-1, user-2"));

        hub.Clients.Received(1).Users(Arg.Is<IReadOnlyList<string>>(u => string.Join('|', u) == "user-1|user-2"));
        await hub.Users.Received(1).SendCoreAsync("UserTargeted", Arg.Any<object?[]>(), Arg.Any<CancellationToken>());
        await hub.All.DidNotReceiveWithAnyArgs().SendCoreAsync(default!, default!, default);
    }

    [Fact]
    public async Task BroadcastAsync_GroupTargets_SendToResolvedGroupsOnly()
    {
        var hub = new FakeHub(_serviceProvider);
        var broadcaster = new SignalRNotificationBroadcaster(_serviceProvider, _options, _logger);

        await broadcaster.BroadcastAsync(new GroupTargetedNotification("Test", "admins"));

        hub.Clients.Received(1).Groups(Arg.Is<IReadOnlyList<string>>(g => string.Join('|', g) == "admins"));
        await hub.Groups.Received(1).SendCoreAsync("GroupTargeted", Arg.Any<object?[]>(), Arg.Any<CancellationToken>());
        await hub.All.DidNotReceiveWithAnyArgs().SendCoreAsync(default!, default!, default);
    }

    [Fact]
    public void AllowBroadcastToAll_DuplicateRegistration_Throws()
    {
        var options = new SignalROptions().AllowBroadcastToAll<BroadcastableNotification>();

        Should.Throw<InvalidOperationException>(() => options.AllowBroadcastToAll(typeof(BroadcastableNotification)))
            .Message.ShouldContain(typeof(BroadcastableNotification).FullName!);
    }

    [Theory]
    [InlineData(typeof(INotification))]
    [InlineData(typeof(GenericNotification<>))]
    [InlineData(typeof(string))]
    public void AllowBroadcastToAll_InvalidType_ThrowsArgumentException(Type type)
    {
        Should.Throw<ArgumentException>(() => new SignalROptions().AllowBroadcastToAll(type))
            .ParamName.ShouldBe("notificationType");
    }

    [Fact]
    public void AllowBroadcastToAll_NullType_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new SignalROptions().AllowBroadcastToAll(null!));
    }

    [Fact]
    public void AllowBroadcastToAll_AfterFreeze_Throws_AndAllowNotificationToo()
    {
        var options = new SignalROptions().AllowBroadcastToAll<BroadcastableNotification>();
        options.IsBroadcastToAllAllowed(typeof(BroadcastableNotification)).ShouldBeTrue();

        Should.Throw<InvalidOperationException>(() => options.AllowBroadcastToAll<CustomMethodNotification>());
        Should.Throw<InvalidOperationException>(() => options.AllowNotification<CustomMethodNotification>());
        options.IsBroadcastToAllAllowed(typeof(CustomMethodNotification)).ShouldBeFalse();
        options.GetAllowList().ShouldBeEmpty();
    }

    // A substitute hub context whose All, Users and Groups proxies are distinct, so a test can prove
    // which audience received the send.
    private sealed class FakeHub
    {
        public FakeHub(IServiceProvider serviceProvider)
        {
            var context = Substitute.For<IHubContext<Hub>>();
            context.Clients.Returns(Clients);
            Clients.All.Returns(All);
            Clients.Users(Arg.Any<IReadOnlyList<string>>()).Returns(Users);
            Clients.Groups(Arg.Any<IReadOnlyList<string>>()).Returns(Groups);
            serviceProvider.GetService(typeof(IHubContext<Hub>)).Returns(context);
        }

        public IHubClients Clients { get; } = Substitute.For<IHubClients>();

        public IClientProxy All { get; } = Substitute.For<IClientProxy>();

        public IClientProxy Users { get; } = Substitute.For<IClientProxy>();

        public IClientProxy Groups { get; } = Substitute.For<IClientProxy>();

        public void AssertNothingSent()
        {
            All.ReceivedCalls().ShouldBeEmpty();
            Users.ReceivedCalls().ShouldBeEmpty();
            Groups.ReceivedCalls().ShouldBeEmpty();
        }
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

    [BroadcastToSignalR(TargetUsers = "   ", TargetGroups = " ")]
    private sealed record WhitespaceTargetsNotification(string Message) : INotification;

    [BroadcastToSignalR(TargetUsers = "{UserId}", TargetGroups = "{GroupName}")]
    private sealed record UserAndGroupTargetedNotification(string UserId, string GroupName) : INotification;

    private sealed record GenericNotification<T>(T Value) : INotification;
}
