using Encina.Messaging.Outbox;
using Encina.Messaging.Serialization;
using Encina.TestInfrastructure.Extensions;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.IntegrationTests.Messaging.Outbox;

/// <summary>
/// Provider-agnostic scenarios for the outbox delivery rules of #1151 and #1150, run by each provider's
/// outbox store integration tests against its real database.
/// </summary>
/// <remarks>
/// The scenarios drive <see cref="OutboxOrchestrator.ProcessPendingMessagesAsync"/>, which shares its
/// processing cycle with every provider's background <c>OutboxProcessor</c>. The orchestrator's clock is
/// one hour behind the store's, so a scheduled retry is already due when the store is queried again.
/// </remarks>
internal static class OutboxRetryScenarios
{
    private const string HandlerErrorMessage = "Rejected by handler";

    /// <summary>
    /// A <c>Left</c> from the publish callback records a failure with a next retry and leaves the
    /// message unprocessed and pending.
    /// </summary>
    public static async Task LeftResultSchedulesRetryAsync(IOutboxStore store, IOutboxMessageFactory factory)
    {
        var messageId = await AddMessageAsync(store, factory);
        var orchestrator = CreateOrchestrator(store, factory, maxRetries: 3);

        var processed = (await orchestrator.ProcessPendingMessagesAsync(Reject)).ShouldBeRight();
        (await store.SaveChangesAsync()).ShouldBeRight();

        processed.ShouldBe(0);
        var pending = (await store.GetPendingMessagesAsync(100, 3)).ShouldBeRight();
        var message = pending.Single(m => m.Id == messageId);
        message.ProcessedAtUtc.ShouldBeNull();
        message.RetryCount.ShouldBe(1);
        // Only the error code is persisted; EncinaError.Message can carry personal data (#1259 review).
        message.ErrorMessage.ShouldBe("handler.rejected");
        message.NextRetryAtUtc.ShouldNotBeNull();
    }

    /// <summary>
    /// The failure that uses up <see cref="OutboxOptions.MaxRetries"/> records no next retry, and the
    /// store stops returning the message for that retry limit while it stays unprocessed.
    /// </summary>
    public static async Task ExhaustedMessageIsNoLongerFetchedAsync(IOutboxStore store, IOutboxMessageFactory factory)
    {
        var messageId = await AddMessageAsync(store, factory);
        var orchestrator = CreateOrchestrator(store, factory, maxRetries: 2);

        for (var attempt = 0; attempt < 2; attempt++)
        {
            (await orchestrator.ProcessPendingMessagesAsync(Reject)).ShouldBeRight();
            (await store.SaveChangesAsync()).ShouldBeRight();
        }

        (await store.GetPendingMessagesAsync(100, 2)).ShouldBeRight().ShouldNotContain(m => m.Id == messageId);

        var unrestricted = (await store.GetPendingMessagesAsync(100, int.MaxValue)).ShouldBeRight();
        var message = unrestricted.Single(m => m.Id == messageId);
        message.ProcessedAtUtc.ShouldBeNull();
        message.RetryCount.ShouldBe(2);
        message.NextRetryAtUtc.ShouldBeNull();
        message.IsDeadLettered(2).ShouldBeTrue();
    }

    /// <summary>
    /// The pending count includes messages scheduled for a later retry, the exhausted count includes the
    /// messages whose retries are used up, and neither includes processed messages (#1150).
    /// </summary>
    public static async Task CountsSeparatePendingFromExhaustedAsync(IOutboxStore store, IOutboxMessageFactory factory)
    {
        const int maxRetries = 2;
        var pendingBefore = (await store.GetPendingCountAsync(maxRetries)).ShouldBeRight();
        var exhaustedBefore = (await store.GetExhaustedCountAsync(maxRetries)).ShouldBeRight();

        await AddMessageAsync(store, factory);
        var scheduledId = await AddMessageAsync(store, factory);
        await FailAsync(store, scheduledId, times: 1, nextRetryAtUtc: DateTime.UtcNow.AddHours(1));
        var exhaustedId = await AddMessageAsync(store, factory);
        await FailAsync(store, exhaustedId, times: maxRetries);
        var processedId = await AddMessageAsync(store, factory);
        await FailAsync(store, processedId, times: maxRetries);
        (await store.MarkAsProcessedAsync(processedId)).ShouldBeRight();
        (await store.SaveChangesAsync()).ShouldBeRight();

        (await store.GetPendingCountAsync(maxRetries)).ShouldBeRight().ShouldBe(pendingBefore + 2);
        (await store.GetExhaustedCountAsync(maxRetries)).ShouldBeRight().ShouldBe(exhaustedBefore + 1);

        // A higher retry limit turns the exhausted message back into a pending one.
        (await store.GetExhaustedCountAsync(maxRetries + 1)).ShouldBeRight().ShouldBe(exhaustedBefore);
    }

