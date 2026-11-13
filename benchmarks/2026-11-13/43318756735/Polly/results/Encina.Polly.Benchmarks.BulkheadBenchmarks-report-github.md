```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.53GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|-------------:|------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           | 1,903.81 ns |    368.19 ns |   307.45 ns | 1,877.50 ns |  1.02 |    0.23 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           | 1,782.27 ns |    380.36 ns |   317.61 ns | 1,712.50 ns |  0.96 |    0.22 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    47.38 ns |     25.42 ns |    21.23 ns |    45.00 ns |  0.03 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           | 2,035.86 ns |    653.93 ns |   579.70 ns | 1,873.00 ns |  1.10 |    0.35 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 3,451.31 ns |    437.15 ns |   365.04 ns | 3,325.00 ns |  1.86 |    0.35 |    5528 B |        7.05 |
|                                |            |                |             |             |             |              |             |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           | 3,843.67 ns | 42,772.92 ns | 2,344.53 ns | 3,230.00 ns |  1.28 |    0.98 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           | 3,819.33 ns | 40,236.26 ns | 2,205.49 ns | 3,034.00 ns |  1.27 |    0.95 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |   725.83 ns | 18,197.71 ns |   997.48 ns |   164.50 ns |  0.24 |    0.34 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           | 5,150.83 ns | 33,409.53 ns | 1,831.29 ns | 4,746.50 ns |  1.72 |    1.03 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 6,194.67 ns | 44,444.44 ns | 2,436.15 ns | 5,613.00 ns |  2.07 |    1.29 |    5528 B |        7.05 |
