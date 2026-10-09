using Encina.Messaging.Inbox;
using Encina.Messaging.Serialization;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.UnitTests.Messaging.Inbox;

/// <summary>
/// Proves the retry contract of <see cref="InboxOrchestrator"/> (#2084): the handler runs exactly
/// <see cref="InboxOptions.MaxRetries"/> times, one increment per failed attempt, and every store
/// <c>Left</c> fails the operation.
/// </summary>
public sealed class InboxOrchestratorRetryTests
{
    private const string MessageId = "msg-2084";

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public async Task ProcessAsync_HandlerAlwaysThrows_RunsHandlerMaxRetriesTimesThenRejects(int maxRetries)
    {
        var store = new StatefulInboxStore();
        var orchestrator = CreateOrchestrator(store, maxRetries);
        var runs = 0;

        Func<ValueTask<Either<EncinaError, string>>> handler = () =>
        {
            runs++;
            throw new InvalidOperationException("boom");
        };

        for (var i = 0; i < maxRetries; i++)
        {
            var failed = await orchestrator.ProcessAsync(MessageId, "Req", "corr", null, handler);
            failed.IsLeft.ShouldBeTrue();
            failed.LeftToArray()[0].GetCode().IfNone(string.Empty).ShouldBe("inbox.processing_failed");
        }

        runs.ShouldBe(maxRetries);
        store.Message!.RetryCount.ShouldBe(maxRetries);

        var rejected = await orchestrator.ProcessAsync(MessageId, "Req", "corr", null, handler);

        rejected.IsLeft.ShouldBeTrue();
        rejected.LeftToArray()[0].GetCode().IfNone(string.Empty).ShouldBe(InboxErrorCodes.MaxRetriesExceeded);
        runs.ShouldBe(maxRetries);
    }

    [Fact]
    public async Task ProcessAsync_HandlerReturnsLeft_CachesResponseAndDoesNotRunHandlerOnRedelivery()
    {
        var store = new StatefulInboxStore();
        var orchestrator = CreateOrchestrator(store, 3);
        var runs = 0;

        Func<ValueTask<Either<EncinaError, string>>> handler = () =>
        {
            runs++;
            return ValueTask.FromResult<Either<EncinaError, string>>(EncinaErrors.Create("biz.rule", "business rule"));
        };

        var first = await orchestrator.ProcessAsync(MessageId, "Req", "corr", null, handler);
        var second = await orchestrator.ProcessAsync(MessageId, "Req", "corr", null, handler);

        first.IsLeft.ShouldBeTrue();
        second.IsLeft.ShouldBeTrue();
        second.LeftToArray()[0].GetCode().IfNone(string.Empty).ShouldBe(InboxErrorCodes.CachedError);
        runs.ShouldBe(1);
        store.Message!.RetryCount.ShouldBe(0);
        store.Message.IsProcessed.ShouldBeTrue();
    }

    [Fact]
    public async Task ProcessAsync_CallerCancels_PropagatesCancellationAndDoesNotCountAttempt()
    {
        var store = new StatefulInboxStore();
        var orchestrator = CreateOrchestrator(store, 3);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = async () => await orchestrator.ProcessAsync<string>(
            MessageId, "Req", "corr", null, () => throw new OperationCanceledException(cts.Token), cts.Token);

        await act.ShouldThrowAsync<OperationCanceledException>();
        store.Message!.RetryCount.ShouldBe(0);
    }

    [Fact]
    public async Task ProcessAsync_HandlerThrowsOperationCanceledWithoutCallerCancel_CountsAsFailedAttempt()
    {
        var store = new StatefulInboxStore();
        var orchestrator = CreateOrchestrator(store, 3);

        var result = await orchestrator.ProcessAsync<string>(
            MessageId, "Req", "corr", null, () => throw new OperationCanceledException());

        result.IsLeft.ShouldBeTrue();
        store.Message!.RetryCount.ShouldBe(1);
    }

    [Fact]
    public async Task ProcessAsync_CachedSuccessEqualToDefault_IsReturnedAsThatSuccess()
    {
        var store = new StatefulInboxStore();
        var orchestrator = CreateOrchestrator(store, 3);
        var runs = 0;

        Func<ValueTask<Either<EncinaError, int>>> handler = () =>
        {
            runs++;
            return ValueTask.FromResult<Either<EncinaError, int>>(0);
        };

        await orchestrator.ProcessAsync(MessageId, "Req", "corr", null, handler);
        var second = await orchestrator.ProcessAsync(MessageId, "Req", "corr", null, handler);

        runs.ShouldBe(1);
        second.IsRight.ShouldBeTrue();
        second.RightToArray()[0].ShouldBe(0);
    }

    [Fact]
    public async Task ProcessAsync_GetMessageLeft_ReturnsLeftWithoutRunningHandler()
    {
        var store = new StatefulInboxStore { FailGet = true };
        var (result, runs) = await RunSuccessHandlerAsync(store);

        result.IsLeft.ShouldBeTrue();
        runs.ShouldBe(0);
    }

    [Fact]
    public async Task ProcessAsync_AddLeft_ReturnsLeftWithoutRunningHandler()
    {
        var store = new StatefulInboxStore { FailAdd = true };
        var (result, runs) = await RunSuccessHandlerAsync(store);

        result.IsLeft.ShouldBeTrue();
        runs.ShouldBe(0);
    }

