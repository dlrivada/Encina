using Encina.Messaging.DeadLetter;
using Encina.Messaging.Serialization;
using Encina.Testing.Shouldly;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

#pragma warning disable CA2012 // NSubstitute setup of ValueTask-returning members

namespace Encina.UnitTests.Messaging.DeadLetter;

/// <summary>
/// The tenancy gate (fail closed when multi-tenancy is in use and no tenant is resolved), the input rules
/// and the batch replay error handling of <see cref="DeadLetterManager"/>.
/// </summary>
public sealed class DeadLetterManagerTenancyAndBatchTests
{
    private const int AllTenantsOptOutEvent = 2993;
    private const int TenantRequiredDeniedEvent = 2994;
    private const int BatchReplayAbortedEvent = 2995;

    private static readonly DateTime FixedUtcNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    public sealed record BatchCommand(int Value) : IRequest<int>;

    private sealed class Rig
    {
        public required DeadLetterManager Manager { get; init; }
        public required IDeadLetterStore Store { get; init; }
        public required IServiceProvider ServiceProvider { get; init; }
        public required FakeLogger<DeadLetterManager> Logger { get; init; }

        public bool Logged(int eventId) => Logger.Collector.GetSnapshot().Any(r => r.Id.Id == eventId);
    }

    private static Rig NewRig(bool tenancy, string? ambientTenant = null)
    {
        var store = Substitute.For<IDeadLetterStore>();
        store.TryClaimForReplayAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, bool>(true));
        store.MarkAsReplayedAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, bool>(true));
        store.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Right<EncinaError, Unit>(unit));
        store.GetMessagesAsync(Arg.Any<DeadLetterFilter>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IEnumerable<IDeadLetterMessage>>(System.Array.Empty<IDeadLetterMessage>()));
        store.GetCountAsync(Arg.Any<DeadLetterFilter>(), Arg.Any<CancellationToken>()).Returns(Right<EncinaError, int>(0));
        store.DeleteManyAsync(Arg.Any<DeadLetterFilter>(), Arg.Any<CancellationToken>()).Returns(Right<EncinaError, int>(0));

        var accessor = Substitute.For<IRequestContextAccessor>();
        if (ambientTenant is not null)
        {
            var context = Substitute.For<IRequestContext>();
            context.TenantId.Returns(ambientTenant);
            accessor.RequestContext.Returns(context);
        }

        var serviceProvider = Substitute.For<IServiceProvider>();
        var orchestrator = new DeadLetterOrchestrator(
            store,
            Substitute.For<IDeadLetterMessageFactory>(),
            new DeadLetterOptions(),
            NullLogger<DeadLetterOrchestrator>.Instance,
            new JsonMessageSerializer(),
            accessor);
        var logger = new FakeLogger<DeadLetterManager>();
        var manager = new DeadLetterManager(
            store,
            orchestrator,
            serviceProvider,
            logger,
            new JsonMessageSerializer(),
            new DeadLetterOptions(),
            accessor,
            new FakeTimeProvider(new DateTimeOffset(FixedUtcNow)),
            tenancy ? new MultiTenancyMarker() : null);

        return new Rig { Manager = manager, Store = store, ServiceProvider = serviceProvider, Logger = logger };
    }

    private static IDeadLetterMessage Message(Guid id, string? requestType = null, bool expired = false, string content = "{\"value\":5}")
    {
        var message = Substitute.For<IDeadLetterMessage>();
        message.Id.Returns(id);
        message.IsReplayed.Returns(false);
        message.IsExpiredAt(Arg.Any<DateTime>()).Returns(expired);
        message.RequestType.Returns(requestType ?? typeof(BatchCommand).AssemblyQualifiedName!);
        message.RequestContent.Returns(content);
        message.TenantId.Returns("tenant-a");
        return message;
    }

    private static void ArrangeMessage(Rig rig, Guid id)
    {
        var message = Message(id);
        rig.Store.GetAsync(id, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Option<IDeadLetterMessage>>(Option<IDeadLetterMessage>.Some(message)));
    }

    private static void ArrangePage(Rig rig, params IDeadLetterMessage[] messages)
        => rig.Store.GetMessagesAsync(Arg.Any<DeadLetterFilter>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IEnumerable<IDeadLetterMessage>>(messages));

    private static IEncina ArrangeEncina(Rig rig)
    {
        var encina = Substitute.For<IEncina>();
        encina.Send(Arg.Any<IRequest<int>>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, int>>(Right<EncinaError, int>(1)));
        rig.ServiceProvider.GetService(typeof(IEncina)).Returns(encina);
        return encina;
    }

    // ------------------------------------------------------------------ tenancy gate on filtered operations

    public static TheoryData<string> FilteredOperations => new()
    {
        "get_messages", "count", "delete_all", "replay_all"
    };

    private static Task<bool> RunFilteredAsync(Rig rig, string operation, DeadLetterFilter filter)
        => operation switch
        {
            "get_messages" => rig.Manager.GetMessagesAsync(filter).ContinueWith(t => t.Result.IsRight),
            "count" => rig.Manager.GetCountAsync(filter).ContinueWith(t => t.Result.IsRight),
            "delete_all" => rig.Manager.DeleteAllAsync(filter).ContinueWith(t => t.Result.IsRight),
            _ => rig.Manager.ReplayAllAsync(filter).ContinueWith(t => t.Result.IsRight)
        };

    [Theory]
    [MemberData(nameof(FilteredOperations))]
    public async Task FilteredOperation_MultiTenancyMarkerAndNoTenant_IsDeniedWithTheAuthorizationCodeAndNeverTouchesTheStore(string operation)
    {
        var rig = NewRig(tenancy: true);

        var succeeded = await RunFilteredAsync(rig, operation, DeadLetterFilter.All);

        succeeded.ShouldBeFalse();
        rig.Store.ReceivedCalls().Count(c => c.GetMethodInfo().Name is not "SaveChangesAsync").ShouldBe(0);
        rig.Logged(TenantRequiredDeniedEvent).ShouldBeTrue();
    }

    [Fact]
    public async Task GetMessagesAsync_MultiTenancyMarkerAndNoTenant_ReturnsTenantRequiredWhichIsAnAuthorizationCode()
    {
        var rig = NewRig(tenancy: true);

        var result = await rig.Manager.GetMessagesAsync();

        result.ShouldBeErrorWithCode(DeadLetterErrorCodes.TenantRequired);
        DeadLetterErrorCodes.TenantRequired.ShouldStartWith("encina.authorization.");
    }

    [Theory]
    [MemberData(nameof(FilteredOperations))]
    public async Task FilteredOperation_MultiTenancyMarkerAndAllTenants_RunsAcrossTenantsAndLogsTheOptOut(string operation)
    {
        var rig = NewRig(tenancy: true);

        var succeeded = await RunFilteredAsync(rig, operation, new DeadLetterFilter { AllTenants = true });

        succeeded.ShouldBeTrue();
        rig.Logged(AllTenantsOptOutEvent).ShouldBeTrue();
        rig.Logged(TenantRequiredDeniedEvent).ShouldBeFalse();
    }

    [Fact]
    public async Task GetMessagesAsync_MultiTenancyMarkerAndAllTenants_LeavesTheStoreFilterUnscoped()
    {
        var rig = NewRig(tenancy: true, ambientTenant: "tenant-a");

        await rig.Manager.GetMessagesAsync(new DeadLetterFilter { AllTenants = true });

        await rig.Store.Received(1).GetMessagesAsync(
            Arg.Is<DeadLetterFilter>(f => f.TenantId == null), 0, 100, false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMessagesAsync_MultiTenancyMarkerAndExplicitTenant_IsAllowedWithoutAmbientTenant()
    {
        var rig = NewRig(tenancy: true);

        var result = await rig.Manager.GetMessagesAsync(new DeadLetterFilter { TenantId = "tenant-b" });

        result.IsRight.ShouldBeTrue();
        await rig.Store.Received(1).GetMessagesAsync(
            Arg.Is<DeadLetterFilter>(f => f.TenantId == "tenant-b"), 0, 100, false, Arg.Any<CancellationToken>());
        rig.Logged(AllTenantsOptOutEvent).ShouldBeFalse();
    }

    [Fact]
    public async Task GetMessagesAsync_MultiTenancyMarkerAndAmbientTenant_ScopesToTheAmbientTenant()
    {
        var rig = NewRig(tenancy: true, ambientTenant: "tenant-a");

        await rig.Manager.GetMessagesAsync();

        await rig.Store.Received(1).GetMessagesAsync(
            Arg.Is<DeadLetterFilter>(f => f.TenantId == "tenant-a"), 0, 100, false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMessagesAsync_NoTenancyAndNoTenant_StaysUnscopedAndLogsNothing()
    {
        var rig = NewRig(tenancy: false);

        var result = await rig.Manager.GetMessagesAsync(new DeadLetterFilter { AllTenants = true });

        result.IsRight.ShouldBeTrue();
        await rig.Manager.GetCountAsync();
        rig.Logged(AllTenantsOptOutEvent).ShouldBeFalse();
        rig.Logged(TenantRequiredDeniedEvent).ShouldBeFalse();
    }

    [Fact]
    public async Task FilteredOperations_NoTenancyAndNoTenant_RunWithoutDenial()
    {
        var rig = NewRig(tenancy: false);

        (await rig.Manager.GetMessagesAsync()).IsRight.ShouldBeTrue();
        (await rig.Manager.GetCountAsync()).IsRight.ShouldBeTrue();
        (await rig.Manager.DeleteAllAsync(DeadLetterFilter.All)).IsRight.ShouldBeTrue();
        (await rig.Manager.ReplayAllAsync(DeadLetterFilter.All)).IsRight.ShouldBeTrue();
    }

    // ------------------------------------------------------------------ tenancy gate on operations by id

    [Fact]
    public async Task ReplayAsync_MultiTenancyMarkerAndNoTenant_IsDeniedBeforeAnyStoreCall()
    {
        var rig = NewRig(tenancy: true);

        var result = await rig.Manager.ReplayAsync(Guid.NewGuid());

        result.ShouldBeErrorWithCode(DeadLetterErrorCodes.TenantRequired);
        await rig.Store.DidNotReceiveWithAnyArgs().GetAsync(default, default);
    }

    [Fact]
    public async Task GetMessageAsync_MultiTenancyMarkerAndNoTenant_IsDenied()
    {
        var rig = NewRig(tenancy: true);

        var result = await rig.Manager.GetMessageAsync(Guid.NewGuid());

        result.ShouldBeErrorWithCode(DeadLetterErrorCodes.TenantRequired);
    }

    [Fact]
    public async Task DeleteAsync_MultiTenancyMarkerAndNoTenant_IsDeniedAndDeletesNothing()
    {
        var rig = NewRig(tenancy: true);

        var result = await rig.Manager.DeleteAsync(Guid.NewGuid());

        result.ShouldBeErrorWithCode(DeadLetterErrorCodes.TenantRequired);
        await rig.Store.DidNotReceiveWithAnyArgs().DeleteAsync(default, default);
    }

    [Fact]
    public async Task GetStatisticsAsync_MultiTenancyMarkerAndNoTenant_IsDenied()
    {
        var rig = NewRig(tenancy: true);

        var result = await rig.Manager.GetStatisticsAsync();

        result.ShouldBeErrorWithCode(DeadLetterErrorCodes.TenantRequired);
    }

    [Fact]
    public async Task GetMessageAsync_MultiTenancyMarkerAndAmbientTenant_ReadsTheMessage()
    {
        var rig = NewRig(tenancy: true, ambientTenant: "tenant-a");
        var id = Guid.NewGuid();
        ArrangeMessage(rig, id);

        var result = (await rig.Manager.GetMessageAsync(id)).ShouldBeRight();

        result.IsSome.ShouldBeTrue();
    }

    [Fact]
    public async Task GetMessageAsync_NoTenancyAndNoTenant_ReadsTheMessage()
    {
        var rig = NewRig(tenancy: false);
        var id = Guid.NewGuid();
        ArrangeMessage(rig, id);

        (await rig.Manager.GetMessageAsync(id)).ShouldBeRight().IsSome.ShouldBeTrue();
    }

    // ------------------------------------------------------------------ input rules on filters

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public async Task GetMessagesAsync_NonUtcInstantInTheFilter_Throws(DateTimeKind kind)
    {
        var rig = NewRig(tenancy: false);
        var instant = DateTime.SpecifyKind(new DateTime(2026, 1, 1, 0, 0, 0), kind);

        await Should.ThrowAsync<ArgumentException>(() => rig.Manager.GetMessagesAsync(new DeadLetterFilter { DeadLetteredAfterUtc = instant }));
        await Should.ThrowAsync<ArgumentException>(() => rig.Manager.GetCountAsync(new DeadLetterFilter { DeadLetteredBeforeUtc = instant }));
        await Should.ThrowAsync<ArgumentException>(() => rig.Manager.DeleteAllAsync(new DeadLetterFilter { ExpiresAtOrBeforeUtc = instant }));
        await rig.Store.DidNotReceiveWithAnyArgs().GetMessagesAsync(default!, default, default, default, default);
    }

    [Theory]
    [InlineData(" order-1")]
    [InlineData("order-1 ")]
    [InlineData("order-1\t")]
    public async Task GetCountAsync_IdentityValueWithEdgeWhiteSpace_Throws(string value)
    {
        var rig = NewRig(tenancy: false);

        await Should.ThrowAsync<ArgumentException>(() => rig.Manager.GetCountAsync(new DeadLetterFilter { SourceMessageId = value }));
        await Should.ThrowAsync<ArgumentException>(() => rig.Manager.GetCountAsync(new DeadLetterFilter { TenantId = value }));
        await Should.ThrowAsync<ArgumentException>(() => rig.Manager.GetCountAsync(new DeadLetterFilter { SourcePattern = value }));
        await Should.ThrowAsync<ArgumentException>(() => rig.Manager.GetCountAsync(new DeadLetterFilter { RequestType = value }));
    }

    [Fact]
    public async Task GetCountAsync_UtcInstantAndTrimmedValues_AreAccepted()
    {
        var rig = NewRig(tenancy: false);
        var filter = new DeadLetterFilter
        {
            SourceMessageId = "order 1",
            DeadLetteredAfterUtc = FixedUtcNow,
            DeadLetteredBeforeUtc = FixedUtcNow,
            ExpiresAtOrBeforeUtc = FixedUtcNow
        };

        (await rig.Manager.GetCountAsync(filter)).IsRight.ShouldBeTrue();
    }

    // ------------------------------------------------------------------ batch replay

    [Fact]
    public async Task ReplayAllAsync_WhenTheClaimOfALaterMessageFails_FailsTheOperationWithThatLeftAfterRecordingTheEarlierOne()
    {
        var rig = NewRig(tenancy: false);
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var third = Guid.NewGuid();
        ArrangePage(rig, Message(first), Message(second), Message(third));
        ArrangeEncina(rig);
        rig.Store.TryClaimForReplayAsync(second, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, bool>(EncinaErrors.Create("db.down", "down")));

        var result = await rig.Manager.ReplayAllAsync(DeadLetterFilter.All);

        result.ShouldBeErrorWithCode("db.down");
        await rig.Store.Received(1).MarkAsReplayedAsync(first, DeadLetterErrorCodes.ReplaySucceeded, Arg.Any<CancellationToken>());
        await rig.Store.DidNotReceive().TryClaimForReplayAsync(third, Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        rig.Logged(BatchReplayAbortedEvent).ShouldBeTrue();
    }

    [Fact]
    public async Task ReplayAllAsync_WhenRecordingTheOutcomeFails_FailsTheOperationWithThatLeft()
    {
        var rig = NewRig(tenancy: false);
        ArrangePage(rig, Message(Guid.NewGuid()));
        ArrangeEncina(rig);
        rig.Store.MarkAsReplayedAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, bool>(EncinaErrors.Create("db.write", "write failed")));

        var result = await rig.Manager.ReplayAllAsync(DeadLetterFilter.All);

        result.ShouldBeErrorWithCode("db.write");
    }

    [Fact]
    public async Task ReplayAllAsync_WhenAMessageCannotBeDeserialized_ReportsItAsFailedAndContinues()
    {
        var rig = NewRig(tenancy: false);
        var bad = Guid.NewGuid();
        var good = Guid.NewGuid();
        ArrangePage(rig, Message(bad, requestType: "No.Such.Type, Nowhere"), Message(good));
        ArrangeEncina(rig);

        var batch = (await rig.Manager.ReplayAllAsync(DeadLetterFilter.All)).ShouldBeRight();

        batch.TotalProcessed.ShouldBe(2);
        batch.SuccessCount.ShouldBe(1);
        batch.Results.Single(r => r.MessageId == bad).ErrorMessage.ShouldBe(DeadLetterErrorCodes.DeserializationFailed);
    }

    [Fact]
    public async Task ReplayAllAsync_AnExpiredMessage_IsAPerMessageFailureAndTheBatchContinues()
    {
        var rig = NewRig(tenancy: false);
        var expired = Guid.NewGuid();
        var good = Guid.NewGuid();
        ArrangePage(rig, Message(expired, expired: true), Message(good));
        ArrangeEncina(rig);

        var batch = (await rig.Manager.ReplayAllAsync(DeadLetterFilter.All)).ShouldBeRight();

        batch.SuccessCount.ShouldBe(1);
        batch.Results.Single(r => r.MessageId == expired).ErrorMessage.ShouldBe(DeadLetterErrorCodes.Expired);
    }

    [Fact]
    public async Task ReplayAllAsync_WhenTheStoreThrowsWhileRecordingTheOutcome_PropagatesInsteadOfRecordingAFailedReplay()
    {
        var rig = NewRig(tenancy: false);
        ArrangePage(rig, Message(Guid.NewGuid()));
        ArrangeEncina(rig);
        rig.Store.MarkAsReplayedAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task<Either<EncinaError, bool>>>(_ => throw new InvalidOperationException("connection lost"));

        await Should.ThrowAsync<InvalidOperationException>(() => rig.Manager.ReplayAllAsync(DeadLetterFilter.All));

        await rig.Store.Received(1).MarkAsReplayedAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReplayAllAsync_WhenTheCallerCancelsBetweenMessages_ThrowsInsteadOfReportingASuccess()
    {
        var rig = NewRig(tenancy: false);
        using var cts = new CancellationTokenSource();
        ArrangePage(rig, Message(Guid.NewGuid()), Message(Guid.NewGuid()));
        var encina = Substitute.For<IEncina>();
        encina.Send(Arg.Any<IRequest<int>>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cts.Cancel();
                return new ValueTask<Either<EncinaError, int>>(Right<EncinaError, int>(1));
            });
        rig.ServiceProvider.GetService(typeof(IEncina)).Returns(encina);

        await Should.ThrowAsync<OperationCanceledException>(() => rig.Manager.ReplayAllAsync(DeadLetterFilter.All, 100, cts.Token));
    }

    // ------------------------------------------------------------------ gate edge cases

    [Fact]
    public async Task GetMessagesAsync_MultiTenancyMarkerAndEmptyTenantId_IsDeniedLikeNoTenant()
    {
        var rig = NewRig(tenancy: true);

        var result = await rig.Manager.GetMessagesAsync(new DeadLetterFilter { TenantId = "" });

        result.ShouldBeErrorWithCode(DeadLetterErrorCodes.TenantRequired);
    }

    [Fact]
    public async Task GetMessagesAsync_AmbientTenantWithTrailingSpace_Throws()
    {
        var rig = NewRig(tenancy: true, ambientTenant: "tenant-a ");

        await Should.ThrowAsync<ArgumentException>(() => rig.Manager.GetMessagesAsync());
    }

    [Fact]
    public async Task AddAsync_AmbientTenantWithTrailingSpace_Throws()
    {
        var rig = NewRig(tenancy: false, ambientTenant: "tenant-a ");
        var orchestrator = new DeadLetterOrchestrator(
            rig.Store,
            Substitute.For<IDeadLetterMessageFactory>(),
            new DeadLetterOptions(),
            NullLogger<DeadLetterOrchestrator>.Instance,
            new JsonMessageSerializer(),
            AccessorFor("tenant-a "));

        await Should.ThrowAsync<ArgumentException>(() => orchestrator.AddAsync(
            new BatchCommand(1),
            new DeadLetterContext(EncinaErrors.Create("x", "y"), null, "Outbox", 1, FixedUtcNow, SourceMessageId: "m-1")));
    }

    private static IRequestContextAccessor AccessorFor(string tenantId)
    {
        var context = Substitute.For<IRequestContext>();
        context.TenantId.Returns(tenantId);
        var accessor = Substitute.For<IRequestContextAccessor>();
        accessor.RequestContext.Returns(context);
        return accessor;
    }

    [Fact]
    public async Task ReplayAsync_WhenThePayloadCannotBeDeserialized_IsAFailedReplayWithTheOutcomeRecorded()
    {
        var rig = NewRig(tenancy: false);
        var id = Guid.NewGuid();
        var message = Message(id, content: "not json");
        rig.Store.GetAsync(id, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Option<IDeadLetterMessage>>(Option<IDeadLetterMessage>.Some(message)));
        ArrangeEncina(rig);

        var result = await rig.Manager.ReplayAsync(id);

        result.IsLeft.ShouldBeTrue();
        await rig.Store.Received(1).MarkAsReplayedAsync(id, DeadLetterErrorCodes.ReplayFailed, Arg.Any<CancellationToken>());
    }

    // ------------------------------------------------------------------ cancellation

    [Fact]
    public async Task ReplayAsync_WhenTheHandlerRaisesItsOwnCancellation_IsAFailedReplayNotAPropagatedCancellation()
    {
        var rig = NewRig(tenancy: false);
        var id = Guid.NewGuid();
        ArrangeMessage(rig, id);
        var encina = Substitute.For<IEncina>();
        encina.Send(Arg.Any<IRequest<int>>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, int>>>(_ => throw new OperationCanceledException("handler timeout"));
        rig.ServiceProvider.GetService(typeof(IEncina)).Returns(encina);

        var result = (await rig.Manager.ReplayAsync(id)).ShouldBeRight();

        result.Success.ShouldBeFalse();
        await rig.Store.Received(1).MarkAsReplayedAsync(id, DeadLetterErrorCodes.ReplayFailed, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReplayAsync_WhenTheCallerCancels_PropagatesTheCancellation()
    {
        var rig = NewRig(tenancy: false);
        var id = Guid.NewGuid();
        using var cts = new CancellationTokenSource();
        ArrangeMessage(rig, id);
        var encina = Substitute.For<IEncina>();
        encina.Send(Arg.Any<IRequest<int>>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, int>>>(_ =>
            {
                cts.Cancel();
                throw new OperationCanceledException(cts.Token);
            });
        rig.ServiceProvider.GetService(typeof(IEncina)).Returns(encina);

        await Should.ThrowAsync<OperationCanceledException>(() => rig.Manager.ReplayAsync(id, cts.Token));
    }
}