    /// <summary>
    /// Requeuing by identifier resets only the requested exhausted message, which the store then fetches
    /// again with a fresh retry budget (#1150).
    /// </summary>
    public static async Task RequeueExhaustedByIdAsync(IOutboxStore store, IOutboxMessageFactory factory)
    {
        const int maxRetries = 2;
        var requestedId = await AddMessageAsync(store, factory);
        var otherId = await AddMessageAsync(store, factory);
        var pendingId = await AddMessageAsync(store, factory);
        await FailAsync(store, requestedId, times: maxRetries);
        await FailAsync(store, otherId, times: maxRetries);
        var exhaustedBefore = (await store.GetExhaustedCountAsync(maxRetries)).ShouldBeRight();

        var orchestrator = CreateOrchestrator(store, factory, maxRetries);
        var requeued = (await orchestrator.RequeueExhaustedAsync([requestedId, pendingId, Guid.NewGuid()])).ShouldBeRight();

        requeued.ShouldBe(1);
        (await store.GetExhaustedCountAsync(maxRetries)).ShouldBeRight().ShouldBe(exhaustedBefore - 1);

        var fetched = (await store.GetPendingMessagesAsync(100, maxRetries)).ShouldBeRight().ToList();
        var message = fetched.Single(m => m.Id == requestedId);
        message.RetryCount.ShouldBe(0);
        message.NextRetryAtUtc.ShouldBeNull();
        message.ErrorMessage.ShouldBeNull();
        message.ProcessedAtUtc.ShouldBeNull();
        fetched.ShouldNotContain(m => m.Id == otherId);
    }

    /// <summary>
    /// Requeuing everything resets every exhausted message and leaves processed messages alone (#1150).
    /// </summary>
    public static async Task RequeueAllExhaustedAsync(IOutboxStore store, IOutboxMessageFactory factory)
    {
        const int maxRetries = 2;
        var firstId = await AddMessageAsync(store, factory);
        var secondId = await AddMessageAsync(store, factory);
        var processedId = await AddMessageAsync(store, factory);
        await FailAsync(store, firstId, times: maxRetries);
        await FailAsync(store, secondId, times: maxRetries);
        await FailAsync(store, processedId, times: maxRetries);
        (await store.MarkAsProcessedAsync(processedId)).ShouldBeRight();
        (await store.SaveChangesAsync()).ShouldBeRight();
        var exhaustedBefore = (await store.GetExhaustedCountAsync(maxRetries)).ShouldBeRight();
        exhaustedBefore.ShouldBeGreaterThanOrEqualTo(2);

        var orchestrator = CreateOrchestrator(store, factory, maxRetries);
        var requeued = (await orchestrator.RequeueExhaustedAsync()).ShouldBeRight();

        requeued.ShouldBe(exhaustedBefore);
        (await store.GetExhaustedCountAsync(maxRetries)).ShouldBeRight().ShouldBe(0);

        var fetched = (await store.GetPendingMessagesAsync(100, maxRetries)).ShouldBeRight().ToList();
        fetched.ShouldContain(m => m.Id == firstId && m.RetryCount == 0);
        fetched.ShouldContain(m => m.Id == secondId && m.RetryCount == 0);
        fetched.ShouldNotContain(m => m.Id == processedId);
    }

    private static async Task FailAsync(IOutboxStore store, Guid messageId, int times, DateTime? nextRetryAtUtc = null)
    {
        for (var attempt = 0; attempt < times; attempt++)
        {
            (await store.MarkAsFailedAsync(messageId, HandlerErrorMessage, nextRetryAtUtc)).ShouldBeRight();
            (await store.SaveChangesAsync()).ShouldBeRight();
        }
    }

    private static async Task<Guid> AddMessageAsync(IOutboxStore store, IOutboxMessageFactory factory)
    {
        var message = factory.Create(
            Guid.NewGuid(),
            typeof(OutboxScenarioNotification).AssemblyQualifiedName!,
            "{\"value\":\"payload\"}",
            DateTime.UtcNow.AddMinutes(-5));

        (await store.AddAsync(message)).ShouldBeRight();
        (await store.SaveChangesAsync()).ShouldBeRight();
        return message.Id;
    }

    private static OutboxOrchestrator CreateOrchestrator(IOutboxStore store, IOutboxMessageFactory factory, int maxRetries)
        => new(
            store,
            new OutboxOptions
            {
                BatchSize = 100,
                MaxRetries = maxRetries,
                BaseRetryDelay = TimeSpan.FromSeconds(5),
                RetryJitterRatio = 0
            },
            NullLogger<OutboxOrchestrator>.Instance,
            factory,
            new JsonMessageSerializer(),
            new FakeTimeProvider(DateTimeOffset.UtcNow.AddHours(-1)));

    private static ValueTask<Either<EncinaError, Unit>> Reject(IOutboxMessage message, Type type, object notification)
        => ValueTask.FromResult(Left<EncinaError, Unit>(EncinaErrors.Create("handler.rejected", HandlerErrorMessage)));
}

/// <summary>
/// Notification payload used by <see cref="OutboxRetryScenarios"/>.
/// </summary>
/// <param name="Value">An arbitrary payload value.</param>
public sealed record OutboxScenarioNotification(string Value);
