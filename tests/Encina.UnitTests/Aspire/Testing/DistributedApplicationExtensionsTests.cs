using Encina.Aspire.Testing;
using Encina.Messaging.Sagas;
using Encina.Testing.Fakes.Models;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Encina.UnitTests.Aspire.Testing;

/// <summary>
/// Unit tests for <see cref="DistributedApplicationExtensions"/> run against a real
/// <c>DistributedApplication</c> over a host that is never started; the fake stores are the
/// real <c>Encina.Testing.Fakes</c> implementations.
/// </summary>
public sealed class DistributedApplicationExtensionsTests
{
    private sealed class OrderPlaced;

    private sealed class PaymentSaga;

    private sealed class OtherSaga;

    private sealed class PlaceOrder;

    private static readonly TimeSpan ShortTimeout = TimeSpan.FromMilliseconds(60);

    private static FakeOutboxMessage OutboxMessage<TNotification>() => new()
    {
        NotificationType = typeof(TNotification).FullName!,
        Content = "{}"
    };

    private static FakeInboxMessage InboxMessage(string messageId, string requestType) => new()
    {
        MessageId = messageId,
        RequestType = requestType
    };

    // ─── Service access ───

    [Fact]
    public void GetEncinaTestContext_Registered_ReturnsContext()
    {
        using var host = AspireTestApp.Create();

        var context = host.App.GetEncinaTestContext();

        context.ShouldBeSameAs(host.Services.GetRequiredService<EncinaTestContext>());
        context.OutboxStore.ShouldBeSameAs(host.Outbox);
    }

    [Fact]
    public void GetEncinaTestContext_NotRegistered_ThrowsInvalidOperationException()
    {
        using var host = AspireTestApp.Create(registerTestSupport: false);

        Should.Throw<InvalidOperationException>(() => host.App.GetEncinaTestContext());
    }

    [Fact]
    public void GetStores_Registered_ReturnTheRegisteredInstances()
    {
        using var host = AspireTestApp.Create();

        host.App.GetOutboxStore().ShouldBeSameAs(host.Outbox);
        host.App.GetInboxStore().ShouldBeSameAs(host.Inbox);
        host.App.GetSagaStore().ShouldBeSameAs(host.Sagas);
    }

    // ─── Outbox ───

    [Fact]
    public async Task AssertOutboxContainsAsync_Generic_MessageAdded_Completes()
    {
        using var host = AspireTestApp.Create();
        await host.Outbox.AddAsync(OutboxMessage<OrderPlaced>());

        await host.App.AssertOutboxContainsAsync<OrderPlaced>();
    }

    [Fact]
    public async Task AssertOutboxContainsAsync_Generic_MessageMissing_ThrowsTimeoutException()
    {
        using var host = AspireTestApp.Create();

        var ex = await Should.ThrowAsync<TimeoutException>(
            () => host.App.AssertOutboxContainsAsync<OrderPlaced>(ShortTimeout));

        ex.Message.ShouldContain(typeof(OrderPlaced).FullName!);
    }

    [Fact]
    public async Task AssertOutboxContainsAsync_Generic_MessageArrivesLater_Completes()
    {
        using var host = AspireTestApp.Create();

        var assertion = host.App.AssertOutboxContainsAsync<OrderPlaced>(TimeSpan.FromSeconds(30));
        await host.Outbox.AddAsync(OutboxMessage<OrderPlaced>());

        await assertion;
    }

    [Fact]
    public async Task AssertOutboxContainsAsync_Generic_CallerCancels_ThrowsOperationCanceledNotTimeout()
    {
        using var host = AspireTestApp.Create();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(
            () => host.App.AssertOutboxContainsAsync<OrderPlaced>(TimeSpan.FromSeconds(30), cts.Token));
    }

    [Fact]
    public async Task AssertOutboxContainsAsync_Predicate_Matching_Completes()
    {
        using var host = AspireTestApp.Create();
        var message = OutboxMessage<OrderPlaced>();
        message.Content = "marker-content";
        await host.Outbox.AddAsync(message);

        await host.App.AssertOutboxContainsAsync(m => m.Content == "marker-content");
    }

