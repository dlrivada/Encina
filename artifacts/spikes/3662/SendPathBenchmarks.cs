using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using LanguageExt;

namespace RopUnions.Spike;

/// <summary>Reference-type response, like most Encina query results (DTOs).</summary>
public sealed record OrderDto(int Id, string Name);

/// <summary>
/// IEncina.Send-style hot path: a non-inlined handler returns success or EncinaError, the caller pattern-matches it.
/// (a) LanguageExt Either (baseline), (b) standard C# 15 union, (c) custom non-boxing union, (d) closed hierarchy.
/// </summary>
[MemoryDiagnoser]
[CategoriesColumn]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public class SendPathBenchmarks
{
    private static readonly EncinaError Err = EncinaError.New("Order not found");
    private static readonly OrderDto Dto = new(1, "order");
    private int _input = 41;

    // ---------------- handlers ----------------
    [MethodImpl(MethodImplOptions.NoInlining)] private Either<EncinaError, int> HandleEither(bool ok) => ok ? _input + 1 : Err;
    [MethodImpl(MethodImplOptions.NoInlining)] private StdResult<int> HandleStd(bool ok) => ok ? new StdResult<int>(_input + 1) : new StdResult<int>(Err);
    [MethodImpl(MethodImplOptions.NoInlining)] private Result<int> HandleCustom(bool ok) => ok ? new Result<int>(_input + 1) : new Result<int>(Err);
    [MethodImpl(MethodImplOptions.NoInlining)] private HResult<int> HandleHier(bool ok) => ok ? new HResult<int>.Ok(_input + 1) : new HResult<int>.Fail(Err);

    [MethodImpl(MethodImplOptions.NoInlining)] private static Either<EncinaError, OrderDto> HandleEitherRef() => Dto;
    [MethodImpl(MethodImplOptions.NoInlining)] private static StdResult<OrderDto> HandleStdRef() => new(Dto);
    [MethodImpl(MethodImplOptions.NoInlining)] private static Result<OrderDto> HandleCustomRef() => new(Dto);
    [MethodImpl(MethodImplOptions.NoInlining)] private static HResult<OrderDto> HandleHierRef() => new HResult<OrderDto>.Ok(Dto);

    // ---------------- success, value T ----------------
    [Benchmark(Baseline = true), BenchmarkCategory("Success_int")]
    public int Either_Success() => HandleEither(true).Match(Right: v => v, Left: _ => -1);
    [Benchmark, BenchmarkCategory("Success_int")]
    public int StdUnion_Success() => HandleStd(true) switch { int v => v, EncinaError => -1 };
    [Benchmark, BenchmarkCategory("Success_int")]
    public int Custom_Success() => HandleCustom(true) switch { int v => v, EncinaError => -1 };
    [Benchmark, BenchmarkCategory("Success_int")]
    public int Hierarchy_Success() => HandleHier(true) switch { HResult<int>.Ok o => o.Value, _ => -1 };

    // ---------------- success, reference T ----------------
    [Benchmark(Baseline = true), BenchmarkCategory("Success_ref")]
    public int Either_SuccessRef() => HandleEitherRef().Match(Right: v => v.Id, Left: _ => -1);
    [Benchmark, BenchmarkCategory("Success_ref")]
    public int StdUnion_SuccessRef() => HandleStdRef() switch { OrderDto v => v.Id, EncinaError => -1 };
    [Benchmark, BenchmarkCategory("Success_ref")]
    public int Custom_SuccessRef() => HandleCustomRef() switch { OrderDto v => v.Id, EncinaError => -1 };
    [Benchmark, BenchmarkCategory("Success_ref")]
    public int Hierarchy_SuccessRef() => HandleHierRef() switch { HResult<OrderDto>.Ok o => o.Value.Id, _ => -1 };

    // ---------------- failure ----------------
    [Benchmark(Baseline = true), BenchmarkCategory("Failure")]
    public int Either_Failure() => HandleEither(false).Match(Right: v => v, Left: e => e.Message.Length);
    [Benchmark, BenchmarkCategory("Failure")]
    public int StdUnion_Failure() => HandleStd(false) switch { int v => v, EncinaError e => e.Message.Length };
    [Benchmark, BenchmarkCategory("Failure")]
    public int Custom_Failure() => HandleCustom(false) switch { int v => v, EncinaError e => e.Message.Length };
    [Benchmark, BenchmarkCategory("Failure")]
    public int Hierarchy_Failure() => HandleHier(false) switch { HResult<int>.Fail f => f.Error.Message.Length, HResult<int>.Ok o => o.Value, _ => 0 };

    // ---------------- Bind -> Map -> Bind chain (success path) ----------------
    [Benchmark(Baseline = true), BenchmarkCategory("Chain3")]
    public int Either_Chain3() => HandleEither(true)
        .Bind(static v => v > 0 ? Either<EncinaError, int>.Right(v * 2) : Either<EncinaError, int>.Left(Err))
        .Map(static v => v + 1)
        .Bind(static v => v < 1_000_000 ? Either<EncinaError, long>.Right(v) : Either<EncinaError, long>.Left(Err))
        .Match(Right: static v => (int)v, Left: static _ => -1);
    [Benchmark, BenchmarkCategory("Chain3")]
    public int StdUnion_Chain3() => HandleStd(true)
        .Bind(static v => v > 0 ? new StdResult<int>(v * 2) : new StdResult<int>(Err))
        .Map(static v => v + 1)
        .Bind(static v => v < 1_000_000 ? new StdResult<long>((long)v) : new StdResult<long>(Err))
        .Match(static v => (int)v, static _ => -1);
    [Benchmark, BenchmarkCategory("Chain3")]
    public int Custom_Chain3() => HandleCustom(true)
        .Bind(static v => v > 0 ? new Result<int>(v * 2) : new Result<int>(Err))
        .Map(static v => v + 1)
        .Bind(static v => v < 1_000_000 ? new Result<long>((long)v) : new Result<long>(Err))
        .Match(static v => (int)v, static _ => -1);
    [Benchmark, BenchmarkCategory("Chain3")]
    public int Hierarchy_Chain3() => HandleHier(true)
        .Bind(static v => v > 0 ? new HResult<int>.Ok(v * 2) : (HResult<int>)new HResult<int>.Fail(Err))
        .Map(static v => v + 1)
        .Bind(static v => v < 1_000_000 ? new HResult<long>.Ok(v) : (HResult<long>)new HResult<long>.Fail(Err))
        .Match(static v => (int)v, static _ => -1);

    // ---------------- Bind -> Map -> Bind chain (failure at step 1, short-circuit) ----------------
    [Benchmark(Baseline = true), BenchmarkCategory("Chain3_failure")]
    public int Either_Chain3Failure() => HandleEither(false)
        .Bind(static v => Either<EncinaError, int>.Right(v * 2))
        .Map(static v => v + 1)
        .Bind(static v => Either<EncinaError, long>.Right(v))
        .Match(Right: static v => (int)v, Left: static _ => -1);
    [Benchmark, BenchmarkCategory("Chain3_failure")]
    public int StdUnion_Chain3Failure() => HandleStd(false)
        .Bind(static v => new StdResult<int>(v * 2))
        .Map(static v => v + 1)
        .Bind(static v => new StdResult<long>((long)v))
        .Match(static v => (int)v, static _ => -1);
    [Benchmark, BenchmarkCategory("Chain3_failure")]
    public int Custom_Chain3Failure() => HandleCustom(false)
        .Bind(static v => new Result<int>(v * 2))
        .Map(static v => v + 1)
        .Bind(static v => new Result<long>((long)v))
        .Match(static v => (int)v, static _ => -1);
    [Benchmark, BenchmarkCategory("Chain3_failure")]
    public int Hierarchy_Chain3Failure() => HandleHier(false)
        .Bind(static v => (HResult<int>)new HResult<int>.Ok(v * 2))
        .Map(static v => v + 1)
        .Bind(static v => (HResult<long>)new HResult<long>.Ok(v))
        .Match(static v => (int)v, static _ => -1);

    // ---------------- async ValueTask wrapping (IEncina.Send shape, synchronous completion) ----------------
    [MethodImpl(MethodImplOptions.NoInlining)] private ValueTask<Either<EncinaError, int>> HandleEitherAsync(bool ok) => new(HandleEither(ok));
    [MethodImpl(MethodImplOptions.NoInlining)] private ValueTask<StdResult<int>> HandleStdAsync(bool ok) => new(HandleStd(ok));
    [MethodImpl(MethodImplOptions.NoInlining)] private ValueTask<Result<int>> HandleCustomAsync(bool ok) => new(HandleCustom(ok));
    [MethodImpl(MethodImplOptions.NoInlining)] private ValueTask<HResult<int>> HandleHierAsync(bool ok) => new(HandleHier(ok));

    private async ValueTask<Either<EncinaError, int>> SendEither(bool ok) => await HandleEitherAsync(ok).ConfigureAwait(false);
    private async ValueTask<StdResult<int>> SendStd(bool ok) => await HandleStdAsync(ok).ConfigureAwait(false);
    private async ValueTask<Result<int>> SendCustom(bool ok) => await HandleCustomAsync(ok).ConfigureAwait(false);
    private async ValueTask<HResult<int>> SendHier(bool ok) => await HandleHierAsync(ok).ConfigureAwait(false);

    [Benchmark(Baseline = true), BenchmarkCategory("Async_success")]
    public async ValueTask<int> Either_Async() => (await SendEither(true).ConfigureAwait(false)).Match(Right: v => v, Left: _ => -1);
    [Benchmark, BenchmarkCategory("Async_success")]
    public async ValueTask<int> StdUnion_Async() => (await SendStd(true).ConfigureAwait(false)) switch { int v => v, EncinaError => -1 };
    [Benchmark, BenchmarkCategory("Async_success")]
    public async ValueTask<int> Custom_Async() => (await SendCustom(true).ConfigureAwait(false)) switch { int v => v, EncinaError => -1 };
    [Benchmark, BenchmarkCategory("Async_success")]
    public async ValueTask<int> Hierarchy_Async() => (await SendHier(true).ConfigureAwait(false)) switch { HResult<int>.Ok o => o.Value, _ => -1 };

    [Benchmark(Baseline = true), BenchmarkCategory("Async_failure")]
    public async ValueTask<int> Either_AsyncFailure() => (await SendEither(false).ConfigureAwait(false)).Match(Right: v => v, Left: e => e.Message.Length);
    [Benchmark, BenchmarkCategory("Async_failure")]
    public async ValueTask<int> StdUnion_AsyncFailure() => (await SendStd(false).ConfigureAwait(false)) switch { int v => v, EncinaError e => e.Message.Length };
    [Benchmark, BenchmarkCategory("Async_failure")]
    public async ValueTask<int> Custom_AsyncFailure() => (await SendCustom(false).ConfigureAwait(false)) switch { int v => v, EncinaError e => e.Message.Length };
    [Benchmark, BenchmarkCategory("Async_failure")]
    public async ValueTask<int> Hierarchy_AsyncFailure() => (await SendHier(false).ConfigureAwait(false)) switch { HResult<int>.Fail f => f.Error.Message.Length, _ => -1 };
}
