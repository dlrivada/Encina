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
        message.ErrorMessage.ShouldBe(HandlerErrorMessage);
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
