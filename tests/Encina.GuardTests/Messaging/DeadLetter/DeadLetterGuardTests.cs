using Encina.Messaging.DeadLetter;
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
            NullLogger<DeadLetterOrchestrator>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("store");
    }

    [Fact]
    public void DeadLetterOrchestrator_NullMessageFactory_ThrowsArgumentNullException()
    {
        var act = () => new DeadLetterOrchestrator(
            Substitute.For<IDeadLetterStore>(),
            null!,
            new DeadLetterOptions(),
            NullLogger<DeadLetterOrchestrator>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("messageFactory");
    }

    [Fact]
    public void DeadLetterOrchestrator_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new DeadLetterOrchestrator(
            Substitute.For<IDeadLetterStore>(),
            Substitute.For<IDeadLetterMessageFactory>(),
            null!,
            NullLogger<DeadLetterOrchestrator>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void DeadLetterOrchestrator_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DeadLetterOrchestrator(
            Substitute.For<IDeadLetterStore>(),
            Substitute.For<IDeadLetterMessageFactory>(),
            new DeadLetterOptions(),
            null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
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
            NullLogger<DeadLetterManager>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("store");
    }

    [Fact]
    public void DeadLetterManager_NullOrchestrator_ThrowsArgumentNullException()
    {
        var act = () => new DeadLetterManager(
            Substitute.For<IDeadLetterStore>(),
            null!,
            Substitute.For<IServiceProvider>(),
            NullLogger<DeadLetterManager>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("orchestrator");
    }

    [Fact]
    public void DeadLetterManager_NullServiceProvider_ThrowsArgumentNullException()
    {
        var act = () => new DeadLetterManager(
            Substitute.For<IDeadLetterStore>(),
            CreateOrchestrator(),
            null!,
            NullLogger<DeadLetterManager>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("serviceProvider");
    }

    [Fact]
    public void DeadLetterManager_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DeadLetterManager(
            Substitute.For<IDeadLetterStore>(),
            CreateOrchestrator(),
            Substitute.For<IServiceProvider>(),
            null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
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

    #region Helpers

    private static DeadLetterOrchestrator CreateOrchestrator()
    {
        return new DeadLetterOrchestrator(
            Substitute.For<IDeadLetterStore>(),
            Substitute.For<IDeadLetterMessageFactory>(),
            new DeadLetterOptions(),
            NullLogger<DeadLetterOrchestrator>.Instance);
    }

    private static DeadLetterManager CreateManager()
    {
        return new DeadLetterManager(
            Substitute.For<IDeadLetterStore>(),
            CreateOrchestrator(),
            Substitute.For<IServiceProvider>(),
            NullLogger<DeadLetterManager>.Instance);
    }

    #endregion
}
