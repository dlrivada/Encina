using Encina.Messaging.DeadLetter;
using Encina.Messaging.Serialization;
using Encina.Testing.Fakes.Models;
using Encina.Testing.Fakes.Stores;
using Encina.Testing.Shouldly;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using Shouldly;

using static LanguageExt.Prelude;

#pragma warning disable CA2012 // NSubstitute setup of ValueTask-returning members

namespace Encina.UnitTests.Messaging.DeadLetter;

/// <summary>
/// Unit tests for <see cref="DeadLetterManager"/>.
/// </summary>
public sealed class DeadLetterManagerTests
{
    private static readonly DateTime FixedUtcNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    #region Constructor

    [Fact]
    public void Constructor_WithNullStore_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new DeadLetterManager(
            null!, CreateOrchestrator(), Substitute.For<IServiceProvider>(), NullLogger<DeadLetterManager>.Instance,
            new JsonMessageSerializer(), new DeadLetterOptions(), Substitute.For<IRequestContextAccessor>()))
            .ParamName.ShouldBe("store");
    }

    [Fact]
    public void Constructor_WithNullOrchestrator_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new DeadLetterManager(
            Substitute.For<IDeadLetterStore>(), null!, Substitute.For<IServiceProvider>(), NullLogger<DeadLetterManager>.Instance,
            new JsonMessageSerializer(), new DeadLetterOptions(), Substitute.For<IRequestContextAccessor>()))
            .ParamName.ShouldBe("orchestrator");
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new DeadLetterManager(
            Substitute.For<IDeadLetterStore>(), CreateOrchestrator(), null!, NullLogger<DeadLetterManager>.Instance,
            new JsonMessageSerializer(), new DeadLetterOptions(), Substitute.For<IRequestContextAccessor>()))
            .ParamName.ShouldBe("serviceProvider");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new DeadLetterManager(
            Substitute.For<IDeadLetterStore>(), CreateOrchestrator(), Substitute.For<IServiceProvider>(), null!,
            new JsonMessageSerializer(), new DeadLetterOptions(), Substitute.For<IRequestContextAccessor>()))
            .ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_WithNullMessageSerializer_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new DeadLetterManager(
            Substitute.For<IDeadLetterStore>(), CreateOrchestrator(), Substitute.For<IServiceProvider>(),
            NullLogger<DeadLetterManager>.Instance, null!, new DeadLetterOptions(), Substitute.For<IRequestContextAccessor>()))
            .ParamName.ShouldBe("messageSerializer");
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new DeadLetterManager(
            Substitute.For<IDeadLetterStore>(), CreateOrchestrator(), Substitute.For<IServiceProvider>(),
            NullLogger<DeadLetterManager>.Instance, new JsonMessageSerializer(), null!, Substitute.For<IRequestContextAccessor>()))
            .ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_WithNullRequestContextAccessor_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new DeadLetterManager(
            Substitute.For<IDeadLetterStore>(), CreateOrchestrator(), Substitute.For<IServiceProvider>(),
            NullLogger<DeadLetterManager>.Instance, new JsonMessageSerializer(), new DeadLetterOptions(), null!))
            .ParamName.ShouldBe("requestContextAccessor");
    }

    [Fact]
    public void Constructor_WithValidParameters_Succeeds()
    {
        var (manager, _, _, _) = CreateManager();

        manager.ShouldNotBeNull();
    }

    #endregion

    #region ReplayAsync

    [Fact]
    public async Task ReplayAsync_WhenStoreGetFails_ReturnsTheLeft()
    {
        var (manager, store, _, _) = CreateManager();
        store.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Option<IDeadLetterMessage>>(EncinaErrors.Create("db.down", "down")));

        var result = await manager.ReplayAsync(Guid.NewGuid());

        result.ShouldBeErrorWithCode("db.down");
    }

    [Fact]
    public async Task ReplayAsync_WhenMessageNotFound_ReturnsError()
    {
        var (manager, store, _, _) = CreateManager();
        var messageId = Guid.NewGuid();

        store.GetAsync(messageId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Option<IDeadLetterMessage>>(Option<IDeadLetterMessage>.None));

        var result = await manager.ReplayAsync(messageId);

        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: e => e.Message.ShouldContain("not found"));
    }

    [Fact]
    public async Task ReplayAsync_WhenAlreadyReplayed_ReturnsError()
    {
        var (manager, store, _, _) = CreateManager();
        var messageId = Guid.NewGuid();

        ArrangeMessage(store, CreateMockMessage(messageId, isReplayed: true));

        var result = await manager.ReplayAsync(messageId);

        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: e => e.Message.ShouldContain("already been replayed"));
    }

    [Fact]
    public async Task ReplayAsync_WhenExpired_ReturnsError()
    {
        var (manager, store, _, _) = CreateManager();
        var messageId = Guid.NewGuid();

        ArrangeMessage(store, CreateMockMessage(messageId, isExpired: true));

        var result = await manager.ReplayAsync(messageId);

        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: e => e.Message.ShouldContain("expired"));
        await store.DidNotReceive().TryClaimForReplayAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReplayAsync_EvaluatesExpiryAtTheTimeProviderInstant()
    {
        var (manager, store, _, _) = CreateManager();
        var messageId = Guid.NewGuid();
        var message = CreateMockMessage(messageId);
        ArrangeMessage(store, message);

        await manager.ReplayAsync(messageId);

        message.Received().IsExpiredAt(FixedUtcNow);
    }

    [Fact]
    public async Task ReplayAsync_WhenUnknownRequestType_RecordsTheOutcomeCodeAndReturnsError()
    {
        var (manager, store, _, _) = CreateManager();
        var messageId = Guid.NewGuid();

        ArrangeMessage(store, CreateMockMessage(messageId, requestType: "NonExistent.Type, NonExistent.Assembly"));

        var result = await manager.ReplayAsync(messageId);

        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: e => e.Message.ShouldContain("Cannot resolve type"));
        await store.Received(1).MarkAsReplayedAsync(
            messageId, DeadLetterErrorCodes.DeserializationFailed, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReplayAsync_WhenTheRejectionOutcomeCannotBeRecorded_ReturnsTheStoreLeft()
    {
        var (manager, store, _, _) = CreateManager();
        var messageId = Guid.NewGuid();
        ArrangeMessage(store, CreateMockMessage(messageId, requestType: "NonExistent.Type, NonExistent.Assembly"));
        store.MarkAsReplayedAsync(messageId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, bool>(EncinaErrors.Create("db.down", "down")));

        var result = await manager.ReplayAsync(messageId);

        result.ShouldBeErrorWithCode("db.down");
    }

    /// <summary>A request the manager can resolve by name and deserialize.</summary>
    public sealed record ReplayedCommand(int Value) : IRequest<int>;

    [Fact]
    public async Task ReplayAsync_WhenTheReplayedRequestSucceeds_ReportsSuccessAndStoresTheOutcomeCode()
    {
        var (manager, store, _, serviceProvider) = CreateManager();
        var messageId = Guid.NewGuid();
        var message = CreateMockMessage(messageId, requestType: typeof(ReplayedCommand).AssemblyQualifiedName!);
        message.RequestContent.Returns("{\"value\":5}");
        ArrangeMessage(store, message);
        var encina = ArrangeEncina(serviceProvider, Right<EncinaError, int>(5));

        var result = await manager.ReplayAsync(messageId);

        var replay = result.ShouldBeRight();
        replay.ErrorMessage.ShouldBeNull();
        replay.Success.ShouldBeTrue();
        await encina.Received(1).Send(Arg.Is<IRequest<int>>(r => r is ReplayedCommand && ((ReplayedCommand)r).Value == 5), Arg.Any<CancellationToken>());
        await store.Received(1).MarkAsReplayedAsync(messageId, DeadLetterErrorCodes.ReplaySucceeded, Arg.Any<CancellationToken>());
        await store.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReplayAsync_ClaimsTheMessageBeforeDispatchingWithTheClaimTimeout()
    {
        var options = new DeadLetterOptions { ReplayClaimTimeout = TimeSpan.FromMinutes(2) };
        var (manager, store, _, serviceProvider) = CreateManager(options: options);
        var messageId = Guid.NewGuid();
        var message = CreateMockMessage(messageId, requestType: typeof(ReplayedCommand).AssemblyQualifiedName!);
        message.RequestContent.Returns("{\"value\":5}");
        ArrangeMessage(store, message);
        ArrangeEncina(serviceProvider, Right<EncinaError, int>(5));

        await manager.ReplayAsync(messageId);

        await store.Received(1).TryClaimForReplayAsync(
            messageId, FixedUtcNow.AddMinutes(-2), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReplayAsync_WhenTheClaimIsLost_DoesNotDispatchAndReportsInProgress()
    {
        var (manager, store, _, serviceProvider) = CreateManager();
        var messageId = Guid.NewGuid();
        var message = CreateMockMessage(messageId, requestType: typeof(ReplayedCommand).AssemblyQualifiedName!);
        ArrangeMessage(store, message);
        store.TryClaimForReplayAsync(messageId, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, bool>(false));
        var encina = ArrangeEncina(serviceProvider, Right<EncinaError, int>(5));

        var result = await manager.ReplayAsync(messageId);

        var replay = result.ShouldBeRight();
        replay.Success.ShouldBeFalse();
        replay.ErrorMessage.ShouldBe(DeadLetterErrorCodes.ReplayInProgress);
        await encina.DidNotReceive().Send(Arg.Any<IRequest<int>>(), Arg.Any<CancellationToken>());
        await store.DidNotReceive().MarkAsReplayedAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReplayAsync_WhenTheClaimFails_ReturnsTheStoreLeft()
    {
        var (manager, store, _, _) = CreateManager();
        var messageId = Guid.NewGuid();
        ArrangeMessage(store, CreateMockMessage(messageId));
        store.TryClaimForReplayAsync(messageId, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, bool>(EncinaErrors.Create("db.down", "down")));

        var result = await manager.ReplayAsync(messageId);

        result.ShouldBeErrorWithCode("db.down");
    }

    [Fact]
    public async Task ReplayAsync_TwoConcurrentReplaysOnTheFakeStore_DispatchExactlyOnce()
    {
        var store = new FakeDeadLetterStore(new FakeTimeProvider(new DateTimeOffset(FixedUtcNow)));
        var message = new FakeDeadLetterMessage
        {
            RequestType = typeof(ReplayedCommand).AssemblyQualifiedName!,
            RequestContent = "{\"value\":5}",
            SourcePattern = "Outbox",
            DeadLetteredAtUtc = FixedUtcNow
        };
        await store.AddAsync(message);
        var (manager, serviceProvider) = CreateManagerOn(store);
        var encina = ArrangeEncina(serviceProvider, Right<EncinaError, int>(5));

        var first = await manager.ReplayAsync(message.Id);
        var second = await manager.ReplayAsync(message.Id);

        first.ShouldBeRight().Success.ShouldBeTrue();
        second.ShouldBeError();
        await encina.Received(1).Send(Arg.Any<IRequest<int>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReplayAsync_WhenAnotherHostHoldsALiveClaimOnTheFakeStore_DoesNotDispatch()
    {
        var store = new FakeDeadLetterStore(new FakeTimeProvider(new DateTimeOffset(FixedUtcNow)));
        var message = new FakeDeadLetterMessage
        {
            RequestType = typeof(ReplayedCommand).AssemblyQualifiedName!,
            RequestContent = "{\"value\":5}",
            SourcePattern = "Outbox",
            DeadLetteredAtUtc = FixedUtcNow,
            ReplayClaimedAtUtc = FixedUtcNow.AddMinutes(-1)
        };
        await store.AddAsync(message);
        var (manager, serviceProvider) = CreateManagerOn(store);
        var encina = ArrangeEncina(serviceProvider, Right<EncinaError, int>(5));

        var result = await manager.ReplayAsync(message.Id);

        result.ShouldBeRight().ErrorMessage.ShouldBe(DeadLetterErrorCodes.ReplayInProgress);
        await encina.DidNotReceive().Send(Arg.Any<IRequest<int>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReplayAsync_WhenAClaimIsOlderThanTheTimeoutOnTheFakeStore_TakesItOver()
    {
        var store = new FakeDeadLetterStore(new FakeTimeProvider(new DateTimeOffset(FixedUtcNow)));
        var message = new FakeDeadLetterMessage
        {
            RequestType = typeof(ReplayedCommand).AssemblyQualifiedName!,
            RequestContent = "{\"value\":5}",
            SourcePattern = "Outbox",
            DeadLetteredAtUtc = FixedUtcNow,
            ReplayClaimedAtUtc = FixedUtcNow.AddMinutes(-10)
        };
        await store.AddAsync(message);
        var (manager, serviceProvider) = CreateManagerOn(store);
        ArrangeEncina(serviceProvider, Right<EncinaError, int>(5));

        var result = await manager.ReplayAsync(message.Id);

        result.ShouldBeRight().Success.ShouldBeTrue();
    }

    [Fact]
    public async Task ReplayAsync_WhenTheReplayedRequestReturnsLeft_StoresOnlyTheErrorCode()
    {
        var (manager, store, _, serviceProvider) = CreateManager();
        var messageId = Guid.NewGuid();
        var message = CreateMockMessage(messageId, requestType: typeof(ReplayedCommand).AssemblyQualifiedName!);
        message.RequestContent.Returns("{\"value\":5}");
        ArrangeMessage(store, message);
        ArrangeEncina(serviceProvider,
            Left<EncinaError, int>(EncinaErrors.Create("consent.missing", "Consent missing for subject 'patient-1'")));

        var result = await manager.ReplayAsync(messageId);

        // Only the error code travels: EncinaError.Message can carry personal data and must not
        // reach the returned ReplayResult or the stored outcome (#1259 review, #2012).
        var replay = result.ShouldBeRight();
        replay.Success.ShouldBeFalse();
        replay.ErrorMessage.ShouldNotBeNull().ShouldContain("consent.missing");
        replay.ErrorMessage.ShouldNotContain("patient-1");
        await store.Received(1).MarkAsReplayedAsync(messageId, "consent.missing", Arg.Any<CancellationToken>());
        await store.DidNotReceive().MarkAsReplayedAsync(
            messageId, Arg.Is<string>(s => s.Contains("patient-1")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReplayAsync_WhenTheHandlerThrows_StoresTheReplayFailedCodeNotTheMessage()
    {
        var (manager, store, _, serviceProvider) = CreateManager();
        var messageId = Guid.NewGuid();
        var message = CreateMockMessage(messageId, requestType: typeof(ReplayedCommand).AssemblyQualifiedName!);
        message.RequestContent.Returns("{\"value\":5}");
        ArrangeMessage(store, message);
        var encina = Substitute.For<IEncina>();
        encina.Send(Arg.Any<IRequest<int>>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, int>>>(_ => throw new InvalidOperationException("patient-1 secret"));
        serviceProvider.GetService(typeof(IEncina)).Returns(encina);

        var result = await manager.ReplayAsync(messageId);

        result.ShouldBeRight().Success.ShouldBeFalse();
        await store.Received(1).MarkAsReplayedAsync(
            messageId, DeadLetterErrorCodes.ReplayFailed, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReplayAsync_WhenIEncinaIsNotRegistered_RecordsReplayFailedAndReturnsError()
    {
        var (manager, store, _, _) = CreateManager();
        var messageId = Guid.NewGuid();
        var message = CreateMockMessage(messageId, requestType: typeof(ReplayedCommand).AssemblyQualifiedName!);
        message.RequestContent.Returns("{\"value\":5}");
        ArrangeMessage(store, message);

        var result = await manager.ReplayAsync(messageId);

        result.ShouldBeError();
        await store.Received(1).MarkAsReplayedAsync(
            messageId, DeadLetterErrorCodes.ReplayFailed, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReplayAsync_WhenRecordingTheOutcomeFails_ReturnsTheStoreLeft()
    {
        var (manager, store, _, serviceProvider) = CreateManager();
        var messageId = Guid.NewGuid();
        var message = CreateMockMessage(messageId, requestType: typeof(ReplayedCommand).AssemblyQualifiedName!);
        message.RequestContent.Returns("{\"value\":5}");
        ArrangeMessage(store, message);
        ArrangeEncina(serviceProvider, Right<EncinaError, int>(5));
        store.MarkAsReplayedAsync(messageId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, bool>(EncinaErrors.Create("db.down", "down")));

        var result = await manager.ReplayAsync(messageId);

        result.ShouldBeErrorWithCode("db.down");
    }

    [Fact]
    public async Task ReplayAsync_WhenSavingTheOutcomeFails_ReturnsTheStoreLeft()
    {
        var (manager, store, _, serviceProvider) = CreateManager();
        var messageId = Guid.NewGuid();
        var message = CreateMockMessage(messageId, requestType: typeof(ReplayedCommand).AssemblyQualifiedName!);
        message.RequestContent.Returns("{\"value\":5}");
        ArrangeMessage(store, message);
        ArrangeEncina(serviceProvider, Right<EncinaError, int>(5));
        store.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(EncinaErrors.Create("db.save", "down")));

        var result = await manager.ReplayAsync(messageId);

        result.ShouldBeErrorWithCode("db.save");
    }

    [Fact]
    public async Task ReplayAsync_WhenAConcurrentReplayRecordedFirst_ReportsAlreadyReplayed()
    {
        var (manager, store, _, serviceProvider) = CreateManager();
        var messageId = Guid.NewGuid();
        var message = CreateMockMessage(messageId, requestType: typeof(ReplayedCommand).AssemblyQualifiedName!);
        message.RequestContent.Returns("{\"value\":5}");
        ArrangeMessage(store, message);
        ArrangeEncina(serviceProvider, Right<EncinaError, int>(5));
        store.MarkAsReplayedAsync(messageId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, bool>(false));

        var result = await manager.ReplayAsync(messageId);

        var replay = result.ShouldBeRight();
        replay.Success.ShouldBeFalse();
        replay.ErrorMessage.ShouldBe(DeadLetterErrorCodes.AlreadyReplayed);
    }

    #endregion

    #region Ambient tenant (B7)

    [Fact]
    public async Task GetMessagesAsync_AmbientTenant_IsAppliedToTheStoreFilterWithoutMutatingTheCallersFilter()
    {
        var accessor = AccessorFor("tenant-a");
        var (manager, store, _, _) = CreateManager(accessor: accessor);
        var filter = new DeadLetterFilter { SourcePattern = "Outbox" };
        ArrangeEmptyPage(store);

        await manager.GetMessagesAsync(filter, 0, 10);

        await store.Received(1).GetMessagesAsync(
            Arg.Is<DeadLetterFilter>(f => f.TenantId == "tenant-a" && f.SourcePattern == "Outbox"),
            0, 10, false, Arg.Any<CancellationToken>());
        filter.TenantId.ShouldBeNull();
    }

    [Fact]
    public async Task GetMessagesAsync_AllTenants_SkipsTheAmbientDefault()
    {
        var accessor = AccessorFor("tenant-a");
        var (manager, store, _, _) = CreateManager(accessor: accessor);
        ArrangeEmptyPage(store);

        await manager.GetMessagesAsync(new DeadLetterFilter { AllTenants = true });

        await store.Received(1).GetMessagesAsync(
            Arg.Is<DeadLetterFilter>(f => f.TenantId == null), 0, 100, false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMessagesAsync_ExplicitTenant_WinsOverTheAmbientTenant()
    {
        var accessor = AccessorFor("tenant-a");
        var (manager, store, _, _) = CreateManager(accessor: accessor);
        ArrangeEmptyPage(store);

        await manager.GetMessagesAsync(new DeadLetterFilter { TenantId = "tenant-b" });

        await store.Received(1).GetMessagesAsync(
            Arg.Is<DeadLetterFilter>(f => f.TenantId == "tenant-b"), 0, 100, false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMessagesAsync_NoAmbientTenant_LeavesTheFilterUnscoped()
    {
        var (manager, store, _, _) = CreateManager();
        ArrangeEmptyPage(store);

        await manager.GetMessagesAsync();

        await store.Received(1).GetMessagesAsync(
            Arg.Is<DeadLetterFilter>(f => f.TenantId == null), 0, 100, false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCountAsync_AmbientTenant_IsAppliedToTheStoreFilter()
    {
        var (manager, store, _, _) = CreateManager(accessor: AccessorFor("tenant-a"));
        store.GetCountAsync(Arg.Any<DeadLetterFilter>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, int>(3));

        var result = await manager.GetCountAsync();

        result.ShouldBeRight().ShouldBe(3);
        await store.Received(1).GetCountAsync(
            Arg.Is<DeadLetterFilter>(f => f.TenantId == "tenant-a"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMessageAsync_MessageOfAnotherTenant_ReadsAsNotFound()
    {
        var (manager, store, _, _) = CreateManager(accessor: AccessorFor("tenant-a"));
        var messageId = Guid.NewGuid();
        var message = CreateMockMessage(messageId);
        message.TenantId.Returns("tenant-b");
        ArrangeMessage(store, message);

        var result = await manager.GetMessageAsync(messageId);

        result.ShouldBeRight().IsNone.ShouldBeTrue();
    }

    [Fact]
    public async Task ReplayAsync_MessageOfAnotherTenant_IsReportedAsNotFoundAndNeverClaimed()
    {
        var (manager, store, _, _) = CreateManager(accessor: AccessorFor("tenant-a"));
        var messageId = Guid.NewGuid();
        var message = CreateMockMessage(messageId);
        message.TenantId.Returns("tenant-b");
        ArrangeMessage(store, message);

        var result = await manager.ReplayAsync(messageId);

        result.ShouldBeErrorWithCode(DeadLetterErrorCodes.NotFound);
        await store.DidNotReceive().TryClaimForReplayAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_MessageOfAnotherTenant_IsNotDeleted()
    {
        var (manager, store, _, _) = CreateManager(accessor: AccessorFor("tenant-a"));
        var messageId = Guid.NewGuid();
        var message = CreateMockMessage(messageId);
        message.TenantId.Returns("tenant-b");
        ArrangeMessage(store, message);

        var result = await manager.DeleteAsync(messageId);

        result.ShouldBeError();
        await store.DidNotReceive().DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_MessageOfTheAmbientTenant_IsDeleted()
    {
        var (manager, store, _, _) = CreateManager(accessor: AccessorFor("tenant-a"));
        var messageId = Guid.NewGuid();
        var message = CreateMockMessage(messageId);
        message.TenantId.Returns("tenant-a");
        ArrangeMessage(store, message);
        store.DeleteAsync(messageId, Arg.Any<CancellationToken>()).Returns(Right<EncinaError, bool>(true));

        var result = await manager.DeleteAsync(messageId);

        result.ShouldBeRight();
    }

    [Fact]
    public async Task DeleteAsync_AmbientTenantAndLookupFails_ReturnsTheLeft()
    {
        var (manager, store, _, _) = CreateManager(accessor: AccessorFor("tenant-a"));
        store.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Option<IDeadLetterMessage>>(EncinaErrors.Create("db.down", "down")));

        var result = await manager.DeleteAsync(Guid.NewGuid());

        result.ShouldBeErrorWithCode("db.down");
    }

    #endregion

    #region GetMessageAsync

    [Fact]
    public async Task GetMessageAsync_DelegatesToStore()
    {
        var (manager, store, _, _) = CreateManager();
        var messageId = Guid.NewGuid();
        ArrangeMessage(store, CreateMockMessage(messageId));

        var result = await manager.GetMessageAsync(messageId);

        result.ShouldBeRight().IsSome.ShouldBeTrue();
        await store.Received(1).GetAsync(messageId, Arg.Any<CancellationToken>());
    }

    #endregion

    #region GetMessagesAsync

    [Fact]
    public async Task GetMessagesAsync_DelegatesToStore()
    {
        var (manager, store, _, _) = CreateManager();
        var messages = new List<IDeadLetterMessage>();
        store.GetMessagesAsync(Arg.Any<DeadLetterFilter>(), 0, 100, false, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IEnumerable<IDeadLetterMessage>>(messages));

        var result = await manager.GetMessagesAsync(new DeadLetterFilter(), 0, 100);

        result.ShouldBeRight().ShouldBe(messages);
    }

    #endregion

    #region GetCountAsync

    [Fact]
    public async Task GetCountAsync_DelegatesToStore()
    {
        var (manager, store, _, _) = CreateManager();
        store.GetCountAsync(Arg.Any<DeadLetterFilter>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, int>(42));

        var result = await manager.GetCountAsync(new DeadLetterFilter());

        result.ShouldBeRight().ShouldBe(42);
    }

    #endregion

    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_DelegatesToStore()
    {
        var (manager, store, _, _) = CreateManager();
        var messageId = Guid.NewGuid();
        store.DeleteAsync(messageId, Arg.Any<CancellationToken>()).Returns(Right<EncinaError, bool>(true));

        var result = await manager.DeleteAsync(messageId);

        result.IsRight.ShouldBeTrue();
        await store.Received(1).DeleteAsync(messageId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ReturnsDeleteFailed()
    {
        var (manager, store, _, _) = CreateManager();
        var messageId = Guid.NewGuid();
        store.DeleteAsync(messageId, Arg.Any<CancellationToken>()).Returns(Right<EncinaError, bool>(false));

        var result = await manager.DeleteAsync(messageId);

        result.ShouldBeErrorWithCode(DeadLetterErrorCodes.DeleteFailed);
    }

    [Fact]
    public async Task DeleteAsync_WhenTheStoreFails_ReturnsTheStoreLeftNotNotFound()
    {
        var (manager, store, _, _) = CreateManager();
        var messageId = Guid.NewGuid();
        store.DeleteAsync(messageId, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, bool>(EncinaErrors.Create("db.down", "down")));

        var result = await manager.DeleteAsync(messageId);

        result.ShouldBeErrorWithCode("db.down");
    }

    #endregion

    #region DeleteAllAsync

    [Fact]
    public async Task DeleteAllAsync_WithNullFilter_ThrowsArgumentNullException()
    {
        var (manager, _, _, _) = CreateManager();

        await Should.ThrowAsync<ArgumentNullException>(async () =>
        {
            await manager.DeleteAllAsync(null!);
        });
    }

    [Fact]
    public async Task DeleteAllAsync_DeletesInOneSetBasedCallAndSaves()
    {
        var (manager, store, _, _) = CreateManager();
        store.DeleteManyAsync(Arg.Any<DeadLetterFilter>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, int>(2));

        var result = await manager.DeleteAllAsync(new DeadLetterFilter { RequestType = "TestType" });

        result.ShouldBeRight().ShouldBe(2);
        await store.Received(1).DeleteManyAsync(
            Arg.Is<DeadLetterFilter>(f => f.RequestType == "TestType"), Arg.Any<CancellationToken>());
        await store.DidNotReceive().DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await store.DidNotReceive().GetMessagesAsync(
            Arg.Any<DeadLetterFilter>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        await store.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAllAsync_WithTheAllFilter_DeletesTheWholeQueueInOneStatement()
    {
        var (manager, store, _, _) = CreateManager();
        store.DeleteManyAsync(Arg.Any<DeadLetterFilter>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, int>(7));

        var result = await manager.DeleteAllAsync(DeadLetterFilter.All);

        result.ShouldBeRight().ShouldBe(7);
        await store.Received(1).DeleteManyAsync(
            Arg.Is<DeadLetterFilter>(f => f.TenantId == null && f.SourcePattern == null && f.ExcludeReplayed == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAllAsync_AmbientTenant_ScopesTheDelete()
    {
        var (manager, store, _, _) = CreateManager(accessor: AccessorFor("tenant-a"));
        store.DeleteManyAsync(Arg.Any<DeadLetterFilter>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, int>(1));

        await manager.DeleteAllAsync(DeadLetterFilter.All);

        await store.Received(1).DeleteManyAsync(
            Arg.Is<DeadLetterFilter>(f => f.TenantId == "tenant-a"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAllAsync_WhenNoMatches_ReturnsZero()
    {
        var (manager, store, _, _) = CreateManager();
        store.DeleteManyAsync(Arg.Any<DeadLetterFilter>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, int>(0));

        var result = await manager.DeleteAllAsync(new DeadLetterFilter());

        result.ShouldBeRight().ShouldBe(0);
    }

    [Fact]
    public async Task DeleteAllAsync_WhenTheDeleteFails_ReturnsTheLeftAndDoesNotSave()
    {
        var (manager, store, _, _) = CreateManager();
        store.DeleteManyAsync(Arg.Any<DeadLetterFilter>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, int>(EncinaErrors.Create("db.down", "down")));

        var result = await manager.DeleteAllAsync(new DeadLetterFilter());

        result.ShouldBeErrorWithCode("db.down");
        await store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAllAsync_WhenSavingFails_ReturnsTheLeft()
    {
        var (manager, store, _, _) = CreateManager();
        store.DeleteManyAsync(Arg.Any<DeadLetterFilter>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, int>(3));
        store.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(EncinaErrors.Create("db.save", "down")));

        var result = await manager.DeleteAllAsync(new DeadLetterFilter());

        result.ShouldBeErrorWithCode("db.save");
    }

    #endregion

    #region CleanupExpiredAsync

    [Fact]
    public async Task CleanupExpiredAsync_DelegatesToOrchestrator()
    {
        var (manager, store, _, _) = CreateManager();
        store.DeleteExpiredAsync(Arg.Any<CancellationToken>()).Returns(Right<EncinaError, int>(0));

        var result = await manager.CleanupExpiredAsync();

        result.ShouldBeRight().ShouldBe(0);
    }

    #endregion

    #region GetStatisticsAsync

    [Fact]
    public async Task GetStatisticsAsync_ReturnsStatisticsFromOrchestrator()
    {
        var (manager, store, _, _) = CreateManager();
        store.GetCountAsync(Arg.Any<DeadLetterFilter>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, int>(10));
        store.GetMessagesAsync(
            Arg.Any<DeadLetterFilter>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IEnumerable<IDeadLetterMessage>>(System.Array.Empty<IDeadLetterMessage>()));

        var result = await manager.GetStatisticsAsync();

        result.ShouldBeRight().TotalCount.ShouldBe(10);
    }

    #endregion

    #region ReplayAllAsync

    [Fact]
    public async Task ReplayAllAsync_WithNullFilter_ThrowsArgumentNullException()
    {
        var (manager, _, _, _) = CreateManager();

        await Should.ThrowAsync<ArgumentNullException>(async () =>
        {
            await manager.ReplayAllAsync(null!);
        });
    }

    [Fact]
    public async Task ReplayAllAsync_WithNonPositiveMaxMessages_ThrowsArgumentOutOfRangeException()
    {
        var (manager, _, _, _) = CreateManager();

        await Should.ThrowAsync<ArgumentOutOfRangeException>(async () =>
        {
            await manager.ReplayAllAsync(new DeadLetterFilter(), maxMessages: 0);
        });
    }

    [Fact]
    public async Task ReplayAllAsync_WhenNoMessages_ReturnsEmptyResult()
    {
        var (manager, store, _, _) = CreateManager();
        ArrangeEmptyPage(store);

        var result = await manager.ReplayAllAsync(new DeadLetterFilter());

        var batch = result.ShouldBeRight();
        batch.TotalProcessed.ShouldBe(0);
        batch.SuccessCount.ShouldBe(0);
        batch.FailureCount.ShouldBe(0);
    }

    [Fact]
    public async Task ReplayAllAsync_DoesNotMutateTheCallersFilterAndRequestsPendingOnly()
    {
        var (manager, store, _, _) = CreateManager();
        ArrangeEmptyPage(store);
        var filter = new DeadLetterFilter { SourcePattern = "Outbox", ExcludeReplayed = null };

        await manager.ReplayAllAsync(filter);

        filter.ExcludeReplayed.ShouldBeNull();
        await store.Received(1).GetMessagesAsync(
            Arg.Is<DeadLetterFilter>(f => f.ExcludeReplayed == true && f.SourcePattern == "Outbox"),
            0, 100, false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReplayAllAsync_CapsTheBatchAtTheMaximumPageSize()
    {
        var (manager, store, _, _) = CreateManager();
        ArrangeEmptyPage(store);

        await manager.ReplayAllAsync(new DeadLetterFilter(), maxMessages: 5000);

        await store.Received(1).GetMessagesAsync(
            Arg.Any<DeadLetterFilter>(), 0, DeadLetterStoreLimits.MaxPageSize, false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReplayAllAsync_WhenTheStoreReadFails_ReturnsTheLeft()
    {
        var (manager, store, _, _) = CreateManager();
        store.GetMessagesAsync(
            Arg.Any<DeadLetterFilter>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, IEnumerable<IDeadLetterMessage>>(EncinaErrors.Create("db.down", "down")));

        var result = await manager.ReplayAllAsync(new DeadLetterFilter());

        result.ShouldBeErrorWithCode("db.down");
    }

    [Fact]
    public async Task ReplayAllAsync_ReplaysEachMessageAndReportsPerMessageFailuresByCode()
    {
        var (manager, store, _, serviceProvider) = CreateManager();
        var okId = Guid.NewGuid();
        var expiredId = Guid.NewGuid();
        var ok = CreateMockMessage(okId, requestType: typeof(ReplayedCommand).AssemblyQualifiedName!);
        ok.RequestContent.Returns("{\"value\":5}");
        var expired = CreateMockMessage(expiredId, isExpired: true);
        store.GetMessagesAsync(
            Arg.Any<DeadLetterFilter>(), 0, 100, false, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IEnumerable<IDeadLetterMessage>>(new[] { ok, expired }));
        ArrangeEncina(serviceProvider, Right<EncinaError, int>(5));

        var batch = (await manager.ReplayAllAsync(new DeadLetterFilter())).ShouldBeRight();

        batch.TotalProcessed.ShouldBe(2);
        batch.SuccessCount.ShouldBe(1);
        batch.FailureCount.ShouldBe(1);
        batch.Results.Single(r => r.MessageId == expiredId).ErrorMessage.ShouldBe(DeadLetterErrorCodes.Expired);
    }

    [Fact]
    public async Task ReplayAllAsync_AllTenants_ReplaysMessagesOfOtherTenantsDespiteAnAmbientTenant()
    {
        var (manager, store, _, serviceProvider) = CreateManager(accessor: AccessorFor("tenant-a"));
        var id = Guid.NewGuid();
        var message = CreateMockMessage(id, requestType: typeof(ReplayedCommand).AssemblyQualifiedName!);
        message.RequestContent.Returns("{\"value\":5}");
        message.TenantId.Returns("tenant-b");
        store.GetMessagesAsync(
            Arg.Any<DeadLetterFilter>(), 0, 100, false, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IEnumerable<IDeadLetterMessage>>(new[] { message }));
        ArrangeEncina(serviceProvider, Right<EncinaError, int>(5));

        var batch = (await manager.ReplayAllAsync(new DeadLetterFilter { AllTenants = true })).ShouldBeRight();

        batch.SuccessCount.ShouldBe(1);
    }

    #endregion

    #region Helper Methods

    private static (DeadLetterManager Manager, IDeadLetterStore Store, DeadLetterOrchestrator Orchestrator, IServiceProvider ServiceProvider) CreateManager(
        IRequestContextAccessor? accessor = null,
        DeadLetterOptions? options = null)
    {
        var store = Substitute.For<IDeadLetterStore>();
        store.TryClaimForReplayAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, bool>(true));
        store.MarkAsReplayedAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, bool>(true));
        store.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Right<EncinaError, Unit>(unit));

        var serviceProvider = Substitute.For<IServiceProvider>();
        var effectiveAccessor = accessor ?? Substitute.For<IRequestContextAccessor>();
        var orchestrator = CreateOrchestrator(store, effectiveAccessor);

        var manager = NewManager(store, orchestrator, serviceProvider, effectiveAccessor, options);

        return (manager, store, orchestrator, serviceProvider);
    }

    private static (DeadLetterManager Manager, IServiceProvider ServiceProvider) CreateManagerOn(IDeadLetterStore store)
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        var accessor = Substitute.For<IRequestContextAccessor>();
        var manager = NewManager(store, CreateOrchestrator(store, accessor), serviceProvider, accessor);

        return (manager, serviceProvider);
    }

    private static DeadLetterManager NewManager(
        IDeadLetterStore store,
        DeadLetterOrchestrator orchestrator,
        IServiceProvider serviceProvider,
        IRequestContextAccessor accessor,
        DeadLetterOptions? options = null)
        => new(
            store,
            orchestrator,
            serviceProvider,
            NullLogger<DeadLetterManager>.Instance,
            new JsonMessageSerializer(),
            options ?? new DeadLetterOptions(),
            accessor,
            new FakeTimeProvider(new DateTimeOffset(FixedUtcNow)));

    private static DeadLetterOrchestrator CreateOrchestrator(
        IDeadLetterStore? store = null,
        IRequestContextAccessor? accessor = null)
    {
        store ??= Substitute.For<IDeadLetterStore>();
        var messageFactory = Substitute.For<IDeadLetterMessageFactory>();
        var options = new DeadLetterOptions();
        var logger = NullLogger<DeadLetterOrchestrator>.Instance;

        return new DeadLetterOrchestrator(
            store, messageFactory, options, logger, new JsonMessageSerializer(),
            accessor ?? Substitute.For<IRequestContextAccessor>());
    }

    private static IRequestContextAccessor AccessorFor(string tenantId)
    {
        var context = Substitute.For<IRequestContext>();
        context.TenantId.Returns(tenantId);
        var accessor = Substitute.For<IRequestContextAccessor>();
        accessor.RequestContext.Returns(context);
        return accessor;
    }

    private static void ArrangeMessage(IDeadLetterStore store, IDeadLetterMessage message)
        => store.GetAsync(message.Id, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Option<IDeadLetterMessage>>(Option<IDeadLetterMessage>.Some(message)));

    private static void ArrangeEmptyPage(IDeadLetterStore store)
        => store.GetMessagesAsync(
                Arg.Any<DeadLetterFilter>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IEnumerable<IDeadLetterMessage>>(System.Array.Empty<IDeadLetterMessage>()));

    private static IEncina ArrangeEncina(IServiceProvider serviceProvider, Either<EncinaError, int> outcome)
    {
        var encina = Substitute.For<IEncina>();
        encina.Send(Arg.Any<IRequest<int>>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, int>>(outcome));
        serviceProvider.GetService(typeof(IEncina)).Returns(encina);
        return encina;
    }

    private static IDeadLetterMessage CreateMockMessage(
        Guid messageId,
        bool isReplayed = false,
        bool isExpired = false,
        string requestType = "System.Object, System.Runtime")
    {
        var message = Substitute.For<IDeadLetterMessage>();
        message.Id.Returns(messageId);
        message.IsReplayed.Returns(isReplayed);
        message.IsExpiredAt(Arg.Any<DateTime>()).Returns(isExpired);
        message.RequestType.Returns(requestType);
        message.RequestContent.Returns("{}");
        message.ErrorCode.Returns("test.error");
        message.CorrelationId.Returns("test-correlation");
        message.DeadLetteredAtUtc.Returns(FixedUtcNow);
        return message;
    }

    #endregion
}
