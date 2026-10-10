using Encina.Messaging.DeadLetter;
using Encina.Messaging.Recoverability;
using Encina.Testing.Shouldly;
using LanguageExt;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.Messaging.DeadLetter;

/// <summary>
/// Tests for <see cref="DeadLetterSourceCapture"/>: the flag of each source, idempotency and failure reporting.
/// </summary>
public sealed class DeadLetterSourceCaptureTests
{
    private sealed record SampleRequest(int Value) : IRequest<int>;

    public static TheoryData<string, Action<DeadLetterOptions>> FlagsOff => new()
    {
        { DeadLetterSourcePatterns.Recoverability, o => o.IntegrateWithRecoverability = false },
        { DeadLetterSourcePatterns.Outbox, o => o.IntegrateWithOutbox = false },
        { DeadLetterSourcePatterns.Inbox, o => o.IntegrateWithInbox = false },
        { DeadLetterSourcePatterns.Scheduling, o => o.IntegrateWithScheduling = false },
        { DeadLetterSourcePatterns.Saga, o => o.IntegrateWithSagas = false }
    };

    [Theory]
    [InlineData(DeadLetterSourcePatterns.Recoverability)]
    [InlineData(DeadLetterSourcePatterns.Outbox)]
    [InlineData(DeadLetterSourcePatterns.Inbox)]
    [InlineData(DeadLetterSourcePatterns.Scheduling)]
    [InlineData(DeadLetterSourcePatterns.Saga)]
    public void IsEnabledFor_BuiltInSource_IsOnByDefault(string sourcePattern)
    {
        using var host = DeadLetterCaptureHost.Create();

        host.Capture.IsEnabledFor(sourcePattern).ShouldBeTrue();
    }

    [Theory]
    [MemberData(nameof(FlagsOff))]
    public void IsEnabledFor_SourceWithItsFlagOff_IsFalse(string sourcePattern, Action<DeadLetterOptions> configure)
    {
        using var host = DeadLetterCaptureHost.Create(configure);

        host.Capture.IsEnabledFor(sourcePattern).ShouldBeFalse();
    }

    [Theory]
    [InlineData(DeadLetterSourcePatterns.Choreography)]
    [InlineData("Custom")]
    public void IsEnabledFor_OtherPattern_IsFalse(string sourcePattern)
    {
        using var host = DeadLetterCaptureHost.Create();

        host.Capture.IsEnabledFor(sourcePattern).ShouldBeFalse();
    }

    [Fact]
    public async Task CaptureAsync_FlagOn_StoresOneDeadLetterWithTheSourceKeyAndTenant()
    {
        using var host = DeadLetterCaptureHost.Create();

        var result = await host.Capture.CaptureAsync(new SampleRequest(1), Context(DeadLetterSourcePatterns.Inbox, "msg-1", tenantId: "tenant-a"));

        result.ShouldBeRight();
        var stored = host.DeadLettersOf(DeadLetterSourcePatterns.Inbox).ShouldHaveSingleItem();
        stored.SourceMessageId.ShouldBe("msg-1");
        stored.TenantId.ShouldBe("tenant-a");
        stored.ErrorCode.ShouldBe("sample.failed");
        stored.RequestType.ShouldContain(nameof(SampleRequest));
    }

    [Fact]
    public async Task CaptureAsync_SameSourceMessageTwice_KeepsOneDeadLetter()
    {
        using var host = DeadLetterCaptureHost.Create();

        (await host.Capture.CaptureAsync(new SampleRequest(1), Context(DeadLetterSourcePatterns.Inbox, "msg-1"))).ShouldBeRight();
        (await host.Capture.CaptureAsync(new SampleRequest(1), Context(DeadLetterSourcePatterns.Inbox, "msg-1"))).ShouldBeRight();

        host.DeadLettersOf(DeadLetterSourcePatterns.Inbox).Count.ShouldBe(1);
    }

    [Theory]
    [MemberData(nameof(FlagsOff))]
    public async Task CaptureAsync_FlagOff_StoresNothingAndSucceeds(string sourcePattern, Action<DeadLetterOptions> configure)
    {
        using var host = DeadLetterCaptureHost.Create(configure);

        var result = await host.Capture.CaptureAsync(new SampleRequest(1), Context(sourcePattern, "msg-1"));

        result.ShouldBeRight();
        host.Store.GetMessages().ShouldBeEmpty();
    }

    [Fact]
    public async Task CaptureSerializedAsync_KeepsTheStoredTypeNameAndContentAsTheyAre()
    {
        using var host = DeadLetterCaptureHost.Create();

        var result = await host.Capture.CaptureSerializedAsync(
            "Some.Type, Some.Assembly", "{\"encrypted\":\"payload\"}", Context(DeadLetterSourcePatterns.Outbox, "row-1"));

        result.ShouldBeRight();
        var stored = host.DeadLettersOf(DeadLetterSourcePatterns.Outbox).ShouldHaveSingleItem();
        stored.RequestType.ShouldBe("Some.Type, Some.Assembly");
        stored.RequestContent.ShouldBe("{\"encrypted\":\"payload\"}");
    }

