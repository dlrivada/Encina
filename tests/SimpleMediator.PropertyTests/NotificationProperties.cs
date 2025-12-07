using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FsCheck;
using FsCheck.Xunit;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using SimpleMediator;

namespace SimpleMediator.PropertyTests;

public sealed class NotificationProperties
{
    private const int MaxHandlers = 6;

    private enum HandlerOutcome
    {
        Success,
        Fault,
        Cancellation
    }

    private sealed record ExecutionResult(Either<Error, Unit> Outcome, IReadOnlyList<string> Events);

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
                             && err.GetMediatorCode() == "mediator.notification.invoke_exception"
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
            Left: err => err.GetMediatorCode() == "mediator.notification.cancelled"
                             && err.Exception.Match(Some: ex => ex is OperationCanceledException, None: () => false)
                             && result.Events.SequenceEqual(expected),
            Right: _ => false);
    }

    private static ExecutionResult ExecutePublish(IReadOnlyList<HandlerOutcome> outcomes, bool cancelBeforePublish)
    {
        var services = new ServiceCollection();
        services.AddSingleton<CallRecorder>();
        services.AddSimpleMediator(System.Array.Empty<Assembly>());

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

        var mediator = new global::SimpleMediator.SimpleMediator(provider.GetRequiredService<IServiceScopeFactory>());
        var notification = new TrackedNotification(Guid.NewGuid().ToString("N"));
        var tokenSource = cancelBeforePublish ? new CancellationTokenSource() : null;
        tokenSource?.Cancel();

        var outcome = mediator.Publish(notification, tokenSource?.Token ?? CancellationToken.None)
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

    private sealed class RecordingNotificationHandler : INotificationHandler<TrackedNotification>
    {
        private readonly CallRecorder _recorder;
        private readonly int _label;
        private readonly HandlerOutcome _outcome;

        public RecordingNotificationHandler(CallRecorder recorder, int label, HandlerOutcome outcome)
        {
            _recorder = recorder;
            _label = label;
            _outcome = outcome;
        }

        public Task Handle(TrackedNotification notification, CancellationToken cancellationToken)
        {
            _recorder.Add($"handler:{_label}");

            return _outcome switch
            {
                HandlerOutcome.Success => Task.CompletedTask,
                HandlerOutcome.Fault => Task.FromException(new InvalidOperationException($"fault:{_label}")),
                HandlerOutcome.Cancellation => Task.FromCanceled(cancellationToken.IsCancellationRequested
                    ? cancellationToken
                    : new CancellationToken(true)),
                _ => Task.CompletedTask
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

        public IReadOnlyList<string> Snapshot()
        {
            lock (_lock)
            {
                return _events.ToArray();
            }
        }
    }
}
