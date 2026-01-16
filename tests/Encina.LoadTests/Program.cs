using System.Collections.Concurrent;
using System.Diagnostics;
using Encina;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using static LanguageExt.Prelude;

var options = LoadHarnessOptions.Parse(Environment.GetCommandLineArgs().Skip(1).ToArray());

Console.WriteLine("Encina Load Harness");
Console.WriteLine($"Duration: {options.Duration}. Send workers: {options.SendWorkers}. Publish workers: {options.PublishWorkers}.");

var services = new ServiceCollection();
services.AddEncina(config => config.RegisterServicesFromAssemblyContaining<PingCommand>());

using var provider = services.BuildServiceProvider(new ServiceProviderOptions
{
    ValidateScopes = false,
    ValidateOnBuild = false
});

await WarmUpEncinaAsync(provider).ConfigureAwait(false);

var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
var metrics = new LoadMetrics();
using var cts = new CancellationTokenSource(options.Duration);
var throughputSampler = new ThroughputSampler(metrics);
var throughputTask = throughputSampler.RunAsync(cts.Token);

var sendTasks = Enumerable
    .Range(0, options.SendWorkers)
    .Select(index => Task.Run(() => ExecuteSendWorkerAsync(index, scopeFactory, metrics, cts.Token)))
    .ToArray();

var publishTasks = Enumerable
    .Range(0, options.PublishWorkers)
    .Select(index => Task.Run(() => ExecutePublishWorkerAsync(index, scopeFactory, metrics, cts.Token)))
    .ToArray();

await Task.WhenAll(sendTasks.Concat(publishTasks));
await throughputTask.ConfigureAwait(false);

metrics.PrintSummary(options.Duration, throughputSampler.ToSummary());

