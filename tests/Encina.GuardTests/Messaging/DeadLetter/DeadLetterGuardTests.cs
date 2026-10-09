using Encina.Messaging.DeadLetter;
using Encina.Messaging.Serialization;
using Shouldly;

namespace Encina.GuardTests.Messaging.DeadLetter;

/// <summary>
/// Guard clause tests for DeadLetterOrchestrator, DeadLetterManager, and DeadLetterCleanupProcessor.
/// </summary>
public class DeadLetterGuardTests
{
    #region DeadLetterOrchestrator Constructor

    [Fact]
    public void DeadLetterOrchestrator_NullStore_ThrowsArgumentNullException()
    {
        var act = () => new DeadLetterOrchestrator(
            null!,
            Substitute.For<IDeadLetterMessageFactory>(),
            new DeadLetterOptions(),
            NullLogger<DeadLetterOrchestrator>.Instance,
            new JsonMessageSerializer(),
            Substitute.For<IRequestContextAccessor>());

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("store");
    }

    [Fact]
    public void DeadLetterOrchestrator_NullMessageFactory_ThrowsArgumentNullException()
    {
        var act = () => new DeadLetterOrchestrator(
            Substitute.For<IDeadLetterStore>(),
            null!,
            new DeadLetterOptions(),
            NullLogger<DeadLetterOrchestrator>.Instance,
            new JsonMessageSerializer(),
            Substitute.For<IRequestContextAccessor>());

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("messageFactory");
    }

    [Fact]
    public void DeadLetterOrchestrator_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new DeadLetterOrchestrator(
            Substitute.For<IDeadLetterStore>(),
            Substitute.For<IDeadLetterMessageFactory>(),
            null!,
            NullLogger<DeadLetterOrchestrator>.Instance,
            new JsonMessageSerializer(),
            Substitute.For<IRequestContextAccessor>());

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void DeadLetterOrchestrator_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DeadLetterOrchestrator(
            Substitute.For<IDeadLetterStore>(),
            Substitute.For<IDeadLetterMessageFactory>(),
            new DeadLetterOptions(),
            null!,
            new JsonMessageSerializer(),
            Substitute.For<IRequestContextAccessor>());

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    [Fact]
    public void DeadLetterOrchestrator_NullMessageSerializer_ThrowsArgumentNullException()
    {
        var act = () => new DeadLetterOrchestrator(
            Substitute.For<IDeadLetterStore>(),
            Substitute.For<IDeadLetterMessageFactory>(),
            new DeadLetterOptions(),
            NullLogger<DeadLetterOrchestrator>.Instance,
            null!,
            Substitute.For<IRequestContextAccessor>());

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("messageSerializer");
    }

