using Encina.Messaging.DeadLetter;
using Encina.Testing.Shouldly;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

#pragma warning disable CA2012 // NSubstitute setup of ValueTask-returning members

namespace Encina.IntegrationTests.Messaging.DeadLetter;

/// <summary>
/// The capture, replay and expiry path of the persistent dead letter queue, run through the services a
/// provider registration builds (<c>UseDeadLetterQueue = true</c>) against a real database.
/// </summary>
/// <remarks>
/// The replayed request goes to a substitute <see cref="IEncina"/>, the handler of this scenario; every other
/// service (store, factory, orchestrator, manager) is the registered one. Time comes from a
/// <see cref="FakeTimeProvider"/> that the caller registers as <see cref="TimeProvider"/>.
/// </remarks>
internal static class DeadLetterEndToEndScenario
{
    /// <summary>A request the manager can resolve by name and deserialize.</summary>
    public sealed record EndToEndCommand(int Value) : IRequest<int>;

    /// <summary>Creates the services every family shares: logging, the fake clock and the replay handler.</summary>
    public static (ServiceCollection Services, FakeTimeProvider Clock, IEncina Encina) NewServices()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 3, 1, 9, 0, 0, TimeSpan.Zero));
        var encina = Substitute.For<IEncina>();
        var outcome = Right<EncinaError, int>(7);
        encina.Send(Arg.Any<IRequest<int>>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, int>>(outcome));

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<TimeProvider>(clock);
        services.AddSingleton(encina);
        return (services, clock, encina);
    }

    /// <summary>Captures two failures, replays one through the manager and expires both.</summary>
    public static async Task RunAsync(IServiceProvider provider, FakeTimeProvider clock, IEncina encina)
    {
        using var scope = provider.CreateScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<DeadLetterOrchestrator>();
        var manager = scope.ServiceProvider.GetRequiredService<IDeadLetterManager>();
        var store = scope.ServiceProvider.GetRequiredService<IDeadLetterStore>();
        var started = clock.GetUtcNow().UtcDateTime;

        // Capture: stored once, a second capture of the same source message returns the existing row.
        var first = (await orchestrator.AddAsync(new EndToEndCommand(1), Context("e2e-1", started))).ShouldBeRight();
        var again = (await orchestrator.AddAsync(new EndToEndCommand(1), Context("e2e-1", started))).ShouldBeRight();
        again.Id.ShouldBe(first.Id);
        var second = (await orchestrator.AddAsync(new EndToEndCommand(2), Context("e2e-2", started))).ShouldBeRight();
        (await manager.GetCountAsync()).ShouldBeRight().ShouldBe(2);

        // Replay: dispatched once, the outcome code recorded, a second replay refused.
        var replay = (await manager.ReplayAsync(first.Id)).ShouldBeRight();
        replay.Success.ShouldBeTrue();
        await encina.Received(1).Send(Arg.Is<IRequest<int>>(r => r is EndToEndCommand), Arg.Any<CancellationToken>());
        var replayed = (await store.GetAsync(first.Id)).ShouldBeRight().IfNone(() => throw new InvalidOperationException("missing"));
        replayed.IsReplayed.ShouldBeTrue();
        replayed.ReplayResult.ShouldBe(DeadLetterErrorCodes.ReplaySucceeded);
        (await manager.ReplayAsync(first.Id)).ShouldBeErrorWithCode(DeadLetterErrorCodes.AlreadyReplayed);
        (await manager.GetCountAsync(new DeadLetterFilter { ExcludeReplayed = true })).ShouldBeRight().ShouldBe(1);

        // Expiry: nothing is deleted before the retention period ends, both rows after it.
        (await manager.CleanupExpiredAsync()).ShouldBeRight().ShouldBe(0);
        clock.Advance(TimeSpan.FromDays(366));
        (await manager.CleanupExpiredAsync()).ShouldBeRight().ShouldBe(2);
        (await manager.GetCountAsync()).ShouldBeRight().ShouldBe(0);
        (await store.GetAsync(second.Id)).ShouldBeRight().IsNone.ShouldBeTrue();
    }

    private static DeadLetterContext Context(string sourceMessageId, DateTime firstFailedAtUtc)
        => new(
            EncinaErrors.Create("e2e.failed", "failure"),
            Exception: null,
            SourcePattern: DeadLetterSourcePatterns.Outbox,
            TotalRetryAttempts: 3,
            FirstFailedAtUtc: firstFailedAtUtc,
            SourceMessageId: sourceMessageId);
}