    [Fact]
    public async Task CaptureFailedMessageAsync_KeysTheDeadLetterByTheChainId()
    {
        using var host = DeadLetterCaptureHost.Create();
        var failed = FailedMessageOf(new SampleRequest(3), host.Clock.GetUtcNow().UtcDateTime);

        (await host.Capture.CaptureFailedMessageAsync(failed)).ShouldBeRight();
        (await host.Capture.CaptureFailedMessageAsync(failed)).ShouldBeRight();

        var stored = host.DeadLettersOf(DeadLetterSourcePatterns.Recoverability).ShouldHaveSingleItem();
        stored.SourceMessageId.ShouldBe(failed.Id.ToString("D"));
        stored.TotalRetryAttempts.ShouldBe(4);
    }

    [Fact]
    public async Task CaptureFailedMessageAsync_FlagOff_StoresNothing()
    {
        using var host = DeadLetterCaptureHost.Create(o => o.IntegrateWithRecoverability = false);

        (await host.Capture.CaptureFailedMessageAsync(FailedMessageOf(new SampleRequest(3), host.Clock.GetUtcNow().UtcDateTime))).ShouldBeRight();

        host.Store.GetMessages().ShouldBeEmpty();
    }

    [Fact]
    public async Task CaptureAsync_StoreReturnsLeft_ReturnsTheStoreError()
    {
        var store = Substitute.For<IDeadLetterStore>();
        store.AddAsync(Arg.Any<IDeadLetterMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Either<EncinaError, bool>>(EncinaErrors.Create(DeadLetterErrorCodes.StoreFailed, "down")));
        using var host = DeadLetterCaptureHost.Create(store: store);

        var result = await host.Capture.CaptureAsync(new SampleRequest(1), Context(DeadLetterSourcePatterns.Inbox, "msg-1"));

        result.ShouldBeErrorWithCode(DeadLetterErrorCodes.StoreFailed);
    }

    [Fact]
    public async Task CaptureAsync_CaptureThrows_ReturnsCaptureFailed()
    {
        using var host = DeadLetterCaptureHost.Create();

        // An identity value with edge white space is rejected by the orchestrator (ArgumentException).
        var result = await host.Capture.CaptureAsync(new SampleRequest(1), Context(DeadLetterSourcePatterns.Inbox, " padded "));

        result.ShouldBeErrorWithCode(DeadLetterErrorCodes.CaptureFailed);
        host.Store.GetMessages().ShouldBeEmpty();
    }

    [Fact]
    public async Task CaptureAsync_CallerCancelled_PropagatesTheCancellation()
    {
        var store = Substitute.For<IDeadLetterStore>();
        store.AddAsync(Arg.Any<IDeadLetterMessage>(), Arg.Any<CancellationToken>())
            .Returns<Task<Either<EncinaError, bool>>>(_ => throw new OperationCanceledException());
        using var host = DeadLetterCaptureHost.Create(store: store);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(
            () => host.Capture.CaptureAsync(new SampleRequest(1), Context(DeadLetterSourcePatterns.Inbox, "msg-1"), cts.Token));
    }

    [Fact]
    public void AsUtc_StoredColumnWithEveryKind_IsReturnedAsTheSameUtcInstant()
    {
        var utc = new DateTime(2026, 5, 1, 7, 0, 0, DateTimeKind.Utc);

        DeadLetterInputs.AsUtc(utc).ShouldBe(utc);
        DeadLetterInputs.AsUtc(DateTime.SpecifyKind(utc, DateTimeKind.Unspecified)).ShouldSatisfyAllConditions(
            value => value.Kind.ShouldBe(DateTimeKind.Utc),
            value => value.Ticks.ShouldBe(utc.Ticks));
        var local = utc.ToLocalTime();
        DeadLetterInputs.AsUtc(local).ShouldSatisfyAllConditions(
            value => value.Kind.ShouldBe(DateTimeKind.Utc),
            value => value.ShouldBe(utc));
    }

    private static DeadLetterContext Context(string sourcePattern, string sourceMessageId, string? tenantId = null)
        => new(
            EncinaErrors.Create("sample.failed", "failure"),
            Exception: null,
            sourcePattern,
            TotalRetryAttempts: 2,
            FirstFailedAtUtc: new DateTime(2026, 5, 1, 7, 0, 0, DateTimeKind.Utc),
            SourceMessageId: sourceMessageId,
            TenantId: tenantId);

    private static FailedMessage FailedMessageOf(object request, DateTime nowUtc) => new()
    {
        Id = Guid.NewGuid(),
        Request = request,
        RequestType = request.GetType().AssemblyQualifiedName!,
        Error = EncinaErrors.Create("sample.permanent", "failure"),
        TotalAttempts = 4,
        ImmediateRetryAttempts = 3,
        DelayedRetryAttempts = 0,
        FirstAttemptAtUtc = nowUtc,
        FailedAtUtc = nowUtc
    };
}