static async Task ExecuteSendWorkerAsync(int workerId, IServiceScopeFactory scopeFactory, LoadMetrics metrics, CancellationToken cancellationToken)
{
    using var scope = scopeFactory.CreateScope();
    var Encina = scope.ServiceProvider.GetRequiredService<IEncina>();

    while (!cancellationToken.IsCancellationRequested)
    {
        var request = new PingCommand(Interlocked.Increment(ref HarnessState.SendSequence));

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await Encina.Send(request, cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            if (result.IsRight)
            {
                metrics.RecordSendSuccess(stopwatch.Elapsed);
            }
            else
            {
                var message = result.Match(Left: err => err.Message, Right: _ => string.Empty);
                metrics.RecordSendFailure(stopwatch.Elapsed, message);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            metrics.RecordSendFailure(stopwatch.Elapsed, $"Worker {workerId}: {ex.Message}");
        }
    }
}

static async Task WarmUpEncinaAsync(IServiceProvider provider)
{
    try
    {
        using var scope = provider.CreateScope();
        var Encina = scope.ServiceProvider.GetRequiredService<IEncina>();

        var sendResult = await Encina.Send(new PingCommand(-1), CancellationToken.None).ConfigureAwait(false);
        if (sendResult.IsLeft)
        {
            var message = sendResult.Match(Left: err => err.Message, Right: _ => string.Empty);
            var details = sendResult.Match(
                Left: err => err.Exception.Match(Some: ex => ex.ToString(), None: () => "(sin excepción)"),
                Right: _ => string.Empty);
            Console.WriteLine("Warm-up send failed: " + message);
            if (!string.IsNullOrEmpty(details))
            {
                Console.WriteLine(details);
            }
        }

        var publishResult = await Encina.Publish(new BroadcastNotification(-1), CancellationToken.None).ConfigureAwait(false);
        if (publishResult.IsLeft)
        {
            var message = publishResult.Match(Left: err => err.Message, Right: _ => string.Empty);
            var details = publishResult.Match(
                Left: err => err.Exception.Match(Some: ex => ex.ToString(), None: () => "(sin excepción)"),
                Right: _ => string.Empty);
            Console.WriteLine("Warm-up publish failed: " + message);
            if (!string.IsNullOrEmpty(details))
            {
                Console.WriteLine(details);
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("Warm-up threw an exception: " + ex.Message);
    }
}

static async Task ExecutePublishWorkerAsync(int workerId, IServiceScopeFactory scopeFactory, LoadMetrics metrics, CancellationToken cancellationToken)
{
    using var scope = scopeFactory.CreateScope();
    var Encina = scope.ServiceProvider.GetRequiredService<IEncina>();

    while (!cancellationToken.IsCancellationRequested)
    {
        var notification = new BroadcastNotification(Interlocked.Increment(ref HarnessState.PublishSequence));

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await Encina.Publish(notification, cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            if (result.IsRight)
            {
                metrics.RecordPublishSuccess(stopwatch.Elapsed);
            }
            else
            {
                var message = result.Match(Left: err => err.Message, Right: _ => string.Empty);
                metrics.RecordPublishFailure(stopwatch.Elapsed, message);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            metrics.RecordPublishFailure(stopwatch.Elapsed, $"Worker {workerId}: {ex.Message}");
        }
    }
}

internal sealed record LoadHarnessOptions(TimeSpan Duration, int SendWorkers, int PublishWorkers)
{
    public static LoadHarnessOptions Parse(string[] args)
    {
        var duration = TimeSpan.FromSeconds(30);
        var sendWorkers = Math.Max(1, Environment.ProcessorCount);
        var publishWorkers = Math.Max(1, Environment.ProcessorCount / 2);

        for (var index = 0; index < args.Length; index++)
        {
            var current = args[index];
            switch (current)
            {
                case "--duration" when TryReadTimeSpan(args, ref index, out var parsedDuration):
                    duration = parsedDuration;
                    break;
                case "--send-workers" when TryReadInt(args, ref index, out var parsedSendWorkers):
                    sendWorkers = parsedSendWorkers;
                    break;
                case "--publish-workers" when TryReadInt(args, ref index, out var parsedPublishWorkers):
                    publishWorkers = parsedPublishWorkers;
                    break;
            }
        }

        return new LoadHarnessOptions(duration, sendWorkers, publishWorkers);
    }

    private static bool TryReadTimeSpan(string[] args, ref int index, out TimeSpan result)
    {
        result = default;
        if (index + 1 >= args.Length)
        {
            return false;
        }

        if (TimeSpan.TryParse(args[++index], out var parsed))
        {
            result = parsed;
            return true;
        }

        return false;
    }

    private static bool TryReadInt(string[] args, ref int index, out int result)
    {
        result = default;
        if (index + 1 >= args.Length)
        {
            return false;
        }

        if (int.TryParse(args[++index], out var parsed))
        {
            result = parsed;
            return true;
        }

        return false;
    }
}

internal sealed record PingCommand(long Id) : IRequest<int>;

internal sealed class PingCommandHandler : IRequestHandler<PingCommand, int>
{
    public Task<Either<EncinaError, int>> Handle(PingCommand request, CancellationToken cancellationToken)
    {
        var computed = unchecked((int)(request.Id % 1_000));
        return Task.FromResult(Right<EncinaError, int>(computed));
    }
}

internal sealed record BroadcastNotification(long Id) : INotification;

internal sealed class BroadcastNotificationHandler : INotificationHandler<BroadcastNotification>
{
    public Task<Either<EncinaError, Unit>> Handle(BroadcastNotification notification, CancellationToken cancellationToken)
    {
        return Task.FromResult(Right<EncinaError, Unit>(Unit.Default));
    }
}

internal static class HarnessState
{
    internal static long SendSequence;
    internal static long PublishSequence;
}

internal sealed class LoadMetrics
{
    private long _sendSuccess;
    private long _sendFailure;
    private long _publishSuccess;
    private long _publishFailure;
    private long _sendTicks;
    private long _publishTicks;
    private readonly ConcurrentQueue<string> _sendErrors = new();
    private readonly ConcurrentQueue<string> _publishErrors = new();

    public void RecordSendSuccess(TimeSpan duration)
    {
        Interlocked.Increment(ref _sendSuccess);
        Interlocked.Add(ref _sendTicks, duration.Ticks);
    }

    public void RecordSendFailure(TimeSpan duration, string message)
    {
        Interlocked.Increment(ref _sendFailure);
        Interlocked.Add(ref _sendTicks, duration.Ticks);
        EnqueueError(_sendErrors, message);
    }

    public void RecordPublishSuccess(TimeSpan duration)
    {
        Interlocked.Increment(ref _publishSuccess);
        Interlocked.Add(ref _publishTicks, duration.Ticks);
    }

    public void RecordPublishFailure(TimeSpan duration, string message)
    {
        Interlocked.Increment(ref _publishFailure);
        Interlocked.Add(ref _publishTicks, duration.Ticks);
        EnqueueError(_publishErrors, message);
    }

    public void PrintSummary(TimeSpan duration, ThroughputSummary throughputSummary)
    {
        var totals = SnapshotTotals();
        var totalSend = totals.SendTotal;
        var totalPublish = totals.PublishTotal;

        Console.WriteLine();
        Console.WriteLine("=== Load Summary ===");
        Console.WriteLine($"Send → total: {totalSend}, success: {_sendSuccess}, failures: {_sendFailure}");
        Console.WriteLine($"Publish → total: {totalPublish}, success: {_publishSuccess}, failures: {_publishFailure}");

        if (totalSend > 0)
        {
            var meanSend = TimeSpan.FromTicks(_sendTicks / totalSend);
            Console.WriteLine($"Send mean duration: {meanSend.TotalMicroseconds():F3} µs");
            Console.WriteLine($"Send throughput: {totalSend / duration.TotalSeconds:F2} ops/sec");
            if (!double.IsNaN(throughputSummary.SendP50OpsPerSecond))
            {
                Console.WriteLine($"Send throughput P50: {throughputSummary.SendP50OpsPerSecond:F2} ops/sec");
                Console.WriteLine($"Send throughput P95: {throughputSummary.SendP95OpsPerSecond:F2} ops/sec");
            }
        }

        if (totalPublish > 0)
        {
            var meanPublish = TimeSpan.FromTicks(_publishTicks / totalPublish);
            Console.WriteLine($"Publish mean duration: {meanPublish.TotalMicroseconds():F3} µs");
            Console.WriteLine($"Publish throughput: {totalPublish / duration.TotalSeconds:F2} ops/sec");
            if (!double.IsNaN(throughputSummary.PublishP50OpsPerSecond))
            {
                Console.WriteLine($"Publish throughput P50: {throughputSummary.PublishP50OpsPerSecond:F2} ops/sec");
                Console.WriteLine($"Publish throughput P95: {throughputSummary.PublishP95OpsPerSecond:F2} ops/sec");
            }
        }

        if (!_sendErrors.IsEmpty)
        {
            Console.WriteLine("Sample send errors:");
            foreach (var message in _sendErrors)
            {
                Console.WriteLine("- " + message);
            }
        }

        if (!_publishErrors.IsEmpty)
        {
            Console.WriteLine("Sample publish errors:");
            foreach (var message in _publishErrors)
            {
                Console.WriteLine("- " + message);
            }
        }
    }

    private static void EnqueueError(ConcurrentQueue<string> queue, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        if (queue.Count >= 10)
        {
            return;
        }

        queue.Enqueue(message);
    }

    public LoadTotals SnapshotTotals()
    {
        var sendSuccess = Interlocked.Read(ref _sendSuccess);
        var sendFailure = Interlocked.Read(ref _sendFailure);
        var publishSuccess = Interlocked.Read(ref _publishSuccess);
        var publishFailure = Interlocked.Read(ref _publishFailure);

        return new LoadTotals(sendSuccess + sendFailure, publishSuccess + publishFailure);
    }
}

internal static class TimeSpanExtensions
{
    public static double TotalMicroseconds(this TimeSpan value)
        => value.Ticks * (1_000_000.0 / TimeSpan.TicksPerSecond);
}

internal sealed class ThroughputSampler(LoadMetrics metrics)
{
    private static readonly TimeSpan SamplingInterval = TimeSpan.FromSeconds(1);

    private readonly LoadMetrics _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
    private readonly List<double> _sendSamples = new();
    private readonly List<double> _publishSamples = new();
    private readonly object _sync = new();

    public Task RunAsync(CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
        {
            var totals = _metrics.SnapshotTotals();
            var previousSend = totals.SendTotal;
            var previousPublish = totals.PublishTotal;
            var stopwatch = Stopwatch.StartNew();
            var lastSampleTime = stopwatch.Elapsed;

            while (true)
            {
                try
                {
                    await Task.Delay(SamplingInterval, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    CaptureSample(stopwatch.Elapsed, ref lastSampleTime, ref previousSend, ref previousPublish);
                    break;
                }

                CaptureSample(stopwatch.Elapsed, ref lastSampleTime, ref previousSend, ref previousPublish);
            }
        }, cancellationToken);
    }

    public ThroughputSummary ToSummary()
    {
        lock (_sync)
        {
            return new ThroughputSummary(
                Percentile(_sendSamples, 50),
                Percentile(_sendSamples, 95),
                Percentile(_publishSamples, 50),
                Percentile(_publishSamples, 95));
        }
    }

    private void CaptureSample(TimeSpan currentElapsed, ref TimeSpan lastSampleTime, ref long previousSend, ref long previousPublish)
    {
        var interval = currentElapsed - lastSampleTime;
        if (interval <= TimeSpan.Zero)
        {
            lastSampleTime = currentElapsed;
            return;
        }

        var totals = _metrics.SnapshotTotals();
        var sendDelta = totals.SendTotal - previousSend;
        var publishDelta = totals.PublishTotal - previousPublish;

        var sendThroughput = sendDelta / interval.TotalSeconds;
        var publishThroughput = publishDelta / interval.TotalSeconds;

        lock (_sync)
        {
            if (sendThroughput > 0)
            {
                _sendSamples.Add(sendThroughput);
            }

            if (publishThroughput > 0)
            {
                _publishSamples.Add(publishThroughput);
            }
        }

        previousSend = totals.SendTotal;
        previousPublish = totals.PublishTotal;
        lastSampleTime = currentElapsed;
    }

    private static double Percentile(List<double> samples, double percentile)
    {
        if (samples.Count == 0)
        {
            return double.NaN;
        }

        var sorted = samples.OrderBy(value => value).ToArray();
        var position = (percentile / 100.0) * (sorted.Length - 1);
        var lowerIndex = (int)Math.Floor(position);
        var upperIndex = (int)Math.Ceiling(position);

        if (lowerIndex == upperIndex)
        {
            return sorted[lowerIndex];
        }

        var weight = position - lowerIndex;
        return sorted[lowerIndex] + (sorted[upperIndex] - sorted[lowerIndex]) * weight;
    }
}

internal readonly record struct LoadTotals(long SendTotal, long PublishTotal);

internal readonly record struct ThroughputSummary(double SendP50OpsPerSecond, double SendP95OpsPerSecond, double PublishP50OpsPerSecond, double PublishP95OpsPerSecond)
{
    public static ThroughputSummary Empty => new(double.NaN, double.NaN, double.NaN, double.NaN);
}
