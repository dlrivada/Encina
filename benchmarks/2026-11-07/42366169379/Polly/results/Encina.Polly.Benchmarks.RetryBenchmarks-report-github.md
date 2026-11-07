```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.02GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-NTRUNJ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                             | Job        | IterationCount | LaunchCount | Mean | Error | Ratio | RatioSD | Alloc Ratio |
|----------------------------------- |----------- |--------------- |------------ |-----:|------:|------:|--------:|------------:|
| NoRetryAttribute_Baseline          | Job-NTRUNJ | 5              | Default     |   NA |    NA |     ? |       ? |           ? |
| WithRetryAttribute_NoActualRetries | Job-NTRUNJ | 5              | Default     |   NA |    NA |     ? |       ? |           ? |
|                                    |            |                |             |      |       |       |         |             |
| NoRetryAttribute_Baseline          | ShortRun   | 3              | 1           |   NA |    NA |     ? |       ? |           ? |
| WithRetryAttribute_NoActualRetries | ShortRun   | 3              | 1           |   NA |    NA |     ? |       ? |           ? |

Benchmarks with issues:
  RetryBenchmarks.NoRetryAttribute_Baseline: Job-NTRUNJ(IterationCount=5, WarmupCount=3)
  RetryBenchmarks.WithRetryAttribute_NoActualRetries: Job-NTRUNJ(IterationCount=5, WarmupCount=3)
  RetryBenchmarks.NoRetryAttribute_Baseline: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  RetryBenchmarks.WithRetryAttribute_NoActualRetries: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
