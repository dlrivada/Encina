```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.23GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-NTRUNJ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3


```
| Method                             | Job        | IterationCount | LaunchCount | WarmupCount | Mean | Error | Ratio | RatioSD | Alloc Ratio |
|----------------------------------- |----------- |--------------- |------------ |------------ |-----:|------:|------:|--------:|------------:|
| NoRetryAttribute_Baseline          | Job-NTRUNJ | 5              | Default     | 3           |   NA |    NA |     ? |       ? |           ? |
| WithRetryAttribute_NoActualRetries | Job-NTRUNJ | 5              | Default     | 3           |   NA |    NA |     ? |       ? |           ? |
|                                    |            |                |             |             |      |       |       |         |             |
| NoRetryAttribute_Baseline          | MediumRun  | 15             | 2           | 10          |   NA |    NA |     ? |       ? |           ? |
| WithRetryAttribute_NoActualRetries | MediumRun  | 15             | 2           | 10          |   NA |    NA |     ? |       ? |           ? |

Benchmarks with issues:
  RetryBenchmarks.NoRetryAttribute_Baseline: Job-NTRUNJ(IterationCount=5, WarmupCount=3)
  RetryBenchmarks.WithRetryAttribute_NoActualRetries: Job-NTRUNJ(IterationCount=5, WarmupCount=3)
  RetryBenchmarks.NoRetryAttribute_Baseline: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  RetryBenchmarks.WithRetryAttribute_NoActualRetries: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
