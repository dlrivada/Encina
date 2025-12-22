using System.Reflection;
using FsCheck;
using FsCheck.Xunit;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using static LanguageExt.Prelude;

namespace Encina.PropertyTests;

public sealed class NotificationProperties
{
    private const int MaxHandlers = 6;
    private const int MaxConcurrentPublishes = 4;

    private enum HandlerOutcome
    {
        Success,
        Fault,
        Cancellation
    }

    private sealed record ExecutionResult(Either<EncinaError, Unit> Outcome, IReadOnlyList<string> Events);

    [Property(MaxTest = 120)]
    public bool Publish_NotifiesHandlersInRegistrationOrder(PositiveInt countSeed)
    {
        var handlerCount = NormalizeCount(countSeed.Get);
        var outcomes = Enumerable.Repeat(HandlerOutcome.Success, handlerCount).ToArray();

        var result = ExecutePublish(outcomes, cancelBeforePublish: false);
        var expected = Enumerable.Range(0, handlerCount).Select(i => $"handler:{i}").ToArray();

        return result.Outcome.IsRight && result.Events.SequenceEqual(expected);
    }

    [Property(MaxTest = 120)]
    public bool Publish_StopsAfterFaultingHandler(PositiveInt countSeed, NonNegativeInt faultSeed)
    {
        var handlerCount = NormalizeCount(countSeed.Get);
        var failingIndex = faultSeed.Get % handlerCount;

        var outcomes = Enumerable.Repeat(HandlerOutcome.Success, handlerCount).ToArray();
        outcomes[failingIndex] = HandlerOutcome.Fault;

        var result = ExecutePublish(outcomes, cancelBeforePublish: false);
        var expected = Enumerable.Range(0, failingIndex + 1).Select(i => $"handler:{i}").ToArray();

        return result.Outcome.Match(
            Left: err => err.Exception.Match(Some: ex => ex is InvalidOperationException, None: () => false)
                             && err.GetEncinaCode() == "encina.notification.exception"
                             && result.Events.SequenceEqual(expected),
            Right: _ => false);
    }

    [Property(MaxTest = 120)]
    public bool Publish_PropagatesCancellationAndStopsSequence(PositiveInt countSeed)
    {
        var handlerCount = Math.Max(2, NormalizeCount(countSeed.Get));
        var outcomes = Enumerable.Repeat(HandlerOutcome.Success, handlerCount).ToArray();
        outcomes[0] = HandlerOutcome.Cancellation;

        var result = ExecutePublish(outcomes, cancelBeforePublish: true);
        var expected = new[] { "handler:0" };

        return result.Outcome.Match(
            Left: err => err.GetEncinaCode() == "encina.notification.cancelled"
                             && err.Exception.Match(Some: ex => ex is OperationCanceledException, None: () => false)
                             && result.Events.SequenceEqual(expected),
            Right: _ => false);
    }

    [Property(MaxTest = 80)]
    public bool Publish_AllHandlersInvokedUnderConcurrency(PositiveInt handlerSeed, PositiveInt publishSeed)
    {
        var handlerCount = NormalizeCount(handlerSeed.Get);

        var publishIterations = publishSeed.Get % MaxConcurrentPublishes;
        if (publishIterations < 2)
        {
            publishIterations += 2;
        }
        var publishCount = Math.Clamp(publishIterations, 2, MaxConcurrentPublishes);

        var services = new ServiceCollection();
        services.AddSingleton<CallRecorder>();
        services.AddEncina(System.Array.Empty<Assembly>());

        for (var index = 0; index < handlerCount; index++)
        {
            var handlerIndex = index;
            services.AddSingleton<INotificationHandler<TrackedNotification>>(sp =>
                new RecordingNotificationHandler(
                    sp.GetRequiredService<CallRecorder>(),
                    label: handlerIndex,
                    outcome: HandlerOutcome.Success));
        }

        using var provider = services.BuildServiceProvider();
        var recorder = provider.GetRequiredService<CallRecorder>();
        recorder.Clear();

        var Encina = new global::Encina.Encina(provider.GetRequiredService<IServiceScopeFactory>());

        var publishTasks = Enumerable.Range(0, publishCount)
            .Select(iteration => Encina.Publish(
                new TrackedNotification(Guid.NewGuid().ToString("N")),
                CancellationToken.None).AsTask())
            .ToArray();

        var outcomes = Task.WhenAll(publishTasks).GetAwaiter().GetResult();

        if (outcomes.Any(o => o.IsLeft))
        {
            return false;
        }

        var events = recorder.Snapshot();
        if (events.Length != handlerCount * publishCount)
        {
            return false;
        }

        for (var handlerIndex = 0; handlerIndex < handlerCount; handlerIndex++)
        {
            var expectedOccurrences = publishCount;
            var label = $"handler:{handlerIndex}";
            var actualOccurrences = events.Count(evt => evt == label);
            if (actualOccurrences != expectedOccurrences)
            {
                return false;
            }
        }

        return true;
    }

