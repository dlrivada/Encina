using System.Linq.Expressions;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Benchmarks;

/// <summary>
/// Micro-benchmarks to measure the overhead of different invocation strategies.
/// These benchmarks validate the performance claims in ADR-003 (Caching Strategy).
/// </summary>
[MemoryDiagnoser]
public class DelegateInvocationBenchmarks
{
    private SampleHandler _handler = default!;
    private SampleNotification _notification = default!;
    private CancellationToken _ct;

    // Different invocation strategies
    private Func<SampleHandler, SampleNotification, CancellationToken, Task<Either<EncinaError, Unit>>> _compiledDelegate = default!;
    private MethodInfo _methodInfo = default!;

    [GlobalSetup]
    public void Setup()
    {
        _handler = new SampleHandler();
        _notification = new SampleNotification(Guid.NewGuid());
        _ct = CancellationToken.None;

        // Pre-compile the expression tree delegate (simulates cache hit)
        _methodInfo = typeof(SampleHandler).GetMethod(nameof(SampleHandler.Handle))!;
        _compiledDelegate = CreateCompiledDelegate(_methodInfo);

        // Pre-create generic type for GenericTypeConstruction benchmark
        _genericHandlerType = typeof(INotificationHandler<>).MakeGenericType(typeof(SampleNotification));
    }

    /// <summary>
    /// Baseline: Direct method call (fastest possible).
    /// Uses <c>OperationsPerInvoke</c> to amortize the sub-nanosecond per-call cost
    /// over 1 000 invocations so BDN's measurement overhead does not dominate the signal
    /// (Palanca 4). Without this, the benchmark reports CoV ~30 % at N = 30 because the
    /// per-iteration variance is dominated by CPU jitter rather than code behavior.
    /// </summary>
    private const int DirectCallOps = 1000;

    [BenchmarkCategory("DocRef:bench:delegates/direct-call")]
    [Benchmark(Baseline = true, OperationsPerInvoke = DirectCallOps)]
    public async Task<Unit> DirectCall()
    {
        Unit last = Unit.Default;
        for (int i = 0; i < DirectCallOps; i++)
        {
            var result = await _handler.Handle(_notification, _ct).ConfigureAwait(false);
            last = result.Match(Left: _ => Unit.Default, Right: u => u);
        }
        return last;
    }

    /// <summary>
    /// Expression tree compiled delegate (what Encina uses after cache warmup)
    /// </summary>
    [BenchmarkCategory("DocRef:bench:delegates/compiled-delegate")]
    [Benchmark]
    public async Task<Unit> CompiledDelegate()
    {
        var result = await _compiledDelegate(_handler, _notification, _ct).ConfigureAwait(false);
        return result.Match(
            Left: _ => Unit.Default,
            Right: u => u);
    }

    /// <summary>
    /// Reflection with MethodInfo.Invoke (slow baseline)
    /// </summary>
    [BenchmarkCategory("DocRef:bench:delegates/methodinfo-invoke")]
    [Benchmark]
    public async Task<Unit> MethodInfoInvoke()
    {
        var task = (Task<Either<EncinaError, Unit>>)_methodInfo.Invoke(_handler, new object[] { _notification, _ct })!;
        var result = await task.ConfigureAwait(false);
        return result.Match(
            Left: _ => Unit.Default,
            Right: u => u);
    }

    /// <summary>
    /// Generic type construction + reflection (worst case)
    /// </summary>
    [BenchmarkCategory("DocRef:bench:delegates/generic-type-construction")]
    [Benchmark]
    public async Task<Unit> GenericTypeConstruction()
    {
        // Cache the generic type to avoid CLR internal errors from repeated MakeGenericType calls
        var handlerType = _genericHandlerType;
        var method = handlerType.GetMethod("Handle")!;
        var task = (Task<Either<EncinaError, Unit>>)method.Invoke(_handler, new object[] { _notification, _ct })!;
        var result = await task.ConfigureAwait(false);
        return result.Match(
            Left: _ => Unit.Default,
            Right: u => u);
    }

    private Type _genericHandlerType = default!;

    /// <summary>
    /// Simulates the first call cost: expression compilation
    /// </summary>
    [BenchmarkCategory("DocRef:bench:delegates/expression-compilation")]
    [Benchmark]
    public Func<SampleHandler, SampleNotification, CancellationToken, Task<Either<EncinaError, Unit>>> ExpressionCompilation()
    {
        return CreateCompiledDelegate(_methodInfo);
    }

    private static Func<SampleHandler, SampleNotification, CancellationToken, Task<Either<EncinaError, Unit>>> CreateCompiledDelegate(MethodInfo method)
    {
        // Simulate what Encina does in NotificationHandlerInvokerCache
        var handlerParam = Expression.Parameter(typeof(SampleHandler), "handler");
        var notificationParam = Expression.Parameter(typeof(SampleNotification), "notification");
        var ctParam = Expression.Parameter(typeof(CancellationToken), "ct");

        var call = Expression.Call(handlerParam, method, notificationParam, ctParam);

        var lambda = Expression.Lambda<Func<SampleHandler, SampleNotification, CancellationToken, Task<Either<EncinaError, Unit>>>>(
            call, handlerParam, notificationParam, ctParam);

        return lambda.Compile();
    }

    // Test types (public for delegate signature accessibility)
    public sealed record SampleNotification(Guid Id) : INotification;

    public sealed class SampleHandler : INotificationHandler<SampleNotification>
    {
        public Task<Either<EncinaError, Unit>> Handle(SampleNotification notification, CancellationToken cancellationToken)
        {
            // Minimal work to isolate invocation overhead
            return Task.FromResult(Right<EncinaError, Unit>(Unit.Default));
        }
    }
}