    [Fact]
    public async Task AssertOutboxContainsAsync_Predicate_NoMatch_ThrowsTimeoutException()
    {
        using var host = AspireTestApp.Create();
        await host.Outbox.AddAsync(OutboxMessage<OrderPlaced>());

        await Should.ThrowAsync<TimeoutException>(
            () => host.App.AssertOutboxContainsAsync(m => m.Content == "absent", ShortTimeout));
    }

    [Fact]
    public async Task AssertOutboxContainsAsync_Predicate_NullPredicate_ThrowsArgumentNullException()
    {
        using var host = AspireTestApp.Create();

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            () => host.App.AssertOutboxContainsAsync((Func<global::Encina.Messaging.Outbox.IOutboxMessage, bool>)null!));

        ex.ParamName.ShouldBe("predicate");
    }

    [Fact]
    public async Task GetPendingOutboxMessages_ReturnsOnlyUnprocessed()
    {
        using var host = AspireTestApp.Create();
        var pending = OutboxMessage<OrderPlaced>();
        var processed = OutboxMessage<OrderPlaced>();
        await host.Outbox.AddAsync(pending);
        await host.Outbox.AddAsync(processed);
        await host.Outbox.MarkAsProcessedAsync(processed.Id);

        var result = host.App.GetPendingOutboxMessages();

        result.Count.ShouldBe(1);
        result.Single().Id.ShouldBe(pending.Id);
    }

    [Fact]
    public void GetPendingOutboxMessages_EmptyStore_ReturnsEmpty()
    {
        using var host = AspireTestApp.Create();

        host.App.GetPendingOutboxMessages().ShouldBeEmpty();
    }

    // ─── Inbox ───

    [Fact]
    public async Task AssertInboxProcessedAsync_ById_Processed_Completes()
    {
        using var host = AspireTestApp.Create();
        await host.Inbox.AddAsync(InboxMessage("msg-1", "req"));
        await host.Inbox.MarkAsProcessedAsync("msg-1", "ok");

        await host.App.AssertInboxProcessedAsync("msg-1");
    }

    [Fact]
    public async Task AssertInboxProcessedAsync_ById_NotProcessed_ThrowsTimeoutException()
    {
        using var host = AspireTestApp.Create();
        await host.Inbox.AddAsync(InboxMessage("msg-1", "req"));

        var ex = await Should.ThrowAsync<TimeoutException>(
            () => host.App.AssertInboxProcessedAsync("msg-1", ShortTimeout));

        ex.Message.ShouldContain("msg-1");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AssertInboxProcessedAsync_ById_InvalidId_Throws(string? messageId)
    {
        using var host = AspireTestApp.Create();

        await Should.ThrowAsync<ArgumentException>(
            () => host.App.AssertInboxProcessedAsync(messageId!));
    }

    [Fact]
    public async Task AssertInboxProcessedAsync_Generic_ProcessedOfType_Completes()
    {
        using var host = AspireTestApp.Create();
        await host.Inbox.AddAsync(InboxMessage("msg-2", typeof(PlaceOrder).FullName!));
        await host.Inbox.MarkAsProcessedAsync("msg-2", "ok");

        await host.App.AssertInboxProcessedAsync<PlaceOrder>();
    }

    [Fact]
    public async Task AssertInboxProcessedAsync_Generic_OtherTypeProcessed_ThrowsTimeoutException()
    {
        using var host = AspireTestApp.Create();
        await host.Inbox.AddAsync(InboxMessage("msg-3", "Some.Other.Type"));
        await host.Inbox.MarkAsProcessedAsync("msg-3", "ok");

        await Should.ThrowAsync<TimeoutException>(
            () => host.App.AssertInboxProcessedAsync<PlaceOrder>(ShortTimeout));
    }

    // ─── Sagas ───

    [Fact]
    public async Task AssertSagaCompletedAsync_CompletedSaga_Completes()
    {
        using var host = AspireTestApp.Create();
        host.AddSaga<PaymentSaga>(SagaStatus.Completed);

        await host.App.AssertSagaCompletedAsync<PaymentSaga>();
    }

    [Fact]
    public async Task AssertSagaCompletedAsync_OnlyRunningSaga_ThrowsTimeoutException()
    {
        using var host = AspireTestApp.Create();
        host.AddSaga<PaymentSaga>(SagaStatus.Running);

        var ex = await Should.ThrowAsync<TimeoutException>(
            () => host.App.AssertSagaCompletedAsync<PaymentSaga>(timeout: ShortTimeout));

        ex.Message.ShouldContain(typeof(PaymentSaga).FullName!);
    }

    [Fact]
    public async Task AssertSagaCompletedAsync_CompletedSagaOfOtherType_ThrowsTimeoutException()
    {
        using var host = AspireTestApp.Create();
        host.AddSaga<OtherSaga>(SagaStatus.Completed);

        await Should.ThrowAsync<TimeoutException>(
            () => host.App.AssertSagaCompletedAsync<PaymentSaga>(timeout: ShortTimeout));
    }

    [Fact]
    public async Task AssertSagaCompletedAsync_PredicateFiltersSagas()
    {
        using var host = AspireTestApp.Create();
        var matching = host.AddSaga<PaymentSaga>(SagaStatus.Completed);
        matching.Data = "{\"id\":42}";

        await host.App.AssertSagaCompletedAsync<PaymentSaga>(s => s.Data.Contains("42"));
        await Should.ThrowAsync<TimeoutException>(
            () => host.App.AssertSagaCompletedAsync<PaymentSaga>(s => s.Data.Contains("99"), ShortTimeout));
    }

    [Theory]
    [InlineData(SagaStatus.Compensating)]
    [InlineData(SagaStatus.Failed)]
    public async Task AssertSagaCompensatedAsync_CompensatingOrFailed_Completes(string status)
    {
        using var host = AspireTestApp.Create();
        host.AddSaga<PaymentSaga>(status);

        await host.App.AssertSagaCompensatedAsync<PaymentSaga>();
    }

    [Fact]
    public async Task AssertSagaCompensatedAsync_OnlyRunningSaga_ThrowsTimeoutException()
    {
        using var host = AspireTestApp.Create();
        host.AddSaga<PaymentSaga>(SagaStatus.Running);

        await Should.ThrowAsync<TimeoutException>(
            () => host.App.AssertSagaCompensatedAsync<PaymentSaga>(timeout: ShortTimeout));
    }

    [Fact]
    public async Task AssertSagaCompensatedAsync_PredicateFiltersSagas()
    {
        using var host = AspireTestApp.Create();
        var failed = host.AddSaga<PaymentSaga>(SagaStatus.Failed);
        failed.Data = "{\"id\":7}";

        await host.App.AssertSagaCompensatedAsync<PaymentSaga>(s => s.Data.Contains('7'));
        await Should.ThrowAsync<TimeoutException>(
            () => host.App.AssertSagaCompensatedAsync<PaymentSaga>(s => s.Data.Contains('8'), ShortTimeout));
    }

    [Fact]
    public void GetRunningSagas_ReturnsOnlyRunningSagasOfRequestedType()
    {
        using var host = AspireTestApp.Create();
        var running = host.AddSaga<PaymentSaga>(SagaStatus.Running);
        host.AddSaga<PaymentSaga>(SagaStatus.Completed);
        host.AddSaga<OtherSaga>(SagaStatus.Running);

        var result = host.App.GetRunningSagas<PaymentSaga>();

        result.Count.ShouldBe(1);
        result[0].SagaId.ShouldBe(running.SagaId);
    }

    [Fact]
    public void GetRunningSagas_NoSagas_ReturnsEmpty()
    {
        using var host = AspireTestApp.Create();

        host.App.GetRunningSagas<PaymentSaga>().ShouldBeEmpty();
    }

    // ─── Dead letters ───

    [Fact]
    public async Task AssertDeadLetterContainsAsync_MessageAdded_Completes()
    {
        using var host = AspireTestApp.Create();
        await host.App.AddToDeadLetterAsync(typeof(PlaceOrder).FullName!, "{}");

        await host.App.AssertDeadLetterContainsAsync<PlaceOrder>();
    }

    [Fact]
    public async Task AssertDeadLetterContainsAsync_Missing_ThrowsTimeoutException()
    {
        using var host = AspireTestApp.Create();
        await host.App.AddToDeadLetterAsync("Some.Other.Type", "{}");

        await Should.ThrowAsync<TimeoutException>(
            () => host.App.AssertDeadLetterContainsAsync<PlaceOrder>(ShortTimeout));
    }

    [Fact]
    public async Task GetDeadLetterMessages_ReturnsStoredMessages()
    {
        using var host = AspireTestApp.Create();
        var id = await host.App.AddToDeadLetterAsync("Type.A", "{}");

        var result = host.App.GetDeadLetterMessages();

        result.Count.ShouldBe(1);
        result.Single().Id.ShouldBe(id);
    }

    // ─── Wait helpers ───

    [Fact]
    public async Task WaitForOutboxProcessingAsync_ZeroExpected_CompletesImmediately()
    {
        using var host = AspireTestApp.Create();

        await host.App.WaitForOutboxProcessingAsync(0);
    }

    [Fact]
    public async Task WaitForOutboxProcessingAsync_EnoughProcessed_Completes()
    {
        using var host = AspireTestApp.Create();
        var first = OutboxMessage<OrderPlaced>();
        var second = OutboxMessage<OrderPlaced>();
        await host.Outbox.AddAsync(first);
        await host.Outbox.AddAsync(second);
        await host.Outbox.MarkAsProcessedAsync(first.Id);
        await host.Outbox.MarkAsProcessedAsync(second.Id);

        await host.App.WaitForOutboxProcessingAsync(2);
    }

    [Fact]
    public async Task WaitForOutboxProcessingAsync_NotEnoughProcessed_ThrowsTimeoutException()
    {
        using var host = AspireTestApp.Create();
        var only = OutboxMessage<OrderPlaced>();
        await host.Outbox.AddAsync(only);
        await host.Outbox.MarkAsProcessedAsync(only.Id);

        var ex = await Should.ThrowAsync<TimeoutException>(
            () => host.App.WaitForOutboxProcessingAsync(2, ShortTimeout));

        ex.Message.ShouldContain("2");
    }

    [Fact]
    public async Task WaitForOutboxProcessingAsync_NegativeCount_ThrowsArgumentOutOfRangeException()
    {
        using var host = AspireTestApp.Create();

        await Should.ThrowAsync<ArgumentOutOfRangeException>(
            () => host.App.WaitForOutboxProcessingAsync(-1));
    }

    [Fact]
    public async Task WaitForSagaCompletionAsync_CompletedSaga_Completes()
    {
        using var host = AspireTestApp.Create();
        var saga = host.AddSaga<PaymentSaga>(SagaStatus.Completed);

        await host.App.WaitForSagaCompletionAsync<PaymentSaga>(saga.SagaId);
    }

    [Fact]
    public async Task WaitForSagaCompletionAsync_RunningSaga_ThrowsTimeoutException()
    {
        using var host = AspireTestApp.Create();
        var saga = host.AddSaga<PaymentSaga>(SagaStatus.Running);

        var ex = await Should.ThrowAsync<TimeoutException>(
            () => host.App.WaitForSagaCompletionAsync<PaymentSaga>(saga.SagaId, ShortTimeout));

        ex.Message.ShouldContain(saga.SagaId.ToString());
    }

    [Fact]
    public async Task WaitForSagaCompletionAsync_UnknownSaga_ThrowsTimeoutException()
    {
        using var host = AspireTestApp.Create();

        await Should.ThrowAsync<TimeoutException>(
            () => host.App.WaitForSagaCompletionAsync<PaymentSaga>(Guid.NewGuid(), ShortTimeout));
    }

    [Fact]
    public async Task WaitForSagaCompletionAsync_SagaCompletesLater_Completes()
    {
        using var host = AspireTestApp.Create();
        var saga = host.AddSaga<PaymentSaga>(SagaStatus.Running);

        var wait = host.App.WaitForSagaCompletionAsync<PaymentSaga>(saga.SagaId, TimeSpan.FromSeconds(30));
        host.Sagas.GetSaga(saga.SagaId)!.Status = SagaStatus.Completed;

        await wait;
    }
}
