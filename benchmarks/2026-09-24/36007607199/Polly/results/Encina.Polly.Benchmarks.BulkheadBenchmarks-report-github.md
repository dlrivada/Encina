```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|-------------:|----------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  8,033.0 ns |    224.00 ns | 198.57 ns |  1.00 |    0.03 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  6,975.8 ns |    114.08 ns |  95.27 ns |  0.87 |    0.02 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    625.7 ns |     29.56 ns |  26.20 ns |  0.08 |    0.00 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  6,863.1 ns |     72.11 ns |  63.92 ns |  0.85 |    0.02 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 10,549.4 ns |    162.23 ns | 143.81 ns |  1.31 |    0.04 |    5528 B |        7.05 |
|                                |            |                |             |             |             |              |           |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  7,256.8 ns |  4,119.38 ns | 225.80 ns |  1.00 |    0.04 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           |  7,674.0 ns | 11,164.82 ns | 611.98 ns |  1.06 |    0.08 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    520.8 ns |  1,574.71 ns |  86.32 ns |  0.07 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  6,980.0 ns |  6,919.57 ns | 379.28 ns |  0.96 |    0.05 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 11,111.3 ns |  7,726.61 ns | 423.52 ns |  1.53 |    0.06 |    5528 B |        7.05 |