    [Fact]
    public void DeadLetterOrchestrator_NullRequestContextAccessor_ThrowsArgumentNullException()
    {
        var act = () => new DeadLetterOrchestrator(
            Substitute.For<IDeadLetterStore>(),
            Substitute.For<IDeadLetterMessageFactory>(),
            new DeadLetterOptions(),
            NullLogger<DeadLetterOrchestrator>.Instance,
            new JsonMessageSerializer(),
            null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("requestContextAccessor");
    }

    #endregion

    #region DeadLetterOrchestrator.AddAsync

    [Fact]
    public async Task AddAsync_NullRequest_ThrowsArgumentNullException()
    {
        var orchestrator = CreateOrchestrator();
        var context = new DeadLetterContext(
            EncinaError.New("err"), null, "Outbox", 3, DateTime.UtcNow);

        var act = async () => await orchestrator.AddAsync<string>(null!, context);

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("request");
    }

    [Fact]
    public async Task AddAsync_NullContext_ThrowsArgumentNullException()
    {
        var orchestrator = CreateOrchestrator();

        var act = async () => await orchestrator.AddAsync("request", null!);

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("context");
    }

    [Fact]
    public async Task AddAsync_EmptySourcePattern_ThrowsArgumentException()
    {
        var orchestrator = CreateOrchestrator();
        var context = new DeadLetterContext(
            EncinaError.New("err"), null, "", 3, DateTime.UtcNow);

        var act = async () => await orchestrator.AddAsync("request", context);

        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("context.SourcePattern");
    }

    #endregion

    #region DeadLetterOrchestrator.AddFromFailedMessageAsync

    [Fact]
    public async Task AddFromFailedMessageAsync_NullFailedMessage_ThrowsArgumentNullException()
    {
        var orchestrator = CreateOrchestrator();

        var act = async () => await orchestrator.AddFromFailedMessageAsync(null!, "Outbox");

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("failedMessage");
    }

    [Fact]
    public async Task AddFromFailedMessageAsync_NullSourcePattern_ThrowsArgumentException()
    {
        var orchestrator = CreateOrchestrator();
        var failedMessage = new global::Encina.Messaging.Recoverability.FailedMessage
        {
            Id = Guid.NewGuid(),
            Request = "test",
            RequestType = "System.String",
            Error = EncinaError.New("err"),
            TotalAttempts = 1,
            ImmediateRetryAttempts = 1,
            DelayedRetryAttempts = 0,
            FirstAttemptAtUtc = DateTime.UtcNow,
            FailedAtUtc = DateTime.UtcNow
        };

        var act = async () => await orchestrator.AddFromFailedMessageAsync(failedMessage, null!);

        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("sourcePattern");
    }

    [Fact]
    public async Task AddFromFailedMessageAsync_EmptySourcePattern_ThrowsArgumentException()
    {
        var orchestrator = CreateOrchestrator();
        var failedMessage = new global::Encina.Messaging.Recoverability.FailedMessage
        {
            Id = Guid.NewGuid(),
            Request = "test",
            RequestType = "System.String",
            Error = EncinaError.New("err"),
            TotalAttempts = 1,
            ImmediateRetryAttempts = 1,
            DelayedRetryAttempts = 0,
            FirstAttemptAtUtc = DateTime.UtcNow,
            FailedAtUtc = DateTime.UtcNow
        };

        var act = async () => await orchestrator.AddFromFailedMessageAsync(failedMessage, "");

        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("sourcePattern");
    }

    #endregion

    #region DeadLetterManager Constructor

    [Fact]
    public void DeadLetterManager_NullStore_ThrowsArgumentNullException()
    {
        var act = () => new DeadLetterManager(
            null!,
            CreateOrchestrator(),
            Substitute.For<IServiceProvider>(),
            NullLogger<DeadLetterManager>.Instance,
            new JsonMessageSerializer(),
            new DeadLetterOptions(),
            Substitute.For<IRequestContextAccessor>());

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("store");
    }

    [Fact]
    public void DeadLetterManager_NullOrchestrator_ThrowsArgumentNullException()
    {
        var act = () => new DeadLetterManager(
            Substitute.For<IDeadLetterStore>(),
            null!,
            Substitute.For<IServiceProvider>(),
            NullLogger<DeadLetterManager>.Instance,
            new JsonMessageSerializer(),
            new DeadLetterOptions(),
            Substitute.For<IRequestContextAccessor>());

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("orchestrator");
    }

    [Fact]
    public void DeadLetterManager_NullServiceProvider_ThrowsArgumentNullException()
    {
        var act = () => new DeadLetterManager(
            Substitute.For<IDeadLetterStore>(),
            CreateOrchestrator(),
            null!,
            NullLogger<DeadLetterManager>.Instance,
            new JsonMessageSerializer(),
            new DeadLetterOptions(),
            Substitute.For<IRequestContextAccessor>());

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("serviceProvider");
    }

    [Fact]
    public void DeadLetterManager_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DeadLetterManager(
            Substitute.For<IDeadLetterStore>(),
            CreateOrchestrator(),
            Substitute.For<IServiceProvider>(),
            null!,
            new JsonMessageSerializer(),
            new DeadLetterOptions(),
            Substitute.For<IRequestContextAccessor>());

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    [Fact]
    public void DeadLetterManager_NullMessageSerializer_ThrowsArgumentNullException()
    {
        var act = () => new DeadLetterManager(
            Substitute.For<IDeadLetterStore>(),
            CreateOrchestrator(),
            Substitute.For<IServiceProvider>(),
            NullLogger<DeadLetterManager>.Instance,
            null!,
            new DeadLetterOptions(),
            Substitute.For<IRequestContextAccessor>());

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("messageSerializer");
    }

    [Fact]
    public void DeadLetterManager_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new DeadLetterManager(
            Substitute.For<IDeadLetterStore>(),
            CreateOrchestrator(),
            Substitute.For<IServiceProvider>(),
            NullLogger<DeadLetterManager>.Instance,
            new JsonMessageSerializer(),
            null!,
            Substitute.For<IRequestContextAccessor>());

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void DeadLetterManager_NullRequestContextAccessor_ThrowsArgumentNullException()
    {
        var act = () => new DeadLetterManager(
            Substitute.For<IDeadLetterStore>(),
            CreateOrchestrator(),
            Substitute.For<IServiceProvider>(),
            NullLogger<DeadLetterManager>.Instance,
            new JsonMessageSerializer(),
            new DeadLetterOptions(),
            null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("requestContextAccessor");
    }

    #endregion

    #region DeadLetterManager.ReplayAllAsync

    [Fact]
    public async Task ReplayAllAsync_NullFilter_ThrowsArgumentNullException()
    {
        var manager = CreateManager();

        var act = async () => await manager.ReplayAllAsync(null!);

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("filter");
    }

    [Fact]
    public async Task ReplayAllAsync_NonPositiveMaxMessages_ThrowsArgumentOutOfRangeException()
    {
        var manager = CreateManager();

        var act = async () => await manager.ReplayAllAsync(new DeadLetterFilter(), maxMessages: 0);

        await Should.ThrowAsync<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public async Task ReplayAllAsync_FilterInstantWithoutUtcKind_ThrowsArgumentException()
    {
        var manager = CreateManager();
        var filter = new DeadLetterFilter { DeadLetteredAfterUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified) };

        var act = async () => await manager.ReplayAllAsync(filter);

        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("DeadLetteredAfterUtc");
    }

    [Fact]
    public async Task GetMessagesAsync_FilterIdentityWithTrailingSpace_ThrowsArgumentException()
    {
        var manager = CreateManager();

        var act = async () => await manager.GetMessagesAsync(new DeadLetterFilter { SourceMessageId = "order-1 " });

        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("SourceMessageId");
    }

    #endregion

    #region DeadLetterManager.DeleteAllAsync

    [Fact]
    public async Task DeleteAllAsync_NullFilter_ThrowsArgumentNullException()
    {
        var manager = CreateManager();

        var act = async () => await manager.DeleteAllAsync(null!);

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("filter");
    }

    #endregion

    #region DeadLetterCleanupProcessor Constructor

    [Fact]
    public void DeadLetterCleanupProcessor_NullScopeFactory_ThrowsArgumentNullException()
    {
        var act = () => new DeadLetterCleanupProcessor(
            null!,
            new DeadLetterOptions(),
            NullLogger<DeadLetterCleanupProcessor>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("scopeFactory");
    }

    [Fact]
    public void DeadLetterCleanupProcessor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new DeadLetterCleanupProcessor(
            Substitute.For<Microsoft.Extensions.DependencyInjection.IServiceScopeFactory>(),
            null!,
            NullLogger<DeadLetterCleanupProcessor>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void DeadLetterCleanupProcessor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DeadLetterCleanupProcessor(
            Substitute.For<Microsoft.Extensions.DependencyInjection.IServiceScopeFactory>(),
            new DeadLetterOptions(),
            null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    #endregion

    #region DeadLetterOptions

    [Fact]
    public void DeadLetterOptions_NonPositiveRetentionPeriod_ThrowsArgumentOutOfRangeException()
    {
        var options = new DeadLetterOptions();

        Should.Throw<ArgumentOutOfRangeException>(() => options.RetentionPeriod = TimeSpan.Zero)
            .ParamName.ShouldBe("RetentionPeriod");
    }

    [Fact]
    public void DeadLetterOptions_NullRetentionPeriod_IsAllowed()
    {
        var options = new DeadLetterOptions { RetentionPeriod = null };

        options.RetentionPeriod.ShouldBeNull();
    }

    [Fact]
    public void DeadLetterOptions_NonPositiveCleanupInterval_ThrowsArgumentOutOfRangeException()
    {
        var options = new DeadLetterOptions();

        Should.Throw<ArgumentOutOfRangeException>(() => options.CleanupInterval = TimeSpan.FromSeconds(-1))
            .ParamName.ShouldBe("CleanupInterval");
    }

    [Fact]
    public void DeadLetterOptions_NonPositiveReplayClaimTimeout_ThrowsArgumentOutOfRangeException()
    {
        var options = new DeadLetterOptions();

        Should.Throw<ArgumentOutOfRangeException>(() => options.ReplayClaimTimeout = TimeSpan.Zero)
            .ParamName.ShouldBe("ReplayClaimTimeout");
    }

    #endregion

    #region Helpers

    private static DeadLetterOrchestrator CreateOrchestrator()
    {
        return new DeadLetterOrchestrator(
            Substitute.For<IDeadLetterStore>(),
            Substitute.For<IDeadLetterMessageFactory>(),
            new DeadLetterOptions(),
            NullLogger<DeadLetterOrchestrator>.Instance,
            new JsonMessageSerializer(),
            Substitute.For<IRequestContextAccessor>());
    }

    private static DeadLetterManager CreateManager()
    {
        return new DeadLetterManager(
            Substitute.For<IDeadLetterStore>(),
            CreateOrchestrator(),
            Substitute.For<IServiceProvider>(),
            NullLogger<DeadLetterManager>.Instance,
            new JsonMessageSerializer(),
            new DeadLetterOptions(),
            Substitute.For<IRequestContextAccessor>());
    }

    #endregion
}
