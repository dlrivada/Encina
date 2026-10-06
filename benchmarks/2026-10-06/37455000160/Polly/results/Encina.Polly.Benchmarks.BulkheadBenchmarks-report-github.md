```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|-------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  6,221.7 ns |  1,758.36 ns | 1,468.31 ns |  1.06 |    0.37 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  6,337.1 ns |    471.53 ns |   418.00 ns |  1.08 |    0.28 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    267.6 ns |     88.32 ns |    78.29 ns |  0.05 |    0.02 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  4,783.3 ns |    855.50 ns |   714.38 ns |  0.81 |    0.24 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 12,402.4 ns |    792.66 ns |   702.67 ns |  2.11 |    0.55 |    5528 B |        7.05 |
|                                |            |                |             |             |             |              |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  7,146.8 ns | 61,670.99 ns | 3,380.39 ns |  1.14 |    0.63 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           |  7,363.2 ns | 61,251.82 ns | 3,357.42 ns |  1.18 |    0.64 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    553.3 ns |  4,696.88 ns |   257.45 ns |  0.09 |    0.05 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  8,581.7 ns | 50,022.73 ns | 2,741.91 ns |  1.37 |    0.62 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 12,318.2 ns | 52,586.32 ns | 2,882.43 ns |  1.97 |    0.80 |    5528 B |        7.05 |
