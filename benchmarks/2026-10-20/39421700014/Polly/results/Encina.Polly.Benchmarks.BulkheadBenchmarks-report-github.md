```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|-------------:|----------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  7,566.8 ns |    876.78 ns | 732.15 ns |  1.01 |    0.13 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  7,156.4 ns |    267.75 ns | 223.58 ns |  0.95 |    0.09 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    615.9 ns |     20.06 ns |  18.77 ns |  0.08 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  7,377.9 ns |    563.55 ns | 499.57 ns |  0.98 |    0.11 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 14,449.5 ns |    540.87 ns | 451.65 ns |  1.93 |    0.18 |    5528 B |        7.05 |
|                                |            |                |             |             |             |              |           |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  7,472.5 ns |  5,963.13 ns | 326.86 ns |  1.00 |    0.05 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           |  7,632.3 ns |  6,586.83 ns | 361.05 ns |  1.02 |    0.06 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    569.7 ns |  2,111.89 ns | 115.76 ns |  0.08 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  7,474.0 ns |  3,480.68 ns | 190.79 ns |  1.00 |    0.04 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 11,653.7 ns | 10,396.91 ns | 569.89 ns |  1.56 |    0.09 |    5528 B |        7.05 |
