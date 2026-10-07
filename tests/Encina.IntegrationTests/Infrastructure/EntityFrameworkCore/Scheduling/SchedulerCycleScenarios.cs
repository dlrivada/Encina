using Encina.EntityFrameworkCore.Scheduling;
using Encina.Messaging.Scheduling;
using Encina.Messaging.Serialization;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.Scheduling;

/// <summary>
/// Provider-independent scenarios that run <see cref="SchedulerOrchestrator"/> cycles on EF Core,
/// one <see cref="DbContext"/> per cycle exactly as <c>ScheduledMessageProcessor</c> creates one
/// scope per cycle, and assert what reached the database (#1970).
/// </summary>
internal static class SchedulerCycleScenarios
{
    internal sealed record CycleRequest(int Value);

    private sealed class FixedCronParser(DateTime next) : ICronParser
    {
        public Either<EncinaError, DateTime> GetNextOccurrence(string cronExpression, DateTime after) => next;
    }

    internal static async Task DueOneOffMessage_RunsOnceAcrossThreeCycles_AndIsPersistedAsProcessed(
        Func<TestEFDbContext> createContext)
    {
        var id = await SeedAsync(createContext, isRecurring: false, cron: null);
        var executions = 0;

        for (var cycle = 0; cycle < 3; cycle++)
        {
            await RunCycleAsync(createContext, null, (_, _, _, _) =>
            {
                executions++;
                return new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(Unit.Default));
            });
        }

        executions.ShouldBe(1);
        var stored = await LoadAsync(createContext, id);
        stored.ProcessedAtUtc.ShouldNotBeNull();
    }

    internal static async Task FailingHandler_IncrementsRetryCountAndSetsNextRetry(
        Func<TestEFDbContext> createContext)
    {
        var id = await SeedAsync(createContext, isRecurring: false, cron: null);
        var executions = 0;

        for (var cycle = 0; cycle < 2; cycle++)
        {
            await RunCycleAsync(createContext, null, (_, _, _, _) =>
            {
                executions++;
                return new ValueTask<Either<EncinaError, Unit>>(
                    Left<EncinaError, Unit>(EncinaErrors.Create("test.handler_failed", "boom")));
            });
        }

        // The second cycle must not pick the message up again: its retry is scheduled in the future.
        executions.ShouldBe(1);
        var stored = await LoadAsync(createContext, id);
        stored.RetryCount.ShouldBe(1);
        stored.NextRetryAtUtc.ShouldNotBeNull();
        stored.NextRetryAtUtc!.Value.ShouldBeGreaterThan(DateTime.UtcNow);
        stored.ProcessedAtUtc.ShouldBeNull();
    }

    internal static async Task RecurringMessage_GetsItsNextScheduledAt(
        Func<TestEFDbContext> createContext)
    {
        var id = await SeedAsync(createContext, isRecurring: true, cron: "* * * * *");
        var next = DateTime.UtcNow.AddHours(1);
        var executions = 0;

        for (var cycle = 0; cycle < 2; cycle++)
        {
            await RunCycleAsync(createContext, new FixedCronParser(next), (_, _, _, _) =>
            {
                executions++;
                return new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(Unit.Default));
            });
        }

        executions.ShouldBe(1);
        var stored = await LoadAsync(createContext, id);
        stored.ScheduledAtUtc.ShouldBe(next, TimeSpan.FromSeconds(1));
        stored.ProcessedAtUtc.ShouldBeNull();
    }

    private static async Task<Guid> SeedAsync(Func<TestEFDbContext> createContext, bool isRecurring, string? cron)
    {
        await using var context = createContext();
        await context.Set<ScheduledMessage>().ExecuteDeleteAsync();
        var store = new ScheduledMessageStoreEF(context);

        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = typeof(CycleRequest).AssemblyQualifiedName!,
            Content = new JsonMessageSerializer().Serialize(new CycleRequest(1)),
            ScheduledAtUtc = DateTime.UtcNow.AddMinutes(-5),
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-10),
            IsRecurring = isRecurring,
            CronExpression = cron,
            RetryCount = 0
        };

        (await store.AddAsync(message)).IsRight.ShouldBeTrue();
        (await store.SaveChangesAsync()).IsRight.ShouldBeTrue();
        return message.Id;
    }

    private static async Task RunCycleAsync(
        Func<TestEFDbContext> createContext,
        ICronParser? cronParser,
        Func<IScheduledMessage, Type, object, CancellationToken, ValueTask<Either<EncinaError, Unit>>> callback)
    {
        await using var context = createContext();
        var options = new SchedulingOptions();
        var orchestrator = new SchedulerOrchestrator(
            new ScheduledMessageStoreEF(context),
            options,
            NullLogger<SchedulerOrchestrator>.Instance,
            new ScheduledMessageFactory(),
            new ExponentialBackoffRetryPolicy(options),
            new JsonMessageSerializer(),
            cronParser);

        var result = await orchestrator.ProcessDueMessagesAsync(callback);
        result.IsRight.ShouldBeTrue();
    }

    private static async Task<ScheduledMessage> LoadAsync(Func<TestEFDbContext> createContext, Guid id)
    {
        await using var verifyContext = createContext();
        return await verifyContext.Set<ScheduledMessage>().AsNoTracking().SingleAsync(m => m.Id == id);
    }
}
