```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error         | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|--------------:|------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           | 3,320.17 ns |     364.09 ns |   284.26 ns | 3,310.00 ns |  1.01 |    0.12 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           | 1,724.65 ns |     432.38 ns |   361.06 ns | 1,592.50 ns |  0.52 |    0.11 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    84.27 ns |      41.67 ns |    34.79 ns |    79.50 ns |  0.03 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           | 3,253.08 ns |     292.66 ns |   244.38 ns | 3,140.00 ns |  0.99 |    0.11 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 3,797.19 ns |     309.14 ns |   258.14 ns | 3,680.50 ns |  1.15 |    0.12 |    5528 B |        7.05 |
|                                |            |                |             |             |             |               |             |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           | 4,327.00 ns |  41,518.61 ns | 2,275.78 ns | 3,886.00 ns |  1.21 |    0.81 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           | 7,780.00 ns | 145,582.54 ns | 7,979.87 ns | 3,390.00 ns |  2.17 |    2.31 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |   367.00 ns |   7,651.87 ns |   419.42 ns |   140.00 ns |  0.10 |    0.12 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           | 4,685.33 ns |  52,375.45 ns | 2,870.88 ns | 4,061.00 ns |  1.31 |    0.96 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 9,581.00 ns |  42,332.11 ns | 2,320.37 ns | 8,813.00 ns |  2.68 |    1.35 |    5528 B |        7.05 |
