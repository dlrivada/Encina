using BenchmarkDotNet.Attributes;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Encina.Quartz;
using static LanguageExt.Prelude;

namespace Encina.Benchmarks;

/// <summary>
/// Benchmarks comparing job scheduling with Quartz.NET.
/// Note: Hangfire benchmarks excluded due to infrastructure complexity in benchmark environment.
/// </summary>
[MemoryDiagnoser]
[RankColumn]
public class JobSchedulingBenchmarks
{
    private IServiceProvider _quartzProvider = default!;
    private IScheduler _quartzScheduler = default!;

    [GlobalSetup]
    public async Task Setup()
    {
        // Quartz setup (in-memory scheduler)
        var quartzServices = new ServiceCollection();
        quartzServices.AddEncina();
        quartzServices.AddEncinaQuartz(config =>
        {
            config.UseInMemoryStore();
        });
        quartzServices.AddTransient<ICommandHandler<ScheduleJobCommand, Guid>, ScheduleJobCommandHandler>();
        _quartzProvider = quartzServices.BuildServiceProvider();

        // Start Quartz scheduler
        var schedulerFactory = _quartzProvider.GetRequiredService<ISchedulerFactory>();
        _quartzScheduler = await schedulerFactory.GetScheduler();
        await _quartzScheduler.Start();
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        if (_quartzScheduler != null)
        {
            await _quartzScheduler.Shutdown();
        }

        (_quartzProvider as IDisposable)?.Dispose();
    }

    [Benchmark(Baseline = true, Description = "Quartz (Immediate Trigger)")]
    public async Task<TriggerKey> Quartz_ScheduleJob()
    {
        var command = new ScheduleJobCommand(Guid.NewGuid());
        var trigger = TriggerBuilder.Create()
            .StartNow()
            .Build();

        return await _quartzScheduler.ScheduleRequest<ScheduleJobCommand, Guid>(
            command,
            trigger);
    }

    [Benchmark(Description = "Quartz (Delayed Trigger +5s)")]
    public async Task<TriggerKey> Quartz_ScheduleJobWithDelay()
    {
        var command = new ScheduleJobCommand(Guid.NewGuid());
        var trigger = TriggerBuilder.Create()
            .StartAt(DateTimeOffset.UtcNow.AddSeconds(5))
            .Build();

        return await _quartzScheduler.ScheduleRequest<ScheduleJobCommand, Guid>(
            command,
            trigger);
    }

    [Benchmark(Description = "Quartz (CRON Daily Trigger)")]
    public async Task<TriggerKey> Quartz_ScheduleJobWithCron()
    {
        var command = new ScheduleJobCommand(Guid.NewGuid());
        var trigger = TriggerBuilder.Create()
            .WithCronSchedule("0 0 0 * * ?") // Daily at midnight
            .Build();

        return await _quartzScheduler.ScheduleRequest<ScheduleJobCommand, Guid>(
            command,
            trigger);
    }

    // Test command and handler
    private sealed record ScheduleJobCommand(Guid JobId) : ICommand<Guid>;

    private sealed class ScheduleJobCommandHandler : ICommandHandler<ScheduleJobCommand, Guid>
    {
        public Task<Either<MediatorError, Guid>> Handle(ScheduleJobCommand request, CancellationToken cancellationToken)
        {
            // Simulate minimal work
            return Task.FromResult(Right<MediatorError, Guid>(request.JobId));
        }
    }
}
