using Encina.Messaging.Inbox;
using Encina.Messaging.Serialization;
using Encina.TestInfrastructure.Extensions;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.IntegrationTests.Messaging;

/// <summary>
/// Provider-neutral scenario for #2084: drives <see cref="InboxOrchestrator"/> over a real
/// <see cref="IInboxStore"/> and proves the handler runs exactly <c>MaxRetries</c> times before the
/// inbox rejects the message with <c>inbox.max_retries_exceeded</c>, and that a handler Left is
/// cached without consuming retries.
/// </summary>
public static class InboxRetryScenario
{
    /// <summary>Asserts the attempt boundary for a handler that always throws.</summary>
    /// <param name="store">The provider store under test.</param>
    /// <param name="factory">The provider message factory.</param>
    /// <param name="maxRetries">The configured <see cref="InboxOptions.MaxRetries"/>.</param>
    /// <param name="saveChanges">Flushes pending changes for unit-of-work stores (EF Core); null for immediate stores.</param>
    public static async Task AssertHandlerRunsMaxRetriesTimesAsync(
        IInboxStore store,
        IInboxMessageFactory factory,
        int maxRetries,
        Func<Task>? saveChanges = null)
    {
        var orchestrator = Create(store, factory, maxRetries);
        var messageId = Guid.NewGuid().ToString();
        var runs = 0;

        Func<ValueTask<Either<EncinaError, string>>> handler = () =>
        {
            runs++;
            throw new InvalidOperationException("boom");
        };

        for (var i = 0; i < maxRetries; i++)
        {
            var failed = await orchestrator.ProcessAsync(new object(), messageId,"TestRequest", "corr", null, handler);
            if (saveChanges is not null)
                await saveChanges();

            failed.IsLeft.ShouldBeTrue();
            failed.LeftToArray()[0].GetCode().IfNone(string.Empty).ShouldBe("inbox.processing_failed");
        }

        runs.ShouldBe(maxRetries);

        var rejected = await orchestrator.ProcessAsync(new object(), messageId,"TestRequest", "corr", null, handler);

        rejected.IsLeft.ShouldBeTrue();
        rejected.LeftToArray()[0].GetCode().IfNone(string.Empty).ShouldBe(InboxErrorCodes.MaxRetriesExceeded);
        runs.ShouldBe(maxRetries);

        var stored = (await store.GetMessageAsync(messageId)).ShouldBeRight();
        stored.IsSome.ShouldBeTrue();
        stored.IfSome(m => m.RetryCount.ShouldBe(maxRetries));
    }

    /// <summary>Asserts that a handler Left is cached and not re-run on redelivery.</summary>
    /// <param name="store">The provider store under test.</param>
    /// <param name="factory">The provider message factory.</param>
    /// <param name="saveChanges">Flushes pending changes for unit-of-work stores (EF Core); null for immediate stores.</param>
    public static async Task AssertHandlerLeftIsCachedAsync(
        IInboxStore store,
        IInboxMessageFactory factory,
        Func<Task>? saveChanges = null)
    {
        var orchestrator = Create(store, factory, 3);
        var messageId = Guid.NewGuid().ToString();
        var runs = 0;

        Func<ValueTask<Either<EncinaError, string>>> handler = () =>
        {
            runs++;
            return ValueTask.FromResult<Either<EncinaError, string>>(EncinaErrors.Create("biz.rule", "business rule"));
        };

        var first = await orchestrator.ProcessAsync(new object(), messageId,"TestRequest", "corr", null, handler);
        if (saveChanges is not null)
            await saveChanges();
        var second = await orchestrator.ProcessAsync(new object(), messageId,"TestRequest", "corr", null, handler);

        first.IsLeft.ShouldBeTrue();
        second.IsLeft.ShouldBeTrue();
        second.LeftToArray()[0].GetCode().IfNone(string.Empty).ShouldBe(InboxErrorCodes.CachedError);
        runs.ShouldBe(1);

        var stored = (await store.GetMessageAsync(messageId)).ShouldBeRight();
        stored.IfSome(m => m.RetryCount.ShouldBe(0));
    }

    private static InboxOrchestrator Create(IInboxStore store, IInboxMessageFactory factory, int maxRetries) =>
        new(store,
            new InboxOptions { MaxRetries = maxRetries },
            NullLogger<InboxOrchestrator>.Instance,
            factory,
            new JsonMessageSerializer());
}