    private static ExecutionResult ExecutePublish(IReadOnlyList<HandlerOutcome> outcomes, bool cancelBeforePublish)
    {
        var services = new ServiceCollection();
        services.AddSingleton<CallRecorder>();
        services.AddEncina(System.Array.Empty<Assembly>());

        for (var index = 0; index < outcomes.Count; index++)
        {
            var handlerIndex = index;
            var handlerOutcome = outcomes[index];
            services.AddSingleton<INotificationHandler<TrackedNotification>>(sp =>
                new RecordingNotificationHandler(
                    sp.GetRequiredService<CallRecorder>(),
                    label: handlerIndex,
                    handlerOutcome));
        }

        using var provider = services.BuildServiceProvider();
        var recorder = provider.GetRequiredService<CallRecorder>();
        recorder.Clear();

        var Encina = new global::Encina.Encina(provider.GetRequiredService<IServiceScopeFactory>());
        var notification = new TrackedNotification(Guid.NewGuid().ToString("N"));
        var tokenSource = cancelBeforePublish ? new CancellationTokenSource() : null;
        tokenSource?.Cancel();

        var outcome = Encina.Publish(notification, tokenSource?.Token ?? CancellationToken.None)
            .AsTask()
            .GetAwaiter()
            .GetResult();

        var events = recorder.Snapshot();
        return new ExecutionResult(outcome, events);
    }

    private static int NormalizeCount(int seed)
    {
        var normalized = seed % MaxHandlers;
        if (normalized <= 0)
        {
            normalized += MaxHandlers;
        }

        return Math.Clamp(normalized, 1, MaxHandlers);
    }

    private sealed record TrackedNotification(string Value) : INotification;

    private sealed class RecordingNotificationHandler(NotificationProperties.CallRecorder recorder, int label, NotificationProperties.HandlerOutcome outcome) : INotificationHandler<TrackedNotification>
    {
        private readonly CallRecorder _recorder = recorder;
        private readonly int _label = label;
        private readonly HandlerOutcome _outcome = outcome;

        public Task<Either<EncinaError, Unit>> Handle(TrackedNotification notification, CancellationToken cancellationToken)
        {
            _recorder.Add($"handler:{_label}");

            return _outcome switch
            {
                HandlerOutcome.Success => Task.FromResult(Right<EncinaError, Unit>(Unit.Default)),
                HandlerOutcome.Fault => Task.FromException<Either<EncinaError, Unit>>(new InvalidOperationException($"fault:{_label}")),
                HandlerOutcome.Cancellation => Task.FromCanceled<Either<EncinaError, Unit>>(cancellationToken.IsCancellationRequested
                    ? cancellationToken
                    : new CancellationToken(true)),
                _ => Task.FromResult(Right<EncinaError, Unit>(Unit.Default))
            };
        }
    }

    private sealed class CallRecorder
    {
        private readonly List<string> _events = new();
        private readonly object _lock = new();

        public void Add(string entry)
        {
            lock (_lock)
            {
                _events.Add(entry);
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _events.Clear();
            }
        }

        public string[] Snapshot()
        {
            lock (_lock)
            {
                return _events.ToArray();
            }
        }
    }
}
