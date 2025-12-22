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
    private Func<SampleHandler, SampleNotification, CancellationToken, Task<Either<MediatorError, Unit>>> _compiledDelegate = default!;
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
    /// Baseline: Direct method call (fastest possible)
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task<Unit> DirectCall()
    {
        var result = await _handler.Handle(_notification, _ct).ConfigureAwait(false);
        return result.Match(
            Left: _ => Unit.Default,
            Right: u => u);
    }

    /// <summary>
    /// Expression tree compiled delegate (what Encina uses after cache warmup)
    /// </summary>
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
    [Benchmark]
    public async Task<Unit> MethodInfoInvoke()
    {
        var task = (Task<Either<MediatorError, Unit>>)_methodInfo.Invoke(_handler, new object[] { _notification, _ct })!;
        var result = await task.ConfigureAwait(false);
        return result.Match(
            Left: _ => Unit.Default,
            Right: u => u);
    }

    /// <summary>
    /// Generic type construction + reflection (worst case)
    /// </summary>
    [Benchmark]
    public async Task<Unit> GenericTypeConstruction()
    {
        // Cache the generic type to avoid CLR internal errors from repeated MakeGenericType calls
        var handlerType = _genericHandlerType;
        var method = handlerType.GetMethod("Handle")!;
        var task = (Task<Either<MediatorError, Unit>>)method.Invoke(_handler, new object[] { _notification, _ct })!;
        var result = await task.ConfigureAwait(false);
        return result.Match(
            Left: _ => Unit.Default,
            Right: u => u);
    }

    private Type _genericHandlerType = default!;

    /// <summary>
    /// Simulates the first call cost: expression compilation
    /// </summary>
    [Benchmark]
    public Func<SampleHandler, SampleNotification, CancellationToken, Task<Either<MediatorError, Unit>>> ExpressionCompilation()
    {
        return CreateCompiledDelegate(_methodInfo);
    }

    private static Func<SampleHandler, SampleNotification, CancellationToken, Task<Either<MediatorError, Unit>>> CreateCompiledDelegate(MethodInfo method)
    {
        // Simulate what Encina does in NotificationHandlerInvokerCache
        var handlerParam = Expression.Parameter(typeof(SampleHandler), "handler");
        var notificationParam = Expression.Parameter(typeof(SampleNotification), "notification");
        var ctParam = Expression.Parameter(typeof(CancellationToken), "ct");

        var call = Expression.Call(handlerParam, method, notificationParam, ctParam);

        var lambda = Expression.Lambda<Func<SampleHandler, SampleNotification, CancellationToken, Task<Either<MediatorError, Unit>>>>(
            call, handlerParam, notificationParam, ctParam);

        return lambda.Compile();
    }

    // Test types (public for delegate signature accessibility)
    public sealed record SampleNotification(Guid Id) : INotification;

    public sealed class SampleHandler : INotificationHandler<SampleNotification>
    {
        public Task<Either<MediatorError, Unit>> Handle(SampleNotification notification, CancellationToken cancellationToken)
        {
            // Minimal work to isolate invocation overhead
            return Task.FromResult(Right<MediatorError, Unit>(Unit.Default));
        }
    }
}
