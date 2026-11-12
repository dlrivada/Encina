#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Abstractions;
using Encina.Compliance.DPIA.ReadModels;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.Compliance.DPIA;

/// <summary>
/// Unit tests for <see cref="DPIAReviewReminderService"/>: the service always runs (there is no
/// switch, #3270), runs a first cycle at start, then one cycle per
/// <see cref="DPIAOptions.ExpirationCheckInterval"/> on the injected clock, and a failing cycle
/// never ends the loop.
/// </summary>
public class DPIAReviewReminderServiceTests
{
    private static readonly TimeSpan SignalTimeout = TimeSpan.FromSeconds(30);

    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero));
    private readonly IDPIAService _service = Substitute.For<IDPIAService>();

    #region ExecuteAsync - Always on

    [Fact]
    public async Task ExecuteAsync_DefaultOptions_RunsFirstCycleImmediately()
    {
        var calls = new CallCounter(1);
        SetupEmpty(calls);

        var sut = CreateSut(new DPIAOptions { PublishNotifications = false });

        await sut.StartAsync(CancellationToken.None);
        await calls.Reached.WaitAsync(SignalTimeout);
        await sut.StopAsync(CancellationToken.None);

        calls.Count.ShouldBe(1);
    }

    [Fact]
    public async Task ExecuteAsync_IntervalElapsesOnInjectedClock_RunsNextCycle()
    {
        var interval = TimeSpan.FromMinutes(15);
        var calls = new CallCounter(2);
        SetupEmpty(calls);

        var sut = CreateSut(new DPIAOptions { ExpirationCheckInterval = interval, PublishNotifications = false });

        await sut.StartAsync(CancellationToken.None);
        await WaitForFirstCycleAsync(calls);

        // Before the interval elapses on the injected clock no second cycle runs
        _time.Advance(interval - TimeSpan.FromSeconds(1));
        calls.Count.ShouldBe(1);

        _time.Advance(TimeSpan.FromSeconds(1));

        await calls.Reached.WaitAsync(SignalTimeout);
        await sut.StopAsync(CancellationToken.None);

        calls.Count.ShouldBe(2);
    }

    #endregion

    #region ExecuteAsync - Error Handling

    [Fact]
    public async Task ExecuteAsync_StoreReturnsError_KeepsTheLoopAlive()
    {
        var calls = new CallCounter(2);
        _service.GetExpiredAssessmentsAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                calls.Increment();
                return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<DPIAReadModel>>>(
                    EncinaError.New("Store error"));
            });

        var sut = CreateSut(new DPIAOptions { PublishNotifications = false });

        await sut.StartAsync(CancellationToken.None);
        await WaitForFirstCycleAsync(calls);

        _time.Advance(TimeSpan.FromHours(1));

        await calls.Reached.WaitAsync(SignalTimeout);
        await Should.NotThrowAsync(() => sut.StopAsync(CancellationToken.None));

        (sut.ExecuteTask?.IsFaulted).ShouldBe(false);
    }

    [Fact]
    public async Task ExecuteAsync_StoreThrows_KeepsTheLoopAlive()
    {
        var calls = new CallCounter(2);
        _service.GetExpiredAssessmentsAsync(Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, IReadOnlyList<DPIAReadModel>>>>(_ =>
            {
                calls.Increment();
                throw new InvalidOperationException("Connection failed");
            });

        var sut = CreateSut(new DPIAOptions { PublishNotifications = false });

        await sut.StartAsync(CancellationToken.None);
        await WaitForFirstCycleAsync(calls);

        _time.Advance(TimeSpan.FromHours(1));

        await calls.Reached.WaitAsync(SignalTimeout);
        await Should.NotThrowAsync(() => sut.StopAsync(CancellationToken.None));

        (sut.ExecuteTask?.IsFaulted).ShouldBe(false);
    }

    [Fact]
    public async Task ExecuteAsync_StoppedDuringCycle_LogsTheCycleAsCancelled()
    {
        var cycleStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _service.GetExpiredAssessmentsAsync(Arg.Any<CancellationToken>())
            .Returns(call => WaitForCancellationAsync(cycleStarted, call.Arg<CancellationToken>()));

        var logger = new ComplianceLogCapture<DPIAReviewReminderService>(e => e.Message.Contains("cancelled"));
        var sut = new DPIAReviewReminderService(
            CreateScopeFactory(),
            Options.Create(new DPIAOptions { PublishNotifications = false }),
            _time,
            logger);

        await sut.StartAsync(CancellationToken.None);
        await cycleStarted.Task.WaitAsync(SignalTimeout);
        await sut.StopAsync(CancellationToken.None);
        await logger.Signaled.WaitAsync(SignalTimeout);

        logger.Entries.ShouldContain(e => e.Message.Contains("cancelled") && e.Exception == null);
    }

    #endregion

    #region Registration

    [Fact]
    public void AddEncinaDPIA_WithoutAnyOption_RegistersTheReminderServiceAndValidatesTheGraph()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_service);

        services.AddEncinaDPIA();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        provider.GetServices<IHostedService>()
            .OfType<DPIAReviewReminderService>()
            .Count()
            .ShouldBe(1);
    }

    [Fact]
    public void AddEncinaDPIA_IntervalOutsidePeriodicTimerRange_FailsWhenTheReminderServiceIsResolved()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_service);

        services.AddEncinaDPIA(o => o.ExpirationCheckInterval = TimeSpan.Zero);

        using var provider = services.BuildServiceProvider();

        // The host resolves its hosted services at start, so a bad interval stops the start loudly
        var ex = Should.Throw<OptionsValidationException>(() => provider.GetServices<IHostedService>().ToList());
        ex.Message.ShouldContain("ExpirationCheckInterval");
    }

    [Fact]
    public void AddEncinaDPIA_CalledTwice_RegistersTheReminderServiceOnce()
    {
        var services = new ServiceCollection();

        services.AddEncinaDPIA();
        services.AddEncinaDPIA();

        services.Count(d => d.ServiceType == typeof(IHostedService)
                            && d.ImplementationType == typeof(DPIAReviewReminderService))
            .ShouldBe(1);
    }

    #endregion

    #region Constructor

    [Fact]
    public void Constructor_NullScopeFactory_ThrowsArgumentNullException()
    {
        var act = () => new DPIAReviewReminderService(
            null!,
            Options.Create(new DPIAOptions()),
            _time,
            new NullLogger<DPIAReviewReminderService>());

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("scopeFactory");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var scopeFactory = Substitute.For<IServiceScopeFactory>();

        var act = () => new DPIAReviewReminderService(
            scopeFactory,
            null!,
            _time,
            new NullLogger<DPIAReviewReminderService>());

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var scopeFactory = Substitute.For<IServiceScopeFactory>();

        var act = () => new DPIAReviewReminderService(
            scopeFactory,
            Options.Create(new DPIAOptions()),
            null!,
            new NullLogger<DPIAReviewReminderService>());

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var scopeFactory = Substitute.For<IServiceScopeFactory>();

        var act = () => new DPIAReviewReminderService(
            scopeFactory,
            Options.Create(new DPIAOptions()),
            _time,
            null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("logger");
    }

    #endregion

    #region Helpers

    private static ValueTask<Either<EncinaError, IReadOnlyList<DPIAReadModel>>> EmptyResult()
        => ValueTask.FromResult(
            Prelude.Right<EncinaError, IReadOnlyList<DPIAReadModel>>(
                Array.Empty<DPIAReadModel>() as IReadOnlyList<DPIAReadModel>));

    private void SetupEmpty(CallCounter calls)
        => _service.GetExpiredAssessmentsAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                calls.Increment();
                return EmptyResult();
            });

    // The first cycle runs before the timer is awaited and after the PeriodicTimer is created,
    // so a tick fired by Advance once the first call is observed is never lost.
    private static Task WaitForFirstCycleAsync(CallCounter calls) => calls.First.WaitAsync(SignalTimeout);

    private static async ValueTask<Either<EncinaError, IReadOnlyList<DPIAReadModel>>> WaitForCancellationAsync(
        TaskCompletionSource cycleStarted,
        CancellationToken cancellationToken)
    {
        cycleStarted.TrySetResult();

        // Completes only when the stopping token is cancelled; no real time elapses
        await Task.Delay(Timeout.Infinite, cancellationToken);
        return Prelude.Right<EncinaError, IReadOnlyList<DPIAReadModel>>(Array.Empty<DPIAReadModel>());
    }

    // Use the real scope factory from the built ServiceProvider so CreateAsyncScope() works
    private IServiceScopeFactory CreateScopeFactory()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_service);

        return services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }

    private DPIAReviewReminderService CreateSut(DPIAOptions options)
        => new(
            CreateScopeFactory(),
            Options.Create(options),
            _time,
            new NullLogger<DPIAReviewReminderService>());

    /// <summary>Counts store calls and completes a signal when a given count is reached.</summary>
    private sealed class CallCounter(int target)
    {
        private readonly TaskCompletionSource _first = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _reached = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _count;

        public Task First => _first.Task;

        public Task Reached => _reached.Task;

        public int Count => Volatile.Read(ref _count);

        public void Increment()
        {
            var count = Interlocked.Increment(ref _count);
            _first.TrySetResult();
            if (count >= target)
            {
                _reached.TrySetResult();
            }
        }
    }

    #endregion
}
