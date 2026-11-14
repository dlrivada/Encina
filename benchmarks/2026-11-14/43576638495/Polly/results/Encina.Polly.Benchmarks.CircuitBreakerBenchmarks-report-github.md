```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-NTRUNJ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                                  | Job        | IterationCount | LaunchCount | Mean | Error | Ratio | RatioSD | Alloc Ratio |
|---------------------------------------- |----------- |--------------- |------------ |-----:|------:|------:|--------:|------------:|
| NoCircuitBreakerAttribute_Baseline      | Job-NTRUNJ | 5              | Default     |   NA |    NA |     ? |       ? |           ? |
| WithCircuitBreakerAttribute_ClosedState | Job-NTRUNJ | 5              | Default     |   NA |    NA |     ? |       ? |           ? |
|                                         |            |                |             |      |       |       |         |             |
| NoCircuitBreakerAttribute_Baseline      | ShortRun   | 3              | 1           |   NA |    NA |     ? |       ? |           ? |
| WithCircuitBreakerAttribute_ClosedState | ShortRun   | 3              | 1           |   NA |    NA |     ? |       ? |           ? |

Benchmarks with issues:
  CircuitBreakerBenchmarks.NoCircuitBreakerAttribute_Baseline: Job-NTRUNJ(IterationCount=5, WarmupCount=3)
  CircuitBreakerBenchmarks.WithCircuitBreakerAttribute_ClosedState: Job-NTRUNJ(IterationCount=5, WarmupCount=3)
  CircuitBreakerBenchmarks.NoCircuitBreakerAttribute_Baseline: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  CircuitBreakerBenchmarks.WithCircuitBreakerAttribute_ClosedState: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