    [Fact]
    public async Task ProcessAsync_MarkAsProcessedLeft_ReturnsStoreErrorNotSuccess()
    {
        var store = new StatefulInboxStore { FailMarkProcessed = true };
        var (result, runs) = await RunSuccessHandlerAsync(store);

        result.IsLeft.ShouldBeTrue();
        result.LeftToArray()[0].GetCode().IfNone(string.Empty).ShouldBe("test.mark_processed");
        runs.ShouldBe(1);
        store.Message!.IsProcessed.ShouldBeFalse();
    }

    [Fact]
    public async Task ProcessAsync_MarkAsFailedLeft_ReturnsStoreErrorNotProcessingFailed()
    {
        var store = new StatefulInboxStore { FailMarkFailed = true };
        var orchestrator = CreateOrchestrator(store, 3);

        var result = await orchestrator.ProcessAsync<string>(
            MessageId, "Req", "corr", null, () => throw new InvalidOperationException("boom"));

        result.IsLeft.ShouldBeTrue();
        result.LeftToArray()[0].GetCode().IfNone(string.Empty).ShouldBe("test.mark_failed");
    }

    private static async Task<(Either<EncinaError, string> Result, int Runs)> RunSuccessHandlerAsync(StatefulInboxStore store)
    {
        var orchestrator = CreateOrchestrator(store, 3);
        var runs = 0;
        var result = await orchestrator.ProcessAsync(MessageId, "Req", "corr", null, () =>
        {
            runs++;
            return ValueTask.FromResult<Either<EncinaError, string>>("ok");
        });
        return (result, runs);
    }

    private static InboxOrchestrator CreateOrchestrator(StatefulInboxStore store, int maxRetries) =>
        new(store,
            new InboxOptions { MaxRetries = maxRetries },
            NullLogger<InboxOrchestrator>.Instance,
            new StatefulMessageFactory(),
            new JsonMessageSerializer());

    private sealed class StatefulMessageFactory : IInboxMessageFactory
    {
        public IInboxMessage Create(string messageId, string requestType, DateTime receivedAtUtc, DateTime expiresAtUtc, InboxMetadata? metadata) =>
            new TestInboxMessage
            {
                MessageId = messageId,
                RequestType = requestType,
                ReceivedAtUtc = receivedAtUtc,
                ExpiresAtUtc = expiresAtUtc
            };
    }

    /// <summary>Single-message store with the production contract: only MarkAsFailedAsync increments RetryCount.</summary>
    private sealed class StatefulInboxStore : IInboxStore
    {
        public TestInboxMessage? Message { get; private set; }
        public bool FailGet { get; init; }
        public bool FailAdd { get; init; }
        public bool FailMarkProcessed { get; init; }
        public bool FailMarkFailed { get; init; }

        public Task<Either<EncinaError, Option<IInboxMessage>>> GetMessageAsync(string messageId, CancellationToken cancellationToken = default)
        {
            if (FailGet)
                return Task.FromResult<Either<EncinaError, Option<IInboxMessage>>>(EncinaErrors.Create("test.get", "get"));

            Option<IInboxMessage> found = Message is null ? Option<IInboxMessage>.None : Option<IInboxMessage>.Some(Message);
            return Task.FromResult<Either<EncinaError, Option<IInboxMessage>>>(found);
        }

        public Task<Either<EncinaError, Unit>> AddAsync(IInboxMessage message, CancellationToken cancellationToken = default)
        {
            if (FailAdd)
                return Task.FromResult<Either<EncinaError, Unit>>(EncinaErrors.Create("test.add", "add"));

            Message = (TestInboxMessage)message;
            return Task.FromResult<Either<EncinaError, Unit>>(Unit.Default);
        }

        public Task<Either<EncinaError, Unit>> MarkAsProcessedAsync(string messageId, string response, CancellationToken cancellationToken = default)
        {
            if (FailMarkProcessed)
                return Task.FromResult<Either<EncinaError, Unit>>(EncinaErrors.Create("test.mark_processed", "mark processed"));

            Message!.Response = response;
            Message.ProcessedAtUtc = DateTime.UtcNow;
            return Task.FromResult<Either<EncinaError, Unit>>(Unit.Default);
        }

        public Task<Either<EncinaError, Unit>> MarkAsFailedAsync(string messageId, string errorMessage, DateTime? nextRetryAtUtc, CancellationToken cancellationToken = default)
        {
            if (FailMarkFailed)
                return Task.FromResult<Either<EncinaError, Unit>>(EncinaErrors.Create("test.mark_failed", "mark failed"));

            Message!.ErrorMessage = errorMessage;
            Message.RetryCount++;
            Message.NextRetryAtUtc = nextRetryAtUtc;
            return Task.FromResult<Either<EncinaError, Unit>>(Unit.Default);
        }

        public Task<Either<EncinaError, IEnumerable<IInboxMessage>>> GetExpiredMessagesAsync(int batchSize, CancellationToken cancellationToken = default) =>
            Task.FromResult<Either<EncinaError, IEnumerable<IInboxMessage>>>(Array.Empty<IInboxMessage>());

        public Task<Either<EncinaError, Unit>> RemoveExpiredMessagesAsync(IEnumerable<string> messageIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<Either<EncinaError, Unit>>(Unit.Default);

        public Task<Either<EncinaError, Unit>> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<Either<EncinaError, Unit>>(Unit.Default);
    }
}
